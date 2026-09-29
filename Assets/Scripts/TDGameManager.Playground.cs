using UnityEngine;

// Debug Playground: a bright, high-contrast sandbox board for testing towers,
// waves and boss behaviours in isolation. Reached from the main menu.
//
//   - pick any wave (1-35) and send it on demand (no auto-wave)
//   - place any tower at any tier for free (unlimited money)
//   - delete / clear towers, clear mobs, speed up time (1x/2x/4x)
//   - leaks never cost lives
//
// The normal 35-wave run is untouched: this is a standalone GameState that
// reuses the existing map layout (off-white board, light-grey path, raised
// dark-grey buildable pads, pastel start/end) and the shared SpawnMob /
// CreateTower paths so it exercises the exact same gameplay code.
public partial class TDGameManager
{
    private bool playgroundActive;

    private int pgWave = 1;
    private TowerType pgTower = TowerType.SingleShot;
    private int pgTier = 1;
    private bool pgPlacing = true;
    private bool pgDeleting;
    private bool pgShowWaves;
    private int pgSpeed = 1;

    // Stored UI rects (drawn in OnGUI, hit-tested next frame) so clicks on the
    // panels never fall through to the board underneath.
    private Rect pgWavePanel, pgTowerPanel, pgTierPanel, pgToolbar, pgSelPanel, pgWaveGrid;

    // ------------------------------------------------------------------ enter
    void StartPlayground()
    {
        paused = false;
        settingsOpen = false;
        RestoreTimeScale();
        Time.timeScale = 1f;
        pgSpeed = 1;

        boardOffset = Vector3.zero;
        viewOffset = Vector3.zero;
        Mobs.Clear();
        towers.Clear();
        Selected = null;
        merging = building = goldBuilding = reRolling = false;
        cleared = eliminated = false;
        if (worldRoot != null) Destroy(worldRoot.gameObject);

        Money = 0;
        GoldGenerated = 0;
        Lives = 999999;                       // leaks are free anyway (see OnMobLeaked)
        Wave = pgWave;
        spawnQueue.Clear();
        spawnIndex = 0;
        Round = RoundState.Preparing;
        prepTimer = 0f;
        message = "";
        damageTestDummy = null;
        damageTestTimer = 0f;
        damageTestResult = "";
        damageTestResultTimer = 0f;

        camFocus = Vector3.zero;
        camYaw = GameYaw;
        camPitch = GamePitch;
        camDist = GameDist;

        playgroundActive = true;
        State = GameState.Playground;
        BuildPlaygroundWorld();
    }

    void ExitPlayground()
    {
        playgroundActive = false;
        RestoreTimeScale();
        Time.timeScale = 1f;
        pgShowWaves = false;
        pgPlacing = true;
        pgDeleting = false;
        if (cam != null) cam.backgroundColor = new Color(0.12f, 0.14f, 0.17f);
        State = GameState.MainMenu;
        ClearWorld();
    }

    // ------------------------------------------------------------------ world
    void BuildPlaygroundWorld()
    {
        worldRoot = new GameObject("World").transform;
        towersRoot = new GameObject("Towers").transform;
        towersRoot.SetParent(worldRoot, false);
        mobsRoot = new GameObject("Mobs").transform;
        mobsRoot.SetParent(worldRoot, false);
        ProjectilesRoot = new GameObject("Projectiles").transform;
        ProjectilesRoot.SetParent(worldRoot, false);

        string[] layout = Layout;
        Vector2Int[] route = Route;
        float cell = 2f;
        map = TDBoardBuilder.CreateMap(layout, route, cell, Vector3.zero);

        BuildPlaygroundTiles(worldRoot, map);
        BuildHover();
        BuildGhost();

        if (cam != null) cam.backgroundColor = new Color(0.96f, 0.96f, 0.95f);
    }

