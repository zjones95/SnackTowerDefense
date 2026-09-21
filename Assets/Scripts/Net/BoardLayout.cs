using UnityEngine;

/// <summary>Where each player's board sits in the world.</summary>
public static class BoardLayout
{
    // Rooms extend ~30 x 32 units around the grid, so space them a bit wider.
    public const float SpaceX = 34f;
    public const float SpaceZ = 36f;

    public static int Columns(int count)
    {
        if (count <= 1) return 1;
        if (count <= 4) return count;
        if (count <= 6) return 3;
        return 4;
    }

    public static Vector3 Position(int slot, int count)
    {
        if (slot < 0) slot = 0;
        if (count < 1) count = 1;

        int cols = Columns(count);
        int rows = Mathf.CeilToInt(count / (float)cols);
        int r = slot / cols;
        int c = slot % cols;

        float x = (c - (cols - 1) * 0.5f) * SpaceX;
        float z = -(r - (rows - 1) * 0.5f) * SpaceZ;
        return new Vector3(x, 0f, z);
    }
}
