using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Entities
{
    /// <summary>
    /// 障碍物 - 静态的不可移动的物体
    /// </summary>
    public class Obstacle : BaseEntity
    {
        /// <summary>
        /// 碰撞体
        /// </summary>
        public Collider Collider { get; private set; }

        /// <summary>
        /// 障碍物颜色
        /// </summary>
        public Color ObstacleColor { get; set; }

        /// <summary>
        /// 障碍物图片（可选）
        /// </summary>
        public Image ObstacleImage { get; set; }

        /// <summary>
        /// 从图集中截取的源矩形
        /// </summary>
        private Rectangle _sourceRect;

        public Obstacle(float x, float y, float width, float height, Color? color = null)
            : base(x, y, width, height)
        {
            // 创建碰撞体（覆盖整个障碍物）
            Collider = new Collider(width, height, "obstacle");
            
            // 设置颜色
            ObstacleColor = color ?? Color.SaddleBrown;
            ObstacleImage = null;
            _sourceRect = new Rectangle(0, 0, (int)width, (int)height);
            
            // 障碍物不动
            IsActive = false;
        }

        public Obstacle(float x, float y, float width, float height, Image image)
            : base(x, y, width, height)
        {
            // 创建碰撞体（覆盖整个障碍物）
            Collider = new Collider(width, height, "obstacle");
            
            // 使用图片
            ObstacleImage = image;
            ObstacleColor = Color.White;
            _sourceRect = new Rectangle(0, 0, image.Width, image.Height);
            
            // 障碍物不动
            IsActive = false;
        }

        /// <summary>
        /// 从图集中设置源矩形（直接指定坐标和大小）
        /// </summary>
        public void SetSourceRect(int x, int y, int width, int height)
        {
            _sourceRect = new Rectangle(x, y, width, height);
        }

        /// <summary>
        /// 从障碍物图集中截取指定位置的障碍物
        /// 例如：SetObstacleFromSpriteSheet(0, 0, 32, 32) 表示获取第 0 列第 0 行的障碍物（每个 32x32）
        /// </summary>
        public void SetObstacleFromSpriteSheet(int obstacleX, int obstacleY, int obstacleWidth, int obstacleHeight)
        {
            if (ObstacleImage == null) return;

            // 计算源矩形
            int x = obstacleX * obstacleWidth;
            int y = obstacleY * obstacleHeight;
            
            // 确保不超出图片范围
            if (x + obstacleWidth > ObstacleImage.Width)
                x = ObstacleImage.Width - obstacleWidth;
            if (y + obstacleHeight > ObstacleImage.Height)
                y = ObstacleImage.Height - obstacleHeight;

            _sourceRect = new Rectangle(x, y, obstacleWidth, obstacleHeight);
        }

        /// <summary>
        /// 获取当前的源矩形
        /// </summary>
        public Rectangle GetSourceRect()
        {
            return _sourceRect;
        }

        /// <summary>
        /// 渲染障碍物
        /// </summary>
        public override void Render(Graphics graphics)
        {
            if (!IsVisible) return;

            if (ObstacleImage != null)
            {
                // 使用源矩形渲染指定部分的图片
                graphics.DrawImage(
                    ObstacleImage,
                    new Rectangle((int)X, (int)Y, (int)Width, (int)Height),
                    _sourceRect,
                    GraphicsUnit.Pixel);
            }
            else
            {
                // 绘制矩形（纯色）
                Rectangle rect = new Rectangle((int)X, (int)Y, (int)Width, (int)Height);
                graphics.FillRectangle(new SolidBrush(ObstacleColor), rect);
                graphics.DrawRectangle(Pens.DarkRed, rect);
            }
        }

        /// <summary>
        /// 绘制碰撞体边框（调试用）
        /// </summary>
        public void DrawColliderDebug(Graphics graphics)
        {
            Collider?.DrawDebug(graphics, X, Y);
        }
    }
}
