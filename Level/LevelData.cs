using System.Collections.Generic;
using System.Drawing;

namespace move_game.Level
{
    internal class LevelData
    {
        public int LevelNumber { get; set; }
        public string Name { get; set; }
        public float PlayerStartX { get; set; }
        public float PlayerStartY { get; set; }
        public Point? EndTile { get; set; }
        public List<Point> StopTiles { get; set; } = new List<Point>();
        public List<Point> Obstacles { get; set; } = new List<Point>();
        public List<Point> EnemySpawns { get; set; } = new List<Point>();
        public List<Point> LightTiles { get; set; } = new List<Point>();
        public bool UseDefaultBounds { get; set; } = true;
    }
}
