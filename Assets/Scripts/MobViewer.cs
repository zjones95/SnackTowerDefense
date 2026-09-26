using UnityEngine;

/// <summary>
/// Off-screen turntable for browsing mobs from the main menu. Same idea as
/// <see cref="TowerViewer"/>, but it renders mob models (which fall back to the
/// procedural art when a model is missing).
/// </summary>
public class MobViewer : MonoBehaviour
{
    public static MobViewer Instance { get; private set; }

    public RenderTexture Texture { get; private set; }
    public int Index { get; private set; }
    public bool IsOpen { get; private set; }
    public MobDef Current { get { return MobCatalog.All[Index]; } }

    private const float StageY = -600f;
    private const int Size = 512;

    private Camera cam;
    private Transform root;
    private GameObject model;

    public static MobViewer Ensure()
    {
        if (Instance != null) return Instance;
        GameObject go = new GameObject("MobViewer");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<MobViewer>();
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
        int n = MobCatalog.All.Length;
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
        disc.transform.localScale = new Vector3(2.4f, 0.03f, 2.4f);
        disc.GetComponent<Renderer>().sharedMaterial =
            TDVisuals.Mat(new Color(0.15f, 0.17f, 0.21f), 0.25f, 0.55f);

        GameObject camGO = new GameObject("ViewerCam");
        camGO.transform.SetParent(transform, false);
        camGO.transform.position = origin + new Vector3(0f, 1.10f, -3.6f);
        camGO.transform.LookAt(origin + new Vector3(0f, 0.55f, 0f));

        cam = camGO.AddComponent<Camera>();
        cam.fieldOfView = 32f;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 20f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.10f, 0.12f, 0.16f);
        cam.allowMSAA = true;
        cam.enabled = false;

        Texture = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
        Texture.antiAliasing = 8;
        cam.targetTexture = Texture;
    }

    void Show(int index)
    {
        int n = MobCatalog.All.Length;
        Index = ((index % n) + n) % n;

        if (model != null) Destroy(model);

        model = new GameObject("ViewerMob");
        model.transform.SetParent(root, false);
        model.transform.localPosition = Vector3.zero;

        Transform fill;
        MobVisual.Build(model.transform, Current, out fill);

        // no Mob component here, so drop the health bar (it wouldn't billboard)
        Transform hp = model.transform.Find("HPBar");
        if (hp != null) Destroy(hp.gameObject);
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
