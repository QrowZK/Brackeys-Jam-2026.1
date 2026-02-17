using System.Collections.Generic;
using UnityEngine;

public sealed class WallVisibilityController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RoomManager roomManager;
    [SerializeField] private Camera insideCamera;

    [Header("Mode")]
    [SerializeField] private ViewMode mode = ViewMode.Inside;

    [Header("Continuous Fade (Inside)")]
    [Tooltip("Dot at or below this is fully hidden.")]
    [SerializeField] private float hideDot = -0.10f;

    [Tooltip("Dot at or above this is fully visible.")]
    [SerializeField] private float showDot = 0.35f;

    [Tooltip("If true, apply SmoothStep to the fade curve.")]
    [SerializeField] private bool smoothFadeCurve = true;

    [Header("Stability")]
    [Tooltip("How quickly the dot filter reacts. Higher reacts faster. 10 to 25 is typical.")]
    [SerializeField] private float dotFilterSharpness = 18f;

    [Tooltip("Ignore tiny dot changes to prevent jitter. 0.01 to 0.03 is typical.")]
    [SerializeField] private float dotDeadzone = 0.015f;

    [Tooltip("Do not send fade updates unless the target changes by at least this amount.")]
    [SerializeField] private float minTargetChange = 0.02f;

    [Header("Inspect Visibility")]
    [Tooltip("In Inspect mode, show exterior and hide interior.")]
    [SerializeField] private bool inspectShowsExteriorOnly = true;

    private struct WallState
    {
        public WallVisual visual;
        public float filteredDot;
        public float lastSentFade;
        public bool initialized;
    }

    private readonly Dictionary<Wall, WallState> _states = new Dictionary<Wall, WallState>(32);

    private void OnEnable()
    {
        CameraModeController.OnInspectEntered += OnInspectEntered;
        CameraModeController.OnInspectExited += OnInspectExited;

        if (roomManager != null)
            roomManager.OnRoomLoaded += OnRoomLoaded;
    }

    private void OnDisable()
    {
        CameraModeController.OnInspectEntered -= OnInspectEntered;
        CameraModeController.OnInspectExited -= OnInspectExited;

        if (roomManager != null)
            roomManager.OnRoomLoaded -= OnRoomLoaded;
    }

    private void Update()
    {
        var room = roomManager != null ? roomManager.CurrentRoom : null;
        if (room == null || room.AllWalls == null) return;

        if (mode == ViewMode.Inspect)
            ApplyInspect(room);
        else
            ApplyInside(room);
    }

    private void OnInspectEntered()
    {
        mode = ViewMode.Inspect;
        ApplyNow();
    }

    private void OnInspectExited()
    {
        mode = ViewMode.Inside;
        ApplyNow();
    }

    private void OnRoomLoaded(Room _)
    {
        _states.Clear();
        ApplyNow();
    }

    private void ApplyNow()
    {
        var room = roomManager != null ? roomManager.CurrentRoom : null;
        if (room == null || room.AllWalls == null) return;

        if (mode == ViewMode.Inspect) ApplyInspect(room);
        else ApplyInside(room);
    }

    private void ApplyInspect(Room room)
    {
        for (int i = 0; i < room.AllWalls.Count; i++)
        {
            var wall = room.AllWalls[i];
            if (!wall) continue;

            bool visible = inspectShowsExteriorOnly ? !wall.isInterior : true;
            SetWallFade(wall, visible ? 1f : 0f, bypassThresholds: true);
        }
    }

    private void ApplyInside(Room room)
    {
        if (insideCamera == null) return;

        Vector3 camPos = insideCamera.transform.position;

        // Exponential smoothing factor for dot
        float k = 1f - Mathf.Exp(-Mathf.Max(0.01f, dotFilterSharpness) * Time.deltaTime);

        for (int i = 0; i < room.AllWalls.Count; i++)
        {
            var wall = room.AllWalls[i];
            if (!wall) continue;

            // Keep exterior hidden in inside mode
            if (!wall.isInterior)
            {
                SetWallFade(wall, 0f, bypassThresholds: true);
                continue;
            }

            Transform t = wall.transform;
            Vector3 wallNormal = -t.forward;

            Vector3 toCam = camPos - t.position;
            float len = toCam.magnitude;
            toCam = (len > 0.0001f) ? (toCam / len) : Vector3.forward;

            float rawDot = Mathf.Clamp(Vector3.Dot(wallNormal, toCam), -1f, 1f);

            // Get or init state
            if (!_states.TryGetValue(wall, out var st) || st.visual == null)
            {
                st = new WallState
                {
                    visual = wall.GetComponentInChildren<WallVisual>(true),
                    filteredDot = rawDot,
                    lastSentFade = -999f,
                    initialized = true
                };
            }

            if (st.visual == null)
            {
                _states[wall] = st;
                continue;
            }

            // Deadzone on dot to reduce micro jitter
            float deltaDot = rawDot - st.filteredDot;
            if (Mathf.Abs(deltaDot) < dotDeadzone)
                rawDot = st.filteredDot;

            // Filtered dot
            st.filteredDot = Mathf.Lerp(st.filteredDot, rawDot, k);

            // Dot -> fade
            float fade01 = Mathf.InverseLerp(hideDot, showDot, st.filteredDot);
            if (smoothFadeCurve) fade01 = Mathf.SmoothStep(0f, 1f, fade01);

            // Only send if meaningfully changed
            if (Mathf.Abs(fade01 - st.lastSentFade) >= minTargetChange || st.lastSentFade < -10f)
            {
                st.visual.SetFadeTarget(fade01);
                st.lastSentFade = fade01;
            }

            _states[wall] = st;
        }
    }

    private void SetWallFade(Wall wall, float fade01, bool bypassThresholds)
    {
        if (!wall) return;

        if (!_states.TryGetValue(wall, out var st) || st.visual == null)
        {
            st = new WallState
            {
                visual = wall.GetComponentInChildren<WallVisual>(true),
                filteredDot = 0f,
                lastSentFade = -999f,
                initialized = true
            };
        }

        if (st.visual != null)
        {
            if (bypassThresholds || Mathf.Abs(fade01 - st.lastSentFade) >= minTargetChange || st.lastSentFade < -10f)
            {
                st.visual.SetFadeTarget(fade01);
                st.lastSentFade = fade01;
            }
        }

        _states[wall] = st;
    }
}
