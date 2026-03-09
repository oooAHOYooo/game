using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// GameHUD — cinematic runtime UI built entirely in code.
/// Displays: Heart-based HP (Super Mario 64 style), Ki meters (with charge glow),
/// wave counter, ghost badge, wave banners, wave-clear fanfare, and intermission countdown.
/// </summary>
public class GameHUD : MonoBehaviour
{
    // ── References (found at runtime) ────────────────────────────────────
    private PlayerHealth[]     _players   = new PlayerHealth[2];
    private NinjaController[]  _ctrls     = new NinjaController[2];
    private Village            _village;

    // ── Heart config ──────────────────────────────────────────────────────
    private const float HP_PER_HEART    = 20f;  // each heart = 20 HP
    private const int   MAX_HEARTS      = 5;    // max hearts displayed per player
    private const int   VILLAGE_HEARTS  = 10;   // village hearts

    // ── UI elements ───────────────────────────────────────────────────────
    // Player 1 (left side)
    private List<Image> _p1Hearts = new List<Image>();
    private Image _p1KiFill;
    private Text  _p1Label;
    private Text  _p1Combo;

    // Player 2 (right side)
    private List<Image> _p2Hearts = new List<Image>();
    private Image _p2KiFill;
    private Text  _p2Label;
    private Text  _p2GhostBadge;
    private Text  _p2Combo;

    // Wave info (centre top)
    private Text  _waveBanner;
    private Text  _intermissionText;

    // Village HP (centre top)
    private List<Image> _villageHearts = new List<Image>();
    private Text  _villageLabel;

    // Ball Score
    private Text _ballScore;
    private static readonly Color BallScoreColor = new Color(0.1f, 0.9f, 1.0f); // Divine Cyan

    // Divider line (split-screen centre)
    private Image _centreDivider;


    // Moves list (top-left)
    private Text  _movesList;

    // Heart colors
    private static readonly Color HeartFull  = new Color(1f, 0.15f, 0.25f);
    private static readonly Color HeartHalf  = new Color(1f, 0.5f, 0.55f);
    private static readonly Color HeartEmpty = new Color(0.2f, 0.15f, 0.18f, 0.6f);
    private static readonly Color VillageHeartFull  = new Color(0.2f, 0.85f, 0.3f);
    private static readonly Color VillageHeartEmpty = new Color(0.12f, 0.18f, 0.12f, 0.5f);

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        BuildHUD();
        UpdateMovesList();
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
        BuildHeartRow(p1Panel, _p1Hearts, MAX_HEARTS, new Vector2(10f, -28f), HeartFull);
        _p1KiFill  = CreateBar(p1Panel, "KI",  new Vector2(10f, -54f), GameBootstrapper.PaletteCyan, GameBootstrapper.PalettePurple, out _);
        _p1Combo   = CreateLabel(p1Panel, "", new Vector2(10f, -80f), 12, GameBootstrapper.PaletteGold);

        // ─── Player 2 panel (bottom-right) ───────────────────────────────
        var p2Panel = CreatePanel("P2Panel", new Vector2(0.75f, 0f), new Vector2(1f, 0.18f),
            GameBootstrapper.PaletteDeepNavy * 0.7f);

        _p2Label      = CreateLabel(p2Panel, "PLAYER 2", new Vector2(10f, -8f),  11, GameBootstrapper.PaletteGhostBlue);
        _p2GhostBadge = CreateLabel(p2Panel, "[ GHOST AI ]", new Vector2(10f, -22f), 9, new Color(0.6f, 0.8f, 1f, 0.85f));
        BuildHeartRow(p2Panel, _p2Hearts, MAX_HEARTS, new Vector2(10f, -40f), HeartFull);
        _p2KiFill     = CreateBar(p2Panel, "KI", new Vector2(10f, -66f), GameBootstrapper.PaletteCyan, GameBootstrapper.PalettePurple, out _);
        _p2Combo      = CreateLabel(p2Panel, "", new Vector2(10f, -92f), 12, GameBootstrapper.PaletteGold);

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

