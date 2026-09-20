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
}
