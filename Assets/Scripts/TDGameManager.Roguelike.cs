using System.Collections.Generic;
using UnityEngine;

// Roguelike upgrade offers (issue #8): after clearing boss waves 5/15 (commons),
// 25/35 (rares) and 45 (one epic), the player picks a run-scoped bonus. Picks
// are per-player: in multiplayer the cleared report is deferred until the pick
// resolves, so other boards are unaffected. A 15s timer freezes the next wave;
// expiry auto-picks at random.
public partial class TDGameManager : MonoBehaviour
{
    private bool rogueOpen;
    private float rogueTimer;
    private List<RogueDef> rogueOffered;
    private bool roguePendingMP;
    private float rogueComboTimer;

    const float RoguePickSeconds = 15f;

    static bool RogueOfferFor(int wave, out RogueDef[] pool, out int count)
    {
        pool = null;
        count = 0;
        if (wave == 5 || wave == 15) { pool = RogueUpgrades.Commons; count = 3; return true; }
        if (wave == 25 || wave == 35) { pool = RogueUpgrades.Rares; count = 3; return true; }
        if (wave == 45) { pool = RogueUpgrades.Epics; count = 1; return true; }
        return false;
    }

    /// <summary>Shared end-of-wave bookkeeping for rogue mods (both branches).</summary>
    void RogueOnWaveCleared()
    {
        RogueMods.WavesCleared++;
        if (RogueMods.Wage > 0)
        {
            Money += RogueMods.Wage;
            message += "  +$" + RogueMods.Wage + " allowance";
        }
    }

    void OpenRogueOffer(RogueDef[] pool, int count, bool isMP)
    {
        rogueOffered = RogueUpgrades.Offer(pool, count);
        rogueOpen = true;
        roguePendingMP = isMP;
        rogueTimer = RoguePickSeconds;
        HideHover();
        if (TDAudio.Instance != null) TDAudio.Instance.RoundClear();
    }

    void TickRogueOffer()
    {
        rogueTimer -= Time.deltaTime;
        if (rogueTimer <= 0f && rogueOffered != null && rogueOffered.Count > 0)
            ResolveRoguePick(rogueOffered[Random.Range(0, rogueOffered.Count)]);
    }

    void ResolveRoguePick(RogueDef def)
    {
        ApplyRogue(def);
        rogueOpen = false;
        rogueOffered = null;

        if (roguePendingMP)
        {
            cleared = true;
            Round = RoundState.Preparing;
            prepTimer = 0f;
            if (MatchSync.Instance != null)
                MatchSync.Instance.ReportLocal(Lives, Money, Wave, true, eliminated);
            return;
        }

        Wave++;
        if (Wave > TDBalance.TotalWaves)
        {
            RestoreTimeScale();
            State = GameState.Victory;
            return;
        }
        BeginWave();   // no prep wait: the next wave starts the moment the pick lands
    }

    void ApplyRogue(RogueDef def)
    {
        RogueMods.Owned.Add(def.name);
        switch (def.id)
        {
            case "heavy": RogueMods.Damage *= 1.10f; break;
            case "sugar": RogueMods.Rate *= 1.10f; break;
            case "straws": RogueMods.Range *= 1.12f; break;
            case "salt": RogueMods.SlowStrength *= 1.15f; break;
            case "sweet": Money += 200; break;
            case "merger": RogueMods.MergeMult = 0.5f; break;
            case "allow": RogueMods.Wage += 40; break;
            case "overclock": RogueMods.Rate *= 1.25f; RogueMods.Range *= 0.9f; break;
            case "lucky": RogueMods.CritChance += 0.08f; break;
            case "caramel": RogueMods.Caramelized = true; break;
            case "happy": RogueMods.HappyHour = true; break;
            case "dessert": break;   // passive: +1%/cleared wave, read live
            case "scoop": RogueMods.DoubleScoop = true; break;
            case "sour": RogueMods.SourPower = true; break;
            case "combo": RogueMods.ComboMeal = true; break;
            case "artillery": RogueMods.Artillery = true; break;
            case "overdrive": RogueMods.Overdrive = true; break;
            case "slayer": RogueMods.GiantSlayer = true; break;
        }
        RefreshRogueCensus();
        message = def.name + " claimed! " + def.blurb;
        messageTimer = 3f;
        if (TDAudio.Instance != null) TDAudio.Instance.Merge();
    }

    /// <summary>Recounts distinct type+tier pairs (Combo Meal) and the top tier
    /// (Giant Slayer) from the live towers.</summary>
    void RefreshRogueCensus()
    {
        List<int> seen = new List<int>();
        int top = 0;
        foreach (Tower t in towers.Values)
        {
            if (t == null) continue;
            if (t.Tier > top) top = t.Tier;
            int key = ((int)t.Type) * 16 + t.Tier;
            if (!seen.Contains(key)) seen.Add(key);
        }
        RogueMods.MaxTier = top;
        RogueMods.ComboMult = 1f + 0.02f * seen.Count;
    }

    void DrawRogueModal()
    {
        Color old = GUI.color;
        GUI.color = new Color(0.05f, 0.06f, 0.08f, 0.82f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = old;

        bool single = rogueOffered != null && rogueOffered.Count == 1;
        if (rogueOffered == null || rogueOffered.Count == 0) return;
        float cw = single ? 420f : 200f, ch = single ? 120f : 150f, gap = 14f;
        float w = rogueOffered.Count * cw + (rogueOffered.Count - 1) * gap;
        float h = 100f + ch + 24f;
        float x0 = (Screen.width - w) * 0.5f;
        float y0 = (Screen.height - h) * 0.5f;
        float cardsY = y0 + 100f;
        DrawPanel(new Rect(x0 - 24f, y0 - 24f, w + 48f, h + 48f));

        GUI.Label(new Rect(x0, y0, w, 40f), single ? "AN EPIC UPGRADE AWAITS" : "WAVE CLEARED - CHOOSE AN UPGRADE",
            Style(26, TextAnchor.MiddleCenter, new Color(1f, 0.88f, 0.45f)));
        GUI.Label(new Rect(x0, y0 + 40f, w, 24f),
            "Auto-picks in " + Mathf.CeilToInt(Mathf.Max(0f, rogueTimer)) + "s - next wave waits",
            Style(14, TextAnchor.MiddleCenter, new Color(0.75f, 0.78f, 0.84f)));
        if (RogueMods.Owned.Count > 0)
        {
            GUI.Label(new Rect(x0, y0 + 64f, w, 22f), "Owned: " + string.Join(", ", RogueMods.Owned.ToArray()),
                Style(12, TextAnchor.MiddleCenter, new Color(0.55f, 0.9f, 0.6f)));
        }

        GUIStyle btn = PaperButton(15);
        for (int i = 0; i < rogueOffered.Count; i++)
        {
            RogueDef def = rogueOffered[i];
            Rect r = new Rect(x0 + i * (cw + gap), cardsY, cw, ch);
            string label = def.name + "\n" + def.blurb + (single ? "\n(click to claim)" : "");
            Color prev = GUI.color;
            GUI.color = RogueUpgrades.TierColor(def.tier);
            bool hit = GUI.Button(r, label, btn);
            GUI.color = prev;
            if (hit) { Click(); ResolveRoguePick(def); return; }
        }
    }
}
