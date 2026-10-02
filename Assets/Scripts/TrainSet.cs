using System.Collections.Generic;
using UnityEngine;

// Wooden toy train circling the bedroom floor just outside the play mat.
// Pure decoration: a rounded-rectangle track loop plus a locomotive and two
// carts advanced by arc length, so spacing stays even through the corners.
// Built procedurally like the other room props; Blender is not required.
public class TrainSet : MonoBehaviour
{
    const float Speed = 1.6f;        // loop units per second
    const float RailTopY = 0.09f;    // train origin height (top of the rails)
    const float Gauge = 0.28f;       // half distance between the rails

    float hx, hz, cornerR, perimeter;
    float dist;
    Transform engine, carA, carB;
    readonly List<Transform> wheels = new List<Transform>();

    /// <summary>Builds the track and train under parent (room-local coords).</summary>
    public static TrainSet Build(Transform parent, TDMap map)
    {
        GameObject go = new GameObject("TrainSet");
        go.transform.SetParent(parent, false);
        TrainSet t = go.AddComponent<TrainSet>();
        t.hx = map.Width * map.Cell * 0.5f + 1f;
        t.hz = map.Height * map.Cell * 0.5f + 1f;
        t.cornerR = 2f;
        float straightX = 2f * (t.hx - t.cornerR);
        float straightZ = 2f * (t.hz - t.cornerR);
        t.perimeter = 2f * straightX + 2f * straightZ + 2f * Mathf.PI * t.cornerR;
        t.BuildTrack(go.transform);
        t.engine = t.BuildEngine(go.transform);
        t.carA = t.BuildCar(go.transform, new Color(0.30f, 0.55f, 0.90f), new Color(0.95f, 0.80f, 0.25f));
        t.carB = t.BuildCar(go.transform, new Color(0.35f, 0.75f, 0.40f), new Color(0.95f, 0.55f, 0.20f));
        return t;
    }

    void Update()
    {
        dist += Speed * Time.deltaTime;
        Place(engine, dist);
        Place(carA, dist - 1.8f);
        Place(carB, dist - 3.35f);
        float spin = Speed * Time.deltaTime / 0.22f;
        for (int i = 0; i < wheels.Count; i++)
            wheels[i].Rotate(0f, spin * Mathf.Rad2Deg, 0f, Space.Self);
    }

    void Place(Transform t, float d)
    {
        Vector3 tan;
        Vector3 p = CenterAt(d, out tan);
        t.position = transform.position + new Vector3(p.x, RailTopY, p.z);
        t.rotation = Quaternion.Euler(0f, Mathf.Atan2(tan.x, tan.z) * Mathf.Rad2Deg, 0f);
    }

    /// <summary>Loop length in world units (for checks and previews).</summary>
    public float LoopLength { get { return perimeter; } }

    /// <summary>Loop centreline point at arc distance d (for checks and previews).</summary>
    public Vector3 SampleLoop(float d)
    {
        Vector3 tan;
        return CenterAt(d, out tan);
    }

    // ---- loop geometry: rounded rectangle, counter-clockwise from north-west
    Vector3 CenterAt(float d, out Vector3 tangent)
    {
        d = d % perimeter;
        if (d < 0f) d += perimeter;
        Vector3 p = CenterAtRaw(d);
        Vector3 a = CenterAtRaw(d + 0.05f);
        Vector3 b = CenterAtRaw(d - 0.05f);
        tangent = new Vector3(a.x - b.x, 0f, a.z - b.z).normalized;
        return p;
    }

    Vector3 CenterAtRaw(float d)
    {
        d = d % perimeter;
        if (d < 0f) d += perimeter;
        float sx = 2f * (hx - cornerR);   // north/south straight length
        float sz = 2f * (hz - cornerR);   // east/west straight length
        float arc = Mathf.PI * cornerR / 2f;

        if (d < sx) return new Vector3(-(hx - cornerR) + d, 0f, hz);   // north, +X
        d -= sx;
        if (d < arc) return Corner(hx - cornerR, hz - cornerR, Mathf.PI / 2f - d / cornerR);
        d -= arc;
        if (d < sz) return new Vector3(hx, 0f, (hz - cornerR) - d);    // east, -Z
        d -= sz;
        if (d < arc) return Corner(hx - cornerR, -(hz - cornerR), -d / cornerR);
        d -= arc;
        if (d < sx) return new Vector3((hx - cornerR) - d, 0f, -hz);   // south, -X
        d -= sx;
        if (d < arc) return Corner(-(hx - cornerR), -(hz - cornerR), -Mathf.PI / 2f - d / cornerR);
        d -= arc;
        if (d < sz) return new Vector3(-hx, 0f, -(hz - cornerR) + d);  // west, +Z
        d -= sz;
        return Corner(-(hx - cornerR), hz - cornerR, Mathf.PI - d / cornerR);
    }

