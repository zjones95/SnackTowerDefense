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
        if (theme == BoardTheme.DesertHighway) { BuildDesert(parent, map); return; }
        if (theme == BoardTheme.CandyShop) { BuildCandy(parent, map); return; }
        if (theme == BoardTheme.SewerSubway) { BuildSewer(parent, map); return; }
        if (theme == BoardTheme.MedievalCastle) { BuildCastle(parent, map); return; }
        if (theme == BoardTheme.FactoryFloor) { BuildFactory(parent, map); return; }
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

    // ---------------------------------------------------------- desert theme
    static void BuildDesert(Transform parent, TDMap map)
    {
        Material sand = TDVisuals.Mat(new Color(0.86f, 0.71f, 0.46f), 0f, 0.35f);
        Material rock = TDVisuals.Mat(new Color(0.60f, 0.31f, 0.21f), 0f, 0.35f);
        Material rockTop = TDVisuals.Mat(new Color(0.72f, 0.41f, 0.28f), 0f, 0.35f);
        Material boulder = TDVisuals.Mat(new Color(0.66f, 0.43f, 0.31f), 0f, 0.35f);
        Material cactus = TDVisuals.Mat(new Color(0.26f, 0.50f, 0.27f), 0f, 0.45f);
        Material cactusDark = TDVisuals.Mat(new Color(0.19f, 0.38f, 0.21f), 0f, 0.45f);
        Material wood = TDVisuals.Mat(new Color(0.55f, 0.38f, 0.22f), 0f, 0.4f);
        Material metal = TDVisuals.Mat(new Color(0.58f, 0.56f, 0.52f), 0.5f, 0.4f);
        Material pumpBody = TDVisuals.Mat(new Color(0.74f, 0.72f, 0.67f), 0f, 0.4f);
        Material pumpRed = TDVisuals.Mat(new Color(0.72f, 0.22f, 0.16f), 0f, 0.45f);
        Material brush = TDVisuals.Mat(new Color(0.62f, 0.50f, 0.30f), 0f, 0.3f);

        float halfX = map.Width * map.Cell * 0.5f + 6f;
        float halfZ = map.Height * map.Cell * 0.5f + 6f;

        // desert sand floor
        TDVisuals.Box(parent, "RoomFloor", new Vector3(0f, -0.15f, 0f), new Vector3(halfX * 2f, 0.2f, halfZ * 2f), sand);

        // low mesa walls with a lighter sunlit cap
        float wh = 2.6f, wt = 1.0f;
        TDVisuals.Box(parent, "WallN", new Vector3(0f, wh * 0.5f, halfZ), new Vector3(halfX * 2f + wt, wh, wt), rock);
        TDVisuals.Box(parent, "WallS", new Vector3(0f, wh * 0.5f, -halfZ), new Vector3(halfX * 2f + wt, wh, wt), rock);
        TDVisuals.Box(parent, "WallE", new Vector3(halfX, wh * 0.5f, 0f), new Vector3(wt, wh, halfZ * 2f + wt), rock);
        TDVisuals.Box(parent, "WallW", new Vector3(-halfX, wh * 0.5f, 0f), new Vector3(wt, wh, halfZ * 2f + wt), rock);
        TDVisuals.Box(parent, "CapN", new Vector3(0f, wh + 0.25f, halfZ), new Vector3(halfX * 2f + wt + 0.3f, 0.5f, wt + 0.3f), rockTop);
        TDVisuals.Box(parent, "CapS", new Vector3(0f, wh + 0.25f, -halfZ), new Vector3(halfX * 2f + wt + 0.3f, 0.5f, wt + 0.3f), rockTop);
        TDVisuals.Box(parent, "CapE", new Vector3(halfX, wh + 0.25f, 0f), new Vector3(wt + 0.3f, 0.5f, halfZ * 2f + wt + 0.3f), rockTop);
        TDVisuals.Box(parent, "CapW", new Vector3(-halfX, wh + 0.25f, 0f), new Vector3(wt + 0.3f, 0.5f, halfZ * 2f + wt + 0.3f), rockTop);
        // mesa buttes breaking the rim line
        TDVisuals.Box(parent, "Butte0", new Vector3(-6f, wh + 0.6f, halfZ), new Vector3(5f, 1.2f, 1.6f), rockTop);
        TDVisuals.Box(parent, "Butte1", new Vector3(7f, wh + 0.9f, -halfZ), new Vector3(6f, 1.8f, 1.7f), rockTop);
        TDVisuals.Box(parent, "Butte2", new Vector3(halfX, wh + 0.7f, 6f), new Vector3(1.7f, 1.4f, 5f), rockTop);

        float westX = -halfX + 3.0f;
        float eastX = halfX - 3.0f;

        // cacti
        BuildCactus(parent, new Vector3(westX - 0.6f, 0f, -8f), cactus, cactusDark, sand);
        BuildCactus(parent, new Vector3(eastX + 0.4f, 0f, 8f), cactus, cactusDark, sand);
        BuildCactus(parent, new Vector3(eastX - 0.4f, 0f, -12f), cactus, cactusDark, sand);

        // derelict gas pumps (west)
        BuildGasPump(parent, new Vector3(westX, 0f, 10f), pumpBody, pumpRed, metal);
        BuildGasPump(parent, new Vector3(westX - 0.2f, 0f, 7.6f), pumpBody, pumpRed, metal);

        // wooden crates (west-south)
        BuildCrate(parent, new Vector3(westX + 0.2f, 0.7f, -3.5f), 1.6f, wood, TDVisuals.Mat(new Color(0.40f, 0.28f, 0.16f), 0f, 0.4f), wood);
        BuildCrate(parent, new Vector3(westX - 0.5f, 0.5f, -6.0f), 1.1f, wood, TDVisuals.Mat(new Color(0.40f, 0.28f, 0.16f), 0f, 0.4f), wood);

        // signpost (north-west)
        TDVisuals.Cyl(parent, "SignPost", new Vector3(westX + 0.4f, 1.4f, 12.5f), 0.1f, 2.8f, wood);
        TDVisuals.Box(parent, "SignBoard", new Vector3(westX + 0.4f, 2.4f, 12.5f), new Vector3(1.6f, 0.6f, 0.12f),
            TDVisuals.Mat(new Color(0.74f, 0.60f, 0.36f), 0f, 0.4f));

        // tumbleweeds and scattered rocks
        TDVisuals.Sphere(parent, "Tumbleweed0", new Vector3(eastX, 0.4f, -10f), 0.8f, brush);
        TDVisuals.Sphere(parent, "Tumbleweed1", new Vector3(4f, 0.35f, -13f), 0.7f, brush);
        TDVisuals.Sphere(parent, "Rock0", new Vector3(eastX + 1.2f, 0.3f, 2f), 0.9f, boulder);
        TDVisuals.Sphere(parent, "Rock1", new Vector3(-8f, 0.35f, -13f), 1.1f, boulder);
        TDVisuals.Sphere(parent, "Rock2", new Vector3(9f, 0.25f, 13f), 0.7f, boulder);
    }

    static void BuildCactus(Transform parent, Vector3 basePos, Material green, Material dark, Material sand)
    {
        TDVisuals.Cyl(parent, "CactusTrunk", basePos + new Vector3(0f, 1.2f, 0f), 0.30f, 2.4f, green);
        TDVisuals.Cyl(parent, "CactusBand", basePos + new Vector3(0f, 1.2f, 0f), 0.315f, 0.9f, dark);

        GameObject armL = TDVisuals.Cyl(parent, "CactusArmL", basePos + new Vector3(-0.58f, 1.45f, 0f), 0.19f, 0.75f, green);
        armL.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        TDVisuals.Cyl(parent, "CactusArmLup", basePos + new Vector3(-0.90f, 1.95f, 0f), 0.19f, 1.0f, green);

        GameObject armR = TDVisuals.Cyl(parent, "CactusArmR", basePos + new Vector3(0.58f, 1.15f, 0f), 0.19f, 0.75f, green);
        armR.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        TDVisuals.Cyl(parent, "CactusArmRup", basePos + new Vector3(0.90f, 1.65f, 0f), 0.19f, 1.0f, green);

        TDVisuals.Cyl(parent, "CactusSand", basePos + new Vector3(0f, 0.06f, 0f), 0.55f, 0.12f, sand);
    }

    static void BuildGasPump(Transform parent, Vector3 pos, Material body, Material red, Material metal)
    {
        TDVisuals.Box(parent, "PumpBase", pos + new Vector3(0f, 0.2f, 0f), new Vector3(1.2f, 0.4f, 0.9f), metal);
        TDVisuals.Box(parent, "PumpBody", pos + new Vector3(0f, 1.0f, 0f), new Vector3(1.1f, 1.2f, 0.8f), body);
        TDVisuals.Box(parent, "PumpTop", pos + new Vector3(0f, 1.75f, 0f), new Vector3(1.2f, 0.3f, 0.9f), red);
        TDVisuals.Cyl(parent, "PumpHose", pos + new Vector3(0.78f, 0.95f, 0f), 0.09f, 1.0f, metal);
    }

    // ----------------------------------------------------------- candy theme
    static void BuildCandy(Transform parent, TDMap map)
    {
        Material floor = TDVisuals.Mat(new Color(0.44f, 0.27f, 0.16f), 0f, 0.4f);      // wood shop floor
        Material wood = TDVisuals.Mat(new Color(0.60f, 0.40f, 0.24f), 0f, 0.4f);
        Material woodDark = TDVisuals.Mat(new Color(0.47f, 0.30f, 0.17f), 0f, 0.4f);
        Material cream = TDVisuals.Mat(new Color(0.93f, 0.88f, 0.80f), 0f, 0.45f);
        Material glass = TDVisuals.Mat(new Color(0.86f, 0.92f, 0.94f), 0.2f, 0.7f);
        Material caneRed = TDVisuals.Mat(new Color(0.90f, 0.20f, 0.22f), 0f, 0.5f);
        Material sugar = TDVisuals.Mat(new Color(0.97f, 0.95f, 0.92f), 0f, 0.5f);

        Color[] candyCols =
        {
            new Color(0.98f, 0.62f, 0.70f),
            new Color(0.62f, 0.78f, 0.98f),
            new Color(0.98f, 0.90f, 0.55f),
            new Color(0.62f, 0.92f, 0.70f)
        };
        Material[] candyMats = new Material[candyCols.Length];
        for (int i = 0; i < candyCols.Length; i++)
            candyMats[i] = TDVisuals.Mat(candyCols[i], 0f, 0.6f);

        // lollipop discs use saturated candy, not the pastel plot tints
        Material[] lolliMats =
        {
            TDVisuals.Mat(new Color(1.00f, 0.45f, 0.60f), 0f, 0.65f),
            TDVisuals.Mat(new Color(0.40f, 0.70f, 1.00f), 0f, 0.65f),
            TDVisuals.Mat(new Color(1.00f, 0.85f, 0.35f), 0f, 0.65f)
        };

        float halfX = map.Width * map.Cell * 0.5f + 6f;
        float halfZ = map.Height * map.Cell * 0.5f + 6f;

        // shop floor
        TDVisuals.Box(parent, "RoomFloor", new Vector3(0f, -0.15f, 0f), new Vector3(halfX * 2f, 0.2f, halfZ * 2f), floor);

        // shop walls (light wood panelling) + skirting
        float wh = 2.8f, wt = 0.8f;
        TDVisuals.Box(parent, "WallN", new Vector3(0f, wh * 0.5f, halfZ), new Vector3(halfX * 2f + wt, wh, wt), wood);
        TDVisuals.Box(parent, "WallS", new Vector3(0f, wh * 0.5f, -halfZ), new Vector3(halfX * 2f + wt, wh, wt), wood);
        TDVisuals.Box(parent, "WallE", new Vector3(halfX, wh * 0.5f, 0f), new Vector3(wt, wh, halfZ * 2f + wt), wood);
        TDVisuals.Box(parent, "WallW", new Vector3(-halfX, wh * 0.5f, 0f), new Vector3(wt, wh, halfZ * 2f + wt), wood);
        TDVisuals.Box(parent, "SkirtN", new Vector3(0f, 0.35f, halfZ - wt * 0.5f), new Vector3(halfX * 2f, 0.7f, 0.14f), woodDark);
        TDVisuals.Box(parent, "SkirtS", new Vector3(0f, 0.35f, -halfZ + wt * 0.5f), new Vector3(halfX * 2f, 0.7f, 0.14f), woodDark);
        TDVisuals.Box(parent, "SkirtE", new Vector3(halfX - wt * 0.5f, 0.35f, 0f), new Vector3(0.14f, 0.7f, halfZ * 2f), woodDark);
        TDVisuals.Box(parent, "SkirtW", new Vector3(-halfX + wt * 0.5f, 0.35f, 0f), new Vector3(0.14f, 0.7f, halfZ * 2f), woodDark);

        float westX = -halfX + 3.0f;
        float eastX = halfX - 3.0f;

        // sweet shelves along the north wall, lined with candy jars
        TDVisuals.Box(parent, "ShelfN0", new Vector3(0f, 1.5f, halfZ - 1.4f), new Vector3(16f, 0.18f, 1.6f), woodDark);
        TDVisuals.Box(parent, "ShelfN1", new Vector3(0f, 2.3f, halfZ - 1.4f), new Vector3(16f, 0.18f, 1.6f), woodDark);
        for (int i = 0; i < 7; i++)
        {
            float jx = -7.2f + i * 2.4f;
            BuildJar(parent, new Vector3(jx, 1.72f, halfZ - 1.4f), 0.34f, candyMats[i % candyMats.Length], glass, cream);
            BuildJar(parent, new Vector3(jx, 2.52f, halfZ - 1.4f), 0.32f, candyMats[(i + 2) % candyMats.Length], glass, cream);
        }

        // lollipops on poles (west and east floor)
        BuildLollipop(parent, new Vector3(westX - 0.4f, 0f, 10.5f), 1.5f, lolliMats[0], cream, sugar);
        BuildLollipop(parent, new Vector3(westX + 0.6f, 0f, 7.0f), 1.2f, lolliMats[1], cream, sugar);
        BuildLollipop(parent, new Vector3(eastX, 0f, 11f), 1.4f, lolliMats[2], cream, sugar);

        // candy cane
        TDVisuals.Cyl(parent, "CanePole", new Vector3(westX - 1.0f, 1.2f, -6f), 0.22f, 2.4f, sugar);
        for (int i = 0; i < 5; i++)
            TDVisuals.Cyl(parent, "CaneStripe" + i, new Vector3(westX - 1.0f, 0.3f + i * 0.5f, -6f), 0.235f, 0.18f, caneRed);

        // gumdrop boulders (east)
        for (int i = 0; i < 3; i++)
            TDVisuals.Sphere(parent, "Gumdrop" + i, new Vector3(eastX + 0.6f, 0.45f + i * 0.05f, -8f - i * 1.5f),
                1.1f - i * 0.15f, TDVisuals.Mat(candyCols[i % candyCols.Length], 0f, 0.6f));

        // cookie crate (south-east) and wrapped bonbons
        TDVisuals.Box(parent, "CookieCrate", new Vector3(eastX, 0.6f, -1.5f), new Vector3(2.0f, 1.2f, 1.6f), wood);
        TDVisuals.Box(parent, "CookieCrateBand", new Vector3(eastX, 0.6f, -1.5f), new Vector3(2.06f, 0.24f, 1.66f), woodDark);
        for (int i = 0; i < 4; i++)
            TDVisuals.Sphere(parent, "Cookie" + i, new Vector3(eastX - 0.5f + i * 0.33f, 1.35f, -1.5f), 0.3f,
                TDVisuals.Mat(new Color(0.68f, 0.46f, 0.26f), 0f, 0.5f));

        TDVisuals.Sphere(parent, "Bonbon0", new Vector3(-4f, 0.35f, -13f), 0.7f, TDVisuals.Mat(candyCols[1], 0f, 0.6f));
        TDVisuals.Sphere(parent, "Bonbon1", new Vector3(5f, 0.3f, 13.5f), 0.6f, TDVisuals.Mat(candyCols[3], 0f, 0.6f));
    }

    static void BuildJar(Transform parent, Vector3 pos, float radius, Material candyFill, Material glass, Material lid)
    {
        // The candy is the dominant volume; the glass is only a bright rim, so a
        // jar still reads as "full of candy" at board distance.
        TDVisuals.Cyl(parent, "JarCandy", pos, radius * 0.88f, radius * 1.45f, candyFill);
        TDVisuals.Cyl(parent, "JarRim", pos + new Vector3(0f, radius * 0.72f, 0f), radius, radius * 0.12f, glass);
        TDVisuals.Cyl(parent, "JarLid", pos + new Vector3(0f, radius * 0.92f, 0f), radius * 0.95f, radius * 0.26f, lid);
    }

    static void BuildLollipop(Transform parent, Vector3 basePos, float discR, Material swirl, Material stick, Material sugar)
    {
        TDVisuals.Cyl(parent, "LolliStick", basePos + new Vector3(0f, discR * 0.8f, 0f), 0.08f, discR * 1.6f, stick);

        // disc faces −Z, the side the board camera looks from
        GameObject disc = TDVisuals.Cyl(parent, "LolliDisc", basePos + new Vector3(0f, discR * 1.6f, 0f), discR, 0.18f, swirl);
        disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        GameObject swirlSpot = TDVisuals.Cyl(parent, "LolliSwirl", basePos + new Vector3(0f, discR * 1.6f, -0.11f), discR * 0.45f, 0.20f, sugar);
        swirlSpot.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
    }

    // ---------------------------------------------------------- sewer theme
    static void BuildSewer(Transform parent, TDMap map)
    {
        Material wet = TDVisuals.Mat(new Color(0.28f, 0.28f, 0.28f), 0.05f, 0.55f);
        Material tileWall = TDVisuals.Mat(new Color(0.40f, 0.42f, 0.40f), 0f, 0.4f);
        Material tileBand = TDVisuals.Mat(new Color(0.52f, 0.53f, 0.50f), 0f, 0.45f);
        Material pipe = TDVisuals.Mat(new Color(0.42f, 0.31f, 0.22f), 0.35f, 0.4f);
        Material valve = TDVisuals.Mat(new Color(0.55f, 0.30f, 0.18f), 0.4f, 0.45f);
        Material grate = TDVisuals.Mat(new Color(0.26f, 0.26f, 0.27f), 0.5f, 0.4f);
        Material water = TDVisuals.Mat(new Color(0.16f, 0.26f, 0.30f), 0.2f, 0.7f);
        Material wood = TDVisuals.Mat(new Color(0.48f, 0.36f, 0.24f), 0f, 0.4f);
        Material lamp = TDVisuals.Mat(new Color(0.98f, 0.80f, 0.42f), 0f, 0.6f);

        float halfX = map.Width * map.Cell * 0.5f + 6f;
        float halfZ = map.Height * map.Cell * 0.5f + 6f;

        // flooded concrete floor
        TDVisuals.Box(parent, "RoomFloor", new Vector3(0f, -0.15f, 0f), new Vector3(halfX * 2f, 0.2f, halfZ * 2f), wet);

        // tiled tunnel walls with a lighter band
        float wh = 3.2f, wt = 1.0f;
        TDVisuals.Box(parent, "WallN", new Vector3(0f, wh * 0.5f, halfZ), new Vector3(halfX * 2f + wt, wh, wt), tileWall);
        TDVisuals.Box(parent, "WallS", new Vector3(0f, wh * 0.5f, -halfZ), new Vector3(halfX * 2f + wt, wh, wt), tileWall);
        TDVisuals.Box(parent, "WallE", new Vector3(halfX, wh * 0.5f, 0f), new Vector3(wt, wh, halfZ * 2f + wt), tileWall);
        TDVisuals.Box(parent, "WallW", new Vector3(-halfX, wh * 0.5f, 0f), new Vector3(wt, wh, halfZ * 2f + wt), tileWall);
        TDVisuals.Box(parent, "BandN", new Vector3(0f, 2.0f, halfZ - wt * 0.5f), new Vector3(halfX * 2f, 0.5f, 0.12f), tileBand);
        TDVisuals.Box(parent, "BandS", new Vector3(0f, 2.0f, -halfZ + wt * 0.5f), new Vector3(halfX * 2f, 0.5f, 0.12f), tileBand);
        TDVisuals.Box(parent, "BandE", new Vector3(halfX - wt * 0.5f, 2.0f, 0f), new Vector3(0.12f, 0.5f, halfZ * 2f), tileBand);
        TDVisuals.Box(parent, "BandW", new Vector3(-halfX + wt * 0.5f, 2.0f, 0f), new Vector3(0.12f, 0.5f, halfZ * 2f), tileBand);

        float westX = -halfX + 3.0f;
        float eastX = halfX - 3.0f;

        // fat pipes along the east wall, with valve wheels
        for (int i = 0; i < 2; i++)
        {
            GameObject p = TDVisuals.Cyl(parent, "Pipe" + i, new Vector3(eastX + 1.4f, 0.8f + i * 0.8f, 0f), 0.26f, 14f, pipe);
            p.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }
        for (int i = 0; i < 3; i++)
        {
            GameObject v = TDVisuals.Cyl(parent, "Valve" + i, new Vector3(eastX + 1.4f, 1.6f, -5f + i * 5f), 0.42f, 0.12f, valve);
            v.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }

        // floor grate (south-west)
        TDVisuals.Box(parent, "GrateFrame", new Vector3(westX, 0.02f, -9f), new Vector3(3.2f, 0.14f, 2.4f), grate);
        for (int i = 0; i < 7; i++)
            TDVisuals.Box(parent, "GrateSlat" + i, new Vector3(westX - 1.4f + i * 0.47f, 0.12f, -9f),
                new Vector3(0.16f, 0.14f, 2.3f), water);

        // drainage channel running beside the play area
        TDVisuals.Box(parent, "Channel", new Vector3(0f, -0.07f, -halfZ + 2.2f), new Vector3(halfX * 1.4f, 0.06f, 1.2f), water);

        // ladder up the north wall
        TDVisuals.Box(parent, "LadderRailL", new Vector3(11f, 1.6f, halfZ - wt * 0.5f - 0.2f), new Vector3(0.12f, 3.2f, 0.12f), grate);
        TDVisuals.Box(parent, "LadderRailR", new Vector3(11.6f, 1.6f, halfZ - wt * 0.5f - 0.2f), new Vector3(0.12f, 3.2f, 0.12f), grate);
        for (int i = 0; i < 6; i++)
            TDVisuals.Box(parent, "LadderRung" + i, new Vector3(11.3f, 0.4f + i * 0.55f, halfZ - wt * 0.5f - 0.2f),
                new Vector3(0.75f, 0.09f, 0.09f), grate);

        // crates and a warm lamp
        TDVisuals.Box(parent, "Crate0", new Vector3(westX - 0.5f, 0.6f, 8f), new Vector3(1.6f, 1.2f, 1.6f), wood);
        TDVisuals.Box(parent, "Crate1", new Vector3(westX + 0.4f, 0.45f, 10.2f), new Vector3(1.2f, 0.9f, 1.2f), wood);
        TDVisuals.Cyl(parent, "LampPole", new Vector3(westX - 0.6f, 1.5f, -13f), 0.09f, 3.0f, grate);
        TDVisuals.Sphere(parent, "LampGlobe", new Vector3(westX - 0.6f, 3.05f, -13f), 0.7f, lamp);
    }

    // --------------------------------------------------------- castle theme
    static void BuildCastle(Transform parent, TDMap map)
    {
        Material earth = TDVisuals.Mat(new Color(0.56f, 0.46f, 0.32f), 0f, 0.3f);
        Material stone = TDVisuals.Mat(new Color(0.62f, 0.61f, 0.58f), 0f, 0.35f);
        Material stoneDark = TDVisuals.Mat(new Color(0.50f, 0.49f, 0.47f), 0f, 0.35f);
        Material wood = TDVisuals.Mat(new Color(0.52f, 0.37f, 0.22f), 0f, 0.4f);
        Material iron = TDVisuals.Mat(new Color(0.40f, 0.40f, 0.42f), 0.6f, 0.4f);
        Material flame = TDVisuals.Mat(new Color(1.00f, 0.62f, 0.18f), 0f, 0.7f);
        Material straw = TDVisuals.Mat(new Color(0.88f, 0.76f, 0.38f), 0f, 0.4f);
        Material red = TDVisuals.Mat(new Color(0.72f, 0.20f, 0.20f), 0f, 0.45f);
        Material blue = TDVisuals.Mat(new Color(0.22f, 0.36f, 0.68f), 0f, 0.45f);

        float halfX = map.Width * map.Cell * 0.5f + 6f;
        float halfZ = map.Height * map.Cell * 0.5f + 6f;

        // packed-earth courtyard
        TDVisuals.Box(parent, "RoomFloor", new Vector3(0f, -0.15f, 0f), new Vector3(halfX * 2f, 0.2f, halfZ * 2f), earth);

        // stone curtain walls with crenellations
        float wh = 3.0f, wt = 1.0f;
        TDVisuals.Box(parent, "WallN", new Vector3(0f, wh * 0.5f, halfZ), new Vector3(halfX * 2f + wt, wh, wt), stone);
        TDVisuals.Box(parent, "WallS", new Vector3(0f, wh * 0.5f, -halfZ), new Vector3(halfX * 2f + wt, wh, wt), stone);
        TDVisuals.Box(parent, "WallE", new Vector3(halfX, wh * 0.5f, 0f), new Vector3(wt, wh, halfZ * 2f + wt), stone);
        TDVisuals.Box(parent, "WallW", new Vector3(-halfX, wh * 0.5f, 0f), new Vector3(wt, wh, halfZ * 2f + wt), stone);
        TDVisuals.Box(parent, "CourseN", new Vector3(0f, 1.5f, halfZ - wt * 0.5f), new Vector3(halfX * 2f, 0.16f, 0.1f), stoneDark);
        TDVisuals.Box(parent, "CourseS", new Vector3(0f, 1.5f, -halfZ + wt * 0.5f), new Vector3(halfX * 2f, 0.16f, 0.1f), stoneDark);
        TDVisuals.Box(parent, "CourseE", new Vector3(halfX - wt * 0.5f, 1.5f, 0f), new Vector3(0.1f, 0.16f, halfZ * 2f), stoneDark);
        TDVisuals.Box(parent, "CourseW", new Vector3(-halfX + wt * 0.5f, 1.5f, 0f), new Vector3(0.1f, 0.16f, halfZ * 2f), stoneDark);

        for (int i = 0; i < 11; i++)
        {
            float cx = -13f + i * 2.6f;
            TDVisuals.Box(parent, "CrenN" + i, new Vector3(cx, wh + 0.4f, halfZ), new Vector3(1.4f, 0.8f, 1.1f), stone);
            TDVisuals.Box(parent, "CrenS" + i, new Vector3(cx, wh + 0.4f, -halfZ), new Vector3(1.4f, 0.8f, 1.1f), stone);
        }
        for (int i = 0; i < 8; i++)
        {
            float cz = -13f + i * 3.4f;
            TDVisuals.Box(parent, "CrenE" + i, new Vector3(halfX, wh + 0.4f, cz), new Vector3(1.1f, 0.8f, 1.4f), stone);
            TDVisuals.Box(parent, "CrenW" + i, new Vector3(-halfX, wh + 0.4f, cz), new Vector3(1.1f, 0.8f, 1.4f), stone);
        }

        float westX = -halfX + 3.0f;
        float eastX = halfX - 3.0f;

        // torches along the walls
        for (int i = 0; i < 4; i++)
        {
            float tx = -9f + i * 6f;
            BuildTorch(parent, new Vector3(tx, 0f, halfZ - wt * 0.5f - 0.24f), wood, iron, flame);
        }
        BuildTorch(parent, new Vector3(westX + 1.2f, 0f, 2f), wood, iron, flame);
        BuildTorch(parent, new Vector3(eastX - 1.2f, 0f, -4f), wood, iron, flame);

        // barrels
        for (int i = 0; i < 3; i++)
            BuildBarrel(parent, new Vector3(westX - 0.6f + i * 0.2f, 0f, 9.5f - i * 2.2f), wood, iron);

        // banner poles
        BuildWallBanner(parent, new Vector3(westX, 0f, -11f), red, wood);
        BuildWallBanner(parent, new Vector3(eastX, 0f, 11f), blue, wood);

        // siege catapult (east floor)
        TDVisuals.Box(parent, "CatFrame", new Vector3(eastX, 0.5f, -10f), new Vector3(2.4f, 0.5f, 1.6f), wood);
        GameObject arm = TDVisuals.Box(parent, "CatArm", new Vector3(eastX, 1.4f, -10f), new Vector3(0.3f, 2.6f, 0.3f), wood);
        arm.transform.localRotation = Quaternion.Euler(0f, 0f, 35f);
        GameObject wheelL = TDVisuals.Cyl(parent, "CatWheelL", new Vector3(eastX - 0.9f, 0.55f, -9.2f), 0.55f, 0.24f, wood);
        wheelL.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        GameObject wheelR = TDVisuals.Cyl(parent, "CatWheelR", new Vector3(eastX + 0.9f, 0.55f, -9.2f), 0.55f, 0.24f, wood);
        wheelR.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        // straw bales
        GameObject bale0 = TDVisuals.Cyl(parent, "Straw0", new Vector3(westX + 1.2f, 0.5f, 13f), 0.9f, 0.9f, straw);
        bale0.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        GameObject bale1 = TDVisuals.Cyl(parent, "Straw1", new Vector3(eastX - 1.0f, 0.45f, 4f), 0.8f, 0.8f, straw);
        bale1.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
    }

    static void BuildTorch(Transform parent, Vector3 basePos, Material wood, Material iron, Material flame)
    {
        TDVisuals.Cyl(parent, "TorchPole", basePos + new Vector3(0f, 1.1f, 0f), 0.09f, 2.2f, wood);
        TDVisuals.Cyl(parent, "TorchCup", basePos + new Vector3(0f, 2.25f, 0f), 0.24f, 0.3f, iron);
        TDVisuals.Sphere(parent, "TorchFlame", basePos + new Vector3(0f, 2.6f, 0f), 0.6f, flame);
    }

    static void BuildBarrel(Transform parent, Vector3 basePos, Material wood, Material iron)
    {
        TDVisuals.Cyl(parent, "Barrel", basePos + new Vector3(0f, 0.65f, 0f), 0.5f, 1.3f, wood);
        TDVisuals.Cyl(parent, "BarrelHoop0", basePos + new Vector3(0f, 0.95f, 0f), 0.53f, 0.1f, iron);
        TDVisuals.Cyl(parent, "BarrelHoop1", basePos + new Vector3(0f, 0.35f, 0f), 0.53f, 0.1f, iron);
    }

    static void BuildWallBanner(Transform parent, Vector3 basePos, Material cloth, Material pole)
    {
        TDVisuals.Cyl(parent, "BannerPole", basePos + new Vector3(0f, 1.6f, 0f), 0.1f, 3.2f, pole);
        TDVisuals.Box(parent, "BannerCloth", basePos + new Vector3(0.6f, 2.3f, 0f), new Vector3(1.1f, 1.6f, 0.1f), cloth);
    }

    // -------------------------------------------------------- factory theme
    static void BuildFactory(Transform parent, TDMap map)
    {
        Material deck = TDVisuals.Mat(new Color(0.42f, 0.44f, 0.47f), 0.35f, 0.5f);
        Material wall = TDVisuals.Mat(new Color(0.56f, 0.60f, 0.64f), 0.2f, 0.45f);
        Material wallDark = TDVisuals.Mat(new Color(0.42f, 0.46f, 0.50f), 0.2f, 0.45f);
        Material hazard = TDVisuals.Mat(new Color(0.92f, 0.78f, 0.20f), 0f, 0.45f);
        Material steel = TDVisuals.Mat(new Color(0.68f, 0.70f, 0.72f), 0.6f, 0.5f);
        Material wood = TDVisuals.Mat(new Color(0.58f, 0.42f, 0.26f), 0f, 0.4f);
        Material green = TDVisuals.Mat(new Color(0.32f, 0.78f, 0.40f), 0f, 0.5f);
        Material panel = TDVisuals.Mat(new Color(0.28f, 0.30f, 0.34f), 0.3f, 0.5f);

        float halfX = map.Width * map.Cell * 0.5f + 6f;
        float halfZ = map.Height * map.Cell * 0.5f + 6f;

        // checker-plate floor
        TDVisuals.Box(parent, "RoomFloor", new Vector3(0f, -0.15f, 0f), new Vector3(halfX * 2f, 0.2f, halfZ * 2f), deck);

        // corrugated walls with hazard trim
        float wh = 3.2f, wt = 0.9f;
        TDVisuals.Box(parent, "WallN", new Vector3(0f, wh * 0.5f, halfZ), new Vector3(halfX * 2f + wt, wh, wt), wall);
        TDVisuals.Box(parent, "WallS", new Vector3(0f, wh * 0.5f, -halfZ), new Vector3(halfX * 2f + wt, wh, wt), wall);
        TDVisuals.Box(parent, "WallE", new Vector3(halfX, wh * 0.5f, 0f), new Vector3(wt, wh, halfZ * 2f + wt), wall);
        TDVisuals.Box(parent, "WallW", new Vector3(-halfX, wh * 0.5f, 0f), new Vector3(wt, wh, halfZ * 2f + wt), wall);
        TDVisuals.Box(parent, "TrimN", new Vector3(0f, 2.4f, halfZ - wt * 0.5f), new Vector3(halfX * 2f, 0.34f, 0.12f), hazard);
        TDVisuals.Box(parent, "TrimS", new Vector3(0f, 2.4f, -halfZ + wt * 0.5f), new Vector3(halfX * 2f, 0.34f, 0.12f), hazard);
        TDVisuals.Box(parent, "TrimE", new Vector3(halfX - wt * 0.5f, 2.4f, 0f), new Vector3(0.12f, 0.34f, halfZ * 2f), hazard);
        TDVisuals.Box(parent, "TrimW", new Vector3(-halfX + wt * 0.5f, 2.4f, 0f), new Vector3(0.12f, 0.34f, halfZ * 2f), hazard);
        // vertical ribs so the walls read as corrugated
        for (int i = 0; i < 14; i++)
        {
            float rx = -16f + i * 2.5f;
            TDVisuals.Box(parent, "RibN" + i, new Vector3(rx, wh * 0.5f, halfZ - wt * 0.5f), new Vector3(0.3f, wh, 0.1f), wallDark);
        }

        float westX = -halfX + 3.0f;
        float eastX = halfX - 3.0f;

        // control consoles
        BuildConsole(parent, new Vector3(eastX, 0f, -4f), panel, steel, green);
        BuildConsole(parent, new Vector3(eastX, 0f, 6.5f), panel, steel, green);

        // crate stacks
        TDVisuals.Box(parent, "Crate0", new Vector3(westX, 0.7f, 10f), new Vector3(1.8f, 1.4f, 1.8f), wood);
        TDVisuals.Box(parent, "Crate1", new Vector3(westX - 0.3f, 0.55f, 7.8f), new Vector3(1.4f, 1.1f, 1.4f), wood);
        TDVisuals.Box(parent, "Crate2", new Vector3(westX + 0.2f, 1.9f, 10f), new Vector3(1.4f, 1.1f, 1.4f), wood);

        // overhead chain hoist
        TDVisuals.Box(parent, "Chain", new Vector3(eastX + 1.5f, 2.0f, 10.5f), new Vector3(0.12f, 2.0f, 0.12f), steel);
        TDVisuals.Box(parent, "Hook", new Vector3(eastX + 1.5f, 0.85f, 10.5f), new Vector3(0.5f, 0.5f, 0.3f), steel);

        // conduit runs along the east wall
        GameObject conduit = TDVisuals.Cyl(parent, "Conduit", new Vector3(eastX + 1.6f, 2.0f, 0f), 0.18f, 16f, steel);
        conduit.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
    }

    static void BuildConsole(Transform parent, Vector3 basePos, Material panel, Material steel, Material green)
    {
        TDVisuals.Box(parent, "ConsoleBody", basePos + new Vector3(0f, 0.7f, 0f), new Vector3(2.0f, 1.4f, 1.2f), panel);
        GameObject face = TDVisuals.Box(parent, "ConsoleFace", basePos + new Vector3(0f, 1.25f, -0.35f), new Vector3(1.8f, 0.7f, 0.3f), steel);
        face.transform.localRotation = Quaternion.Euler(-30f, 0f, 0f);
        TDVisuals.Sphere(parent, "ConsoleLight", basePos + new Vector3(-0.5f, 1.5f, -0.5f), 0.22f, green);
        TDVisuals.Box(parent, "ConsoleBase", basePos + new Vector3(0f, 0.1f, 0f), new Vector3(2.2f, 0.2f, 1.4f), steel);
    }
}
