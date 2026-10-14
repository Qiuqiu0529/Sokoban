using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct WallInfo
{
    public Vector2Int cellPos;
    public WallDirection direction;

    public override bool Equals(object obj)
    {
        if (obj is WallInfo other)
        {
            return cellPos.Equals(other.cellPos) && direction == other.direction;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return cellPos.GetHashCode() ^ direction.GetHashCode();
    }
}
