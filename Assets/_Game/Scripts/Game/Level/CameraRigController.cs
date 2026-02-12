using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class CameraRigController : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float zoomSpeed = 6f;
    [SerializeField] private float minZoom = 4f;
    [SerializeField] private float maxZoom = 16f;
    [SerializeField] private Vector2 boundsMin = new Vector2(-20f, -12f);
    [SerializeField] private Vector2 boundsMax = new Vector2(20f, 12f);
    [SerializeField] private bool ignoreInputOverUi = true;
    [SerializeField, Min(0f)] private float panSmoothDuration = 0.12f;
    [SerializeField, Min(0f)] private float zoomSmoothDuration = 0.12f;
    [SerializeField] private Ease panSmoothEase = Ease.OutCubic;
    [SerializeField] private Ease zoomSmoothEase = Ease.OutCubic;

    private Vector3 lastPointerWorldPosition;
    private bool isPanning;
    private bool panBlockedUntilPointerRelease;
    private Tweener panTween;
    private Tweener zoomTween;
    private Vector3 desiredCameraPosition;
    private float desiredOrthographicSize;

    public Camera TargetCamera => targetCamera;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        CacheDesiredCameraState();
    }

    private void OnEnable()
    {
        CacheDesiredCameraState();
    }

    private void OnDisable()
    {
        StopCameraTweens();
    }

    private void OnDestroy()
    {
        StopCameraTweens();
    }

    private void LateUpdate()
    {
        if (targetCamera == null || !targetCamera.orthographic)
        {
            return;
        }

        HandleZoomInput();
        HandlePanInput();
        ClampCameraToBounds();
    }

    public void SetBounds(Vector2 min, Vector2 max)
    {
        boundsMin = min;
        boundsMax = max;
        ClampCameraToBounds();
    }

    public void CenterOnWorldPosition(Vector3 worldPosition)
    {
        MoveCameraTo(worldPosition, panSmoothDuration);
    }

    private void HandlePanInput()
    {
        // Do not pan while pinching.
        if (GetActiveTouchCount() > 1)
        {
            isPanning = false;
            panBlockedUntilPointerRelease = false;
            return;
        }

        bool pointerDown;
        bool pointerHeld;
        Vector2 pointerPosition;
        TryGetPointerState(out pointerDown, out pointerHeld, out pointerPosition);

        if (pointerDown)
        {
            if (ignoreInputOverUi && IsPointerOverUi())
            {
                isPanning = false;
                panBlockedUntilPointerRelease = true;
                return;
            }

            panBlockedUntilPointerRelease = false;
            StopPanTween();
            desiredCameraPosition = targetCamera.transform.position;
            isPanning = true;
            lastPointerWorldPosition = targetCamera.ScreenToWorldPoint(pointerPosition);
            return;
        }

        if (!pointerHeld)
        {
            isPanning = false;
            panBlockedUntilPointerRelease = false;
            return;
        }

        if (ignoreInputOverUi && panBlockedUntilPointerRelease)
        {
            isPanning = false;
            panBlockedUntilPointerRelease = true;
            return;
        }

        if (!isPanning)
        {
            lastPointerWorldPosition = targetCamera.ScreenToWorldPoint(pointerPosition);
            isPanning = true;
            return;
        }

        Vector3 currentPointerWorldPosition = targetCamera.ScreenToWorldPoint(pointerPosition);
        Vector3 delta = lastPointerWorldPosition - currentPointerWorldPosition;
        delta.z = 0f;
        MoveCameraTo(desiredCameraPosition + delta, panSmoothDuration);
        lastPointerWorldPosition = targetCamera.ScreenToWorldPoint(pointerPosition);
    }

    private void HandleZoomInput()
    {
        if (ignoreInputOverUi && IsPointerOverUi())
        {
            return;
        }

        float zoomDelta = 0f;

        if (TryGetPinchDelta(out float pinchDelta))
        {
            zoomDelta = pinchDelta * 0.01f;
        }
        else
        {
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                zoomDelta = mouse.scroll.ReadValue().y * 0.02f;
            }
        }

        if (Mathf.Approximately(zoomDelta, 0f))
        {
            return;
        }

        float targetZoom = desiredOrthographicSize - (zoomDelta * zoomSpeed * Time.deltaTime * 60f);
        MoveZoomTo(targetZoom, zoomSmoothDuration);
    }

    private void ClampCameraToBounds()
    {
        Vector3 position = GetClampedCameraPosition(targetCamera.transform.position, targetCamera.orthographicSize);
        targetCamera.transform.position = position;
        desiredCameraPosition = GetClampedCameraPosition(desiredCameraPosition, desiredOrthographicSize);
    }

    private Vector3 GetClampedCameraPosition(Vector3 position, float zoomValue)
    {
        if (targetCamera == null)
        {
            return position;
        }

        float verticalExtent = zoomValue;
        float horizontalExtent = verticalExtent * targetCamera.aspect;

        float minX = boundsMin.x + horizontalExtent;
        float maxX = boundsMax.x - horizontalExtent;
        float minY = boundsMin.y + verticalExtent;
        float maxY = boundsMax.y - verticalExtent;

        position.x = ClampAxis(position.x, minX, maxX);
        position.y = ClampAxis(position.y, minY, maxY);
        position.z = targetCamera.transform.position.z;
        return position;
    }

    private static float ClampAxis(float value, float min, float max)
    {
        if (min > max)
        {
            return (min + max) * 0.5f;
        }

        return Mathf.Clamp(value, min, max);
    }

    private static void TryGetPointerState(out bool pointerDown, out bool pointerHeld, out Vector2 pointerPosition)
    {
        pointerDown = false;
        pointerHeld = false;
        pointerPosition = Vector2.zero;

        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen != null)
        {
            var primaryTouch = touchscreen.primaryTouch;
            if (primaryTouch != null && (primaryTouch.press.isPressed || primaryTouch.press.wasPressedThisFrame))
            {
                pointerDown = primaryTouch.press.wasPressedThisFrame;
                pointerHeld = primaryTouch.press.isPressed;
                pointerPosition = primaryTouch.position.ReadValue();
                return;
            }
        }

        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            pointerDown = mouse.leftButton.wasPressedThisFrame;
            pointerHeld = mouse.leftButton.isPressed;
            if (pointerDown || pointerHeld)
            {
                pointerPosition = mouse.position.ReadValue();
            }
        }
    }

    private static int GetActiveTouchCount()
    {
        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen == null)
        {
            return 0;
        }

        int activeTouchCount = 0;
        for (int i = 0; i < touchscreen.touches.Count; i++)
        {
            if (touchscreen.touches[i].press.isPressed)
            {
                activeTouchCount++;
            }
        }

        return activeTouchCount;
    }

    private static bool TryGetPinchDelta(out float pinchDelta)
    {
        pinchDelta = 0f;

        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen == null)
        {
            return false;
        }

        var activeTouches = new Vector2[4];
        var activeTouchDeltas = new Vector2[4];
        int activeTouchCount = 0;

        for (int i = 0; i < touchscreen.touches.Count; i++)
        {
            var touch = touchscreen.touches[i];
            if (!touch.press.isPressed)
            {
                continue;
            }

            if (activeTouchCount >= activeTouches.Length)
            {
                break;
            }

            activeTouches[activeTouchCount] = touch.position.ReadValue();
            activeTouchDeltas[activeTouchCount] = touch.delta.ReadValue();
            activeTouchCount++;
        }

        if (activeTouchCount < 2)
        {
            return false;
        }

        Vector2 firstCurrent = activeTouches[0];
        Vector2 secondCurrent = activeTouches[1];

        Vector2 firstPrevious = firstCurrent - activeTouchDeltas[0];
        Vector2 secondPrevious = secondCurrent - activeTouchDeltas[1];

        float previousDistance = Vector2.Distance(firstPrevious, secondPrevious);
        float currentDistance = Vector2.Distance(firstCurrent, secondCurrent);

        pinchDelta = currentDistance - previousDistance;
        return true;
    }

    private static bool IsPointerOverUi()
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen != null)
        {
            for (int i = 0; i < touchscreen.touches.Count; i++)
            {
                var touch = touchscreen.touches[i];
                if (touch.press.isPressed && EventSystem.current.IsPointerOverGameObject(touch.touchId.ReadValue()))
                {
                    return true;
                }
            }
        }

        return EventSystem.current.IsPointerOverGameObject(-1);
    }

    private void MoveCameraTo(Vector3 worldPosition, float duration, bool instant = false)
    {
        if (targetCamera == null)
        {
            return;
        }

        worldPosition.z = targetCamera.transform.position.z;
        desiredCameraPosition = GetClampedCameraPosition(worldPosition, desiredOrthographicSize);

        if (instant || duration <= 0f)
        {
            StopPanTween();
            targetCamera.transform.position = desiredCameraPosition;
            ClampCameraToBounds();
            return;
        }

        if (panTween != null && panTween.IsActive())
        {
            panTween.ChangeEndValue(desiredCameraPosition, true);
            return;
        }

        panTween = targetCamera.transform
            .DOMove(desiredCameraPosition, duration)
            .SetEase(panSmoothEase)
            .SetUpdate(UpdateType.Late)
            .OnUpdate(ClampCameraToBounds)
            .OnKill(() => panTween = null);
    }

    private void MoveZoomTo(float zoomValue, float duration, bool instant = false)
    {
        if (targetCamera == null)
        {
            return;
        }

        desiredOrthographicSize = Mathf.Clamp(zoomValue, minZoom, maxZoom);

        if (instant || duration <= 0f)
        {
            StopZoomTween();
            targetCamera.orthographicSize = desiredOrthographicSize;
            ClampCameraToBounds();
            return;
        }

        if (zoomTween != null && zoomTween.IsActive())
        {
            zoomTween.ChangeEndValue(desiredOrthographicSize, true);
            return;
        }

        zoomTween = DOTween
            .To(
                () => targetCamera.orthographicSize,
                value =>
                {
                    targetCamera.orthographicSize = value;
                    ClampCameraToBounds();
                },
                desiredOrthographicSize,
                duration)
            .SetEase(zoomSmoothEase)
            .SetUpdate(UpdateType.Late)
            .OnKill(() => zoomTween = null);
    }

    private void CacheDesiredCameraState()
    {
        if (targetCamera == null)
        {
            return;
        }

        desiredCameraPosition = targetCamera.transform.position;
        desiredOrthographicSize = Mathf.Clamp(targetCamera.orthographicSize, minZoom, maxZoom);
    }

    private void StopPanTween()
    {
        if (panTween != null)
        {
            panTween.Kill();
            panTween = null;
        }
    }

    private void StopZoomTween()
    {
        if (zoomTween != null)
        {
            zoomTween.Kill();
            zoomTween = null;
        }
    }

    private void StopCameraTweens()
    {
        StopPanTween();
        StopZoomTween();
    }
}
