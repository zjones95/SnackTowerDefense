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
    // Upbeat deep-house loop: Am7 - Fmaj7 - Dm7 - E7 (two bars each), 8 bars at
    // 122 BPM. Four-on-the-floor kick, backbeat claps with a flam, offbeat hats,
    // a rolling sub bass, warm pads and offbeat Rhodes stabs, plus a sparse
    // pentatonic lead and a filter riser that loops back to the top.

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

    // ---- deep house voices ----
    static float Note(float semisFromA2) { return 110f * Mathf.Pow(2f, semisFromA2 / 12f); }

    // Four-on-the-floor kick: sine with a fast pitch drop plus a click transient.
    static void Kick(float[] l, float[] r, float t0, float amp)
    {
        int start = (int)(t0 * SR);
        int len = (int)(0.30f * SR);
        float phase = 0f;
        for (int i = 0; i < len; i++)
        {
            float t = (float)i / SR;
            float f = 48f + 120f * Mathf.Exp(-t * 26f);
            phase += TAU * f / SR;
            float click = Noise() * Mathf.Exp(-t * 180f) * 0.30f;
            float val = (Mathf.Sin(phase) * Mathf.Exp(-t * 10f) + click) * amp;
            W(l, start + i, val);
            W(r, start + i, val);
        }
    }

    // High-passed noise hat (short = closed, longer = open).
    static void Hat(float[] l, float[] r, float t0, float amp, float decay, float pan)
    {
        int start = (int)(t0 * SR);
        int len = (int)(0.28f * SR);
        float gl = PanL(pan), gr = PanR(pan);
        float lp = 0f;
        for (int i = 0; i < len; i++)
        {
            float t = (float)i / SR;
            float x = Noise();
            lp += (x - lp) * 0.55f;
            float val = (x - lp) * Mathf.Exp(-t * decay) * amp;
            W(l, start + i, val * gl);
            W(r, start + i, val * gr);
        }
    }

    // Backbeat clap: band-ish noise burst (call twice for a flam).
    static void Clap(float[] l, float[] r, float t0, float amp)
    {
        int start = (int)(t0 * SR);
        int len = (int)(0.22f * SR);
        float lp = 0f;
        for (int i = 0; i < len; i++)
        {
            float t = (float)i / SR;
            float x = Noise();
            lp += (x - lp) * 0.40f;
            float val = (x - lp) * Mathf.Exp(-t * 18f) * amp;
            W(l, start + i, val);
            W(r, start + i, val);
        }
    }

    // Warm rolling sub: sine plus a lowpassed saw for body.
    static void Bass(float[] l, float[] r, float t0, float dur, float freq, float amp)
    {
        int start = (int)(t0 * SR);
        int len = (int)(dur * SR);
        float phase = 0f, lp = 0f;
        for (int i = 0; i < len; i++)
        {
            float t = (float)i / SR;
            float env = Mathf.Min(1f, t * 90f) * Mathf.Clamp01((dur - t) / 0.05f);
            phase += TAU * freq / SR;
            lp += (Saw(phase) - lp) * 0.16f;
            float val = (Mathf.Sin(phase) * 0.85f + lp * 0.22f) * env * amp;
            W(l, start + i, val);
            W(r, start + i, val);
        }
    }

    // Rhodes-ish chord stab: summed sines with a soft attack and a quick decay.
    static void ChordHit(float[] l, float[] r, float t0, float dur, float[] freqs, float amp, float pan)
    {
        int start = (int)(t0 * SR);
        int len = (int)(dur * SR);
        float gl = PanL(pan), gr = PanR(pan);
        float[] ph = new float[freqs.Length];
        for (int i = 0; i < len; i++)
        {
            float t = (float)i / SR;
            float env = Mathf.Min(1f, t * 70f) * Mathf.Exp(-t * 3.0f);
            float s = 0f;
            for (int v = 0; v < freqs.Length; v++)
            {
                ph[v] += TAU * freqs[v] / SR;
                s += Mathf.Sin(ph[v]) + 0.22f * Mathf.Sin(ph[v] * 2f);
            }
            s /= freqs.Length;
            float val = s * env * amp;
            W(l, start + i, val * gl);
            W(r, start + i, val * gr);
        }
    }

    public static AudioClip Music()
    {
        const float bpm = 122f;
        const int bars = 8;
        float beat = 60f / bpm;
        float bar = beat * 4f;
        float dur = bars * bar;          // ~15.7s loop

        int n = (int)(dur * SR);
        float[] l = new float[n];
        float[] r = new float[n];

        // i - VI - iv - V in A minor, two bars each (Am7 / Fmaj7 / Dm7 / E7)
        float[] roots = { 0f, -4f, 5f, 7f };                  // semitones from A2
        int[][] chords =
        {
            new[] { 0, 3, 7, 10 },   // Am7
            new[] { 0, 4, 7, 11 },   // Fmaj7
            new[] { 0, 3, 7, 10 },   // Dm7
            new[] { 0, 4, 7, 10 }    // E7
        };

        for (int b = 0; b < bars; b++)
        {
            int seg = b / 2;
            float root = roots[seg];
            int[] chord = chords[seg];
            float b0 = b * bar;

            // four-on-the-floor kick
            for (int k = 0; k < 4; k++) Kick(l, r, b0 + k * beat, 0.95f);

            // backbeat clap (2 and 4) with a flam
            Clap(l, r, b0 + beat, 0.30f);
            Clap(l, r, b0 + beat + 0.013f, 0.18f);
            Clap(l, r, b0 + 3f * beat, 0.30f);
            Clap(l, r, b0 + 3f * beat + 0.013f, 0.18f);

            // hats: open on the off-beats of 2 and 4, closed elsewhere
            for (int k = 0; k < 4; k++)
            {
                float off = b0 + (k + 0.5f) * beat;
                bool isOpen = (k == 1 || k == 3);
                Hat(l, r, off, isOpen ? 0.18f : 0.10f, isOpen ? 8f : 30f, (k % 2 == 0) ? -0.3f : 0.3f);
            }

            // rolling sub bass with an octave/fifth pickup at the end of the bar
            float br = Note(root - 12f);
            Bass(l, r, b0, beat * 0.85f, br, 0.55f);
            Bass(l, r, b0 + 1.5f * beat, beat * 0.4f, br, 0.35f);
            Bass(l, r, b0 + 2f * beat, beat * 0.85f, br, 0.55f);
            Bass(l, r, b0 + 2.5f * beat, beat * 0.4f, br, 0.35f);
            Bass(l, r, b0 + 3f * beat, beat * 0.45f, br * 1.5f, 0.32f);
            Bass(l, r, b0 + 3.5f * beat, beat * 0.45f, br, 0.40f);

            // warm pad, filter opening across the loop
            float open = Mathf.Lerp(0.10f, 0.30f, b / (float)(bars - 1));
            for (int c = 0; c < chord.Length; c++)
                Strings(l, r, b0, bar * 1.02f, Note(root + chord[c] + 12f), 0.05f, 0.6f, 0.6f, 0.006f, 4, 0f, open * 0.6f, open);

            // offbeat Rhodes stabs (from bar 3)
            if (b >= 2)
            {
                float[] cf = new float[chord.Length];
                for (int c = 0; c < chord.Length; c++) cf[c] = Note(root + chord[c] + 12f);
                ChordHit(l, r, b0 + 1.5f * beat, 0.55f, cf, 0.13f, -0.12f);
                ChordHit(l, r, b0 + 3.5f * beat, 0.55f, cf, 0.10f, 0.12f);
            }

            // sparse pentatonic lead, easing in from bar 5
            if (b >= 4)
            {
                int[] mel = { 12, 15, 17, 19, 17, 15, 12, 10 };
                for (int m = 0; m < mel.Length; m++)
                {
                    if (b == 4 && m < 4) continue;
                    float mf = Note(mel[(m + b) % mel.Length]);
                    Pluck(l, r, b0 + m * (bar / 8f), mf, 0.09f, 7f, (m % 2 == 0) ? -0.25f : 0.25f);
                }
            }

            // filter riser on the last bar, looping back into the top
            if (b == bars - 1)
                Riser(l, r, b0, bar, 0.26f);
        }

        Reverb(l, r, 0.28f, 0.70f);

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
