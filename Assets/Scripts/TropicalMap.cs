using UnityEngine;

/// <summary>Solo prototype for the future shared co-op map. Every in-bounds
/// cell is buildable unless it belongs to the mob route.</summary>
public static class TropicalMap
{
    // 18 x 14: a winding, asymmetric single route with buildable pockets on
    // both sides. No 'x' void cells: the whole rectangle can hold towers.
    public static readonly string[] Layout =
    {
        "tttttttttttttttttt",
        "tttttttttttttttttt",
        "smmmmttttttttttttt",
        "ttttmttttttmmmmmmt",
        "ttttmttttttmttttmt",
        "ttttmmmmmmmmttttmt",
        "ttttttttttttttttmt",
        "ttttttttttttttttmt",
        "ttttttttttttttttmt",
        "tttttttmmmmmmmmmmt",
        "tttttttmtttttttttt",
        "tttttttmmmmmmmmmme",
        "tttttttttttttttttt",
        "tttttttttttttttttt"
    };

    // Ordered waypoints; every straight run follows the 'm' cells above.
    public static readonly Vector2Int[] Route =
    {
        new Vector2Int(0, 2),
        new Vector2Int(4, 2),
        new Vector2Int(4, 5),
        new Vector2Int(11, 5),
        new Vector2Int(11, 3),
        new Vector2Int(16, 3),
        new Vector2Int(16, 9),
        new Vector2Int(7, 9),
        new Vector2Int(7, 11),
        new Vector2Int(17, 11)
    };
}
