using UnityEngine;

// Sanity-checks the generated music clip (length, peak, RMS, NaNs) without
// entering play mode. Run:
//   Unity -batchmode -projectPath . -executeMethod TDMusicCheck.Verify -quit
public static class TDMusicCheck
{
    public static void Verify()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        AudioClip c = TDSynth.Music();
        sw.Stop();
        if (c == null) { Debug.LogError("TDMusicCheck: clip was null"); return; }
        Debug.Log("TDMusicCheck: generation took " + sw.ElapsedMilliseconds + " ms");

        float[] data = new float[c.samples * c.channels];
        c.GetData(data, 0);

        float peak = 0f;
        double sum = 0.0;
        int bad = 0;
        for (int i = 0; i < data.Length; i++)
        {
            float v = data[i];
            if (float.IsNaN(v) || float.IsInfinity(v)) { bad++; continue; }
            peak = Mathf.Max(peak, Mathf.Abs(v));
            sum += (double)v * v;
        }
        double rms = System.Math.Sqrt(sum / Mathf.Max(1, data.Length));

        Debug.Log(string.Format(
            "TDMusicCheck: {0} len={1:F2}s ch={2} sr={3} peak={4:F3} rms={5:F3} bad={6}",
            c.name, c.length, c.channels, c.frequency, peak, rms, bad));
    }
}
