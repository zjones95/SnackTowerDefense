using System;
using System.Collections.Generic;
using UnityEngine;

// unity run . -- -executeMethod NewSnackTowerCheck.Verify
public static class NewSnackTowerCheck
{
    public static void Verify()
    {
        TowerType[] types = { TowerType.HotSauce, TowerType.CoffeeMug,
            TowerType.PopTartToaster, TowerType.CookieCrumbler, TowerType.SourFizz };
        foreach (TowerType type in types)
        {
            if (Array.IndexOf(TowerCatalog.RandomTypes, type) < 0)
                throw new Exception(type + " missing from random build pool");
            TowerDef def = TowerCatalog.Get(type);
            if (def.tiers.Count != 6) throw new Exception(type + " must have six tiers");
            GameObject prefab = SnackModels.Load(SnackModels.TowerPath(type));
            if (prefab == null) throw new Exception("Model failed to import: " + type);
            for (int tier = 1; tier <= 6; tier++)
            {
                TowerTierStats stats = def.Stats(tier);
                if (type == TowerType.HotSauce || type == TowerType.CoffeeMug)
                {
                    if (stats.auraRadius <= 0f || stats.auraBonus <= 0f)
                        throw new Exception(type + " has no aura at T" + tier);
                }
                else if (stats.range <= 0f || stats.fireInterval <= 0f)
                    throw new Exception(type + " cannot attack at T" + tier);
            }
            GameObject model = UnityEngine.Object.Instantiate(prefab);
            try
            {
                Renderer[] parts = model.GetComponentsInChildren<Renderer>();
                if (parts.Length == 0) throw new Exception(type + " has no visible geometry");
                Bounds bounds = parts[0].bounds;
                for (int i = 1; i < parts.Length; i++) bounds.Encapsulate(parts[i].bounds);
                if (bounds.size.y < .5f || bounds.size.y > 2f)
                    throw new Exception(type + " imported at unexpected height: " + bounds.size.y);
            }
            finally { UnityEngine.Object.DestroyImmediate(model); }
        }

        // The random draw bag must hand out every type once before repeating, so
        // merges can't drop the same tower several times in a row.
        TowerCatalog.ResetRandomBag();
        var drawn = new HashSet<TowerType>();
        for (int i = 0; i < TowerCatalog.RandomTypes.Length; i++) drawn.Add(TowerCatalog.RandomType());
        TowerCatalog.ResetRandomBag();
        if (drawn.Count != TowerCatalog.RandomTypes.Length)
            throw new Exception("Random draw bag repeated a type within one full cycle");

        // Owned one-shot upgrades must not be offered again (the pick did nothing).
        RogueMods.Reset();
        RogueMods.Owned.Add("Dessert");
        foreach (RogueDef d in RogueUpgrades.Offer(RogueUpgrades.Rares, 3))
            if (d.name == "Dessert")
                throw new Exception("An owned one-shot upgrade was offered again");
        RogueMods.Reset();

        // Attack-speed bonuses must add flatly, never compound.
        RogueMods.Reset();
        RogueMods.Rate = 0.25f + 0.25f;   // two Overclock picks
        if (Mathf.Abs(RogueMods.EffRate(false) - 1.50f) > 0.0001f)
            throw new Exception("Attack-speed bonuses compounded instead of adding");
        RogueMods.Overdrive = true;
        if (Mathf.Abs(RogueMods.EffRate(true) - 1.80f) > 0.0001f)
            throw new Exception("Overdrive did not add flatly on a boss wave");
        // Damage bonuses must add flatly too (two Heavy picks = +20%, not 1.21x).
        RogueMods.Reset();
        RogueMods.Damage = 0.10f + 0.10f;
        if (Mathf.Abs(RogueMods.DamageMult() - 1.20f) > 0.0001f)
            throw new Exception("Damage bonuses compounded instead of adding");
        RogueMods.Reset();

        // Support towers now also fire: weak single shots plus the aura.
        if (TowerCatalog.Get(TowerType.HotSauce).Stats(1).damage <= 0f ||
            TowerCatalog.Get(TowerType.CoffeeMug).Stats(6).damage <= 0f ||
            TowerCatalog.Get(TowerType.SourFizz).Stats(1).damage <= 0f)
            throw new Exception("Support towers must still fire single shots");
        if (Mathf.Abs(TowerCatalog.Get(TowerType.HotSauce).Stats(1).auraRadius - 4.375f) > .001f)
            throw new Exception("Aura ranges were not expanded by 25%");
        if (TowerCatalog.Get(TowerType.CookieCrumbler).Stats(3).crumbCount != 5)
            throw new Exception("Cookie Crumbler must fire five cookie bits");
        if (Mathf.Abs(TowerCatalog.Get(TowerType.PopTartToaster).Stats(1).splashRadius - .9f) > .001f)
            throw new Exception("Toaster ground splash was not reduced to 60%");

        // Flat armour reduction must be multiplicative on armour, not damage.
        GameObject mobGo = new GameObject("SourDamageCheck");
        try
        {
            Mob mob = mobGo.AddComponent<Mob>();
            mob.Def = new MobDef { armour = 10f };
            mob.Health = mob.MaxHealth = 100f;
            mob.TakeDamage(20f);
            float plainHit = 100f - mob.Health;
            mob.ApplySour(.4f, 4f, null, 0f);
            mob.TakeDamage(20f);
            float souredHit = 100f - plainHit - mob.Health;
            if (Mathf.Abs(souredHit - plainHit - 4f) > .01f)
                throw new Exception("Sour Fizz did not remove 40% of 10 flat armour");
        }
        finally { UnityEngine.Object.DestroyImmediate(mobGo); }

        var objects = new List<GameObject>();
        try
        {
            Tower receiver = MakeTower(objects, TowerType.SingleShot, 1, 0f);
            Tower weak = MakeTower(objects, TowerType.HotSauce, 1, 1f);
            Tower strong = MakeTower(objects, TowerType.HotSauce, 6, 1f);
            Tower coffee = MakeTower(objects, TowerType.CoffeeMug, 4, 2f);
            var towers = new List<Tower> { receiver, weak, strong, coffee };
            float dmg, speed;
            Tower.CalculateSnackBuffs(receiver, towers, out dmg, out speed);
            if (Mathf.Abs(dmg - .33f) > .001f || Mathf.Abs(speed - .11f) > .001f)
                throw new Exception("Support auras must use strongest same-type bonus and both different types");
            weak.transform.position = new Vector3(50f, 0f, 0f);
            Tower.CalculateSnackBuffs(receiver, towers, out dmg, out speed);
            if (Mathf.Abs(dmg - .33f) > .001f)
                throw new Exception("T6 support bonus must count same-type towers offscreen");
            strong.transform.position = new Vector3(50f, 0f, 0f);
            Tower.CalculateSnackBuffs(receiver, towers, out dmg, out speed);
            if (dmg != 0f) throw new Exception("Support auras reach outside their radius");
        }
        finally { foreach (GameObject obj in objects) UnityEngine.Object.DestroyImmediate(obj); }
        Debug.Log("NewSnackTowerCheck OK: five models, 30 tiers, sour armour, aura stacking/radius");
    }

