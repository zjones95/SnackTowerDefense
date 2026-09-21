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

    public readonly List<Mob> Mobs = new List<Mob>();

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
    private Camera cam;
    private Vector3 camFocus;
    private float camYaw = 45f, camPitch = 42f;
    private float camDist = 32f;
    private readonly float camDistMin = 12f, camDistMax = 90f;
    private Transform worldRoot, towersRoot, mobsRoot;
    private readonly Dictionary<int, Tower> towers = new Dictionary<int, Tower>();

    private float prepTimer;
    private string message = "";
    private float messageTimer;
    private bool merging;
    private Transform hover;
    private Renderer[] hoverRends;
    private Material hoverValid, hoverInvalid;

    // wave spawning
    private struct SpawnEntry { public MobType type; public float time; }
    private struct SpawnGroup
    {
        public MobType type; public int count; public float startDelay; public float interval;
        public SpawnGroup(MobType t, int c, float d, float i) { type = t; count = c; startDelay = d; interval = i; }
    }
    private static readonly SpawnGroup[][] Waves =
    {
        new[] { new SpawnGroup(MobType.Basic, 8, 0f, 0.9f) },
        new[] { new SpawnGroup(MobType.Basic, 10, 0f, 0.7f), new SpawnGroup(MobType.Fast, 4, 5f, 0.7f) },
        new[] { new SpawnGroup(MobType.Basic, 10, 0f, 0.6f), new SpawnGroup(MobType.Tank, 3, 4f, 1.4f) },
        new[] { new SpawnGroup(MobType.Fast, 12, 0f, 0.45f), new SpawnGroup(MobType.Tank, 5, 5f, 1.2f) },
        new[] { new SpawnGroup(MobType.Basic, 12, 0f, 0.5f), new SpawnGroup(MobType.Tank, 6, 4f, 1.0f), new SpawnGroup(MobType.Boss, 1, 12f, 1f) },
    };

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
    void ApplyCamera()
    {
        if (cam == null) return;
        cam.transform.rotation = Quaternion.Euler(camPitch, camYaw, 0f);
        cam.transform.position = camFocus - cam.transform.forward * camDist;
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
        camFocus.x = Mathf.Clamp(camFocus.x, -hx, hx);
        camFocus.z = Mathf.Clamp(camFocus.z, -hz, hz);
        camFocus.y = 0f;

        ApplyCamera();
    }

    // ------------------------------------------------------------ game flow
    void StartRun()
    {
        Mobs.Clear();
        towers.Clear();
        Selected = null;
        merging = false;
        if (worldRoot != null) Destroy(worldRoot.gameObject);

        Money = TDBalance.StartMoney;
        Lives = TDBalance.StartLives;
        Wave = 1;
        spawnQueue.Clear();
        spawnIndex = 0;
        State = GameState.Playing;
        Round = RoundState.Preparing;
        prepTimer = TDBalance.PrepDuration;
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
        map = new TDMap(layout, route, cell, new Vector3(-gw * cell * 0.5f, 0f, -gh * cell * 0.5f));

        // (room floor is built by TDRoom)

        // colourful foam play-mat tiles
        Color[] tileCols =
        {
            new Color(0.82f, 0.34f, 0.34f),
            new Color(0.34f, 0.56f, 0.86f),
            new Color(0.88f, 0.76f, 0.30f),
            new Color(0.42f, 0.76f, 0.44f)
        };
        Material[] tileMats = new Material[tileCols.Length];
        for (int i = 0; i < tileCols.Length; i++)
            tileMats[i] = TDVisuals.TexturedMat(TDTextures.Weave(), tileCols[i], new Vector2(3f, 3f));

        Material pathMat = TDVisuals.TexturedMat(TDTextures.Road(), Color.white, Vector2.one);        // toy train track
        Material crossMat = TDVisuals.TexturedMat(TDTextures.RoadCross(), Color.white, Vector2.one);  // track junctions
        Material voidMat = TDVisuals.Mat(new Color(0.60f, 0.54f, 0.44f), 0f, 0.25f);                  // bare carpet
        Material startMat = TDVisuals.Mat(new Color(0.25f, 0.80f, 0.35f), 0f, 0.3f);
        Material endMat = TDVisuals.Mat(new Color(0.85f, 0.22f, 0.22f), 0f, 0.3f);

        for (int ly = 0; ly < gh; ly++)
        {
            for (int x = 0; x < gw; x++)
            {
                char c = layout[ly][x];
                Vector3 pos = map.CellCenter(x, ly) + Vector3.up * 0.05f;
                Vector3 scale = new Vector3(cell * 0.97f, 0.10f, cell * 0.97f);
                if (c == 'm')
                {
                    int o = PathOrientation(x, ly);
                    GameObject tile = TDVisuals.Box(worldRoot, "Tile", pos, scale, o == 2 ? crossMat : pathMat);
                    if (o == 1) tile.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                }
                else
                {
                    Material m = voidMat;
                    if (c == 't') m = tileMats[(x + ly) % tileMats.Length];
                    else if (c == 's') m = startMat;
                    else if (c == 'e') m = endMat;
                    TDVisuals.Box(worldRoot, "Tile", pos, scale, m);
                }
            }
        }

        BuildHover();
        TDRoom.Build(worldRoot, map);
    }

    // 0 = horizontal road, 1 = vertical road, 2 = intersection
    int PathOrientation(int x, int ly)
    {
        bool l = map.IsPath(x - 1, ly), r = map.IsPath(x + 1, ly);
        bool u = map.IsPath(x, ly - 1), d = map.IsPath(x, ly + 1);
        bool horiz = l || r, vert = u || d;
        if (horiz && vert) return 2;
        if (vert) return 1;
        return 0;
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
        SpawnGroup[] groups = Waves[Mathf.Clamp(wave - 1, 0, Waves.Length - 1)];
        for (int g = 0; g < groups.Length; g++)
        {
            SpawnGroup grp = groups[g];
            for (int i = 0; i < grp.count; i++)
                q.Add(new SpawnEntry { type = grp.type, time = grp.startDelay + i * grp.interval });
        }
        q.Sort((a, b) => a.time.CompareTo(b.time));
        return q;
    }

    void EndWave()
    {
        int bonus = TDBalance.RoundBonus(Wave);
        Money += bonus;
        message = "Round cleared! +$" + bonus;
        messageTimer = 2.5f;
        if (TDAudio.Instance != null) TDAudio.Instance.RoundClear();

        Wave++;
        if (Wave > TDBalance.TotalWaves)
        {
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
            SpawnMob(spawnQueue[spawnIndex].type);
            spawnIndex++;
        }

        // Round ends only after every queued mob has spawned and the field is clear.
        if (spawnQueue.Count > 0 && spawnIndex >= spawnQueue.Count && Mobs.Count == 0) EndWave();
    }

    void SpawnMob(MobType type)
    {
        MobDef def = MobCatalog.Get(type);
        GameObject go = new GameObject("Mob_" + type);
        go.transform.SetParent(mobsRoot, false);
        Mob m = go.AddComponent<Mob>();
        m.Init(def, map.Waypoints, this, TDBalance.HealthMult(Wave), TDBalance.SpeedMult(Wave));
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
            State = GameState.GameOver;
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
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (merging) { merging = false; return; }
            State = GameState.MainMenu;
            ClearWorld();
            return;
        }

        if (Round == RoundState.Preparing)
        {
            prepTimer -= Time.deltaTime;
            if (Input.GetKeyDown(KeyCode.Space)) prepTimer = 0f;
            if (prepTimer <= 0f) BeginWave();
        }
        else
        {
            TickWave();
        }

        UpdateCamera(Time.deltaTime);
        HandleMouse();
        UpdateHover();
    }

    void UpdateHover()
    {
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
        hover.position = map.CellCenter(x, y) + Vector3.up * 0.12f;
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
        if (State == GameState.MainMenu) { DrawMenu(); return; }
        if (State == GameState.MultiplayerMenu || State == GameState.Lobby) { DrawMultiplayer(); return; }
        DrawHud();
        if (State == GameState.GameOver) DrawEnd(false);
        else if (State == GameState.Victory) DrawEnd(true);
    }

    GUIStyle Style(int size, TextAnchor anchor, Color color)
    {
        GUIStyle s = new GUIStyle(GUI.skin.label);
        s.fontSize = size;
        s.alignment = anchor;
        s.normal.textColor = color;
        return s;
    }

    void DrawMenu()
    {
        Color old = GUI.color;
        GUI.color = new Color(0.05f, 0.06f, 0.09f, 0.92f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = old;

        GUI.Label(new Rect(0, Screen.height * 0.22f, Screen.width, 70), "SNACK TOWER DEFENSE", Style(46, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.4f)));

        float bw = 260f, bh = 56f;
        float bx = (Screen.width - bw) * 0.5f;
        float by = Screen.height * 0.46f;
        if (GUI.Button(new Rect(bx, by, bw, bh), "Single Player"))
        {
            if (TDAudio.Instance != null) TDAudio.Instance.Click();
            StartRun();
        }
        if (GUI.Button(new Rect(bx, by + bh + 16f, bw, bh), "Multiplayer"))
        {
            if (TDAudio.Instance != null) TDAudio.Instance.Click();
            EnterMultiplayer();
        }
        if (GUI.Button(new Rect(bx, by + 2f * (bh + 16f), bw, bh), "Quit")) Application.Quit();
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
            "Left-click: build ($" + TDBalance.BuildCost + ") / select   |   Merge ($" + TDBalance.MergeCost + ") -> same-tier   |   WASD: move   |   Middle-drag: rotate   |   Scroll: zoom   |   Esc: menu",
            Style(13, TextAnchor.MiddleCenter, new Color(0.8f, 0.8f, 0.8f)));
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
        if (GUI.Button(new Rect(bx - 120f, Screen.height * 0.55f, bw, bh), "Retry"))
        {
            if (TDAudio.Instance != null) TDAudio.Instance.Click();
            StartRun();
        }
        if (GUI.Button(new Rect(bx + 120f, Screen.height * 0.55f, bw, bh), "Main Menu"))
        {
            if (TDAudio.Instance != null) TDAudio.Instance.Click();
            State = GameState.MainMenu;
            ClearWorld();
        }
    }
}
