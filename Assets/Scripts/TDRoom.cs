using UnityEngine;

// Builds environments around the grid map. Bedroom is the original kid's room;
// ArcticOutpost is the winter reskin (same Layout/Route, only visuals change).
public static class TDRoom
{
    public static void Build(Transform parent, TDMap map, BoardTheme theme = BoardTheme.Bedroom)
    {
        if (theme == BoardTheme.ArcticOutpost) { BuildArctic(parent, map); return; }
        if (theme == BoardTheme.VolcanicCaldera) { BuildVolcanic(parent, map); return; }
        BuildBedroom(parent, map);
    }

    static void BuildBedroom(Transform parent, TDMap map)
    {
        Material carpet = TDVisuals.Mat(new Color(0.60f, 0.54f, 0.44f), 0f, 0.25f);
        Material wall = TDVisuals.Mat(new Color(0.56f, 0.62f, 0.68f), 0f, 0.3f);
        Material baseboard = TDVisuals.Mat(new Color(0.82f, 0.82f, 0.83f), 0f, 0.45f);
        Material wood = TDVisuals.Mat(new Color(0.58f, 0.42f, 0.25f), 0f, 0.4f);
        Material blanket = TDVisuals.Mat(new Color(0.76f, 0.28f, 0.34f), 0f, 0.5f);
        Material pillow = TDVisuals.Mat(new Color(0.86f, 0.84f, 0.78f), 0f, 0.5f);
        Material metal = TDVisuals.Mat(new Color(0.62f, 0.64f, 0.68f), 0.7f, 0.4f);
        Material shade = TDVisuals.Mat(new Color(0.88f, 0.80f, 0.52f), 0f, 0.5f);

        float halfX = map.Width * map.Cell * 0.5f + 6f;   // room extends past the grid
        float halfZ = map.Height * map.Cell * 0.5f + 6f;

        // carpet floor
        TDVisuals.Box(parent, "RoomFloor", new Vector3(0f, -0.15f, 0f), new Vector3(halfX * 2f, 0.2f, halfZ * 2f), carpet);

        // low walls + baseboards
        float wh = 2.4f, wt = 0.4f;
        TDVisuals.Box(parent, "WallN", new Vector3(0f, wh * 0.5f, halfZ), new Vector3(halfX * 2f + wt, wh, wt), wall);
        TDVisuals.Box(parent, "WallS", new Vector3(0f, wh * 0.5f, -halfZ), new Vector3(halfX * 2f + wt, wh, wt), wall);
        TDVisuals.Box(parent, "WallE", new Vector3(halfX, wh * 0.5f, 0f), new Vector3(wt, wh, halfZ * 2f + wt), wall);
        TDVisuals.Box(parent, "WallW", new Vector3(-halfX, wh * 0.5f, 0f), new Vector3(wt, wh, halfZ * 2f + wt), wall);
        TDVisuals.Box(parent, "BaseN", new Vector3(0f, 0.15f, halfZ - wt * 0.5f), new Vector3(halfX * 2f, 0.3f, 0.12f), baseboard);
        TDVisuals.Box(parent, "BaseS", new Vector3(0f, 0.15f, -halfZ + wt * 0.5f), new Vector3(halfX * 2f, 0.3f, 0.12f), baseboard);
        TDVisuals.Box(parent, "BaseE", new Vector3(halfX - wt * 0.5f, 0.15f, 0f), new Vector3(0.12f, 0.3f, halfZ * 2f), baseboard);
        TDVisuals.Box(parent, "BaseW", new Vector3(-halfX + wt * 0.5f, 0.15f, 0f), new Vector3(0.12f, 0.3f, halfZ * 2f), baseboard);

        float westX = -halfX + 3.0f;
        float eastX = halfX - 3.0f;

        // bed (along the west wall)
        TDVisuals.Box(parent, "BedFrame", new Vector3(westX, 0.35f, 2f), new Vector3(2.8f, 0.7f, 4.6f), wood);
        TDVisuals.Box(parent, "Mattress", new Vector3(westX, 0.85f, 2f), new Vector3(2.6f, 0.4f, 4.4f), pillow);
        TDVisuals.Box(parent, "Blanket", new Vector3(westX, 1.05f, 3.0f), new Vector3(2.6f, 0.2f, 2.4f), blanket);
        TDVisuals.Box(parent, "Pillow", new Vector3(westX, 1.1f, -0.1f), new Vector3(1.6f, 0.3f, 0.9f), pillow);
        TDVisuals.Box(parent, "Headboard", new Vector3(westX - 1.3f, 1.0f, 2f), new Vector3(0.2f, 2.0f, 4.6f), wood);

        // bookshelf (east wall)
        TDVisuals.Box(parent, "Shelf", new Vector3(eastX, 1.0f, 0f), new Vector3(1.2f, 2.0f, 3.0f), wood);
        Color[] bookCols = { new Color(0.9f,0.3f,0.3f), new Color(0.3f,0.6f,0.9f), new Color(0.95f,0.8f,0.25f), new Color(0.4f,0.8f,0.4f) };
        for (int s = 0; s < 3; s++)
        {
            TDVisuals.Box(parent, "ShelfBoard" + s, new Vector3(eastX, 0.5f + s * 0.65f, 0f), new Vector3(1.25f, 0.08f, 3.0f), wood);
            for (int b = 0; b < 4; b++)
                TDVisuals.Box(parent, "Book" + s + "_" + b,
                    new Vector3(eastX, 0.72f + s * 0.65f, -0.9f + b * 0.55f),
                    new Vector3(0.9f, 0.45f, 0.16f), TDVisuals.Mat(bookCols[(s + b) % bookCols.Length], 0f, 0.5f));
        }

        // toy chest (north-east)
        TDVisuals.Box(parent, "Chest", new Vector3(eastX, 0.6f, 9f), new Vector3(2.2f, 1.2f, 1.6f), wood);
        TDVisuals.Box(parent, "ChestLid", new Vector3(eastX, 1.3f, 9f), new Vector3(2.3f, 0.2f, 1.7f), TDVisuals.Mat(new Color(0.62f,0.42f,0.24f), 0f, 0.4f));
        TDVisuals.Box(parent, "ChestStripe", new Vector3(eastX, 0.6f, 9f), new Vector3(2.28f, 0.3f, 1.68f), metal);

        // block stack (north-east floor)
        Color[] blockCols = { new Color(0.9f,0.35f,0.35f), new Color(0.35f,0.6f,0.9f), new Color(0.95f,0.82f,0.3f), new Color(0.45f,0.85f,0.45f) };
        for (int i = 0; i < 4; i++)
            TDVisuals.Box(parent, "Block" + i, new Vector3(eastX + 0.2f, 0.35f + i * 0.7f, 11.5f),
                new Vector3(0.7f, 0.7f, 0.7f), TDVisuals.Mat(blockCols[i], 0f, 0.5f));

        // ball
        TDVisuals.Sphere(parent, "Ball", new Vector3(westX, 0.55f, -9f), 1.1f, blanket);

        // lamp
        TDVisuals.Cyl(parent, "LampPole", new Vector3(westX - 0.6f, 1.1f, 10f), 0.08f, 2.2f, metal);
        TDVisuals.Cyl(parent, "LampShade", new Vector3(westX - 0.6f, 2.35f, 10f), 0.55f, 0.7f, shade);
    }

