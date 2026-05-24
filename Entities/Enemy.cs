using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;

namespace Game.Entities
{
    public abstract class Enemy : BaseEntity
    {
        private const int TileSize = 16;
        private const float ChasePathRefreshSeconds = 0.35f;
        private const float FailedPathRetrySeconds = 0.8f;
        private const float CollisionRepathSeconds = 0.25f;

        private readonly Random _random = new Random();
        private readonly Dictionary<Point, int> _visitedTiles = new Dictionary<Point, int>();
        private readonly List<Point> _path = new List<Point>();
        private readonly HashSet<Point> _blockedTiles = new HashSet<Point>();

        private Point? _lastKnownPlayerTile;
        private Point? _currentPathDestination;
        private Point? _failedDestination;
        private float _pathRefreshTimer;
        private float _failedPathRetryTimer;
        private float _collisionRepathTimer;
        private float _movingTimer;
        private float _restTimer;
        private bool _navigationGridReady;
        private int _minTileX;
        private int _minTileY;
        private int _maxTileX;
        private int _maxTileY;

        public int Direction { get; private set; } = 1;
        public string CurrentState { get; private set; } = "idle";
        public Collider Collider { get; private set; }
        public Animation.AnimationSet AnimationSet { get; private set; } = new Animation.AnimationSet();
        public Animation.SpriteAnimator Animator { get; private set; } = new Animation.SpriteAnimator();
        public Character TargetPlayer { get; set; }
        public Func<IEnumerable<Obstacle>> GetObstacles { get; set; }
        public float VisionRadius { get; set; } = 170f;
        public float ChaseSpeed { get; set; } = 95f;
        public float WanderSpeed { get; set; } = 55f;
        public float RestAfterSeconds { get; set; } = 10f;
        public float RestSeconds { get; set; } = 2f;
        public bool DrawVision { get; set; }
        public bool IsFrozen { get; set; }

        protected Enemy(float x, float y, float width, float height, float colliderWidth, float colliderHeight, float colliderOffsetX, float colliderOffsetY)
            : base(x, y, width, height)
        {
            Tag = "Enemy";
            Collider = new Collider(colliderWidth, colliderHeight, "enemy");
            Collider.Bounds = new RectangleF(colliderOffsetX, colliderOffsetY, colliderWidth, colliderHeight);
        }

        public void SetAnimations(Image idleImage, Image walkImage, float idleFrameDuration = 0.2f, float walkFrameDuration = 0.15f)
        {
            if (idleImage != null)
            {
                AnimationSet.AddAnimation("idle", CreateAnimation("idle", idleImage, idleFrameDuration, 10));
            }

            if (walkImage != null)
            {
                AnimationSet.AddAnimation("walk", CreateAnimation("walk", walkImage, walkFrameDuration, 10));
            }

            ChangeState("idle");
        }

        public void BuildNavigationGrid()
        {
            _blockedTiles.Clear();
            _navigationGridReady = true;

            var obstacles = GetObstacles?.Invoke()
                .Where(obstacle => obstacle.Collider != null && obstacle.Collider.Enabled)
                .ToList();

            if (obstacles == null || obstacles.Count == 0)
            {
                Point currentTile = WorldToTile(GetNavigationCenter());
                _minTileX = currentTile.X - 20;
                _maxTileX = currentTile.X + 20;
                _minTileY = currentTile.Y - 20;
                _maxTileY = currentTile.Y + 20;
                return;
            }

            var obstacleBoundsList = obstacles
                .Select(obstacle => obstacle.Collider.GetWorldBounds(obstacle.X, obstacle.Y))
                .ToList();

            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int maxX = int.MinValue;
            int maxY = int.MinValue;

            foreach (RectangleF bounds in obstacleBoundsList)
            {
                int left = (int)Math.Floor(bounds.Left / TileSize);
                int right = (int)Math.Floor((bounds.Right - 0.01f) / TileSize);
                int top = (int)Math.Floor(bounds.Top / TileSize);
                int bottom = (int)Math.Floor((bounds.Bottom - 0.01f) / TileSize);

                minX = Math.Min(minX, left);
                minY = Math.Min(minY, top);
                maxX = Math.Max(maxX, right);
                maxY = Math.Max(maxY, bottom);
            }

            _minTileX = minX - 2;
            _minTileY = minY - 2;
            _maxTileX = maxX + 2;
            _maxTileY = maxY + 2;

            for (int y = _minTileY; y <= _maxTileY; y++)
            {
                for (int x = _minTileX; x <= _maxTileX; x++)
                {
                    Point tile = new Point(x, y);
                    RectangleF footprint = GetFootprintAtTile(tile);
                    if (obstacleBoundsList.Any(bounds => footprint.IntersectsWith(bounds)))
                    {
                        _blockedTiles.Add(tile);
                    }
                }
            }
        }

