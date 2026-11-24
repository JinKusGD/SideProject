using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class UIPopIn : MonoBehaviour
{
    private const float DurationSeconds = 0.25f;
    private const float StartScale = 0.9f;

    private CanvasGroup _canvasGroup;
    private Sequence _sequence;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        _sequence?.Kill();

        _canvasGroup.alpha = 0f;
        transform.localScale = Vector3.one * StartScale;

        _sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
        _sequence.Join(transform.DOScale(1f, DurationSeconds).SetEase(Ease.OutBack));
        _sequence.Join(_canvasGroup.DOFade(1f, DurationSeconds).SetEase(Ease.OutCubic));
    }

    private void OnDisable()
    {
        if (_sequence != null)
        {
            _sequence.Kill(true);
            _sequence = null;
        }
    }
}
