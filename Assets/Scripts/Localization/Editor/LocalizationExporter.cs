using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.Networking;

public static class LocalizationExporter
{
    private const string ExportMenuPath = "Tools/Localization/Export Json From Google Sheet";
    private const string SettingsMenuPath = "Tools/Localization/Select Export Settings";

    private const string DefaultSettingsPath = "Assets/Ignore/LocalizationExportSettings.asset";

    private const string ResourcesRoot = "Assets/Resources/";
    private const string MainAssetFolder = "Assets/Localization/Main/";

    private const string AddressableGroupName = "Localization";
    private const string KeysFilePath = "Assets/Scripts/Localization/LocalizationKeys.cs";

    private const string KeyColumnName = "Key";
    private const string GroupColumnName = "Group";
    private const string CommentRowPrefix = "//";

    private static readonly Regex KeyPattern = new Regex("^[A-Z][A-Z0-9_]*$");

    private static readonly Regex PlaceholderPattern = new Regex(@"\{(\d+)(?:[^}]*)\}");

    private static UnityWebRequest _request;

    [MenuItem(ExportMenuPath)]
    private static void Export()
    {
        if (_request != null)
        {
            Debug.LogWarning($"{nameof(LocalizationExporter)}: export is already in progress.");
            return;
        }

        if (!TryFindSettings(out LocalizationExportSettings settings))
        {
            return;
        }

        if (settings == null || !settings.IsConfigured)
        {
            Debug.LogError($"{nameof(LocalizationExporter)}: set the sheet ID first. Use {SettingsMenuPath}.");
            SelectSettings();
            return;
        }

        _request = UnityWebRequest.Get(settings.CsvUrl);
        _request.SendWebRequest();

        EditorApplication.update += PollRequest;
    }

    [MenuItem(SettingsMenuPath)]
    private static void SelectSettings()
    {
        if (!TryFindSettings(out LocalizationExportSettings settings))
        {
            return;
        }

        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<LocalizationExportSettings>();
            AssetDatabase.CreateAsset(settings, DefaultSettingsPath);
            AssetDatabase.SaveAssets();
        }

