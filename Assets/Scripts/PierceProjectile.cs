using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A thrown skewer: travels in a straight line at a slow, steady pace and
/// skewers every enemy it passes through, up to a hit cap. Unlike the homing
/// projectiles it never changes course and doesn't die on the first hit.
/// At Pierce T6 ("Boomerang Skewer") it travels its full length, then returns
/// straight to the tower that threw it, skewering again at full damage.
/// </summary>
public class PierceProjectile : MonoBehaviour
{
    public Vector3 Dir = Vector3.forward;
    public float Speed = 9f;
    public float Damage = 10f;
    public Tower Source;   // tower credited with the damage
    public TowerType Type; // which tower fired this (for remote visual type)
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
            // boomerang straight back to the tower that threw it
            Vector3 to = start - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.0001f) Dir = to.normalized;

            transform.position += Dir * step;
            transform.rotation = Quaternion.FromToRotation(Vector3.up, Dir);
            returnTravelled += step;

            // back at the tower (or, as a safety, travelled its full length twice): done
            if (Vector3.Distance(start, transform.position) <= Mathf.Max(0.35f, step)
                || returnTravelled >= MaxDistance * 2f)
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
                    if (Source != null) Source.AddDamage(Damage);
                    hits++;
                }
            }
        }
    }
}
