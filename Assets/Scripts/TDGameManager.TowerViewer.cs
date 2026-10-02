using UnityEngine;

// Main-menu tower gallery: a rotating 3D preview flanked by arrows, with the
// tower's name, per-tier stats and a short description of its mechanic.
public partial class TDGameManager
{
    void OpenTowerViewer()
    {
        TowerViewer.Ensure().Open();
        State = GameState.TowerViewer;
    }

    void CloseTowerViewer()
    {
        if (TowerViewer.Instance != null) TowerViewer.Instance.Close();
        State = GameState.MainMenu;
    }

    void DrawTowerViewer()
    {
        TowerViewer v = TowerViewer.Ensure();
        if (!v.IsOpen) v.Open();

        TowerType type = v.CurrentType;
        TowerDef def = TowerCatalog.Get(type);

        // backdrop
        Color old = GUI.color;
        GUI.color = new Color(0.10f, 0.11f, 0.12f, 0.96f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = old;

        GUI.Label(new Rect(0f, Screen.height * 0.04f, Screen.width, 44f), "TOWER VIEWER",
            Style(34, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.4f)));

        float size = Mathf.Clamp(Screen.height * 0.42f, 220f, 430f);
        float cx = Screen.width * 0.5f;
        float top = Screen.height * 0.135f;
        Rect panel = new Rect(cx - size * 0.5f, top, size, size);

        GUI.Label(new Rect(cx - 320f, top - 48f, 640f, 38f), def.displayName,
            Style(28, TextAnchor.MiddleCenter, Color.white));

        GUI.DrawTexture(panel, v.Texture, ScaleMode.ScaleToFit, false);

        // arrows, vertically centred on the 3D panel
        GUIStyle arrow = PaperButton(28);
        float ay = panel.y + panel.height * 0.5f - 50f;
        if (GUI.Button(new Rect(panel.x - 92f, ay, 72f, 100f), "<", arrow))
        {
            Click();
            v.Next(-1);
        }
        if (GUI.Button(new Rect(panel.xMax + 20f, ay, 72f, 100f), ">", arrow))
        {
            Click();
            v.Next(1);
        }

        DrawTowerStats(def, new Rect(cx - size * 0.5f, panel.yMax + 16f, size, 200f));

        if (Event.current.type == EventType.KeyDown)
        {
            if (Event.current.keyCode == KeyCode.LeftArrow) { Click(); v.Next(-1); }
            else if (Event.current.keyCode == KeyCode.RightArrow) { Click(); v.Next(1); }
            else if (Event.current.keyCode == KeyCode.Escape) CloseTowerViewer();
        }

        float bw = 200f, bh = 46f;
        if (GUI.Button(new Rect(cx - bw * 0.5f, Screen.height - 72f, bw, bh), "Back", PaperButton(20)))
        {
            Click();
            CloseTowerViewer();
        }

        GUI.Label(new Rect(0f, Screen.height - 24f, Screen.width, 20f),
            "Left / Right to browse   -   Esc to go back",
            Style(13, TextAnchor.MiddleCenter, new Color(0.7f, 0.73f, 0.78f)));
    }

    void DrawTowerStats(TowerDef def, Rect area)
    {
        bool t7 = TowerCatalog.IsT7Type(def.type);
        int cols = t7 ? 1 : def.tiers.Count;   // 6 for the random types, 4 for Gold, 1 for a T7 fusion
        int last = def.tiers.Count - 1;
        float rowH = 26f;
        float labelW = area.width * 0.22f;
        float colW = (area.width - labelW) / cols;

        int fs = cols > 4 ? 12 : 15;                // keep six columns legible
        GUIStyle head = Style(fs, TextAnchor.MiddleCenter, new Color(0.65f, 0.85f, 1f));
        GUIStyle lab = Style(fs, TextAnchor.MiddleLeft, new Color(0.78f, 0.80f, 0.84f));
        GUIStyle val = Style(fs, TextAnchor.MiddleCenter, Color.white);

        for (int t = 0; t < cols; t++)
            GUI.Label(new Rect(area.x + labelW + t * colW, area.y, colW, rowH),
                "T" + (t7 ? TowerCatalog.MaxTier : (t + 1)), head);

        string[] rows = { "Damage", "Range", "Rate", "Special" };
        for (int r = 0; r < rows.Length; r++)
        {
            float y = area.y + (r + 1) * rowH;
            GUI.Label(new Rect(area.x, y, labelW, rowH), rows[r], lab);
            for (int t = 0; t < cols; t++)
            {
                TowerTierStats s = def.tiers[t7 ? last : t];
                GUI.Label(new Rect(area.x + labelW + t * colW, y, colW, rowH), StatText(def.type, r, s), val);
            }
        }

        GUI.Label(new Rect(area.x - 40f, area.y + 5f * rowH + 6f, area.width + 80f, 46f), Blurb(def.type),
            Style(14, TextAnchor.UpperCenter, new Color(0.85f, 0.88f, 0.92f)));

        // tier 5/6 modifiers, or the tier 7 fusion tag (word-wrapped, full text)
        string mod7 = TowerCatalog.ModifierText(def.type, 7);
        string mod5 = TowerCatalog.ModifierText(def.type, 5);
        string mod6 = TowerCatalog.ModifierText(def.type, 6);
        GUIStyle mod7Style = Style(14, TextAnchor.UpperCenter, new Color(1f, 0.92f, 0.55f));
        mod7Style.wordWrap = true;
        // Preview is at T1 (T7 for fusions), so bonuses show greyed out here.
        Color lockedMod = new Color(0.55f, 0.58f, 0.63f);
        GUIStyle mod5Style = Style(14, TextAnchor.UpperCenter, lockedMod);
        mod5Style.wordWrap = true;
        GUIStyle mod6Style = Style(14, TextAnchor.UpperCenter, lockedMod);
        mod6Style.wordWrap = true;
        float my0 = area.y + 5f * rowH + 54f;
        if (mod7 != null)
        {
            GUI.Label(new Rect(area.x - 40f, my0, area.width + 80f, 34f), "T7   " + mod7, mod7Style);
        }
        else if (mod5 != null)
        {
            GUI.Label(new Rect(area.x - 40f, my0, area.width + 80f, 28f), "T5   " + mod5, mod5Style);
            if (mod6 != null)
                GUI.Label(new Rect(area.x - 40f, my0 + 30f, area.width + 80f, 28f), "T6   " + mod6, mod6Style);
        }
        else
        {
            GUIStyle noModStyle = Style(14, TextAnchor.UpperCenter, new Color(0.75f, 0.75f, 0.78f));
            noModStyle.wordWrap = true;
            GUI.Label(new Rect(area.x - 40f, my0, area.width + 80f, 34f), "No tier 5/6 modifiers" +
                (def.type == TowerType.Gold ? " (Gold max tier 4)" : ""), noModStyle);
        }
    }