    Vector3 Corner(float cx, float cz, float theta)
    {
        return new Vector3(cx + cornerR * Mathf.Cos(theta), 0f, cz + cornerR * Mathf.Sin(theta));
    }

    // ---- track
    void BuildTrack(Transform root)
    {
        Material wood = TDVisuals.Mat(new Color(0.55f, 0.38f, 0.22f), 0f, 0.5f);
        Material steel = TDVisuals.Mat(new Color(0.70f, 0.72f, 0.76f), 0.8f, 0.5f);

        // sleepers around the whole loop
        int n = Mathf.FloorToInt(perimeter / 0.6f);
        for (int i = 0; i < n; i++)
        {
            Vector3 tan;
            Vector3 p = CenterAt(i * 0.6f, out tan);
            GameObject s = TDVisuals.Box(root, "Sleeper", new Vector3(p.x, -0.02f, p.z),
                new Vector3(0.85f, 0.06f, 0.22f), wood);
            s.transform.localRotation = Quaternion.Euler(0f, Mathf.Atan2(tan.x, tan.z) * Mathf.Rad2Deg, 0f);
        }

        // rails: single boxes on the straights, chords around the corners
        Rail(root, new Vector3(-(hx - cornerR), 0.05f, hz - Gauge), new Vector3(hx - cornerR, 0.05f, hz - Gauge), steel);
        Rail(root, new Vector3(-(hx - cornerR), 0.05f, hz + Gauge), new Vector3(hx - cornerR, 0.05f, hz + Gauge), steel);
        Rail(root, new Vector3(-(hx - cornerR), 0.05f, -hz - Gauge), new Vector3(hx - cornerR, 0.05f, -hz - Gauge), steel);
        Rail(root, new Vector3(-(hx - cornerR), 0.05f, -hz + Gauge), new Vector3(hx - cornerR, 0.05f, -hz + Gauge), steel);
        Rail(root, new Vector3(hx - Gauge, 0.05f, -(hz - cornerR)), new Vector3(hx - Gauge, 0.05f, hz - cornerR), steel);
        Rail(root, new Vector3(hx + Gauge, 0.05f, -(hz - cornerR)), new Vector3(hx + Gauge, 0.05f, hz - cornerR), steel);
        Rail(root, new Vector3(-hx - Gauge, 0.05f, -(hz - cornerR)), new Vector3(-hx - Gauge, 0.05f, hz - cornerR), steel);
        Rail(root, new Vector3(-hx + Gauge, 0.05f, -(hz - cornerR)), new Vector3(-hx + Gauge, 0.05f, hz - cornerR), steel);

        Vector2[] corners = {
            new Vector2(hx - cornerR, hz - cornerR), new Vector2(hx - cornerR, -(hz - cornerR)),
            new Vector2(-(hx - cornerR), -(hz - cornerR)), new Vector2(-(hx - cornerR), hz - cornerR)
        };
        float[] starts = { Mathf.PI / 2f, 0f, -Mathf.PI / 2f, Mathf.PI };
        for (int ci = 0; ci < 4; ci++)
        {
            const int k = 8;
            for (int r = -1; r <= 1; r += 2)
            {
                Vector3 prev = CornerPoint(corners[ci], starts[ci], cornerR + r * Gauge);
                for (int i = 1; i <= k; i++)
                {
                    // all four corners sweep theta negative (PI -> PI/2 included)
                    float th = starts[ci] - (Mathf.PI / 2f) * (i / (float)k);
                    Vector3 next = CornerPoint(corners[ci], th, cornerR + r * Gauge);
                    Rail(root, prev, next, steel);
                    prev = next;
                }
            }
        }
    }

    Vector3 CornerPoint(Vector2 center, float theta, float radius)
    {
        return new Vector3(center.x + radius * Mathf.Cos(theta), 0.05f, center.y + radius * Mathf.Sin(theta));
    }

    static void Rail(Transform root, Vector3 a, Vector3 b, Material mat)
    {
        Vector3 mid = (a + b) * 0.5f;
        Vector3 d = b - a;
        float len = Mathf.Max(0.05f, d.magnitude + 0.04f);
        GameObject g = TDVisuals.Box(root, "Rail", mid, new Vector3(0.09f, 0.08f, len), mat);
        g.transform.localRotation = Quaternion.Euler(0f, Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, 0f);
    }

