using System.Collections.Generic;
using UnityEngine;

public partial class TDGameManager : MonoBehaviour
{
    public static TDGameManager Instance;

    // ---- tuning: see TDBalance for all curves ----

    // ---- state ----
    public GameState State { get; private set; }
    public RoundState Round { get; private set; }
    public int Money { get; private set; }
    public int Lives { get; private set; }
    public int Wave { get; private set; }
    public Difficulty CurrentDifficulty { get; private set; } = Difficulty.Normal;

    public readonly List<Mob> Mobs = new List<Mob>();

    /// <summary>Live towers on the local board (spectating snapshots).</summary>
    public IEnumerable<Tower> AllTowers { get { return towers.Values; } }
    public Vector3 BoardOffset { get { return boardOffset; } }

    private ushort nextMobId = 1;

    // map layout + ordered waypoints (shared with tools/previews)
    public static readonly string[] Layout =
    {
        "sxxexxxxx",
        "mttmtmmmm",
        "mttmtmttm",
        "mttmtmttm",
        "mmmmmmmmm",
        "tttmtmttt",
        "mmmmmmmmm",
        "mttmtmttm",
        "mttmtmttm",
        "mmmmtmmmm"
    };
    public static readonly Vector2Int[] Route =
    {
        new Vector2Int(0, 0),
        new Vector2Int(0, 4),
        new Vector2Int(8, 4),
        new Vector2Int(8, 1),
        new Vector2Int(5, 1),
        new Vector2Int(5, 9),
        new Vector2Int(8, 9),
        new Vector2Int(8, 6),
        new Vector2Int(0, 6),
        new Vector2Int(0, 9),
        new Vector2Int(3, 9),
        new Vector2Int(3, 0)
    };

    public Transform ProjectilesRoot { get; private set; }
    public Tower Selected { get; private set; }

    private TDMap map;
    private Vector3 boardOffset;
    private Camera cam;
    private Vector3 camFocus;
    // in-game camera state (reset to these when a run starts)
    private const float GameYaw = 45f, GamePitch = 42f, GameDist = 32f;
    private float camYaw = GameYaw, camPitch = GamePitch;
    private float camDist = GameDist;
    private readonly float camDistMin = 12f, camDistMax = 90f;
    // menu backdrop has its OWN camera state so it never leaks into a run
    private float menuYaw = 35f;
    private const float MenuPitch = 38f, MenuDist = 31f, MenuOrbitDegPerSec = 3f;
    private Transform worldRoot, towersRoot, mobsRoot;
    private readonly Dictionary<int, Tower> towers = new Dictionary<int, Tower>();

    private float prepTimer;
    private string message = "";
    private float messageTimer;
    private bool merging;
    private Transform hover;
    private Renderer[] hoverRends;
    private Material hoverValid, hoverInvalid;

    // wave spawning — the wave table itself lives in TDBalance
    private struct SpawnEntry { public string mob; public float time; }

