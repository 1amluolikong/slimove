using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Game.Entities
{
    /// <summary>
    /// 基础实体类 - 所有游戏对象的基类
    /// </summary>
    public class BaseEntity
    {
        /// <summary>
        /// 实体在世界中的位置
        /// </summary>
        public float X { get; set; }
        public float Y { get; set; }

        /// <summary>
        /// 上一帧的位置（用于碰撞检测回退）
        /// </summary>
        public float PreviousX { get; private set; }
        public float PreviousY { get; private set; }

        /// <summary>
        /// 实体的宽度和高度
        /// </summary>
        public float Width { get; set; }
        public float Height { get; set; }

        /// <summary>
        /// 实体的中心
        /// </summary>
        public PointF Center
        {
            get { return new PointF(X + Width / 2f, Y + Height / 2f); }
        }

        /// <summary>
        /// 实体的速度
        /// </summary>
        public float VelocityX { get; set; }
        public float VelocityY { get; set; }

        /// <summary>
        /// 实体是否活跃（是否参与 Update 和 Render）
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// 实体是否可见（是否被渲染）
        /// </summary>
        public bool IsVisible { get; set; }

        /// <summary>
        /// 实体的旋转角度（弧度）
        /// </summary>
        public float Rotation { get; set; }

        /// <summary>
        /// 实体的透明度（0-1）
        /// </summary>
        public float Alpha { get; set; }

        /// <summary>
        /// 实体的标签，用于识别和分类
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        /// 渲染层级，数值越小越先绘制。同层内继续按 Y 轴深度排序。
        /// </summary>
        public int RenderLayer { get; set; }

        public BaseEntity(float x = 0, float y = 0, float width = 32, float height = 32)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            VelocityX = 0;
            VelocityY = 0;
            IsActive = true;
            IsVisible = true;
            Rotation = 0;
            Alpha = 1.0f;
            Tag = "";
            RenderLayer = 10;
        }

        /// <summary>
        /// 更新实体逻辑（每帧调用）
        /// </summary>
        public virtual void Update(float deltaTime)
        {
            // 记录上一帧的位置
            PreviousX = X;
            PreviousY = Y;

            // 应用速度
            X += VelocityX * deltaTime;
            Y += VelocityY * deltaTime;
        }

        /// <summary>
        /// 渲染实体（由渲染器调用）
        /// </summary>
        public virtual void Render(Graphics graphics)
        {
            if (!IsVisible) return;

            // 绘制一个简单的矩形作为占位符
            Rectangle rect = new Rectangle((int)X, (int)Y, (int)Width, (int)Height);
            graphics.FillRectangle(Brushes.White, rect);
            graphics.DrawRectangle(Pens.Yellow, rect);
        }

        /// <summary>
        /// 获取实体的边界矩形
        /// </summary>
        public RectangleF GetBounds()
        {
            return new RectangleF(X, Y, Width, Height);
        }

        /// <summary>
        /// 设置实体位置
        /// </summary>
        public void SetPosition(float x, float y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// 设置实体速度
        /// </summary>
        public void SetVelocity(float vx, float vy)
        {
            VelocityX = vx;
            VelocityY = vy;
        }

        /// <summary>
        /// 检查两个实体是否碰撞
        /// </summary>
        public bool IsCollidingWith(BaseEntity other)
        {
            return GetBounds().IntersectsWith(other.GetBounds());
        }
    }
}
