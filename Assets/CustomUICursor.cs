using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class FloatingUICursor : MonoBehaviour
{
    [Header("Cursor Setup")]
    [SerializeField] private RectTransform cursorRect;
    [SerializeField] private Canvas canvas;
    [SerializeField] private float cursorSpeed = 1000f;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveCursorAction;
    [SerializeField] private InputActionReference clickAction;
    [SerializeField] private bool clampToScreen = true;

    [Header("UI Interaction")]
    [SerializeField] private GraphicRaycaster raycaster;
    [SerializeField] private EventSystem eventSystem;

    private Vector2 screenPosition;
    private Coroutine clickCoroutine;
    private void OnEnable()
    {
        moveCursorAction.action.Enable();
        clickAction.action.Enable();
        clickAction.action.performed += OnClick;
    }

    private void OnDisable()
    {
        moveCursorAction.action.Disable();
        clickAction.action.Disable();
        clickAction.action.performed -= OnClick;
    }

    private void Start()
    {
        screenPosition = new Vector2(Screen.width / 2f, Screen.height / 2f);
    }

    private void Update()
    {
        Vector2 move = moveCursorAction.action.ReadValue<Vector2>();
        screenPosition += cursorSpeed * Time.deltaTime * move;

        if (clampToScreen)
        {
            screenPosition.x = Mathf.Clamp(screenPosition.x, 0, Screen.width);
            screenPosition.y = Mathf.Clamp(screenPosition.y, 0, Screen.height);
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            screenPosition,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out Vector2 localPos))
        {
            cursorRect.anchoredPosition = localPos;
        }
    }

    private void OnClick(InputAction.CallbackContext ctx) => clickCoroutine ??= StartCoroutine(DelayedClick());

    //delay because we need to let UI clean up to prevent click propagation
    private IEnumerator DelayedClick()
    {
        yield return new WaitForSeconds(0.1f);
        PointerEventData pointer = new(eventSystem)
        {
            position = screenPosition
        };

        List<RaycastResult> results = new();
        raycaster.Raycast(pointer, results);

        if (results.Count > 0 && results[0].gameObject != null)
            SimulateClick(results[0].gameObject);
        yield return new WaitForSeconds(0.1f);
        clickCoroutine = null;
        yield break;
    }

    private void SimulateClick(GameObject target)
    {
        var pointerData = new PointerEventData(EventSystem.current)
        {
            pointerId = -1,
            position = screenPosition,
            clickTime = Time.unscaledTime,
            clickCount = 1,
            button = PointerEventData.InputButton.Left,
            eligibleForClick = true,
            useDragThreshold = true
        };

        EventSystem.current.SetSelectedGameObject(target);

        ExecuteEvents.Execute(target, pointerData, ExecuteEvents.pointerEnterHandler);
        ExecuteEvents.Execute(target, pointerData, ExecuteEvents.pointerDownHandler);
        ExecuteEvents.Execute(target, pointerData, ExecuteEvents.pointerClickHandler);
        ExecuteEvents.Execute(target, pointerData, ExecuteEvents.pointerUpHandler);
        ExecuteEvents.Execute(target, pointerData, ExecuteEvents.selectHandler);
        ExecuteEvents.Execute(target, pointerData, ExecuteEvents.submitHandler);
        pointerData.eligibleForClick = false;
        pointerData.pointerPress = null;
        EventSystem.current.SetSelectedGameObject(null);
    }
}
