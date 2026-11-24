using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class LocalizedText : MonoBehaviour
{
    [SerializeField] private string _key;

    private TMP_Text _text;

    private object[] _args;

    private void Awake()
    {
        _text = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        LocalizationManager.TablesChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        LocalizationManager.TablesChanged -= Refresh;
    }

    public void SetKey(string key)
    {
        SetKey(key, null);
    }

    public void SetKey(string key, params object[] args)
    {
        _key = key;
        _args = args;
        Refresh();
    }

    private void Refresh()
    {
        if (_text == null || string.IsNullOrEmpty(_key))
        {
            return;
        }

        _text.text = LocalizationManager.Get(_key, _args);
    }
}
