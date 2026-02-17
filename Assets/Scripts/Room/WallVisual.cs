using UnityEngine;

[DisallowMultipleComponent]
public sealed class WallVisual : MonoBehaviour
{
    [Header("Renderer Sources")]
    [SerializeField] private Renderer[] renderers;

    [Header("Shader Fade")]
    [SerializeField] private string fadePropertyName = "_Fade";

    [Header("Smoothing")]
    [Tooltip("Higher = faster response when fading in.")]
    [SerializeField] private float fadeInSharpness = 16f;

    [Tooltip("Higher = faster response when fading out.")]
    [SerializeField] private float fadeOutSharpness = 14f;

    [Tooltip("Do not re-apply property block unless fade changed by at least this much.")]
    [SerializeField] private float applyEpsilon = 0.005f;

    [Header("Disable Renderers")]
    [SerializeField] private bool disableRenderersAtZero = true;

    [Tooltip("Wait this long at zero fade before disabling renderers. Prevents flicker near 0.")]
    [SerializeField] private float disableDelay = 0.10f;

    private int _fadeId;
    private MaterialPropertyBlock _mpb;

    private float _current = 1f;
    private float _target = 1f;
    private float _lastApplied = -999f;

    private float _zeroTimer;

    private void Reset()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
    }

    private void Awake()
    {
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>(true);

        _fadeId = Shader.PropertyToID(fadePropertyName);
        _mpb = new MaterialPropertyBlock();

        ApplyImmediate(_current);
    }

    private void Update()
    {
        float sharp = (_target > _current) ? fadeInSharpness : fadeOutSharpness;
        sharp = Mathf.Max(0.01f, sharp);

        float k = 1f - Mathf.Exp(-sharp * Time.deltaTime);
        _current = Mathf.Lerp(_current, _target, k);

        // Snap extremely close values
        if (Mathf.Abs(_current - _target) < 0.0005f)
            _current = _target;

        // Disable delay handling
        if (disableRenderersAtZero)
        {
            if (_current <= 0.001f && _target <= 0.001f)
                _zeroTimer += Time.deltaTime;
            else
                _zeroTimer = 0f;
        }

        // Apply only when meaningful change happens
        if (Mathf.Abs(_current - _lastApplied) >= applyEpsilon)
            Apply(_current);

        // Disable after staying at zero
        if (disableRenderersAtZero && _zeroTimer >= disableDelay)
            SetRenderersEnabled(false);
    }

    public void SetFadeTarget(float fade01)
    {
        _target = Mathf.Clamp01(fade01);

        // If we are fading in, ensure renderers are enabled immediately
        if (disableRenderersAtZero && _target > 0.001f)
        {
            _zeroTimer = 0f;
            SetRenderersEnabled(true);
        }
    }

    public void SetVisible(bool visible)
    {
        SetFadeTarget(visible ? 1f : 0f);
    }

    public void SetFadeInstant(float fade01)
    {
        _target = _current = Mathf.Clamp01(fade01);
        _zeroTimer = 0f;

        if (disableRenderersAtZero)
            SetRenderersEnabled(_current > 0.001f);

        ApplyImmediate(_current);
    }

    private void ApplyImmediate(float fade01)
    {
        _lastApplied = fade01;
        Apply(fade01);
    }

    private void Apply(float fade01)
    {
        // If we are very close to zero and disabling is enabled, keep them enabled until delay expires
        if (disableRenderersAtZero && fade01 <= 0.001f && _target <= 0.001f && _zeroTimer < disableDelay)
            SetRenderersEnabled(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (!r) continue;

            r.GetPropertyBlock(_mpb);
            _mpb.SetFloat(_fadeId, fade01);
            r.SetPropertyBlock(_mpb);
        }
    }

    private void SetRenderersEnabled(bool enabled)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r) r.enabled = enabled;
        }
    }
}