    /// <summary>Logs the mob-health curve so LateWaveCheck's expected values can
    /// be re-derived after a deliberate rebalance.</summary>
    public static void DumpCurve()
    {
        for (int w = 25; w <= 50; w += 5)
            Debug.Log("HealthMult(" + w + ") = " + TDBalance.HealthMult(w).ToString("F4"));
        MobDef caesar = MobCatalog.Get("CaesarSalad");
        MobDef granola = MobCatalog.Get("GranolaMom");
        Debug.Log("CaesarSalad@45 HP = " + (caesar.health * TDBalance.HealthMult(45) * TDBalance.MobHealthScale).ToString("F1"));
        Debug.Log("GranolaMom@50 HP = " + (granola.health * TDBalance.HealthMult(50) * TDBalance.MobHealthScale).ToString("F1"));
    }

    /// <summary>Renders the five new towers at tiers 1/3/6 through the real
    /// TowerVisual path so bases and centring can be checked after a model change.</summary>
    public static void RenderNewTowers()
    {
        GameObject lightGO = new GameObject("PreviewLight");
        Light l = lightGO.AddComponent<Light>();
        l.type = LightType.Directional;
        l.color = new Color(1f, .90f, .74f);
        l.intensity = 1.15f;
        l.shadows = LightShadows.Soft;
        l.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.26f, .21f, .17f);

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        UnityEngine.Object.DestroyImmediate(ground.GetComponent<Collider>());
        ground.transform.position = new Vector3(0f, -.5f, 0f);
        ground.transform.localScale = new Vector3(30f, 1f, 16f);
        ground.GetComponent<Renderer>().sharedMaterial = TDVisuals.Mat(new Color(.22f, .28f, .22f), 0f, .2f);

        TowerType[] types = { TowerType.HotSauce, TowerType.CoffeeMug, TowerType.PopTartToaster,
                              TowerType.CookieCrumbler, TowerType.SourFizz };
        int[] tiers = { 1, 3, 6 };
        for (int i = 0; i < types.Length; i++)
        {
            for (int j = 0; j < tiers.Length; j++)
            {
                GameObject go = new GameObject("T_" + types[i] + "_" + tiers[j]);
                go.transform.position = new Vector3(-6f + i * 3f, 0f, 2.6f - j * 2.8f);
                Tower t = go.AddComponent<Tower>();
                t.Setup(types[i], tiers[j], 0, 0);
                if (j == 1)   // show the range ring at T3 (Update sizes it at runtime)
                {
                    t.SetSelected(true);
                    Transform ring = go.transform.Find("RangeRing");
                    if (ring != null) ring.localScale = new Vector3(11f, 11f, 1f);
                }
            }
        }

        GameObject camGO = new GameObject("PreviewCam");
        camGO.tag = "MainCamera";
        Camera cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6.5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(.075f, .065f, .06f);
        cam.transform.position = new Vector3(-9f, 13f, -16f);
        cam.transform.LookAt(new Vector3(0f, .5f, 0f));

