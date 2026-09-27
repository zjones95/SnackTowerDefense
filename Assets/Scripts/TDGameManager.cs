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
    public int GoldGenerated { get; private set; }   // total gold paid by Gold towers
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
    // bottom HUD chrome: the controls legend sits directly above the build toolbar
    private const float ToolbarButtonH = 34f;
    private const float ToolbarBottomMargin = 6f;
    private const float ToolbarLegendH = 22f;
    private float camYaw = GameYaw, camPitch = GamePitch;
    private float camDist = GameDist;
    private readonly float camDistMin = 12f, camDistMax = 90f;
    // menu backdrop has its OWN camera state so it never leaks into a run
    private float menuYaw = 35f;
    private const float MenuPitch = 38f, MenuDist = 31f, MenuOrbitDegPerSec = 3f;
    private Transform worldRoot, towersRoot, mobsRoot;
    private readonly Dictionary<int, Tower> towers = new Dictionary<int, Tower>();

    private float prepTimer;
    private float waveIntroTimer;   // shows the wave title + modifiers at wave start
    private string message = "";
    private float messageTimer;
    private bool merging;
    private bool building;      // build mode: click a cell to place a random tower
    private bool goldBuilding;  // Gold mode: click a cell to place a Gold tower
    private bool reRolling;     // re-roll mode: click a tower one tier below
    private TowerGhost ghost;   // "?" placement preview shown in build mode
    private Transform hover;
    private Renderer[] hoverRends;
    private Material hoverValid, hoverInvalid, hoverSelected;

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
        // Keep simulating when the window/tab loses focus, so a multiplayer host
        // that alt-tabs away doesn't freeze its board or stall the wave clock.
        Application.runInBackground = true;
        // Let a throttled (background) frame advance closer to real time, so a
        // WebGL host in a hidden tab doesn't slow the wave clock to a crawl.
        Time.maximumDeltaTime = 1f;
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
        cam.backgroundColor = new Color(0.12f, 0.14f, 0.17f);

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
        sun.intensity = 0.95f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.40f;                                // much lighter, softer shadows
        sun.shadowBias = 0.05f;
        sun.shadowNormalBias = 0.4f;
        sun.transform.rotation = Quaternion.Euler(33f, 35f, 0f);   // lower sun: longer, gentler shadows

        // tiny cool fill so shadows aren't pitch black
        GameObject fillGO = new GameObject("Fill");
        Light fill = fillGO.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.color = new Color(0.55f, 0.62f, 0.78f);
        fill.intensity = 0.16f;
        fill.shadows = LightShadows.None;
        fillGO.transform.rotation = Quaternion.Euler(28f, -140f, 0f);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.19f, 0.18f, 0.17f);
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
        building = false;
        goldBuilding = false;
        reRolling = false;
        cleared = false;
        eliminated = false;
        if (worldRoot != null) Destroy(worldRoot.gameObject);

        Money = TDBalance.StartMoney;
        GoldGenerated = 0;
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
        BuildGhost();
        TDBoardBuilder.BuildRoom(worldRoot, map, boardOffset);
    }

    void BuildHover()
    {
        GameObject h = new GameObject("HoverFrame");
        h.transform.SetParent(worldRoot, false);
        hover = h.transform;

        hoverValid = TDVisuals.Mat(new Color(0.40f, 1f, 0.50f), 0f, 0.7f);
        hoverInvalid = TDVisuals.Mat(new Color(1f, 0.35f, 0.30f), 0f, 0.7f);
        hoverSelected = TDVisuals.Mat(new Color(1f, 0.85f, 0.15f), 0f, 0.7f);   // yellow

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

    void BuildGhost()
    {
        ghost = SnackArt.BuildGhost(worldRoot);
        ghost.root.gameObject.SetActive(false);
    }

    void BeginWave()
    {
        spawnQueue = BuildWave(Wave);
        spawnIndex = 0;
        waveTime = 0f;
        Round = RoundState.WaveActive;
        waveIntroTimer = 3.5f;
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
        BeginWave();   // no prep wait: the next wave starts the moment the last mob dies
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
        // Flagged bosses (Granola Mom) end the run outright if they leak.
        Lives -= mob.Def.instantLossOnLeak ? Lives : mob.Def.leakDamage;
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

    /// <summary>Adds money to the local board (Gold towers award this on a hit).
    /// Money is per-peer, so this is safe in multiplayer.</summary>
    public void AwardMoney(int amount)
    {
        if (amount <= 0) return;
        Money += amount;
        GoldGenerated += amount;
    }

    // -------------------------------------------------------------- update
    void Update()
    {
        if (messageTimer > 0f)
        {
            messageTimer -= Time.deltaTime;
            if (messageTimer <= 0f) message = "";
        }

        if (waveIntroTimer > 0f) waveIntroTimer -= Time.deltaTime;

        if (State != GameState.Playing)
        {
            HideHover();

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
            // Esc first cancels an active build/merge/re-roll mode, then pauses.
            if (building || goldBuilding || merging || reRolling) CancelMode();
            else OpenPause();
            return;
        }

        HandleHotkeys();

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

    /// <summary>
    /// Single shared tile frame. Priority: a selected tower shows the frame in
    /// YELLOW pinned to its cell; otherwise, while a build mode is active
    /// (random or Gold), it follows the mouse green/red and drives the ghost.
    /// Hidden the rest of the time.
    /// </summary>
    void UpdateHover()
    {
        if (!ViewingOwnBoard || hover == null) { HideHover(); return; }

        // 1) Selection wins: yellow frame on the selected tower's cell.
        if (Selected != null)
        {
            SetHoverMat(hoverSelected);
            hover.position = map.CellCenter(Selected.CellX, Selected.CellY) + Vector3.up * 0.05f;
            hover.gameObject.SetActive(true);
            if (ghost != null) ghost.root.gameObject.SetActive(false);
            return;
        }

        // 2) Build modes only (random build or Gold).
        bool buildMode = building || goldBuilding;
        if (!buildMode || MouseOverUI()) { HideHover(); return; }

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, Vector3.zero);
        float d;
        if (!plane.Raycast(ray, out d)) { HideHover(); return; }
        Vector3 p = ray.GetPoint(d);

        int x, y;
        map.WorldToCell(p, out x, out y);
        if (!map.InBounds(x, y)) { HideHover(); return; }

        bool placeable = map.IsBuildable(x, y) && !towers.ContainsKey(map.Idx(x, y));
        SetHoverMat(placeable ? hoverValid : hoverInvalid);
        hover.position = map.CellCenter(x, y) + Vector3.up * 0.05f;
        hover.gameObject.SetActive(true);

        // The "?" ghost follows the mouse while any build mode is active.
        if (ghost == null) return;
        bool affordable = Money >= TDBalance.BuildCost;
        bool goldOk = !goldBuilding || GoldTowerCount() < TowerCatalog.MaxGoldTowers;
        ghost.root.position = map.CellCenter(x, y) + Vector3.up * 0.02f;
        ghost.SetValid(placeable && affordable && goldOk);
        ghost.root.gameObject.SetActive(true);
    }

    void SetHoverMat(Material m)
    {
        for (int i = 0; i < hoverRends.Length; i++) hoverRends[i].sharedMaterial = m;
    }

    void HideHover()
    {
        if (hover != null) hover.gameObject.SetActive(false);
        if (ghost != null) ghost.root.gameObject.SetActive(false);
    }

    void ClearWorld()
    {
        Mobs.Clear();
        towers.Clear();
        Selected = null;
        merging = false;
        building = false;
        goldBuilding = false;
        reRolling = false;
        hover = null;
        ghost = null;
        if (worldRoot != null) { Destroy(worldRoot.gameObject); worldRoot = null; }
    }

    bool MouseOverUI()
    {
        float mx = Input.mousePosition.x;
        float my = Screen.height - Input.mousePosition.y;
        if (my < 84f) return true;                                    // top stats (top-left build buttons removed)
        if (my > Screen.height - ToolbarBottomMargin - ToolbarButtonH - ToolbarLegendH) return true;  // bottom legend + toolbar
        if (Selected != null && mx < 460f && my < 446f) return true;  // selected-tower panel
        return false;
    }

    void HandleMouse()
    {
        if (!ViewingOwnBoard) { SetSelected(null); return; }

        if (Input.GetMouseButtonDown(1))
        {
            if (building || goldBuilding || merging || reRolling) CancelMode();
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
        if (!map.InBounds(x, y))
        {
            if (!building && !goldBuilding && !merging && !reRolling) SetSelected(null);
            return;
        }

        int idx = map.Idx(x, y);
        Tower t = towers.ContainsKey(idx) ? towers[idx] : null;

        if (building)
        {
            TryBuild(x, y);   // stays in build mode for repeated placement
            return;
        }

        if (goldBuilding)
        {
            TryBuildGold(x, y);   // stays in Gold mode for repeated placement
            return;
        }

        if (merging)
        {
            if (Selected == null) { merging = false; return; }
            if (t == null) { message = "Select a tower to merge with"; messageTimer = 1.5f; return; }
            if (t == Selected) return;
            if (Selected.Type == TowerType.Gold || t.Type == TowerType.Gold)
            { message = "Gold towers upgrade with U, not merge"; messageTimer = 2f; merging = false; }
            else if (t.Tier == Selected.Tier) { if (TryMerge(Selected, t)) merging = false; }
            else { message = "Need a Tier " + Selected.Tier + " tower"; messageTimer = 2f; }
            return;
        }

        if (reRolling)
        {
            if (Selected == null) { reRolling = false; return; }
            if (t == null) { message = "Select a Tier " + (Selected.Tier - 1) + " tower to re-roll with"; messageTimer = 1.8f; return; }
            if (t == Selected) return;
            if (t.Tier == Selected.Tier - 1) { if (TryReRoll(Selected, t)) reRolling = false; }
            else { message = "Need a Tier " + (Selected.Tier - 1) + " tower"; messageTimer = 2f; }
            return;
        }

        // Not in a mode: left-click only selects (or deselects) a tower, never builds.
        if (t != null)
        {
            if (Selected == t) SetSelected(null);
            else SetSelected(t);
        }
        else if (Selected != null)
        {
            SetSelected(null);
        }
    }

    // --------------------------------------------------------- action modes
    void HandleHotkeys()
    {
        if (Input.GetKeyDown(KeyCode.B)) ToggleBuildMode();
        if (Input.GetKeyDown(KeyCode.G)) ToggleGoldBuild();
        if (Input.GetKeyDown(KeyCode.E)) TryStartMerge();
        if (Input.GetKeyDown(KeyCode.U)) TryStartAscend();
        if (Input.GetKeyDown(KeyCode.R)) TryStartReRoll();
        if (Input.GetKeyDown(KeyCode.X)) TrySell();
    }

    void CancelMode()
    {
        if (building) { building = false; message = "Build cancelled"; }
        else if (goldBuilding) { goldBuilding = false; message = "Gold build cancelled"; }
        else if (merging) { merging = false; message = "Merge cancelled"; }
        else if (reRolling) { reRolling = false; message = "Re-roll cancelled"; }
        messageTimer = 1.5f;
    }

    void ToggleBuildMode()
    {
        if (!ViewingOwnBoard) return;   // spectating is read-only
        if (building) { CancelMode(); return; }
        if (TDAudio.Instance != null) TDAudio.Instance.Click();
        building = true;
        goldBuilding = false;
        SetSelected(null);   // clears the panel, and any merge/re-roll mode
        message = "Build mode: click a tile to place ($" + TDBalance.BuildCost + ").  Right-click / Esc cancels.";
        messageTimer = 3f;
    }

    /// <summary>Gold placement mode (hotkey G): same ghost UX as build mode,
    /// but the placed tower is always Gold. Capped at TowerCatalog.MaxGoldTowers.</summary>
    void ToggleGoldBuild()
    {
        if (!ViewingOwnBoard) return;   // spectating is read-only
        if (goldBuilding) { CancelMode(); return; }
        if (GoldTowerCount() >= TowerCatalog.MaxGoldTowers)
        {
            message = "Gold tower limit reached (" + TowerCatalog.MaxGoldTowers + ")";
            messageTimer = 2f;
            return;
        }
        if (TDAudio.Instance != null) TDAudio.Instance.Click();
        goldBuilding = true;
        building = false;
        SetSelected(null);   // clears the panel, and any merge/re-roll mode
        message = "Gold mode: click a tile to place a Gold Coin ($" + TDBalance.BuildCost + ").  "
                + GoldTowerCount() + "/" + TowerCatalog.MaxGoldTowers + " built.  Right-click / Esc cancels.";
        messageTimer = 3f;
    }

    void TryStartMerge()
    {
        if (!ViewingOwnBoard) return;
        if (Selected == null) { message = "Select a tower first"; messageTimer = 1.5f; return; }
        if (Selected.Type == TowerType.Gold) { message = "Gold towers upgrade with U, not merge"; messageTimer = 2f; return; }
        if (!CanMergeTier(Selected.Tier))
        {
            int up = TDBalance.AscendCost(Selected.Tier);
            message = up > 0
                ? "Tier " + Selected.Tier + " ascends with U ($" + up + ")"
                : "Max tier reached";
            messageTimer = 1.8f;
            return;
        }
        int cost = MergeCostFor(Selected.Tier);
        if (Money < cost)
        {
            message = "Not enough money to merge ($" + cost + ")";
            messageTimer = 1.8f;
            return;
        }
        if (TDAudio.Instance != null) TDAudio.Instance.Click();
        building = false;
        goldBuilding = false;
        merging = true;
        reRolling = false;
        message = Selected.Tier == TowerCatalog.MaxTier - 1
            ? "Select another Tier 6 tower to fuse into a Tier 7"
            : "Select another Tier " + Selected.Tier + " tower";
        messageTimer = 2.5f;
    }

    /// <summary>A 2:1 merge is allowed at tiers 1-3 (cheap), and at T6 where it
    /// becomes the T6+T6 -> T7 fusion. T7 is terminal.</summary>
    static bool CanMergeTier(int tier)
    {
        return tier <= TowerCatalog.MaxMergeTier || tier == TowerCatalog.MaxTier - 1;
    }

    /// <summary>Money for a merge at this tier: the cheap cost below T4, else the fusion cost.</summary>
    static int MergeCostFor(int tier)
    {
        return tier == TowerCatalog.MaxTier - 1 ? TDBalance.FuseCost : TDBalance.MergeCost;
    }

    /// <summary>Cash ascension (hotkey U): upgrades the selected tier 4/5 tower in
    /// place for money, consuming no second tower. Tiers 1-3 use merging instead.</summary>
    void TryStartAscend()
    {
        if (!ViewingOwnBoard) return;
        if (Selected == null) { message = "Select a tower first"; messageTimer = 1.5f; return; }
        merging = false;   // ascension targets no second tower: leave any pick mode
        reRolling = false;
        TryAscend(Selected);
    }

    void TryStartReRoll()
    {
        if (!ViewingOwnBoard) return;
        if (Selected == null) { message = "Select a tower first"; messageTimer = 1.5f; return; }
        if (Selected.Tier < 2) { message = "A Tier 1 tower can't re-roll"; messageTimer = 1.8f; return; }
        if (Selected.Tier >= TowerCatalog.MaxTier) { message = "Tier 7 towers can't re-roll"; messageTimer = 1.8f; return; }
        if (TDAudio.Instance != null) TDAudio.Instance.Click();
        building = false;
        goldBuilding = false;
        merging = false;
        reRolling = true;
        message = "Select a Tier " + (Selected.Tier - 1) + " tower to re-roll with";
        messageTimer = 3f;
    }

    void SetSelected(Tower t)
    {
        if (Selected != null) Selected.SetSelected(false);
        Selected = t;
        if (Selected != null) Selected.SetSelected(true);
        else { merging = false; reRolling = false; }
    }

    void TryBuild(int x, int y)
    {
        if (!map.IsBuildable(x, y)) { message = "Can't build there"; messageTimer = 1.2f; return; }
        int idx = map.Idx(x, y);
        if (towers.ContainsKey(idx)) { message = "That tile is taken"; messageTimer = 1.2f; return; }

        if (Money < TDBalance.BuildCost)
        {
            message = "Not enough money ($" + TDBalance.BuildCost + ")";
            messageTimer = 1.5f;
            return;
        }
        Money -= TDBalance.BuildCost;
        CreateTower(x, y, TowerCatalog.RandomType(), 1).InvestedCost = TDBalance.BuildCost;
        if (TDAudio.Instance != null) TDAudio.Instance.Build();
        building = false;   // one tower per activation
    }

    /// <summary>Places a Gold tower (Gold mode only). Same cost as any build,
    /// but refused once the board holds <see cref="TowerCatalog.MaxGoldTowers"/>.</summary>
    void TryBuildGold(int x, int y)
    {
        if (!map.IsBuildable(x, y)) { message = "Can't build there"; messageTimer = 1.2f; return; }
        int idx = map.Idx(x, y);
        if (towers.ContainsKey(idx)) { message = "That tile is taken"; messageTimer = 1.2f; return; }

        if (GoldTowerCount() >= TowerCatalog.MaxGoldTowers)
        {
            message = "Gold tower limit reached (" + TowerCatalog.MaxGoldTowers + ")";
            messageTimer = 2f;
            return;
        }
        if (Money < TDBalance.BuildCost)
        {
            message = "Not enough money ($" + TDBalance.BuildCost + ")";
            messageTimer = 1.5f;
            return;
        }
        Money -= TDBalance.BuildCost;
        CreateTower(x, y, TowerType.Gold, 1).InvestedCost = TDBalance.BuildCost;
        if (TDAudio.Instance != null) TDAudio.Instance.Build();
        goldBuilding = false;   // one tower per activation
    }

    int GoldTowerCount()
    {
        int n = 0;
        foreach (Tower t in towers.Values)
            if (t != null && t.Type == TowerType.Gold) n++;
        return n;
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
        if (a.Tier != b.Tier) return false;
        // 2:1 merging consumes tiers up to MaxMergeTier (T3+T3 -> T4 is the top);
        // at T6 the same action becomes the T6+T6 -> random T7 fusion. T7 is terminal.
        bool fuse = a.Tier == TowerCatalog.MaxTier - 1;
        if (!fuse && a.Tier > TowerCatalog.MaxMergeTier) return false;
        if (a.Tier >= TowerCatalog.MaxTier) return false;
        if (a.Type == TowerType.Gold || b.Type == TowerType.Gold) return false;   // Gold upgrades with cash

        int cost = fuse ? TDBalance.FuseCost : TDBalance.MergeCost;
        if (Money < cost)
        {
            message = "Not enough money to merge ($" + cost + ")";
            messageTimer = 1.8f;
            return false;
        }
        Money -= cost;

        int cx = a.CellX, cy = a.CellY;
        int invested = a.InvestedCost + b.InvestedCost + cost;
        towers.Remove(map.Idx(a.CellX, a.CellY));
        towers.Remove(map.Idx(b.CellX, b.CellY));
        a.SetSelected(false);
        Destroy(a.gameObject);
        Destroy(b.gameObject);

        TowerType result = fuse ? TowerCatalog.RandomT7Type() : TowerCatalog.RandomType();
        Tower nt = CreateTower(cx, cy, result, a.Tier + 1);
        nt.InvestedCost = invested;
        SetSelected(nt);
        if (TDAudio.Instance != null) TDAudio.Instance.Merge();
        message = fuse
            ? "Fused into " + nt.DisplayName + " (Tier 7)  -$" + cost
            : "Merged into " + nt.DisplayName + " (Tier " + nt.Tier + ")  -$" + cost;
        messageTimer = 2f;
        return true;
    }

    /// <summary>Cash cost to upgrade the tower one tier: Gold upgrades 1->2->3,
    /// other towers ascend 4->5->6. 0 means no cash upgrade is available.</summary>
    static int UpgradeCost(Tower t)
    {
        if (t == null) return 0;
        return t.Type == TowerType.Gold ? TDBalance.GoldUpgradeCost(t.Tier)
                                        : TDBalance.AscendCost(t.Tier);
    }

    /// <summary>
    /// Cash upgrade: promotes a tower in place to tier + 1 for money, keeping its
    /// type and cell and leaving the new tower selected. No second tower is
    /// consumed. Gold upgrades at tiers 1-2 (max 3); other towers ascend at 4-5.
    /// </summary>
    bool TryAscend(Tower t)
    {
        if (t == null) return false;

        int cost = UpgradeCost(t);
        if (cost <= 0)
        {
            if (t.Type == TowerType.Gold) message = "Gold is max tier (Tier 3)";
            else if (t.Tier >= TowerCatalog.MaxTier) message = "Already max tier";
            else if (t.Tier == TowerCatalog.MaxTier - 1) message = "Tier 6 fuses with E (merge two T6s)";
            else message = "Merge to Tier 4, then ascend with U";
            messageTimer = 1.8f;
            return false;
        }
        if (Money < cost)
        {
            message = "Not enough money to ascend ($" + cost + ")";
            messageTimer = 1.8f;
            return false;
        }

        Money -= cost;

        int cx = t.CellX, cy = t.CellY;
        TowerType type = t.Type;
        int next = t.Tier + 1;
        int invested = t.InvestedCost + cost;

        towers.Remove(map.Idx(cx, cy));
        t.SetSelected(false);
        Destroy(t.gameObject);

        Tower nt = CreateTower(cx, cy, type, next);   // same cell, same type, +1 tier
        nt.InvestedCost = invested;
        SetSelected(nt);
        if (TDAudio.Instance != null)
        {
            TDAudio.Instance.Merge();   // upgrade chime
            TDAudio.Instance.Click();
        }
        message = "Ascended " + nt.DisplayName + " to Tier " + next + "  -$" + cost;
        messageTimer = 2f;
        return true;
    }

    /// <summary>
    /// Re-rolls tower A into a different random type, consuming tower B (exactly
    /// one tier below) instead of money. A keeps its tier, cell and selection.
    /// Tier 7 fusion towers cannot re-roll - there is no random base type to
    /// produce at tier 7 - so the cap is MaxTier.
    /// </summary>
    bool TryReRoll(Tower a, Tower b)
    {
        if (a == null || b == null || a == b) return false;
        if (a.Tier < 2 || a.Tier >= TowerCatalog.MaxTier || b.Tier != a.Tier - 1) return false;

        int cx = a.CellX, cy = a.CellY;
        int tier = a.Tier;
        TowerType old = a.Type;
        int invested = a.InvestedCost + b.InvestedCost;   // b's price is the re-roll fee

        towers.Remove(map.Idx(a.CellX, a.CellY));
        towers.Remove(map.Idx(b.CellX, b.CellY));
        a.SetSelected(false);
        Destroy(a.gameObject);
        Destroy(b.gameObject);

        TowerType result = TowerCatalog.RandomTypeExcluding(old);
        Tower nt = CreateTower(cx, cy, result, tier);   // same cell, same tier, new type
        nt.InvestedCost = invested;
        SetSelected(nt);
        if (TDAudio.Instance != null) TDAudio.Instance.Merge();
        message = "Re-rolled " + TowerCatalog.Get(old).displayName + " into " + nt.DisplayName + " (Tier " + tier + ")";
        messageTimer = 2f;
        return true;
    }

    /// <summary>Sells the selected tower back for a fraction of everything sunk
    /// into it (hotkey X). Gold towers can be sold too; removing it from the
    /// board frees a Gold cap slot because GoldTowerCount recomputes.</summary>
    void TrySell()
    {
        if (!ViewingOwnBoard) return;
        if (Selected == null) { message = "Select a tower to sell"; messageTimer = 1.5f; return; }

        Tower t = Selected;
        int refund = Mathf.FloorToInt(TDBalance.SellRefund * t.InvestedCost);
        string sold = t.DisplayName + " (Tier " + t.Tier + ")";
        int idx = map.Idx(t.CellX, t.CellY);

        SetSelected(null);              // clear the selection BEFORE destroying the tower
        towers.Remove(idx);
        Destroy(t.gameObject);
        Money += refund;

        if (TDAudio.Instance != null) TDAudio.Instance.Merge();   // cash-in chime
        message = "Sold " + sold + " for $" + refund;
        messageTimer = 2f;
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
        GUI.Label(new Rect(Screen.width - 290, 58, 278, 24), "Gold Generated: $" + GoldGenerated, Style(16, TextAnchor.MiddleRight, new Color(1f, 0.85f, 0.35f)));
        if (mpActive)
            GUI.Label(new Rect(Screen.width - 240, 82, 228, 24), "Multiplayer: " + mpPlayerCount, Style(16, TextAnchor.MiddleRight, new Color(0.65f, 0.85f, 1f)));
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
            GUI.Box(new Rect(12, 118, 440, 316), GUIContent.none);
            string info = Selected.DisplayName + "  -  Tier " + Selected.Tier + "\n";
            if (Selected.Type == TowerType.Gold)
                info += "Gold +" + s.goldPerHit + " per hit    Rate " + s.fireInterval.ToString("0.00") + "s";
            else
                info += "Damage " + s.damage + "    Rate " + s.fireInterval.ToString("0.00") + "s";
            info += "\nDamage done: " + Mathf.RoundToInt(Selected.DamageDone);
            if (Selected.Type == TowerType.Gold)
                info += "\nGold made: $" + Selected.GoldEarned;
            GUI.Label(new Rect(20, 122, 412, 72), info, Style(14, TextAnchor.UpperLeft, Color.white));

            // unique tier 5/6 modifiers, or the tier 7 fusion tag (word-wrapped)
            string mod5 = TowerCatalog.ModifierText(Selected.Type, 5);
            string mod6 = TowerCatalog.ModifierText(Selected.Type, 6);
            string mod7 = TowerCatalog.ModifierText(Selected.Type, 7);
            if (mod7 != null)
            {
                GUIStyle mod7Style = Style(12, TextAnchor.UpperLeft, new Color(1f, 0.92f, 0.55f));
                mod7Style.wordWrap = true;
                GUI.Label(new Rect(20, 196, 412, 34), "T7: " + mod7, mod7Style);
            }
            else
            {
                GUIStyle mod5Style = Style(12, TextAnchor.UpperLeft, new Color(0.72f, 0.86f, 1f));
                mod5Style.wordWrap = true;
                GUIStyle mod6Style = Style(12, TextAnchor.UpperLeft, new Color(1f, 0.82f, 0.45f));
                mod6Style.wordWrap = true;
                if (mod5 != null)
                    GUI.Label(new Rect(20, 196, 412, 34), "T5: " + mod5, mod5Style);
                if (mod6 != null)
                    GUI.Label(new Rect(20, 230, 412, 34), "T6: " + mod6, mod6Style);
                if (mod5 == null)
                {
                    GUIStyle noModStyle = Style(12, TextAnchor.UpperLeft, new Color(0.75f, 0.75f, 0.78f));
                    noModStyle.wordWrap = true;
                    GUI.Label(new Rect(20, 196, 412, 34), "No tier 5/6 modifiers (max tier 3)", noModStyle);
                }
            }

            // targeting mode: full names, 3 columns x 2 rows
            GUI.Label(new Rect(20, 268, 120, 18), "Target:",
                Style(12, TextAnchor.MiddleLeft, new Color(0.80f, 0.85f, 0.92f)));
            TowerTargeting[] tmodes = { TowerTargeting.Default, TowerTargeting.Nearest, TowerTargeting.Farthest,
                                        TowerTargeting.Random, TowerTargeting.HighestHealth, TowerTargeting.LowestHealth };
            string[] tnames = { "Default", "Nearest", "Farthest", "Random", "Highest health", "Lowest health" };
            for (int i = 0; i < tmodes.Length; i++)
            {
                bool on = Selected.Targeting == tmodes[i];
                Color oldBg = GUI.backgroundColor;
                if (on) GUI.backgroundColor = new Color(1f, 0.9f, 0.45f);
                int col = i % 3, row = i / 3;
                if (GUI.Button(new Rect(20 + col * 140, 288 + row * 28, 132, 26), tnames[i]))
                {
                    if (TDAudio.Instance != null) TDAudio.Instance.Click();
                    Selected.SetTargeting(tmodes[i]);
                }
                GUI.backgroundColor = oldBg;
            }
            GUIStyle targetStyle = Style(12, TextAnchor.UpperLeft, new Color(0.85f, 0.85f, 0.88f));
            targetStyle.wordWrap = true;
            GUI.Label(new Rect(20, 346, 412, 26), TargetingName(Selected.Targeting), targetStyle);

            bool canFuse = Selected.Type != TowerType.Gold && Selected.Tier == TowerCatalog.MaxTier - 1;
            bool canMerge = Selected.Type != TowerType.Gold &&
                            (Selected.Tier <= TowerCatalog.MaxMergeTier || canFuse);
            int mergeCost = canFuse ? TDBalance.FuseCost : TDBalance.MergeCost;
            int ascendCost = UpgradeCost(Selected);   // Gold 1->2->3; others ascend 4->5->6
            bool canAscend = ascendCost > 0;
            bool canReRoll = Selected.Tier >= 2 && Selected.Tier < TowerCatalog.MaxTier;   // T7 is terminal
            int sellValue = Mathf.FloorToInt(TDBalance.SellRefund * Selected.InvestedCost);

            if (merging)
            {
                GUI.Label(new Rect(20, 376, 412, 20),
                    Selected.Tier == TowerCatalog.MaxTier - 1
                        ? "Select another Tier 6 tower to fuse with"
                        : "Select a Tier " + Selected.Tier + " tower to merge with",
                    Style(13, TextAnchor.UpperLeft, new Color(1f, 0.9f, 0.4f)));
                if (GUI.Button(new Rect(20, 400, 110, 26), "Cancel"))
                {
                    if (TDAudio.Instance != null) TDAudio.Instance.Click();
                    CancelMode();
                }
                if (GUI.Button(new Rect(140, 400, 150, 26), "Sell (X)  $" + sellValue))
                    TrySell();
            }
            else if (reRolling)
            {
                GUI.Label(new Rect(20, 376, 412, 20),
                    "Select a Tier " + (Selected.Tier - 1) + " tower to re-roll with",
                    Style(13, TextAnchor.UpperLeft, new Color(1f, 0.9f, 0.4f)));
                if (GUI.Button(new Rect(20, 400, 110, 26), "Cancel"))
                {
                    if (TDAudio.Instance != null) TDAudio.Instance.Click();
                    CancelMode();
                }
                if (GUI.Button(new Rect(140, 400, 150, 26), "Sell (X)  $" + sellValue))
                    TrySell();
            }
            else
            {
                if (canMerge)
                {
                    bool canAfford = Money >= mergeCost;
                    GUI.enabled = canAfford;
                    string mergeLabel = (canFuse ? "Fuse (E)  $" : "Merge (E)  $") + mergeCost;
                    if (GUI.Button(new Rect(20, 378, 150, 30), mergeLabel))
                        TryStartMerge();
                    GUI.enabled = true;
                }
                else if (canAscend)
                {
                    bool canAfford = Money >= ascendCost;
                    GUI.enabled = canAfford;
                    string upLabel = (Selected.Type == TowerType.Gold ? "Upgrade (U)  $" : "Ascend (U)  $") + ascendCost;
                    if (GUI.Button(new Rect(20, 378, 150, 30), upLabel))
                        TryStartAscend();
                    GUI.enabled = true;
                }
                else
                {
                    GUI.Label(new Rect(20, 382, 150, 22), "Max tier",
                        Style(13, TextAnchor.MiddleLeft, new Color(0.8f, 0.8f, 0.8f)));
                }

                if (canReRoll && GUI.Button(new Rect(180, 378, 130, 30), "Re-roll (R)"))
                    TryStartReRoll();
                if (GUI.Button(new Rect(320, 378, 120, 30), "Sell (X)  $" + sellValue))
                    TrySell();
            }
        }

        // ---- bottom toolbar: build slots (room for future options) + controls legend ----
        float legendY = Screen.height - ToolbarBottomMargin - ToolbarButtonH - ToolbarLegendH;
        GUI.Label(new Rect(0, legendY, Screen.width, ToolbarLegendH),
            "B: Build ($" + TDBalance.BuildCost + ")   |   G: Gold ($" + TDBalance.BuildCost + ", max " + TowerCatalog.MaxGoldTowers + ")   |   E: Merge ($" + TDBalance.MergeCost + "), T6 fuse ($" + TDBalance.FuseCost + ")   |   U: Ascend ($" + TDBalance.AscendCost4to5 + "/$" + TDBalance.AscendCost5to6 + "), Gold ($" + TDBalance.GoldUpgrade1to2 + "/$" + TDBalance.GoldUpgrade2to3 + ")   |   R: Re-roll   |   X: Sell (50%)   |   Left-click: place / select   |   Right-click: cancel   |   WASD: move   |   Middle-drag: rotate   |   Scroll: zoom   |   M: music   |   Esc: menu",
            Style(13, TextAnchor.MiddleCenter, new Color(0.8f, 0.8f, 0.8f)));

        if (ViewingOwnBoard)
        {
            float btnY = Screen.height - ToolbarBottomMargin - ToolbarButtonH;
            const float slotW = 200f, slotGap = 8f;
            float slotX = 12f;

            Color prevBg = GUI.backgroundColor;
            if (building) GUI.backgroundColor = new Color(0.45f, 1f, 0.5f);
            if (GUI.Button(new Rect(slotX, btnY, slotW, ToolbarButtonH),
                "Tower (B)  $" + TDBalance.BuildCost))
                ToggleBuildMode();
            GUI.backgroundColor = prevBg;
            slotX += slotW + slotGap;   // onward: room for future build options

            int goldCount = GoldTowerCount();
            bool goldCapped = goldCount >= TowerCatalog.MaxGoldTowers;
            prevBg = GUI.backgroundColor;
            if (goldBuilding) GUI.backgroundColor = new Color(1f, 0.85f, 0.3f);
            GUI.enabled = goldBuilding || !goldCapped;   // always allow toggling off
            if (GUI.Button(new Rect(slotX, btnY, slotW, ToolbarButtonH),
                "Gold Tower (G)  " + goldCount + "/" + TowerCatalog.MaxGoldTowers))
                ToggleGoldBuild();
            GUI.enabled = true;
            GUI.backgroundColor = prevBg;
            slotX += slotW + slotGap;   // future slot starts here
        }

        DrawWaveIntro();
        DrawBossBar();
    }

    /// <summary>Wave title + any unique modifiers, shown for a few seconds at wave start.</summary>
    void DrawWaveIntro()
    {
        if (State != GameState.Playing || waveIntroTimer <= 0f) return;
        if (Wave < 1 || Wave > TDBalance.TotalWaves) return;

        MobDef md = MobCatalog.Get(TDBalance.Waves[Wave - 1].mob);
        string traits = MobTraits(md);
        float a = Mathf.Clamp01(waveIntroTimer / 1.2f);   // fade over the last moment
        float y = 78f;

        Color old = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, a);
        GUI.Label(new Rect(0f, y, Screen.width, 30f), "WAVE " + Wave,
            Style(26, TextAnchor.MiddleCenter, new Color(1f, 0.88f, 0.45f)));
        string line = md.displayName.ToUpper();
        if (!string.IsNullOrEmpty(traits)) line += "   -   " + traits;
        GUI.Label(new Rect(0f, y + 30f, Screen.width, 24f), line,
            Style(16, TextAnchor.MiddleCenter, Color.white));
        GUI.color = old;
    }

    /// <summary>Boss-style gimmick tags for a mob, or its archetype when it has none.</summary>
    static string MobTraits(MobDef d)
    {
        List<string> tags = new List<string>();
        if (d.regen > 0f) tags.Add("Regenerates");
        if (d.armour > 0f) tags.Add("Armoured");
        if (d.slowResist > 0f) tags.Add("Slow-resistant");
        if (d.enrage > 0f) tags.Add("Enrages");
        if (d.dashEvery > 0f) tags.Add("Dashes");
        if (tags.Count == 0)
        {
            switch (d.archetype)
            {
                case MobArchetype.Fast: return "Fast";
                case MobArchetype.Tank: return "Tank";
                case MobArchetype.Swarm: return "Swarm";
                default: return "";
            }
        }
        return string.Join("  -  ", tags.ToArray());
    }

    /// <summary>Human-readable description of a targeting mode.</summary>
    static string TargetingName(TowerTargeting t)
    {
        switch (t)
        {
            case TowerTargeting.Nearest: return "Nearest to tower";
            case TowerTargeting.Farthest: return "Farthest from tower";
            case TowerTargeting.Random: return "Random target each shot";
            case TowerTargeting.HighestHealth: return "Highest health (tie: closest to finishing)";
            case TowerTargeting.LowestHealth: return "Lowest health (tie: closest to finishing)";
            default: return "Default: closest to finishing (furthest along the path)";
        }
    }

    /// <summary>Big centred boss bar at the top of the screen while a boss is alive.
    /// Bosses have no floating bar, so this is the only read on their health.</summary>
    void DrawBossBar()
    {
        if (State != GameState.Playing) return;

        string name;
        float frac;
        bool bSlowed, bPoisoned;
        int bStacks;
        bool bTarred;
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
            bSlowed = boss.IsSlowed;
            bStacks = boss.PoisonStacks;
            bPoisoned = bStacks > 0;
            bTarred = boss.IsTarred;
        }
        else
        {
            RemoteBoard rb = BoardForSlot(viewSlot);
            if (rb == null || !rb.TryGetBoss(out name, out frac, out bSlowed, out bPoisoned, out bStacks, out bTarred)) return;
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

        DrawBossStatusIcons(x + w + 10f, y + h * 0.5f, bSlowed, bPoisoned, bStacks, bTarred);
    }

    /// <summary>Small status badges just right of the boss HUD bar, using the same
    /// procedural icon textures as the floating mob bars so a boss reads the same
    /// on your own board and on a spectated one.</summary>
    void DrawBossStatusIcons(float cx, float cy, bool slowed, bool poisoned, int stacks, bool tarred)
    {
        const float size = 30f;
        const float gap = 8f;
        float x = cx;
        float top = cy - size * 0.5f;

        if (poisoned)
        {
            GUI.DrawTexture(new Rect(x, top, size, size), TDTextures.IconBurn(), ScaleMode.ScaleToFit, true);
            if (stacks > 0)
                GUI.Label(new Rect(x + size - 2f, top - 16f, 44f, 18f), "x" + stacks,
                    Style(13, TextAnchor.LowerLeft, new Color(0.62f, 1f, 0.55f)));
            x += size + gap;
        }
        if (slowed)
        {
            GUI.DrawTexture(new Rect(x, top, size, size), TDTextures.IconSlow(), ScaleMode.ScaleToFit, true);
            x += size + gap;
        }
        if (tarred)
            GUI.DrawTexture(new Rect(x, top, size, size), TDTextures.IconTar(), ScaleMode.ScaleToFit, true);
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
