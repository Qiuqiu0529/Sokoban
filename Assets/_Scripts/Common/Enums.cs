[System.Flags]
public enum TileType
{
    None = 0,
    Walkable = 1 << 0,
    Placeable = 1 << 3,
    GhostWalkable = 1 << 4,
}

public enum ObjectDirection
{
    North,
    East,
    South,
    West
}
public enum WallDirection
{
    Up,
    Down,
    Left,
    Right
}

[System.Flags]
public enum ObjectType
{
    None = 0,
    // Keep established bit positions used by serialized ObjData assets.
    // Retired bits remain unused so existing data keeps its meaning.
    Pushable = 1 << 0,                 // Pomme can push this object.
    Player = 1 << 2,                   // Pomme, the player-controlled actor.
    Rollable = 1 << 4,                 // Object has rolling movement behavior.
    Goal = 1 << 11,                    // Non-blocking grave/goal marker.
    Ghost = 1 << 12,                   // Bark-guided ghost.
    BlocksGhost = 1 << 14,             // Stops a ghost's slide.
    BlocksMovement = 1 << 15,          // Solid obstacle such as a large stone.
    LightSource = 1 << 16,             // Soul lantern that changes ghost access.
    PressurePlate = 1 << 17,           // Non-blocking floor mechanism.
    CanActivatePressurePlate = 1 << 18,// Object can press a pressure plate.
    PressureControlled = 1 << 19,      // Disappears while its linked plate is active.
}

public enum GameplayState
{
    Playing,
    Paused,
    Menu
}

public enum PairColor
{
    Orange,
    Blue,
    Green,
}
