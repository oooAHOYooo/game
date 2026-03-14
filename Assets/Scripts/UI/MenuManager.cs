using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// MenuManager — procedural Start Menu built in code.
/// Inspired by minimalist game menus (Firewatch, Journey).
/// Left-aligned title + menu items. Transparent background so
/// the procedural island scene shows through.
/// Fully navigable by Keyboard, PS4 and Xbox gamepad.
/// </summary>
public class MenuManager : MonoBehaviour
{
    private GameObject _menuRoot;
    private Button     _playButton;
    private Button     _creditsButton;
    private Button     _quitButton;

    void Start()
    {
        BuildMenu();
    }

    void BuildMenu()
    {
        // ─── Ensure EventSystem with new Input System ────────────────────
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // ─── Root Canvas ─────────────────────────────────────────────────
        _menuRoot = new GameObject("StartMenu");
        _menuRoot.transform.SetParent(transform, false);

        var canvas = _menuRoot.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = _menuRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight  = 0.5f;

        _menuRoot.AddComponent<GraphicRaycaster>();

        // ─── Semi-transparent gradient on the left so text is legible ────
        var gradient = new GameObject("LeftGradient");
        gradient.transform.SetParent(_menuRoot.transform, false);
        var gImg = gradient.AddComponent<Image>();
        gImg.color = new Color(0f, 0f, 0f, 0.55f);
        var gRT = gradient.GetComponent<RectTransform>();
        gRT.anchorMin  = new Vector2(0f, 0f);
        gRT.anchorMax  = new Vector2(0.38f, 1f);
        gRT.offsetMin  = gRT.offsetMax = Vector2.zero;

        // ─── Version Label (bottom right) ────────────────────────────────
        var verLbl = MakeLabel(_menuRoot, $"v{BuildInfo.Version}  •  {BuildInfo.BuildTime}", 14, new Color(0.6f, 0.6f, 0.6f, 0.8f));
        var verRT  = verLbl.GetComponent<RectTransform>();
        verRT.anchorMin        = new Vector2(1f, 0f);
        verRT.anchorMax        = new Vector2(1f, 0f);
        verRT.pivot            = new Vector2(1f, 0f);
        verRT.anchoredPosition = new Vector2(-20f, 14f);
        verRT.sizeDelta        = new Vector2(260f, 24f);
        verLbl.alignment       = TextAnchor.MiddleRight;

        // ─── Title — Top Left ─────────────────────────────────────────────
        var titleLbl = MakeLabel(_menuRoot, "NINJA\nISLAND", 96, Color.white);
        titleLbl.fontStyle  = FontStyle.Bold;
        titleLbl.lineSpacing = 0.85f;
        titleLbl.alignment  = TextAnchor.UpperLeft;
        var titleRT = titleLbl.GetComponent<RectTransform>();
        titleRT.anchorMin        = new Vector2(0f, 1f);
        titleRT.anchorMax        = new Vector2(0.36f, 1f);
        titleRT.pivot            = new Vector2(0f, 1f);
        titleRT.anchoredPosition = new Vector2(60f, -50f);
        titleRT.sizeDelta        = new Vector2(700f, 260f);

        // Thin underline beneath title
        MakeRule(_menuRoot, new Vector2(60f, -325f), new Vector2(280f, 2f));

        // ─── Subtitle ─────────────────────────────────────────────────────
        var subLbl = MakeLabel(_menuRoot, "OPEN WORLD NINJA COMBAT", 14, new Color(0.75f, 0.85f, 1f, 0.9f));
        subLbl.alignment   = TextAnchor.MiddleLeft;
        subLbl.fontStyle   = FontStyle.Normal;
        var subRT = subLbl.GetComponent<RectTransform>();
        subRT.anchorMin        = new Vector2(0f, 1f);
        subRT.anchorMax        = new Vector2(0.36f, 1f);
        subRT.pivot            = new Vector2(0f, 1f);
        subRT.anchoredPosition = new Vector2(60f, -340f);
        subRT.sizeDelta        = new Vector2(600f, 30f);

        // ─── Button Stack (left-bottom area) ─────────────────────────────
        // Anchor buttons to the lower-left quadrant
        _playButton    = MakeMenuButton(_menuRoot, "PLAY",    new Vector2(60f, -450f), () => GameBootstrapper.Instance.StartGame());
        _creditsButton = MakeMenuButton(_menuRoot, "CREDITS", new Vector2(60f, -520f), () => Debug.Log("Credits coming soon!"));
        _quitButton    = MakeMenuButton(_menuRoot, "EXIT",    new Vector2(60f, -590f), () => GameBootstrapper.Instance.QuitGame());

        // ─── Navigation wiring (D-Pad / keyboard arrows) ─────────────────
        SetNav(_playButton,    _creditsButton, _quitButton);
        SetNav(_creditsButton, _quitButton,    _playButton);
        SetNav(_quitButton,    _playButton,    _creditsButton);

        // ─── Credits (bottom left) ────────────────────────────────────────
        var credLbl = MakeLabel(_menuRoot, "produced by barnacle studios  |  created by alex gonzalez", 12, new Color(0.65f, 0.65f, 0.65f, 0.9f));
        credLbl.alignment = TextAnchor.LowerLeft;
        var credRT = credLbl.GetComponent<RectTransform>();
        credRT.anchorMin        = new Vector2(0f, 0f);
        credRT.anchorMax        = new Vector2(0.5f, 0f);
        credRT.pivot            = new Vector2(0f, 0f);
        credRT.anchoredPosition = new Vector2(60f, 18f);
        credRT.sizeDelta        = new Vector2(900f, 26f);

        ShowMenu();
    }

