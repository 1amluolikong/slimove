using System.Drawing;

namespace Game.Entities
{
    public class LightTile : BaseEntity
    {
        private Rectangle _sourceRect;

        public Image TileImage { get; set; }
        public Color TileColor { get; set; }
        public float LightRadius { get; set; }
        public float FullBrightRadius { get; set; }
        public bool IsLit { get; private set; }

        public LightTile(float x, float y, float width, float height, Image image = null)
            : base(x, y, width, height)
        {
            TileImage = image;
            TileColor = Color.FromArgb(255, 238, 118);
            _sourceRect = image != null
                ? new Rectangle(0, 0, image.Width, image.Height)
                : new Rectangle(0, 0, (int)width, (int)height);
            LightRadius = 150f;
            FullBrightRadius = 100f;
            RenderLayer = 6;
            IsActive = false;
            IsLit = true;
        }

        public void TurnOn()
        {
            IsLit = true;
        }

        public void SetSourceRect(int x, int y, int width, int height)
        {
            _sourceRect = new Rectangle(x, y, width, height);
        }

        public void SetTileFromSpriteSheet(int tileX, int tileY, int tileWidth, int tileHeight)
        {
            if (TileImage == null) return;

            int x = tileX * tileWidth;
            int y = tileY * tileHeight;

            if (x + tileWidth > TileImage.Width)
                x = TileImage.Width - tileWidth;
            if (y + tileHeight > TileImage.Height)
                y = TileImage.Height - tileHeight;

            _sourceRect = new Rectangle(x, y, tileWidth, tileHeight);
        }

        public PointF GetCenter()
        {
            return Center;
        }

        public override void Render(Graphics graphics)
        {
            if (!IsVisible) return;

            if (TileImage != null)
            {
                Rectangle targetRect = new Rectangle((int)X, (int)Y, (int)Width, (int)Height);
                graphics.DrawImage(
                    TileImage,
                    targetRect,
                    _sourceRect,
                    GraphicsUnit.Pixel);

                if (!IsLit)
                {
                    using (Brush dimBrush = new SolidBrush(Color.FromArgb(135, 0, 0, 0)))
                    {
                        graphics.FillRectangle(dimBrush, targetRect);
                    }
                }

                return;
            }

            Rectangle rect = new Rectangle((int)X, (int)Y, (int)Width, (int)Height);
            int fillAlpha = IsLit ? 205 : 80;
            int outlineAlpha = IsLit ? 235 : 120;
            using (Brush glowBrush = new SolidBrush(Color.FromArgb(fillAlpha, 255, 235, 96)))
            using (Pen outlinePen = new Pen(Color.FromArgb(outlineAlpha, 255, 246, 150), 1))
            {
                graphics.FillEllipse(glowBrush, rect);
                graphics.DrawEllipse(outlinePen, rect);
            }
        }
    }
}
