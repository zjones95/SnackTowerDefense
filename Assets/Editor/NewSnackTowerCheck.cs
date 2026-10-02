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