    static string StatText(TowerType t, int row, TowerTierStats s)
    {
        switch (row)
        {
            case 0: return s.damage > 0 ? s.damage.ToString() : "none";
            case 1: return s.range.ToString("0.#");
            case 2: return s.fireInterval.ToString("0.00") + "s";
            default:
                switch (t)
                {
                    case TowerType.SingleShot:
                        if (s.multiShot > 1) return "x" + s.multiShot + " pel";
                        if (s.bounceCount > 0) return "bnc " + s.bounceCount;
                        return "-";
                    case TowerType.Splash: return "spl " + s.splashRadius.ToString("0.#");
                    case TowerType.Slow: return "x" + s.multiShot + " " + Mathf.RoundToInt(s.slowFactor * 100f) + "%";
                    case TowerType.Sniper: return SniperSpecial(s);
                    case TowerType.Chain: return "jmp " + s.chainCount;
                    case TowerType.Pierce: return "prc " + s.pierceCount;
                    case TowerType.Poison: return s.poisonDps.ToString("0.#") + "dps";
                    case TowerType.Gold: return "+$" + s.goldPerHit;
                    case TowerType.FondueFountain: return "dip +" + Mathf.RoundToInt(s.dippedBonus * 100f) + "% x" + s.dippedMaxStacks;
                    case TowerType.IceCreamTruck: return Mathf.RoundToInt(s.stunChance * 100f) + "% stun";
                    case TowerType.BobaBlaster: return s.rateMinInterval > 0f ? ("-> " + s.rateMinInterval.ToString("0.00") + "s") : "-";
                    case TowerType.PizzaOven: return s.zoneDps.ToString("0.#") + " dps";
                    default: return "-";
                }
        }
    }

    static string SniperSpecial(TowerTierStats s)
    {
        if (s.deadeyeRamp > 0f) return "deadeye";
        if (s.critChance > 0f) return Mathf.RoundToInt(s.critChance * 100f) + "% cr";
        return "-";
    }

    static string Blurb(TowerType t)
    {
        switch (t)
        {
            case TowerType.SingleShot: return "Cheap, fast single-target shots - the reliable backbone.";
            case TowerType.Splash: return "Lobs area damage; strong against tight groups.";
            case TowerType.Slow: return "Deals no damage - snares enemies so other towers can finish them.";
            case TowerType.Sniper: return "Huge hits at very long range, but slow to fire.";
            case TowerType.Chain: return "Damage arcs on to nearby enemies after the first hit.";
            case TowerType.Pierce: return "Fires a line that skewers every enemy it passes through.";
            case TowerType.Poison: return "Applies damage over time that keeps ticking.";
            case TowerType.Gold: return "Deals no damage - pays out gold on every confirmed hit (max 4 per board).";
            case TowerType.FondueFountain: return "Tier 7 fusion. Heavy molten-chocolate beam that stacks +damage taken (max 5).";
            case TowerType.IceCreamTruck: return "Tier 7 fusion. Splash cones that can stun everything caught (20%).";
            case TowerType.BobaBlaster: return "Tier 7 fusion. Single-target DPS that ramps its fire rate on one target.";
            case TowerType.PizzaOven: return "Tier 7 fusion. Lands a hit and leaves a 5s damaging pizza zone.";
            default: return "";
        }
    }
}
