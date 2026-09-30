using System;
using System.Collections.Generic;
using UnityEngine;

public static class TropicalMapCheck
{
    public static void Verify()
    {
        string[] layout = TropicalMap.Layout;
        Vector2Int[] route = TropicalMap.Route;
        if (layout.Length != 14 || layout[0].Length != 18) throw new Exception("Unexpected tropical map dimensions");
        if (layout[2][0] != 's' || layout[11][17] != 'e') throw new Exception("Spawn or exit moved");

        HashSet<Vector2Int> path = new HashSet<Vector2Int>();
        for (int i = 1; i < route.Length; i++)
        {
            Vector2Int a = route[i - 1], b = route[i];
            if (a.x != b.x && a.y != b.y) throw new Exception("Diagonal route segment");
            Vector2Int step = new Vector2Int(Math.Sign(b.x - a.x), Math.Sign(b.y - a.y));
            for (Vector2Int p = a; p != b; p += step) path.Add(p);
            path.Add(b);
        }
        for (int y = 0; y < layout.Length; y++)
        {
            if (layout[y].Length != 18) throw new Exception("Uneven row " + y);
            for (int x = 0; x < 18; x++)
            {
                char c = layout[y][x];
                bool onPath = path.Contains(new Vector2Int(x, y));
                if (onPath ? c != 'm' && c != 's' && c != 'e' : c != 't')
                    throw new Exception("Tile/route mismatch at " + x + "," + y);
            }
        }
        Debug.Log("TropicalMapCheck OK: 18x14, " + (252 - path.Count) + " buildable cells, all remaining cells on route");
    }
}
