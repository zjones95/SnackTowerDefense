using UnityEngine;

// Pause menu + settings overlay. Split into a partial class so both share
// TDGameManager's state, styles and audio helpers.
//
// The settings overlay is a single IMGUI screen reused from two places: the
// main menu (return context = menu) and the in-game pause menu (return context
// = pause). A bool pair tracks that context instead of adding GameState values.
public partial class TDGameManager
{
    private bool paused;            // in-game pause menu open (SP: timeScale = 0; MP: overlay only, sim keeps running)
    private bool settingsOpen;      // settings overlay visible
    private bool settingsFromPause; // Esc/Back returns to the pause menu, not the menu

    // --------------------------------------------------------------- helpers
    /// <summary>Un-freezes the game and clears the pause flag. Safe to call anywhere.
    /// In multiplayer the sim never freezes, so timeScale is left alone.</summary>
    void RestoreTimeScale()
    {
        if (!mpActive) Time.timeScale = 1f;
        paused = false;
    }

    /// <summary>Opens the pause menu. Single-player freezes (timeScale = 0);
    /// multiplayer keeps simulating underneath (overlay only).</summary>
    void OpenPause()
    {
        paused = true;
        settingsOpen = false;
        if (!mpActive) Time.timeScale = 0f;
    }

    void ResumeGame()
    {
        settingsOpen = false;
        settingsFromPause = false;
        RestoreTimeScale();
    }

    /// <summary>Esc while paused: close settings first, otherwise resume.
    /// Ignored while typing chat so Esc cancels the chat line first.</summary>
    void HandlePauseInput()
    {
        if (!Input.GetKeyDown(KeyCode.Escape) || ChatSync.IsTyping) return;
        if (settingsOpen) CloseSettings();
        else ResumeGame();
    }

    /// <summary>Leaves a run (single-player or multiplayer) cleanly for the main menu.</summary>
    void ReturnToMainMenu()
    {
        settingsOpen = false;
        settingsFromPause = false;
        RestoreTimeScale();
        if (mpActive)
        {
            LeaveMultiplayer();   // unhooks, NetworkSession.Leave(), ClearWorld(), State = MainMenu
            return;
        }
        State = GameState.MainMenu;
        ClearWorld();
    }