    // ─────────────────────────────────────────────────────────────────────
    // PUBLIC API
    // ─────────────────────────────────────────────────────────────────────
    public void ShowMenu()
    {
        _menuRoot.SetActive(true);
        Cursor.visible   = true;
        Cursor.lockState = CursorLockMode.None;
        Time.timeScale   = 0f;

        if (EventSystem.current != null && _playButton != null)
            EventSystem.current.SetSelectedGameObject(_playButton.gameObject);
    }

    public void HideMenu()
    {
        _menuRoot.SetActive(false);
        Cursor.visible   = false;
        Cursor.lockState = CursorLockMode.Locked;
        Time.timeScale   = 1f;
    }

    // ─────────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>Free-placed label on the root canvas.</summary>
    Text MakeLabel(GameObject parent, string text, int fontSize, Color color)
    {
        var obj = new GameObject("Lbl_" + text.Split('\n')[0].Replace(" ", "_"));
        obj.transform.SetParent(parent.transform, false);
        var t          = obj.AddComponent<Text>();
        t.text         = text;
        t.fontSize     = fontSize;
        t.color        = color;
        t.font         = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontStyle    = FontStyle.Bold;
        t.resizeTextForBestFit = false;
        return t;
    }

    /// <summary>Hairline horizontal rule.</summary>
    void MakeRule(GameObject parent, Vector2 anchoredPos, Vector2 size)
    {
        var obj = new GameObject("Rule");
        obj.transform.SetParent(parent.transform, false);
        var img   = obj.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.35f);
        var rt    = obj.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(0f, 1f);
        rt.pivot            = new Vector2(0f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = size;
    }

    /// <summary>
    /// Transparent-background, left-aligned text button that looks like
    /// the Firewatch / Journey menu items — just text with a hover arrow.
    /// </summary>
    Button MakeMenuButton(GameObject parent, string label, Vector2 anchoredPos, UnityEngine.Events.UnityAction action)
    {
        var btnObj = new GameObject("Btn_" + label);
        btnObj.transform.SetParent(parent.transform, false);

        // Invisible hit-area image
        var img   = btnObj.AddComponent<Image>();
        img.color = Color.clear;

        var btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(action);

        // Wide transparent hit-area anchored to left edge
        var rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(0f, 1f);
        rt.pivot            = new Vector2(0f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = new Vector2(340f, 52f);

        // ── Arrow indicator ───────────────────────────────────────────────
        var arrowObj       = new GameObject("Arrow");
        arrowObj.transform.SetParent(btnObj.transform, false);
        var arrowImg       = arrowObj.AddComponent<Image>();
        arrowImg.color     = Color.clear;           // invisible by default
        var arrowRT        = arrowObj.GetComponent<RectTransform>();
        arrowRT.anchorMin  = new Vector2(0f, 0.5f);
        arrowRT.anchorMax  = new Vector2(0f, 0.5f);
        arrowRT.pivot      = new Vector2(0f, 0.5f);
        arrowRT.anchoredPosition = new Vector2(0f, 0f);
        arrowRT.sizeDelta  = new Vector2(20f, 20f);

        // ── Text ──────────────────────────────────────────────────────────
        var lblObj          = new GameObject("Text");
        lblObj.transform.SetParent(btnObj.transform, false);
        var lbl             = lblObj.AddComponent<Text>();
        lbl.text            = label;
        lbl.font            = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        lbl.fontSize        = 22;
        lbl.fontStyle       = FontStyle.Bold;
        lbl.color           = new Color(0.85f, 0.85f, 0.85f, 1f);
        lbl.alignment       = TextAnchor.MiddleLeft;
        var lblRT           = lblObj.GetComponent<RectTransform>();
        lblRT.anchorMin     = Vector2.zero;
        lblRT.anchorMax     = Vector2.one;
        lblRT.offsetMin     = new Vector2(26f, 0f);
        lblRT.offsetMax     = Vector2.zero;

        // Underline rule beneath each button
        MakeRule(btnObj, new Vector2(0f, 0f), new Vector2(240f, 1f));

        // ── Colour states: bright + larger on selection / hover ───────────
        var cb                 = btn.colors;
        cb.normalColor         = new Color(1f, 1f, 1f, 0f);   // transparent — text does the job
        cb.highlightedColor    = new Color(1f, 1f, 1f, 0.08f);
        cb.selectedColor       = new Color(1f, 1f, 1f, 0.12f);
        cb.pressedColor        = new Color(1f, 0.5f, 0.1f, 0.2f);
        cb.fadeDuration        = 0.1f;
        btn.colors             = cb;

        // Tint text on select via a simple helper component
        btn.targetGraphic      = img;                          // drive on the invisible img so lbl stays white

        // We manage the label color manually from a tiny helper attached here
        var hi = btnObj.AddComponent<MenuButtonHighlight>();
        hi.Label     = lbl;
        hi.Arrow     = arrowImg;
        hi.NormalCol = new Color(0.72f, 0.72f, 0.72f);
        hi.SelectCol = Color.white;
        hi.ArrowCol  = new Color(1f, 1f, 1f, 0.9f);

        return btn;
    }

    static void SetNav(Button btn, Button down, Button up)
    {
        var nav      = btn.navigation;
        nav.mode     = Navigation.Mode.Explicit;
        nav.selectOnDown = down;
        nav.selectOnUp   = up;
        btn.navigation   = nav;
    }
}