    static void BuildArctic(Transform parent, TDMap map)
    {
        Material snow = TDVisuals.Mat(new Color(0.92f, 0.94f, 0.97f), 0f, 0.4f);
        Material ice = TDVisuals.Mat(new Color(0.75f, 0.85f, 0.92f), 0f, 0.3f);
        Material iceDark = TDVisuals.Mat(new Color(0.62f, 0.75f, 0.85f), 0f, 0.3f);
        Material wood = TDVisuals.Mat(new Color(0.45f, 0.32f, 0.20f), 0f, 0.4f);
        Material woodDark = TDVisuals.Mat(new Color(0.36f, 0.25f, 0.15f), 0f, 0.4f);
        Material metal = TDVisuals.Mat(new Color(0.45f, 0.48f, 0.52f), 0.7f, 0.4f);
        Material pine = TDVisuals.Mat(new Color(0.22f, 0.45f, 0.30f), 0f, 0.5f);
        Material trunk = TDVisuals.Mat(new Color(0.35f, 0.24f, 0.15f), 0f, 0.4f);

        float halfX = map.Width * map.Cell * 0.5f + 6f;
        float halfZ = map.Height * map.Cell * 0.5f + 6f;

        TDVisuals.Box(parent, "RoomFloor", new Vector3(0f, -0.15f, 0f), new Vector3(halfX * 2f, 0.2f, halfZ * 2f), snow);

        float wh = 2.4f, wt = 0.8f;
        TDVisuals.Box(parent, "WallN", new Vector3(0f, wh * 0.5f, halfZ), new Vector3(halfX * 2f + wt, wh, wt), ice);
        TDVisuals.Box(parent, "WallS", new Vector3(0f, wh * 0.5f, -halfZ), new Vector3(halfX * 2f + wt, wh, wt), ice);
        TDVisuals.Box(parent, "WallE", new Vector3(halfX, wh * 0.5f, 0f), new Vector3(wt, wh, halfZ * 2f + wt), ice);
        TDVisuals.Box(parent, "WallW", new Vector3(-halfX, wh * 0.5f, 0f), new Vector3(wt, wh, halfZ * 2f + wt), ice);
        // cliff caps for silhouette
        TDVisuals.Box(parent, "CliffN0", new Vector3(-6f, wh + 0.5f, halfZ), new Vector3(5f, 1.2f, 1.4f), iceDark);
        TDVisuals.Box(parent, "CliffN1", new Vector3(5f, wh + 0.7f, halfZ), new Vector3(6f, 1.6f, 1.5f), snow);
        TDVisuals.Box(parent, "CliffS0", new Vector3(2f, wh + 0.5f, -halfZ), new Vector3(7f, 1.2f, 1.4f), iceDark);
        TDVisuals.Box(parent, "CliffE0", new Vector3(halfX, wh + 0.6f, -4f), new Vector3(1.5f, 1.4f, 6f), snow);
        TDVisuals.Box(parent, "CliffW0", new Vector3(-halfX, wh + 0.6f, 4f), new Vector3(1.5f, 1.4f, 6f), snow);

        float westX = -halfX + 3.0f;
        float eastX = halfX - 3.0f;

        // research crate (west)
        TDVisuals.Box(parent, "Crate", new Vector3(westX, 0.6f, 9f), new Vector3(2.2f, 1.2f, 1.6f), wood);
        TDVisuals.Box(parent, "CrateLid", new Vector3(westX, 1.3f, 9f), new Vector3(2.3f, 0.2f, 1.7f), woodDark);
        TDVisuals.Box(parent, "CrateStripe", new Vector3(westX, 0.6f, 9f), new Vector3(2.28f, 0.3f, 1.68f), metal);

        // sled (west-south)
        TDVisuals.Box(parent, "SledBase", new Vector3(westX, 0.35f, -9f), new Vector3(1.4f, 0.15f, 2.4f), wood);
        TDVisuals.Box(parent, "SledRunnerL", new Vector3(westX - 0.7f, 0.15f, -9f), new Vector3(0.12f, 0.2f, 2.6f), woodDark);
        TDVisuals.Box(parent, "SledRunnerR", new Vector3(westX + 0.7f, 0.15f, -9f), new Vector3(0.12f, 0.2f, 2.6f), woodDark);

        // antenna (east-north)
        TDVisuals.Cyl(parent, "AntennaPole", new Vector3(eastX, 1.5f, 11f), 0.12f, 3.0f, metal);
        TDVisuals.Box(parent, "AntennaBar0", new Vector3(eastX, 2.4f, 11f), new Vector3(1.4f, 0.1f, 0.1f), metal);
        TDVisuals.Box(parent, "AntennaBar1", new Vector3(eastX, 2.0f, 11f), new Vector3(1.0f, 0.1f, 0.1f), metal);

        // barrel (east)
        TDVisuals.Cyl(parent, "Barrel", new Vector3(eastX, 0.6f, 0f), 0.6f, 1.2f, wood);
        TDVisuals.Cyl(parent, "BarrelHoop0", new Vector3(eastX, 0.9f, 0f), 0.63f, 0.1f, metal);
        TDVisuals.Cyl(parent, "BarrelHoop1", new Vector3(eastX, 0.3f, 0f), 0.63f, 0.1f, metal);

        // pines (corners, outside grid)
        BuildPine(parent, new Vector3(westX - 0.5f, 0f, -11.5f), pine, trunk, snow);
        BuildPine(parent, new Vector3(eastX + 0.5f, 0f, -11f), pine, trunk, snow);
        BuildPine(parent, new Vector3(eastX + 0.3f, 0f, 11.8f), pine, trunk, snow);
    }

