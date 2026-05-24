using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Entities
{
    /// <summary>
    /// 背景瓦片 - 用于渲染游戏背景
    /// 支持从雪碧图中截取指定瓦片
    /// </summary>
    public class TileMap : BaseEntity
    {
        private Image _tileImage; // 瓦片图集
        private int _tileSize = 32; // 单个瓦片大小（像素）
        private Rectangle _sourceTile; // 从图集中截取的源矩形

        /// <summary>
        /// 构造函数 - 创建背景，使用整个图片平铺
        /// </summary>
        public TileMap(Image tileImage, int tileSize = 32)
        {
            _tileImage = tileImage;
            _tileSize = tileSize;
            _sourceTile = new Rectangle(0, 0, tileSize, tileSize);
            RenderLayer = 0;
            IsActive = false; // 背景不需要 Update
        }

        /// <summary>
        /// 从雪碧图中截取指定位置的瓦片
        /// 例如：SetTileFromSpriteSheet(0, 0) 表示获取第 0 列第 0 行的瓦片
        /// </summary>
        public void SetTileFromSpriteSheet(int tileX, int tileY)
        {
            if (_tileImage == null) return;

            // 计算源矩形
            int x = tileX * _tileSize;
            int y = tileY * _tileSize;
            
            // 确保不超出图片范围
            if (x + _tileSize > _tileImage.Width)
                x = _tileImage.Width - _tileSize;
            if (y + _tileSize > _tileImage.Height)
                y = _tileImage.Height - _tileSize;

            _sourceTile = new Rectangle(x, y, _tileSize, _tileSize);
        }

        /// <summary>
        /// 设置源矩形（直接指定坐标和大小）
        /// </summary>
        public void SetSourceRect(int x, int y, int width, int height)
        {
            _sourceTile = new Rectangle(x, y, width, height);
        }

        /// <summary>
        /// 获取当前的源矩形
        /// </summary>
        public Rectangle GetSourceRect()
        {
            return _sourceTile;
        }

        /// <summary>
        /// 渲染背景 - 平铺指定的瓦片
        /// </summary>
        public override void Render(Graphics graphics)
        {
            if (_tileImage == null || !IsVisible) return;

            // 获取绘图区域尺寸
            int screenWidth = (int)graphics.ClipBounds.Width > 0 ? (int)graphics.ClipBounds.Width : 800;
            int screenHeight = (int)graphics.ClipBounds.Height > 0 ? (int)graphics.ClipBounds.Height : 600;

            // 平铺绘制指定的瓦片
            for (int y = 0; y < screenHeight; y += _sourceTile.Height)
            {
                for (int x = 0; x < screenWidth; x += _sourceTile.Width)
                {
                    graphics.DrawImage(_tileImage, x, y, _sourceTile, GraphicsUnit.Pixel);
                }
            }
        }
    }
}