    /// <summary>Bright, high-contrast board: off-white floor, light-grey path,
    /// raised dark-grey pads marking buildable cells, pastel start/end.</summary>
    void BuildPlaygroundTiles(Transform parent, TDMap map)
    {
        int gw = map.Width, gh = map.Height;
        float cell = map.Cell;
        float W = gw * cell, H = gh * cell;

        // off-white board. Top sits at y = -0.02 so the tiles (tops at 0.00+)
        // never go coplanar with it — coplanar faces are what shimmered when
        // the camera rotated.
        TDVisuals.Box(parent, "Board", new Vector3(0f, -0.10f, 0f), new Vector3(W + 2f, 0.16f, H + 2f),
            TDVisuals.Mat(new Color(0.93f, 0.92f, 0.89f), 0f, 0.5f));

        Material path = TDVisuals.Mat(new Color(0.72f, 0.72f, 0.75f), 0f, 0.5f);
        Material pad = TDVisuals.Mat(new Color(0.26f, 0.27f, 0.31f), 0f, 0.55f);
        Material start = TDVisuals.Mat(new Color(0.56f, 0.89f, 0.56f), 0f, 0.5f);
        Material end = TDVisuals.Mat(new Color(0.95f, 0.58f, 0.58f), 0f, 0.5f);

        for (int ly = 0; ly < gh; ly++)
        {
            for (int x = 0; x < gw; x++)
            {
                char c = map.At(x, ly);
                Material m = null;
                float h = 0.10f;      // flush with the board top
                float cy = -0.05f;
                if (c == 'm') m = path;
                else if (c == 's') m = start;
                else if (c == 'e') m = end;
                else if (c == 't') { m = pad; h = 0.12f; cy = -0.04f; }   // buildable pads sit slightly proud
                if (m == null) continue;
                Vector3 pos = map.CellCenter(x, ly) + Vector3.up * cy;
                TDVisuals.Box(parent, "Tile", pos, new Vector3(cell * 0.97f, h, cell * 0.97f), m);
            }
        }
    }