        int w = 1400, h = 820;
        RenderTexture rt = new RenderTexture(w, h, 24);
        rt.antiAliasing = 8;
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "opencode");
        System.IO.Directory.CreateDirectory(dir);
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, "snack_new_towers_render.png"), tex.EncodeToPNG());
        Debug.Log("NewSnackTowerCheck: wrote snack_new_towers_render.png");
    }

    /// <summary>Logs each new tower model's renderer bounds so centring/size
    /// problems can be measured rather than eyeballed.</summary>
    public static void DumpBounds()
    {
        TowerType[] types = { TowerType.CoffeeMug, TowerType.HotSauce, TowerType.PopTartToaster,
                              TowerType.CookieCrumbler, TowerType.SourFizz, TowerType.SingleShot };
        foreach (TowerType type in types)
        {
            GameObject prefab = SnackModels.Load(SnackModels.TowerPath(type));
            if (prefab == null) { Debug.Log(type + ": MISSING"); continue; }
            GameObject go = UnityEngine.Object.Instantiate(prefab);
            Renderer[] rs = go.GetComponentsInChildren<Renderer>();
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            Debug.Log(type + ": union center=" + b.center.ToString("F3") + " size=" + b.size.ToString("F3")
                      + " min=" + b.min.ToString("F3"));
            for (int i = 0; i < rs.Length; i++)
                Debug.Log("    " + rs[i].name + " center=" + rs[i].bounds.center.ToString("F3")
                          + " size=" + rs[i].bounds.size.ToString("F3"));
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    /// <summary>Writes the shield/sword/lightning badges side by side for a quick
    /// visual check that each icon is filled in.</summary>
    public static void RenderIcons()
    {
        Texture2D[] icons = { TDTextures.IconArmour(), TDTextures.IconSword(), TDTextures.IconLightning() };
        int cell = 64, pad = 10;
        int w = icons.Length * cell + (icons.Length + 1) * pad;
        int h = cell + 2 * pad;
        Texture2D sheet = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color bg = new Color(.12f, .12f, .14f, 1f);
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) sheet.SetPixel(x, y, bg);
        for (int i = 0; i < icons.Length; i++)
        {
            int ox = pad + i * (cell + pad), oy = pad;
            for (int y = 0; y < cell; y++) for (int x = 0; x < cell; x++)
            {
                Color c = icons[i].GetPixel(x, y);
                sheet.SetPixel(ox + x, oy + y, Color.Lerp(bg, c, c.a));
            }
        }
        sheet.Apply();
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "opencode");
        System.IO.Directory.CreateDirectory(dir);
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, "snack_buff_icons.png"), sheet.EncodeToPNG());
        Debug.Log("NewSnackTowerCheck: wrote snack_buff_icons.png");
    }

    /// <summary>Renders the three badges with their overlaid percentage text so the
    /// centred layout and black outline can be eyeballed.</summary>
    public static void RenderBadges()
    {
        GameObject lightGO = new GameObject("PreviewLight");
        Light l = lightGO.AddComponent<Light>();
        l.type = LightType.Directional;
        l.color = new Color(1f, .9f, .74f);
        l.intensity = 1.2f;
        l.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.30f, .30f, .33f);

        GameObject root = new GameObject("Badges");
        Badge(root.transform, MobVisual.IconMaterial(TDTextures.IconSword()), "40%", new Vector3(-.7f, 0f, 0f));
        Badge(root.transform, MobVisual.IconMaterial(TDTextures.IconLightning()), "15%", new Vector3(0f, 0f, 0f));
        Badge(root.transform, MobVisual.IconMaterial(TDTextures.IconArmour()), "45%", new Vector3(.7f, 0f, 0f));

        GameObject camGO = new GameObject("PreviewCam");
        camGO.tag = "MainCamera";
        Camera cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = .7f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(.10f, .10f, .12f);
        cam.transform.position = new Vector3(0f, 0f, -5f);
        cam.transform.LookAt(Vector3.zero);

        int w = 720, h = 280;
        RenderTexture rt = new RenderTexture(w, h, 24);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "opencode");
        System.IO.Directory.CreateDirectory(dir);
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, "snack_badge_preview.png"), tex.EncodeToPNG());
        Debug.Log("NewSnackTowerCheck: wrote snack_badge_preview.png");
    }

    static void Badge(Transform parent, Material mat, string text, Vector3 pos)
    {
        GameObject q = TDVisuals.Quad(parent, "Icon", pos, .5f, mat);
        OutlinedText.Build(q.transform, text, .036f, Color.white, new Vector3(0f, 0f, -.01f));
    }

    static Tower MakeTower(List<GameObject> objects, TowerType type, int tier, float x)
    {
        GameObject obj = new GameObject(type.ToString());
        objects.Add(obj);
        obj.transform.position = new Vector3(x, 0f, 0f);
        Tower tower = obj.AddComponent<Tower>();
        tower.Type = type; tower.Tier = tier;
        return tower;
    }
}
