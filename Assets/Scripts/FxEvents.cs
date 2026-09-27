using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Transient cosmetic events (splash bursts, tracer bolts) queued so the
/// multiplayer snapshot can replay them on remote boards. Purely visual; the
/// gameplay effects (damage, status) are already applied by the owner and travel
/// via the normal snapshot. Events are best-effort: they ride the same
/// unreliable snapshot, so a dropped one just means one missed sparkle.
/// </summary>
public enum FxKind : byte { Splash = 0, Tracer = 1, Beam = 2 }

public struct FxEvent
{
    public FxKind Kind;
    public Vector3 From;   // splash position / tracer start
    public Vector3 To;     // tracer end (unused for splash)
    public float Radius;   // splash radius
    public Color A;        // splash colour / tracer glow
    public Color B;        // tracer core
}

public static class FxEvents
{
    const int MaxPending = 48;   // safety cap per snapshot interval
    static readonly List<FxEvent> pending = new List<FxEvent>();

    public static void Splash(Vector3 pos, float radius, Color colour)
    {
        if (pending.Count < MaxPending)
            pending.Add(new FxEvent { Kind = FxKind.Splash, From = pos, Radius = radius, A = colour });
    }

    public static void Tracer(Vector3 from, Vector3 to, Color glow, Color core)
    {
        if (pending.Count < MaxPending)
            pending.Add(new FxEvent { Kind = FxKind.Tracer, From = from, To = to, A = glow, B = core });
    }

    /// <summary>A solid beam (Fondue Fountain): drawn locally and mirrored.</summary>
    public static void Beam(Vector3 from, Vector3 to, Color colour)
    {
        DrawBeam(from, to, colour);
        if (pending.Count < MaxPending)
            pending.Add(new FxEvent { Kind = FxKind.Beam, From = from, To = to, A = colour });
    }

    /// <summary>Copies the queued events into <paramref name="into"/> and clears the queue.</summary>
    public static void Drain(List<FxEvent> into)
    {
        into.Clear();
        into.AddRange(pending);
        pending.Clear();
    }

    /// <summary>Replays a remote event locally WITHOUT re-queuing it.</summary>
    public static void Play(FxEvent e)
    {
        if (e.Kind == FxKind.Splash) SplashFX.PlayRemote(e.From, e.Radius, e.A);
        else if (e.Kind == FxKind.Tracer) DrawTracer(e.From, e.To, e.A, e.B);
        else DrawBeam(e.From, e.To, e.A);
    }

    /// <summary>Same look as Tower.Tracer: two flat, camera-facing ribbons.</summary>
    static void DrawTracer(Vector3 a, Vector3 b, Color glow, Color core)
    {
        Vector3 dir = b - a;
        float len = dir.magnitude;
        if (len < 0.01f) return;

        Vector3 mid = a + dir * 0.5f;
        Vector3 fwd = dir / len;
        Vector3 toCam = Camera.main != null ? Camera.main.transform.position - mid : Vector3.up;
        Vector3 right = Vector3.Cross(fwd, toCam);
        if (right.sqrMagnitude < 0.0001f) right = Vector3.Cross(fwd, Vector3.up);
        right.Normalize();
        Vector3 up = Vector3.Cross(right, fwd).normalized;
        Quaternion rot = Quaternion.LookRotation(fwd, up);

        Ribbon("BoltGlow", mid, rot, new Vector3(0.190f, 0.050f, len), glow);
        Ribbon("BoltCore", mid, rot, new Vector3(0.075f, 0.022f, len * 1.01f), core);
    }

    /// <summary>A solid brown beam (Fondue Fountain) — a thin stretched cylinder.</summary>
    static void DrawBeam(Vector3 a, Vector3 b, Color col)
    {
        Vector3 dir = b - a;
        float len = dir.magnitude;
        if (len < 0.01f) return;

        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Collider c = g.GetComponent<Collider>();
        if (c != null) Object.Destroy(c);
        g.name = "Beam";
        g.transform.position = a + dir * 0.5f;
        g.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
        g.transform.localScale = new Vector3(0.16f, len * 0.5f, 0.16f);   // Unity's cylinder is 2 units tall
        g.GetComponent<Renderer>().sharedMaterial = TDVisuals.Mat(col, 0f, 0.6f);
        Object.Destroy(g, 0.12f);
    }

    static void Ribbon(string name, Vector3 pos, Quaternion rot, Vector3 scale, Color col)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Collider c = g.GetComponent<Collider>();
        if (c != null) Object.Destroy(c);
        g.name = name;
        g.transform.position = pos;
        g.transform.rotation = rot;
        g.transform.localScale = scale;
        g.GetComponent<Renderer>().sharedMaterial = TDVisuals.Mat(col, 0f, 0.85f);
        Object.Destroy(g, 0.09f);
    }
}