    void QuitGame()
    {
        RestoreTimeScale();
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // ------------------------------------------------------------ pause menu
    void DrawPauseMenu()
    {
        if (!paused || (State != GameState.Playing && !SpectatingAfterResult)) return;
        if (settingsOpen && settingsFromPause) return;   // the settings overlay covers the pause menu

        Color old = GUI.color;
        GUI.color = new Color(0.10f, 0.11f, 0.12f, 0.70f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = old;

        GUI.Label(new Rect(0f, Screen.height * 0.26f, Screen.width, 60f), SpectatingAfterResult ? "SPECTATING" : "PAUSED",
            Style(44, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.4f)));

        float bw = 300f, bh = 52f, gap = 14f;
        float bx = (Screen.width - bw) * 0.5f;
        float by = Screen.height * 0.40f;
        GUIStyle btn = PaperButton(22);

        if (GUI.Button(new Rect(bx, by, bw, bh), SpectatingAfterResult ? "Resume Spectating" : "Resume", btn))
        {
            Click();
            ResumeGame();
            return;
        }
        if (GUI.Button(new Rect(bx, by + 1f * (bh + gap), bw, bh), "Settings", btn))
        {
            Click();
            OpenSettings(true);
        }
        if (GUI.Button(new Rect(bx, by + 2f * (bh + gap), bw, bh), "Quit to Main Menu", btn))
        {
            Click();
            ReturnToMainMenu();
        }
        if (GUI.Button(new Rect(bx, by + 3f * (bh + gap), bw, bh), "Quit Game", btn))
        {
            Click();
            QuitGame();
        }

        GUI.Label(new Rect(0f, Screen.height - 30f, Screen.width, 24f), SpectatingAfterResult ? "Esc to return to spectating" : "Esc to resume",
            Style(13, TextAnchor.MiddleCenter, new Color(0.78f, 0.8f, 0.84f)));
    }

    // -------------------------------------------------------------- settings
    void OpenSettings(bool fromPause)
    {
        settingsOpen = true;
        settingsFromPause = fromPause;
        controlsOpen = false;   // always land on Settings, never a stale Controls layer
    }

    void CloseSettings()
    {
        if (controlsOpen) { controlsOpen = false; return; }   // Esc/Back closes Controls first
        settingsOpen = false;
        settingsFromPause = false;   // pause (if any) stays open behind it
    }

    void DrawSettings()
    {
        if (controlsOpen) { DrawControls(); return; }   // Controls overlay (TDGameManager.Controls.cs)
        TDAudio audio = TDAudio.Instance;

        // scrim over whatever opened this
        Color old = GUI.color;
        GUI.color = new Color(0.10f, 0.11f, 0.12f, 0.78f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = old;

        GUI.Label(new Rect(0f, Screen.height * 0.19f, Screen.width, 64f), "SETTINGS",
            Style(40, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.4f)));

        float w = Mathf.Min(560f, Screen.width - 80f);
        float x = (Screen.width - w) * 0.5f;
        float y = Screen.height * 0.35f;

        // panel behind the rows
        DrawPanel(new Rect(x - 24f, y - 24f, w + 48f, 220f));

        float labelW = 120f, pctW = 80f;
        float sliderX = x + labelW + 12f;
        float sliderW = w - labelW - pctW - 24f;

        // ---- music ----
        float mv = audio != null ? audio.MusicVolume : 0.40f;
        GUI.Label(new Rect(x, y, labelW, 30f), "Music",
            Style(20, TextAnchor.MiddleLeft, Color.white));
        float nm = GUI.HorizontalSlider(new Rect(sliderX, y + 8f, sliderW, 20f), mv, 0f, 1f);
        GUI.Label(new Rect(x + w - pctW, y, pctW, 30f), Mathf.RoundToInt(nm * 100f) + "%",
            Style(20, TextAnchor.MiddleRight, Color.white));
        if (audio != null && Mathf.Abs(nm - mv) > 0.0001f)
        {
            audio.MusicVolume = nm;
            if (Event.current.type == EventType.MouseUp) Click();
        }

        // ---- sfx ----
        float sy = y + 62f;
        float sv = audio != null ? audio.SfxVolume : 1f;
        GUI.Label(new Rect(x, sy, labelW, 30f), "SFX",
            Style(20, TextAnchor.MiddleLeft, Color.white));
        float ns = GUI.HorizontalSlider(new Rect(sliderX, sy + 8f, sliderW, 20f), sv, 0f, 1f);
        GUI.Label(new Rect(x + w - pctW, sy, pctW, 30f), Mathf.RoundToInt(ns * 100f) + "%",
            Style(20, TextAnchor.MiddleRight, Color.white));
        if (audio != null && Mathf.Abs(ns - sv) > 0.0001f)
        {
            audio.SfxVolume = ns;
            if (Event.current.type == EventType.MouseUp) Click();
        }

        // ---- controls + back ----
        float bw = 220f, bh = 50f;
        float btnY = y + 126f;
        float cx = (Screen.width - (bw * 2f + 16f)) * 0.5f;
        if (GUI.Button(new Rect(cx, btnY, bw, bh), "Controls", PaperButton(20)))
        {
            Click();
            OpenControls();
        }
        if (GUI.Button(new Rect(cx + bw + 16f, btnY, bw, bh), "Back", PaperButton(20)))
        {
            Click();
            CloseSettings();
        }

        GUI.Label(new Rect(0f, Screen.height - 30f, Screen.width, 24f), "Esc to close",
            Style(13, TextAnchor.MiddleCenter, new Color(0.78f, 0.8f, 0.84f)));
    }
}
