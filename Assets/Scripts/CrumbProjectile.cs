using UnityEngine;

/// <summary>
/// One cookie bit fired by the Cookie Crumbler. Unlike <see cref="Projectile"/>
/// these do not home: each travels straight along its own fan direction until it
/// strikes a mob or runs out of reach, so a volley reads as a spreading cone.
/// </summary>
public class CrumbProjectile : MonoBehaviour
{
    public Vector3 Direction = Vector3.forward;
    public float Speed = 13f;
    public float Range = 2.8f;
    public float Damage = 4f;
    public float HitRadius = 0.42f;
    public Tower Source;

    private float travelled;
    private Vector3 spinAxis;

    void Start()
    {
        spinAxis = Random.onUnitSphere;
    }

    void Update()
    {
        float step = Speed * Time.deltaTime;
        transform.position += Direction * step;
        transform.Rotate(spinAxis, 420f * Time.deltaTime, Space.World);
        travelled += step;

        TDGameManager gm = TDGameManager.Instance;
        var mobs = gm != null ? gm.Mobs : null;
        if (mobs != null)
        {
            for (int i = 0; i < mobs.Count; i++)
            {
                Mob m = mobs[i];
                if (m == null) continue;
                // Compare on the mat plane: the bit flies above the mob's origin.
                Vector3 d = m.transform.position - transform.position;
                d.y = 0f;
                if (d.sqrMagnitude <= HitRadius * HitRadius)
                {
                    m.TakeDamageFromTower(Damage, Source);
                    if (Source != null) Source.AddDamage(Damage);
                    Destroy(gameObject);
                    return;
                }
            }
        }

        if (travelled >= Range) Destroy(gameObject);
    }
}
