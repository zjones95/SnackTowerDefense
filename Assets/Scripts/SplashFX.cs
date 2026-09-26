using UnityEngine;

/// <summary>
/// Impact burst for splash projectiles: a quickly expanding blob plus a few
/// droplets that fly out, fall, and fade. Purely cosmetic; the damage is
/// applied by the projectile.
/// </summary>
public class SplashFX : MonoBehaviour
{
    private const float Life = 0.32f;

    private Material mat;
    private Color tint;
    private Transform core;
    private Transform[] drops;
    private Vector3[] vel;
    private float targetRadius;
    private float t;

    public static void Spawn(Vector3 pos, float radius, Color color)
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
        mat = TDVisuals.TransparentMat(color, 0.85f, 0.15f);

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
            Color c = new Color(tint.r, tint.g, tint.b, (1f - k) * 0.85f);
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
