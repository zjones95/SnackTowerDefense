using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A thrown skewer: travels in a straight line at a slow, steady pace and
/// skewers every enemy it passes through, up to a hit cap. Unlike the homing
/// projectiles it never changes course and doesn't die on the first hit.
/// </summary>
public class PierceProjectile : MonoBehaviour
{
    public Vector3 Dir = Vector3.forward;
    public float Speed = 9f;
    public float Damage = 10f;
    public float Width = 1.2f;
    public int MaxHits = 3;
    public float MaxDistance = 7f;

    private Vector3 start;
    private int hits;
    private readonly List<Mob> alreadyHit = new List<Mob>();

    void Start() { start = transform.position; }

    void Update()
    {
        transform.position += Dir * Speed * Time.deltaTime;

        TDGameManager gm = TDGameManager.Instance;
        var mobs = gm != null ? gm.Mobs : null;

        if (mobs != null && hits < MaxHits)
        {
            for (int i = 0; i < mobs.Count && hits < MaxHits; i++)
            {
                Mob m = mobs[i];
                if (m == null || alreadyHit.Contains(m)) continue;

                if (Vector3.Distance(m.transform.position, transform.position) <= Width)
                {
                    alreadyHit.Add(m);
                    m.TakeDamage(Damage);
                    hits++;
                }
            }
        }

        if (Vector3.Distance(start, transform.position) >= MaxDistance)
            Destroy(gameObject);
    }
}