        public override void Update(float deltaTime)
        {
            EnsureNavigationGrid();

            if (_failedPathRetryTimer > 0)
            {
                _failedPathRetryTimer -= deltaTime;
            }

            if (_collisionRepathTimer > 0)
            {
                _collisionRepathTimer -= deltaTime;
            }

            if (IsFrozen)
            {
                SetVelocity(0, 0);
                ChangeState("idle");
                Animator.Update(deltaTime);
                return;
            }

            UpdateBrain(deltaTime);
            base.Update(deltaTime);

            Point currentTile = WorldToTile(GetNavigationCenter());
            _visitedTiles[currentTile] = _visitedTiles.ContainsKey(currentTile) ? _visitedTiles[currentTile] + 1 : 1;

            ChangeState(Math.Abs(VelocityX) > 0.01f || Math.Abs(VelocityY) > 0.01f ? "walk" : "idle");
            Animator.Update(deltaTime);
        }

        public override void Render(Graphics graphics)
        {
            if (!IsVisible) return;

            if (Animator != null && Animator.GetCurrentFrame() != null)
            {
                Animator.Render(graphics, (int)X, (int)Y, Direction);
            }
            else
            {
                base.Render(graphics);
            }

            if (DrawVision)
            {
                DrawVisionDebug(graphics);
            }
        }

        public void StopMoving()
        {
            _path.Clear();
            _currentPathDestination = null;
            SetVelocity(0, 0);
            ChangeState("idle");
        }

        public void NotifyObstacleCollision(bool hitX, bool hitY)
        {
            if (hitX)
            {
                VelocityX = 0;
            }

            if (hitY)
            {
                VelocityY = 0;
            }

            bool canSlideAlongObstacle =
                (hitX && Math.Abs(VelocityY) > 0.01f) ||
                (hitY && Math.Abs(VelocityX) > 0.01f);

            if (!canSlideAlongObstacle)
            {
                _collisionRepathTimer = CollisionRepathSeconds;

                if (_path.Count > 0)
                {
                    _path.RemoveAt(0);
                }
            }
        }

        public void DrawColliderDebug(Graphics graphics)
        {
            Collider?.DrawDebug(graphics, X, Y);
        }

        protected void ChangeState(string newState)
        {
            if (CurrentState == newState) return;

            CurrentState = newState;
            if (AnimationSet.HasAnimation(newState))
            {
                Animator.PlayAnimation(AnimationSet.GetAnimation(newState));
            }
        }

