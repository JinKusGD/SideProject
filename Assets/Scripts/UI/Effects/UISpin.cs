using DG.Tweening;
using UnityEngine;

public class UISpin : MonoBehaviour
{
    [SerializeField] private float _degreesPerSecond = -360f;

    private Quaternion _initialRotation;
    private Tween _spinTween;

    private void Awake()
    {
        _initialRotation = transform.localRotation;
    }

    private void OnEnable()
    {
        transform.localRotation = _initialRotation;

        if (Mathf.Approximately(_degreesPerSecond, 0f))
        {
            return;
        }

        float duration = 360f / Mathf.Abs(_degreesPerSecond);
        Vector3 rotation = new Vector3(0f, 0f, Mathf.Sign(_degreesPerSecond) * 360f);

        _spinTween = transform.DOLocalRotate(rotation, duration, RotateMode.FastBeyond360).SetRelative(true).SetEase(Ease.Linear).SetLoops(-1, LoopType.Restart).SetUpdate(true).SetLink(gameObject);
    }

    private void OnDisable()
    {
        if (_spinTween != null)
        {
            _spinTween.Kill();
            _spinTween = null;
        }

        transform.localRotation = _initialRotation;
    }
}
