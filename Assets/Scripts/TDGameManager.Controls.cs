using UnityEngine;

// Controls (keybinding map) overlay, opened from Settings.
// Split into its own partial-class file so Settings.cs pause/timeScale logic
// stays untouched: DrawSettings() hooks this with a one-line branch, and
// CloseSettings() closes this layer first (see TDGameManager.Settings.cs).
//
// Contexts: works from both menu-settings and pause-settings, because it never
// touches paused/timeScale itself -- Back/Esc just clears controlsOpen and the
// settings overlay underneath stays on whichever context opened it.
public partial class TDGameManager
{
    private bool controlsOpen;   // Controls overlay visible on top of Settings

    void OpenControls()
    {
        controlsOpen = true;
    }

    void CloseControls()
    {
        controlsOpen = false;
    }

    /// <summary>Keybinding map overlay, styled like the settings panel
    /// (paper Back button, Style() labels, dark scrim + panel).</summary>
    void DrawControls()
    {
        // scrim over the settings underneath
        Color old = GUI.color;
        GUI.color = new Color(0.10f, 0.11f, 0.12f, 0.94f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = old;

        GUI.Label(new Rect(0f, Screen.height * 0.08f, Screen.width, 64f), "CONTROLS",
            Style(40, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.4f)));

        // key -> action rows
        string[,] rows = {
            { "B", "Build tower (click a tile to place)" },
            { "G", "Gold tower (max " + TowerCatalog.MaxGoldTowers + " per board)" },
            { "E", "Merge / fuse selected (T6 + T6 fuse to T7)" },
            { "U", "Ascend / upgrade selected in place" },
            { "R", "Re-roll selected (consumes a tower one tier below)" },
            { "X", "Sell selected tower" },
            { "Space", "Start wave early (during prep countdown)" },
            { "Esc", "Cancel mode, then pause / close" },
            { "WASD / Arrows", "Pan camera" },
            { "Middle-drag", "Rotate camera" },
            { "Scroll", "Zoom camera" },
            { "M", "Music on / off" },
            { "T", "Chat (multiplayer)" },
            { "0", "Back to your board (multiplayer spectating)" },
        };
        int n = rows.GetLength(0);

        float w = Mathf.Min(640f, Screen.width - 80f);
        float x = (Screen.width - w) * 0.5f;
        float y = Screen.height * 0.22f;

        float rowH = 26f;
        float listH = n * rowH;
        float backH = 50f;
        float panelH = listH + backH + 56f;

        // panel behind the rows
        old = GUI.color;
        GUI.color = new Color(0.12f, 0.13f, 0.14f, 0.94f);
        GUI.DrawTexture(new Rect(x - 24f, y - 20f, w + 48f, panelH), Texture2D.whiteTexture);
        GUI.color = old;

        float keyW = 150f;
        GUIStyle keyStyle = Style(18, TextAnchor.MiddleRight, new Color(1f, 0.85f, 0.4f));
        GUIStyle descStyle = Style(18, TextAnchor.MiddleLeft, Color.white);
        for (int i = 0; i < n; i++)
        {
            float ry = y + i * rowH;
            GUI.Label(new Rect(x, ry, keyW, rowH), rows[i, 0], keyStyle);
            GUI.Label(new Rect(x + keyW + 14f, ry, w - keyW - 14f, rowH), rows[i, 1], descStyle);
        }

        // ---- back ----
        float bw = 220f;
        if (GUI.Button(new Rect((Screen.width - bw) * 0.5f, y + listH + 20f, bw, backH), "Back", PaperButton(20)))
        {
            Click();
            CloseControls();
        }

        GUI.Label(new Rect(0f, Screen.height - 30f, Screen.width, 24f), "Esc to close",
            Style(13, TextAnchor.MiddleCenter, new Color(0.78f, 0.8f, 0.84f)));
    }
}
