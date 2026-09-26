using UnityEngine;

/// <summary>
/// Off-screen turntable used by the main-menu tower viewer. Builds a small stage
/// far below the world, renders one tower model with its own camera into a
/// RenderTexture, and slowly spins it. The UI just draws <see cref="Texture"/>.
/// </summary>
public class TowerViewer : MonoBehaviour
{
    public static TowerViewer Instance { get; private set; }

    public RenderTexture Texture { get; private set; }
    public int Index { get; private set; }
    public bool IsOpen { get; private set; }
    public TowerType CurrentType { get { return TowerCatalog.AllTypes[Index]; } }

    private const float StageY = -400f;
    private const int Size = 512;

    private Camera cam;
    private Transform root;
    private GameObject model;

    public static TowerViewer Ensure()
    {
        if (Instance != null) return Instance;
        GameObject go = new GameObject("TowerViewer");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<TowerViewer>();
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
        Show(Index);
    }

    public void Close() { IsOpen = false; }

    public void Next(int dir)
    {
        int n = TowerCatalog.AllTypes.Length;
        Show((Index + dir % n + n) % n);
    }

    void EnsureStage()
    {
        if (cam != null) return;

        Vector3 origin = new Vector3(0f, StageY, 0f);

        GameObject rootGO = new GameObject("Stage");
        rootGO.transform.SetParent(transform, false);
        rootGO.transform.position = origin;
        root = rootGO.transform;

        GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Collider dc = disc.GetComponent<Collider>();
        if (dc != null) Destroy(dc);
        disc.name = "Turntable";
        disc.transform.SetParent(root, false);
        disc.transform.localPosition = new Vector3(0f, -0.03f, 0f);
        disc.transform.localScale = new Vector3(1.75f, 0.03f, 1.75f);
        disc.GetComponent<Renderer>().sharedMaterial =
            TDVisuals.Mat(new Color(0.15f, 0.17f, 0.21f), 0.25f, 0.55f);

        GameObject camGO = new GameObject("ViewerCam");
        camGO.transform.SetParent(transform, false);
        camGO.transform.position = origin + new Vector3(0f, 0.80f, -3.0f);
        camGO.transform.LookAt(origin + new Vector3(0f, 0.48f, 0f));

        cam = camGO.AddComponent<Camera>();
        cam.fieldOfView = 32f;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 20f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.10f, 0.12f, 0.16f);
        cam.allowMSAA = true;
        cam.enabled = false;              // rendered manually into the RT

        Texture = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
        Texture.antiAliasing = 8;
        cam.targetTexture = Texture;
    }

    void Show(int index)
    {
        int n = TowerCatalog.AllTypes.Length;
        Index = ((index % n) + n) % n;

        if (model != null) Destroy(model);

        model = new GameObject("ViewerTower");
        model.transform.SetParent(root, false);
        model.transform.localPosition = Vector3.zero;
        TowerVisual.Build(model.transform, CurrentType, 1);   // same art as in-game
    }

    void Update()
    {
        if (IsOpen && root != null)
            root.Rotate(0f, 34f * Time.deltaTime, 0f);
    }

    void LateUpdate()
    {
        if (IsOpen && cam != null && cam.targetTexture != null)
            cam.Render();
    }
}
