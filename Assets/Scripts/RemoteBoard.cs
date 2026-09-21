using UnityEngine;

/// <summary>
/// An inert copy of another player's board: room + play-mat, plus a floating
/// nameplate. Phase 2 shows the board itself; live mobs/towers arrive with the
/// spectating work in Phase 3.
/// </summary>
public class RemoteBoard : MonoBehaviour
{
    public ulong ClientId;
    public string PlayerName = "Player";
    public Vector3 BoardOffset;

    private Transform nameplate;

    public static RemoteBoard Create(Transform parent, Vector3 offset, ulong clientId, string playerName)
    {
        GameObject go = new GameObject("RemoteBoard_" + playerName);
        go.transform.SetParent(parent, false);
        RemoteBoard rb = go.AddComponent<RemoteBoard>();
        rb.ClientId = clientId;
        rb.PlayerName = playerName;
        rb.BoardOffset = offset;

        TDMap map = TDBoardBuilder.CreateMap(TDGameManager.Layout, TDGameManager.Route, 2f, offset);
        TDBoardBuilder.BuildTiles(go.transform, map);
        TDBoardBuilder.BuildRoom(go.transform, map, offset);
        rb.BuildNameplate(offset);
        return rb;
    }

    void BuildNameplate(Vector3 offset)
    {
        GameObject plate = new GameObject("BoardName");
        plate.transform.SetParent(transform, false);
        plate.transform.position = offset + new Vector3(0f, 4.5f, 0f);
        nameplate = plate.transform;

        TextMesh tm = plate.AddComponent<TextMesh>();
        tm.text = PlayerName;
        tm.characterSize = 0.30f;
        tm.fontSize = 90;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = new Color(1f, 0.96f, 0.75f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font != null)
        {
            tm.font = font;
            MeshRenderer mr = plate.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = font.material;
        }
    }

    void Update()
    {
        if (nameplate != null && Camera.main != null)
            nameplate.rotation = Camera.main.transform.rotation; // TextMesh reads from its -Z face
    }
}
