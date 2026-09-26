using UnityEngine;

public class Projectile : MonoBehaviour
{
    public Mob Target;
    public float Speed = 22f;
    public float Damage = 10f;
    public float SplashRadius = 0f;
    public int Bounces = 0;
    public float BounceRange = 0f;
    public float PoisonDps = 0f;
    public float PoisonDuration = 0f;

    void Update()
    {
        if (Target == null) { Destroy(gameObject); return; }

        // tumble the model so it reads as a thrown snack
        transform.Rotate(150f * Time.deltaTime, 210f * Time.deltaTime, 90f * Time.deltaTime);

        Vector3 tp = Target.transform.position + Vector3.up * 0.4f;
        Vector3 dir = tp - transform.position;
        float step = Speed * Time.deltaTime;
        if (dir.magnitude <= step) { Hit(); return; }
        transform.position += dir.normalized * step;
    }

    void Hit()
    {
        TDGameManager gm = TDGameManager.Instance;
        var mobs = gm != null ? gm.Mobs : null;

        if (SplashRadius > 0f && mobs != null)
        {
            for (int i = mobs.Count - 1; i >= 0; i--)
            {
                Mob m = mobs[i];
                if (m == null) continue;
                if (Vector3.Distance(m.transform.position, transform.position) <= SplashRadius)
                    m.TakeDamage(Damage);
            }
        }
        else if (Target != null)
        {
            if (PoisonDps > 0f) Target.ApplyPoison(PoisonDps, PoisonDuration);
            Target.TakeDamage(Damage);
        }

        // ricochet to the nearest other enemy
        if (Bounces > 0 && mobs != null && Target != null)
        {
            Mob next = null;
            float best = BounceRange;
            for (int i = 0; i < mobs.Count; i++)
            {
                Mob m = mobs[i];
                if (m == null || m == Target) continue;
                float d = Vector3.Distance(transform.position, m.transform.position);
                if (d <= best) { best = d; next = m; }
            }
            if (next != null)
            {
                Bounces--;
                Target = next;
                return; // keep flying
            }
        }

        Destroy(gameObject);
    }
}
