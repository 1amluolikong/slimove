using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Game.Entities;
using move_game.Level;
using move_game;

namespace Game
{
    public partial class MainGame : Form
    {
        private Form _parentForm; // 主窗口引用
        private Core.GameEngine _gameEngine; // 游戏引擎
        private Panel _gamePanel; // 游戏绘图面板
        private Entities.Character _playerCharacter; // 玩家角色引用
        private readonly List<Entities.LightTile> _lightTiles = new List<Entities.LightTile>();
        private Dictionary<Keys, bool> _keyStates = new Dictionary<Keys, bool>(); // 键盘状态
        private readonly List<Keys> _movementKeyOrder = new List<Keys>();
        private LevelData _levelData; // 当前关卡数据
        private int _currentLevelNumber; // 当前关卡编号
        private GameDifficulty _difficulty; // 当前难度
        private Timer _closeDelayTimer; // 通关后延迟关闭的计时器

        private bool _isGameCompleted = false; // 游戏是否已完成
        private bool _isLastLevel = false;
        private bool _isPlayerDeathSequenceStarted = false;
        private bool _isDeathMessagePending = false;
        private Timer _transitionTimer; // 关卡切换动画计时器
        private Timer _deathFallbackTimer;
        private TransitionState _transitionState = TransitionState.None;
        private float _transitionProgress = 0f; // 0 = 无黑幕，1 = 全黑
        private const float TransitionStep = 0.055f;
        private const float PlayerLightRadius = 150f;
        private const float PlayerFullBrightRadius = 58f;
        private const int MaxDarknessAlpha = 246;
        private const int TileSize = 16;
        private static readonly SpriteSheetRegion ObstacleTileRegion = new SpriteSheetRegion(6, 3, 16, 16);
        private static readonly SpriteSheetRegion StopTileRegion = new SpriteSheetRegion(3, 0, 16, 16);
        private static readonly SpriteSheetRegion EndTileRegion = new SpriteSheetRegion(3, 3, 16, 16);
        private static readonly SpriteSheetRegion LightTileRegion = new SpriteSheetRegion(2, 2, 16, 16);

        private enum TransitionState
        {
            None,
            Closing,
            WaitingForKey,
            Opening,
            ShowingMessage,
            ShowingDeathMessage,
        }

        private struct SpriteSheetRegion
        {
            public static readonly SpriteSheetRegion FullImage = new SpriteSheetRegion(0, 0, 0, 0, true);

            public readonly int Column;
            public readonly int Row;
            public readonly int Width;
            public readonly int Height;
            public readonly bool UseFullImage;

            public SpriteSheetRegion(int column, int row, int width, int height)
                : this(column, row, width, height, false)
            {
            }

            private SpriteSheetRegion(int column, int row, int width, int height, bool useFullImage)
            {
                Column = column;
                Row = row;
                Width = width;
                Height = height;
                UseFullImage = useFullImage;
            }
        }

        public MainGame(Form parentForm = null, int levelNumber = 1, GameDifficulty difficulty = GameDifficulty.Simple)
        {
            InitializeComponent();
            _parentForm = parentForm;
            _currentLevelNumber = levelNumber;
            _difficulty = difficulty;
            _levelData = LevelManager.GetLevel(levelNumber);

            // 初始化键盘状态字典
            _keyStates[Keys.W] = false;
            _keyStates[Keys.A] = false;
            _keyStates[Keys.S] = false;
            _keyStates[Keys.D] = false;

            // 初始化游戏面板
            InitializeGamePanel();

            // 绑定键盘事件
            this.KeyDown += MainGame_KeyDown;
            this.KeyUp += MainGame_KeyUp;
        }

        /// <summary>
        /// 初始化游戏面板
        /// </summary>
        private void InitializeGamePanel()
        {
            // 创建游戏渲染面板
            _gamePanel = new DoubleBufferedPanel();
            _gamePanel.Dock = DockStyle.Fill;
            _gamePanel.BackColor = Color.Black;
            this.Controls.Add(_gamePanel);

            // 创建游戏引擎
            _gameEngine = new Core.GameEngine(_gamePanel, this.ClientSize.Width, this.ClientSize.Height);

            // 创建角色和背景
            CreateCharacterAndBackground();

            // 根据关卡数据创建障碍物、停靠格和终点
            CreateLevelLayout();

            // 启动游戏引擎
            _gameEngine.Start();

            // 绑定 Paint 事件以绘制关卡切换黑幕，确保它覆盖在游戏画面之上
            _gamePanel.Paint += GamePanel_Paint;
        }

