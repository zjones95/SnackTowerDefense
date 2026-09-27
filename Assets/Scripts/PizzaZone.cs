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

    private float life;

    void Start()
    {
        life = Duration;

        // flat cheese disc with a sauce layer on top (radius reuses the tower's
        // splashRadius); the primitives are purely cosmetic.
        GameObject baseGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Strip(baseGO);
        baseGO.name = "Cheese";
        baseGO.transform.SetParent(transform, false);
        baseGO.transform.localPosition = new Vector3(0f, 0.04f, 0f);
        baseGO.transform.localScale = new Vector3(Radius * 2f, 0.03f, Radius * 2f);
        baseGO.GetComponent<Renderer>().sharedMaterial =
            TDVisuals.Mat(new Color(0.95f, 0.78f, 0.38f), 0f, 0.5f);

        GameObject sauceGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Strip(sauceGO);
        sauceGO.name = "Sauce";
        sauceGO.transform.SetParent(transform, false);
        sauceGO.transform.localPosition = new Vector3(0f, 0.06f, 0f);
        sauceGO.transform.localScale = new Vector3(Radius * 1.75f, 0.02f, Radius * 1.75f);
        sauceGO.GetComponent<Renderer>().sharedMaterial =
            TDVisuals.Mat(new Color(0.85f, 0.20f, 0.12f), 0f, 0.6f);
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
                    m.TakeDamage(dmg);
                    if (Source != null) Source.AddDamage(dmg);
                }
            }
        }

        if (life <= 0f) Destroy(gameObject);
    }
}