        private void UpdateBrain(float deltaTime)
        {
            if (_restTimer > 0)
            {
                _restTimer -= deltaTime;
                SetVelocity(0, 0);
                return;
            }

            bool canSeePlayer = CanSeePlayer();
            float speed = canSeePlayer || _lastKnownPlayerTile.HasValue ? ChaseSpeed : WanderSpeed;

            if (_collisionRepathTimer > 0)
            {
                SetVelocity(0, 0);
                return;
            }

            if (canSeePlayer)
            {
                _lastKnownPlayerTile = WorldToTile(TargetPlayerCenter());
                _pathRefreshTimer -= deltaTime;

                if (_pathRefreshTimer <= 0 || _path.Count == 0)
                {
                    TryBuildPathTo(_lastKnownPlayerTile.Value);
                    _pathRefreshTimer = ChasePathRefreshSeconds;
                }
            }
            else if (_lastKnownPlayerTile.HasValue)
            {
                if (_path.Count == 0)
                {
                    TryBuildPathTo(_lastKnownPlayerTile.Value);
                }

                Point currentTile = WorldToTile(GetNavigationCenter());
                if (currentTile == _lastKnownPlayerTile.Value ||
                    (_currentPathDestination.HasValue && currentTile == _currentPathDestination.Value && _path.Count == 0))
                {
                    _lastKnownPlayerTile = null;
                    _currentPathDestination = null;
                    _path.Clear();
                }
            }
            else if (_path.Count == 0)
            {
                ChooseWanderPath();
            }

            FollowPath(speed);

            if (!_lastKnownPlayerTile.HasValue && !canSeePlayer && (Math.Abs(VelocityX) > 0.01f || Math.Abs(VelocityY) > 0.01f))
            {
                _movingTimer += deltaTime;
                if (_movingTimer >= RestAfterSeconds)
                {
                    _movingTimer = 0;
                    _restTimer = RestSeconds;
                    _path.Clear();
                    _currentPathDestination = null;
                    SetVelocity(0, 0);
                }
            }
        }

        private bool CanSeePlayer()
        {
            if (TargetPlayer == null || TargetPlayer.IsDead()) return false;

            PointF playerCenter = TargetPlayerCenter();
            PointF enemyCenter = GetNavigationCenter();
            float dx = playerCenter.X - enemyCenter.X;
            float dy = playerCenter.Y - enemyCenter.Y;
            return dx * dx + dy * dy <= VisionRadius * VisionRadius;
        }

        private void FollowPath(float speed)
        {
            if (_path.Count == 0)
            {
                SetVelocity(0, 0);
                return;
            }

            Point targetTile = _path[0];
            PointF target = TileCenter(targetTile);
            PointF center = GetNavigationCenter();
            float dx = target.X - center.X;
            float dy = target.Y - center.Y;
            float distance = (float)Math.Sqrt(dx * dx + dy * dy);

            if (distance < 2f)
            {
                _path.RemoveAt(0);
                SetVelocity(0, 0);
                return;
            }

            float vx = dx / distance * speed;
            float vy = dy / distance * speed;
            SetVelocity(vx, vy);

            if (Math.Abs(vx) > 0.01f)
            {
                Direction = vx < 0 ? -1 : 1;
            }
        }

        private void ChooseWanderPath()
        {
            Point start = WorldToTile(GetNavigationCenter());
            var candidates = GetWalkableNeighbors(start)
                .OrderBy(tile => _visitedTiles.ContainsKey(tile) ? _visitedTiles[tile] : 0)
                .ThenBy(tile => _random.Next())
                .Take(3)
                .ToList();

            if (candidates.Count == 0)
            {
                SetVelocity(0, 0);
                return;
            }

            Point destination = candidates[_random.Next(candidates.Count)];
            TryBuildPathTo(destination);
        }

        private bool TryBuildPathTo(Point requestedDestination)
        {
            if (_failedDestination.HasValue &&
                _failedDestination.Value == requestedDestination &&
                _failedPathRetryTimer > 0)
            {
                return false;
            }

            Point? destination = GetNearestWalkableTile(requestedDestination);
            if (!destination.HasValue)
            {
                MarkPathFailed(requestedDestination);
                return false;
            }

            bool builtPath = BuildPathTo(destination.Value);
            if (!builtPath)
            {
                MarkPathFailed(requestedDestination);
                return false;
            }

            _currentPathDestination = destination.Value;
            _failedDestination = null;
            _failedPathRetryTimer = 0;
            return true;
        }

