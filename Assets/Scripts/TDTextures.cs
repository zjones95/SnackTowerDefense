using UnityEngine;

// Procedurally generated tile textures (no image assets).
public static class TDTextures
{
    private static Texture2D sidewalk, road, roadCross;
    private static Texture2D tropicalSand;

    public static Texture2D TropicalSand()
    {
        if (tropicalSand != null) return tropicalSand;
        const int size = 64;
        Texture2D t = New(size);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float n = (N(x, y) - 0.5f) * 0.13f;
                bool pebble = N(x / 3 + 17, y / 3 + 23) > 0.97f && x % 3 == 1 && y % 3 == 1;
                t.SetPixel(x, y, pebble ? new Color(0.69f, 0.53f, 0.34f) :
                    new Color(0.85f + n, 0.72f + n, 0.48f + n));
            }
        t.Apply();
        tropicalSand = t;
        return t;
    }

    static Texture2D New(int size)
    {
        Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Repeat;
        t.filterMode = FilterMode.Bilinear;
        return t;
    }

    // deterministic value noise
    static float N(int x, int y)
    {
        float n = Mathf.Sin(x * 12.9898f + y * 78.233f) * 43758.5453f;
        return n - Mathf.Floor(n);
    }

    public static Texture2D Sidewalk()
    {
        if (sidewalk != null) return sidewalk;
        int S = 128;
        Texture2D t = New(S);
        Color baseC = new Color(0.74f, 0.74f, 0.75f);
        Color grout = new Color(0.50f, 0.50f, 0.52f);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float n = (N(x, y) - 0.5f) * 0.06f;
                Color c = baseC + new Color(n, n, n);
                int border = 3;
                if (x < border || y < border || x >= S - border || y >= S - border) c = grout;
                // paving slab divisions (2x2 per tile)
                if (Mathf.Abs(x - S / 2) < 2 || Mathf.Abs(y - S / 2) < 2) c = Color.Lerp(c, grout, 0.75f);
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        sidewalk = t;
        return t;
    }

    public static Texture2D Road()
    {
        if (road != null) return road;
        int S = 128;
        Texture2D t = New(S);
        Color ballast = new Color(0.52f, 0.38f, 0.24f);
        Color tie = new Color(0.38f, 0.27f, 0.16f);
        Color rail = new Color(0.82f, 0.84f, 0.88f);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float n = (N(x, y) - 0.5f) * 0.08f;
                Color c = ballast + new Color(n, n, n);
                // wooden sleepers across the track
                if ((x % 16) < 6 && y > 24 && y < 104) c = tie + new Color(n, n, n);
                // two metal rails running along the track
                if ((y >= 42 && y <= 46) || (y >= 82 && y <= 86)) c = rail;
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        road = t;
        return t;
    }

    static Texture2D weave;

    // soft woven carpet (greyscale; tint per material)
    public static Texture2D Weave()
    {
        if (weave != null) return weave;
        int S = 64;
        Texture2D t = New(S);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float n = (N(x, y) - 0.5f) * 0.05f;
                float w = (((x / 2) + (y / 2)) % 2 == 0) ? 0.045f : -0.045f;
                float v = 0.93f + n + w;
                t.SetPixel(x, y, new Color(v, v, v));
            }
        }
        t.Apply();
        weave = t;
        return t;
    }

    public static Texture2D RoadCross()
    {
        if (roadCross != null) return roadCross;
        int S = 128;
        Texture2D t = New(S);
        Color ballast = new Color(0.52f, 0.38f, 0.24f);
        Color tie = new Color(0.38f, 0.27f, 0.16f);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float n = (N(x, y) - 0.5f) * 0.08f;
                Color c = ballast + new Color(n, n, n);
                if ((x % 16) < 6 || (y % 16) < 6) c = tie + new Color(n, n, n);
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        roadCross = t;
        return t;
    }

    // ------------------------------------------------------ arctic theme
    static Texture2D frost, snowTrack, snowCross;

    /// <summary>Frosted ice tile: pale blue centre, white snow-dusted border,
    /// faint snowflake etched in the middle. Tint per material.</summary>
    public static Texture2D Frost()
    {
        if (frost != null) return frost;
        int S = 128;
        Texture2D t = New(S);
        Color ice = new Color(0.84f, 0.91f, 0.94f);
        Color snow = new Color(0.96f, 0.97f, 0.99f);
        Color etch = new Color(0.98f, 0.99f, 1.0f);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float n = (N(x, y) - 0.5f) * 0.05f;
                Color c = ice + new Color(n, n, n);
                int edge = 10;
                if (x < edge || y < edge || x >= S - edge || y >= S - edge) c = snow + new Color(n, n, n);
                float cx = x - S * 0.5f, cy = y - S * 0.5f;
                float d = Mathf.Sqrt(cx * cx + cy * cy);
                // 6-arm snowflake etch around the centre
                float arm = 999f;
                for (int i = 0; i < 3; i++)
                {
                    float a = i * Mathf.PI / 3f;
                    float dx = Mathf.Abs(cx * Mathf.Sin(a) - cy * Mathf.Cos(a));
                    float along = Mathf.Abs(cx * Mathf.Cos(a) + cy * Mathf.Sin(a));
                    if (along < 28f) arm = Mathf.Min(arm, dx);
                }
                if (arm < 1.6f && d < 30f && d > 4f) c = Color.Lerp(c, etch, 0.8f);
                if (d < 4f) c = etch;
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        frost = t;
        return t;
    }

    /// <summary>Packed snowmobile track: snow white with twin ski grooves.</summary>
    public static Texture2D SnowTrack()
    {
        if (snowTrack != null) return snowTrack;
        int S = 128;
        Texture2D t = New(S);
        Color snow = new Color(0.93f, 0.95f, 0.98f);
        Color groove = new Color(0.76f, 0.83f, 0.90f);
        Color drift = new Color(0.98f, 0.99f, 1.0f);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float n = (N(x, y) - 0.5f) * 0.06f;
                Color c = snow + new Color(n, n, n);
                if (x < 8 || x >= S - 8) c = drift + new Color(n, n, n);
                if ((y >= 42 && y <= 47) || (y >= 81 && y <= 86)) c = groove + new Color(n, n, n);
                if (y == 42 || y == 47 || y == 81 || y == 86) c = drift;
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        snowTrack = t;
        return t;
    }

    public static Texture2D SnowCross()
    {
        if (snowCross != null) return snowCross;
        int S = 128;
        Texture2D t = New(S);
        Color snow = new Color(0.93f, 0.95f, 0.98f);
        Color groove = new Color(0.76f, 0.83f, 0.90f);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float n = (N(x, y) - 0.5f) * 0.06f;
                Color c = snow + new Color(n, n, n);
                bool gx = (y >= 42 && y <= 47) || (y >= 81 && y <= 86);
                bool gz = (x >= 42 && x <= 47) || (x >= 81 && x <= 86);
                if (gx || gz) c = groove + new Color(n, n, n);
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        snowCross = t;
        return t;
    }

    // ----------------------------------------------------- volcanic theme
    static Texture2D basalt, lavaFlow, lavaCross;

    /// <summary>Cooled basalt slab for tower plots: near-black rock with a
    /// faint ember-crack border. Deliberately much darker than the path, so
    /// buildable plots read as shadowed ground rather than walkable lava.</summary>
    public static Texture2D Basalt()
    {
        if (basalt != null) return basalt;
        int S = 128;
        Texture2D t = New(S);
        Color rock = new Color(0.34f, 0.33f, 0.35f);
        Color dark = new Color(0.21f, 0.20f, 0.22f);
        Color ember = new Color(0.85f, 0.34f, 0.10f);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float n = (N(x, y) - 0.5f) * 0.07f;
                float blotch = (N(x / 9 + 4, y / 9 + 7) - 0.5f) * 0.10f;
                Color c = dark + new Color(n, n * 0.97f, n) + new Color(blotch, blotch, blotch);

                // slab bevel: lighter toward the centre so each plot reads as a
                // cut stone block rather than a flat dark square
                float bx = Mathf.Abs(x - S * 0.5f) / (S * 0.5f);
                float by = Mathf.Abs(y - S * 0.5f) / (S * 0.5f);
                float bevel = Mathf.Clamp01(1f - Mathf.Max(bx, by));
                c += new Color(bevel * 0.05f, bevel * 0.05f, bevel * 0.055f);

                // a couple of hairline cracks with an ember glow inside
                float c1 = Mathf.Abs((y - 34) - Mathf.Sin(x * 0.09f) * 6f);
                float c2 = Mathf.Abs((x - 96) - Mathf.Cos(y * 0.11f) * 5f);
                float crack = Mathf.Min(c1, c2);
                if (crack < 1.6f) c = Color.Lerp(c, ember, (1.6f - crack) * 0.85f);
                if (x < 4 || y < 4 || x >= S - 4 || y >= S - 4) c = Color.Lerp(c, rock, 0.6f);
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        basalt = t;
        return t;
    }

    /// <summary>Cooled lava channel: black basalt crust with bright orange
    /// magma cracks running along the flow (the mob path).</summary>
    public static Texture2D LavaFlow()
    {
        if (lavaFlow != null) return lavaFlow;
        int S = 128;
        Texture2D t = New(S);
        Color crust = new Color(0.17f, 0.15f, 0.15f);
        Color magma = new Color(1.00f, 0.45f, 0.06f);
        Color hot = new Color(1.00f, 0.84f, 0.35f);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float n = (N(x, y) - 0.5f) * 0.09f;
                Color c = crust + new Color(n * 0.5f, n * 0.4f, n * 0.4f);

                // two molten channels running along the tile, with a wavy edge
                float wob = Mathf.Sin(x * 0.10f) * 3.5f;
                float d0 = Mathf.Abs((y - 40) - wob);
                float d1 = Mathf.Abs((y - 88) + wob);
                float seam = Mathf.Min(d0, d1);
                if (seam < 9f)
                {
                    float glow = Mathf.Clamp01(1f - seam / 9f);
                    Color m = Color.Lerp(magma, hot, glow * glow);
                    c = Color.Lerp(c, m, Mathf.Clamp01(glow * 1.35f));
                }
                // thin side cracks
                float sc = Mathf.Min(Mathf.Abs(y - 24), Mathf.Abs(y - 104));
                if (sc < 1.4f) c = Color.Lerp(c, magma, (1.4f - sc) * 0.6f);
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        lavaFlow = t;
        return t;
    }

    public static Texture2D LavaCross()
    {
        if (lavaCross != null) return lavaCross;
        int S = 128;
        Texture2D t = New(S);
        Color crust = new Color(0.17f, 0.15f, 0.15f);
        Color magma = new Color(1.00f, 0.45f, 0.06f);
        Color hot = new Color(1.00f, 0.84f, 0.35f);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float n = (N(x, y) - 0.5f) * 0.09f;
                Color c = crust + new Color(n * 0.5f, n * 0.4f, n * 0.4f);

                float wy = Mathf.Sin(x * 0.10f) * 3.5f;
                float wx = Mathf.Sin(y * 0.10f) * 3.5f;
                float d = Mathf.Min(
                    Mathf.Min(Mathf.Abs((y - 40) - wy), Mathf.Abs((y - 88) + wy)),
                    Mathf.Min(Mathf.Abs((x - 40) - wx), Mathf.Abs((x - 88) + wx)));
                if (d < 9f)
                {
                    float glow = Mathf.Clamp01(1f - d / 9f);
                    Color m = Color.Lerp(magma, hot, glow * glow);
                    c = Color.Lerp(c, m, Mathf.Clamp01(glow * 1.35f));
                }
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        lavaCross = t;
        return t;
    }

    // -------------------------------------------------- space-station theme
    static Texture2D deckPanel, magRail, magRailCross;

    /// <summary>Space-station deck plate for tower plots: gunmetal panel with a
    /// recessed seam, an inner bevel highlight and corner rivets. Tint per material.</summary>
    public static Texture2D DeckPanel()
    {
        if (deckPanel != null) return deckPanel;
        int S = 128;
        Texture2D t = New(S);
        Color plate = new Color(0.44f, 0.48f, 0.56f);
        Color seam = new Color(0.20f, 0.23f, 0.29f);
        Color rivet = new Color(0.60f, 0.65f, 0.73f);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float n = (N(x, y) - 0.5f) * 0.05f;
                float brush = (N(x / 3 + 9, y / 3 + 2) - 0.5f) * 0.05f;   // brushed metal streaks
                Color c = plate + new Color(n, n, n * 1.06f) + new Color(brush, brush, brush);

                const int b = 5;                       // recessed border seam
                if (x < b || y < b || x >= S - b || y >= S - b)
                {
                    c = seam;
                }
                else if (x < b + 3 || y < b + 3 || x >= S - b - 3 || y >= S - b - 3)
                {
                    c = Color.Lerp(c, rivet, 0.22f);   // bevel catches the light
                }
                else
                {
                    // corner rivets
                    int d = b + 5;
                    bool rx = Mathf.Abs(x - d) < 2 || Mathf.Abs(x - (S - 1 - d)) < 2;
                    bool ry = Mathf.Abs(y - d) < 2 || Mathf.Abs(y - (S - 1 - d)) < 2;
                    if (rx && ry) c = rivet;
                }
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        deckPanel = t;
        return t;
    }

    /// <summary>Mag-rail lane (the mob path): a dark rail bed with twin glowing
    /// cyan guide strips, so the route is by far the brightest thing on the board.</summary>
    public static Texture2D MagRail()
    {
        if (magRail != null) return magRail;
        int S = 128;
        Texture2D t = New(S);
        Color bed = new Color(0.13f, 0.15f, 0.19f);
        Color tread = new Color(0.20f, 0.23f, 0.29f);
        Color glow = new Color(0.35f, 0.92f, 1.00f);
        Color core = new Color(0.88f, 1.00f, 1.00f);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float n = (N(x, y) - 0.5f) * 0.04f;
                Color c = bed + new Color(n, n, n);
                if ((x % 16) < 8 && y > 20 && y < 108) c = tread + new Color(n, n, n);   // tread plates

                float s = Mathf.Min(Mathf.Abs(y - 26f), Mathf.Abs(y - 102f));
                if (s < 7f) c = Color.Lerp(c, glow, Mathf.Clamp01(1f - s / 7f));
                if (s < 2.2f) c = Color.Lerp(c, core, 1f - s / 2.2f);
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        magRail = t;
        return t;
    }

    public static Texture2D MagRailCross()
    {
        if (magRailCross != null) return magRailCross;
        int S = 128;
        Texture2D t = New(S);
        Color bed = new Color(0.13f, 0.15f, 0.19f);
        Color tread = new Color(0.20f, 0.23f, 0.29f);
        Color glow = new Color(0.35f, 0.92f, 1.00f);
        Color core = new Color(0.88f, 1.00f, 1.00f);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float n = (N(x, y) - 0.5f) * 0.04f;
                Color c = bed + new Color(n, n, n);
                if ((x % 16) < 8 && (y % 16) < 8) c = tread + new Color(n, n, n);

                float s = Mathf.Min(
                    Mathf.Min(Mathf.Abs(y - 26f), Mathf.Abs(y - 102f)),
                    Mathf.Min(Mathf.Abs(x - 26f), Mathf.Abs(x - 102f)));
                if (s < 7f) c = Color.Lerp(c, glow, Mathf.Clamp01(1f - s / 7f));
                if (s < 2.2f) c = Color.Lerp(c, core, 1f - s / 2.2f);
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        magRailCross = t;
        return t;
    }

    // ------------------------------------------------------ desert theme
    static Texture2D adobe, asphalt, asphaltCross;

    /// <summary>Sun-baked paving slab for tower plots: light sandstone with a
    /// 2x2 seam split and fine speckle. Kept light so per-material tints read
    /// as tan / terracotta / turquoise paving.</summary>
    public static Texture2D Adobe()
    {
        if (adobe != null) return adobe;
        int S = 128;
        Texture2D t = New(S);
        Color baseC = new Color(0.94f, 0.90f, 0.83f);
        Color seam = new Color(0.68f, 0.62f, 0.53f);
        Color fleck = new Color(0.82f, 0.75f, 0.64f);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float n = (N(x, y) - 0.5f) * 0.05f;
                float mot = (N(x / 11 + 3, y / 11 + 5) - 0.5f) * 0.06f;
                Color c = baseC + new Color(n, n * 0.96f, n * 0.86f) + new Color(mot, mot * 0.95f, mot * 0.82f);
                // paving seams: a 2x2 split, plus the slab border
                if (Mathf.Abs(x - S / 2) < 2 || Mathf.Abs(y - S / 2) < 2) c = seam;
                if (x < 3 || y < 3 || x >= S - 3 || y >= S - 3) c = Color.Lerp(c, seam, 0.75f);
                if (N(x * 7 + 1, y * 7 + 9) > 0.988f) c = fleck;
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        adobe = t;
        return t;
    }

    /// <summary>Highway lane (the mob path): dark asphalt with a dashed white
    /// centre line and solid white edge lines. The lines run along the tile's
    /// local X, matching how the track textures are oriented.</summary>
    public static Texture2D Asphalt()
    {
        if (asphalt != null) return asphalt;
        int S = 128;
        Texture2D t = New(S);
        Color road = new Color(0.19f, 0.19f, 0.20f);
        Color grit = new Color(0.29f, 0.29f, 0.30f);
        Color paint = new Color(0.96f, 0.95f, 0.90f);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float n = (N(x, y) - 0.5f) * 0.07f;
                Color c = road + new Color(n, n, n);
                if (N(x * 5 + 2, y * 5 + 6) > 0.965f) c = grit + new Color(n, n, n);
                if (Mathf.Abs(y - 20) < 2 || Mathf.Abs(y - 108) < 2) c = paint;              // edge lines
                if (Mathf.Abs(y - 64) < 3 && (x % 32) < 18) c = paint;                        // centre dashes
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        asphalt = t;
        return t;
    }

    public static Texture2D AsphaltCross()
    {
        if (asphaltCross != null) return asphaltCross;
        int S = 128;
        Texture2D t = New(S);
        Color road = new Color(0.19f, 0.19f, 0.20f);
        Color grit = new Color(0.29f, 0.29f, 0.30f);
        Color paint = new Color(0.96f, 0.95f, 0.90f);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float n = (N(x, y) - 0.5f) * 0.07f;
                Color c = road + new Color(n, n, n);
                if (N(x * 5 + 2, y * 5 + 6) > 0.965f) c = grit + new Color(n, n, n);

                bool edge = Mathf.Abs(y - 20) < 2 || Mathf.Abs(y - 108) < 2
                         || Mathf.Abs(x - 20) < 2 || Mathf.Abs(x - 108) < 2;
                bool dashX = Mathf.Abs(y - 64) < 3 && (x % 32) < 18;
                bool dashY = Mathf.Abs(x - 64) < 3 && (y % 32) < 18;
                if (edge || dashX || dashY) c = paint;
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        asphaltCross = t;
        return t;
    }

    // ------------------------------------------------------- candy theme
    static Texture2D candy, chocolate, chocolateCross;

    /// <summary>Pastel candy slab for tower plots: matte taffy with a pressed
    /// edge and a scatter of sugar crystals. Kept light so per-material tints
    /// read as pink / blue / lemon / mint candy.</summary>
    public static Texture2D Candy()
    {
        if (candy != null) return candy;
        int S = 128;
        Texture2D t = New(S);
        Color baseC = new Color(0.96f, 0.95f, 0.94f);
        Color edge = new Color(0.78f, 0.76f, 0.77f);
        Color sugar = new Color(1.00f, 1.00f, 1.00f);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float n = (N(x, y) - 0.5f) * 0.03f;
                float soft = (N(x / 9 + 2, y / 9 + 6) - 0.5f) * 0.05f;   // soft taffy blotch
                Color c = baseC + new Color(n, n, n) + new Color(soft, soft, soft);

                const int b = 6;                       // pressed candy edge
                if (x < b || y < b || x >= S - b || y >= S - b)
                {
                    c = edge;
                }
                else if (x < b + 2 || y < b + 2 || x >= S - b - 2 || y >= S - b - 2)
                {
                    c = Color.Lerp(c, sugar, 0.35f);
                }
                if (N(x * 11 + 5, y * 11 + 3) > 0.975f) c = sugar;   // sugar crystals
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        candy = t;
        return t;
    }

    /// <summary>Chocolate lane (the mob path): dark glossy chocolate bar segments
    /// with a wavy piped icing drizzle running along both edges.</summary>
    public static Texture2D Chocolate()
    {
        if (chocolate != null) return chocolate;
        int S = 128;
        Texture2D t = New(S);
        Color choc = new Color(0.26f, 0.14f, 0.08f);
        Color chocLight = new Color(0.38f, 0.21f, 0.12f);
        Color icing = new Color(0.97f, 0.95f, 0.92f);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float n = (N(x, y) - 0.5f) * 0.06f;
                Color c = choc + new Color(n, n * 0.8f, n * 0.6f);
                if ((x % 32) < 2) c = chocLight * 0.7f;                             // bar grooves

                float gl = Mathf.Clamp01(1f - Mathf.Abs(y - 44f) / 26f);            // glossy sheen
                c = Color.Lerp(c, chocLight, gl * 0.30f);

                float wy = Mathf.Sin(x * 0.26f) * 4f;                              // wavy icing drizzle
                float d = Mathf.Min(Mathf.Abs(y - (26f + wy)), Mathf.Abs(y - (102f - wy)));
                if (d < 4.5f) c = Color.Lerp(c, icing, Mathf.Clamp01(1f - d / 4.5f));
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        chocolate = t;
        return t;
    }

    public static Texture2D ChocolateCross()
    {
        if (chocolateCross != null) return chocolateCross;
        int S = 128;
        Texture2D t = New(S);
        Color choc = new Color(0.26f, 0.14f, 0.08f);
        Color chocLight = new Color(0.38f, 0.21f, 0.12f);
        Color icing = new Color(0.97f, 0.95f, 0.92f);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float n = (N(x, y) - 0.5f) * 0.06f;
                Color c = choc + new Color(n, n * 0.8f, n * 0.6f);
                if ((x % 32) < 2 || (y % 32) < 2) c = chocLight * 0.7f;

                float gl = Mathf.Clamp01(1f - Mathf.Abs((x + y) * 0.5f - 64f) / 30f);
                c = Color.Lerp(c, chocLight, gl * 0.22f);

                float wy = Mathf.Sin(x * 0.26f) * 4f;
                float wx = Mathf.Sin(y * 0.26f) * 4f;
                float d = Mathf.Min(
                    Mathf.Min(Mathf.Abs(y - (26f + wy)), Mathf.Abs(y - (102f - wy))),
                    Mathf.Min(Mathf.Abs(x - (26f + wx)), Mathf.Abs(x - (102f - wx))));
                if (d < 4.5f) c = Color.Lerp(c, icing, Mathf.Clamp01(1f - d / 4.5f));
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        chocolateCross = t;
        return t;
    }

    // -------------------------------------------------------- sewer theme
    static Texture2D concrete, railTrack, railCross;

    /// <summary>Damp concrete slab for tower plots: mid-grey with a painted
    /// hazard border and scuff marks. Kept light so tints read as grey /
    /// hazard yellow / caution orange / olive.</summary>
    public static Texture2D Concrete()
    {
        if (concrete != null) return concrete;
        int S = 128;
        Texture2D t = New(S);
        Color baseC = new Color(0.84f, 0.84f, 0.82f);
        Color paint = new Color(0.58f, 0.56f, 0.52f);
        Color scuff = new Color(0.70f, 0.69f, 0.66f);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float n = (N(x, y) - 0.5f) * 0.05f;
                float grime = (N(x / 13 + 6, y / 13 + 2) - 0.5f) * 0.09f;
                Color c = baseC + new Color(n, n, n) + new Color(grime, grime * 0.98f, grime * 0.92f);

                const int b = 7;                       // painted hazard border
                if (x < b || y < b || x >= S - b || y >= S - b) c = paint;
                if (N(x * 13 + 4, y * 13 + 7) > 0.982f) c = scuff;
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        concrete = t;
        return t;
    }

    /// <summary>Rail line (the mob path): dark wet ballast with sleepers and two
    /// bright polished steel rails, so the route is the brightest element.</summary>
    public static Texture2D RailTrack()
    {
        if (railTrack != null) return railTrack;
        int S = 128;
        Texture2D t = New(S);
        Color ballast = new Color(0.30f, 0.28f, 0.27f);
        Color sleeper = new Color(0.24f, 0.20f, 0.17f);
        Color steel = new Color(0.92f, 0.94f, 0.97f);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float n = (N(x, y) - 0.5f) * 0.08f;
                Color c = ballast + new Color(n, n * 0.96f, n * 0.90f);
                if ((x % 16) < 6 && y > 22 && y < 106) c = sleeper + new Color(n, n, n);
                if ((y >= 40 && y <= 45) || (y >= 83 && y <= 88)) c = steel;
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        railTrack = t;
        return t;
    }

    public static Texture2D RailCross()
    {
        if (railCross != null) return railCross;
        int S = 128;
        Texture2D t = New(S);
        Color ballast = new Color(0.30f, 0.28f, 0.27f);
        Color sleeper = new Color(0.24f, 0.20f, 0.17f);
        Color steel = new Color(0.92f, 0.94f, 0.97f);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float n = (N(x, y) - 0.5f) * 0.08f;
                Color c = ballast + new Color(n, n * 0.96f, n * 0.90f);
                if ((x % 16) < 6 || (y % 16) < 6) c = sleeper + new Color(n, n, n);
                bool rail = ((y >= 40 && y <= 45) || (y >= 83 && y <= 88)
                          || (x >= 40 && x <= 45) || (x >= 83 && x <= 88));
                if (rail) c = steel;
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        railCross = t;
        return t;
    }

    // ------------------------------------------------------- castle theme
    static Texture2D banner, cobble, cobbleCross;

    /// <summary>Heraldic banner for tower plots: flat matte cloth with a woven
    /// texture and a broad chevron. Kept light so tints read as red / blue /
    /// gold / green heraldry.</summary>
    public static Texture2D Banner()
    {
        if (banner != null) return banner;
        int S = 128;
        Texture2D t = New(S);
        Color cloth = new Color(0.96f, 0.95f, 0.92f);
        Color weaveC = new Color(0.88f, 0.86f, 0.83f);
        Color chevron = new Color(0.74f, 0.72f, 0.68f);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float n = (N(x, y) - 0.5f) * 0.03f;
                float w = (((x / 2) + (y / 2)) % 2 == 0) ? 0.022f : -0.022f;   // cloth weave
                Color c = cloth + new Color(n + w, n + w, n + w);

                float dx = Mathf.Abs(x - S * 0.5f);
                float cy = 46f - dx * 0.45f;                                    // upward chevron
                float d = Mathf.Abs(y - cy);
                if (d < 8f) c = Color.Lerp(c, chevron, 1f - d / 8f);

                if (x < 3 || y < 3 || x >= S - 3 || y >= S - 3) c = weaveC * 0.92f;
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        banner = t;
        return t;
    }

    /// <summary>Cobblestone road (the mob path): irregular grey stones with
    /// weathered timber edging along the direction of travel.</summary>
    public static Texture2D Cobble()
    {
        if (cobble != null) return cobble;
        int S = 128;
        Texture2D t = New(S);
        Color stone = new Color(0.76f, 0.75f, 0.73f);
        Color mortar = new Color(0.56f, 0.55f, 0.53f);
        Color plank = new Color(0.52f, 0.38f, 0.24f);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float jitter = (N(x / 16, y / 16) - 0.5f) * 0.13f;             // per-stone tone
                float n = (N(x, y) - 0.5f) * 0.05f;
                Color c = stone + new Color(jitter + n, jitter + n, jitter + n);
                if ((x % 16) < 3 || (y % 16) < 3) c = mortar + new Color(n, n, n);
                if (y < 6 || y >= S - 6) c = plank;                            // timber edging
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        cobble = t;
        return t;
    }

    public static Texture2D CobbleCross()
    {
        if (cobbleCross != null) return cobbleCross;
        int S = 128;
        Texture2D t = New(S);
        Color stone = new Color(0.76f, 0.75f, 0.73f);
        Color mortar = new Color(0.56f, 0.55f, 0.53f);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float jitter = (N(x / 16, y / 16) - 0.5f) * 0.13f;
                float n = (N(x, y) - 0.5f) * 0.05f;
                Color c = stone + new Color(jitter + n, jitter + n, jitter + n);
                if ((x % 16) < 3 || (y % 16) < 3) c = mortar + new Color(n, n, n);
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        cobbleCross = t;
        return t;
    }

    // ------------------------------------------------------ factory theme
    static Texture2D safetyPlate, conveyor, conveyorCross;

    /// <summary>Painted steel plate for tower plots: light base with scuffed
    /// paint patches and corner bolts. Tints read as machine blue / safety
    /// yellow / industrial grey / dark green.</summary>
    public static Texture2D SafetyPlate()
    {
        if (safetyPlate != null) return safetyPlate;
        int S = 128;
        Texture2D t = New(S);
        Color plate = new Color(0.92f, 0.92f, 0.90f);
        Color scuff = new Color(0.76f, 0.75f, 0.73f);
        Color rivet = new Color(0.64f, 0.64f, 0.64f);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float n = (N(x, y) - 0.5f) * 0.04f;
                float wear = (N(x / 7 + 1, y / 7 + 8) - 0.5f) * 0.10f;
                Color c = plate + new Color(n, n, n) + new Color(wear, wear * 0.98f, wear * 0.96f);
                if (N(x * 17 + 3, y * 17 + 5) > 0.976f) c = scuff;             // chipped paint

                const int d = 8;                                               // corner bolts
                bool rx = Mathf.Abs(x - d) < 2 || Mathf.Abs(x - (S - 1 - d)) < 2;
                bool ry = Mathf.Abs(y - d) < 2 || Mathf.Abs(y - (S - 1 - d)) < 2;
                if (rx && ry) c = rivet;
                if (x < 3 || y < 3 || x >= S - 3 || y >= S - 3) c = Color.Lerp(c, scuff, 0.7f);
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        safetyPlate = t;
        return t;
    }

    /// <summary>Conveyor belt (the mob path): black rubber with steel roller
    /// segments and a bright painted safety stripe along one edge.</summary>
    public static Texture2D Conveyor()
    {
        if (conveyor != null) return conveyor;
        int S = 128;
        Texture2D t = New(S);
        Color belt = new Color(0.13f, 0.13f, 0.14f);
        Color beltLit = new Color(0.21f, 0.21f, 0.23f);
        Color roller = new Color(0.56f, 0.58f, 0.61f);
        Color stripe = new Color(0.96f, 0.80f, 0.16f);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float n = (N(x, y) - 0.5f) * 0.04f;
                Color c = belt + new Color(n, n, n);
                if ((x % 14) < 5 && y > 18 && y < 110) c = roller + new Color(n, n, n);

                float gl = Mathf.Clamp01(1f - Mathf.Abs(y - 64f) / 30f);
                c = Color.Lerp(c, beltLit, gl * 0.5f);

                if (Mathf.Abs(y - 22f) < 7f) c = stripe;                        // safety stripe
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        conveyor = t;
        return t;
    }

    public static Texture2D ConveyorCross()
    {
        if (conveyorCross != null) return conveyorCross;
        int S = 128;
        Texture2D t = New(S);
        Color belt = new Color(0.13f, 0.13f, 0.14f);
        Color beltLit = new Color(0.21f, 0.21f, 0.23f);
        Color roller = new Color(0.56f, 0.58f, 0.61f);
        Color stripe = new Color(0.96f, 0.80f, 0.16f);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float n = (N(x, y) - 0.5f) * 0.04f;
                Color c = belt + new Color(n, n, n);
                if ((x % 14) < 5 || (y % 14) < 5) c = roller + new Color(n, n, n);

                float gl = Mathf.Clamp01(1f - Mathf.Abs((x + y) * 0.5f - 64f) / 34f);
                c = Color.Lerp(c, beltLit, gl * 0.35f);

                bool stripeOn = Mathf.Abs(y - 22f) < 7f || Mathf.Abs(x - 22f) < 7f;
                if (stripeOn) c = stripe;
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        conveyorCross = t;
        return t;
    }

    // --------------------------------------------------------- reef theme
    static Texture2D seabed, shipWreck, shipWreckCross;

    /// <summary>Seabed tile for tower plots: matte sand with scattered grit and
    /// shell flecks. Kept light so tints read as coral / anemone / algae / shell.</summary>
    public static Texture2D Seabed()
    {
        if (seabed != null) return seabed;
        int S = 128;
        Texture2D t = New(S);
        Color baseC = new Color(0.94f, 0.93f, 0.91f);
        Color grit = new Color(0.82f, 0.80f, 0.78f);
        Color shell = new Color(0.99f, 0.99f, 0.98f);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float n = (N(x, y) - 0.5f) * 0.05f;
                float mot = (N(x / 8 + 4, y / 8 + 9) - 0.5f) * 0.07f;
                Color c = baseC + new Color(n, n, n) + new Color(mot, mot, mot);
                if (N(x * 19 + 7, y * 19 + 2) > 0.972f) c = grit;
                if (N(x * 7 + 11, y * 7 + 6) > 0.988f) c = shell;
                if (x < 3 || y < 3 || x >= S - 3 || y >= S - 3) c = Color.Lerp(c, grit, 0.6f);
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        seabed = t;
        return t;
    }

    /// <summary>Sunken wreck walkway (the mob path): weathered dark planks laid
    /// across the route with pale frayed rope lines along both edges.</summary>
    public static Texture2D ShipWreck()
    {
        if (shipWreck != null) return shipWreck;
        int S = 128;
        Texture2D t = New(S);
        Color plank = new Color(0.30f, 0.24f, 0.19f);
        Color plankAlt = new Color(0.23f, 0.18f, 0.14f);
        Color gap = new Color(0.15f, 0.12f, 0.10f);
        Color rope = new Color(0.86f, 0.79f, 0.60f);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float n = (N(x, y) - 0.5f) * 0.06f;
                Color c = (((x / 22) % 2 == 0) ? plank : plankAlt) + new Color(n, n * 0.9f, n * 0.8f);
                if ((x % 22) < 2) c = gap;
                float d = Mathf.Min(Mathf.Abs(y - 20f), Mathf.Abs(y - 108f));
                if (d < 5f) c = Color.Lerp(c, rope, Mathf.Clamp01(1f - d / 5f));
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        shipWreck = t;
        return t;
    }

    public static Texture2D ShipWreckCross()
    {
        if (shipWreckCross != null) return shipWreckCross;
        int S = 128;
        Texture2D t = New(S);
        Color plank = new Color(0.30f, 0.24f, 0.19f);
        Color plankAlt = new Color(0.23f, 0.18f, 0.14f);
        Color rope = new Color(0.86f, 0.79f, 0.60f);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float n = (N(x, y) - 0.5f) * 0.06f;
                Color c = ((((x + y) / 22) % 2 == 0) ? plank : plankAlt) + new Color(n, n * 0.9f, n * 0.8f);
                float d = Mathf.Min(
                    Mathf.Min(Mathf.Abs(y - 20f), Mathf.Abs(y - 108f)),
                    Mathf.Min(Mathf.Abs(x - 20f), Mathf.Abs(x - 108f)));
                if (d < 5f) c = Color.Lerp(c, rope, Mathf.Clamp01(1f - d / 5f));
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        shipWreckCross = t;
        return t;
    }

    // ---------------------------------------------------------- zen theme
    static Texture2D rakedGravel, steppingPath, steppingPathCross;

    /// <summary>Raked gravel plot tile: fine wavy rake lines over pale sand.</summary>
    public static Texture2D RakedGravel()
    {
        if (rakedGravel != null) return rakedGravel;
        int S = 128;
        Texture2D t = New(S);
        Color sand = new Color(0.95f, 0.94f, 0.91f);
        Color groove = new Color(0.80f, 0.79f, 0.76f);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float n = (N(x, y) - 0.5f) * 0.03f;
                Color c = sand + new Color(n, n, n);
                float wave = Mathf.Sin(y * 0.45f + Mathf.Sin(x * 0.08f) * 1.4f) * 0.5f + 0.5f;
                c = Color.Lerp(c, groove, wave * 0.55f);
                if (x < 4 || y < 4 || x >= S - 4 || y >= S - 4) c = Color.Lerp(c, groove, 0.85f);
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        rakedGravel = t;
        return t;
    }

    /// <summary>Stepping-stone walkway (the mob path): pale raked sand with a
    /// band of dark irregular stone slabs down the middle, edged in timber.</summary>
    public static Texture2D SteppingPath()
    {
        if (steppingPath != null) return steppingPath;
        int S = 128;
        Texture2D t = New(S);
        Color sand = new Color(0.96f, 0.95f, 0.90f);
        Color sandGroove = new Color(0.84f, 0.83f, 0.78f);
        Color stone = new Color(0.40f, 0.41f, 0.42f);
        Color stoneLit = new Color(0.52f, 0.53f, 0.54f);
        Color wood = new Color(0.50f, 0.37f, 0.24f);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float n = (N(x, y) - 0.5f) * 0.04f;
                Color c = sand + new Color(n, n, n);
                c = Color.Lerp(c, sandGroove, (Mathf.Sin(y * 0.5f) * 0.5f + 0.5f) * 0.5f);

                float halfBand = 30f + Mathf.Sin(x * 0.18f) * 5f;
                if (Mathf.Abs(y - 64f) < halfBand)
                {
                    c = stone + new Color(n, n, n);
                    if (Mathf.Abs(y - 64f) < 8f) c = Color.Lerp(c, stoneLit, 0.4f);
                }
                if (y < 6 || y >= S - 6) c = wood;
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        steppingPath = t;
        return t;
    }

    public static Texture2D SteppingPathCross()
    {
        if (steppingPathCross != null) return steppingPathCross;
        int S = 128;
        Texture2D t = New(S);
        Color sand = new Color(0.96f, 0.95f, 0.90f);
        Color sandGroove = new Color(0.84f, 0.83f, 0.78f);
        Color stone = new Color(0.40f, 0.41f, 0.42f);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float n = (N(x, y) - 0.5f) * 0.04f;
                Color c = sand + new Color(n, n, n);
                c = Color.Lerp(c, sandGroove, (Mathf.Sin((x + y) * 0.5f) * 0.5f + 0.5f) * 0.5f);
                if (Mathf.Abs(y - 64f) < 26f || Mathf.Abs(x - 64f) < 26f) c = stone + new Color(n, n, n);
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        steppingPathCross = t;
        return t;
    }

    // ---------------------------------------------------- classroom theme
    static Texture2D notePaper, notebook, notebookCross;

    /// <summary>Sticky-note plot tile: flat paper with a soft fibre and a folded
    /// lower corner. Kept near-white so tints read as bright notes.</summary>
    public static Texture2D NotePaper()
    {
        if (notePaper != null) return notePaper;
        int S = 128;
        Texture2D t = New(S);
        Color paper = new Color(0.97f, 0.97f, 0.96f);
        Color fibre = new Color(0.93f, 0.93f, 0.92f);
        Color edge = new Color(0.82f, 0.81f, 0.80f);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float n = (N(x, y) - 0.5f) * 0.03f;
                float fib = (N(x / 2 + 3, y * 3 + 5) - 0.5f) * 0.03f;
                Color c = paper + new Color(n, n, n) + new Color(fib, fib, fib);
                if (x + y > 2 * S - 32) c = Color.Lerp(c, fibre, 0.75f);   // dog-eared corner
                if (x < 3 || y < 3 || x >= S - 3 || y >= S - 3) c = edge;
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        notePaper = t;
        return t;
    }

    /// <summary>Ruled notebook sheet (the mob path): white paper with blue rules,
    /// a red margin and grey pencil rails along both edges.</summary>
    public static Texture2D Notebook()
    {
        if (notebook != null) return notebook;
        int S = 128;
        Texture2D t = New(S);
        Color paper = new Color(0.98f, 0.98f, 0.97f);
        Color rule = new Color(0.70f, 0.79f, 0.90f);
        Color margin = new Color(0.86f, 0.55f, 0.55f);
        Color pencil = new Color(0.62f, 0.58f, 0.52f);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float n = (N(x, y) - 0.5f) * 0.02f;
                Color c = paper + new Color(n, n, n);
                if ((y % 16) < 2) c = rule;                       // ruled lines
                if (Mathf.Abs(x - 18) < 2) c = margin;            // red margin
                if (y < 7 || y >= S - 7) c = pencil;              // pencil rails
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        notebook = t;
        return t;
    }

    public static Texture2D NotebookCross()
    {
        if (notebookCross != null) return notebookCross;
        int S = 128;
        Texture2D t = New(S);
        Color paper = new Color(0.98f, 0.98f, 0.97f);
        Color rule = new Color(0.70f, 0.79f, 0.90f);
        Color pencil = new Color(0.62f, 0.58f, 0.52f);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float n = (N(x, y) - 0.5f) * 0.02f;
                Color c = paper + new Color(n, n, n);
                if ((y % 16) < 2 || (x % 16) < 2) c = rule;
                bool rail = y < 7 || y >= S - 7 || x < 7 || x >= S - 7;
                if (rail) c = pencil;
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        notebookCross = t;
        return t;
    }

    // ---------------------------------------------------------- shared UI
    static Texture2D button, buttonHover, buttonPressed, panel, field, menuFade;

    public static Texture2D Button() { return button != null ? button : (button = BuildButton(0)); }
    public static Texture2D ButtonHover() { return buttonHover != null ? buttonHover : (buttonHover = BuildButton(1)); }
    public static Texture2D ButtonPressed() { return buttonPressed != null ? buttonPressed : (buttonPressed = BuildButton(2)); }
    public static Texture2D Panel() { return panel != null ? panel : (panel = Solid(new Color(0.12f, 0.13f, 0.14f, 0.96f))); }
    public static Texture2D Field() { return field != null ? field : (field = Solid(new Color(0.08f, 0.09f, 0.10f))); }

    static Texture2D Solid(Color color)
    {
        Texture2D t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp;
        t.SetPixels(new[] { color, color, color, color });
        t.Apply();
        return t;
    }

    // Top lighter than bottom; hover lifts the fill, pressed darkens it.
    static Texture2D BuildButton(int state)
    {
        const int height = 64;
        Texture2D t = new Texture2D(2, height, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp;
        t.filterMode = FilterMode.Bilinear;
        Color bottom = state == 2 ? new Color(0.07f, 0.27f, 0.13f) :
            state == 1 ? new Color(0.12f, 0.40f, 0.20f) : new Color(0.09f, 0.34f, 0.17f);
        Color top = state == 2 ? new Color(0.12f, 0.37f, 0.19f) :
            state == 1 ? new Color(0.22f, 0.55f, 0.29f) : new Color(0.17f, 0.46f, 0.24f);
        for (int y = 0; y < height; y++)
        {
            Color c = Color.Lerp(bottom, top, y / (float)(height - 1));
            t.SetPixel(0, y, c);
            t.SetPixel(1, y, c);
        }
        t.Apply();
        return t;
    }

    /// <summary>
    /// Dark-grey full-screen scrim. Slightly darker at the top to keep titles
    /// readable while the orbiting board remains faintly visible underneath.
    /// </summary>
    public static Texture2D MenuFade()
    {
        if (menuFade != null) return menuFade;
        int S = 64;
        Texture2D t = new Texture2D(1, S, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp;
        t.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < S; y++)
        {
            float f = y / (float)(S - 1);                     // 1 = top row of the texture
            float a = Mathf.Lerp(0.83f, 0.93f, f);
            t.SetPixel(0, y, new Color(0.10f, 0.11f, 0.12f, a));
        }
        t.Apply();
        menuFade = t;
        return t;
    }

    // ------------------------------------------------------ mob status icons
    static Texture2D iconBurn, iconSlow, iconTar;

    /// <summary>Orange burn flame on a transparent background — the
    /// damage-over-time status (replaced the green poison droplet).</summary>
    public static Texture2D IconBurn()
    {
        if (iconBurn != null) return iconBurn;
        int S = 64;
        Texture2D t = New(S);
        t.wrapMode = TextureWrapMode.Clamp;   // a single icon, not a tiling texture
        Color body = new Color(1.00f, 0.42f, 0.06f);   // orange flame
        Color edge = new Color(0.55f, 0.10f, 0.02f);   // deep red rim
        Color hot  = new Color(1.00f, 0.80f, 0.18f);   // yellow-hot upper flame
        Color core = new Color(1.00f, 0.96f, 0.62f);   // pale inner core

        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float u = (x + 0.5f) / S * 2f - 1f;   // -1 .. 1, y up
                float v = (y + 0.5f) / S * 2f - 1f;

                // Flame: a round base with a tapered point, pinched at the waist
                // so the silhouette reads as fire rather than a teardrop.
                const float cy = -0.30f, r = 0.56f, top = 0.92f;
                float inside;
                if (v <= cy)
                {
                    float dx = u, dy = v - cy;
                    inside = r - Mathf.Sqrt(dx * dx + dy * dy);
                }
                else
                {
                    float taper = Mathf.Clamp01((top - v) / (top - cy));
                    float halfW = r * taper * (0.72f + 0.28f * taper);   // pinched upward
                    inside = Mathf.Min(halfW - Mathf.Abs(u), top - v);
                }
                float cov = Mathf.Clamp01(inside * S * 0.5f + 0.5f);   // ~1px anti-alias
                if (cov <= 0.001f) { t.SetPixel(x, y, new Color(0f, 0f, 0f, 0f)); continue; }

                float rim = Mathf.Clamp01(inside / 0.14f);             // darken near the edge
                Color c = Color.Lerp(edge, body, rim);
                c = Color.Lerp(c, hot, Mathf.Clamp01((inside + 0.06f) / 0.52f) * 0.85f);   // hotter toward the centre

                float gx = u, gy = v + 0.24f;                          // pale core low in the flame
                float gl = Mathf.Clamp01(1f - Mathf.Sqrt(gx * gx + gy * gy) / 0.26f);
                c = Color.Lerp(c, core, gl * 0.70f);
                t.SetPixel(x, y, new Color(c.r, c.g, c.b, cov));
            }
        }
        t.Apply();
        t.filterMode = FilterMode.Bilinear;
        iconBurn = t;
        return t;
    }

    /// <summary>Light-blue ice cube with a white snowflake on a transparent background.</summary>
    public static Texture2D IconSlow()
    {
        if (iconSlow != null) return iconSlow;
        int S = 64;
        Texture2D t = New(S);
        t.wrapMode = TextureWrapMode.Clamp;
        Color top = new Color(0.78f, 0.94f, 1.00f);
        Color bottom = new Color(0.40f, 0.72f, 0.95f);
        Color edge = new Color(0.20f, 0.46f, 0.78f);
        Color snow = new Color(0.97f, 1.00f, 1.00f);

        const float half = 0.80f, radius = 0.24f;
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float u = (x + 0.5f) / S * 2f - 1f;
                float v = (y + 0.5f) / S * 2f - 1f;

                // Rounded box (signed distance field; negative inside).
                float qx = Mathf.Abs(u) - (half - radius);
                float qy = Mathf.Abs(v) - (half - radius);
                float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
                float d = Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
                float inside = -d;
                float cov = Mathf.Clamp01(inside * S * 0.5f + 0.5f);
                if (cov <= 0.001f) { t.SetPixel(x, y, new Color(0f, 0f, 0f, 0f)); continue; }

                float grad = Mathf.Clamp01((v + 1f) * 0.5f);
                Color c = Color.Lerp(bottom, top, grad);
                c = Color.Lerp(edge, c, Mathf.Clamp01(inside / 0.16f));   // shaded rim

                float snowCov = Mathf.Clamp01((0.050f - SnowflakeDist(u, v)) * S * 0.5f + 0.5f);
                c = Color.Lerp(c, snow, snowCov);

                t.SetPixel(x, y, new Color(c.r, c.g, c.b, cov));
            }
        }
        t.Apply();
        t.filterMode = FilterMode.Bilinear;
        iconSlow = t;
        return t;
    }

    /// <summary>Glossy amber syrup droplet on a transparent background — the
    /// Sticky Tar debuff (targets take extra damage), deliberately warmer and
    /// browner than the green poison droplet.</summary>
    public static Texture2D IconTar()
    {
        if (iconTar != null) return iconTar;
        int S = 64;
        Texture2D t = New(S);
        t.wrapMode = TextureWrapMode.Clamp;   // a single icon, not a tiling texture
        Color body = new Color(0.88f, 0.54f, 0.14f);   // warm amber syrup
        Color edge = new Color(0.26f, 0.12f, 0.03f);   // dark brown rim
        Color gloss = new Color(1.00f, 0.87f, 0.55f);  // pale golden highlight

        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float u = (x + 0.5f) / S * 2f - 1f;   // -1 .. 1, y up
                float v = (y + 0.5f) / S * 2f - 1f;

                // Fat teardrop (thicker than the poison drip) with a tapered
                // point above it, plus a small satellite blob on the right so
                // the silhouette reads as one thick dollop of syrup.
                const float cy = -0.24f, r = 0.60f, top = 0.88f;
                float inside;
                if (v <= cy)
                {
                    float dx = u, dy = v - cy;
                    inside = r - Mathf.Sqrt(dx * dx + dy * dy);
                }
                else
                {
                    float halfW = r * Mathf.Clamp01((top - v) / (top - cy));
                    inside = Mathf.Min(halfW - Mathf.Abs(u), top - v);
                }

                float bx = u - 0.62f, by = v + 0.42f;   // satellite drip
                float inside2 = 0.20f - Mathf.Sqrt(bx * bx + by * by);
                if (inside2 > inside) inside = inside2;

                float cov = Mathf.Clamp01(inside * S * 0.5f + 0.5f);   // ~1px anti-alias
                if (cov <= 0.001f) { t.SetPixel(x, y, new Color(0f, 0f, 0f, 0f)); continue; }

                float rim = Mathf.Clamp01(inside / 0.14f);             // darken near the edge
                Color c = Color.Lerp(edge, body, rim);
                c = Color.Lerp(c, gloss, Mathf.Clamp01((v + 0.30f) / 0.55f) * 0.32f);   // light from above

                float gx = u + 0.22f, gy = v - 0.04f;                  // glossy highlight
                float gl = Mathf.Clamp01(1f - Mathf.Sqrt(gx * gx + gy * gy) / 0.20f);
                c = Color.Lerp(c, gloss, gl * 0.80f);
                t.SetPixel(x, y, new Color(c.r, c.g, c.b, cov));
            }
        }
        t.Apply();
        t.filterMode = FilterMode.Bilinear;
        iconTar = t;
        return t;
    }

    /// <summary>Distance from (x, y) to the nearest line of a 6-arm snowflake.</summary>
    static float SnowflakeDist(float x, float y)
    {
        float best = 10f;
        for (int i = 0; i < 6; i++)
        {
            float a = i * Mathf.PI / 3f;
            float ex = Mathf.Cos(a) * 0.58f, ey = Mathf.Sin(a) * 0.58f;
            best = Mathf.Min(best, SegmentDist(x, y, 0f, 0f, ex, ey));

            // two small branches near the outer third of each arm
            float bx = Mathf.Cos(a) * 0.36f, by = Mathf.Sin(a) * 0.36f;
            for (int s = -1; s <= 1; s += 2)
            {
                float ba = a + s * Mathf.PI / 3f;
                best = Mathf.Min(best, SegmentDist(x, y, bx, by,
                    bx + Mathf.Cos(ba) * 0.20f, by + Mathf.Sin(ba) * 0.20f));
            }
        }
        return best;
    }

    static float SegmentDist(float px, float py, float ax, float ay, float bx, float by)
    {
        float dx = bx - ax, dy = by - ay;
        float l2 = dx * dx + dy * dy;
        float q = l2 > 0f ? Mathf.Clamp01(((px - ax) * dx + (py - ay) * dy) / l2) : 0f;
        float cx = ax + q * dx, cy = ay + q * dy;
        return Mathf.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy));
    }
}