        /// <summary>
        /// 根据当前关卡数据创建关卡元素
        /// </summary>
        private void CreateLevelLayout()
        {
            try
            {
                Image tileImage = Core.AssetLoader.LoadImage("assets", "tilemaps.png");

                if (tileImage == null)
                {
                    MessageBox.Show("assets图片未找到: tilemaps.png");
                    return;
                }

                if (_levelData.Name != "")
                {
                    this.Text = _levelData.Name;
                }

                if (_levelData.UseDefaultBounds)
                {
                    CreateDefaultBounds(tileImage);
                }

                foreach (var obstacle in _levelData.Obstacles)
                {
                    AddObstacleTile(obstacle, tileImage);
                }

                foreach (var stopTile in _levelData.StopTiles)
                {
                    AddStopTile(stopTile, tileImage);
                }

                if (_levelData.EndTile.HasValue)
                {
                    AddEndTile(_levelData.EndTile.Value, tileImage);
                }

                ApplyDifficultyFeatures(tileImage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载关卡失败: {ex.Message}");
            }
        }

        private void ApplyDifficultyFeatures(Image tileImage)
        {
            if (_difficulty >= GameDifficulty.Normal)
            {
                ApplyNormalDifficultyFeatures();
            }

            if (_difficulty >= GameDifficulty.Hard)
            {
                ApplyHardDifficultyFeatures(tileImage);
            }
        }

        private void ApplyNormalDifficultyFeatures()
        {
            foreach (var enemySpawn in _levelData.EnemySpawns)
            {
                AddEnemy(enemySpawn);
            }
        }

        private void ApplyHardDifficultyFeatures(Image tileImage)
        {
            foreach (var lightTile in _levelData.LightTiles)
            {
                AddLightTile(lightTile, tileImage);
            }
        }

        private void AddLightTile(Point tilePosition, Image tileImage)
        {
            Image lightImage = Core.AssetLoader.LoadImage("assets", "Light.png") ?? tileImage;
            if (lightImage == null)
            {
                return;
            }

            SpriteSheetRegion region = lightImage == tileImage ? LightTileRegion : SpriteSheetRegion.FullImage;
            var lightTile = AddSpriteSheetEntity(
                tilePosition.X * TileSize,
                tilePosition.Y * TileSize,
                TileSize,
                TileSize,
                lightImage,
                region,
                (x, y, width, height, image) => new Entities.LightTile(x, y, width, height, image),
                (tile, spriteRegion) =>
                {
                    if (spriteRegion.UseFullImage)
                    {
                        tile.SetSourceRect(0, 0, lightImage.Width, lightImage.Height);
                    }
                    else
                    {
                        tile.SetTileFromSpriteSheet(spriteRegion.Column, spriteRegion.Row, spriteRegion.Width, spriteRegion.Height);
                    }
                });
            _lightTiles.Add(lightTile);
        }

        private struct LightSource
        {
            public readonly PointF Center;
            public readonly float FullBrightRadius;
            public readonly float LightRadius;

            public LightSource(PointF center, float fullBrightRadius, float lightRadius)
            {
                Center = center;
                FullBrightRadius = fullBrightRadius;
                LightRadius = lightRadius;
            }
        }

        private void AddEnemy(Point tilePosition)
        {
            Image idleImage = Core.AssetLoader.LoadImage("assets", "SkeletonIdle.png") ?? Core.AssetLoader.LoadImage("assets", "Idle.png");
            Image walkImage = Core.AssetLoader.LoadImage("assets", "SkeletonWalk.png") ?? Core.AssetLoader.LoadImage("assets", "Walk.png");

            var skeleton = new Entities.Skeleton(tilePosition.X * 16, tilePosition.Y * 16);
            skeleton.SetAnimations(idleImage, walkImage);
            skeleton.TargetPlayer = _playerCharacter;
            skeleton.GetObstacles = () => _gameEngine.GetEntities().OfType<Entities.Obstacle>();
            skeleton.BuildNavigationGrid();
            // 是否显示视野范围
            skeleton.DrawVision = false;
            _gameEngine.AddEntity(skeleton);
        }

        private void CreateDefaultBounds(Image tileImage)
        {
            for (int x = 0; x < 1500; x += 16)
            {
                AddObstacleTile(x, 0, tileImage);
            }

            for (int y = 16; y < 966; y += 16)
            {
                AddObstacleTile(0, y, tileImage);
            }

            for (int x = 0; x < 1500; x += 16)
            {
                AddObstacleTile(x, 464, tileImage);
            }

            for (int y = 16; y < 966; y += 16)
            {
                AddObstacleTile(736, y, tileImage);
            }
        }

        private void AddObstacleTile(Point tilePosition, Image tileImage)
        {
            AddObstacleTile(tilePosition.X * TileSize, tilePosition.Y * TileSize, tileImage);
        }

        private void AddObstacleTile(float x, float y, Image tileImage)
        {
            AddSpriteSheetEntity(
                x,
                y,
                TileSize,
                TileSize,
                tileImage,
                ObstacleTileRegion,
                (entityX, entityY, width, height, image) => new Entities.Obstacle(entityX, entityY, width, height, image),
                (obstacle, region) => obstacle.SetObstacleFromSpriteSheet(region.Column, region.Row, region.Width, region.Height));
        }

        private void AddStopTile(Point tilePosition, Image tileImage)
        {
            AddInteractiveTile(tilePosition, tileImage, StopTileRegion,
                (x, y, width, height, image) => new Entities.TriggerTile(x, y, width, height, image));
        }

        private void AddEndTile(Point tilePosition, Image tileImage)
        {
            var endTile = AddInteractiveTile(tilePosition, tileImage, EndTileRegion,
                (x, y, width, height, image) => new EndTile(x, y, width, height, image));
            endTile.OnGameCompleted += (s, a) =>
            {
                OnGameVictory();
            };
        }

        private TTile AddInteractiveTile<TTile>(
            Point tilePosition,
            Image image,
            SpriteSheetRegion region,
            Func<float, float, float, float, Image, TTile> createTile)
            where TTile : Entities.InteractiveTile
        {
            return AddSpriteSheetEntity(
                tilePosition.X * TileSize,
                tilePosition.Y * TileSize,
                TileSize,
                TileSize,
                image,
                region,
                createTile,
                (tile, spriteRegion) =>
                {
                    if (spriteRegion.UseFullImage)
                    {
                        tile.SetSourceRect(0, 0, image.Width, image.Height);
                    }
                    else
                    {
                        tile.SetTileFromSpriteSheet(spriteRegion.Column, spriteRegion.Row, spriteRegion.Width, spriteRegion.Height);
                    }
                });
        }

        private TEntity AddSpriteSheetEntity<TEntity>(
            float x,
            float y,
            float width,
            float height,
            Image image,
            SpriteSheetRegion region,
            Func<float, float, float, float, Image, TEntity> createEntity,
            Action<TEntity, SpriteSheetRegion> configureSource)
            where TEntity : Entities.BaseEntity
        {
            TEntity entity = createEntity(x, y, width, height, image);
            configureSource(entity, region);
            _gameEngine.AddEntity(entity);
            return entity;
        }

        /// <summary>
        /// 创建角色和背景（用于演示）
        /// </summary>
        private void CreateCharacterAndBackground()
        {
            try
            {
                CreateBackground();
                CreateCharacter();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"创建游戏场景失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建背景
        /// </summary>
        private void CreateBackground()
        {
            Image bgImage = Core.AssetLoader.LoadImage("assets", "tilemaps.png");

            if (bgImage != null)
            {
                var tileMap = new Entities.TileMap(bgImage, 16);
                tileMap.SetTileFromSpriteSheet(5, 1);
                _gameEngine.AddEntity(tileMap);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("背景图片未找到: tilemaps.png");
            }
        }

        /// <summary>
        /// 创建角色
        /// </summary>
        private void CreateCharacter()
        {
            Image idleImage = Core.AssetLoader.LoadImage("assets", "Idle.png");
            Image walkImage = Core.AssetLoader.LoadImage("assets", "Walk.png");
            Image deathImage = Core.AssetLoader.LoadImage("assets", "Death.png");

            if (idleImage == null)
            {
                System.Diagnostics.Debug.WriteLine("找不到 Idle 动画图片");
                return;
            }

            _playerCharacter = Entities.Character.CreatePlayer(_levelData.PlayerStartX, _levelData.PlayerStartY);
            _playerCharacter.Died += PlayerCharacter_Died;
            _playerCharacter.Animator.OnAnimationFinished += PlayerAnimator_OnAnimationFinished;
            _playerCharacter.SetAnimations(idleImage, walkImage, deathImage);
            _gameEngine.AddEntity(_playerCharacter);

            System.Diagnostics.Debug.WriteLine($"角色已创建: 帧大小 {_playerCharacter.Width}x{_playerCharacter.Height}");
        }

        private void PlayerAnimator_OnAnimationFinished(object sender, EventArgs e)
        {
            if (_playerCharacter == null ||
                !_playerCharacter.IsDead() ||
                _playerCharacter.CurrentState != "death")
            {
                return;
            }

            BeginPlayerDeathMessage();
        }

        private void PlayerCharacter_Died(object sender, EventArgs e)
        {
            if (_isPlayerDeathSequenceStarted || _transitionState != TransitionState.None)
            {
                return;
            }

            if (_playerCharacter != null && _playerCharacter.CurrentState == "death")
            {
                return;
            }

            StartDeathFallbackTimer();
        }

        private void BeginPlayerDeathMessage()
        {
            if (_isPlayerDeathSequenceStarted)
            {
                return;
            }

            _isPlayerDeathSequenceStarted = true;
            _isDeathMessagePending = true;
            _deathFallbackTimer?.Stop();
            _deathFallbackTimer?.Dispose();
            _deathFallbackTimer = null;
            StartClosingTransition();
        }

        private void StartDeathFallbackTimer()
        {
            _deathFallbackTimer?.Stop();
            _deathFallbackTimer?.Dispose();

            _deathFallbackTimer = new Timer();
            _deathFallbackTimer.Interval = 700;
            _deathFallbackTimer.Tick += (sender, e) =>
            {
                _deathFallbackTimer.Stop();
                _deathFallbackTimer.Dispose();
                _deathFallbackTimer = null;
                BeginPlayerDeathMessage();
            };
            _deathFallbackTimer.Start();
        }

        private void MainGame_FormClosed(object sender, FormClosedEventArgs e)
        {
            // 游戏窗口关闭时，重新显示主窗口
            _transitionTimer?.Stop();
            _transitionTimer?.Dispose();
            _closeDelayTimer?.Stop();
            _closeDelayTimer?.Dispose();
            _deathFallbackTimer?.Stop();
            _deathFallbackTimer?.Dispose();
            _gameEngine?.Dispose();

            if (_parentForm != null && !_parentForm.IsDisposed)
            {
                _parentForm.WindowState = FormWindowState.Normal;
                _parentForm.Show();
                _parentForm.BringToFront();
            }
        }

        /// <summary>
        /// 键盘按下事件
        /// </summary>
        private void MainGame_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;

            if (_transitionState == TransitionState.WaitingForKey)
            {
                StartNextLevelOpening();
                return;
            }

            if (_transitionState != TransitionState.None)
            {
                return;
            }

            // 记录按键状态
            if (_keyStates.ContainsKey(e.KeyCode))
            {
                bool wasPressed = _keyStates[e.KeyCode];
                _keyStates[e.KeyCode] = true;
                if (!wasPressed)
                {
                    _movementKeyOrder.Remove(e.KeyCode);
                    _movementKeyOrder.Add(e.KeyCode);
                }
            }

            // 更新角色速度
            UpdatePlayerMovement();
        }

        /// <summary>
        /// 键盘释放事件
        /// </summary>
        private void MainGame_KeyUp(object sender, KeyEventArgs e)
        {
            e.Handled = true;

            if (_transitionState != TransitionState.None)
            {
                return;
            }

            // 记录按键状态
            if (_keyStates.ContainsKey(e.KeyCode))
            {
                _keyStates[e.KeyCode] = false;
                _movementKeyOrder.Remove(e.KeyCode);
            }

            // 更新角色速度
            UpdatePlayerMovement();
        }

        /// <summary>
        /// 游戏胜利处理
        /// </summary>
        private void OnGameVictory()
        {
            if (_isGameCompleted || _transitionState != TransitionState.None)
            {
                return;
            }

            _isGameCompleted = true;

            // 判断是否为最后一关
            _isLastLevel = !LevelManager.LevelExists(_currentLevelNumber + 1);

            if (_playerCharacter != null)
            {
                _playerCharacter.SetVelocity(0, 0);
                _playerCharacter.ChangeState("idle");
            }

            SetEnemiesFrozen(true, true);
            StartClosingTransition();
        }

        private void StartClosingTransition()
        {
            _transitionProgress = 0f;
            _transitionState = TransitionState.Closing;
            StartTransitionTimer();
        }

        private void StartNextLevelOpening()
        {
            int nextLevelNumber = _currentLevelNumber + 1;
            LoadLevel(nextLevelNumber);
            SetEnemiesFrozen(true, true);

            _transitionProgress = 1f;
            _transitionState = TransitionState.Opening;
            StartTransitionTimer();
        }

        private void StartTransitionTimer()
        {
            if (_transitionTimer == null)
            {
                _transitionTimer = new Timer();
                _transitionTimer.Interval = 16;
                _transitionTimer.Tick += TransitionTimer_Tick;
            }

            _transitionTimer.Start();
            _gamePanel?.Invalidate();
        }

        private void StartCloseDelayTimer(int interval)
        {
            _closeDelayTimer?.Stop();
            _closeDelayTimer?.Dispose();

            _closeDelayTimer = new Timer();
            _closeDelayTimer.Interval = interval;
            _closeDelayTimer.Tick += (s, args) =>
            {
                _closeDelayTimer.Stop();
                _closeDelayTimer.Dispose();
                _closeDelayTimer = null;
                this.Close();
            };
            _closeDelayTimer.Start();
        }

        private void TransitionTimer_Tick(object sender, EventArgs e)
        {
            if (_transitionState == TransitionState.Closing)
            {
                _transitionProgress = Math.Min(1f, _transitionProgress + TransitionStep);

                if (_transitionProgress >= 1f)
                {
                    _transitionTimer.Stop();

                    if (_isDeathMessagePending)
                    {
                        _isDeathMessagePending = false;
                        _transitionState = TransitionState.ShowingDeathMessage;
                        StartCloseDelayTimer(3500);
                    }
                    else if (LevelManager.LevelExists(_currentLevelNumber + 1))
                    {
                        _transitionState = TransitionState.WaitingForKey;
                    }
                    else
                    {
                        // 没有下一关 → 显示通关消息
                        _transitionState = TransitionState.ShowingMessage;
                        StartCloseDelayTimer(2200);
                    }
                }
            }
            else if (_transitionState == TransitionState.Opening)
            {
                _transitionProgress = Math.Max(0f, _transitionProgress - TransitionStep);

                if (_transitionProgress <= 0f)
                {
                    _transitionTimer.Stop();
                    _transitionState = TransitionState.None;
                    _transitionProgress = 0f;
                    SetEnemiesFrozen(false, false);
                }
            }

            _gamePanel?.Invalidate();
        }

        private void LoadLevel(int levelNumber)
        {
            _currentLevelNumber = levelNumber;
            _levelData = LevelManager.GetLevel(levelNumber);
            _isGameCompleted = false;
            _isLastLevel = false;
            _isPlayerDeathSequenceStarted = false;
            _isDeathMessagePending = false;
            _lightTiles.Clear();

            foreach (var key in _keyStates.Keys.ToList())
            {
                _keyStates[key] = false;
            }
            _movementKeyOrder.Clear();

            _gameEngine.GetEntities().Clear();
            CreateCharacterAndBackground();
            CreateLevelLayout();
            
            
        }

        private void SetEnemiesFrozen(bool isFrozen, bool stopMoving)
        {
            foreach (var enemy in _gameEngine.GetEntities().OfType<Entities.Enemy>())
            {
                enemy.IsFrozen = isFrozen;
                if (stopMoving)
                {
                    enemy.StopMoving();
                }
            }
        }

        /// <summary>
        /// 游戏面板的 Paint 事件处理（绘制游戏胜利界面）
        /// </summary>
        private void GamePanel_Paint(object sender, PaintEventArgs e)
        {
            if (_difficulty >= GameDifficulty.Hard)
            {
                DrawHardModeLighting(e.Graphics);
            }

            if (_transitionState == TransitionState.None)
            {
                return;
            }

            // 如果是显示通关消息的状态，绘制半透明黑幕 + 文字
            if (_transitionState == TransitionState.ShowingMessage)
            {
                DrawStoryMessage(
                    e.Graphics,
                    "胜利",
                    "亲爱的勇士，多亏了你，\n史莱姆安全逃离了魔物的猎杀。",
                    Color.FromArgb(255, 235, 132));
                return;
            }

            if (_transitionState == TransitionState.ShowingDeathMessage)
            {
                DrawStoryMessage(
                    e.Graphics,
                    "旅程止步于此",
                    "很可惜，亲爱的勇士，\n你未能从魔物的魔爪中拯救史莱姆。",
                    Color.FromArgb(255, 116, 126));
                return; // 不再执行下面的普通黑幕绘制
            }

            DrawCurtain(e.Graphics);

            if (_transitionState == TransitionState.WaitingForKey)
            {
                DrawTransitionPrompt(e.Graphics);
            }
        }

        private void DrawCurtain(Graphics graphics)
        {
            int width = _gamePanel.Width;
            int height = _gamePanel.Height;

            float visibleWidth = width * (1f - _transitionProgress);
            float visibleHeight = height * (1f - _transitionProgress);
            float left = (width - visibleWidth) / 2f;
            float top = (height - visibleHeight) / 2f;
            float right = left + visibleWidth;
            float bottom = top + visibleHeight;

            Color curtainColor = _isLastLevel ? Color.FromArgb(200, 0, 0, 0) : Color.Black;

            using (Brush curtainBrush = new SolidBrush(curtainColor))
            {
                graphics.FillRectangle(curtainBrush, 0, 0, width, top);
                graphics.FillRectangle(curtainBrush, 0, bottom, width, height - bottom);
                graphics.FillRectangle(curtainBrush, 0, top, left, visibleHeight);
                graphics.FillRectangle(curtainBrush, right, top, width - right, visibleHeight);
            }
        }

        private void DrawTransitionPrompt(Graphics graphics)
        {
            string promptText = "按任意键进入下一关";

            using (Font promptFont = new Font("Microsoft YaHei", 26, FontStyle.Bold))
            using (Brush textBrush = new SolidBrush(Color.FromArgb(235, 235, 235)))
            {
                SizeF textSize = graphics.MeasureString(promptText, promptFont);
                float x = (_gamePanel.Width - textSize.Width) / 2f;
                float y = (_gamePanel.Height - textSize.Height) / 2f;
                graphics.DrawString(promptText, promptFont, textBrush, x, y);
            }
        }

        private void DrawHardModeLighting(Graphics graphics)
        {
            if (_playerCharacter == null)
            {
                return;
            }

            PointF playerCenter = _playerCharacter.Center;
            ActivateLightTiles();
            List<LightSource> lightSources = GetActiveLightSources(playerCenter);

            using (Bitmap darkness = CreateDarknessMask(lightSources, _gamePanel.Width, _gamePanel.Height))
            {
                graphics.DrawImageUnscaled(darkness, 0, 0);
            }

            DrawLightBloom(graphics, playerCenter, Color.FromArgb(90, 190, 255, 145), 42f);
            foreach (var lightTile in _lightTiles.Where(light => light.IsLit))
            {
                DrawLightBloom(graphics, lightTile.GetCenter(), Color.FromArgb(120, 255, 231, 96), 34f);
            }

            DrawDirectionGlints(graphics, playerCenter);
        }

        private void ActivateLightTiles()
        {
            if (_playerCharacter?.Collider == null)
            {
                return;
            }

            RectangleF playerBounds = _playerCharacter.Collider.GetWorldBounds(_playerCharacter.X, _playerCharacter.Y);
            foreach (var lightTile in _lightTiles)
            {
                if (!lightTile.IsLit && playerBounds.IntersectsWith(lightTile.GetBounds()))
                {
                    lightTile.TurnOn();
                }
            }
        }

        private List<LightSource> GetActiveLightSources(PointF playerCenter)
        {
            var lightSources = new List<LightSource>
            {
                new LightSource(playerCenter, PlayerFullBrightRadius, PlayerLightRadius)
            };

            lightSources.AddRange(_lightTiles
                .Where(lightTile => lightTile.IsLit)
                .Select(lightTile => new LightSource(lightTile.GetCenter(), lightTile.FullBrightRadius, lightTile.LightRadius)));

            return lightSources;
        }

        private Bitmap CreateDarknessMask(List<LightSource> lightSources, int width, int height)
        {
            Bitmap darkness = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
            Rectangle rect = new Rectangle(0, 0, width, height);
            BitmapData data = darkness.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);

            try
            {
                int byteCount = Math.Abs(data.Stride) * height;
                byte[] pixels = new byte[byteCount];

                for (int y = 0; y < height; y++)
                {
                    int row = y * data.Stride;
                    for (int x = 0; x < width; x++)
                    {
                        int alpha = CalculateDarknessAlpha(x, y, lightSources);
                        int index = row + x * 4;
                        pixels[index] = 0;
                        pixels[index + 1] = 0;
                        pixels[index + 2] = 0;
                        pixels[index + 3] = (byte)alpha;
                    }
                }

                Marshal.Copy(pixels, 0, data.Scan0, byteCount);
            }
            finally
            {
                darkness.UnlockBits(data);
            }

            return darkness;
        }

        private int CalculateDarknessAlpha(int x, int y, List<LightSource> lightSources)
        {
            int contributionCount = 0;
            int minAlpha = 100;

            foreach (var light in lightSources)
            {
                float dx = x - light.Center.X;
                float dy = y - light.Center.Y;
                float distanceSquared = dx * dx + dy * dy;
                float lightRadiusSquared = light.LightRadius * light.LightRadius;

                if (distanceSquared > lightRadiusSquared)
                {
                    continue;
                }

                float fullBrightRadiusSquared = light.FullBrightRadius * light.FullBrightRadius;
                if (distanceSquared <= fullBrightRadiusSquared)
                {
                    return 0;
                }

                float distance = (float)Math.Sqrt(distanceSquared);
                float t = (distance - light.FullBrightRadius) / (light.LightRadius - light.FullBrightRadius);
                t = Math.Max(0f, Math.Min(1f, t));
                float smooth = t * t * (3f - 2f * t);
                int alpha = (int)(MaxDarknessAlpha * smooth);

                contributionCount++;
                minAlpha = Math.Min(alpha, minAlpha);
            }

            if (contributionCount == 0)
            {
                return MaxDarknessAlpha;
            }

            return minAlpha;
        }

        private void DrawLightBloom(Graphics graphics, PointF center, Color color, float radius)
        {
            for (int i = 4; i >= 1; i--)
            {
                float currentRadius = radius * i / 4f;
                int alpha = color.A / (i + 1);
                using (Brush brush = new SolidBrush(Color.FromArgb(alpha, color.R, color.G, color.B)))
                {
                    graphics.FillEllipse(
                        brush,
                        center.X - currentRadius,
                        center.Y - currentRadius,
                        currentRadius * 2,
                        currentRadius * 2);
                }
            }
        }

        private void DrawDirectionGlints(Graphics graphics, PointF playerCenter)
        {
            double now = DateTime.Now.TimeOfDay.TotalSeconds;
            float enemyPulse = GetPulseAlpha(now, 1.45, 0.46, 0.0);
            float lightPulse = GetPulseAlpha(now, 1.65, 0.48, 0.72);

            if (enemyPulse > 0)
            {
                var enemy = GetNearestEntity<Entities.Enemy>(playerCenter);
                if (enemy != null)
                {
                    DrawDirectionGlint(
                        graphics,
                        playerCenter,
                        enemy.Center,
                        Color.FromArgb((int)(210 * enemyPulse), 150, 18, 32),
                        38f);
                }
            }

            if (lightPulse > 0)
            {
                var lightTile = GetNearestLightTile(playerCenter);
                if (lightTile != null)
                {
                    DrawDirectionGlint(
                        graphics,
                        playerCenter,
                        lightTile.GetCenter(),
                        Color.FromArgb((int)(220 * lightPulse), 255, 224, 91),
                        48f);
                }
            }
        }

        private float GetPulseAlpha(double time, double interval, double duration, double offset)
        {
            double local = (time + offset) % interval;
            if (local > duration)
            {
                return 0f;
            }

            double half = duration / 2.0;
            double alpha = local <= half ? local / half : (duration - local) / half;
            return (float)Math.Max(0, Math.Min(1, alpha));
        }

        private void DrawDirectionGlint(Graphics graphics, PointF origin, PointF target, Color color, float distance)
        {
            float dx = target.X - origin.X;
            float dy = target.Y - origin.Y;
            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            if (length < 0.01f)
            {
                return;
            }

            float ux = dx / length;
            float uy = dy / length;
            float px = -uy;
            float py = ux;
            PointF tip = new PointF(origin.X + ux * (distance + 14f), origin.Y + uy * (distance + 14f));
            PointF inner = new PointF(origin.X + ux * distance, origin.Y + uy * distance);
            PointF sideA = new PointF(inner.X + px * 8f, inner.Y + py * 8f);
            PointF sideB = new PointF(inner.X - px * 8f, inner.Y - py * 8f);

            using (var path = new System.Drawing.Drawing2D.GraphicsPath())
            using (Brush brush = new SolidBrush(color))
            using (Pen pen = new Pen(Color.FromArgb(Math.Min(255, color.A + 35), color.R, color.G, color.B), 2f))
            {
                path.AddBezier(sideA, tip, tip, sideB);
                path.AddLine(sideB, inner);
                path.AddLine(inner, sideA);
                graphics.FillPath(brush, path);
                graphics.DrawPath(pen, path);
            }
        }

        private T GetNearestEntity<T>(PointF from) where T : Entities.BaseEntity
        {
            return _gameEngine.GetEntities()
                .OfType<T>()
                .Where(entity => entity.IsVisible)
                .OrderBy(entity => DistanceSquared(from, entity.Center))
                .FirstOrDefault();
        }

        private Entities.LightTile GetNearestLightTile(PointF from)
        {
            var nearestUnlitLight = _lightTiles
                .Where(lightTile => !lightTile.IsLit)
                .OrderBy(lightTile => DistanceSquared(from, lightTile.GetCenter()))
                .FirstOrDefault();

            if (nearestUnlitLight != null)
            {
                return nearestUnlitLight;
            }

            return _lightTiles
                .OrderBy(lightTile => DistanceSquared(from, lightTile.GetCenter()))
                .FirstOrDefault();
        }

        private float DistanceSquared(PointF a, PointF b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            return dx * dx + dy * dy;
        }

        private void DrawStoryMessage(Graphics graphics, string title, string message, Color accentColor)
        {
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

            using (Brush overlayBrush = new SolidBrush(Color.FromArgb(225, 0, 0, 0)))
            {
                graphics.FillRectangle(overlayBrush, 0, 0, _gamePanel.Width, _gamePanel.Height);
            }

            int boxWidth = Math.Min(720, Math.Max(360, _gamePanel.Width - 160));
            int boxHeight = 210;
            Rectangle boxRect = new Rectangle(
                (_gamePanel.Width - boxWidth) / 2,
                (_gamePanel.Height - boxHeight) / 2,
                boxWidth,
                boxHeight);

            using (Brush panelBrush = new SolidBrush(Color.FromArgb(55, 18, 20, 26)))
            using (Pen borderPen = new Pen(Color.FromArgb(150, accentColor), 2))
            {
                graphics.FillRectangle(panelBrush, boxRect);
                graphics.DrawRectangle(borderPen, boxRect);
            }

            using (Font titleFont = new Font("Microsoft YaHei", 30, FontStyle.Bold))
            using (Font messageFont = new Font("Microsoft YaHei", 19, FontStyle.Bold))
            using (Brush titleBrush = new SolidBrush(accentColor))
            using (Brush messageBrush = new SolidBrush(Color.FromArgb(240, 245, 245, 245)))
            using (Brush shadowBrush = new SolidBrush(Color.FromArgb(170, 0, 0, 0)))
            using (var centerFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            })
            {
                Rectangle titleRect = new Rectangle(boxRect.Left + 24, boxRect.Top + 24, boxRect.Width - 48, 54);
                Rectangle messageRect = new Rectangle(boxRect.Left + 42, boxRect.Top + 92, boxRect.Width - 84, 88);
                Rectangle shadowTitle = new Rectangle(titleRect.X + 2, titleRect.Y + 2, titleRect.Width, titleRect.Height);
                Rectangle shadowMessage = new Rectangle(messageRect.X + 2, messageRect.Y + 2, messageRect.Width, messageRect.Height);

                graphics.DrawString(title, titleFont, shadowBrush, shadowTitle, centerFormat);
                graphics.DrawString(title, titleFont, titleBrush, titleRect, centerFormat);
                graphics.DrawString(message, messageFont, shadowBrush, shadowMessage, centerFormat);
                graphics.DrawString(message, messageFont, messageBrush, messageRect, centerFormat);
            }
        }

