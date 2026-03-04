using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Tiny helper component that makes a menu button's label text
/// and arrow sprite change colour when the button is selected/hovered.
/// Works for both mouse hover and keyboard/gamepad selection.
/// </summary>
[RequireComponent(typeof(Button))]
public class MenuButtonHighlight : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
{
    [HideInInspector] public Text  Label;
    [HideInInspector] public Image Arrow;
    [HideInInspector] public Color NormalCol;
    [HideInInspector] public Color SelectCol;
    [HideInInspector] public Color ArrowCol;

    void Start()
    {
        SetState(false);
    }

    public void OnSelect(BaseEventData e)        => SetState(true);
    public void OnDeselect(BaseEventData e)      => SetState(false);
    public void OnPointerEnter(PointerEventData e)
    {
        SetState(true);
        // Also set this as the selected object so gamepad can seamlessly continue from here
        EventSystem.current?.SetSelectedGameObject(gameObject);
    }
    public void OnPointerExit(PointerEventData e) => SetState(false);

    void SetState(bool selected)
    {
        if (Label != null)
        {
            Label.color    = selected ? SelectCol : NormalCol;
            Label.fontSize = selected ? 24 : 22;
        }
        if (Arrow != null)
            Arrow.color = selected ? ArrowCol : Color.clear;
    }
}
