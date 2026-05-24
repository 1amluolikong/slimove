using System;
using System.Drawing;

namespace Game.Entities
{
    /// <summary>
    /// 角色类 - 继承 BaseEntity，添加动画和状态管理
    /// </summary>
    public class Character : BaseEntity
    {
        public event EventHandler Died;

        /// <summary>
        /// 角色的朝向（1为右，-1为左）
        /// </summary>
        public int Direction { get; set; }

        /// <summary>
        /// 角色的当前状态（例如 "idle", "walk", "jump"）
        /// </summary>
        public string CurrentState { get; set; }

        /// <summary>
        /// 角色的生命值
        /// </summary>
        public float Health { get; set; }

        /// <summary>
        /// 角色的最大生命值
        /// </summary>
        public float MaxHealth { get; set; }

        /// <summary>
        /// 角色的速度倍数
        /// </summary>
        public float SpeedMultiplier { get; set; }

        /// <summary>
        /// 角色移动速度（像素/秒）
        /// </summary>
        public float MoveSpeed { get; set; }

        /// <summary>
        /// 角色的动画集合
        /// </summary>
        public Animation.AnimationSet AnimationSet { get; set; }

        /// <summary>
        /// 角色的动画播放器
        /// </summary>
        public Animation.SpriteAnimator Animator { get; set; }

        /// <summary>
        /// 碰撞体
        /// </summary>
        public Collider Collider { get; private set; }

        public float ColliderOffsetX, ColliderOffsetY;
        public bool IsDying { get; private set; }

        public Character(float x = 0, float y = 0, float width = 64, float height = 64, float colliderWidth = 64, float colliderHeight = 64, float colliderOffsetX = 0, float colliderOffsetY = 0)
            : base(x, y, width, height)
        {
            Direction = 1; // 初始朝向向右
            CurrentState = "idle";
            Health = 100;
            MaxHealth = 100;
            SpeedMultiplier = 1.0f;
            MoveSpeed = 150.0f; // 150 像素/秒
            AnimationSet = new Animation.AnimationSet();
            Animator = new Animation.SpriteAnimator();
            ColliderOffsetX = colliderOffsetX;
            ColliderOffsetY = colliderOffsetY;
            IsDying = false;

            // 创建碰撞体（稍小于视觉范围，以便更自然的碰撞）
            // 支持自定义偏移，用于处理角色图片空白区域
            Collider = new Collider(colliderWidth, colliderHeight, "player");
            Collider.Bounds = new System.Drawing.RectangleF(colliderOffsetX, colliderOffsetY, colliderWidth, colliderHeight);
        }

        public static Character CreatePlayer(float x, float y)
        {
            var player = new Character(x, y, 64, 64, 15, 15, 24, 34);
            player.Tag = "Player";
            player.CurrentState = "idle";
            return player;
        }

        public void SetAnimations(Image idleImage, Image walkImage, Image deathImage)
        {
            if (idleImage == null)
            {
                return;
            }

            var idleAnimation = CreateAnimation("idle", idleImage, 0.2f, true, 4);
            AnimationSet.AddAnimation("idle", idleAnimation);

            if (walkImage != null)
            {
                var walkAnimation = CreateAnimation("walk", walkImage, 0.15f, true, 4);
                AnimationSet.AddAnimation("walk", walkAnimation);
            }

            if (deathImage != null)
            {
                var deathAnimation = CreateAnimation("death", deathImage, 0.18f, false, 5);
                AnimationSet.AddAnimation("death", deathAnimation);
            }

            Animator.PlayAnimation(idleAnimation);

            int frameWidth = idleImage.Width / 4;
            Width = frameWidth;
            Height = idleImage.Height;
        }

        /// <summary>
        /// 更新角色
        /// </summary>
        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            // 更新动画
            if (Animator != null)
            {
                Animator.Update(deltaTime);
            }
        }

        /// <summary>
        /// 渲染角色
        /// </summary>
        public override void Render(Graphics graphics)
        {
            if (!IsVisible) return;

            // 如果有动画播放器，使用它来渲染
            if (Animator != null && Animator.GetCurrentFrame() != null)
            {
                Animator.Render(graphics, (int)X, (int)Y, Direction);
            }
            else
            {
                // 否则绘制一个简单的矩形作为占位符
                base.Render(graphics);
            }
        }

        /// <summary>
        /// 改变角色状态
        /// </summary>
        public void ChangeState(string newState)
        {
            if (CurrentState != newState)
            {
                CurrentState = newState;
                
                // 播放对应状态的动画
                if (AnimationSet != null && AnimationSet.HasAnimation(newState))
                {
                    var animation = AnimationSet.GetAnimation(newState);
                    Animator.PlayAnimation(animation);
                }
            }
        }

        /// <summary>
        /// 受伤
        /// </summary>
        public void TakeDamage(float damage)
        {
            Health = Math.Max(0, Health - damage);
        }

        /// <summary>
        /// 治疗
        /// </summary>
        public void Heal(float amount)
        {
            Health = Math.Min(MaxHealth, Health + amount);
        }

        /// <summary>
        /// 检查角色是否死亡
        /// </summary>
        public bool IsDead()
        {
            return Health <= 0 || IsDying;
        }

        public void Die()
        {
            if (IsDying) return;

            IsDying = true;
            Health = 0;
            SetVelocity(0, 0);
            ChangeState(AnimationSet.HasAnimation("death") ? "death" : "idle");
            Died?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 设置角色朝向
        /// </summary>
        public void SetDirection(int direction)
        {
            Direction = Math.Sign(direction) != 0 ? Math.Sign(direction) : Direction;
        }

        private Animation.Animation CreateAnimation(string animationName, Image image, float frameDuration, bool isLooping, int spriteNumber)
        {
            var animation = new Animation.Animation(animationName, isLooping);

            int frameWidth = image.Width / spriteNumber;
            int frameHeight = image.Height;

            for (int i = 0; i < spriteNumber; i++)
            {
                Rectangle sourceRect = new Rectangle(i * frameWidth, 0, frameWidth, frameHeight);
                var frame = new Animation.AnimationFrame(image, sourceRect, frameDuration);
                animation.AddFrame(frame);
            }

            return animation;
        }
    }
}
