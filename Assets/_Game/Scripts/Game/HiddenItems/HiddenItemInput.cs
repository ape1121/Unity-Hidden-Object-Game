using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class HiddenItemInput : MonoBehaviour
{
    [SerializeField] private Camera worldCamera;
    [SerializeField] private LayerMask itemLayerMask = ~0;
    [SerializeField] private bool ignoreClicksOverUi = true;

    public event Action<HiddenItem> OnHiddenItemClicked;

    private void Awake()
    {
        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }
    }

    private void Update()
    {
        Vector2 screenPosition;
        if (!TryGetPointerDownPosition(out screenPosition))
        {
            return;
        }

        if (ignoreClicksOverUi && IsPointerOverUi())
        {
            return;
        }

        if (worldCamera == null)
        {
            return;
        }

        Vector2 worldPosition = worldCamera.ScreenToWorldPoint(screenPosition);
        Collider2D hit = Physics2D.OverlapPoint(worldPosition, itemLayerMask);
        if (hit == null)
        {
            return;
        }

        HiddenItem hiddenItem = hit.GetComponent<HiddenItem>();
        if (hiddenItem != null)
        {
            OnHiddenItemClicked?.Invoke(hiddenItem);
        }
    }

    private static bool TryGetPointerDownPosition(out Vector2 screenPosition)
    {
        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen != null)
        {
            for (int i = 0; i < touchscreen.touches.Count; i++)
            {
                var touch = touchscreen.touches[i];
                if (touch.press.wasPressedThisFrame)
                {
                    screenPosition = touch.position.ReadValue();
                    return true;
                }
            }
        }

        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            screenPosition = mouse.position.ReadValue();
            return true;
        }

        screenPosition = Vector2.zero;
        return false;
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
