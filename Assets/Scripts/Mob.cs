using System.Collections.Generic;
using UnityEngine;

public class Mob : MonoBehaviour
{
    public MobDef Def;
    public float Health;
    public float MaxHealth;
    public float Progress;   // distance travelled along the path (for targeting)
    public ushort NetId;     // stable id for spectating

    private List<Vector3> path;
    private int pathIndex;
    private float slowTimer;
    private float speedMul = 1f;
    private float waveSpeed = 1f;
    private float poisonDps;
    private float poisonTimer;
    private TDGameManager game;

    private Transform hpRoot;
    private Transform hpFill;
    private float barWidth = 0.9f;

    public void Init(MobDef def, List<Vector3> waypoints, TDGameManager g, float healthMult, float speedMult)
    {
        Def = def;
        path = waypoints;
        game = g;
        waveSpeed = speedMult;
        MaxHealth = def.health * healthMult;
        Health = MaxHealth;
        pathIndex = 1;
        transform.position = path[0];

        hpRoot = MobVisual.Build(transform, def, out hpFill);
        barWidth = MobVisual.BarWidth;
    }

    void Update()
    {
        if (path == null || pathIndex >= path.Count) return;

        if (slowTimer > 0f)
        {
            slowTimer -= Time.deltaTime;
            if (slowTimer <= 0f) speedMul = 1f;
        }

        if (poisonTimer > 0f)
        {
            poisonTimer -= Time.deltaTime;
            Health -= poisonDps * Time.deltaTime;
            UpdateBar();
            if (Health <= 0f) { Die(); return; }
        }

        float speed = Def.speed * waveSpeed * speedMul;
        Vector3 target = path[pathIndex];
        Vector3 pos = transform.position;
        Vector3 flat = new Vector3(target.x - pos.x, 0f, target.z - pos.z);
        float dist = flat.magnitude;
        float step = speed * Time.deltaTime;

        if (dist <= step)
        {
            transform.position = new Vector3(target.x, 0f, target.z);
            Progress += dist;
            pathIndex++;
            if (pathIndex >= path.Count) { ReachEnd(); return; }
        }
        else
        {
            Vector3 dir = flat / dist;
            transform.position = pos + dir * step;
            Progress += step;
            transform.rotation = Quaternion.LookRotation(dir);
        }

        // billboard the health bar toward the camera (also keeps fill/bg from z-fighting)
        if (hpRoot != null && Camera.main != null)
        {
            Vector3 dir = Camera.main.transform.position - hpRoot.position;
            if (dir.sqrMagnitude > 0.001f) hpRoot.rotation = Quaternion.LookRotation(dir);
        }
    }

    public void ApplySlow(float removedFraction, float duration)
    {
        float mul = 1f - Mathf.Clamp01(removedFraction);
        if (mul < speedMul) speedMul = mul;
        slowTimer = Mathf.Max(slowTimer, duration);
    }

    public void ApplyPoison(float dps, float duration)
    {
        if (dps > poisonDps) poisonDps = dps;
        poisonTimer = Mathf.Max(poisonTimer, duration);
    }

    public void TakeDamage(float dmg)
    {
        if (dmg <= 0f || Health <= 0f) return;
        Health -= dmg;
        UpdateBar();
        if (Health <= 0f) Die();
    }

    void UpdateBar()
    {
        if (hpFill == null) return;
        float f = Mathf.Clamp01(Health / MaxHealth);
        hpFill.localScale = new Vector3(barWidth * f, hpFill.localScale.y, hpFill.localScale.z);
        hpFill.localPosition = new Vector3(-(barWidth * (1f - f)) * 0.5f, hpFill.localPosition.y, hpFill.localPosition.z);
    }

    void Die()
    {
        if (TDAudio.Instance != null) TDAudio.Instance.Death();
        if (game != null) game.OnMobKilled(this);
        Destroy(gameObject);
    }

    void ReachEnd()
    {
        if (TDAudio.Instance != null) TDAudio.Instance.Leak();
        if (game != null) game.OnMobLeaked(this);
        Destroy(gameObject);
    }
}
