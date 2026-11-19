using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class LoadingBackgroundVideo : MonoBehaviour
{
    private const int FallbackWidth = 1920;
    private const int FallbackHeight = 1080;

    [SerializeField] private RawImage _rawImage;
    [SerializeField] private AspectRatioFitter _aspectRatioFitter;

    private VideoPlayer _videoPlayer;
    private RenderTexture _renderTexture;

    private void Awake()
    {
        _videoPlayer = GetComponent<VideoPlayer>();

        bool hasAllReferences = true;

        hasAllReferences &= UnityUtility.CheckReference(_rawImage, nameof(_rawImage), this);
        hasAllReferences &= UnityUtility.CheckReference(_aspectRatioFitter, nameof(_aspectRatioFitter), this);

        enabled = hasAllReferences;

        if (!hasAllReferences)
        {
            return;
        }

        _rawImage.enabled = false;
    }

    private void OnEnable()
    {
        _videoPlayer.prepareCompleted += OnPrepareCompleted;
        _videoPlayer.frameReady += OnFrameReady;
        _videoPlayer.errorReceived += OnErrorReceived;
    }

    private void OnDisable()
    {
        _videoPlayer.prepareCompleted -= OnPrepareCompleted;
        _videoPlayer.frameReady -= OnFrameReady;
        _videoPlayer.errorReceived -= OnErrorReceived;
    }

    private void Start()
    {
        if (!HasVideoSource())
        {
            DebugUtility.LogWarning($"{nameof(LoadingBackgroundVideo)}: no video source assigned. Showing poster only.");
            enabled = false;
            return;
        }

        _videoPlayer.Prepare();
    }

    private void OnDestroy()
    {
        ReleaseRenderTexture();
    }

    private bool HasVideoSource()
    {
        if (_videoPlayer.source == VideoSource.Url)
        {
            return !string.IsNullOrWhiteSpace(_videoPlayer.url);
        }

        return _videoPlayer.clip != null;
    }

    private void OnPrepareCompleted(VideoPlayer source)
    {
        int width = source.width > 0 ? (int)source.width : FallbackWidth;
        int height = source.height > 0 ? (int)source.height : FallbackHeight;

        ReleaseRenderTexture();
        _renderTexture = new RenderTexture(width, height, 0);

        source.renderMode = VideoRenderMode.RenderTexture;
        source.targetTexture = _renderTexture;

        _rawImage.texture = _renderTexture;
        _aspectRatioFitter.aspectRatio = (float)width / height;

        source.sendFrameReadyEvents = true;
        source.Play();
    }

    private void OnFrameReady(VideoPlayer source, long frameIndex)
    {
        source.sendFrameReadyEvents = false;
        _rawImage.enabled = true;
    }

    private void OnErrorReceived(VideoPlayer source, string message)
    {
        DebugUtility.LogWarning($"{nameof(LoadingBackgroundVideo)}: video playback failed. {message}");

        _rawImage.enabled = false;
        _rawImage.texture = null;
        source.sendFrameReadyEvents = false;
        source.Stop();

        ReleaseRenderTexture();
        enabled = false;
    }

    private void ReleaseRenderTexture()
    {
        if (_renderTexture == null)
        {
            return;
        }

        if (_videoPlayer != null && _videoPlayer.targetTexture == _renderTexture)
        {
            _videoPlayer.targetTexture = null;
        }

        _renderTexture.Release();
        Destroy(_renderTexture);
        _renderTexture = null;
    }
}
