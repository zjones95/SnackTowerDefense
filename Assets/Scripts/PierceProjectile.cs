using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A thrown skewer: travels in a straight line at a slow, steady pace and
/// skewers every enemy it passes through, up to a hit cap. Unlike the homing
/// projectiles it never changes course and doesn't die on the first hit.
/// At Pierce T6 ("Boomerang Skewer") it turns around at max distance and
/// skewers again at full damage, steering toward the nearest un-hit mob.
/// </summary>
public class PierceProjectile : MonoBehaviour
{
    public Vector3 Dir = Vector3.forward;
    public float Speed = 9f;
    public float Damage = 10f;
    public float Width = 1.2f;
    public int MaxHits = 3;
    public float MaxDistance = 7f;
    public bool Boomerang = false;

    private Vector3 start;
    private int hits;
    private bool returning;
    private float returnTravelled;
    private readonly List<Mob> alreadyHit = new List<Mob>();

    void Start() { start = transform.position; }

    void Update()
    {
        TDGameManager gm = TDGameManager.Instance;
        var mobs = gm != null ? gm.Mobs : null;

        float step = Speed * Time.deltaTime;

        if (!returning)
        {
            transform.position += Dir * step;

            if (Vector3.Distance(start, transform.position) >= MaxDistance)
            {
                if (Boomerang)
                {
                    // turn around: a fresh pass, so everything can be skewered again
                    returning = true;
                    returnTravelled = 0f;
                    alreadyHit.Clear();
                    hits = 0;
                }
                else
                {
                    Destroy(gameObject);
                    return;
                }
            }
        }
        else
        {
            // steer toward the nearest mob we haven't hit on the return leg,
            // otherwise head back to where the rod was thrown
            Vector3 dest = start;
            Mob seek = NearestUnhit(mobs);
            if (seek != null) dest = seek.transform.position + Vector3.up * 0.4f;

            Vector3 to = dest - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.0001f)
                Dir = Vector3.RotateTowards(Dir, to.normalized, 8f * Time.deltaTime, 0f).normalized;

            transform.position += Dir * step;
            transform.rotation = Quaternion.FromToRotation(Vector3.up, Dir);
            returnTravelled += step;

            // returned home or travelled the full length back: done
            if (Vector3.Distance(start, transform.position) <= 0.35f || returnTravelled >= MaxDistance)
            {
                Destroy(gameObject);
                return;
            }
        }

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
    }

    Mob NearestUnhit(List<Mob> mobs)
    {
        if (mobs == null) return null;
        Mob best = null;
        float bestDist = float.MaxValue;
        for (int i = 0; i < mobs.Count; i++)
        {
            Mob m = mobs[i];
            if (m == null || alreadyHit.Contains(m)) continue;
            float d = Vector3.Distance(m.transform.position, transform.position);
            if (d < bestDist) { bestDist = d; best = m; }
        }
        return best;
    }
}
