using System.Collections.Generic;
using UnityEngine;

// Main-menu mob gallery: a rotating 3D preview flanked by arrows, with the
// mob's name, which wave it appears on, its stats and any boss traits.
public partial class TDGameManager
{
    void OpenMobViewer()
    {
        MobViewer.Ensure().Open();
        State = GameState.MobViewer;
    }

    void CloseMobViewer()
    {
        if (MobViewer.Instance != null) MobViewer.Instance.Close();
        State = GameState.MainMenu;
    }

    void DrawMobViewer()
    {
        MobViewer v = MobViewer.Ensure();
        if (!v.IsOpen) v.Open();

        MobDef def = v.Current;

        Color old = GUI.color;
        GUI.color = new Color(0.10f, 0.11f, 0.12f, 0.96f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = old;

        GUI.Label(new Rect(0f, Screen.height * 0.04f, Screen.width, 44f), "MOB VIEWER",
            Style(34, TextAnchor.MiddleCenter, new Color(0.6f, 0.9f, 0.5f)));

        float size = Mathf.Clamp(Screen.height * 0.42f, 220f, 430f);
        float cx = Screen.width * 0.5f;
        float top = Screen.height * 0.135f;
        Rect panel = new Rect(cx - size * 0.5f, top, size, size);

        GUI.Label(new Rect(cx - 340f, top - 48f, 680f, 38f), def.displayName,
            Style(28, TextAnchor.MiddleCenter, Color.white));

        GUI.DrawTexture(panel, v.Texture, ScaleMode.ScaleToFit, false);

        float ay = panel.y + panel.height * 0.5f - 50f;
        GUIStyle arrow = PaperButton(28);
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

        DrawMobStats(def, new Rect(cx - size * 0.62f, panel.yMax + 16f, size * 1.24f, 200f));

        GUI.Label(new Rect(0f, panel.y - 84f, Screen.width, 24f),
            (v.Index + 1) + " / " + MobCatalog.All.Length + "   -   " + def.archetype,
            Style(15, TextAnchor.MiddleCenter, new Color(0.7f, 0.75f, 0.8f)));

        if (Event.current.type == EventType.KeyDown)
        {
            if (Event.current.keyCode == KeyCode.LeftArrow) { Click(); v.Next(-1); }
            else if (Event.current.keyCode == KeyCode.RightArrow) { Click(); v.Next(1); }
            else if (Event.current.keyCode == KeyCode.Escape) CloseMobViewer();
        }

        float bw = 200f, bh = 46f;
        if (GUI.Button(new Rect(cx - bw * 0.5f, Screen.height - 72f, bw, bh), "Back", PaperButton(20)))
        {
            Click();
            CloseMobViewer();
        }

        GUI.Label(new Rect(0f, Screen.height - 24f, Screen.width, 20f),
            "Left / Right to browse   -   Esc to go back",
            Style(13, TextAnchor.MiddleCenter, new Color(0.7f, 0.73f, 0.78f)));
    }

    void DrawMobStats(MobDef def, Rect area)
    {
        float rowH = 25f;
        float labelW = area.width * 0.30f;
        GUI.Label(new Rect(area.x, area.y, labelW, rowH), "Wave",
            Style(15, TextAnchor.MiddleLeft, new Color(0.78f, 0.80f, 0.84f)));
        GUI.Label(new Rect(area.x + labelW, area.y, area.width - labelW, rowH),
            WaveFor(def.id), Style(15, TextAnchor.MiddleLeft, Color.white));

        string[,] rows =
        {
            { "Archetype", def.archetype.ToString() },
            { "Health", def.health.ToString("0") },
            { "Speed", def.speed.ToString("0.00") },
            { "Leak damage", def.leakDamage.ToString() },
            { "Traits", Traits(def) },
        };
        for (int i = 0; i < rows.GetLength(0); i++)
        {
            float y = area.y + (i + 1) * rowH;
            GUI.Label(new Rect(area.x, y, labelW, rowH), rows[i, 0],
                Style(15, TextAnchor.MiddleLeft, new Color(0.78f, 0.80f, 0.84f)));
            GUI.Label(new Rect(area.x + labelW, y, area.width - labelW, rowH), rows[i, 1],
                Style(15, TextAnchor.MiddleLeft, new Color(0.95f, 0.95f, 0.9f)));
        }
    }

    static string WaveFor(string id)
    {
        for (int i = 0; i < TDBalance.Waves.Length; i++)
            if (TDBalance.Waves[i].mob == id) return (i + 1).ToString();
        return "-";
    }

    static string Traits(MobDef d)
    {
        List<string> t = new List<string>();
        if (d.regen > 0f) t.Add("regenerates " + d.regen.ToString("0.#") + "/s");
        if (d.armour > 0f) t.Add("armour " + d.armour.ToString("0.#"));
        if (d.slowResist > 0f) t.Add("50% slow resist");
        if (d.enrage > 0f) t.Add("enrages");
        if (d.dashEvery > 0f) t.Add("dashes");
        return t.Count > 0 ? string.Join(", ", t.ToArray()) : "-";
    }
}