        Selection.activeObject = settings;
        EditorGUIUtility.PingObject(settings);
    }

    private static bool TryFindSettings(out LocalizationExportSettings settings)
    {
        string[] guids = AssetDatabase.FindAssets($"t:{nameof(LocalizationExportSettings)}");
        settings = null;

        if (guids.Length == 0)
        {
            return true;
        }

        if (guids.Length > 1)
        {
            string[] paths = new string[guids.Length];

            for (int i = 0; i < guids.Length; i++)
            {
                paths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
            }

            Debug.LogError($"{nameof(LocalizationExporter)}: found {guids.Length} {nameof(LocalizationExportSettings)} assets. Keep only one.\n{string.Join("\n", paths)}");
            return false;
        }

        settings = AssetDatabase.LoadAssetAtPath<LocalizationExportSettings>(AssetDatabase.GUIDToAssetPath(guids[0]));
        return true;
    }

    private static void PollRequest()
    {
        if (!_request.isDone)
        {
            EditorUtility.DisplayProgressBar("Localization", "Downloading Google Sheet...", _request.downloadProgress);
            return;
        }

        EditorApplication.update -= PollRequest;
        EditorUtility.ClearProgressBar();

        try
        {
            if (_request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"{nameof(LocalizationExporter)}: download failed. {_request.error}");
                return;
            }

            string csv = _request.downloadHandler.text;

            if (csv.TrimStart().StartsWith("<", StringComparison.Ordinal))
            {
                Debug.LogError($"{nameof(LocalizationExporter)}: received HTML instead of CSV. Share the sheet as 'Anyone with the link - Viewer'.");
                return;
            }

            ExportFromCsv(csv);
        }
        finally
        {
            _request.Dispose();
            _request = null;
        }
    }

    private static void ExportFromCsv(string csv)
    {
        List<List<string>> rows = CsvParser.Parse(csv);
        List<string> errors = new List<string>();
        List<string> warnings = new List<string>();

        if (rows.Count == 0)
        {
            Debug.LogError($"{nameof(LocalizationExporter)}: the sheet is empty.");
            return;
        }

        List<string> header = rows[0];

        int keyColumn = FindColumn(header, KeyColumnName);
        int groupColumn = FindColumn(header, GroupColumnName);

        LocalizationGroup[] groups = (LocalizationGroup[])Enum.GetValues(typeof(LocalizationGroup));
        Language[] languages = (Language[])Enum.GetValues(typeof(Language));

        Dictionary<Language, int> languageColumns = new Dictionary<Language, int>();

        if (keyColumn < 0)
        {
            errors.Add($"header has no '{KeyColumnName}' column.");
        }

        foreach (Language language in languages)
        {
            string code = LocalizationManager.GetCode(language);
            int column = FindColumn(header, code);

            if (column < 0)
            {
                errors.Add($"header has no '{code}' column.");
                continue;
            }

            languageColumns[language] = column;
        }

        if (errors.Count > 0)
        {
            LogResult(errors, warnings);
            return;
        }

        Dictionary<LocalizationGroup, Dictionary<Language, Dictionary<string, string>>> tables = new Dictionary<LocalizationGroup, Dictionary<Language, Dictionary<string, string>>>();
        Dictionary<LocalizationGroup, int> keyCounts = new Dictionary<LocalizationGroup, int>();

        HashSet<string> keys = new HashSet<string>();
        List<string> orderedKeys = new List<string>();

        foreach (LocalizationGroup group in groups)
        {
            tables[group] = new Dictionary<Language, Dictionary<string, string>>();
            keyCounts[group] = 0;

            foreach (Language language in languages)
            {
                tables[group][language] = new Dictionary<string, string>();
            }
        }

        for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
        {
            List<string> row = rows[rowIndex];
            string key = GetCell(row, keyColumn).Trim();

            int sheetRow = rowIndex + 1;

            if (key.Length == 0 || key.StartsWith(CommentRowPrefix, StringComparison.Ordinal))
            {
                continue;
            }

            if (!KeyPattern.IsMatch(key))
            {
                errors.Add($"row {sheetRow}: key '{key}' must use only A-Z, 0-9 and '_' and start with a letter.");
                continue;
            }

            if (!keys.Add(key))
            {
                errors.Add($"row {sheetRow}: duplicate key '{key}'.");
                continue;
            }

            string groupText = groupColumn < 0 ? string.Empty : GetCell(row, groupColumn).Trim();

            if (!TryParseGroup(groupText, out LocalizationGroup rowGroup))
            {
                errors.Add($"row {sheetRow}: '{key}' has unknown group '{groupText}'. Use {string.Join(", ", Enum.GetNames(typeof(LocalizationGroup)))} or leave it empty for {LocalizationGroup.Main}.");
                continue;
            }

            orderedKeys.Add(key);
            keyCounts[rowGroup]++;

            string fallbackValue = GetCell(row, languageColumns[LocalizationManager.FallbackLanguage]);

            foreach (Language language in languages)
            {
                string value = GetCell(row, languageColumns[language]);
                string code = LocalizationManager.GetCode(language);

                if (value.Length == 0)
                {
                    if (language == LocalizationManager.FallbackLanguage)
                    {
                        errors.Add($"row {sheetRow}: '{key}' has no '{code}' text.");
                    }
                    else
                    {
                        warnings.Add($"row {sheetRow}: '{key}' has no '{code}' translation. '{LocalizationManager.GetCode(LocalizationManager.FallbackLanguage)}' will be shown.");
                    }

                    continue;
                }

                if (language != LocalizationManager.FallbackLanguage && !HasSamePlaceholders(fallbackValue, value))
                {
                    warnings.Add($"row {sheetRow}: '{key}' placeholders differ between '{LocalizationManager.GetCode(LocalizationManager.FallbackLanguage)}' and '{code}'.");
                }

                tables[rowGroup][language][key] = value;
            }
        }

        string keysSource = BuildKeysSource(orderedKeys, errors);

        if (errors.Count > 0)
        {
            LogResult(errors, warnings);
            return;
        }

        foreach (Language language in languages)
        {
            WriteIfChanged(GetBootAssetPath(language), JsonConvert.SerializeObject(tables[LocalizationGroup.Boot][language], Formatting.Indented));
            WriteIfChanged(GetMainAssetPath(language), JsonConvert.SerializeObject(tables[LocalizationGroup.Main][language], Formatting.Indented));
        }

        WriteIfChanged(KeysFilePath, keysSource);

        AssetDatabase.Refresh();
        RegisterMainAddressables(languages);

        LogResult(errors, warnings);
        Debug.Log($"{nameof(LocalizationExporter)}: exported {orderedKeys.Count} keys (Boot {keyCounts[LocalizationGroup.Boot]}, Main {keyCounts[LocalizationGroup.Main]}) for {languages.Length} languages.");
    }

    private static string GetBootAssetPath(Language language)
    {
        return ResourcesRoot + LocalizationManager.GetBootResourcesPath(language) + ".json";
    }

    private static string GetMainAssetPath(Language language)
    {
        return MainAssetFolder + LocalizationManager.GetCode(language) + ".json";
    }

    private static void RegisterMainAddressables(Language[] languages)
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
        AddressableAssetGroup group = settings.FindGroup(AddressableGroupName);

        if (group == null)
        {
            group = settings.CreateGroup(AddressableGroupName, false, false, true, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
        }

        foreach (Language language in languages)
        {
            string path = GetMainAssetPath(language);
            string guid = AssetDatabase.AssetPathToGUID(path);

            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogError($"{nameof(LocalizationExporter)}: '{path}' is not imported. Addressable entry was not created.");
                continue;
            }

            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, false, false);
            entry.SetAddress(LocalizationManager.GetMainAddress(language), false);
        }

        settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);
        AssetDatabase.SaveAssets();
    }

    private static bool TryParseGroup(string text, out LocalizationGroup group)
    {
        if (text.Length == 0)
        {
            group = LocalizationGroup.Main;
            return true;
        }

        return Enum.TryParse(text, true, out group) && Enum.IsDefined(typeof(LocalizationGroup), group);
    }

    private static int FindColumn(List<string> header, string name)
    {
        for (int i = 0; i < header.Count; i++)
        {
            if (string.Equals(header[i].Trim(), name, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private static string GetCell(List<string> row, int column)
    {
        return column < row.Count ? row[column] : string.Empty;
    }

    private static bool HasSamePlaceholders(string source, string translation)
    {
        HashSet<string> sourcePlaceholders = CollectPlaceholders(source);
        HashSet<string> translationPlaceholders = CollectPlaceholders(translation);

        return sourcePlaceholders.SetEquals(translationPlaceholders);
    }

    private static HashSet<string> CollectPlaceholders(string text)
    {
        HashSet<string> placeholders = new HashSet<string>();

        foreach (Match match in PlaceholderPattern.Matches(text))
        {
            placeholders.Add(match.Groups[1].Value);
        }

        return placeholders;
    }

    private static string BuildKeysSource(List<string> keys, List<string> errors)
    {
        StringBuilder builder = new StringBuilder();
        Dictionary<string, string> keysByConstName = new Dictionary<string, string>();


        builder.AppendLine("public static class LocalizationKeys");
        builder.AppendLine("{");

        foreach (string key in keys)
        {
            string constName = ToPascalCase(key);

            if (keysByConstName.TryGetValue(constName, out string existingKey))
            {
                errors.Add($"keys '{existingKey}' and '{key}' both become '{constName}'.");
                continue;
            }

            keysByConstName[constName] = key;
            builder.AppendLine($"    public const string {constName} = \"{key}\";");
        }

        builder.AppendLine("}");

        return builder.ToString();
    }

    private static string ToPascalCase(string key)
    {
        StringBuilder builder = new StringBuilder();

        foreach (string word in key.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries))
        {
            builder.Append(char.ToUpperInvariant(word[0]));
            builder.Append(word.Substring(1).ToLowerInvariant());
        }

        return builder.ToString();
    }

    private static void WriteIfChanged(string path, string content)
    {
        string normalized = content.Replace("\r\n", "\n");

        if (File.Exists(path) && File.ReadAllText(path).Replace("\r\n", "\n") == normalized)
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, normalized, new UTF8Encoding(false));
    }

    private static void LogResult(List<string> errors, List<string> warnings)
    {
        foreach (string warning in warnings)
        {
            Debug.LogWarning($"{nameof(LocalizationExporter)}: {warning}");
        }

        foreach (string error in errors)
        {
            Debug.LogError($"{nameof(LocalizationExporter)}: {error}");
        }

        if (errors.Count > 0)
        {
            Debug.LogError($"{nameof(LocalizationExporter)}: export canceled. Fix {errors.Count} error(s) in the sheet. No files were written.");
        }
    }
}