        /// <summary>
        /// 更新玩家移动
        /// </summary>
        private void UpdatePlayerMovement()
        {
            if (_playerCharacter == null || _isGameCompleted || _playerCharacter.IsDead()) return;

            // 只有当角色速度为零时，才能通过输入改变方向
            // 检查当前是否有速度
            bool hasVelocity = (_playerCharacter.VelocityX != 0) || (_playerCharacter.VelocityY != 0);

            // 如果有速度，不处理输入，直接返回
            if (hasVelocity)
            {
                return;
            }

            float velocityX = 0;
            float velocityY = 0;
            Keys? activeKey = GetActiveMovementKey();

            if (activeKey == Keys.W)
                velocityY -= _playerCharacter.MoveSpeed;
            else if (activeKey == Keys.S)
                velocityY += _playerCharacter.MoveSpeed;
            else if (activeKey == Keys.A)
                velocityX -= _playerCharacter.MoveSpeed;
            else if (activeKey == Keys.D)
                velocityX += _playerCharacter.MoveSpeed;

            TryMovePlayer(velocityX, velocityY);
        }

        private Keys? GetActiveMovementKey()
        {
            for (int i = _movementKeyOrder.Count - 1; i >= 0; i--)
            {
                Keys key = _movementKeyOrder[i];
                if (_keyStates.ContainsKey(key) && _keyStates[key])
                {
                    return key;
                }
            }

            return null;
        }

        private bool TryMovePlayer(float velocityX, float velocityY)
        {
            if (_playerCharacter == null || _isGameCompleted || _playerCharacter.IsDead())
            {
                return false;
            }

            bool hasVelocity = (_playerCharacter.VelocityX != 0) || (_playerCharacter.VelocityY != 0);
            if (hasVelocity)
            {
                return true;
            }

            if (velocityX < 0)
            {
                _playerCharacter.SetDirection(-1);
            }
            else if (velocityX > 0)
            {
                _playerCharacter.SetDirection(1);
            }

            _playerCharacter.SetVelocity(velocityX, velocityY);

            bool isMoving = (velocityX != 0) || (velocityY != 0);
            if (isMoving)
            {
                if (_playerCharacter.CurrentState != "walk" && _playerCharacter.AnimationSet.HasAnimation("walk"))
                {
                    _playerCharacter.ChangeState("walk");
                }
            }
            else if (_playerCharacter.CurrentState != "idle")
            {
                _playerCharacter.ChangeState("idle");
            }

            return true;
        }
    }
}
