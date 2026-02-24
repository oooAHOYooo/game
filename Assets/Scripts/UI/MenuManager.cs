using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// MenuManager — procedural Start Menu built in code.
/// Built to match the project's cinematic palette.
/// </summary>
public class MenuManager : MonoBehaviour
{
    private GameObject _menuRoot;
    private Button     _startButton;
    private Button     _quitButton;

    void Start()
    {
        BuildMenu();
    }

    void BuildMenu()
    {
        // ─── Ensure we have an EventSystem ──────────────────────────────
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        // ─── Menu Container ──────────────────────────────────────────────
        _menuRoot = new GameObject("StartMenu");
        _menuRoot.transform.SetParent(transform, false);
        
        var canvas = _menuRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // Above HUD
        
        _menuRoot.AddComponent<CanvasScaler>();
        _menuRoot.AddComponent<GraphicRaycaster>();

        // ─── Dark Overlay ────────────────────────────────────────────────
        var overlay = new GameObject("Overlay");
        overlay.transform.SetParent(_menuRoot.transform, false);
        var ovImg = overlay.AddComponent<Image>();
        ovImg.color = new Color(0.02f, 0.02f, 0.05f, 0.92f);
        
        var ovRT = overlay.GetComponent<RectTransform>();
        ovRT.anchorMin = Vector2.zero;
        ovRT.anchorMax = Vector2.one;
        ovRT.offsetMin = ovRT.offsetMax = Vector2.zero;

        // ─── Title ───────────────────────────────────────────────────────
        var titleText = CreateLabel(overlay, "NINJA ISLAND", new Vector2(0, 120), 48, GameBootstrapper.PaletteGold);
        titleText.alignment = TextAnchor.MiddleCenter;
        var subtitleText = CreateLabel(overlay, "— Open World Ninja Combat —", new Vector2(0, 70), 14, GameBootstrapper.PaletteCyan);
        subtitleText.alignment = TextAnchor.MiddleCenter;

        // ─── Button Container ────────────────────────────────────────────
        var btnGroup = new GameObject("ButtonGroup");
        btnGroup.transform.SetParent(overlay.transform, false);
        var bgRT = btnGroup.AddComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0.5f, 0.4f);
        bgRT.anchorMax = new Vector2(0.5f, 0.6f);
        bgRT.sizeDelta = new Vector2(300, 150);

        // ─── Start Button ────────────────────────────────────────────────
        _startButton = CreateButton(btnGroup, "ENTER THE ISLAND", new Vector2(0, 40), () => {
            GameBootstrapper.Instance.StartGame();
        });

        // ─── Quit Button ─────────────────────────────────────────────────
        _quitButton = CreateButton(btnGroup, "QUIT GAME", new Vector2(0, -40), () => {
            GameBootstrapper.Instance.QuitGame();
        });
        
        // Default cursor state
        ShowMenu();
    }

    public void ShowMenu()
    {
        _menuRoot.SetActive(true);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void HideMenu()
    {
        _menuRoot.SetActive(false);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // ─── Helpers ─────────────────────────────────────────────────────────

    Text CreateLabel(GameObject parent, string text, Vector2 anchoredPos, int fontSize, Color color)
    {
        var obj = new GameObject("Label_" + text.Replace(" ", "_"));
        obj.transform.SetParent(parent.transform, false);

        var t = obj.AddComponent<Text>();
        t.text      = text;
        t.fontSize  = fontSize;
        t.color     = color;
        t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontStyle = FontStyle.Bold;

        var rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(800, fontSize + 20);

        return t;
    }

    Button CreateButton(GameObject parent, string text, Vector2 anchoredPos, UnityEngine.Events.UnityAction action)
    {
        var btnObj = new GameObject("Button_" + text);
        btnObj.transform.SetParent(parent.transform, false);

        var img = btnObj.AddComponent<Image>();
        img.color = new Color(0.12f, 0.12f, 0.20f, 1f);

        var btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(action);

        var rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(240, 50);

        var label = CreateLabel(btnObj, text, Vector2.zero, 14, Color.white);
        label.alignment = TextAnchor.MiddleCenter;
        var lRT = label.GetComponent<RectTransform>();
        lRT.anchorMin = Vector2.zero;
        lRT.anchorMax = Vector2.one;
        lRT.offsetMin = lRT.offsetMax = Vector2.zero;

        // Hover effect
        var cb = btn.colors;
        cb.normalColor      = new Color(0.12f, 0.12f, 0.20f, 1f);
        cb.highlightedColor = GameBootstrapper.PaletteGold;
        cb.pressedColor     = GameBootstrapper.PaletteCrimson;
        cb.selectedColor    = GameBootstrapper.PaletteGold;
        btn.colors = cb;

        return btn;
    }
}
