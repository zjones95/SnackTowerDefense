using UnityEngine;

// Procedurally generated SFX and music (no audio assets).
//
// Music is written as an exact-period loop: every voice (and the reverb) writes
// with wraparound, so the tail of one pass is the head of the next and the loop
// point is seamless.
public static class TDSynth
{
    const int SR = 44100;
    const float TAU = 2f * Mathf.PI;
    static System.Random rng = new System.Random(4242);
    static float Noise() { return (float)(rng.NextDouble() * 2.0 - 1.0); }

    static AudioClip Make(string n, float[] d)
    {
        AudioClip c = AudioClip.Create(n, d.Length, 1, SR, false);
        c.SetData(d, 0);
        return c;
    }

    static AudioClip MakeStereo(string n, float[] l, float[] r)
    {
        int len = l.Length;
        float[] inter = new float[len * 2];
        for (int i = 0; i < len; i++) { inter[i * 2] = l[i]; inter[i * 2 + 1] = r[i]; }
        AudioClip c = AudioClip.Create(n, len, 2, SR, false);
        c.SetData(inter, 0);
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
            d[start + i] += Mathf.Sin(TAU * freq * t) * env * amp;
        }
    }

    // ================================================================== SFX
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
            ph += TAU * f / SR;
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
            ph += TAU * f / SR;
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
            ph += TAU * f / SR;
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
            d[i] = (Noise() * 0.35f + Mathf.Sin(TAU * 800f * t) * 0.65f) * Mathf.Exp(-t * 70f) * 0.4f;
        }
        return Make("click", d);
    }

    // ================================================================ music
    // Cinematic loop: Dm - Bb - F - C (two bars each), 8 bars at 100 BPM.
    // Ostinato strings + sub drone throughout; pad enters at bar 3, taikos at
    // bar 5, riser through bar 8.

    static void W(float[] buf, int idx, float v)
    {
        int n = buf.Length;
        idx %= n; if (idx < 0) idx += n;
        buf[idx] += v;
    }

    static float Saw(float phase)
    {
        float p = phase / TAU;
        p -= Mathf.Floor(p);
        return p * 2f - 1f;
    }

    static float Env(float t, float dur, float attack, float release)
    {
        if (t < attack) return t / attack;
        if (t > dur - release) return Mathf.Max(0f, (dur - t) / release);
        return 1f;
    }

    static float PanL(float pan) { return Mathf.Sqrt(Mathf.Clamp01((1f - pan) * 0.5f)); }
    static float PanR(float pan) { return Mathf.Sqrt(Mathf.Clamp01((1f + pan) * 0.5f)); }

    // Detuned saw stack through a moving one-pole lowpass -- strings/brass.
    static void Strings(float[] l, float[] r, float t0, float dur, float freq, float amp,
                        float attack, float release, float detune, int voices,
                        float pan, float cutStart, float cutEnd)
    {
        int start = (int)(t0 * SR);
        int len = (int)(dur * SR);
        float gl = PanL(pan), gr = PanR(pan);
        float[] ph = new float[voices];
        for (int v = 0; v < voices; v++) ph[v] = (float)(rng.NextDouble() * TAU);
        float lp = 0f;
        for (int i = 0; i < len; i++)
        {
            float t = (float)i / SR;
            float env = Env(t, dur, attack, release);
            float cut = Mathf.Lerp(cutStart, cutEnd, Mathf.Clamp01(t / dur));
            float s = 0f;
            for (int v = 0; v < voices; v++)
            {
                float dv = 1f + (v - (voices - 1) * 0.5f) * detune;
                ph[v] += TAU * freq * dv / SR;
                s += Saw(ph[v]);
            }
            s /= voices;
            lp += (s - lp) * cut;
            float val = lp * env * amp;
            W(l, start + i, val * gl);
            W(r, start + i, val * gr);
        }
    }

    // Short staccato saw note -- the ostinato.
    static void Pluck(float[] l, float[] r, float t0, float freq, float amp, float decay, float pan)
    {
        int start = (int)(t0 * SR);
        int len = (int)(0.28f * SR);
        float gl = PanL(pan), gr = PanR(pan);
        float phase = (float)(rng.NextDouble() * TAU);
        float lp = 0f;
        for (int i = 0; i < len; i++)
        {
            float t = (float)i / SR;
            float env = Mathf.Min(1f, t * 400f) * Mathf.Exp(-t * decay);
            phase += TAU * freq / SR;
            lp += (Saw(phase) - lp) * 0.28f;
            float val = lp * env * amp;
            W(l, start + i, val * gl);
            W(r, start + i, val * gr);
        }
    }

    static void Sub(float[] l, float[] r, float t0, float dur, float freq, float amp)
    {
        int start = (int)(t0 * SR);
        int len = (int)(dur * SR);
        float phase = 0f;
        for (int i = 0; i < len; i++)
        {
            float t = (float)i / SR;
            float env = Env(t, dur, 0.35f, 0.6f);
            phase += TAU * freq / SR;
            float val = Mathf.Sin(phase) * env * amp;
            W(l, start + i, val);
            W(r, start + i, val);
        }
    }

    static void Taiko(float[] l, float[] r, float t0, float amp)
    {
        int start = (int)(t0 * SR);
        int len = (int)(0.45f * SR);
        float phase = 0f;
        for (int i = 0; i < len; i++)
        {
            float t = (float)i / SR;
            float f = Mathf.Lerp(105f, 42f, Mathf.Clamp01(t / 0.25f));
            phase += TAU * f / SR;
            float body = Mathf.Sin(phase) * Mathf.Exp(-t * 9f);
            float thump = Noise() * Mathf.Exp(-t * 60f) * 0.35f;
            float val = (body + thump) * amp;
            W(l, start + i, val);
            W(r, start + i, val);
        }
    }

    static void Riser(float[] l, float[] r, float t0, float dur, float amp)
    {
        int start = (int)(t0 * SR);
        int len = (int)(dur * SR);
        float lp = 0f, phase = 0f;
        for (int i = 0; i < len; i++)
        {
            float t = (float)i / SR;
            float k = Mathf.Clamp01(t / dur);
            float cut = Mathf.Lerp(0.01f, 0.35f, k * k);
            lp += (Noise() - lp) * cut;
            phase += TAU * Mathf.Lerp(220f, 900f, k) / SR;
            float val = (lp * 0.8f + Mathf.Sin(phase) * 0.25f) * (k * k) * amp;
            W(l, start + i, val * 0.8f);
            W(r, start + i, val);
        }
    }

    // Schroeder-ish reverb, applied to steady state so the loop stays seamless.
    static void Reverb(float[] l, float[] r, float mix, float fb)
    {
        int n = l.Length;
        int[] lenL = { 1557, 1877, 2203 };
        int[] lenR = { 1613, 1931, 2267 };
        float[][] bl = new float[lenL.Length][];
        float[][] br = new float[lenR.Length][];
        int[] pl = new int[lenL.Length];
        int[] pr = new int[lenR.Length];
        for (int c = 0; c < lenL.Length; c++) { bl[c] = new float[lenL[c]]; br[c] = new float[lenR[c]]; }

        float[] inL = (float[])l.Clone();
        float[] inR = (float[])r.Clone();
        float lpL = 0f, lpR = 0f;

        // two passes: the first warms the delay lines so the second is periodic
        for (int pass = 0; pass < 2; pass++)
        {
            for (int i = 0; i < n; i++)
            {
                float oL = 0f, oR = 0f;
                for (int c = 0; c < bl.Length; c++) { oL += bl[c][pl[c]]; oR += br[c][pr[c]]; }
                oL /= bl.Length; oR /= br.Length;

                lpL += (oL - lpL) * 0.35f;
                lpR += (oR - lpR) * 0.35f;

                for (int c = 0; c < bl.Length; c++)
                {
                    bl[c][pl[c]] = inL[i] + lpL * fb;
                    br[c][pr[c]] = inR[i] + lpR * fb;
                    pl[c] = (pl[c] + 1) % bl[c].Length;
                    pr[c] = (pr[c] + 1) % br[c].Length;
                }

                l[i] = inL[i] * (1f - mix) + lpL * mix;
                r[i] = inR[i] * (1f - mix) + lpR * mix;
            }
        }
    }

    public static AudioClip Music()
    {
        const float bpm = 100f;
        const int bars = 8;
        float beat = 60f / bpm;          // 0.6s
        float bar = beat * 4f;           // 2.4s
        float dur = bars * bar;          // 19.2s

        int n = (int)(dur * SR);
        float[] l = new float[n];
        float[] r = new float[n];

        // i - VI - III - VII, two bars each
        float[] roots = { 73.42f, 58.27f, 87.31f, 65.41f }; // D2, Bb1, F2, C2
        int[][] chords =
        {
            new[] { 0, 3, 7, 12 },   // Dm
            new[] { 0, 4, 7, 12 },   // Bb
            new[] { 0, 4, 7, 12 },   // F
            new[] { 0, 4, 7, 12 }    // C
        };
        int[] ostinato = { 0, 7, 12, 7 };  // 16th-note cell

        for (int b = 0; b < bars; b++)
        {
            int seg = b / 2;
            float root = roots[seg];
            int[] chord = chords[seg];
            float b0 = b * bar;

            // sub drone, one note per bar
            Sub(l, r, b0, bar * 0.98f, root * 0.5f, 0.30f);

            // ostinato: every 16th, denser as the loop builds
            int steps = 16;
            for (int s = 0; s < steps; s++)
            {
                if (b < 2 && s % 2 == 1) continue;           // sparse intro
                float t = b0 + s * (bar / steps);
                int semi = ostinato[s % ostinato.Length];
                float f = root * 4f * Mathf.Pow(2f, semi / 12f);
                float pan = (s % 2 == 0) ? -0.30f : 0.30f;
                Pluck(l, r, t, f, 0.16f, 12f, pan);
            }

            // pad: enters bar 3, filter opens across the loop
            if (b >= 2)
            {
                float open = Mathf.Lerp(0.06f, 0.30f, (b - 2) / 5f);
                for (int c = 0; c < chord.Length; c++)
                {
                    float f = root * 2f * Mathf.Pow(2f, chord[c] / 12f);
                    Strings(l, r, b0, bar * 1.02f, f, 0.09f, 0.9f, 0.8f, 0.004f, 5, 0f, open * 0.7f, open);
                }
            }

            // taikos: enter bar 5
            if (b >= 4)
            {
                Taiko(l, r, b0, 0.55f);
                Taiko(l, r, b0 + beat * 1.5f, 0.30f);
                Taiko(l, r, b0 + beat * 2f, 0.50f);
                if (b >= 6) Taiko(l, r, b0 + beat * 3.5f, 0.30f);
            }

            // riser through the final bar, back into the loop
            if (b == bars - 1)
                Riser(l, r, b0, bar, 0.30f);

            // a low hit on the downbeat of each 2-bar phrase for weight
            if (b % 2 == 0) Taiko(l, r, b0, 0.45f);
        }

        Reverb(l, r, 0.34f, 0.72f);

        // normalize, then micro-fade the loop point
        float peak = 0f;
        for (int i = 0; i < n; i++)
        {
            peak = Mathf.Max(peak, Mathf.Abs(l[i]));
            peak = Mathf.Max(peak, Mathf.Abs(r[i]));
        }
        float gain = peak > 0.0001f ? 0.92f / peak : 0f;
        for (int i = 0; i < n; i++) { l[i] *= gain; r[i] *= gain; }

        int fade = (int)(0.004f * SR);
        for (int i = 0; i < fade; i++)
        {
            float g = i / (float)fade;
            l[i] *= g; r[i] *= g;
            l[n - 1 - i] *= g; r[n - 1 - i] *= g;
        }

        return MakeStereo("music_loop", l, r);
    }
}
