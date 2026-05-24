using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Entities
{
    /// <summary>
    /// 碰撞体 - 用于碰撞检测
    /// </summary>
    public class Collider
    {
        /// <summary>
        /// 碰撞体的边界矩形（相对于实体位置）
        /// </summary>
        public RectangleF Bounds { get; set; }

        /// <summary>
        /// 碰撞体是否启用
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// 碰撞体类型（用于区分不同类型的碰撞）
        /// </summary>
        public string ColliderType { get; set; }

        public float Width;
        public float Height;

        public Collider(float width, float height, string colliderType = "default")
        {
            Bounds = new RectangleF(0, 0, width, height);
            Enabled = true;
            ColliderType = colliderType;
            Width = width;
            Height = height;
        }

        public Collider(RectangleF bounds, string colliderType = "default")
        {
            Bounds = bounds;
            Enabled = true;
            ColliderType = colliderType;
        }

        /// <summary>
        /// 获取世界坐标下的碰撞体矩形
        /// </summary>
        public RectangleF GetWorldBounds(float entityX, float entityY)
        {
            return new RectangleF(
                entityX + Bounds.X,
                entityY + Bounds.Y,
                Bounds.Width,
                Bounds.Height
            );
        }

        /// <summary>
        /// 检查与另一个碰撞体是否相交
        /// </summary>
        public bool IsCollidingWith(Collider other, float thisX, float thisY, float otherX, float otherY)
        {
            if (!Enabled || !other.Enabled) return false;

            RectangleF thisBounds = GetWorldBounds(thisX, thisY);
            RectangleF otherBounds = other.GetWorldBounds(otherX, otherY);

            return thisBounds.IntersectsWith(otherBounds);
        }

        /// <summary>
        /// 绘制碰撞体（调试用）
        /// </summary>
        public void DrawDebug(Graphics graphics, float entityX, float entityY)
        {
            RectangleF worldBounds = GetWorldBounds(entityX, entityY);
            graphics.DrawRectangle(Pens.Lime, worldBounds.X, worldBounds.Y, worldBounds.Width, worldBounds.Height);
        }
    }
}