    // --------------------------------------------------------------- per frame
    void TickPlayground()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (pgShowWaves) pgShowWaves = false;
            else ExitPlayground();
            return;
        }
        if (Input.GetKeyDown(KeyCode.Space)) { PlaygroundSendWave(); return; }

        if (Round == RoundState.WaveActive) TickWave();

        UpdateCamera(Time.deltaTime);
        HandlePlaygroundMouse();
        UpdatePlaygroundHover();
    }

    void UpdatePlaygroundHover()
    {
        if (hover == null) return;

        if (Selected != null)
        {
            SetHoverMat(hoverSelected);
            hover.position = map.CellCenter(Selected.CellX, Selected.CellY) + Vector3.up * 0.08f;
            hover.gameObject.SetActive(true);
            return;
        }

        bool mode = pgPlacing || pgDeleting;
        if (!mode || PlaygroundMouseOverUI()) { hover.gameObject.SetActive(false); return; }

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, Vector3.zero);
        float d;
        if (!plane.Raycast(ray, out d)) { hover.gameObject.SetActive(false); return; }
        Vector3 p = ray.GetPoint(d);
        int x, y;
        map.WorldToCell(p, out x, out y);
        if (!map.InBounds(x, y)) { hover.gameObject.SetActive(false); return; }

        int idx = map.Idx(x, y);
        bool ok = pgDeleting ? towers.ContainsKey(idx) : map.IsBuildable(x, y) && !towers.ContainsKey(idx);
        SetHoverMat(ok ? hoverValid : hoverInvalid);
        hover.position = map.CellCenter(x, y) + Vector3.up * 0.08f;
        hover.gameObject.SetActive(true);
    }

    void HandlePlaygroundMouse()
    {
        if (Input.GetMouseButtonDown(1))
        {
            pgPlacing = false;
            pgDeleting = false;
            SetSelected(null);
            return;
        }

        if (PlaygroundMouseOverUI() || !Input.GetMouseButtonDown(0)) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, Vector3.zero);
        float d;
        if (!plane.Raycast(ray, out d)) return;
        Vector3 p = ray.GetPoint(d);
        int x, y;
        map.WorldToCell(p, out x, out y);
        if (!map.InBounds(x, y))
        {
            if (!pgPlacing && !pgDeleting) SetSelected(null);
            return;
        }

        int idx = map.Idx(x, y);
        Tower t = towers.ContainsKey(idx) ? towers[idx] : null;

        if (pgPlacing) { PlaygroundPlace(x, y); return; }
        if (pgDeleting) { if (t != null) PlaygroundDelete(t); return; }

        if (t != null) SetSelected(t == Selected ? null : t);
        else SetSelected(null);
    }

    // ----------------------------------------------------------------- actions
    void PlaygroundPlace(int x, int y)
    {
        if (!map.IsBuildable(x, y)) { message = "Can't build there"; messageTimer = 1.2f; return; }
        int idx = map.Idx(x, y);
        if (towers.ContainsKey(idx)) { message = "That tile is taken"; messageTimer = 1.2f; return; }

        Tower t = CreateTower(x, y, pgTower, pgTier);
        t.InvestedCost = 0;
        if (TDAudio.Instance != null) TDAudio.Instance.Build();
        message = "Placed " + t.DisplayName + " (Tier " + pgTier + ")";
        messageTimer = 1.5f;
    }

    void PlaygroundDelete(Tower t)
    {
        if (t == null) return;
        towers.Remove(map.Idx(t.CellX, t.CellY));
        if (Selected == t) SetSelected(null);
        Destroy(t.gameObject);
        if (TDAudio.Instance != null) TDAudio.Instance.Merge();
    }

    void PlaygroundSendWave()
    {
        pgWave = Mathf.Clamp(pgWave, 1, TDBalance.TotalWaves);
        Wave = pgWave;
        spawnQueue = BuildWave(Wave);
        spawnIndex = 0;
        waveTime = 0f;
        Round = RoundState.WaveActive;
        waveIntroTimer = 3.5f;
        message = "Wave " + Wave + ": " + MobCatalog.Get(TDBalance.Waves[Wave - 1].mob).displayName;
        messageTimer = 2f;
    }

    void PlaygroundSendOne()
    {
        pgWave = Mathf.Clamp(pgWave, 1, TDBalance.TotalWaves);
        Wave = pgWave;
        string id = TDBalance.Waves[Wave - 1].mob;
        SpawnMob(id);
        message = "Spawned 1 " + MobCatalog.Get(id).displayName;
        messageTimer = 1.5f;
    }

    void PlaygroundClearMobs()
    {
        for (int i = Mobs.Count - 1; i >= 0; i--)
            if (Mobs[i] != null) Destroy(Mobs[i].gameObject);
        Mobs.Clear();
        spawnQueue.Clear();
        spawnIndex = 0;
        Round = RoundState.Preparing;
        message = "Cleared all mobs";
        messageTimer = 1.5f;
    }

    void PlaygroundClearTowers()
    {
        foreach (Tower t in AllTowers)
            if (t != null) Destroy(t.gameObject);
        towers.Clear();
        SetSelected(null);
        message = "Cleared all towers";
        messageTimer = 1.5f;
    }

    void PlaygroundCycleSpeed()
    {
        pgSpeed = pgSpeed >= 4 ? 1 : pgSpeed * 2;
        Time.timeScale = pgSpeed;
    }

    void PgSetTower(TowerType t)
    {
        pgTower = t;
        if (t == TowerType.Gold) pgTier = Mathf.Clamp(pgTier, 1, 4);
        else if (TowerCatalog.IsT7Type(t)) pgTier = 7;
        else pgTier = Mathf.Clamp(pgTier, 1, TowerCatalog.MaxTier);
    }

    void PgSetTier(int tier)
    {
        if (TowerCatalog.IsT7Type(pgTower)) { pgTier = 7; return; }
        if (pgTower == TowerType.Gold) pgTier = Mathf.Clamp(tier, 1, 4);
        else pgTier = Mathf.Clamp(tier, 1, TowerCatalog.MaxTier);
    }

    bool PgTierEnabled(int tier)
    {
        if (pgTower == TowerType.Gold) return tier <= 4;
        if (TowerCatalog.IsT7Type(pgTower)) return tier == 7;
        return true;
    }

    // ------------------------------------------------------------------ input
    bool PlaygroundMouseOverUI()
    {
        Vector2 m = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        if (pgWavePanel.Contains(m) || pgTowerPanel.Contains(m) || pgTierPanel.Contains(m) || pgToolbar.Contains(m)) return true;
        if (pgShowWaves && pgWaveGrid.Contains(m)) return true;
        if (Selected != null && pgSelPanel.Contains(m)) return true;
        return false;
    }

    // --------------------------------------------------------------------- GUI
    // Crisp flat button background. The default GUI.skin.button texture is a
    // small rounded-corner bitmap that goes blurry when stretched over our
    // larger panel buttons — a plain white texture tinted via
    // GUI.backgroundColor stays pixel-sharp at any size.
    static Texture2D pgFlatBg;
    static Texture2D PgFlatBg()
    {
        if (pgFlatBg != null) return pgFlatBg;
        pgFlatBg = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        pgFlatBg.filterMode = FilterMode.Point;
        pgFlatBg.wrapMode = TextureWrapMode.Clamp;
        Color[] px = new Color[16];
        for (int i = 0; i < px.Length; i++) px[i] = Color.white;
        pgFlatBg.SetPixels(px);
        pgFlatBg.Apply();
        return pgFlatBg;
    }

    GUIStyle TronButton(int size)
    {
        GUIStyle s = new GUIStyle(GUI.skin.button);
        s.fontSize = size;
        s.fontStyle = FontStyle.Bold;
        s.alignment = TextAnchor.MiddleCenter;
        s.normal.textColor = new Color(0.55f, 0.95f, 1f);
        s.hover.textColor = Color.white;
        s.active.textColor = new Color(0.35f, 0.75f, 0.85f);
        s.focused.textColor = new Color(0.55f, 0.95f, 1f);
        Texture2D bg = PgFlatBg();
        s.normal.background = bg;
        s.hover.background = bg;
        s.active.background = bg;
        s.focused.background = bg;
        s.onNormal.background = bg;
        s.onHover.background = bg;
        s.onActive.background = bg;
        s.padding = new RectOffset(6, 6, 4, 4);
        s.margin = new RectOffset(2, 2, 2, 2);
        s.border = new RectOffset(0, 0, 0, 0);
        return s;
    }

    bool PgButton(Rect r, string label, bool active, int size)
    {
        // 1px darker outline behind the button so the flat fill reads as a
        // deliberate panel control rather than floating text.
        Color prev = GUI.color;
        GUI.color = new Color(0.04f, 0.22f, 0.27f);
        GUI.DrawTexture(new Rect(r.x - 1f, r.y - 1f, r.width + 2f, r.height + 2f), PgFlatBg());
        GUI.color = prev;

        prev = GUI.backgroundColor;
        GUI.backgroundColor = active ? new Color(0.10f, 0.45f, 0.55f) : new Color(0.02f, 0.06f, 0.09f);
        bool hit = GUI.Button(r, label, TronButton(size));
        GUI.backgroundColor = prev;
        return hit;
    }

    void DrawPlayground()
    {
        // status (top-left)
        GUI.Label(new Rect(12f, 6f, 460f, 24f), "PLAYGROUND", Style(22, TextAnchor.MiddleLeft, new Color(0.05f, 0.35f, 0.40f)));
        GUI.Label(new Rect(12f, 32f, 460f, 20f), "Enemies " + Mobs.Count + "    Towers " + towers.Count,
            Style(15, TextAnchor.MiddleLeft, new Color(0.25f, 0.28f, 0.32f)));
        GUI.Label(new Rect(12f, 52f, 460f, 20f), "Money inf    Lives inf",
            Style(15, TextAnchor.MiddleLeft, new Color(0.25f, 0.28f, 0.32f)));

        // wave bar (top-center)
        float barW = 660f;
        float bx = (Screen.width - barW) * 0.5f;
        float by = 8f;
        pgWavePanel = new Rect(bx - 8f, by - 4f, barW + 16f, 52f);

        if (PgButton(new Rect(bx, by, 40f, 40f), "<", false, 20)) { pgWave = Mathf.Max(1, pgWave - 1); Click(); }
        string wn = MobCatalog.Get(TDBalance.Waves[Mathf.Clamp(pgWave, 1, TDBalance.TotalWaves) - 1].mob).displayName;
        GUI.Label(new Rect(bx + 46f, by, 192f, 40f),
            "Wave " + pgWave + " / " + TDBalance.TotalWaves + "\n" + wn,
            Style(13, TextAnchor.MiddleCenter, new Color(0.45f, 0.33f, 0.05f)));
        if (PgButton(new Rect(bx + 244f, by, 40f, 40f), ">", false, 20)) { pgWave = Mathf.Min(TDBalance.TotalWaves, pgWave + 1); Click(); }
        if (PgButton(new Rect(bx + 292f, by, 80f, 40f), "Waves", pgShowWaves, 14)) { pgShowWaves = !pgShowWaves; Click(); }
        if (PgButton(new Rect(bx + 380f, by, 140f, 40f), "SEND WAVE", false, 15)) { PlaygroundSendWave(); Click(); }
        if (PgButton(new Rect(bx + 528f, by, 124f, 40f), "Send 1", false, 15)) { PlaygroundSendOne(); Click(); }

        if (pgShowWaves) DrawPlaygroundWaveGrid();

        // tower picker (right)
        float px = Screen.width - 212f;
        float py = 70f;
        pgTowerPanel = new Rect(px - 10f, py - 34f, 212f, 30f + TowerCatalog.GalleryTypes.Length * 27f + 14f);
        GUI.Label(new Rect(px, py, 200f, 24f), "TOWER", Style(16, TextAnchor.MiddleLeft, new Color(0.35f, 0.18f, 0.45f)));
        float ty = py + 30f;
        for (int i = 0; i < TowerCatalog.GalleryTypes.Length; i++)
        {
            TowerType tt = TowerCatalog.GalleryTypes[i];
            string nm = TowerCatalog.Get(tt).displayName;
            if (PgButton(new Rect(px, ty, 200f, 25f), nm, pgTower == tt, 13)) { PgSetTower(tt); Click(); }
            ty += 27f;
        }

        // tier picker (right, under tower list)
        pgTierPanel = new Rect(px - 10f, ty + 2f, 212f, 46f);
        GUI.Label(new Rect(px, ty + 4f, 200f, 18f), "TIER", Style(13, TextAnchor.MiddleLeft, new Color(0.35f, 0.18f, 0.45f)));
        float ttx = px;
        float tty = ty + 24f;
        for (int t = 1; t <= TowerCatalog.MaxTier; t++)
        {
            GUI.enabled = PgTierEnabled(t);
            if (PgButton(new Rect(ttx, tty, 25f, 26f), t.ToString(), pgTier == t, 14)) { PgSetTier(t); Click(); }
            GUI.enabled = true;
            ttx += 29f;
        }

        // selected tower panel (left)
        pgSelPanel = new Rect(0f, 0f, 0f, 0f);
        if (Selected != null) DrawPlaygroundSelected();

        // bottom toolbar
        pgToolbar = new Rect(0f, Screen.height - 62f, Screen.width, 62f);
        float tbX = 12f;
        if (PgButton(new Rect(tbX, Screen.height - 52f, 90f, 40f), "Place", pgPlacing, 15)) { pgPlacing = !pgPlacing; pgDeleting = false; Click(); }
        tbX += 96f;
        if (PgButton(new Rect(tbX, Screen.height - 52f, 90f, 40f), "Delete", pgDeleting, 15)) { pgDeleting = !pgDeleting; pgPlacing = false; Click(); }
        tbX += 96f;
        if (PgButton(new Rect(tbX, Screen.height - 52f, 90f, 40f), "Speed x" + pgSpeed, false, 15)) { PlaygroundCycleSpeed(); Click(); }
        tbX += 96f;
        if (PgButton(new Rect(tbX, Screen.height - 52f, 110f, 40f), "Clear Mobs", false, 15)) { PlaygroundClearMobs(); Click(); }
        tbX += 116f;
        if (PgButton(new Rect(tbX, Screen.height - 52f, 110f, 40f), "Clear Towers", false, 15)) { PlaygroundClearTowers(); Click(); }
        tbX += 116f;
        if (PgButton(new Rect(tbX, Screen.height - 52f, 90f, 40f), "Back", false, 15)) ExitPlayground();

        // message + hint
        if (!string.IsNullOrEmpty(message))
            GUI.Label(new Rect(0f, 90f, Screen.width, 24f), message, Style(16, TextAnchor.MiddleCenter, new Color(0.10f, 0.45f, 0.15f)));
        GUI.Label(new Rect(0f, Screen.height - 66f, Screen.width, 14f),
            "Left-click: place / select    Right-click: cancel    Space: send wave    WASD / middle-drag / scroll: camera    Esc: back",
            Style(11, TextAnchor.MiddleCenter, new Color(0.35f, 0.38f, 0.42f)));

        DrawWaveIntro();
        DrawBossBar();
    }

    void DrawPlaygroundWaveGrid()
    {
        int cols = 7, rows = 5;
        float cellW = 44f, cellH = 34f, gap = 6f;
        float w = cols * cellW + (cols - 1) * gap + 24f;
        float h = rows * cellH + (rows - 1) * gap + 48f;
        float gx = (Screen.width - w) * 0.5f;
        float gy = 64f;
        pgWaveGrid = new Rect(gx, gy, w, h);

        Color old = GUI.color;
        GUI.color = new Color(0.02f, 0.05f, 0.08f, 0.96f);
        GUI.DrawTexture(pgWaveGrid, Texture2D.whiteTexture);
        GUI.color = old;

        GUI.Label(new Rect(gx, gy + 8f, w, 24f), "SELECT WAVE", Style(16, TextAnchor.MiddleCenter, new Color(0.4f, 1f, 1f)));
        for (int wv = 1; wv <= TDBalance.TotalWaves; wv++)
        {
            int i = wv - 1;
            int col = i % cols, row = i / cols;
            Rect r = new Rect(gx + 12f + col * (cellW + gap), gy + 36f + row * (cellH + gap), cellW, cellH);
            if (PgButton(r, wv.ToString(), pgWave == wv, 14))
            {
                pgWave = wv;
                pgShowWaves = false;
                Click();
            }
            if (TDBalance.Waves[i].boss)
                GUI.Label(new Rect(r.x, r.y - 9f, cellW, 12f), "BOSS", Style(9, TextAnchor.MiddleCenter, new Color(1f, 0.5f, 0.4f)));
        }
    }

    void DrawPlaygroundSelected()
    {
        Tower t = Selected;
        TowerTierStats s = t.Stats;
        pgSelPanel = new Rect(12f, 90f, 440f, 320f);

        Color old = GUI.color;
        GUI.color = new Color(0.02f, 0.05f, 0.08f, 0.94f);
        GUI.DrawTexture(pgSelPanel, Texture2D.whiteTexture);
        GUI.color = old;

        float x = 20f, y = 98f;
        GUI.Label(new Rect(x, y, 420f, 24f), t.DisplayName + "  -  Tier " + t.Tier,
            Style(17, TextAnchor.MiddleLeft, new Color(1f, 0.9f, 0.5f)));
        y += 28f;
        string line = t.Type == TowerType.Gold
            ? "Gold +$" + s.goldPerHit + " per hit    Rate " + s.fireInterval.ToString("0.00") + "s    Range " + s.range.ToString("0.#")
            : "Damage " + s.damage + "    Rate " + s.fireInterval.ToString("0.00") + "s    Range " + s.range.ToString("0.#");
        GUI.Label(new Rect(x, y, 420f, 20f), line, Style(14, TextAnchor.MiddleLeft, Color.white));
        y += 24f;
        GUI.Label(new Rect(x, y, 420f, 20f), "Damage done: " + Mathf.RoundToInt(t.DamageDone),
            Style(14, TextAnchor.MiddleLeft, new Color(0.75f, 0.85f, 0.95f)));
        y += 28f;
        GUI.Label(new Rect(x, y, 100f, 20f), "Target:", Style(13, TextAnchor.MiddleLeft, new Color(0.75f, 0.8f, 0.85f)));
        if (PgButton(new Rect(x + 100f, y - 2f, 200f, 24f), TargetingName(t.Targeting), false, 13))
        {
            t.SetTargeting((TowerTargeting)(((int)t.Targeting + 1) % 6));
            Click();
        }
        y += 30f;
        if (PgButton(new Rect(x, y, 120f, 30f), "Delete", false, 15)) { PlaygroundDelete(t); Click(); }
        y += 40f;

        GUIStyle mw = Style(12, TextAnchor.UpperLeft, new Color(0.8f, 0.88f, 0.95f));
        mw.wordWrap = true;
        string mod7 = TowerCatalog.ModifierText(t.Type, 7);
        if (mod7 != null) GUI.Label(new Rect(x, y, 420f, 40f), "T7: " + mod7, mw);
        else
        {
            string mod5 = TowerCatalog.ModifierText(t.Type, 5);
            string mod6 = TowerCatalog.ModifierText(t.Type, 6);
            if (mod5 != null) GUI.Label(new Rect(x, y, 420f, 20f), "T5: " + mod5, mw);
            if (mod6 != null) GUI.Label(new Rect(x, y + 22f, 420f, 20f), "T6: " + mod6, mw);
        }
    }
}
