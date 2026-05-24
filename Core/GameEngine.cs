using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace Game.Core
{
    /// <summary>
    /// 游戏引擎主类 - 管理游戏的整体生命周期和核心系统
    /// </summary>
    internal class GameEngine
    {
        private const float CollisionSkin = 0.01f;
        private const float CollisionTolerance = 0.2f;

        private GameLoop _gameLoop;
        private List<Entities.BaseEntity> _entities = new List<Entities.BaseEntity>();
        private Rendering.Renderer _renderer;
        private Control _renderTarget; // 渲染目标（通常是 Panel 或 PictureBox）
        private bool _isInitialized = false;
        private bool _isRunning = false;

        public GameEngine(Control renderTarget, int width = 800, int height = 600)
        {
            _renderTarget = renderTarget;
            _renderTarget.Width = width;
            _renderTarget.Height = height;
            _renderTarget.BackColor = Color.Black;

            // 初始化游戏循环
            _gameLoop = new GameLoop(60);
            _gameLoop.OnUpdate += Update;

            // 初始化渲染器
            _renderer = new Rendering.Renderer(_renderTarget);

            _isInitialized = true;
        }

        /// <summary>
        /// 启动游戏引擎
        /// </summary>
        public void Start()
        {
            if (_isInitialized && !_isRunning)
            {
                _isRunning = true;
                _gameLoop.Start();
                
                // 设置绘画事件
                _renderTarget.Paint += RenderTarget_Paint;
                _renderTarget.Invalidate(); // 触发首次绘制
            }
        }

        /// <summary>
        /// 停止游戏引擎
        /// </summary>
        public void Stop()
        {
            if (_isRunning)
            {
                _isRunning = false;
                _gameLoop.Stop();
                _renderTarget.Paint -= RenderTarget_Paint;
            }
        }

        /// <summary>
        /// 游戏的 Update 逻辑
        /// </summary>
        private void Update(float deltaTime)
        {
            // 更新所有活跃的实体
            foreach (var entity in _entities.Where(e => e.IsActive))
            {
                entity.Update(deltaTime);
            }

            // 执行碰撞检测
            CheckCollisions();

            // 触发重绘
            _renderTarget.Invalidate();
        }

        /// <summary>
        /// 渲染目标的 Paint 事件处理
        /// </summary>
        private void RenderTarget_Paint(object sender, PaintEventArgs e)
        {
            if (_isRunning)
            {
                // 清空画布
                e.Graphics.Clear(Color.Black);

                // 渲染所有实体
                _renderer.RenderEntities(e.Graphics, _entities);

                // 调试：绘制碰撞体边框
                //DrawColliderDebug(e.Graphics);

                // 触发游戏循环的渲染事件
                _gameLoop.Render(e.Graphics);
            }
        }

        /// <summary>
        /// 调试：绘制所有碰撞体边框
        /// </summary>
        private void DrawColliderDebug(Graphics graphics)
        {
            // 绘制角色碰撞体
            var characters = _entities.OfType<Entities.Character>();
            foreach (var character in characters)
            {
                character.Collider?.DrawDebug(graphics, character.X, character.Y);
            }

            // 绘制障碍物碰撞体
            var obstacles = _entities.OfType<Entities.Obstacle>();
            foreach (var obstacle in obstacles)
            {
                obstacle.DrawColliderDebug(graphics);
            }

            var enemies = _entities.OfType<Entities.Enemy>();
            foreach (var enemy in enemies)
            {
                enemy.DrawColliderDebug(graphics);
            }
        }

        /// <summary>
        /// 添加实体到游戏世界
        /// </summary>
        public void AddEntity(Entities.BaseEntity entity)
        {
            _entities.Add(entity);
        }

        /// <summary>
        /// 移除实体
        /// </summary>
        public void RemoveEntity(Entities.BaseEntity entity)
        {
            _entities.Remove(entity);
        }

        /// <summary>
        /// 获取所有实体
        /// </summary>
        public List<Entities.BaseEntity> GetEntities()
        {
            return _entities;
        }

        /// <summary>
        /// 获取游戏循环引用
        /// </summary>
        public GameLoop GetGameLoop()
        {
            return _gameLoop;
        }

        /// <summary>
        /// 获取渲染器引用
        /// </summary>
        public Rendering.Renderer GetRenderer()
        {
            return _renderer;
        }

        /// <summary>
        /// 碰撞检测 - 碰撞时沿最小穿透轴推出角色，使其恰好贴着障碍物
        /// 交互格子检测 - 进入后触发 OnEnter，离开时触发 OnExit
        /// </summary>
        private void CheckCollisions()
        {
            var characters = _entities.OfType<Entities.Character>();
            var obstacles = _entities.OfType<Entities.Obstacle>().ToList();
            var interactiveTiles = _entities.OfType<Entities.InteractiveTile>().ToList();
            var enemies = _entities.OfType<Entities.Enemy>().ToList();

            bool playerDead = characters.Any(character => character.IsDead());
            if (playerDead)
            {
                foreach (var enemy in enemies)
                {
                    enemy.IsFrozen = true;
                }
            }

            foreach (var character in characters)
            {
                if (character.Collider == null) continue;

                // 第一步：检查与障碍物的碰撞（物理阻挡）
                foreach (var obstacle in obstacles)
                {
                    if (obstacle.Collider == null || !obstacle.Collider.Enabled) continue;

                    var collision = ResolveCollision(character, character.Collider, obstacle);
                    if (collision.Collided)
                    {
                        StopVelocityOnHitAxis(character, collision);
                    }
                }

                // 第二步：检查与交互格子的接触（触发 OnEnter/OnExit）
                foreach (var tile in interactiveTiles)
                {
                    RectangleF charBounds = character.Collider.GetWorldBounds(character.X, character.Y);
                    RectangleF tileBounds = tile.GetBounds();

                    bool isColliding = charBounds.IntersectsWith(tileBounds);

                    if (isColliding && !tile.IsCharacterOnTile(character))
                    {
                        // 角色第一次进入格子
                        tile.AddCharacter(character);
                        tile.OnEnter(character);
                    }
                    else if (!isColliding && tile.IsCharacterOnTile(character))
                    {
                        // 角色离开了格子
                        tile.RemoveCharacter(character);
                        tile.OnExit(character);
                    }
                }

                foreach (var enemy in enemies)
                {
                    if (enemy.Collider == null || !enemy.Collider.Enabled) continue;
                    if (enemy.IsFrozen) continue;

                    RectangleF charBounds = character.Collider.GetWorldBounds(character.X, character.Y);
                    RectangleF enemyBounds = enemy.Collider.GetWorldBounds(enemy.X, enemy.Y);

                    if (charBounds.IntersectsWith(enemyBounds))
                    {
                        character.Die();
                        foreach (var otherEnemy in enemies)
                        {
                            otherEnemy.IsFrozen = true;
                            otherEnemy.StopMoving();
                        }
                    }
                }
            }

            foreach (var enemy in enemies)
            {
                if (enemy.Collider == null) continue;

                foreach (var obstacle in obstacles)
                {
                    if (obstacle.Collider == null || !obstacle.Collider.Enabled) continue;

                    var collision = ResolveCollision(enemy, enemy.Collider, obstacle);
                    if (collision.Collided)
                    {
                        StopVelocityOnHitAxis(enemy, collision);
                        enemy.NotifyObstacleCollision(collision.HitX, collision.HitY);
                    }
                }
            }
        }

        private CollisionResult ResolveCollision(Entities.BaseEntity entity, Entities.Collider collider, Entities.Obstacle obstacle)
        {
            RectangleF entityBounds = collider.GetWorldBounds(entity.X, entity.Y);
            RectangleF obstacleBounds = obstacle.Collider.GetWorldBounds(obstacle.X, obstacle.Y);

            if (!entityBounds.IntersectsWith(obstacleBounds))
            {
                return CollisionResult.None;
            }

            RectangleF previousBounds = collider.GetWorldBounds(entity.PreviousX, entity.PreviousY);
            float moveX = entity.X - entity.PreviousX;
            float moveY = entity.Y - entity.PreviousY;
            bool hitX = false;
            bool hitY = false;

            if (moveX > 0 && previousBounds.Right <= obstacleBounds.Left + CollisionTolerance)
            {
                entity.X = obstacleBounds.Left - collider.Bounds.X - collider.Bounds.Width - CollisionSkin;
                hitX = true;
            }
            else if (moveX < 0 && previousBounds.Left >= obstacleBounds.Right - CollisionTolerance)
            {
                entity.X = obstacleBounds.Right - collider.Bounds.X + CollisionSkin;
                hitX = true;
            }

            entityBounds = collider.GetWorldBounds(entity.X, entity.Y);
            if (entityBounds.IntersectsWith(obstacleBounds))
            {
                if (moveY > 0 && previousBounds.Bottom <= obstacleBounds.Top + CollisionTolerance)
                {
                    entity.Y = obstacleBounds.Top - collider.Bounds.Y - collider.Bounds.Height - CollisionSkin;
                    hitY = true;
                }
                else if (moveY < 0 && previousBounds.Top >= obstacleBounds.Bottom - CollisionTolerance)
                {
                    entity.Y = obstacleBounds.Bottom - collider.Bounds.Y + CollisionSkin;
                    hitY = true;
                }
            }

            entityBounds = collider.GetWorldBounds(entity.X, entity.Y);
            if (entityBounds.IntersectsWith(obstacleBounds))
            {
                CollisionResult fallback = ResolveBySmallestOverlap(entity, collider, entityBounds, obstacleBounds);
                hitX = hitX || fallback.HitX;
                hitY = hitY || fallback.HitY;
            }

            return new CollisionResult(true, hitX, hitY);
        }

        private CollisionResult ResolveBySmallestOverlap(Entities.BaseEntity entity, Entities.Collider collider, RectangleF entityBounds, RectangleF obstacleBounds)
        {
            float overlapLeft = entityBounds.Right - obstacleBounds.Left;
            float overlapRight = obstacleBounds.Right - entityBounds.Left;
            float overlapTop = entityBounds.Bottom - obstacleBounds.Top;
            float overlapBottom = obstacleBounds.Bottom - entityBounds.Top;

            float minOverlapX = Math.Min(overlapLeft, overlapRight);
            float minOverlapY = Math.Min(overlapTop, overlapBottom);

            if (minOverlapX < minOverlapY)
            {
                entity.X += overlapLeft < overlapRight
                    ? -overlapLeft - CollisionSkin
                    : overlapRight + CollisionSkin;
                return new CollisionResult(true, true, false);
            }

            entity.Y += overlapTop < overlapBottom
                ? -overlapTop - CollisionSkin
                : overlapBottom + CollisionSkin;
            return new CollisionResult(true, false, true);
        }

        private void StopVelocityOnHitAxis(Entities.BaseEntity entity, CollisionResult collision)
        {
            if (collision.HitX)
            {
                entity.VelocityX = 0;
            }

            if (collision.HitY)
            {
                entity.VelocityY = 0;
            }
        }

        private struct CollisionResult
        {
            public static readonly CollisionResult None = new CollisionResult(false, false, false);

            public readonly bool Collided;
            public readonly bool HitX;
            public readonly bool HitY;

            public CollisionResult(bool collided, bool hitX, bool hitY)
            {
                Collided = collided;
                HitX = hitX;
                HitY = hitY;
            }
        }

        /// <summary>
        /// 销毁游戏引擎
        /// </summary>
        public void Dispose()
        {
            Stop();
            _gameLoop?.Dispose();
            _entities.Clear();
        }
    }
}

