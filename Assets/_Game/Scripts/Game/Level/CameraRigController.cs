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

    private Vector3 lastPointerWorldPosition;
    private bool isPanning;
    private bool panBlockedUntilPointerRelease;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
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
        targetCamera.transform.position += delta;
        ClampCameraToBounds();
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

        float targetZoom = targetCamera.orthographicSize - (zoomDelta * zoomSpeed * Time.deltaTime * 60f);
        targetCamera.orthographicSize = Mathf.Clamp(targetZoom, minZoom, maxZoom);
    }

    private void ClampCameraToBounds()
    {
        float verticalExtent = targetCamera.orthographicSize;
        float horizontalExtent = verticalExtent * targetCamera.aspect;

        float minX = boundsMin.x + horizontalExtent;
        float maxX = boundsMax.x - horizontalExtent;
        float minY = boundsMin.y + verticalExtent;
        float maxY = boundsMax.y - verticalExtent;

        Vector3 position = targetCamera.transform.position;
        position.x = ClampAxis(position.x, minX, maxX);
        position.y = ClampAxis(position.y, minY, maxY);
        targetCamera.transform.position = position;
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
}