        private bool BuildPathTo(Point destination)
        {
            Point start = WorldToTile(GetNavigationCenter());
            _path.Clear();
            _currentPathDestination = null;

            if (start == destination)
            {
                return true;
            }

            var openSet = new PathPriorityQueue();
            var cameFrom = new Dictionary<Point, Point>();
            var costSoFar = new Dictionary<Point, int> { [start] = 0 };
            var closed = new HashSet<Point>();

            openSet.Enqueue(start, Manhattan(start, destination));

            while (openSet.Count > 0)
            {
                Point current = openSet.Dequeue();
                if (closed.Contains(current))
                {
                    continue;
                }

                closed.Add(current);

                if (current == destination)
                {
                    break;
                }

                foreach (Point next in GetWalkableNeighbors(current))
                {
                    int newCost = costSoFar[current] + 1;
                    if (!costSoFar.ContainsKey(next) || newCost < costSoFar[next])
                    {
                        costSoFar[next] = newCost;
                        cameFrom[next] = current;
                        openSet.Enqueue(next, newCost + Manhattan(next, destination));
                    }
                }
            }

            if (!cameFrom.ContainsKey(destination))
            {
                return false;
            }

            var reversed = new List<Point>();
            Point step = destination;
            while (step != start)
            {
                reversed.Add(step);
                step = cameFrom[step];
            }

            reversed.Reverse();
            _path.AddRange(reversed);
            return _path.Count > 0;
        }

        private IEnumerable<Point> GetWalkableNeighbors(Point tile)
        {
            var candidates = new[]
            {
                new Point(tile.X + 1, tile.Y),
                new Point(tile.X - 1, tile.Y),
                new Point(tile.X, tile.Y + 1),
                new Point(tile.X, tile.Y - 1)
            };

            foreach (Point candidate in candidates)
            {
                if (IsWalkable(candidate))
                {
                    yield return candidate;
                }
            }
        }

        private Point? GetNearestWalkableTile(Point target)
        {
            if (IsWalkable(target))
            {
                return target;
            }

            const int searchRadius = 8;
            Point? best = null;
            int bestDistance = int.MaxValue;

            for (int radius = 1; radius <= searchRadius; radius++)
            {
                for (int y = target.Y - radius; y <= target.Y + radius; y++)
                {
                    for (int x = target.X - radius; x <= target.X + radius; x++)
                    {
                        if (Math.Abs(x - target.X) != radius && Math.Abs(y - target.Y) != radius)
                        {
                            continue;
                        }

                        Point candidate = new Point(x, y);
                        if (!IsWalkable(candidate))
                        {
                            continue;
                        }

                        int distance = Manhattan(candidate, target);
                        if (distance < bestDistance)
                        {
                            best = candidate;
                            bestDistance = distance;
                        }
                    }
                }

                if (best.HasValue)
                {
                    return best;
                }
            }

            return null;
        }

        private bool IsWalkable(Point tile)
        {
            return IsWithinSearchBounds(tile) && !_blockedTiles.Contains(tile);
        }

        private bool IsWithinSearchBounds(Point tile)
        {
            return tile.X >= _minTileX && tile.X <= _maxTileX && tile.Y >= _minTileY && tile.Y <= _maxTileY;
        }

        private void EnsureNavigationGrid()
        {
            if (!_navigationGridReady)
            {
                BuildNavigationGrid();
            }
        }

        private void MarkPathFailed(Point destination)
        {
            _path.Clear();
            _currentPathDestination = null;
            _failedDestination = destination;
            _failedPathRetryTimer = FailedPathRetrySeconds;
            SetVelocity(0, 0);
        }

        private Point WorldToTile(PointF position)
        {
            return new Point((int)Math.Floor(position.X / TileSize), (int)Math.Floor(position.Y / TileSize));
        }

        private PointF TileCenter(Point tile)
        {
            return new PointF(tile.X * TileSize + TileSize / 2f, tile.Y * TileSize + TileSize / 2f);
        }

