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
    public float SlowFactor = 0f;
    public float SlowDuration = 0f;

    public Color Tint = new Color(0.4f, 0.7f, 1f);
    public bool Spin = true;   // false = liquid blob, wobbles instead

    void Update()
    {
        if (Target == null) { Destroy(gameObject); return; }

        if (Spin)
        {
            // tumble the model so it reads as a thrown snack
            transform.Rotate(150f * Time.deltaTime, 210f * Time.deltaTime, 90f * Time.deltaTime);
        }
        else
        {
            float wob = 1f + Mathf.Sin(Time.time * 22f) * 0.07f;
            transform.localScale = new Vector3(wob, 2f - wob, wob);
        }

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

        if (SplashRadius > 0f)
        {
            SplashFX.Spawn(transform.position, SplashRadius, Tint);

            if (mobs != null)
            {
                for (int i = mobs.Count - 1; i >= 0; i--)
                {
                    Mob m = mobs[i];
                    if (m == null) continue;
                    if (Vector3.Distance(m.transform.position, transform.position) <= SplashRadius)
                        m.TakeDamage(Damage);
                }
            }
        }
        else if (Target != null)
        {
            if (SlowFactor > 0f) Target.ApplySlow(SlowFactor, SlowDuration);
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
