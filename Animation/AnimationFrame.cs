using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Game.Animation
{
    /// <summary>
    /// 动画帧 - 表示动画中的单一帧
    /// </summary>
    public class AnimationFrame
    {
        /// <summary>
        /// 帧的图像数据（或者可以是贴图上的矩形区域）
        /// </summary>
        public Image Image { get; set; }

        /// <summary>
        /// 帧在雪碧图中的源矩形
        /// </summary>
        public Rectangle SourceRect { get; set; }

        /// <summary>
        /// 该帧显示的持续时间（秒）
        /// </summary>
        public float Duration { get; set; }

        public AnimationFrame(Image image, float duration = 0.1f)
        {
            Image = image;
            Duration = duration;
            SourceRect = new Rectangle(0, 0, image.Width, image.Height);
        }

        public AnimationFrame(Image image, Rectangle sourceRect, float duration = 0.1f)
        {
            Image = image;
            SourceRect = sourceRect;
            Duration = duration;
        }
    }
}