    // ---- train (faces +Z, origin at rail top)
    Transform BuildEngine(Transform root)
    {
        GameObject e = new GameObject("Engine");
        e.transform.SetParent(root, false);
        Material red = TDVisuals.Mat(new Color(0.80f, 0.22f, 0.20f), 0.1f, 0.5f);
        Material green = TDVisuals.Mat(new Color(0.20f, 0.55f, 0.30f), 0.1f, 0.5f);
        Material black = TDVisuals.Mat(new Color(0.15f, 0.14f, 0.16f), 0.2f, 0.4f);
        Material brass = TDVisuals.Mat(new Color(0.95f, 0.75f, 0.30f), 0.8f, 0.4f);

        TDVisuals.Box(e.transform, "Frame", new Vector3(0f, 0.35f, 0f), new Vector3(0.7f, 0.18f, 1.7f), black);
        GameObject boiler = TDVisuals.Cyl(e.transform, "Boiler", new Vector3(0f, 0.75f, 0.2f), 0.30f, 1.1f, red);
        boiler.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        TDVisuals.Box(e.transform, "Cab", new Vector3(0f, 0.85f, -0.55f), new Vector3(0.7f, 0.7f, 0.5f), green);
        TDVisuals.Box(e.transform, "Roof", new Vector3(0f, 1.25f, -0.55f), new Vector3(0.8f, 0.1f, 0.62f), black);
        TDVisuals.Cyl(e.transform, "Chimney", new Vector3(0f, 1.10f, 0.60f), 0.12f, 0.35f, black);
        TDVisuals.Cyl(e.transform, "ChimneyCap", new Vector3(0f, 1.30f, 0.60f), 0.18f, 0.10f, brass);
        TDVisuals.Sphere(e.transform, "Dome", new Vector3(0f, 1.02f, 0.15f), 0.22f, brass);
        TDVisuals.Sphere(e.transform, "Lamp", new Vector3(0f, 0.95f, 0.82f), 0.14f,
            TDVisuals.Mat(new Color(1f, 0.90f, 0.55f), 0f, 0.6f));
        GameObject plow = TDVisuals.Box(e.transform, "Plow", new Vector3(0f, 0.22f, 0.98f), new Vector3(0.6f, 0.35f, 0.12f), red);
        plow.transform.localRotation = Quaternion.Euler(-40f, 0f, 0f);

        float[] wz = { -0.55f, 0f, 0.55f };
        for (int i = 0; i < wz.Length; i++)
        {
            Wheel(e.transform, -0.38f, wz[i], 0.22f, black);
            Wheel(e.transform, 0.38f, wz[i], 0.22f, black);
        }
        return e.transform;
    }

    Transform BuildCar(Transform root, Color paint, Color load)
    {
        GameObject c = new GameObject("Car");
        c.transform.SetParent(root, false);
        Material body = TDVisuals.Mat(paint, 0.1f, 0.5f);
        Material dark = TDVisuals.Mat(new Color(0.15f, 0.14f, 0.16f), 0.2f, 0.4f);
        Material candy = TDVisuals.Mat(load, 0f, 0.6f);

        TDVisuals.Box(c.transform, "Frame", new Vector3(0f, 0.32f, 0f), new Vector3(0.66f, 0.15f, 1.3f), dark);
        TDVisuals.Box(c.transform, "WallL", new Vector3(-0.29f, 0.55f, 0f), new Vector3(0.08f, 0.35f, 1.3f), body);
        TDVisuals.Box(c.transform, "WallR", new Vector3(0.29f, 0.55f, 0f), new Vector3(0.08f, 0.35f, 1.3f), body);
        TDVisuals.Box(c.transform, "WallF", new Vector3(0f, 0.55f, 0.61f), new Vector3(0.66f, 0.35f, 0.08f), body);
        TDVisuals.Box(c.transform, "WallB", new Vector3(0f, 0.55f, -0.61f), new Vector3(0.66f, 0.35f, 0.08f), body);
        TDVisuals.Sphere(c.transform, "Load0", new Vector3(0f, 0.62f, -0.35f), 0.42f, candy);
        TDVisuals.Sphere(c.transform, "Load1", new Vector3(0f, 0.62f, 0f), 0.42f, candy);
        TDVisuals.Sphere(c.transform, "Load2", new Vector3(0f, 0.62f, 0.35f), 0.42f, candy);

        float[] wz = { -0.40f, 0.40f };
        for (int i = 0; i < wz.Length; i++)
        {
            Wheel(c.transform, -0.36f, wz[i], 0.18f, dark);
            Wheel(c.transform, 0.36f, wz[i], 0.18f, dark);
        }
        return c.transform;
    }

    void Wheel(Transform parent, float x, float z, float radius, Material mat)
    {
        GameObject w = TDVisuals.Cyl(parent, "Wheel", new Vector3(x, radius, z), radius, 0.10f, mat);
        w.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);   // axle along X; spin on local Y
        wheels.Add(w.transform);
    }
}