        // ─── Village HP hearts (centre top, below banner) ─────────────────
        var villPanel = CreatePanel("VillagePanel", new Vector2(0.30f, 0.78f), new Vector2(0.70f, 0.87f),
            new Color(0.03f, 0.03f, 0.08f, 0.65f));
        _villageLabel  = CreateLabel(villPanel, "🏘  VILLAGE  🏘", new Vector2(0f, -3f), 10, GameBootstrapper.PaletteGold);
        var vlrt = _villageLabel.GetComponent<RectTransform>();
        vlrt.anchorMin = new Vector2(0f, 0.55f);
        vlrt.anchorMax = new Vector2(1f, 1f);
        vlrt.offsetMin = vlrt.offsetMax = Vector2.zero;
        _villageLabel.alignment = TextAnchor.MiddleCenter;

        BuildHeartRow(villPanel, _villageHearts, VILLAGE_HEARTS, new Vector2(10f, -22f), VillageHeartFull, true);

        // ─── Ball Score (Central Highlight) ──────────────────────────────
        _ballScore = CreateLabel(villPanel, "SAVED: 0", new Vector2(0f, -48f), 18, BallScoreColor);
        var bsrt = _ballScore.GetComponent<RectTransform>();
        bsrt.anchorMin = new Vector2(0f, 0f);
        bsrt.anchorMax = new Vector2(1f, 0.5f);
        bsrt.offsetMin = bsrt.offsetMax = Vector2.zero;
        _ballScore.alignment = TextAnchor.MiddleCenter;

        // ─── Centre divider (vertical line) ──────────────────────────────
        var dividerObj = new GameObject("CentreDivider");
        dividerObj.transform.SetParent(transform, false);
        _centreDivider = dividerObj.AddComponent<Image>();
        _centreDivider.color = new Color(0.5f, 0.5f, 0.8f, 0.35f);
        var dRT = dividerObj.GetComponent<RectTransform>();
        dRT.anchorMin = new Vector2(0.499f, 0f);
        dRT.anchorMax = new Vector2(0.501f, 1f);
        dRT.offsetMin = dRT.offsetMax = Vector2.zero;

        // ─── Moves list (top-left, Minecraft-style) ─────────────────────────
        var movesPanel = CreatePanel("MovesPanel", new Vector2(0f, 0.82f), new Vector2(0.25f, 1f),
            new Color(0f, 0f, 0f, 0.65f));

