using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class LocalizationManager
{
    private const string BootResourcesFolder = "Localization/Boot/";
    private const string MainAddressPrefix = "Localization/Main/";
    private const string LanguagePrefKey = "Localization.Language";

    public const Language FallbackLanguage = Language.Korean;

    private static readonly Dictionary<Language, Dictionary<string, string>> Tables = new Dictionary<Language, Dictionary<string, string>>();

    private static readonly HashSet<string> ReportedMissingKeys = new HashSet<string>();

    private static Dictionary<string, string> _currentTable;

    private static Dictionary<string, string> _fallbackTable;

    private static bool _isInitialized;

    private static bool _isMainLoaded;

    public static event Action TablesChanged;

    public static Language CurrentLanguage { get; private set; }

    private static bool IsFallbackLanguage => CurrentLanguage == FallbackLanguage;

    public static void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;

        foreach (Language language in GetLanguages())
        {
            Tables[language] = LoadBootTable(language);
        }

        _fallbackTable = Tables[FallbackLanguage];
        ApplyLanguage(LoadSavedLanguage());
    }

    public static async UniTask LoadMainTablesAsync(CancellationToken cancellationToken)
    {
        Initialize();

        if (_isMainLoaded)
        {
            return;
        }

        Dictionary<Language, Dictionary<string, string>> loadedTables = new Dictionary<Language, Dictionary<string, string>>();

        foreach (Language language in GetLanguages())
        {
            loadedTables[language] = await LoadMainTableAsync(language, cancellationToken);
        }

        foreach (KeyValuePair<Language, Dictionary<string, string>> loadedTable in loadedTables)
        {
            MergeInto(Tables[loadedTable.Key], loadedTable.Value);
        }

        _isMainLoaded = true;

        ReportedMissingKeys.Clear();

        TablesChanged?.Invoke();
    }

    public static void SetLanguage(Language language)
    {
        Initialize();

        if (language == CurrentLanguage)
        {
            return;
        }

        ApplyLanguage(language);

        PlayerPrefs.SetInt(LanguagePrefKey, (int)language);
        PlayerPrefs.Save();

        TablesChanged?.Invoke();
    }

    public static string Get(string key)
    {
        Initialize();

        if (string.IsNullOrEmpty(key))
        {
            return string.Empty;
        }

        if (_currentTable.TryGetValue(key, out string value))
        {
            return value;
        }

        if (!IsFallbackLanguage && _fallbackTable.TryGetValue(key, out string fallbackValue))
        {
            ReportMissingKey(key, $"no '{GetCode(CurrentLanguage)}' translation, using '{GetCode(FallbackLanguage)}'");
            return fallbackValue;
        }

        ReportMissingKey(key, _isMainLoaded ? "key not found" : "key not found in Boot tables (Main tables are not loaded yet)");
        return key;
    }

    public static string Get(string key, params object[] args)
    {
        string format = Get(key);

        if (args == null || args.Length == 0)
        {
            return format;
        }

        try
        {
            return string.Format(format, args);
        }
        catch (FormatException exception)
        {
            DebugUtility.LogWarning($"{nameof(LocalizationManager)}: invalid format for key '{key}'. {exception.Message}");
            return format;
        }
    }

    public static string GetCode(Language language)
    {
        switch (language)
        {
            case Language.Korean:
                return "ko";
            case Language.English:
                return "en";
            default:
                throw new ArgumentOutOfRangeException(nameof(language), language, null);
        }
    }

    public static string GetBootResourcesPath(Language language)
    {
        return BootResourcesFolder + GetCode(language);
    }

    public static string GetMainAddress(Language language)
    {
        return MainAddressPrefix + GetCode(language);
    }

    private static Language[] GetLanguages()
    {
        return (Language[])Enum.GetValues(typeof(Language));
    }

    private static Language LoadSavedLanguage()
    {
        if (PlayerPrefs.HasKey(LanguagePrefKey))
        {
            int savedLanguage = PlayerPrefs.GetInt(LanguagePrefKey);

            if (Enum.IsDefined(typeof(Language), savedLanguage))
            {
                return (Language)savedLanguage;
            }
        }

        return Application.systemLanguage == SystemLanguage.Korean ? Language.Korean : Language.English;
    }

    private static void ApplyLanguage(Language language)
    {
        CurrentLanguage = language;
        _currentTable = Tables[language];

        ReportedMissingKeys.Clear();
    }

    private static Dictionary<string, string> LoadBootTable(Language language)
    {
        string path = GetBootResourcesPath(language);
        TextAsset tableAsset = Resources.Load<TextAsset>(path);

        if (tableAsset == null)
        {
            DebugUtility.LogWarning($"{nameof(LocalizationManager)}: 'Resources/{path}.json' not found. Run Tools/Localization/Export From Google Sheet.");
            return new Dictionary<string, string>();
        }

        Dictionary<string, string> table = ParseTable(tableAsset.text);

        Resources.UnloadAsset(tableAsset);
        return table;
    }

    private static void MergeInto(Dictionary<string, string> target, Dictionary<string, string> source)
    {
        foreach (KeyValuePair<string, string> entry in source)
        {
            target[entry.Key] = entry.Value;
        }
    }

    private static async UniTask<Dictionary<string, string>> LoadMainTableAsync(Language language, CancellationToken cancellationToken)
    {
        AsyncOperationHandle<TextAsset> handle = Addressables.LoadAssetAsync<TextAsset>(GetMainAddress(language));

        try
        {
            TextAsset tableAsset = await handle.ToUniTask(cancellationToken: cancellationToken);
            return ParseTable(tableAsset.text);
        }
        finally
        {
            Addressables.Release(handle);
        }
    }

    private static Dictionary<string, string> ParseTable(string json)
    {
        Dictionary<string, string> table = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

        if (table == null)
        {
            return new Dictionary<string, string>();
        }

        return table;
    }

    private static void ReportMissingKey(string key, string reason)
    {
        if (ReportedMissingKeys.Add(key))
        {
            DebugUtility.LogWarning($"{nameof(LocalizationManager)}: '{key}' {reason}.");
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        Tables.Clear();
        ReportedMissingKeys.Clear();
        _currentTable = null;
        _fallbackTable = null;
        TablesChanged = null;
        CurrentLanguage = default;
        _isInitialized = false;
        _isMainLoaded = false;
    }
}
