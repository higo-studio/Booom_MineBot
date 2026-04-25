namespace Minebot.Common
{
    public static class GridDirections
    {
        public static readonly GridPosition[] Cardinal =
        {
            GridPosition.Up,
            GridPosition.Right,
            GridPosition.Down,
            GridPosition.Left
        };

        public static readonly GridPosition[] EightWay =
        {
            new GridPosition(-1, 1),
            GridPosition.Up,
            new GridPosition(1, 1),
            GridPosition.Left,
            GridPosition.Right,
            new GridPosition(-1, -1),
            GridPosition.Down,
            new GridPosition(1, -1)
        };
    }
}
