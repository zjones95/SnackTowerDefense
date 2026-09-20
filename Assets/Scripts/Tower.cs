using System.Collections.Generic;
using UnityEngine;

public class Tower : MonoBehaviour
{
    public TowerType Type;
    public int Tier = 1;
    public int CellX, CellY;

    private float cooldown;
    private Transform turret;
    private GameObject selectHighlight;
    private Transform tierLabel;
    private bool isSelected;

    public TowerTierStats Stats { get { return TowerCatalog.Get(Type).Stats(Tier); } }
    public string DisplayName { get { return TowerCatalog.Get(Type).displayName; } }

    public void Setup(TowerType type, int tier, int cx, int cy)
    {
        Type = type; Tier = tier; CellX = cx; CellY = cy;
        BuildVisual();
        cooldown = 0f;
    }

    void BuildVisual()
    {
        GameObject prefab = SnackModels.Load(SnackModels.TowerPath(Type));
        if (prefab != null)
        {
            GameObject model = Instantiate(prefab, transform);
            model.name = "Model";
            model.transform.localPosition = Vector3.zero;
            model.transform.localScale = Vector3.one;
            SnackModels.CenterOn(model, transform.position);

            // split base (pedestal/rim) from the rotating head
            Transform pivot = new GameObject("HeadPivot").transform;
            pivot.SetParent(transform, false);
            List<Transform> heads = new List<Transform>();
            foreach (Transform tr in model.GetComponentsInChildren<Transform>())
            {
                if (tr == model.transform) continue;
                string n = tr.name.ToLower();
                if (n.Contains("pedestal") || n.Contains("rim")) continue;
                heads.Add(tr);
            }
            foreach (Transform h in heads) h.SetParent(pivot, true);
            turret = pivot;
        }
        else
        {
            GameObject tg = new GameObject("Turret");
            tg.transform.SetParent(transform, false);
            tg.transform.localPosition = new Vector3(0f, 0.70f, 0f);
            turret = tg.transform;
            SnackArt.BuildTower(transform, turret, Type, Tier);
        }

        // tier number above the tower (replaces the old pips)
        GameObject tierGO = new GameObject("TierLabel");
        tierGO.transform.SetParent(transform, false);
        tierGO.transform.localPosition = new Vector3(0f, 1.55f, 0f);
        tierLabel = tierGO.transform;

        TDVisuals.Box(tierGO.transform, "Badge", new Vector3(0f, 0f, 0.02f), new Vector3(0.40f, 0.52f, 0.02f),
            TDVisuals.TransparentMat(new Color(0.08f, 0.08f, 0.10f), 0.5f, 0.3f));

        GameObject txtGO = new GameObject("Text");
        txtGO.transform.SetParent(tierGO.transform, false);
        txtGO.transform.localPosition = new Vector3(0f, 0f, -0.02f);
        TextMesh tm = txtGO.AddComponent<TextMesh>();
        tm.text = Tier.ToString();
        tm.characterSize = 0.055f;
        tm.fontSize = 110;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = new Color(1f, 0.96f, 0.70f);
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font != null)
        {
            tm.font = font;
            MeshRenderer tr = txtGO.GetComponent<MeshRenderer>();
            if (tr != null) tr.sharedMaterial = font.material;
        }