    private List<SpawnEntry> spawnQueue = new List<SpawnEntry>();
    private int spawnIndex;
    private float waveTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        if (Object.FindAnyObjectByType<TDGameManager>() != null) return;
        GameObject go = new GameObject("TDGameManager");
        go.AddComponent<TDGameManager>();
    }

    void Awake()
    {
        Instance = this;
        QualitySettings.antiAliasing = 8; // MSAA (also smooths the HUD's 3D elements)
        TDAudio.Ensure();
        SetupCameraAndLight();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        State = GameState.MainMenu;
    }

    // ---------------------------------------------------------------- camera
    void SetupCameraAndLight()
    {
        cam = Camera.main;
        if (cam == null)
        {
            GameObject cg = new GameObject("Main Camera");
            cg.tag = "MainCamera";
            cam = cg.AddComponent<Camera>();
            cg.AddComponent<AudioListener>();
        }

        // full 3D perspective camera (orbit with the middle mouse button)
        cam.orthographic = false;
        cam.fieldOfView = 55f;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 2000f;
        cam.allowMSAA = true;
        cam.allowHDR = true;
        camFocus = Vector3.zero;
        ApplyCamera();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.09f, 0.11f, 0.14f);

        // warm, dimmer sunlight (override any default scene light)
        Light sun = null;
        var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i].type == LightType.Directional) { sun = lights[i]; break; }
        }
        if (sun == null)
        {
            GameObject lg = new GameObject("Sun");
            sun = lg.AddComponent<Light>();
            sun.type = LightType.Directional;
        }
        sun.color = new Color(1f, 0.86f, 0.66f);
        sun.intensity = 0.72f;
        sun.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(50f, 35f, 0f);

        // tiny cool fill so shadows aren't pitch black
        GameObject fillGO = new GameObject("Fill");
        Light fill = fillGO.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.color = new Color(0.55f, 0.62f, 0.78f);
        fill.intensity = 0.10f;
        fill.shadows = LightShadows.None;
        fillGO.transform.rotation = Quaternion.Euler(28f, -140f, 0f);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.12f, 0.11f, 0.10f);
        RenderSettings.ambientIntensity = 1f;
    }

    // ----------------------------------------------------------- camera move
    void ApplyCamera() { ApplyCamera(camYaw, camPitch, camDist); }

    void ApplyCamera(float yaw, float pitch, float dist)
    {
        if (cam == null) return;
        cam.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        cam.transform.position = camFocus - cam.transform.forward * dist;
    }

    void UpdateCamera(float dt)
    {
        if (cam == null) return;

        // zoom in/out with the scroll wheel (perspective: changes distance)
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
            camDist = Mathf.Clamp(camDist - scroll * 12f, camDistMin, camDistMax);

        // orbit with the middle mouse button
        if (Input.GetMouseButton(2))
        {
            camYaw += Input.GetAxis("Mouse X") * 4f;
            camPitch = Mathf.Clamp(camPitch - Input.GetAxis("Mouse Y") * 4f, 8f, 85f);
        }

        // ground-plane axes relative to the camera
        Vector3 fwd = cam.transform.forward; fwd.y = 0f;
        fwd = fwd.sqrMagnitude < 0.0001f ? Vector3.forward : fwd.normalized;
        Vector3 right = cam.transform.right; right.y = 0f;
        right = right.sqrMagnitude < 0.0001f ? Vector3.right : right.normalized;

        // pan with WASD / arrows
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f)
        {
            float speed = 16f * (camDist / 32f);
            camFocus += (right * h + fwd * v) * speed * dt;
        }

        // keep the focus over the map
        float hx = map != null ? map.Width * map.Cell * 0.5f : 18f;
        float hz = map != null ? map.Height * map.Cell * 0.5f : 12f;
        camFocus.x = Mathf.Clamp(camFocus.x, viewOffset.x - hx, viewOffset.x + hx);
        camFocus.z = Mathf.Clamp(camFocus.z, viewOffset.z - hz, viewOffset.z + hz);
        camFocus.y = 0f;

        ApplyCamera();
    }

    // --------------------------------------------------------- menu backdrop
    /// <summary>
    /// Builds a real game board for the menus, but only if one isn't already
    /// around (ClearWorld() destroys the board when leaving a match, so the
    /// menu has to rebuild it on the way back).
    /// </summary>
    void EnsureMenuWorld()
    {
        if (worldRoot != null) return;
        boardOffset = Vector3.zero;
        viewOffset = Vector3.zero;
        BuildWorld();
    }

    /// <summary>Slowly orbits the camera over the menu board, independent of the in-game yaw.</summary>
    void UpdateMenuBackdrop()
    {
        EnsureMenuWorld();
        if (cam == null) return;

        menuYaw += MenuOrbitDegPerSec * Time.deltaTime;
        if (menuYaw > 360f) menuYaw -= 360f;

        camFocus = Vector3.zero;
        ApplyCamera(menuYaw, MenuPitch, MenuDist);   // menu-only camera, game state untouched
    }

    // ------------------------------------------------------------ game flow
    void StartRun() { StartRun(Vector3.zero); }

    void StartRun(Vector3 offset)
    {
        paused = false;
        settingsOpen = false;
        RestoreTimeScale();
        boardOffset = offset;
        viewOffset = offset;
        Mobs.Clear();
        towers.Clear();
        Selected = null;
        merging = false;
        cleared = false;
        eliminated = false;
        if (worldRoot != null) Destroy(worldRoot.gameObject);

        Money = TDBalance.StartMoney;
        Lives = TDBalance.StartLives;
        Wave = 1;
        spawnQueue.Clear();
        spawnIndex = 0;
        State = GameState.Playing;
        Round = RoundState.Preparing;
        prepTimer = TDBalance.PrepDuration;
        camFocus = offset;
        camYaw = GameYaw;
        camPitch = GamePitch;
        camDist = GameDist;
        message = "";

        BuildWorld();
    }

    void BuildWorld()
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
        int gw = layout[0].Length, gh = layout.Length;
        map = TDBoardBuilder.CreateMap(layout, route, cell, boardOffset);

        // (room floor is built by TDRoom)

        TDBoardBuilder.BuildTiles(worldRoot, map);
        BuildHover();
        TDBoardBuilder.BuildRoom(worldRoot, map, boardOffset);
    }

    void BuildHover()
    {
        GameObject h = new GameObject("HoverFrame");
        h.transform.SetParent(worldRoot, false);
        hover = h.transform;

        hoverValid = TDVisuals.Mat(new Color(0.40f, 1f, 0.50f), 0f, 0.7f);
        hoverInvalid = TDVisuals.Mat(new Color(1f, 0.35f, 0.30f), 0f, 0.7f);

        float c = map.Cell;
        float t = 0.16f;
        GameObject[] edges = new GameObject[4];
        edges[0] = TDVisuals.Box(hover, "N", new Vector3(0f, 0f, c * 0.46f), new Vector3(c, 0.08f, t), hoverValid);
        edges[1] = TDVisuals.Box(hover, "S", new Vector3(0f, 0f, -c * 0.46f), new Vector3(c, 0.08f, t), hoverValid);
        edges[2] = TDVisuals.Box(hover, "E", new Vector3(c * 0.46f, 0f, 0f), new Vector3(t, 0.08f, c), hoverValid);
        edges[3] = TDVisuals.Box(hover, "W", new Vector3(-c * 0.46f, 0f, 0f), new Vector3(t, 0.08f, c), hoverValid);
        hoverRends = new Renderer[4];
        for (int i = 0; i < 4; i++) hoverRends[i] = edges[i].GetComponent<Renderer>();
        h.SetActive(false);
    }

    void BeginWave()
    {
        spawnQueue = BuildWave(Wave);
        spawnIndex = 0;
        waveTime = 0f;
        Round = RoundState.WaveActive;
    }

    List<SpawnEntry> BuildWave(int wave)
    {
        List<SpawnEntry> q = new List<SpawnEntry>();
        TDBalance.WaveDef w = TDBalance.Waves[Mathf.Clamp(wave - 1, 0, TDBalance.Waves.Length - 1)];
        for (int i = 0; i < w.count; i++)
            q.Add(new SpawnEntry { mob = w.mob, time = w.startDelay + i * w.interval });
        return q;
    }

    void EndWave()
    {
        int bonus = TDBalance.RoundBonus(Wave);
        Money += bonus;
        message = "Round cleared! +$" + bonus;
        messageTimer = 2.5f;
        if (TDAudio.Instance != null) TDAudio.Instance.RoundClear();

        if (mpActive)
        {
            // Hold here until every other board clears; the host advances.
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
        Round = RoundState.Preparing;
        prepTimer = TDBalance.PrepDuration;
    }

    void TickWave()
    {
        waveTime += Time.deltaTime;
        while (spawnIndex < spawnQueue.Count && spawnQueue[spawnIndex].time <= waveTime)
        {
            SpawnMob(spawnQueue[spawnIndex].mob);
            spawnIndex++;
        }

        // Round ends only after every queued mob has spawned and the field is clear.
        if (spawnQueue.Count > 0 && spawnIndex >= spawnQueue.Count && Mobs.Count == 0) EndWave();
    }

    void SpawnMob(string id)
    {
        MobDef def = MobCatalog.Get(id);
        GameObject go = new GameObject("Mob_" + id);
        go.transform.SetParent(mobsRoot, false);
        Mob m = go.AddComponent<Mob>();
        m.Init(def, map.Waypoints, this,
               TDBalance.HealthMult(Wave) * TDBalance.HealthMultiplier(CurrentDifficulty),
               TDBalance.SpeedMult(Wave));
        m.NetId = nextMobId++;
        Mobs.Add(m);
    }

    public void OnMobKilled(Mob mob)
    {
        Mobs.Remove(mob);
        Money += TDBalance.KillReward(Wave);
    }

    public void OnMobLeaked(Mob mob)
    {
        Mobs.Remove(mob);
        Lives -= mob.Def.leakDamage;
        if (Lives <= 0)
        {
            Lives = 0;
            if (mpActive) EliminateLocal();
            else { RestoreTimeScale(); State = GameState.GameOver; }
        }
        else if (mpActive && MatchSync.Instance != null)
        {
            MatchSync.Instance.ReportLocal(Lives, Money, Wave, cleared, eliminated);
        }
    }

    // -------------------------------------------------------------- update
    void Update()
    {
        if (messageTimer > 0f)
        {
            messageTimer -= Time.deltaTime;
            if (messageTimer <= 0f) message = "";
        }

        if (State != GameState.Playing)
        {
            if (hover != null) hover.gameObject.SetActive(false);

            // Settings can be opened over the main menu; Esc closes it there.
            if (settingsOpen && Input.GetKeyDown(KeyCode.Escape)) CloseSettings();

            if (State == GameState.MainMenu || State == GameState.DifficultySelect)
                UpdateMenuBackdrop();
            return;
        }

        // Paused: freeze gameplay input and simulation, run only the menu.
        if (paused)
        {
            HandlePauseInput();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (merging) { merging = false; return; }
            OpenPause();
            return;
        }

        if (mpActive)
        {
            // The match decides when waves start; this countdown is display-only.
            if (Round == RoundState.Preparing)
            {
                if (prepTimer > 0f) prepTimer -= Time.deltaTime;
            }
            else
            {
                TickWave();
            }
        }
        else if (Round == RoundState.Preparing)
        {
            prepTimer -= Time.deltaTime;
            if (prepTimer <= 0f) BeginWave();
        }
        else
        {
            TickWave();
        }

        UpdateCamera(Time.deltaTime);
        HandleMouse();
        UpdateHover();
        if (mpActive) { UpdateRemoteBoards(); UpdateSpectate(); }
    }

    void UpdateHover()
    {
        if (!ViewingOwnBoard)
        {
            if (hover != null) hover.gameObject.SetActive(false);
            return;
        }

        if (hover == null) return;
        if (MouseOverUI()) { hover.gameObject.SetActive(false); return; }

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, Vector3.zero);
        float d;
        if (!plane.Raycast(ray, out d)) { hover.gameObject.SetActive(false); return; }
        Vector3 p = ray.GetPoint(d);

        int x, y;
        map.WorldToCell(p, out x, out y);
        if (!map.InBounds(x, y)) { hover.gameObject.SetActive(false); return; }

        bool valid = map.IsBuildable(x, y) && !towers.ContainsKey(map.Idx(x, y));
        Material m = valid ? hoverValid : hoverInvalid;
        for (int i = 0; i < hoverRends.Length; i++) hoverRends[i].sharedMaterial = m;
        hover.position = map.CellCenter(x, y) + Vector3.up * 0.05f;
        hover.gameObject.SetActive(true);
    }

    void ClearWorld()
    {
        Mobs.Clear();
        towers.Clear();
        Selected = null;
        merging = false;
        hover = null;
        if (worldRoot != null) { Destroy(worldRoot.gameObject); worldRoot = null; }
    }

    bool MouseOverUI()
    {
        float mx = Input.mousePosition.x;
        float my = Screen.height - Input.mousePosition.y;
        if (my < 78f || my > Screen.height - 40f) return true;
        if (Selected != null && mx < 390f && my > 66f && my < 190f) return true;
        return false;
    }

    void HandleMouse()
    {
        if (!ViewingOwnBoard) { SetSelected(null); return; }

        if (Input.GetMouseButtonDown(1))
        {
            if (merging) { merging = false; message = "Merge cancelled"; messageTimer = 1.5f; }
            else SetSelected(null);
            return;
        }

        if (MouseOverUI() || !Input.GetMouseButtonDown(0)) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, Vector3.zero);
        float d;
        if (!plane.Raycast(ray, out d)) return;
        Vector3 p = ray.GetPoint(d);

        int x, y;
        map.WorldToCell(p, out x, out y);
        if (!map.InBounds(x, y)) { if (!merging) SetSelected(null); return; }

        int idx = map.Idx(x, y);
        Tower t = towers.ContainsKey(idx) ? towers[idx] : null;

        if (merging)
        {
            if (Selected == null) { merging = false; return; }
            if (t == null) { message = "Select a tower to merge with"; messageTimer = 1.5f; return; }
            if (t == Selected) return;
            if (t.Tier == Selected.Tier) { if (TryMerge(Selected, t)) merging = false; }
            else { message = "Need a Tier " + Selected.Tier + " tower"; messageTimer = 2f; }
            return;
        }

        if (t != null)
        {
            if (Selected == t) SetSelected(null);
            else SetSelected(t);
        }
        else
        {
            if (Selected != null) { SetSelected(null); return; }
            TryBuild(x, y);
        }
    }

    void SetSelected(Tower t)
    {
        if (Selected != null) Selected.SetSelected(false);
        Selected = t;
        if (Selected != null) Selected.SetSelected(true);
        else merging = false;
    }

    void TryBuild(int x, int y)
    {
        if (!map.IsBuildable(x, y)) return;
        int idx = map.Idx(x, y);
        if (towers.ContainsKey(idx)) return;

        if (Money < TDBalance.BuildCost)
        {
            message = "Not enough money ($" + TDBalance.BuildCost + ")";
            messageTimer = 1.5f;
            return;
        }
        Money -= TDBalance.BuildCost;
        CreateTower(x, y, TowerCatalog.RandomType(), 1);
        if (TDAudio.Instance != null) TDAudio.Instance.Build();
    }

    Tower CreateTower(int x, int y, TowerType type, int tier)
    {
        GameObject go = new GameObject("Tower_" + type + "_T" + tier);
        go.transform.SetParent(towersRoot, false);
        go.transform.position = map.CellCenter(x, y);
        Tower t = go.AddComponent<Tower>();
        t.Setup(type, tier, x, y);
        towers[map.Idx(x, y)] = t;
        return t;
    }

    bool TryMerge(Tower a, Tower b)
    {
        if (a == null || b == null || a == b) return false;
        if (a.Tier != b.Tier || a.Tier >= TowerCatalog.MaxTier) return false;

        if (Money < TDBalance.MergeCost)
        {
            message = "Not enough money to merge ($" + TDBalance.MergeCost + ")";
            messageTimer = 1.8f;
            return false;
        }
        Money -= TDBalance.MergeCost;

        int cx = a.CellX, cy = a.CellY;
        towers.Remove(map.Idx(a.CellX, a.CellY));
        towers.Remove(map.Idx(b.CellX, b.CellY));
        a.SetSelected(false);
        Destroy(a.gameObject);
        Destroy(b.gameObject);

        TowerType result = TowerCatalog.RandomType();
        Tower nt = CreateTower(cx, cy, result, a.Tier + 1);
        SetSelected(nt);
        if (TDAudio.Instance != null) TDAudio.Instance.Merge();
        message = "Merged into " + nt.DisplayName + " (Tier " + nt.Tier + ")  -$" + TDBalance.MergeCost;
        messageTimer = 2f;
        return true;
    }

    // --------------------------------------------------------------- GUI
    void OnGUI()
    {
        if (State == GameState.MainMenu) DrawMenu();
        else if (State == GameState.DifficultySelect) DrawDifficulty();
        else if (State == GameState.TowerViewer) DrawTowerViewer();
        else if (State == GameState.MobViewer) DrawMobViewer();
        else if (State == GameState.MultiplayerMenu || State == GameState.Lobby) DrawMultiplayer();
        else
        {
            DrawHud();
            if (State == GameState.GameOver) DrawEnd(false);
            else if (State == GameState.Victory) DrawEnd(true);
            DrawPauseMenu();
        }

        // Drawn last so it sits on top of whichever screen opened it.
        if (settingsOpen) DrawSettings();
    }

    GUIStyle Style(int size, TextAnchor anchor, Color color)
    {
        GUIStyle s = new GUIStyle(GUI.skin.label);
        s.fontSize = size;
        s.alignment = anchor;
        s.normal.textColor = color;
        return s;
    }

    /// <summary>
    /// Menu button drawn on a light construction-paper texture with black text
    /// (hover/active use slightly darker sheets).
    /// </summary>
    GUIStyle PaperButton(int size)
    {
        GUIStyle s = new GUIStyle(GUI.skin.button);
        s.fontSize = size;
        s.alignment = TextAnchor.MiddleCenter;
        s.padding = new RectOffset(10, 10, 6, 6);
        s.border = new RectOffset(0, 0, 0, 0);   // stretch the sheet flat, no 9-slice edges
        s.normal.background = TDTextures.Paper();
        s.hover.background = TDTextures.PaperHover();
        s.active.background = TDTextures.PaperPressed();
        s.focused.background = TDTextures.Paper();
        s.normal.textColor = Color.black;
        s.hover.textColor = Color.black;
        s.active.textColor = new Color(0.14f, 0.14f, 0.14f);
        s.focused.textColor = Color.black;
        return s;
    }

    /// <summary>Soft vertical scrim for the menus so the board backdrop shows through.</summary>
    void DrawMenuOverlay()
    {
        Color old = GUI.color;
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), TDTextures.MenuFade());
        GUI.color = old;
    }

    void DrawMenu()
    {
        if (settingsOpen && !settingsFromPause) return;   // the settings overlay draws its own backdrop

        DrawMenuOverlay();

        GUI.Label(new Rect(0, Screen.height * 0.20f, Screen.width, 70), "SNACK TOWER DEFENSE", Style(46, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.4f)));

        float bw = 280f, bh = 48f, gap = 11f;
        float bx = (Screen.width - bw) * 0.5f;
        float by = Screen.height * 0.30f;
        GUIStyle btn = PaperButton(22);

        if (GUI.Button(new Rect(bx, by, bw, bh), "Single Player", btn))
        {
            Click();
            State = GameState.DifficultySelect;
        }
        if (GUI.Button(new Rect(bx, by + 1f * (bh + gap), bw, bh), "Multiplayer", btn))
        {
            Click();
            EnterMultiplayer();
        }
        if (GUI.Button(new Rect(bx, by + 2f * (bh + gap), bw, bh), "Tower Viewer", btn))
        {
            Click();
            OpenTowerViewer();
        }
        if (GUI.Button(new Rect(bx, by + 3f * (bh + gap), bw, bh), "Mob Viewer", btn))
        {
            Click();
            OpenMobViewer();
        }
        if (GUI.Button(new Rect(bx, by + 4f * (bh + gap), bw, bh), "Settings", btn))
        {
            Click();
            OpenSettings(false);
        }
        if (GUI.Button(new Rect(bx, by + 5f * (bh + gap), bw, bh), "Quit", btn))
        {
            Click();
            QuitGame();
        }
    }

    void DrawDifficulty()
    {
        DrawMenuOverlay();

        GUI.Label(new Rect(0f, Screen.height * 0.11f, Screen.width, 60f), "SELECT DIFFICULTY",
            Style(40, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.4f)));

        float bw = 300f, bh = 52f, gap = 12f;
        float bx = (Screen.width - bw) * 0.5f - 120f;
        float by = Screen.height * 0.30f;
        GUIStyle btn = PaperButton(22);

        // Dark strip behind the blurbs keeps the coloured text readable over the board.
        Color old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.45f);
        GUI.DrawTexture(new Rect(bx + bw + 10f, by - 4f, 320f, 4f * (bh + gap) + 4f), Texture2D.whiteTexture);
        GUI.color = old;

        for (int i = 0; i < 4; i++)
        {
            Difficulty d = (Difficulty)i;
            float y = by + i * (bh + gap);
            if (GUI.Button(new Rect(bx, y, bw, bh), TDBalance.DifficultyName(d), btn))
            {
                Click();
                CurrentDifficulty = d;
                StartRun();
            }
            GUI.Label(new Rect(bx + bw + 18f, y, 300f, bh), TDBalance.DifficultyBlurb(d),
                Style(16, TextAnchor.MiddleLeft, TDBalance.DifficultyColour(d)));
        }

        if (GUI.Button(new Rect(bx, by + 4f * (bh + gap) + 12f, bw, 46f), "Back", btn))
        {
            Click();
            State = GameState.MainMenu;
        }

        GUI.Label(new Rect(0f, Screen.height - 30f, Screen.width, 24f),
            "Esc to go back", Style(13, TextAnchor.MiddleCenter, new Color(0.85f, 0.88f, 0.92f)));

        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            State = GameState.MainMenu;
    }

    void DrawHud()
    {
        GUIStyle hud = Style(18, TextAnchor.MiddleLeft, Color.white);
        GUI.Label(new Rect(12, 10, 300, 24), "Money: $" + Money, hud);
        GUI.Label(new Rect(12, 34, 300, 24), "Lives: " + Lives, hud);
        GUI.Label(new Rect(12, 58, 300, 24), "Tower: $" + TDBalance.BuildCost, Style(15, TextAnchor.MiddleLeft, new Color(0.8f, 0.9f, 1f)));
        GUI.Label(new Rect(Screen.width - 240, 10, 228, 24), "Wave: " + Mathf.Min(Wave, TDBalance.TotalWaves) + " / " + TDBalance.TotalWaves, Style(18, TextAnchor.MiddleRight, Color.white));
        GUI.Label(new Rect(Screen.width - 240, 34, 228, 24), "Enemies: " + Mobs.Count, Style(18, TextAnchor.MiddleRight, new Color(0.85f, 0.85f, 0.9f)));
        if (mpActive)
            GUI.Label(new Rect(Screen.width - 240, 58, 228, 24), "Multiplayer: " + mpPlayerCount, Style(16, TextAnchor.MiddleRight, new Color(0.65f, 0.85f, 1f)));
        if (mpActive && !ViewingOwnBoard)
            GUI.Label(new Rect(0, 84, Screen.width, 26), "SPECTATING " + SpectateName() + "   -   press 0 for your board",
                Style(18, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.5f)));

        if (State == GameState.Playing)
        {
            string line = Round == RoundState.Preparing
                ? "Next wave in " + Mathf.CeilToInt(prepTimer) + "s   (SPACE to start now)"
                : "Wave " + Wave + " in progress";
            GUI.Label(new Rect(0, 8, Screen.width, 26), line, Style(20, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.7f)));
        }

        if (!string.IsNullOrEmpty(message))
            GUI.Label(new Rect(0, 40, Screen.width, 26), message, Style(16, TextAnchor.MiddleCenter, new Color(0.6f, 1f, 0.6f)));

        if (Selected != null)
        {
            TowerTierStats s = Selected.Stats;
            GUI.Box(new Rect(12, 70, 360, 118), GUIContent.none);
            GUI.Label(new Rect(20, 74, 344, 58),
                Selected.DisplayName + "  -  Tier " + Selected.Tier + "\n" +
                "Damage " + s.damage + "    Rate " + s.fireInterval.ToString("0.00") + "s",
                Style(14, TextAnchor.UpperLeft, Color.white));

            if (merging)
            {
                GUI.Label(new Rect(20, 132, 344, 20),
                    "Select a Tier " + Selected.Tier + " tower to merge with",
                    Style(13, TextAnchor.UpperLeft, new Color(1f, 0.9f, 0.4f)));
                if (GUI.Button(new Rect(20, 154, 120, 26), "Cancel"))
                {
                    if (TDAudio.Instance != null) TDAudio.Instance.Click();
                    merging = false;
                }
            }
            else if (Selected.Tier < TowerCatalog.MaxTier)
            {
                bool canAfford = Money >= TDBalance.MergeCost;
                GUI.enabled = canAfford;
                if (GUI.Button(new Rect(20, 132, 150, 28), "Merge ($" + TDBalance.MergeCost + ")"))
                {
                    if (TDAudio.Instance != null) TDAudio.Instance.Click();
                    merging = true;
                    message = "Select another Tier " + Selected.Tier + " tower";
                    messageTimer = 2.5f;
                }
                GUI.enabled = true;
            }
            else
            {
                GUI.Label(new Rect(20, 132, 344, 20), "Max tier reached", Style(13, TextAnchor.UpperLeft, new Color(0.8f, 0.8f, 0.8f)));
            }
        }

        GUI.Label(new Rect(0, Screen.height - 30, Screen.width, 24),
            "Left-click: build ($" + TDBalance.BuildCost + ") / select   |   Merge ($" + TDBalance.MergeCost + ") -> same-tier   |   WASD: move   |   Middle-drag: rotate   |   Scroll: zoom   |   M: music   |   Esc: menu",
            Style(13, TextAnchor.MiddleCenter, new Color(0.8f, 0.8f, 0.8f)));

        DrawBossBar();
    }

    /// <summary>Big centred boss bar at the top of the screen while a boss is alive.
    /// Bosses have no floating bar, so this is the only read on their health.</summary>
    void DrawBossBar()
    {
        if (State != GameState.Playing) return;

        string name;
        float frac;
        if (ViewingOwnBoard)
        {
            Mob boss = null;
            for (int i = 0; i < Mobs.Count; i++)
            {
                Mob m = Mobs[i];
                if (m != null && m.Def != null && m.Def.archetype == MobArchetype.Boss) { boss = m; break; }
            }
            if (boss == null) return;
            name = boss.Def.displayName;
            frac = boss.MaxHealth > 0f ? Mathf.Clamp01(boss.Health / boss.MaxHealth) : 0f;
        }
        else
        {
            RemoteBoard rb = BoardForSlot(viewSlot);
            if (rb == null || !rb.TryGetBoss(out name, out frac)) return;
        }

        frac = Mathf.Clamp01(frac);
        float w = Mathf.Min(Screen.width * 0.5f, 640f);
        float h = 30f;
        float x = (Screen.width - w) * 0.5f;
        float y = 44f;

        Color old = GUI.color;
        GUI.color = new Color(0.05f, 0.05f, 0.07f, 0.94f);
        GUI.DrawTexture(new Rect(x - 4f, y - 4f, w + 8f, h + 8f), Texture2D.whiteTexture);
        GUI.color = new Color(0.16f, 0.16f, 0.20f, 1f);
        GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
        GUI.color = new Color(0.86f, 0.16f, 0.14f, 1f);
        GUI.DrawTexture(new Rect(x, y, w * frac, h), Texture2D.whiteTexture);
        GUI.color = old;

        GUI.Label(new Rect(x, y, w, h),
            name.ToUpper() + "   " + Mathf.CeilToInt(frac * 100f) + "%",
            Style(18, TextAnchor.MiddleCenter, Color.white));
    }

    void DrawEnd(bool won)
    {
        Color old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.72f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = old;

        GUI.Label(new Rect(0, Screen.height * 0.3f, Screen.width, 70),
            won ? "YOU WIN!" : "GAME OVER",
            Style(48, TextAnchor.MiddleCenter, won ? new Color(0.45f, 1f, 0.5f) : new Color(1f, 0.4f, 0.4f)));

        GUI.Label(new Rect(0, Screen.height * 0.42f, Screen.width, 30),
            "Reached wave " + Mathf.Min(Wave, TDBalance.TotalWaves) + " of " + TDBalance.TotalWaves + "     Money: $" + Money,
            Style(20, TextAnchor.MiddleCenter, Color.white));

        float bw = 220f, bh = 52f;
        float bx = (Screen.width - bw) * 0.5f;

        if (mpActive)
        {
            DrawScoreboard();
            if (GUI.Button(new Rect(bx, Screen.height * 0.74f, bw, bh), "Back to Lobby", PaperButton(22)))
            {
                Click();
                ReturnToLobby();
            }
            return;
        }

        GUIStyle btn = PaperButton(22);
        if (GUI.Button(new Rect(bx - 120f, Screen.height * 0.55f, bw, bh), "Retry", btn))
        {
            Click();
            StartRun();
        }
        if (GUI.Button(new Rect(bx + 120f, Screen.height * 0.55f, bw, bh), "Main Menu", btn))
        {
            Click();
            ReturnToMainMenu();
        }
    }
}
