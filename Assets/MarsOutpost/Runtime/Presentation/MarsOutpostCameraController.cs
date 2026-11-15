using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace MarsOutpost.Presentation
{
    /// <summary>
    /// Fixed-angle 2.5D camera controller for Mars Outpost.
    /// Supports:
    /// - Locked 2.5D perspective angle (DefaultPitch = 43°, DefaultYaw = 0°, orbit rotation fully disabled).
    /// - Smooth Planar Pan (Right Mouse Drag, Middle Mouse Drag, Alt + Left Drag, or WASD/Arrow keys)
    ///   tightly clamped to the safe outpost basin bounds.
    /// - Smooth Zoom (Mouse Scroll Wheel or +/- keys) clamped between 8.0m and 18.5m with cross-platform
    ///   normalized scroll sensitivity.
    /// - Reset View (R key or ResetCameraView()) smoothly returning to DefaultPivot and DefaultDistance.
    /// - UI event blocking and drag threshold detection to prevent interaction interference.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MarsOutpostCameraController : MonoBehaviour
    {
        // -------------------------------------------------------------
        // Locked 2.5D Overview Camera Contract
        // -------------------------------------------------------------
        public static readonly Vector3 DefaultPivot = new Vector3(0.4f, 0.2f, -0.3f);
        public const float DefaultDistance = 15.2f;
        public const float DefaultPitch = 43.0f;
        public const float DefaultYaw = 0.0f;

        // Bounded Pan & Zoom Limits (covers full 16x16 Operations parcel grid x[-32,32], z[-24,40])
        public const float MinDistance = 8.0f;
        public const float MaxDistance = 24.0f;
        public const float MinPivotX = -32.0f;
        public const float MaxPivotX = 32.0f;
        public const float MinPivotZ = -24.0f;
        public const float MaxPivotZ = 40.0f;

        public const float MinPitch = 43.0f;
        public const float MaxPitch = 43.0f;

        // Tuning parameters
        [Header("Sensitivity Settings")]
        [SerializeField] private float mouseOrbitSensitivity = 0.22f; // Preserved for asset serialization compatibility
        [SerializeField] private float mousePanSensitivity = 0.015f;
        [SerializeField] private float scrollSensitivity = 0.005f;
        [SerializeField] private float keyOrbitSpeed = 75.0f; // Preserved for asset serialization compatibility
        [SerializeField] private float keyPanSpeed = 9.0f;
        [SerializeField] private float keyZoomSpeed = 12.0f;
        [SerializeField] private float smoothFactor = 12.0f;

        // Current smoothed values
        private float currentYaw = DefaultYaw;
        private float currentPitch = DefaultPitch;
        private float currentDistance = DefaultDistance;
        private Vector3 currentPivot = DefaultPivot;

        // Target values
        private float targetYaw = DefaultYaw;
        private float targetPitch = DefaultPitch;
        private float targetDistance = DefaultDistance;
        private Vector3 targetPivot = DefaultPivot;

        // Pointer & drag tracking
        private bool isPointerBlockedByUi;
        private bool isPanDragging;
        private Vector2 pointerDownPos;
        private bool dragExceededThreshold;
        private float dragGraceTimer;

        // -------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------
        public float CurrentYaw => currentYaw;
        public float CurrentPitch => currentPitch;
        public float CurrentDistance => currentDistance;
        public Vector3 CurrentPivot => currentPivot;

        public bool IsDraggingCamera => isPanDragging;
        public bool HasDraggedRecently => IsDraggingCamera || dragGraceTimer > 0f;
        public static bool IsMenuBlockingCameraInput { get; set; }

        public static float ComputeAspectAdjustedDefaultDistance(float aspect)
        {
            const float referenceAspect = 16f / 9f;
            if (aspect <= 0.01f || aspect >= referenceAspect - 0.01f)
                return DefaultDistance;

            float scaled = DefaultDistance * (referenceAspect / Mathf.Max(1.2f, aspect));
            return Mathf.Clamp(scaled, DefaultDistance, MaxDistance);
        }

        private float GetAspectDefaultDistance()
        {
            var cam = GetComponent<Camera>();
            float aspect = cam != null ? cam.aspect : (Screen.height > 0 ? (float)Screen.width / Screen.height : 16f / 9f);
            return ComputeAspectAdjustedDefaultDistance(aspect);
        }

        public void ResetCameraView()
        {
            targetYaw = DefaultYaw;
            targetPitch = DefaultPitch;
            targetDistance = GetAspectDefaultDistance();
            targetPivot = DefaultPivot;
        }

        public void FocusWorldPoint(Vector3 worldPoint, bool immediate = false)
        {
            SetCameraPose(
                new Vector3(worldPoint.x, DefaultPivot.y, worldPoint.z),
                targetDistance,
                DefaultPitch,
                DefaultYaw,
                immediate);
        }

        public void SetCameraPose(Vector3 pivot, float distance, float pitch, float yaw, bool immediate = false)
        {
            targetPivot = new Vector3(
                Mathf.Clamp(pivot.x, MinPivotX, MaxPivotX),
                pivot.y,
                Mathf.Clamp(pivot.z, MinPivotZ, MaxPivotZ));
            targetDistance = Mathf.Clamp(distance, MinDistance, MaxDistance);
            targetPitch = DefaultPitch;
            targetYaw = DefaultYaw;

            if (immediate)
            {
                currentPivot = targetPivot;
                currentDistance = targetDistance;
                currentPitch = DefaultPitch;
                currentYaw = DefaultYaw;
                ApplyTransform();
            }
        }

        private void Awake()
        {
            float initialDist = GetAspectDefaultDistance();
            currentPivot = DefaultPivot;
            targetPivot = DefaultPivot;
            currentDistance = initialDist;
            targetDistance = initialDist;
            currentPitch = DefaultPitch;
            targetPitch = DefaultPitch;
            currentYaw = DefaultYaw;
            targetYaw = DefaultYaw;
            ApplyTransform();
        }

        private void Update()
        {
            HandleInput();
            SmoothDampValues();
        }

        private void LateUpdate()
        {
            ApplyTransform();
        }

        // -------------------------------------------------------------
        // Input Handling (New Input System)
        // -------------------------------------------------------------
        private void HandleInput()
        {
            if (dragGraceTimer > 0f)
            {
                dragGraceTimer -= Time.unscaledDeltaTime;
                if (dragGraceTimer < 0f) dragGraceTimer = 0f;
            }

            if (IsMenuBlockingCameraInput)
            {
                isPanDragging = false;
                return;
            }

            var mouse = Mouse.current;
            var kb = Keyboard.current;

            bool isPointerOverUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

            // 1. Mouse Drag Interactions (Planar Pan Only - Orbit Rotation Disabled)
            if (mouse != null)
            {
                bool rightDown = mouse.rightButton.wasPressedThisFrame;
                bool middleDown = mouse.middleButton.wasPressedThisFrame;
                bool leftDown = mouse.leftButton.wasPressedThisFrame;
                bool altPressed = kb != null && kb.altKey.isPressed;

                if (rightDown || middleDown || (leftDown && altPressed))
                {
                    isPointerBlockedByUi = isPointerOverUi;
                    pointerDownPos = mouse.position.ReadValue();
                    dragExceededThreshold = false;
                }

                if (!isPointerBlockedByUi)
                {
                    Vector2 mousePos = mouse.position.ReadValue();
                    if (!dragExceededThreshold && Vector2.Distance(mousePos, pointerDownPos) > 5.0f)
                    {
                        dragExceededThreshold = true;
                    }

                    // Planar Pan: Right Mouse Drag, Middle Mouse Drag, or Alt + Left Drag
                    if (mouse.rightButton.isPressed || mouse.middleButton.isPressed || (mouse.leftButton.isPressed && altPressed))
                    {
                        isPanDragging = true;
                        Vector2 delta = mouse.delta.ReadValue();
                        Vector3 right = Quaternion.Euler(0, DefaultYaw, 0) * Vector3.right;
                        Vector3 forward = Quaternion.Euler(0, DefaultYaw, 0) * Vector3.forward;

                        // Scale pan sensitivity with zoom distance for natural feel
                        float distScale = currentDistance / DefaultDistance;
                        Vector3 panDelta = (-right * delta.x - forward * delta.y) * (mousePanSensitivity * distScale);
                        targetPivot += panDelta;
                        targetPivot.x = Mathf.Clamp(targetPivot.x, MinPivotX, MaxPivotX);
                        targetPivot.z = Mathf.Clamp(targetPivot.z, MinPivotZ, MaxPivotZ);
                    }
                    else
                    {
                        isPanDragging = false;
                    }

                    // Mouse Zoom (only if not over UI)
                    if (!isPointerOverUi)
                    {
                        float scroll = mouse.scroll.ReadValue().y;
                        if (Mathf.Abs(scroll) > 0.01f)
                        {
                            float scrollAmount;
                            if (Mathf.Abs(scroll) >= 50f)
                            {
                                // High-resolution or Windows-style 120-unit scroll tick
                                scrollAmount = (scroll / 120f) * 0.95f;
                            }
                            else
                            {
                                // Linux / macOS low-unit tick (e.g. 1 to 12)
                                scrollAmount = Mathf.Sign(scroll) * Mathf.Clamp(Mathf.Abs(scroll), 0.5f, 12f) * 0.18f;
                            }
                            targetDistance -= scrollAmount;
                            targetDistance = Mathf.Clamp(targetDistance, MinDistance, MaxDistance);
                        }
                    }
                }
                else
                {
                    isPanDragging = false;
                }

                // Check button releases to trigger drag grace timer
                if (mouse.rightButton.wasReleasedThisFrame || mouse.middleButton.wasReleasedThisFrame || mouse.leftButton.wasReleasedThisFrame)
                {
                    if (dragExceededThreshold)
                    {
                        dragGraceTimer = 0.15f;
                    }
                    isPointerBlockedByUi = false;
                    isPanDragging = false;
                }
            }

            // 2. Keyboard Navigation
            if (kb != null)
            {
                // Reset view with 'R'
                if (kb.rKey.wasPressedThisFrame)
                {
                    ResetCameraView();
                }

                // Orbit rotation (Q/E) is intentionally disabled to maintain locked 2.5D angle

                // Pan with WASD / Arrows (only when Alt not pressed)
                bool altPressed = kb.altKey.isPressed;
                if (!altPressed)
                {
                    float panX = 0f;
                    float panZ = 0f;
                    if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) panX -= 1f;
                    if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) panX += 1f;
                    if (kb.sKey.isPressed || kb.downArrowKey.isPressed) panZ -= 1f;
                    if (kb.wKey.isPressed || kb.upArrowKey.isPressed) panZ += 1f;

                    if (Mathf.Abs(panX) > 0.01f || Mathf.Abs(panZ) > 0.01f)
                    {
                        Vector3 right = Quaternion.Euler(0, DefaultYaw, 0) * Vector3.right;
                        Vector3 forward = Quaternion.Euler(0, DefaultYaw, 0) * Vector3.forward;
                        Vector3 moveDir = (right * panX + forward * panZ).normalized;

                        float distScale = currentDistance / DefaultDistance;
                        targetPivot += moveDir * (keyPanSpeed * distScale * Time.unscaledDeltaTime);
                        targetPivot.x = Mathf.Clamp(targetPivot.x, MinPivotX, MaxPivotX);
                        targetPivot.z = Mathf.Clamp(targetPivot.z, MinPivotZ, MaxPivotZ);
                    }
                }

                // Zoom with +/- keys
                float zoomDelta = 0f;
                if (kb.equalsKey.isPressed || kb.numpadPlusKey.isPressed) zoomDelta -= 1f;
                if (kb.minusKey.isPressed || kb.numpadMinusKey.isPressed) zoomDelta += 1f;
                if (Mathf.Abs(zoomDelta) > 0.01f)
                {
                    targetDistance += zoomDelta * keyZoomSpeed * Time.unscaledDeltaTime;
                    targetDistance = Mathf.Clamp(targetDistance, MinDistance, MaxDistance);
                }
            }
        }

        // -------------------------------------------------------------
        // Smoothing & Positioning
        // -------------------------------------------------------------
        private void SmoothDampValues()
        {
            float dt = Time.unscaledDeltaTime;
            float lerpT = 1f - Mathf.Exp(-smoothFactor * dt);

            currentYaw = DefaultYaw;
            currentPitch = DefaultPitch;
            currentDistance = Mathf.Lerp(currentDistance, targetDistance, lerpT);
            currentPivot = Vector3.Lerp(currentPivot, targetPivot, lerpT);
        }

        private void ApplyTransform()
        {
            Quaternion rot = Quaternion.Euler(currentPitch, currentYaw, 0f);
            transform.rotation = rot;
            transform.position = currentPivot - rot * (Vector3.forward * currentDistance);
        }
    }
}
