using UnityEngine;

/// <summary>Small world-space label that rises a little and fades out — used for
/// the "+$X" gold popups over Gold towers. Billboarded so it always faces the
/// camera (TextMesh reads from its -Z face).</summary>
public class FloatingText : MonoBehaviour
{
    public TextMesh Mesh;
    public float Lifetime = 1.1f;
    public float Rise = 0.9f;

    private Vector3 start;
    private Color baseColour;
    private float age;

    public static void Spawn(Vector3 worldPos, string text, Color colour, float size = .03f)
    {
        GameObject go = new GameObject("FloatingText");
        go.transform.position = worldPos;
        go.AddComponent<BillboardLabel>();
        TextMesh tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.characterSize = size;
        tm.fontSize = 100;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = colour;
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font != null)
        {
            tm.font = font;
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            if (mr != null) { mr.sharedMaterial = font.material; mr.sortingOrder = 2; }
        }

        FloatingText ft = go.AddComponent<FloatingText>();
        ft.Mesh = tm;
        ft.baseColour = colour;
        ft.start = worldPos;
    }

    void Update()
    {
        age += Time.deltaTime;
        float k = Mathf.Clamp01(age / Lifetime);
        transform.position = start + Vector3.up * (Rise * k);
        if (Mesh != null)
        {
            Color c = baseColour;
            c.a = 1f - k;
            Mesh.color = c;
        }
        if (age >= Lifetime) Destroy(gameObject);
    }
}
