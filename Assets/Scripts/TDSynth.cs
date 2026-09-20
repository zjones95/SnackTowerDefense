using UnityEngine;

// Procedurally generated SFX (no audio assets).
public static class TDSynth
{
    const int SR = 44100;
    static System.Random rng = new System.Random(4242);
    static float Noise() { return (float)(rng.NextDouble() * 2.0 - 1.0); }

    static AudioClip Make(string n, float[] d)
    {
        AudioClip c = AudioClip.Create(n, d.Length, 1, SR, false);
        c.SetData(d, 0);
        return c;
    }

    static void Tone(float[] d, float startSec, float durSec, float freq, float amp)
    {
        int start = (int)(startSec * SR);
        int len = (int)(durSec * SR);
        for (int i = 0; i < len && start + i < d.Length; i++)
        {
            float t = (float)i / SR;
            float env = Mathf.Min(1f, t * 80f) * Mathf.Exp(-t * 5f);
            d[start + i] += Mathf.Sin(2f * Mathf.PI * freq * t) * env * amp;
        }
    }

    // variant: 0 normal, 1 splash thump, 2 sniper crack, 3 slow pulse
    public static AudioClip Shot(int variant)
    {
        float dur, decay, tone;
        switch (variant)
        {
            case 1: dur = 0.30f; decay = 15f; tone = 200f; break;
            case 2: dur = 0.13f; decay = 45f; tone = 1200f; break;
            case 3: dur = 0.22f; decay = 18f; tone = 320f; break;
            default: dur = 0.16f; decay = 30f; tone = 620f; break;
        }
        int n = (int)(SR * dur);
        float[] d = new float[n];
        float ph = 0f, lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SR;
            float env = Mathf.Exp(-t * decay);
            float x = Noise();
            lp += (x - lp) * 0.5f;
            float f = tone * Mathf.Exp(-t * 12f) + 80f;
            ph += 2f * Mathf.PI * f / SR;
            d[i] = (lp * 0.6f + Mathf.Sin(ph) * 0.4f) * env * 0.8f;
        }
        return Make("shot" + variant, d);
    }

    public static AudioClip Death()
    {
        int n = (int)(SR * 0.40f);
        float[] d = new float[n];
        float ph = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SR;
            float f = Mathf.Lerp(520f, 120f, Mathf.Clamp01(t / 0.4f));
            ph += 2f * Mathf.PI * f / SR;
            d[i] = (Mathf.Sin(ph) * 0.6f + Noise() * 0.15f) * Mathf.Exp(-t * 6f) * 0.6f;
        }
        return Make("death", d);
    }

    public static AudioClip Leak()
    {
        int n = (int)(SR * 0.30f);
        float[] d = new float[n];
        float ph = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SR;
            float f = Mathf.Lerp(170f, 60f, Mathf.Clamp01(t / 0.3f));
            ph += 2f * Mathf.PI * f / SR;
            d[i] = (Mathf.Sin(ph) * 0.7f + Noise() * 0.2f) * Mathf.Exp(-t * 12f) * 0.8f;
        }
        return Make("leak", d);
    }

    public static AudioClip Build()
    {
        int n = (int)(SR * 0.4f);
        float[] d = new float[n];
        Tone(d, 0.00f, 0.12f, 660f, 0.35f);
        Tone(d, 0.10f, 0.22f, 990f, 0.35f);
        return Make("build", d);
    }

    public static AudioClip Merge()
    {
        int n = (int)(SR * 0.55f);
        float[] d = new float[n];
        Tone(d, 0.00f, 0.14f, 523f, 0.32f);
        Tone(d, 0.12f, 0.16f, 784f, 0.32f);
        Tone(d, 0.26f, 0.24f, 1047f, 0.32f);
        return Make("merge", d);
    }

    public static AudioClip RoundClear()
    {
        int n = (int)(SR * 1.1f);
        float[] d = new float[n];
        float[] fs = { 523f, 659f, 784f, 1047f };
        for (int i = 0; i < fs.Length; i++) Tone(d, 0.12f * i, 0.4f, fs[i], 0.3f);
        return Make("clear", d);
    }

    public static AudioClip Click()
    {
        int n = (int)(SR * 0.07f);
        float[] d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SR;
            d[i] = (Noise() * 0.35f + Mathf.Sin(2f * Mathf.PI * 800f * t) * 0.65f) * Mathf.Exp(-t * 70f) * 0.4f;
        }
        return Make("click", d);
    }
}