    static void BuildPine(Transform parent, Vector3 basePos, Material pine, Material trunk, Material snow)
    {
        TDVisuals.Cyl(parent, "PineTrunk", basePos + new Vector3(0f, 0.5f, 0f), 0.18f, 1.0f, trunk);
        TDVisuals.Sphere(parent, "PineLow", basePos + new Vector3(0f, 1.5f, 0f), 1.6f, pine);
        TDVisuals.Sphere(parent, "PineTop", basePos + new Vector3(0f, 2.3f, 0f), 1.1f, pine);
        TDVisuals.Sphere(parent, "PineSnow", basePos + new Vector3(0f, 2.7f, 0f), 0.6f, snow);
    }

    static void BuildVolcanic(Transform parent, TDMap map)
    {
        Material ash = TDVisuals.Mat(new Color(0.13f, 0.13f, 0.15f), 0f, 0.25f);
        Material basalt = TDVisuals.Mat(new Color(0.22f, 0.21f, 0.23f), 0f, 0.3f);
        Material obsidian = TDVisuals.Mat(new Color(0.09f, 0.09f, 0.11f), 0.25f, 0.6f);
        Material lava = TDVisuals.Mat(new Color(1.00f, 0.42f, 0.06f), 0f, 0.65f);
        Material lavaHot = TDVisuals.Mat(new Color(1.00f, 0.78f, 0.28f), 0f, 0.7f);
        Material rock = TDVisuals.Mat(new Color(0.19f, 0.18f, 0.20f), 0f, 0.3f);

        float halfX = map.Width * map.Cell * 0.5f + 6f;
        float halfZ = map.Height * map.Cell * 0.5f + 6f;

        // black volcanic ash plain
        TDVisuals.Box(parent, "RoomFloor", new Vector3(0f, -0.15f, 0f), new Vector3(halfX * 2f, 0.2f, halfZ * 2f), ash);

        // low crater rim walls
        float wh = 2.4f, wt = 0.8f;
        TDVisuals.Box(parent, "WallN", new Vector3(0f, wh * 0.5f, halfZ), new Vector3(halfX * 2f + wt, wh, wt), basalt);
        TDVisuals.Box(parent, "WallS", new Vector3(0f, wh * 0.5f, -halfZ), new Vector3(halfX * 2f + wt, wh, wt), basalt);
        TDVisuals.Box(parent, "WallE", new Vector3(halfX, wh * 0.5f, 0f), new Vector3(wt, wh, halfZ * 2f + wt), basalt);
        TDVisuals.Box(parent, "WallW", new Vector3(-halfX, wh * 0.5f, 0f), new Vector3(wt, wh, halfZ * 2f + wt), basalt);

        // obsidian spires on the rim
        BuildSpire(parent, new Vector3(-7f, wh, halfZ), 2.2f, obsidian);
        BuildSpire(parent, new Vector3(6f, wh, halfZ), 3.0f, obsidian);
        BuildSpire(parent, new Vector3(halfX, wh, -5f), 2.6f, obsidian);
        BuildSpire(parent, new Vector3(-halfX, wh, 4f), 2.0f, obsidian);
        BuildSpire(parent, new Vector3(2f, wh, -halfZ), 2.4f, obsidian);

        float westX = -halfX + 3.0f;
        float eastX = halfX - 3.0f;

        // lava pools (glowing, outside the grid)
        BuildPool(parent, new Vector3(westX, 0.04f, 9f), 2.6f, lava, lavaHot);
        BuildPool(parent, new Vector3(eastX, 0.04f, -9f), 2.2f, lava, lavaHot);

        // scattered cooled boulders, so the ash plain isn't a flat void
        TDVisuals.Sphere(parent, "Boulder0", new Vector3(eastX, 0.6f, 10.5f), 1.5f, rock);
        TDVisuals.Sphere(parent, "Boulder1", new Vector3(eastX + 1.0f, 0.4f, 9.0f), 1.0f, rock);
        TDVisuals.Sphere(parent, "Boulder2", new Vector3(westX - 0.8f, 0.5f, -10f), 1.2f, rock);
        TDVisuals.Sphere(parent, "Boulder3", new Vector3(westX - 1.4f, 0.35f, 2.5f), 0.9f, rock);
        TDVisuals.Sphere(parent, "Boulder4", new Vector3(eastX + 1.2f, 0.45f, 2.0f), 1.1f, rock);
        TDVisuals.Sphere(parent, "Boulder5", new Vector3(1.5f, 0.4f, halfZ - 2.2f), 1.0f, rock);
        TDVisuals.Sphere(parent, "Boulder6", new Vector3(-3.0f, 0.3f, -halfZ + 2.0f), 0.8f, rock);
        TDVisuals.Sphere(parent, "Pebble0", new Vector3(westX + 0.6f, 0.2f, -4f), 0.5f, rock);
        TDVisuals.Sphere(parent, "Pebble1", new Vector3(eastX - 0.8f, 0.2f, 6f), 0.45f, rock);

        // ember lamp post so the board still reads as "lit"
        TDVisuals.Cyl(parent, "EmberPole", new Vector3(westX - 0.6f, 1.1f, 11.5f), 0.09f, 2.2f, obsidian);
        TDVisuals.Sphere(parent, "EmberGlobe", new Vector3(westX - 0.6f, 2.35f, 11.5f), 0.7f, lavaHot);
    }

    static void BuildSpire(Transform parent, Vector3 basePos, float height, Material m)
    {
        TDVisuals.Box(parent, "SpireBase", basePos + new Vector3(0f, height * 0.25f, 0f), new Vector3(0.9f, height * 0.5f, 0.9f), m);
        TDVisuals.Box(parent, "SpireTop", basePos + new Vector3(0f, height * 0.70f, 0f), new Vector3(0.45f, height * 0.45f, 0.45f), m);
    }

    static void BuildPool(Transform parent, Vector3 pos, float diameter, Material lava, Material hot)
    {
        TDVisuals.Cyl(parent, "Pool", pos, diameter * 0.5f, 0.08f, lava);
        TDVisuals.Cyl(parent, "PoolCore", pos + new Vector3(0f, 0.05f, 0f), diameter * 0.26f, 0.08f, hot);
    }
}
