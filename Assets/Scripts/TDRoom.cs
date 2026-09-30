using UnityEngine;

// Builds environments around the grid map. Bedroom is the original kid's room;
// the other themes are visual reskins (same Layout/Route, only visuals change).
public static class TDRoom
{
    public static void Build(Transform parent, TDMap map, BoardTheme theme = BoardTheme.Bedroom)
    {
        if (theme == BoardTheme.ArcticOutpost) { BuildArctic(parent, map); return; }
        if (theme == BoardTheme.VolcanicCaldera) { BuildVolcanic(parent, map); return; }
        if (theme == BoardTheme.SpaceStation) { BuildStation(parent, map); return; }
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

    // -------------------------------------------------------- space station
    static void BuildStation(Transform parent, TDMap map)
    {
        Material floor = TDVisuals.Mat(new Color(0.20f, 0.23f, 0.28f), 0.1f, 0.4f);
        Material hull = TDVisuals.Mat(new Color(0.29f, 0.33f, 0.40f), 0.15f, 0.45f);
        Material hullDark = TDVisuals.Mat(new Color(0.21f, 0.24f, 0.30f), 0.15f, 0.45f);
        Material trim = TDVisuals.Mat(new Color(0.36f, 0.41f, 0.48f), 0.3f, 0.55f);
        Material glow = TDVisuals.Mat(new Color(0.40f, 0.95f, 1.00f), 0f, 0.6f);
        Material hazard = TDVisuals.Mat(new Color(0.74f, 0.58f, 0.13f), 0f, 0.45f);
        Material crate = TDVisuals.Mat(new Color(0.34f, 0.37f, 0.42f), 0f, 0.4f);

        float halfX = map.Width * map.Cell * 0.5f + 6f;
        float halfZ = map.Height * map.Cell * 0.5f + 6f;
        float gridX = map.Width * map.Cell * 0.5f;
        float gridZ = map.Height * map.Cell * 0.5f;

        // deck floor
        TDVisuals.Box(parent, "RoomFloor", new Vector3(0f, -0.15f, 0f), new Vector3(halfX * 2f, 0.2f, halfZ * 2f), floor);

        // hazard trim skirting the play area
        TDVisuals.Box(parent, "HazardN", new Vector3(0f, -0.06f, gridZ + 0.35f), new Vector3(gridX * 2f + 0.7f, 0.05f, 0.35f), hazard);
        TDVisuals.Box(parent, "HazardS", new Vector3(0f, -0.06f, -gridZ - 0.35f), new Vector3(gridX * 2f + 0.7f, 0.05f, 0.35f), hazard);
        TDVisuals.Box(parent, "HazardE", new Vector3(gridX + 0.35f, -0.06f, 0f), new Vector3(0.35f, 0.05f, gridZ * 2f + 0.7f), hazard);
        TDVisuals.Box(parent, "HazardW", new Vector3(-gridX - 0.35f, -0.06f, 0f), new Vector3(0.35f, 0.05f, gridZ * 2f + 0.7f), hazard);

        // hull walls
        float wh = 3.0f, wt = 0.9f;
        TDVisuals.Box(parent, "WallN", new Vector3(0f, wh * 0.5f, halfZ), new Vector3(halfX * 2f + wt, wh, wt), hull);
        TDVisuals.Box(parent, "WallS", new Vector3(0f, wh * 0.5f, -halfZ), new Vector3(halfX * 2f + wt, wh, wt), hull);
        TDVisuals.Box(parent, "WallE", new Vector3(halfX, wh * 0.5f, 0f), new Vector3(wt, wh, halfZ * 2f + wt), hull);
        TDVisuals.Box(parent, "WallW", new Vector3(-halfX, wh * 0.5f, 0f), new Vector3(wt, wh, halfZ * 2f + wt), hull);
        // darker wainscot band along the base of each wall
        TDVisuals.Box(parent, "SkirtN", new Vector3(0f, 0.35f, halfZ - wt * 0.5f), new Vector3(halfX * 2f, 0.7f, 0.15f), hullDark);
        TDVisuals.Box(parent, "SkirtS", new Vector3(0f, 0.35f, -halfZ + wt * 0.5f), new Vector3(halfX * 2f, 0.7f, 0.15f), hullDark);
        TDVisuals.Box(parent, "SkirtE", new Vector3(halfX - wt * 0.5f, 0.35f, 0f), new Vector3(0.15f, 0.7f, halfZ * 2f), hullDark);
        TDVisuals.Box(parent, "SkirtW", new Vector3(-halfX + wt * 0.5f, 0.35f, 0f), new Vector3(0.15f, 0.7f, halfZ * 2f), hullDark);

        // glowing portholes onto the starfield (a bright disc + a bezel)
        float[] px = { -9f, -4.5f, 0f, 4.5f, 9f };
        for (int i = 0; i < px.Length; i++)
            BuildPorthole(parent, new Vector3(px[i], 1.85f, halfZ - wt * 0.5f - 0.04f), true, glow, trim);
        float[] pz = { -6f, 0f, 6f };
        for (int i = 0; i < pz.Length; i++)
        {
            BuildPorthole(parent, new Vector3(halfX - wt * 0.5f - 0.04f, 1.85f, pz[i]), false, glow, trim);
            BuildPorthole(parent, new Vector3(-halfX + wt * 0.5f + 0.04f, 1.85f, pz[i]), false, glow, trim);
        }

        float westX = -halfX + 3.0f;
        float eastX = halfX - 3.0f;

        // cargo crates (west) with hazard accents
        BuildCrate(parent, new Vector3(westX, 0.6f, 9f), 2.0f, crate, hazard, trim);
        BuildCrate(parent, new Vector3(westX - 0.4f, 0.45f, 6.6f), 1.3f, crate, hazard, trim);

        // satellite dish (north-west floor)
        TDVisuals.Cyl(parent, "DishPole", new Vector3(westX + 0.4f, 0.9f, 12f), 0.12f, 1.8f, trim);
        GameObject dish = TDVisuals.Cyl(parent, "Dish", new Vector3(westX + 0.4f, 1.9f, 12f), 0.9f, 0.16f, trim);
        dish.transform.localRotation = Quaternion.Euler(60f, 0f, 0f);

        // coolant pipes running along the east wall
        for (int i = 0; i < 2; i++)
        {
            GameObject pipe = TDVisuals.Cyl(parent, "Pipe" + i,
                new Vector3(eastX + 1.4f, 0.7f + i * 0.5f, 0f), 0.16f, 12f, trim);
            pipe.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }

        // maintenance robot arm (south-east floor)
        TDVisuals.Cyl(parent, "ArmBase", new Vector3(eastX, 0.35f, -8f), 0.45f, 0.7f, hullDark);
        GameObject armLo = TDVisuals.Box(parent, "ArmLower", new Vector3(eastX, 1.0f, -8f), new Vector3(0.35f, 1.4f, 0.35f), trim);
        armLo.transform.localRotation = Quaternion.Euler(0f, 0f, 18f);
        GameObject armHi = TDVisuals.Box(parent, "ArmUpper", new Vector3(eastX + 0.6f, 1.9f, -8f), new Vector3(0.28f, 1.3f, 0.28f), trim);
        armHi.transform.localRotation = Quaternion.Euler(0f, 0f, -35f);
        TDVisuals.Box(parent, "ArmHead", new Vector3(eastX + 1.15f, 2.4f, -8f), new Vector3(0.4f, 0.35f, 0.4f), glow);

        // antenna mast (south-west floor)
        TDVisuals.Cyl(parent, "MastPole", new Vector3(westX - 0.4f, 1.3f, -11.5f), 0.1f, 2.6f, trim);
        TDVisuals.Sphere(parent, "MastTip", new Vector3(westX - 0.4f, 2.7f, -11.5f), 0.45f, glow);
    }

    /// <summary>A glowing porthole disc (plus a rim) let into a hull wall.</summary>
    static void BuildPorthole(Transform parent, Vector3 pos, bool facingZ, Material glow, Material trim)
    {
        GameObject glass = TDVisuals.Cyl(parent, "Porthole", pos, 0.55f, 0.10f, glow);
        GameObject rim = TDVisuals.Cyl(parent, "PortholeRim", pos, 0.70f, 0.06f, trim);
        // a cylinder's axis is its local Y, so rotate it to face out of the wall
        if (facingZ)
        {
            glass.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            rim.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }
        else
        {
            glass.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            rim.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }
        // push the rim slightly behind the glass so it reads as a bezel
        rim.transform.localPosition = pos + (facingZ ? new Vector3(0f, 0f, 0.06f) : new Vector3(0.06f, 0f, 0f));
    }

    static void BuildCrate(Transform parent, Vector3 pos, float size, Material crate, Material hazard, Material trim)
    {
        TDVisuals.Box(parent, "Crate", pos, new Vector3(size, size, size), crate);
        TDVisuals.Box(parent, "CrateBand", pos, new Vector3(size * 1.02f, size * 0.16f, size * 1.02f), hazard);
        TDVisuals.Box(parent, "CrateLid", pos + new Vector3(0f, size * 0.5f, 0f),
            new Vector3(size * 1.04f, size * 0.1f, size * 1.04f), trim);
    }
}
