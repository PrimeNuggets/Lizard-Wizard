using UnityEngine;
using UnityEngine.InputSystem;

//-- Hold the Map button (tab / d-pad up) to make the minimap bigger and zoom it out
//-- goes on the MinimapCircle object
public class MinimapView : MonoBehaviour
{
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private Camera mapCamera;
    [SerializeField, Min(1f)] private float expandedDiameter = 300f;
    [SerializeField, Min(1f)] private float zoomOut = 2f; //-- 2 = see twice as far when expanded
    [SerializeField, Min(0.01f)] private float transitionTime = 0.2f;

    private RectTransform circle;
    private RectTransform canvasRect;
    private InputAction mapAction;
    private float progress; //-- 0 = small, 1 = fully expanded

    //-- remember how the small map was set up in the scene so we can go back to it
    private Vector2 smallAnchor;
    private Vector2 smallPivot;
    private Vector2 smallPosition;
    private Vector2 smallSize;
    private float smallCameraSize;

    private void Start()
    {
        circle = (RectTransform)transform;
        canvasRect = (RectTransform)circle.parent;
        smallAnchor = circle.anchorMin;
        smallPivot = circle.pivot;
        smallPosition = circle.anchoredPosition;
        smallSize = circle.sizeDelta;
        smallCameraSize = mapCamera.orthographicSize;
        mapAction = playerInput.actions.FindAction("Player/Map", true);
    }

    private void LateUpdate()
    {
        bool expand = Time.timeScale > 0f && mapAction.IsPressed();
        if (!expand && progress == 0f) return;

        //-- unscaled so it still shrinks back when the game is paused
        progress = Mathf.MoveTowards(progress, expand ? 1f : 0f, Time.unscaledDeltaTime / transitionTime);
        ApplyLayout(Mathf.SmoothStep(0f, 1f, progress));
    }

    private void ApplyLayout(float blend)
    {
        //-- big map slides to the middle of the left side. cap it at 80% of the screen height for small screens
        Vector2 leftMiddle = new Vector2(0f, 0.5f);
        float diameter = Mathf.Max(smallSize.x, Mathf.Min(expandedDiameter, canvasRect.rect.height * 0.8f));
        circle.anchorMin = Vector2.Lerp(smallAnchor, leftMiddle, blend);
        circle.anchorMax = circle.anchorMin;
        circle.pivot = Vector2.Lerp(smallPivot, leftMiddle, blend);
        circle.sizeDelta = Vector2.Lerp(smallSize, Vector2.one * diameter, blend);
        circle.anchoredPosition = Vector2.Lerp(smallPosition, new Vector2(smallPosition.x, 0f), blend);
        mapCamera.orthographicSize = Mathf.Lerp(smallCameraSize, smallCameraSize * zoomOut, blend);
    }

    private void OnDisable()
    {
        if (mapAction == null || mapCamera == null) return;

        progress = 0f;
        ApplyLayout(0f);
    }
}
