using UnityEngine;

// Procedurally generated tile textures (no image assets).
public static class TDTextures
{
    private static Texture2D sidewalk, road, roadCross;

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

    // ------------------------------------------------------------ paper (UI)
    static Texture2D paper, paperHover, paperPressed, menuFade;

    /// <summary>Off-white construction paper (≈ #F2EEE4) for menu buttons.</summary>
    public static Texture2D Paper() { return paper != null ? paper : (paper = BuildPaper(1f)); }

    /// <summary>Paper a touch darker, for the hover state of a paper button.</summary>
    public static Texture2D PaperHover() { return paperHover != null ? paperHover : (paperHover = BuildPaper(0.94f)); }

    /// <summary>Paper darker still, for the pressed state of a paper button.</summary>
    public static Texture2D PaperPressed() { return paperPressed != null ? paperPressed : (paperPressed = BuildPaper(0.87f)); }

    // Light sheet with subtle mottling, fine grain, pressed fibres and flecks.
    // Kept deliberately light so black UI text stays crisp on top.
    static Texture2D BuildPaper(float shade)
    {
        int S = 128;
        Texture2D t = New(S);
        t.wrapMode = TextureWrapMode.Clamp;      // stretched across the button, not tiled
        Color baseC = new Color(0.949f, 0.933f, 0.894f); // ~#F2EEE4
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float mottle = (N(x / 7, y / 7) - 0.5f) * 0.045f;   // broad blotches
                float grain = (N(x + 11, y + 53) - 0.5f) * 0.030f;  // fine tooth
                float fleck = N(x * 5 + 3, y * 5 + 7) > 0.986f ? -0.09f : 0f;
                float v = mottle + grain + fleck;
                Color c = new Color(baseC.r + v, baseC.g + v * 0.97f, baseC.b + v * 0.90f) * shade;
                t.SetPixel(x, y, c);
            }
        }

        // Short fibres pressed into the sheet.
        for (int i = 0; i < 140; i++)
        {
            int x0 = (int)(N(i * 17 + 1, 5) * S);
            int y0 = (int)(N(i * 29 + 3, 11) * S);
            int len = 3 + (int)(N(i * 13 + 7, 23) * 8f);
            bool diag = N(i * 31 + 9, 41) > 0.72f;
            float tone = 0.03f + N(i * 3 + 2, 17) * 0.035f;
            for (int k = 0; k < len; k++)
            {
                int x = (x0 + k) & (S - 1);
                int y = (diag ? y0 + k : y0) & (S - 1);
                Color c = t.GetPixel(x, y);
                c.r -= tone; c.g -= tone * 0.92f; c.b -= tone * 0.80f;
                t.SetPixel(x, y, c);
            }
        }

        t.Apply();
        t.filterMode = FilterMode.Bilinear;
        return t;
    }

    /// <summary>
    /// Full-screen vertical scrim for the menus: nearly clear at the bottom
    /// (so the orbiting board shows through) and darker at the top (so the
    /// title and buttons stay readable). 1×N so the gradient is smooth.
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
            float a = Mathf.Lerp(0.22f, 0.62f, f);            // darker toward the top of the screen
            t.SetPixel(0, y, new Color(0.05f, 0.06f, 0.09f, a));
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
