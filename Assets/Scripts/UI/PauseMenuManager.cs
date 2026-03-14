using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// PauseMenuManager — procedural Pause Menu built in code.
/// Supports new Input System gamepad / keyboard navigation.
/// </summary>
public class PauseMenuManager : MonoBehaviour
{
    private GameObject _menuRoot;
    private GameObject _buildInfoPanel;
    private Button     _resumeButton;
    private Button     _settingsButton;
    private Button     _saveButton;
    private Button     _exitButton;

    private bool _isPaused = false;
    private bool _buildInfoVisible = false;

    void Start()
    {
        BuildMenu();
        HideMenu(); // hidden by default!
    }

    void Update()
    {
        // Toggle menu with Escape or Gamepad Start
        bool togglePressed = false;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            togglePressed = true;
        
        if (Gamepad.all.Count > 0 && Gamepad.all[0].startButton.wasPressedThisFrame)
            togglePressed = true;

        if (togglePressed)
        {
            if (_isPaused) ResumeGame();
            else PauseGame();
        }
    }

    void BuildMenu()
    {
        // ─── Ensure we have an EventSystem ──────────────────────────────
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // ─── Menu Container ──────────────────────────────────────────────
        _menuRoot = new GameObject("PauseMenu");
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
        var titleText = CreateLabel(overlay, "PAUSED", new Vector2(0, 300), 80, GameBootstrapper.PaletteGold);
        titleText.alignment = TextAnchor.MiddleCenter;

        // ─── Controls Reference (three-column) ──────────────────────────
        BuildControlsSection(overlay);

        // ─── Button Container ────────────────────────────────────────────
        var btnGroup = new GameObject("ButtonGroup");
        btnGroup.transform.SetParent(overlay.transform, false);
        var bgRT = btnGroup.AddComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0.75f, 0.5f);
        bgRT.anchorMax = new Vector2(0.75f, 0.5f);
        bgRT.anchoredPosition = new Vector2(0, -50);
        bgRT.sizeDelta = new Vector2(400, 400);

        // ─── Build Info Panel (hidden by default) ────────────────────────
        _buildInfoPanel = BuildInfoPanel(overlay);
        _buildInfoPanel.SetActive(false);

        // ─── Buttons ─────────────────────────────────────────────────────
        _resumeButton   = CreateButton(btnGroup, "RESUME",    new Vector2(0,  150), () => { ResumeGame(); });
        _settingsButton = CreateButton(btnGroup, "BUILD INFO", new Vector2(0,   50), ToggleBuildInfo);
        _saveButton     = CreateButton(btnGroup, "SAVE GAME", new Vector2(0,  -50), () => { Debug.Log("Save not yet implemented."); });
        _exitButton     = CreateButton(btnGroup, "EXIT GAME", new Vector2(0, -150), () => { QuitGame(); });
        
