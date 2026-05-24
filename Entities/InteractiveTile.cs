 using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Entities
{
    /// <summary>
    /// 交互格子基类 - 所有特殊格子的基类（终点、歇脚点、传送门等）
    /// </summary>
    public abstract class InteractiveTile : BaseEntity
    {
        /// <summary>
        /// 格子颜色
        /// </summary>
        public Color TileColor { get; set; }

        /// <summary>
        /// 格子图片（可选）
        /// </summary>
        public Image TileImage { get; set; }

        /// <summary>
        /// 从图集中截取的源矩形
        /// </summary>
        protected Rectangle _sourceRect;

        /// <summary>
        /// 记录当前在这个格子上的角色（用于追踪状态）
        /// </summary>
        protected HashSet<Character> _charactersOnTile = new HashSet<Character>();

        public InteractiveTile(float x, float y, float width, float height)
            : base(x, y, width, height)
        {
            TileColor = Color.White;
            TileImage = null;
            _sourceRect = new Rectangle(0, 0, (int)width, (int)height);
            RenderLayer = 5;
            IsActive = false; // 交互格子不需要 Update
        }

        /// <summary>
        /// 检查角色是否已在这个格子上
        /// </summary>
        public bool IsCharacterOnTile(Character character)
        {
            return _charactersOnTile.Contains(character);
        }

        /// <summary>
        /// 标记角色进入格子
        /// </summary>
        public void AddCharacter(Character character)
        {
            _charactersOnTile.Add(character);
        }

        /// <summary>
        /// 标记角色离开格子
        /// </summary>
        public void RemoveCharacter(Character character)
        {
            _charactersOnTile.Remove(character);
        }

        /// <summary>
        /// 从图集中设置源矩形（直接指定坐标和大小）
        /// </summary>
        public void SetSourceRect(int x, int y, int width, int height)
        {
            _sourceRect = new Rectangle(x, y, width, height);
        }

        /// <summary>
        /// 从格子图集中截取指定位置的格子
        /// </summary>
        public void SetTileFromSpriteSheet(int tileX, int tileY, int tileWidth, int tileHeight)
        {
            if (TileImage == null) return;

            int x = tileX * tileWidth;
            int y = tileY * tileHeight;

            // 确保不超出图片范围
            if (x + tileWidth > TileImage.Width)
                x = TileImage.Width - tileWidth;
            if (y + tileHeight > TileImage.Height)
                y = TileImage.Height - tileHeight;

            _sourceRect = new Rectangle(x, y, tileWidth, tileHeight);
        }

        /// <summary>
        /// 获取格子的碰撞矩形
        /// </summary>
        public RectangleF GetBounds()
        {
            return new RectangleF(X, Y, Width, Height);
        }

        /// <summary>
        /// 获取格子中心位置
        /// </summary>
        public PointF GetCenter()
        {
            return Center;
        }

        /// <summary>
        /// 角色进入格子时触发（由子类实现）
        /// </summary>
        public virtual void OnEnter(Character character)
        {
        }

        /// <summary>
        /// 角色离开格子时触发（由子类实现）
        /// </summary>
        public virtual void OnExit(Character character)
        {
        }

        /// <summary>
        /// 渲染格子
        /// </summary>
        public override void Render(Graphics graphics)
        {
            if (!IsVisible) return;

            if (TileImage != null)
            {
                // 使用源矩形渲染指定部分的图片
                graphics.DrawImage(
                    TileImage,
                    new Rectangle((int)X, (int)Y, (int)Width, (int)Height),
                    _sourceRect,
                    GraphicsUnit.Pixel);
            }
            else
            {
                // 绘制矩形（纯色）
                Rectangle rect = new Rectangle((int)X, (int)Y, (int)Width, (int)Height);
                graphics.FillRectangle(new SolidBrush(TileColor), rect);
                graphics.DrawRectangle(Pens.DodgerBlue, rect);
            }
        }
    }
}