        private RectangleF GetFootprintAtTile(Point tile)
        {
            PointF center = TileCenter(tile);
            return new RectangleF(
                center.X - Collider.Bounds.Width / 2f,
                center.Y - Collider.Bounds.Height / 2f,
                Collider.Bounds.Width,
                Collider.Bounds.Height);
        }

        private PointF GetNavigationCenter()
        {
            if (Collider == null)
            {
                return Center;
            }

            RectangleF bounds = Collider.GetWorldBounds(X, Y);
            return new PointF(bounds.Left + bounds.Width / 2f, bounds.Top + bounds.Height / 2f);
        }

        private PointF TargetPlayerCenter()
        {
            if (TargetPlayer.Collider != null)
            {
                RectangleF bounds = TargetPlayer.Collider.GetWorldBounds(TargetPlayer.X, TargetPlayer.Y);
                return new PointF(bounds.Left + bounds.Width / 2f, bounds.Top + bounds.Height / 2f);
            }

            return TargetPlayer.Center;
        }

        private int Manhattan(Point a, Point b)
        {
            return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
        }

        private Animation.Animation CreateAnimation(string animationName, Image image, float frameDuration, int spriteNumber)
        {
            var animation = new Animation.Animation(animationName, true);
            int frameWidth = image.Width / spriteNumber;
            int frameHeight = image.Height;

            for (int i = 0; i < spriteNumber; i++)
            {
                animation.AddFrame(new Animation.AnimationFrame(
                    image,
                    new Rectangle(i * frameWidth, 0, frameWidth, frameHeight),
                    frameDuration));
            }

            return animation;
        }

        private void DrawVisionDebug(Graphics graphics)
        {
            PointF center = GetNavigationCenter();
            RectangleF vision = new RectangleF(
                center.X - VisionRadius,
                center.Y - VisionRadius,
                VisionRadius * 2,
                VisionRadius * 2);

            using (Pen pen = new Pen(Color.FromArgb(160, 255, 90, 90), 1))
            using (Brush brush = new SolidBrush(Color.FromArgb(25, 255, 80, 80)))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.FillEllipse(brush, vision);
                graphics.DrawEllipse(pen, vision);
            }
        }

        private class PathPriorityQueue
        {
            private readonly List<PathNode> _nodes = new List<PathNode>();

            public int Count => _nodes.Count;

            public void Enqueue(Point tile, int priority)
            {
                _nodes.Add(new PathNode(tile, priority));
                BubbleUp(_nodes.Count - 1);
            }

            public Point Dequeue()
            {
                Point result = _nodes[0].Tile;
                int lastIndex = _nodes.Count - 1;
                _nodes[0] = _nodes[lastIndex];
                _nodes.RemoveAt(lastIndex);

                if (_nodes.Count > 0)
                {
                    BubbleDown(0);
                }

                return result;
            }

            private void BubbleUp(int index)
            {
                while (index > 0)
                {
                    int parent = (index - 1) / 2;
                    if (_nodes[parent].Priority <= _nodes[index].Priority)
                    {
                        break;
                    }

                    Swap(parent, index);
                    index = parent;
                }
            }

            private void BubbleDown(int index)
            {
                while (true)
                {
                    int left = index * 2 + 1;
                    int right = left + 1;
                    int smallest = index;

                    if (left < _nodes.Count && _nodes[left].Priority < _nodes[smallest].Priority)
                    {
                        smallest = left;
                    }

                    if (right < _nodes.Count && _nodes[right].Priority < _nodes[smallest].Priority)
                    {
                        smallest = right;
                    }

                    if (smallest == index)
                    {
                        break;
                    }

                    Swap(index, smallest);
                    index = smallest;
                }
            }

            private void Swap(int a, int b)
            {
                PathNode temp = _nodes[a];
                _nodes[a] = _nodes[b];
                _nodes[b] = temp;
            }

            private struct PathNode
            {
                public readonly Point Tile;
                public readonly int Priority;

                public PathNode(Point tile, int priority)
                {
                    Tile = tile;
                    Priority = priority;
                }
            }
        }
    }
}
