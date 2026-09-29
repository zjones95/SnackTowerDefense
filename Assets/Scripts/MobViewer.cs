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

    /// <summary>Max world-space dimension allowed in the preview frame. Normal
    /// mobs (~1-unit art, Tank peak ~1.4) pass through at scale 1; bosses
    /// (scale 1.45-1.6 x 1.5 in MobVisual) are scaled down viewer-side only.</summary>
    private const float FitSize = 1.7f;
    /// <summary>Constant gait rate for the preview (no movement delta exists).</summary>
    private const float PreviewWalkRate = 2f;

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

        FitToFrame();
        EnablePreviewWalk();
    }

    /// <summary>Viewer-only auto-fit: measures the built model and shrinks the
    /// viewer root so the max dimension fits the existing panel. Never scales
    /// up, so normal mobs keep their exact size; game-board scaling in
    /// MobVisual.Build is untouched.</summary>
    void FitToFrame()
    {
        if (model == null) return;
        Renderer[] rs = model.GetComponentsInChildren<Renderer>();
        if (rs == null || rs.Length == 0) return;
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        Vector3 size = b.size;
        float maxDim = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
        if (maxDim <= 1e-4f) return;
        float s = FitSize / maxDim;
        if (s >= 1f) return;
        model.transform.localScale *= s;
    }

    /// <summary>Viewer has no movement, so MobWalkAnimation would sit frozen at
    /// speed 0 — force its constant preview gait instead. Falls back to driving
    /// the Legacy clip directly if no walker was attached.</summary>
    void EnablePreviewWalk()
    {
        if (model == null) return;
        MobWalkAnimation w = model.GetComponentInChildren<MobWalkAnimation>();
        if (w != null) { w.SetPreview(true, PreviewWalkRate); return; }
        Animation a = model.GetComponentInChildren<Animation>();
        if (a == null) return;
        a.playAutomatically = false;
        a.cullingType = AnimationCullingType.AlwaysAnimate;
        foreach (AnimationState st in a)
        {
            st.wrapMode = WrapMode.Loop;
            st.speed = PreviewWalkRate;
            a.Play(st.name);
            break;
        }
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
