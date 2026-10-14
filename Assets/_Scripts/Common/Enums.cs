[System.Flags]
public enum TileType
{
    None = 0,
    //Walkable = 1 << 0,
    //Goal = 1 << 1,
    //Portal = 1 << 2,
    Placeable = 1 << 3,
    NPCWalkable = 1 << 4,
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
    Pushable = 1 << 0,
    CanPossess = 1 << 1,
    Player = 1 << 2,
    //Slidable = 1 << 3,
    Rollable = 1 << 4,
    //Tiltable = 1 << 5,
    Jumpable = 1 << 6,
    Attracted= 1 << 7,
    AutoAttracted= 1 << 8,
    TrailBound = 1 << 9,
    TrailAsObstacle= 1 << 10,
    Goal=1 << 11,
    NPC= 1 << 12,
    TrailAsRoad= 1 << 13,

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
