using System.Collections.Generic;
using UnityEngine;

// Layout-driven grid map.
//   's' = spawn (green)
//   'e' = end   (red)
//   'm' = mob path (grey)
//   't' = buildable tower area (brown)
//   'x' = neutral void (grey, not buildable, not path)
// The mob route is an explicit ordered list of cells (the waypoints), so the
// path can wind however the designer wants.
public class TDMap
{
    public readonly int Width;
    public readonly int Height;
    public readonly float Cell;
    public readonly Vector3 Origin;

    private readonly char[,] grid;
    public Vector2Int Spawn;
    public Vector2Int End;
    public readonly List<Vector2Int> Route = new List<Vector2Int>();
    public readonly List<Vector3> Waypoints = new List<Vector3>();

    public TDMap(string[] layout, Vector2Int[] route, float cell, Vector3 origin)
    {
        Height = layout.Length;
        Width = layout[0].Length;
        Cell = cell;
        Origin = origin;
        grid = new char[Width, Height];

        for (int y = 0; y < Height; y++)
        {
            string row = layout[y];
            for (int x = 0; x < Width; x++)
            {
                char c = x < row.Length ? row[x] : 'x';
                grid[x, y] = c;
                if (c == 's') Spawn = new Vector2Int(x, y);
                else if (c == 'e') End = new Vector2Int(x, y);
            }
        }

        for (int i = 0; i < route.Length; i++)
        {
            Route.Add(route[i]);
            Waypoints.Add(CellCenter(route[i].x, route[i].y));
        }
    }

    public bool InBounds(int x, int y) { return x >= 0 && y >= 0 && x < Width && y < Height; }
    public int Idx(int x, int y) { return y * Width + x; }
    public char At(int x, int y) { return InBounds(x, y) ? grid[x, y] : 'x'; }
    public bool IsPath(int x, int y) { char c = At(x, y); return c == 'm' || c == 's' || c == 'e'; }
    public bool IsBuildable(int x, int y) { return At(x, y) == 't'; }

    // layout row 0 is the far side (+z), so it appears at the top on screen
    public Vector3 CellCenter(int x, int ly)
    {
        float z = (Height - 1 - ly + 0.5f) * Cell;
        return Origin + new Vector3((x + 0.5f) * Cell, 0f, z);
    }

    public void WorldToCell(Vector3 p, out int x, out int ly)
    {
        x = Mathf.FloorToInt((p.x - Origin.x) / Cell);
        int zRow = Mathf.FloorToInt((p.z - Origin.z) / Cell);
        ly = Height - 1 - zRow;
    }
}
