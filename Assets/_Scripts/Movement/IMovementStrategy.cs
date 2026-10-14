using UnityEngine;
using Commands;

namespace Movement
{
    public interface IMovementStrategy
    {
        ICommand CreateMoveCommand( Vector2Int moveDir, MapPlane map,  bool player);
        bool CanMove(Vector2Int from, Vector2Int moveDir, int height, MapPlane map, bool player);
        Vector2Int CalculateTargetPosition(Vector2Int currentPos, Vector2Int moveDir, int height, MapPlane map, bool player);
    }
} 