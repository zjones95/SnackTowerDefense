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
    public int GoldPerHit = 0;   // Gold tower: money awarded on a confirmed hit

    public Color Tint = new Color(0.4f, 0.7f, 1f);
    public bool Spin = true;   // false = liquid blob, wobbles instead
    public bool Arc = false;   // true = hops in a parabola (bouncing)

    private bool arcInit;
    private Vector3 arcStart;
    private float arcT;
    private float arcDur;
    private float arcLift;

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

        if (Arc) ArcStep(); else StraightStep();
    }

    void StraightStep()
    {
        Vector3 tp = Target.transform.position + Vector3.up * 0.4f;
        Vector3 dir = tp - transform.position;
        float step = Speed * Time.deltaTime;
        if (dir.magnitude <= step) { Hit(); return; }
        transform.position += dir.normalized * step;
    }

    // a real hop: parabola from where we are to the next target
    void ArcStep()
    {
        Vector3 end = Target.transform.position + Vector3.up * 0.4f;

        if (!arcInit)
        {
            arcInit = true;
            arcStart = transform.position;
            arcT = 0f;
            float d = Vector3.Distance(arcStart, end);
            arcDur = Mathf.Max(0.10f, d / Mathf.Max(1f, Speed));
            arcLift = Mathf.Min(1.6f, d * 0.30f);
        }

        arcT += Time.deltaTime / arcDur;
        float k = Mathf.Clamp01(arcT);
        Vector3 pos = Vector3.Lerp(arcStart, end, k);
        pos.y += Mathf.Sin(k * Mathf.PI) * arcLift;
        transform.position = pos;

        if (k >= 1f) Hit();
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
            // economy towers pay out only on a confirmed hit (the target still exists)
            if (GoldPerHit > 0 && gm != null) gm.AwardMoney(GoldPerHit);
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
                arcInit = false;   // start a fresh hop
                return; // keep flying
            }
        }

        Destroy(gameObject);
    }
}
