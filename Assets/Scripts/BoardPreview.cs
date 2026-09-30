using UnityEngine;

/// <summary>
/// Off-screen turntable used by the difficulty screen's map picker. Builds a
/// complete board (tiles + room) for the chosen <see cref="BoardTheme"/> on a
/// stage far below the world, renders it with its own camera into a
/// RenderTexture, and slowly spins it. The UI just draws <see cref="Texture"/>.
/// </summary>
public class BoardPreview : MonoBehaviour
{
    public static BoardPreview Instance { get; private set; }

    public RenderTexture Texture { get; private set; }
    public bool IsOpen { get; private set; }

    private const float StageY = -800f;   // far below the world, like the other viewers
    private const int Size = 512;
    private const float SpinDegPerSec = 18f;

    private Camera cam;
    private Transform root;
    private GameObject board;
    private bool built;
    private BoardTheme shown;

    public static BoardPreview Ensure()
    {
        if (Instance != null) return Instance;
        GameObject go = new GameObject("BoardPreview");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<BoardPreview>();
        return Instance;
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Open()
    {
        EnsureStage();
        IsOpen = true;
        Show(TDGameManager.ActiveTheme);
    }

    public void Close() { IsOpen = false; }

    void EnsureStage()
    {
        if (cam != null) return;

        Vector3 origin = new Vector3(0f, StageY, 0f);

        GameObject rootGO = new GameObject("PreviewStage");
        rootGO.transform.SetParent(transform, false);
        rootGO.transform.position = origin;
        root = rootGO.transform;

        // Same framing as the editor's TDArenaPreview so the thumbnail matches
        // what the board actually looks like in a run.
        GameObject camGO = new GameObject("PreviewCam");
        camGO.transform.SetParent(transform, false);
        camGO.transform.position = origin + new Vector3(-20f, 30f, -32f);
        camGO.transform.LookAt(origin);

        cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 20f;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 120f;          // short: never sees the real world
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.10f, 0.11f, 0.13f);
        cam.allowMSAA = true;
        cam.enabled = false;              // rendered manually into the RT

        Texture = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
        Texture.antiAliasing = 8;
        cam.targetTexture = Texture;
    }

    /// <summary>
    /// Rebuilds the preview board for <paramref name="theme"/>. Cheap to call
    /// every frame — it early-outs once the right theme is already built.
    /// </summary>
    public void Show(BoardTheme theme)
    {
        EnsureStage();
        if (built && shown == theme) return;
        shown = theme;
        built = true;

        if (board != null) Destroy(board);
        board = new GameObject("PreviewBoard");
        board.transform.SetParent(root, false);

        // Same layout the game runs — a theme only changes the dressing.
        float cell = 2f;
        TDMap map = TDBoardBuilder.CreateMap(TDGameManager.Layout, TDGameManager.Route, cell, Vector3.zero);
        TDBoardBuilder.BuildTiles(board.transform, map, theme);
        TDRoom.Build(board.transform, map, theme);
    }

    void Update()
    {
        if (IsOpen && root != null)
            root.Rotate(0f, SpinDegPerSec * Time.deltaTime, 0f);
    }

    void LateUpdate()
    {
        if (IsOpen && cam != null && cam.targetTexture != null)
            cam.Render();
    }
}
