using UnityEngine;

/// <summary>
/// Impact burst for splash projectiles: a quickly expanding blob plus a few
/// droplets that fly out, fall, and fade. Purely cosmetic; the damage is
/// applied by the projectile.
/// </summary>
public class SplashFX : MonoBehaviour
{
    private const float Life = 0.32f;
    private const float MaxAlpha = 0.2f;   // explosion opacity (rest fades out)

    private Material mat;
    private Color tint;
    private Transform core;
    private Transform[] drops;
    private Vector3[] vel;
    private float targetRadius;
    private float t;

    public static void Spawn(Vector3 pos, float radius, Color color)
    {
        Create(pos, radius, color);
        FxEvents.Splash(pos, radius, color);   // mirror on remote boards
    }

    /// <summary>Replays a remote splash locally (does not re-queue it).</summary>
    public static void PlayRemote(Vector3 pos, float radius, Color color)
    {
        Create(pos, radius, color);
    }

    static void Create(Vector3 pos, float radius, Color color)
    {
        GameObject go = new GameObject("SplashFX");
        go.transform.position = pos;
        SplashFX fx = go.AddComponent<SplashFX>();
        fx.Build(radius, color);
    }

    void Build(float radius, Color color)
    {
        tint = color;
        targetRadius = radius;
        mat = NewFxMaterial(color, MaxAlpha);

        GameObject coreGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Strip(coreGO);
        coreGO.name = "Core";
        coreGO.transform.SetParent(transform, false);
        coreGO.GetComponent<Renderer>().sharedMaterial = mat;
        core = coreGO.transform;

        const int n = 7;
        drops = new Transform[n];
        vel = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            GameObject d = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Strip(d);
            d.name = "Drop" + i;
            d.transform.SetParent(transform, false);
            d.transform.localScale = Vector3.one * 0.16f;
            d.GetComponent<Renderer>().sharedMaterial = mat;
            drops[i] = d.transform;

            float a = i / (float)n * Mathf.PI * 2f;
            vel[i] = new Vector3(Mathf.Cos(a), 1.15f, Mathf.Sin(a)).normalized * (2.4f + radius);
        }
    }

    static void Strip(GameObject g)
    {
        Collider c = g.GetComponent<Collider>();
        if (c != null) Object.Destroy(c);
    }

    static Shader fxShader;

    /// <summary>Build-safe unlit alpha material (Resources shader, so the
    /// transparent variant can't be stripped from the player build).</summary>
    static Material NewFxMaterial(Color c, float alpha)
    {
        if (fxShader == null) fxShader = Resources.Load<Shader>("Snack/Fx");
        if (fxShader == null) fxShader = Shader.Find("Snack/Fx");

        Material m = fxShader != null ? new Material(fxShader)
                                      : TDVisuals.TransparentMat(c, alpha, 0.15f);
        Color col = new Color(c.r, c.g, c.b, alpha);
        if (m.HasProperty("_Color")) m.SetColor("_Color", col);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
        return m;
    }

    void Update()
    {
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / Life);

        if (core != null)
            core.localScale = Vector3.one * Mathf.Lerp(0.25f, targetRadius * 2f, Mathf.Sqrt(k));

        if (drops != null)
        {
            for (int i = 0; i < drops.Length; i++)
            {
                if (drops[i] == null) continue;
                drops[i].position += vel[i] * Time.deltaTime;
                vel[i] += Vector3.down * 13f * Time.deltaTime;
                drops[i].localScale = Vector3.one * 0.16f * (1f - k * 0.6f);
            }
        }

        if (mat != null)
        {
            Color c = new Color(tint.r, tint.g, tint.b, (1f - k) * MaxAlpha);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        }

        if (t >= Life) Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (mat != null) Destroy(mat);
    }
}
