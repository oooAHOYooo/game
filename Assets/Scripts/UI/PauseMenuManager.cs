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
    private Button     _resumeButton;
    private Button     _settingsButton;
    private Button     _saveButton;
    private Button     _exitButton;
    
    private bool _isPaused = false;

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
        var titleText = CreateLabel(overlay, "PAUSED", new Vector2(0, 200), 48, GameBootstrapper.PaletteGold);
        titleText.alignment = TextAnchor.MiddleCenter;

        // ─── Controls Info ───────────────────────────────────────────────
        var controlsText = CreateLabel(overlay, 
            "Controls Reference:\n" +
            "Move: WASD / Left Stick\n" +
            "Jump / Ascend: Space / A (Cross)\n" +
            "Attacks: J, K, U, I / X, Y, RB\n" +
            "Dodge: Shift / B (Circle)\n" +
            "Transform Weapon: P / R3 (Right Stick Click)\n" +
            "Gravity Powers: LT/RT / L2/R2", 
            new Vector2(-300, 0), 16, GameBootstrapper.PaletteCyan);
        controlsText.alignment = TextAnchor.MiddleLeft;

        // ─── Button Container ────────────────────────────────────────────
        var btnGroup = new GameObject("ButtonGroup");
        btnGroup.transform.SetParent(overlay.transform, false);
        var bgRT = btnGroup.AddComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0.7f, 0.5f);
        bgRT.anchorMax = new Vector2(0.7f, 0.5f);
        bgRT.anchoredPosition = new Vector2(0, 0);
        bgRT.sizeDelta = new Vector2(300, 250);

        // ─── Buttons ─────────────────────────────────────────────────────
        _resumeButton = CreateButton(btnGroup, "RESUME", new Vector2(0, 80), () => { ResumeGame(); });
        _settingsButton = CreateButton(btnGroup, "SETTINGS", new Vector2(0, 20), () => { Debug.Log("Settings not yet implemented."); });
        _saveButton = CreateButton(btnGroup, "SAVE GAME", new Vector2(0, -40), () => { Debug.Log("Save not yet implemented."); });
        _exitButton = CreateButton(btnGroup, "EXIT GAME", new Vector2(0, -100), () => { QuitGame(); });
        
        // Link navigation manually to ensure controller D-Pad/Stick works reliably
        SetupNavigation(_resumeButton, _settingsButton, _exitButton);
        SetupNavigation(_settingsButton, _saveButton, _resumeButton);
        SetupNavigation(_saveButton, _exitButton, _settingsButton);
        SetupNavigation(_exitButton, _resumeButton, _saveButton);
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
        rt.sizeDelta = new Vector2(800, 300);

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
