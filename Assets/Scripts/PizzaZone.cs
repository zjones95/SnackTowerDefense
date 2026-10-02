using UnityEngine;

/// <summary>
/// Pizza Oven T7 "Pizza Delivery": a persistent patch of molten cheese left at
/// the impact point. Every enemy inside takes damage over time for as long as
/// the zone lasts, and the source tower keeps the Damage Done credit.
/// </summary>
public class PizzaZone : MonoBehaviour
{
    public Tower Source;   // tower credited with the damage
    public float Radius = 2.5f;
    public float Dps = 120f;
    public float Duration = 5f;
    public bool Toasted;

    private float life;

    static Material cheeseMat, sauceMat, toastedMat, icingMat;
    static Shader zoneShader;

    /// <summary>Build-safe translucent material (the Resources Fx shader, like
    /// SplashFX) at 30% opacity, so the zone reads as molten cheese on the mat.</summary>
    static Material ZoneMat(ref Material cache, Color c)
    {
        if (cache != null) return cache;
        if (zoneShader == null) zoneShader = Resources.Load<Shader>("Snack/Fx");
        if (zoneShader == null) zoneShader = Shader.Find("Snack/Fx");
        cache = zoneShader != null ? new Material(zoneShader) : TDVisuals.TransparentMat(c, 0.30f, 0.3f);
        Color col = new Color(c.r, c.g, c.b, 0.30f);
        if (cache.HasProperty("_Color")) cache.SetColor("_Color", col);
        if (cache.HasProperty("_BaseColor")) cache.SetColor("_BaseColor", col);
        return cache;
    }

    void Start()
    {
        life = Duration;

        // flat cheese disc with a sauce layer on top (radius reuses the tower's
        // splashRadius); the primitives are purely cosmetic.
        GameObject baseGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Strip(baseGO);
        baseGO.name = Toasted ? "ToastedPastry" : "Cheese";
        baseGO.transform.SetParent(transform, false);
        baseGO.transform.localPosition = new Vector3(0f, 0.04f, 0f);
        baseGO.transform.localScale = new Vector3(Radius * 2f, 0.03f, Radius * 2f);
        baseGO.GetComponent<Renderer>().sharedMaterial = Toasted
            ? ZoneMat(ref toastedMat, new Color(0.95f, 0.59f, 0.36f))
            : ZoneMat(ref cheeseMat, new Color(0.95f, 0.78f, 0.38f));

        GameObject sauceGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Strip(sauceGO);
        sauceGO.name = Toasted ? "Icing" : "Sauce";
        sauceGO.transform.SetParent(transform, false);
        sauceGO.transform.localPosition = new Vector3(0f, 0.06f, 0f);
        sauceGO.transform.localScale = new Vector3(Radius * 1.75f, 0.02f, Radius * 1.75f);
        sauceGO.GetComponent<Renderer>().sharedMaterial = Toasted
            ? ZoneMat(ref icingMat, new Color(0.98f, 0.65f, 0.78f))
            : ZoneMat(ref sauceMat, new Color(0.85f, 0.20f, 0.12f));
    }

    static void Strip(GameObject g)
    {
        Collider c = g.GetComponent<Collider>();
        if (c != null) Object.Destroy(c);
    }

    void Update()
    {
        life -= Time.deltaTime;

        // spin the pizza (cosmetic; does not move the damage centre)
        transform.Rotate(0f, 55f * Time.deltaTime, 0f);

        TDGameManager gm = TDGameManager.Instance;
        var mobs = gm != null ? gm.Mobs : null;
        if (mobs != null && Dps > 0f)
        {
            float dmg = Dps * Time.deltaTime;
            // backwards so a lethal tick that removes the mob can't skip the next.
            for (int i = mobs.Count - 1; i >= 0; i--)
            {
                Mob m = mobs[i];
                if (m == null) continue;
                if (Vector3.Distance(m.transform.position, transform.position) <= Radius)
                {
                    m.TakeDamageFromTower(dmg, Source);
                    if (Source != null) Source.AddDamage(dmg);
                }
            }
        }

        if (life <= 0f) Destroy(gameObject);
    }
}