        // pronounced selection highlight: pulsing ring + beam
        selectHighlight = new GameObject("SelectHighlight");
        selectHighlight.transform.SetParent(transform, false);
        Material hi = TDVisuals.Mat(new Color(1f, 0.88f, 0.15f), 0.1f, 0.8f);
        int seg = 28;
        for (int i = 0; i < seg; i++)
        {
            float a = i / (float)seg * Mathf.PI * 2f;
            TDVisuals.Box(selectHighlight.transform, "R" + i,
                new Vector3(Mathf.Cos(a) * 0.62f, 0.06f, Mathf.Sin(a) * 0.62f),
                new Vector3(0.18f, 0.08f, 0.18f), hi);
        }
        TDVisuals.Cyl(selectHighlight.transform, "Beam", new Vector3(0f, 1.4f, 0f), 0.06f, 2.8f, hi);
        selectHighlight.SetActive(false);
    }

    public void SetSelected(bool on)
    {
        isSelected = on;
        if (selectHighlight != null) selectHighlight.SetActive(on);
    }

    void Update()
    {
        if (isSelected && selectHighlight != null)
        {
            float pulse = 1f + Mathf.Sin(Time.time * 6f) * 0.10f;
            selectHighlight.transform.localScale = new Vector3(pulse, 1f, pulse);
            selectHighlight.transform.localRotation = Quaternion.Euler(0f, Time.time * 45f, 0f);
        }

        if (tierLabel != null && Camera.main != null)
            tierLabel.rotation = Camera.main.transform.rotation; // match the camera so the digit reads the right way up

        TowerTierStats s = Stats;
        var mobs = TDGameManager.Instance != null ? TDGameManager.Instance.Mobs : null;

        Mob target = null;
        float best = float.MinValue;
        if (mobs != null)
        {
            for (int i = 0; i < mobs.Count; i++)
            {
                Mob m = mobs[i];
                if (m == null) continue;
                if (Vector3.Distance(transform.position, m.transform.position) <= s.range && m.Progress > best)
                {
                    best = m.Progress;
                    target = m;
                }
            }
        }

        if (turret != null && target != null)
        {
            Vector3 dir = target.transform.position - turret.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                turret.rotation = Quaternion.Slerp(turret.rotation, Quaternion.LookRotation(dir), 12f * Time.deltaTime);
        }

        cooldown -= Time.deltaTime;
        if (cooldown > 0f) return;

        if (Type == TowerType.Slow)
        {
            bool any = false;
            if (mobs != null)
            {
                for (int i = 0; i < mobs.Count; i++)
                {
                    Mob m = mobs[i];
                    if (m == null) continue;
                    if (Vector3.Distance(transform.position, m.transform.position) <= s.range)
                    {
                        m.ApplySlow(s.slowFactor, s.slowDuration);
                        any = true;
                    }
                }
            }
            if (any)
            {
                cooldown = s.fireInterval;
                if (TDAudio.Instance != null) TDAudio.Instance.Shot(Type);
            }
            return;
        }

        if (target == null) return;
        cooldown = s.fireInterval;
        if (TDAudio.Instance != null) TDAudio.Instance.Shot(Type);

        Vector3 muzzle = turret != null ? turret.position + turret.forward * 0.5f : transform.position + Vector3.up * 0.8f;

        switch (Type)
        {
            case TowerType.Sniper:
                target.TakeDamage(s.damage);
                Tracer(muzzle, target.transform.position + Vector3.up * 0.4f);
                break;
            case TowerType.Pierce:
                FirePierce(muzzle, target, s);
                break;
            case TowerType.Chain:
                FireChain(muzzle, target, s);
                break;
            default:
                SpawnProjectile(muzzle, target, s);
                break;
        }
    }

    void FirePierce(Vector3 from, Mob target, TowerTierStats s)
    {
        Vector3 dir = target.transform.position - from;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        dir.Normalize();

        var mobs = TDGameManager.Instance != null ? TDGameManager.Instance.Mobs : null;
        List<Mob> hits = new List<Mob>();
        if (mobs != null)
        {
            for (int i = 0; i < mobs.Count; i++)
            {
                Mob m = mobs[i];
                if (m == null) continue;
                Vector3 to = m.transform.position - from;
                to.y = 0f;
                float along = Vector3.Dot(to, dir);
                if (along < 0f || along > s.range) continue;
                float perp = Vector3.Cross(dir, to).magnitude;
                if (perp > s.pierceWidth) continue;
                hits.Add(m);
            }
        }
        hits.Sort((a, b) => Vector3.Distance(from, a.transform.position).CompareTo(Vector3.Distance(from, b.transform.position)));

        int n = Mathf.Min(hits.Count, Mathf.Max(1, s.pierceCount));
        for (int i = 0; i < n; i++) hits[i].TakeDamage(s.damage);

        Vector3 end = n > 0 ? hits[n - 1].transform.position + Vector3.up * 0.4f : from + dir * s.range;
        Tracer(from, end);
    }

    void FireChain(Vector3 from, Mob target, TowerTierStats s)
    {
        var mobs = TDGameManager.Instance != null ? TDGameManager.Instance.Mobs : null;
        List<Mob> hit = new List<Mob>();
        List<Vector3> pts = new List<Vector3>();

        Mob cur = target;
        for (int i = 0; i <= s.chainCount && cur != null; i++)
        {
            Vector3 cp = cur.transform.position + Vector3.up * 0.4f;
            pts.Add(cp);
            hit.Add(cur);
            cur.TakeDamage(s.damage * Mathf.Pow(0.75f, i));

            Mob next = null;
            float best = s.chainRange;
            if (mobs != null)
            {
                for (int j = 0; j < mobs.Count; j++)
                {
                    Mob m = mobs[j];
                    if (m == null || hit.Contains(m)) continue;
                    float d = Vector3.Distance(cp, m.transform.position);
                    if (d <= best) { best = d; next = m; }
                }
            }
            cur = next;
        }

        for (int i = 0; i < pts.Count; i++) Tracer(i == 0 ? from : pts[i - 1], pts[i]);
    }

    void SpawnProjectile(Vector3 from, Mob target, TowerTierStats s)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Collider c = go.GetComponent<Collider>();
        if (c != null) Destroy(c);
        go.name = "Projectile";
        go.transform.position = from;
        go.transform.localScale = Vector3.one * 0.22f;
        TowerDef def = TowerCatalog.Get(Type);
        go.GetComponent<Renderer>().sharedMaterial = TDVisuals.Mat(def.color, 0.1f, 0.6f);

        Transform parent = TDGameManager.Instance != null ? TDGameManager.Instance.ProjectilesRoot : null;
        if (parent != null) go.transform.SetParent(parent, true);

        Projectile p = go.AddComponent<Projectile>();
        p.Target = target;
        p.Speed = s.projectileSpeed;
        p.Damage = s.damage;
        p.SplashRadius = s.splashRadius;
        p.Bounces = s.bounceCount;
        p.BounceRange = s.bounceRange;
        p.PoisonDps = s.poisonDps;
        p.PoisonDuration = s.poisonDuration;
    }

    void Tracer(Vector3 a, Vector3 b)
    {
        Vector3 dir = b - a;
        float len = dir.magnitude;
        if (len < 0.01f) return;

        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Collider c = g.GetComponent<Collider>();
        if (c != null) Destroy(c);
        g.name = "Tracer";
        g.transform.position = a + dir * 0.5f;
        g.transform.rotation = Quaternion.LookRotation(dir / len);
        g.transform.localScale = new Vector3(0.04f, 0.04f, len);
        g.GetComponent<Renderer>().sharedMaterial = TDVisuals.Mat(new Color(1f, 0.9f, 0.6f), 0f, 0.7f);
        Destroy(g, 0.06f);
    }
}
