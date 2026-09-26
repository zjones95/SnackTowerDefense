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
}
