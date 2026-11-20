using UnityEngine;

public class LocalizationExportSettings : ScriptableObject
{
    [Header("Google Sheet")]
    [Tooltip("시트 주소의 /d/ 와 /edit 사이 값")]
    [SerializeField] private string _sheetId;

    [Tooltip("시트 주소 끝의 gid= 뒤 숫자")]
    [SerializeField] private string _sheetGid = "0";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_sheetId);

    public string CsvUrl => $"https://docs.google.com/spreadsheets/d/{_sheetId.Trim()}/export?format=csv&gid={_sheetGid.Trim()}";
}
