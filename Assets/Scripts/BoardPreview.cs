using UnityEngine;

/// <summary>
/// Off-screen turntable for the single-player setup screen: builds a miniature
/// of the real board (same layout and route, small cells) in the chosen theme
/// on a stage far below the world, and slowly spins it into a RenderTexture.
/// Same idea as <see cref="TowerViewer"/> / <see cref="MobViewer"/>.
/// </summary>
public class BoardPreview : MonoBehaviour
{
    public static BoardPreview Instance { get; private set; }

    public RenderTexture Texture { get; private set; }
    public bool IsOpen { get; private set; }
    public BoardTheme CurrentTheme { get; private set; } = BoardTheme.Classic;

    private const float StageY = -800f;
    private const float Cell = 0.32f;
    private const int Size = 512;

    private Camera cam;
    private Transform spinner;
    private GameObject board;

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
        Show(CurrentTheme);
    }

    public void Close() { IsOpen = false; }

    /// <summary>Rebuilds the miniature only when the theme actually changed.</summary>
    public void Show(BoardTheme theme)
    {
        EnsureStage();
        if (board != null && theme == CurrentTheme) return;
        CurrentTheme = theme;

        if (board != null) Destroy(board);
        board = new GameObject("PreviewBoard");
        board.transform.SetParent(spinner, false);
        board.transform.localPosition = Vector3.zero;

        TDMap map = TDBoardBuilder.CreateMap(TDGameManager.Layout, TDGameManager.Route, Cell, Vector3.zero);
        TDBoardBuilder.BuildTiles(board.transform, map, theme);
        BoardThemeProps.Build(board.transform, map, theme, false);   // no perimeter: keeps the framing tight
    }

    void EnsureStage()
    {
        if (cam != null) return;

        Vector3 origin = new Vector3(0f, StageY, 0f);

        GameObject rootGO = new GameObject("Stage");
        rootGO.transform.SetParent(transform, false);
        rootGO.transform.position = origin;
        spinner = rootGO.transform;

        GameObject camGO = new GameObject("ViewerCam");
        camGO.transform.SetParent(transform, false);
        camGO.transform.position = origin + new Vector3(0f, 5.2f, -6.8f);
        camGO.transform.LookAt(origin + new Vector3(0f, -0.1f, 0f));

        cam = camGO.AddComponent<Camera>();
        cam.fieldOfView = 30f;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 30f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.10f, 0.12f, 0.16f);
        cam.allowMSAA = true;
        cam.enabled = false;              // rendered manually into the RT

        Texture = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
        Texture.antiAliasing = 8;
        cam.targetTexture = Texture;
    }

    void Update()
    {
        if (IsOpen && spinner != null)
            spinner.Rotate(0f, 18f * Time.deltaTime, 0f);
    }

    void LateUpdate()
    {
        if (IsOpen && cam != null && cam.targetTexture != null)
            cam.Render();
    }
}
