using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class UIScrollLoop : MonoBehaviour
{
    [SerializeField] private Vector2 _speed = new Vector2(24f, 0f);
    [SerializeField] private Vector2 _period = new Vector2(16f, 16f);

    private RectTransform _rectTransform;
    private Vector2 _origin;

    private Tween _xTween;
    private Tween _yTween;

    private void Awake()
    {
        _rectTransform = (RectTransform)transform; //새로 알게된 내용(기존 참조 재사용->성능상 좋음)
        _origin = _rectTransform.anchoredPosition;
    }

    private void OnEnable()
    {
        _rectTransform.anchoredPosition = _origin;

        if (CanScroll(_speed.x, _period.x))
        {
            _xTween = _rectTransform.DOAnchorPosX(_origin.x + GetDistance(_speed.x, _period.x), GetDuration(_speed.x, _period.x));
            SetupLoop(_xTween);
        }

        if (CanScroll(_speed.y, _period.y))
        {
            _yTween = _rectTransform.DOAnchorPosY(_origin.y + GetDistance(_speed.y, _period.y), GetDuration(_speed.y, _period.y));
            SetupLoop(_yTween);
        }
    }

    private void OnDisable()
    {
        if (_xTween != null)
        {
            _xTween.Kill();
            _xTween = null;
        }

        if (_yTween != null)
        {
            _yTween.Kill();
            _yTween = null;
        }

        _rectTransform.anchoredPosition = _origin;
    }

    private void SetupLoop(Tween tween)
    {
        tween.SetEase(Ease.Linear).SetLoops(-1, LoopType.Restart).SetUpdate(true).SetLink(gameObject);
    }

    private static bool CanScroll(float speed, float period)
    {
        return period > 0f && !Mathf.Approximately(speed, 0f);
    }

    private static float GetDistance(float speed, float period)
    {
        return Mathf.Sign(speed) * period;
    }

    private static float GetDuration(float speed, float period)
    {
        return period / Mathf.Abs(speed);
    }
}
