using System.Collections.Generic;
using System.Drawing;

namespace move_game.Level
{
    internal static class LevelManager
    {
        private static int totalLevelNumber = 2;

        public static bool LevelExists(int level)
        {
            return level <= totalLevelNumber;
        }

        public static LevelData GetLevel(int levelNumber)
        {
            switch (levelNumber)
            {
                case 1:
                    return CreateLevelOne();

                case 2:
                    return CreateLevelTwo();

                default:
                    return CreateLevelOne();
            }
        }

        private static LevelData CreateLevelOne()
        {
            return new LevelData
            {
                LevelNumber = 1,
                Name = "Level 1",
                PlayerStartX = -7,
                PlayerStartY = -17,
                EndTile = new Point(9, 9),
                StopTiles = new List<Point>(),
                Obstacles = new List<Point>
                {
                    new Point(19, 1),
                    new Point(18, 16),
                    new Point(2, 15),
                    new Point(3, 2),
                    new Point(17, 3),
                    new Point(16, 14),
                    new Point(4, 13),
                    new Point(5, 4),
                    new Point(15, 5),
                    new Point(14, 12),
                    new Point(6, 11),
                    new Point(7, 6),
                    new Point(13, 7),
                    new Point(12, 10),
                    new Point(8, 9),
                },
                EnemySpawns = new List<Point>
                {
                    new Point(41, 20),
                },
                LightTiles = new List<Point>
                {
                    new Point(18, 1),
                    new Point(21, 8),
                    new Point(32, 20),
                    new Point(43, 8),
                }
            };
        }

        private static LevelData CreateLevelTwo()
        {
            return new LevelData
            {
                LevelNumber = 2,
                Name = "Level 2",
                PlayerStartX = -7,
                PlayerStartY = -17,
                EndTile = new Point(13, 11),
                StopTiles = new List<Point>
                {
                    new Point(16, 11),
                    new Point(5, 13),
                    new Point(2, 16),
                    new Point(2, 16),
                    new Point(20, 24),
                    new Point(28, 4),
                    new Point(28, 7),
                    new Point(42, 2),
                    new Point(44, 4),
                    new Point(42, 24),
                    new Point(6, 20),
                    new Point(9, 14),
                    new Point(12, 20),
                    new Point(14, 2),
                    new Point(17, 8),
                    new Point(26, 20),
                    new Point(27, 10),
                    new Point(30, 18),
                    new Point(33, 17),
                    new Point(40, 6),
                    new Point(40, 18),
                    new Point(43, 13),
                    new Point(6, 22),
                    new Point(8, 19),
                    new Point(14, 5),
                    new Point(18, 22),
                    new Point(31, 16),
                    new Point(37, 16),
                    new Point(39, 19),
                    new Point(31, 25),
                    new Point(41, 16),
                    new Point(43, 9),
                    new Point(41, 21),
                    new Point(21, 9),

                },
                Obstacles = new List<Point>
                {
                    new Point(6, 1),
                    new Point(1, 4),
                    new Point(5, 7),
                    new Point(16, 3),
                    new Point(13, 10),
                    new Point(12, 11),
                    new Point(16, 10),
                    new Point(20, 13),
                    new Point(15, 14),
                    new Point(7, 16),
                    new Point(24, 1),
                    new Point(15, 23),
                    new Point(19, 27),
                    new Point(32, 25),
                    new Point(44, 25),
                    new Point(1, 5),
                    new Point(6, 2),
                    new Point(8, 8),
                    new Point(44, 25),
                    new Point(17, 21),
                    new Point(23, 17),
                    new Point(24, 6),
                    new Point(26, 12),
                    new Point(26, 26),
                    new Point(30, 3),
                    new Point(28, 5),
                    new Point(43, 27),
                    new Point(1, 26),
                    new Point(9, 27),
                    new Point(34, 2),
                    new Point(6, 5),
                    new Point(18, 15),
                    new Point(38, 9),
                    new Point(39, 23),
                    new Point(40, 23)
                },
                EnemySpawns = new List<Point>
                {
                    new Point(35, 6),
                },
                LightTiles = new List<Point>
                {
                    new Point(8, 8),
                    new Point(21, 18),
                    new Point(35, 14),
                    new Point(43, 22),
                }
            };
        }
    }
}
