using UnityEngine;
using UnityEngine.UI;

public class StatusIndicator : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private UISpin _spin;

    [SerializeField] private Sprite _loadingSprite;
    [SerializeField] private Sprite _completedSprite;
    [SerializeField] private Sprite _failedSprite;

    [SerializeField] private Color _loadingColor = new Color32(0x60, 0xA5, 0xFA, 0xFF);
    [SerializeField] private Color _completedColor = new Color32(0x4A, 0xDE, 0x80, 0xFF);
    [SerializeField] private Color _failedColor = new Color32(0xF8, 0x71, 0x71, 0xFF);

    public bool HasAllReferences()
    {
        bool hasAllReferences = true;

        hasAllReferences &= UnityUtility.CheckReference(_icon, nameof(_icon), this);
        hasAllReferences &= UnityUtility.CheckReference(_spin, nameof(_spin), this);
        hasAllReferences &= UnityUtility.CheckReference(_loadingSprite, nameof(_loadingSprite), this);
        hasAllReferences &= UnityUtility.CheckReference(_completedSprite, nameof(_completedSprite), this);
        hasAllReferences &= UnityUtility.CheckReference(_failedSprite, nameof(_failedSprite), this);

        return hasAllReferences;
    }

    public void ShowLoading()
    {
        Apply(_loadingSprite, _loadingColor, true);
    }

    public void ShowCompleted()
    {
        Apply(_completedSprite, _completedColor, false);
    }

    public void ShowFailed()
    {
        Apply(_failedSprite, _failedColor, false);
    }

    private void Apply(Sprite sprite, Color color, bool isSpinning)
    {
        _icon.sprite = sprite;
        _icon.color = color;
        _spin.enabled = isSpinning;
    }
}