        // Link navigation manually to ensure controller D-Pad/Stick works reliably
        SetupNavigation(_resumeButton, _settingsButton, _exitButton);
        SetupNavigation(_settingsButton, _saveButton, _resumeButton);
        SetupNavigation(_saveButton, _exitButton, _settingsButton);
        SetupNavigation(_exitButton, _resumeButton, _saveButton);
    }

    void BuildControlsSection(GameObject parent)
    {
        // Section header
        var hdr = CreateLabel(parent, "— CONTROLS REFERENCE —", new Vector2(-210, 195), 34, GameBootstrapper.PaletteGold);
        hdr.alignment = TextAnchor.MiddleCenter;
        var hdrRT = hdr.GetComponent<RectTransform>();
        hdrRT.sizeDelta = new Vector2(860, 50);

        // Column data
        string p1Body =
            "<b>MOVE</b>\nWASD\n\n" +
            "<b>JUMP</b>\nSPACE\n\n" +
            "<b>FLY</b>\nE / Q\n\n" +
            "<b>DODGE</b>\nSHIFT\n\n" +
            "<b>ATTACK</b>\nJ\n\n" +
            "<b>HEAVY</b>\nK (hold)\n\n" +
            "<b>KI BEAM</b>\nL (hold)\n\n" +
            "<b>BLOCK</b>\nI\n\n" +
            "<b>WEAPON</b>\nU (hold 1s)\n\n" +
            "<b>LOCK-ON</b>\nF";

        string padBody =
            "<b>MOVE</b>\nLeft Stick\n\n" +
            "<b>JUMP</b>\nSouth  (A/Cross)\n\n" +
            "<b>FLY</b>\nR-Stick Up\n\n" +
            "<b>DODGE</b>\nEast  (B/Circle)\n\n" +
            "<b>ATTACK</b>\nRB / RT\n\n" +
            "<b>HEAVY</b>\nRT (hold)\n\n" +
            "<b>KI BEAM</b>\nLT (hold)\n\n" +
            "<b>BLOCK</b>\nLB\n\n" +
            "<b>WEAPON</b>\nLB (hold 1s)\n\n" +
            "<b>LOCK-ON</b>\nR-Stick Btn";

        string p2Body =
            "<b>MOVE</b>\nArrow Keys\n\n" +
            "<b>JUMP</b>\nR-Ctrl\n\n" +
            "<b>FLY</b>\nR-Ctrl (hold)\n\n" +
            "<b>DODGE</b>\nR-Shift\n\n" +
            "<b>ATTACK</b>\nP\n\n" +
            "<b>HEAVY</b>\n\\ (hold)\n\n" +
            "<b>KI BEAM</b>\nEnter (hold)\n\n" +
            "<b>BLOCK</b>\n;\n\n" +
            "<b>WEAPON</b>\n' (hold 1s)\n\n" +
            "<b>LOCK-ON</b>\n—";

        CreateControlColumn(parent, "PLAYER 1", "Keyboard", p1Body, new Vector2(-530, -60), GameBootstrapper.PaletteGold);
        CreateControlColumn(parent, "GAMEPAD",  "",          padBody, new Vector2(-165, -60), new Color(0.75f, 0.75f, 0.75f));
        CreateControlColumn(parent, "PLAYER 2", "Keyboard", p2Body, new Vector2( 200, -60), GameBootstrapper.PaletteCyan);
    }

    void CreateControlColumn(GameObject parent, string title, string subtitle, string body, Vector2 pos, Color titleColor)
    {
        const float W = 310f;
        const float H = 490f;

        // Background card
        var card = new GameObject("Card_" + title);
        card.transform.SetParent(parent.transform, false);
        var cardImg = card.AddComponent<Image>();
        cardImg.color = new Color(0.06f, 0.06f, 0.16f, 0.82f);
        var cardRT = card.GetComponent<RectTransform>();
        cardRT.anchorMin = cardRT.anchorMax = new Vector2(0.5f, 0.5f);
        cardRT.anchoredPosition = pos;
        cardRT.sizeDelta = new Vector2(W, H);

        // Thin top accent bar
        var accent = new GameObject("Accent");
        accent.transform.SetParent(card.transform, false);
        var accentImg = accent.AddComponent<Image>();
        accentImg.color = titleColor;
        var aRT = accent.GetComponent<RectTransform>();
        aRT.anchorMin = new Vector2(0f, 1f);
        aRT.anchorMax = new Vector2(1f, 1f);
        aRT.pivot     = new Vector2(0.5f, 1f);
        aRT.offsetMin = aRT.offsetMax = Vector2.zero;
        aRT.sizeDelta = new Vector2(0f, 4f);

        // Title label
        var titleLbl = CreateLabel(card, title, new Vector2(0, -18), 28, titleColor);
        titleLbl.alignment = TextAnchor.UpperCenter;
        titleLbl.supportRichText = false;
        var tRT = titleLbl.GetComponent<RectTransform>();
        tRT.sizeDelta = new Vector2(W - 16f, 38f);

        // Subtitle (e.g. "Keyboard")
        if (!string.IsNullOrEmpty(subtitle))
        {
            var subLbl = CreateLabel(card, subtitle, new Vector2(0, -50), 20, new Color(0.7f, 0.7f, 0.7f));
            subLbl.alignment = TextAnchor.UpperCenter;
            subLbl.supportRichText = false;
            var sRT = subLbl.GetComponent<RectTransform>();
            sRT.sizeDelta = new Vector2(W - 16f, 28f);
        }

        // Body — action / key pairs
        float bodyY = string.IsNullOrEmpty(subtitle) ? -58f : -78f;
        var bodyLbl = CreateLabel(card, body, new Vector2(0, bodyY), 22, Color.white);
        bodyLbl.alignment = TextAnchor.UpperCenter;
        bodyLbl.supportRichText = true;
        bodyLbl.horizontalOverflow = HorizontalWrapMode.Wrap;
        var bRT = bodyLbl.GetComponent<RectTransform>();
        bRT.anchorMin = new Vector2(0f, 0f);
        bRT.anchorMax = new Vector2(1f, 1f);
        bRT.offsetMin = new Vector2(10f, 8f);
        bRT.offsetMax = new Vector2(-10f, bodyY);
    }

    GameObject BuildInfoPanel(GameObject parent)
    {
        var panel = new GameObject("BuildInfoPanel");
        panel.transform.SetParent(parent.transform, false);
        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.04f, 0.12f, 0.97f);
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(500f, 260f);
        rt.anchoredPosition = new Vector2(-100f, 0f);

        // Accent bar
        var accent = new GameObject("Accent");
        accent.transform.SetParent(panel.transform, false);
        accent.AddComponent<Image>().color = GameBootstrapper.PaletteGold;
        var aRT = accent.GetComponent<RectTransform>();
        aRT.anchorMin = new Vector2(0f, 1f); aRT.anchorMax = new Vector2(1f, 1f);
        aRT.pivot = new Vector2(0.5f, 1f);
        aRT.offsetMin = aRT.offsetMax = Vector2.zero;
        aRT.sizeDelta = new Vector2(0f, 4f);

        // Build version + timestamp
        string deviceLine = $"Keyboards: {Keyboard.all.Count}   Gamepads: {Gamepad.all.Count}";
        string gpList = "";
        foreach (var gp in Gamepad.all) gpList += $"\n  • {gp.displayName}";

        string body =
            $"<b>VERSION</b>   v{BuildInfo.Version}\n\n" +
            $"<b>BUILT</b>       {BuildInfo.BuildTime}\n\n" +
            $"<b>PLATFORM</b>  {Application.platform}\n\n" +
            $"<b>INPUT</b>       {deviceLine}{gpList}";

        var lbl = CreateLabel(panel, body, new Vector2(0f, -20f), 20, Color.white);
        lbl.alignment = TextAnchor.UpperLeft;
        lbl.supportRichText = true;
        var lRT = lbl.GetComponent<RectTransform>();
        lRT.anchorMin = Vector2.zero; lRT.anchorMax = Vector2.one;
        lRT.offsetMin = new Vector2(20f, 12f); lRT.offsetMax = new Vector2(-20f, -24f);

        return panel;
    }

    void ToggleBuildInfo()
    {
        _buildInfoVisible = !_buildInfoVisible;
        _buildInfoPanel.SetActive(_buildInfoVisible);
    }

    void SetupNavigation(Button btn, Button downTarget, Button upTarget)
    {
        var nav = btn.navigation;
        nav.mode = Navigation.Mode.Explicit;
        nav.selectOnDown = downTarget;
        nav.selectOnUp = upTarget;
        btn.navigation = nav;
    }

    public void PauseGame()
    {
        _isPaused = true;
        _menuRoot.SetActive(true);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Time.timeScale = 0f;

        if (EventSystem.current != null && _resumeButton != null)
        {
            EventSystem.current.SetSelectedGameObject(_resumeButton.gameObject);
        }
    }

    public void ResumeGame()
    {
        _isPaused = false;
        _menuRoot.SetActive(false);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        Time.timeScale = 1f;
        
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
    
    public void HideMenu()
    {
        _menuRoot.SetActive(false);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    // ─── Helpers ─────────────────────────────────────────────────────────

    Text CreateLabel(GameObject parent, string text, Vector2 anchoredPos, int fontSize, Color color)
    {
        var obj = new GameObject("Label_" + text.Replace(" ", "_").Split('\n')[0]);
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
        rt.sizeDelta = new Vector2(1200, 800);

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
        rt.sizeDelta = new Vector2(350, 80);

        var label = CreateLabel(btnObj, text, Vector2.zero, 32, Color.white);
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
