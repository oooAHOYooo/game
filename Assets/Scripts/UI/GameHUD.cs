using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// GameHUD — cinematic runtime UI built entirely in code.
/// Displays: HP bars, Ki meters (with charge glow), wave counter, ghost badge,
/// wave banners, wave-clear fanfare, and intermission countdown.
/// </summary>
public class GameHUD : MonoBehaviour
{
    // ── References (found at runtime) ────────────────────────────────────
    private PlayerHealth[]     _players   = new PlayerHealth[2];
    private NinjaController[]  _ctrls     = new NinjaController[2];
    private Village            _village;

    // ── UI elements ───────────────────────────────────────────────────────
    // Player 1 (left side)
    private Image _p1HPFill;
    private Image _p1KiFill;
    private Text  _p1Label;

    // Player 2 (right side)
    private Image _p2HPFill;
    private Image _p2KiFill;
    private Text  _p2Label;
    private Text  _p2GhostBadge;

    // Wave info (centre top)
    private Text  _waveBanner;
    private Text  _intermissionText;

    // Village HP (centre top)
    private Image _villageHPFill;
    private Text  _villageLabel;

    // Divider line (split-screen centre)
    private Image _centreDivider;

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        BuildHUD();
        StartCoroutine(FindPlayersLoop());
    }

    IEnumerator FindPlayersLoop()
    {
        while (true)
        {
            var allPlayers = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
            foreach (var p in allPlayers)
                if (p.PlayerIndex < 2) _players[p.PlayerIndex] = p;

            var allCtrls = FindObjectsByType<NinjaController>(FindObjectsSortMode.None);
            foreach (var c in allCtrls)
                if (c.PlayerIndex < 2) _ctrls[c.PlayerIndex] = c;

            if (_village == null)
                _village = FindAnyObjectByType<Village>();

            yield return new WaitForSeconds(0.5f);
        }
    }

    void Update()
    {
        RefreshPlayerBars();
    }

    // ─────────────────────────────────────────────────────────────────────
    // BUILD UI
    // ─────────────────────────────────────────────────────────────────────
    void BuildHUD()
    {
        // ─── Player 1 panel (bottom-left) ────────────────────────────────
        var p1Panel = CreatePanel("P1Panel", new Vector2(0f, 0f), new Vector2(0.25f, 0.18f),
            GameBootstrapper.PaletteDeepNavy * 0.7f);

        _p1Label   = CreateLabel(p1Panel, "PLAYER 1", new Vector2(10f, -8f), 11, GameBootstrapper.PaletteGold);
        _p1HPFill  = CreateBar(p1Panel, "HP",  new Vector2(10f, -28f), GameBootstrapper.PaletteCrimson,  GameBootstrapper.PaletteGold, out _);
        _p1KiFill  = CreateBar(p1Panel, "KI",  new Vector2(10f, -50f), GameBootstrapper.PaletteCyan,     GameBootstrapper.PalettePurple, out _);

        // ─── Player 2 panel (bottom-right) ───────────────────────────────
        var p2Panel = CreatePanel("P2Panel", new Vector2(0.75f, 0f), new Vector2(1f, 0.18f),
            GameBootstrapper.PaletteDeepNavy * 0.7f);

        _p2Label      = CreateLabel(p2Panel, "PLAYER 2", new Vector2(10f, -8f),  11, GameBootstrapper.PaletteGhostBlue);
        _p2GhostBadge = CreateLabel(p2Panel, "[ GHOST AI ]", new Vector2(10f, -22f), 9, new Color(0.6f, 0.8f, 1f, 0.85f));
        _p2HPFill     = CreateBar(p2Panel, "HP", new Vector2(10f, -40f), GameBootstrapper.PaletteCrimson,  GameBootstrapper.PaletteGhostBlue, out _);
        _p2KiFill     = CreateBar(p2Panel, "KI", new Vector2(10f, -62f), GameBootstrapper.PaletteCyan,     GameBootstrapper.PalettePurple,    out _);

        // ─── Wave banner (centre top) ─────────────────────────────────────
        var bannerPanel = CreatePanel("BannerPanel", new Vector2(0.3f, 0.88f), new Vector2(0.7f, 1f),
            new Color(0f,0f,0f,0f));
        _waveBanner        = CreateLabel(bannerPanel, "",  new Vector2(0f, 0f), 22, GameBootstrapper.PaletteGold);
        _intermissionText  = CreateLabel(bannerPanel, "",  new Vector2(0f, -28f), 16, GameBootstrapper.PaletteCyan);

        // Centre text anchored
        var bRT = _waveBanner.GetComponent<RectTransform>();
        bRT.anchorMin = new Vector2(0f, 0f);
        bRT.anchorMax = new Vector2(1f, 1f);
        bRT.offsetMin = bRT.offsetMax = Vector2.zero;
        _waveBanner.alignment = TextAnchor.MiddleCenter;

        var iRT = _intermissionText.GetComponent<RectTransform>();
        iRT.anchorMin = new Vector2(0f, 0f);
        iRT.anchorMax = new Vector2(1f, 0.5f);
        iRT.offsetMin = iRT.offsetMax = Vector2.zero;
        _intermissionText.alignment = TextAnchor.MiddleCenter;

        // ─── Ki weapon transform charge indicator ─────────────────────────
        var xformPanel = CreatePanel("XformPanel", new Vector2(0.42f, 0f), new Vector2(0.58f, 0.025f),
            new Color(0f, 0f, 0f, 0.5f));

        // ─── Village HP bar (centre top, below banner) ────────────────────
        var villPanel = CreatePanel("VillagePanel", new Vector2(0.32f, 0.80f), new Vector2(0.68f, 0.87f),
            new Color(0.03f, 0.03f, 0.08f, 0.65f));
        _villageLabel  = CreateLabel(villPanel, "⛺  VILLAGE  ⛺", new Vector2(0f, -3f), 10, GameBootstrapper.PaletteGold);
        var vlrt = _villageLabel.GetComponent<RectTransform>();
        vlrt.anchorMin = new Vector2(0f, 0.5f);
        vlrt.anchorMax = new Vector2(1f, 1f);
        vlrt.offsetMin = vlrt.offsetMax = Vector2.zero;
        _villageLabel.alignment = TextAnchor.MiddleCenter;

        _villageHPFill = CreateBar(villPanel, "TOWN HP", new Vector2(10f, -20f),
            new Color(0.20f, 0.70f, 0.15f), GameBootstrapper.PaletteGold, out _);

        // ─── Centre divider (vertical line) ──────────────────────────────
        var dividerObj = new GameObject("CentreDivider");
        dividerObj.transform.SetParent(transform, false);
        _centreDivider = dividerObj.AddComponent<Image>();
        _centreDivider.color = new Color(0.5f, 0.5f, 0.8f, 0.35f);
        var dRT = dividerObj.GetComponent<RectTransform>();
        dRT.anchorMin = new Vector2(0.499f, 0f);
        dRT.anchorMax = new Vector2(0.501f, 1f);
        dRT.offsetMin = dRT.offsetMax = Vector2.zero;
    }

    // ─────────────────────────────────────────────────────────────────────
    // REFRESH BARS
    // ─────────────────────────────────────────────────────────────────────
    void RefreshPlayerBars()
    {
        // Player 1
        if (_players[0] != null)
        {
            float hpRatio = _players[0].CurrentHP / _players[0].MaxHP;
            SetBarFill(_p1HPFill, hpRatio);
        }
        if (_ctrls[0] != null)
        {
            float kiRatio = _ctrls[0].CurrentKi / _ctrls[0].KiMax;
            SetBarFill(_p1KiFill, kiRatio);
            PulseKiBar(_p1KiFill, _ctrls[0]);
        }

        // Player 2
        if (_players[1] != null)
        {
            float hpRatio = _players[1].CurrentHP / _players[1].MaxHP;
            SetBarFill(_p2HPFill, hpRatio);
        }
        if (_ctrls[1] != null)
        {
            float kiRatio = _ctrls[1].CurrentKi / _ctrls[1].KiMax;
            SetBarFill(_p2KiFill, kiRatio);
            PulseKiBar(_p2KiFill, _ctrls[1]);

            // Show/hide ghost badge
            if (_p2GhostBadge != null)
                _p2GhostBadge.enabled = _ctrls[1].IsGhost;
        }

        // Village HP
        if (_village != null && _villageHPFill != null)
        {
            SetBarFill(_villageHPFill, _village.GetHealthRatio());
            // Flash bar red when low
            if (_village.GetHealthRatio() < 0.3f)
            {
                float pulse = 0.6f + Mathf.Sin(Time.time * 6f) * 0.4f;
                _villageHPFill.color = new Color(0.9f, 0.1f, 0.1f, pulse);
            }
            else
            {
                _villageHPFill.color = new Color(0.20f, 0.70f, 0.15f, 1f);
            }
        }
    }

    void SetBarFill(Image bar, float ratio)
    {
        if (bar == null) return;
        bar.fillAmount = Mathf.Lerp(bar.fillAmount, ratio, 10f * Time.deltaTime);
    }

    void PulseKiBar(Image bar, NinjaController ctrl)
    {
        if (bar == null || ctrl == null) return;
        if (ctrl.IsChargingKi)
        {
            float pulse = 0.7f + Mathf.Sin(Time.time * 12f) * 0.3f;
            bar.color = new Color(bar.color.r, bar.color.g, bar.color.b, pulse);
        }
        else
        {
            bar.color = new Color(bar.color.r, bar.color.g, bar.color.b, 1f);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // PUBLIC API — called by WaveManager
    // ─────────────────────────────────────────────────────────────────────
    public void ShowWaveBanner(int waveNumber)
    {
        StartCoroutine(AnimateBanner($"— WAVE {waveNumber} —", GameBootstrapper.PaletteGold));
    }

    public void ShowWaveClear()
    {
        StartCoroutine(AnimateBanner("✦  WAVE CLEAR  ✦", GameBootstrapper.PaletteCyan));
    }

    public void ShowIntermission(int secondsLeft)
    {
        if (_intermissionText != null)
            _intermissionText.text = secondsLeft > 0
                ? $"Next wave in  {secondsLeft}..."
                : "";
    }

    IEnumerator AnimateBanner(string message, Color color)
    {
        if (_waveBanner == null) yield break;

        _waveBanner.text  = message;
        _waveBanner.color = new Color(color.r, color.g, color.b, 0f);

        // Fade in
        for (float t = 0; t < 0.4f; t += Time.deltaTime)
        {
            _waveBanner.color = new Color(color.r, color.g, color.b, t / 0.4f);
            // Subtle scale pulse via font size
            _waveBanner.fontSize = Mathf.RoundToInt(Mathf.Lerp(18, 24, t / 0.4f));
            yield return null;
        }

        _waveBanner.fontSize = 22;
        yield return new WaitForSeconds(2.2f);

        // Fade out
        for (float t = 0; t < 0.4f; t += Time.deltaTime)
        {
            _waveBanner.color = new Color(color.r, color.g, color.b, 1f - t / 0.4f);
            yield return null;
        }

        _waveBanner.text = "";
    }

    // ─────────────────────────────────────────────────────────────────────
    // UI FACTORY HELPERS
    // ─────────────────────────────────────────────────────────────────────
    GameObject CreatePanel(string name, Vector2 anchorMin, Vector2 anchorMax, Color bgColor)
    {
        var obj  = new GameObject(name);
        obj.transform.SetParent(transform, false);

        var img  = obj.AddComponent<Image>();
        img.color = bgColor;

        var rt   = obj.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = new Vector2(6f, 6f);
        rt.offsetMax = new Vector2(-6f, -6f);

        return obj;
    }

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
        rt.anchorMin      = new Vector2(0f, 1f);
        rt.anchorMax      = new Vector2(1f, 1f);
        rt.pivot          = new Vector2(0f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta      = new Vector2(-20f, fontSize + 6f);

        return t;
    }

    /// <summary>Creates a labelled fill bar and returns the fill image.</summary>
    Image CreateBar(GameObject parent, string label, Vector2 anchoredPos,
                    Color fillColor, Color labelColor, out Text labelText)
    {
        // Label
        var lbl = CreateLabel(parent, label, anchoredPos, 9, labelColor);
        labelText = lbl;

        // Background track
        var bgObj = new GameObject("Bar_BG_" + label);
        bgObj.transform.SetParent(parent.transform, false);
        var bgImg  = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.08f, 0.08f, 0.15f, 0.9f);

        var bgRT = bgObj.GetComponent<RectTransform>();
        bgRT.anchorMin      = new Vector2(0f, 1f);
        bgRT.anchorMax      = new Vector2(1f, 1f);
        bgRT.pivot          = new Vector2(0f, 1f);
        bgRT.anchoredPosition = new Vector2(10f, anchoredPos.y - 13f);
        bgRT.sizeDelta      = new Vector2(-20f, 10f);

        // Fill
        var fillObj = new GameObject("Bar_Fill_" + label);
        fillObj.transform.SetParent(bgObj.transform, false);
        var fillImg = fillObj.AddComponent<Image>();
        fillImg.color      = fillColor;
        fillImg.type       = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 1f;

        var fillRT = fillObj.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = new Vector2(1f, 1f);
        fillRT.offsetMax = new Vector2(-1f, -1f);

        return fillImg;
    }
}