        _movesList = CreateLabel(movesPanel, "", new Vector2(10f, -8f), 8, new Color(0.7f, 0.9f, 1f, 0.95f));
        var mRT = _movesList.GetComponent<RectTransform>();
        mRT.anchorMin = new Vector2(0f, 1f);
        mRT.anchorMax = new Vector2(1f, 1f);
        mRT.pivot = new Vector2(0f, 1f);
        mRT.anchoredPosition = new Vector2(10f, -8f);
        mRT.sizeDelta = new Vector2(-20f, 200f);
        _movesList.alignment = TextAnchor.UpperLeft;
        _movesList.horizontalOverflow = HorizontalWrapMode.Wrap;
        _movesList.verticalOverflow = VerticalWrapMode.Overflow;
    }

    // ─────────────────────────────────────────────────────────────────────
    // HEART ROW BUILDER
    // ─────────────────────────────────────────────────────────────────────
    void BuildHeartRow(GameObject parent, List<Image> heartList, int count, Vector2 anchoredPos, Color fullColor, bool centred = false)
    {
        var rowObj = new GameObject("HeartRow");
        rowObj.transform.SetParent(parent.transform, false);

        var rowRT = rowObj.AddComponent<RectTransform>();
        rowRT.anchorMin = new Vector2(0f, 1f);
        rowRT.anchorMax = new Vector2(1f, 1f);
        rowRT.pivot = new Vector2(centred ? 0.5f : 0f, 1f);
        rowRT.anchoredPosition = centred ? new Vector2(0f, anchoredPos.y) : anchoredPos;
        rowRT.sizeDelta = new Vector2(-20f, 22f);

        if (centred)
        {
            rowRT.anchorMin = new Vector2(0.5f, 1f);
            rowRT.anchorMax = new Vector2(0.5f, 1f);
        }

        float heartSize = 18f;
        float spacing = 2f;
        float totalWidth = count * heartSize + (count - 1) * spacing;
        float startX = centred ? -totalWidth * 0.5f : 0f;

        for (int i = 0; i < count; i++)
        {
            var heartObj = new GameObject("Heart_" + i);
            heartObj.transform.SetParent(rowObj.transform, false);

            var heartImg = heartObj.AddComponent<Image>();
            heartImg.color = fullColor;

            var hRT = heartObj.GetComponent<RectTransform>();
            hRT.anchorMin = new Vector2(0f, 0.5f);
            hRT.anchorMax = new Vector2(0f, 0.5f);
            hRT.pivot = new Vector2(0f, 0.5f);
            hRT.anchoredPosition = new Vector2(startX + i * (heartSize + spacing), 0f);
            hRT.sizeDelta = new Vector2(heartSize, heartSize);

            heartList.Add(heartImg);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // REFRESH HEARTS & BARS
    // ─────────────────────────────────────────────────────────────────────
    void RefreshPlayerBars()
    {
        // Player 1 hearts
        if (_players[0] != null)
            UpdateHearts(_p1Hearts, _players[0].CurrentHP, _players[0].MaxHP, HeartFull, HeartHalf, HeartEmpty);

        if (_ctrls[0] != null)
        {
            float kiRatio = _ctrls[0].CurrentKi / _ctrls[0].KiMax;
            SetBarFill(_p1KiFill, kiRatio);
            PulseKiBar(_p1KiFill, _ctrls[0]);
        }

        // Player 2 hearts
        if (_players[1] != null)
            UpdateHearts(_p2Hearts, _players[1].CurrentHP, _players[1].MaxHP, HeartFull, HeartHalf, HeartEmpty);

        if (_ctrls[1] != null)
        {
            float kiRatio = _ctrls[1].CurrentKi / _ctrls[1].KiMax;
            SetBarFill(_p2KiFill, kiRatio);
            PulseKiBar(_p2KiFill, _ctrls[1]);

            // Show/hide ghost badge
            if (_p2GhostBadge != null)
                _p2GhostBadge.enabled = _ctrls[1].IsGhost;
        }

        UpdateComboCount(_p1Combo, 0);
        UpdateComboCount(_p2Combo, 1);

        // Village hearts
        if (_village != null)
            UpdateHearts(_villageHearts, _village.CurrentHP, _village.MaxHP, VillageHeartFull, VillageHeartFull * 0.7f, VillageHeartEmpty);

        // Ball Score
        if (_ballScore != null && BallGoal.Instance != null)
        {
            _ballScore.text = $"ORBS SAVED: {BallGoal.Instance.Score}";
        }
    }


    void UpdateHearts(List<Image> hearts, float currentHP, float maxHP, Color fullColor, Color halfColor, Color emptyColor)
    {
        if (hearts == null || hearts.Count == 0) return;

        int totalHearts = hearts.Count;
        float hpPerHeart = maxHP / totalHearts;

        for (int i = 0; i < totalHearts; i++)
        {
            if (hearts[i] == null) continue;

            float heartStart = i * hpPerHeart;
            float heartEnd   = (i + 1) * hpPerHeart;
            float heartMid   = heartStart + hpPerHeart * 0.5f;

            if (currentHP >= heartEnd)
            {
                // Full heart
                hearts[i].color = fullColor;
                hearts[i].transform.localScale = Vector3.one;
            }
            else if (currentHP >= heartMid)
            {
                // Half heart
                hearts[i].color = halfColor;
                hearts[i].transform.localScale = Vector3.one * 0.85f;
            }
            else if (currentHP > heartStart)
            {
                // Quarter heart — small and fading
                hearts[i].color = Color.Lerp(emptyColor, halfColor, 0.4f);
                hearts[i].transform.localScale = Vector3.one * 0.7f;
            }
            else
            {
                // Empty heart
                hearts[i].color = emptyColor;
                hearts[i].transform.localScale = Vector3.one * 0.6f;
            }
        }

        // Pulse the last active heart for drama
        int lastActiveHeart = Mathf.FloorToInt(currentHP / hpPerHeart);
        if (lastActiveHeart >= 0 && lastActiveHeart < totalHearts && currentHP > 0 && currentHP < maxHP * 0.3f)
        {
            float pulse = 0.85f + Mathf.Sin(Time.time * 8f) * 0.15f;
            hearts[Mathf.Min(lastActiveHeart, totalHearts - 1)].transform.localScale = Vector3.one * pulse;
        }
    }

    void SetBarFill(Image bar, float ratio)
    {
        if (bar == null) return;
        bar.fillAmount = Mathf.Lerp(bar.fillAmount, ratio, 10f * Time.deltaTime);
    }

    void UpdateComboCount(Text comboText, int playerIndex)
    {
        if (comboText == null || ComboSystem.Instance == null) return;
        
        int combo = ComboSystem.Instance.GetComboCount(playerIndex);
        if (combo > 1)
        {
            float multi = ComboSystem.Instance.GetMultiplier(playerIndex);
            comboText.text = $"COMBO {combo}  (x{multi:F1} KI)";
            comboText.color = multi >= GameSettings.ComboTier3Multiplier ? GameBootstrapper.PaletteMagenta : 
                              multi >= GameSettings.ComboTier2Multiplier ? GameBootstrapper.PaletteCyan : GameBootstrapper.PaletteGold;
        }
        else
        {
            comboText.text = "";
        }
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
    // INTRO CINEMATIC TEXT
    // ─────────────────────────────────────────────────────────────────────
    public void ShowIntroText()
    {
        StartCoroutine(AnimateIntroText());
    }

    IEnumerator AnimateIntroText()
    {
        var panel = CreatePanel("IntroPanel", Vector2.zero, Vector2.one, new Color(0, 0, 0, 0));
        var text = CreateLabel(panel, "", Vector2.zero, 48, GameBootstrapper.PaletteGold);
        text.alignment = TextAnchor.MiddleCenter;
        
        // Remove background image from panel to make it just text
        Destroy(panel.GetComponent<Image>());

        string[] lines = new string[] {
            "THE TRIBES OF THE ISLAND HAVE CALLED UPON YOU.\n\n",
            "THE OCEAN CORRUPTION APPROACHES.\n\n",
            "PROTECT YOUR WORSHIPPERS.\nUNLEASH YOUR KI.\n\n",
            "BECOME THEIR GODS."
        };

        string currentText = "";
        
        foreach (var line in lines)
        {
            currentText += line;
            text.text = currentText;
            text.color = new Color(text.color.r, text.color.g, text.color.b, 0f);

            // Fade in the whole block (quick fade)
            for (float t = 0; t < 0.6f; t += Time.deltaTime)
            {
                text.color = new Color(text.color.r, text.color.g, text.color.b, t / 0.6f);
                yield return null;
            }
            
            text.color = new Color(text.color.r, text.color.g, text.color.b, 1f);
            yield return new WaitForSeconds(1.2f);
        }

        yield return new WaitForSeconds(2.0f);

        // Fade out
        for (float t = 0; t < 1.5f; t += Time.deltaTime)
        {
            text.color = new Color(text.color.r, text.color.g, text.color.b, 1f - (t / 1.5f));
            yield return null;
        }

        Destroy(panel);
    }

    // ─────────────────────────────────────────────────────────────────────
    // MOVES LIST
    // ─────────────────────────────────────────────────────────────────────
    void UpdateMovesList()
    {
        if (_movesList == null) return;

        // Detect if using gamepad
        bool hasGamepad = UnityEngine.InputSystem.Gamepad.all.Count > 0;

        if (hasGamepad)
        {
            _movesList.text = "CONTROLS\n\n" +
                "<b>MOVE</b>\nLeft Stick\n\n" +
                "<b>JUMP</b>\nSouth Btn\n\n" +
                "<b>FLY</b>\nRight Stick Y\n\n" +
                "<b>ATTACK</b>\nRB / RT\n\n" +
                "<b>HEAVY</b>\nRT (hold)\n\n" +
                "<b>DODGE</b>\nEast Btn\n\n" +
                "<b>KI BEAM</b>\nLT (hold)\n\n" +
                "<b>BLOCK</b>\nLB\n\n" +
                "<b>WEAPON</b>\nLB (hold 1s)\n\n" +
                "<b>LOCK-ON</b>\nR-Stick Btn";
        }
        else
        {
            _movesList.text = "CONTROLS\n\n" +
                "<b>MOVE</b>\nWASD\n\n" +
                "<b>FLY</b>\nE/Q\n\n" +
                "<b>JUMP</b>\nSPACE\n\n" +
                "<b>ATTACK</b>\nJ\n\n" +
                "<b>HEAVY</b>\nK (hold)\n\n" +
                "<b>DODGE</b>\nSHIFT\n\n" +
                "<b>KI BEAM</b>\nL (hold)\n\n" +
                "<b>BLOCK</b>\nI\n\n" +
                "<b>WEAPON</b>\nU (hold 1s)\n\n" +
                "<b>LOCK-ON</b>\nF";
        }
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
