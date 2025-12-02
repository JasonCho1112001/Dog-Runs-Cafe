using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class UIInputDebug : MonoBehaviour
{
    public GraphicRaycaster uiRaycaster;
    public Camera worldCamera;
    public LayerMask worldMask = ~0;
    public float worldRayDistance = 100f;

    void Start()
    {
        if (uiRaycaster == null)
            uiRaycaster = FindObjectOfType<GraphicRaycaster>();
        if (worldCamera == null)
            worldCamera = Camera.main;
        Debug.Log($"UIInputDebug started. EventSystem present: {EventSystem.current != null}. InputModule: {EventSystem.current?.currentInputModule?.GetType().Name ?? "none"}");
    }

    void Update()
    {
        bool clicked = false;
        Vector2 screenPos = Vector2.zero;
        var mouse = Mouse.current;
        if (mouse != null)
        {
            if (mouse.leftButton.wasPressedThisFrame)
            {
                clicked = true;
                screenPos = mouse.position.ReadValue();
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                clicked = true;
                screenPos = Input.mousePosition;
            }
        }

        if (!clicked) return;

        Debug.Log($"Click at {screenPos}. EventSystem: {(EventSystem.current != null)}; InputModule: {EventSystem.current?.currentInputModule?.GetType().Name ?? "none"}; Cursor lock: {Cursor.lockState}; Cursor vis: {Cursor.visible}");

        if (uiRaycaster != null && EventSystem.current != null)
        {
            var pointer = new PointerEventData(EventSystem.current) { position = screenPos };
            var results = new List<RaycastResult>();
            uiRaycaster.Raycast(pointer, results);

            if (results.Count > 0)
            {
                Debug.Log("UI hits (top -> bottom):");
                foreach (var r in results)
                {
                    Debug.Log($"  {r.gameObject.name}  (raycastTarget={HasGraphicRaycastTarget(r.gameObject)})");
                }
            }
            else
            {
                Debug.Log("UI: no hits");
            }
        }
        else
        {
            Debug.Log("No GraphicRaycaster or EventSystem for UI raycast.");
        }

        if (worldCamera != null)
        {
            Ray ray = worldCamera.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, worldRayDistance, worldMask, QueryTriggerInteraction.Ignore))
            {
                Debug.Log($"WORLD hit: {hit.collider.gameObject.name} at {hit.point}");
            }
            else
            {
                Debug.Log("WORLD: no hit");
            }
        }
    }

    bool HasGraphicRaycastTarget(GameObject go)
    {
        var g = go.GetComponent<Graphic>();
        if (g == null) return false;
        return g.raycastTarget;
    }
}