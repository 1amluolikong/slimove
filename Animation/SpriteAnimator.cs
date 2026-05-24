using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Game.Animation
{
    /// <summary>
    /// 精灵动画播放器 - 驱动精灵的动画播放和渲染
    /// </summary>
    public class SpriteAnimator
    {
        /// <summary>
        /// 当前正在播放的动画
        /// </summary>
        private Animation _currentAnimation;

        /// <summary>
        /// 动画播放完成的事件
        /// </summary>
        public event EventHandler<EventArgs> OnAnimationFinished;

        public SpriteAnimator()
        {
            _currentAnimation = null;
        }

        /// <summary>
        /// 播放动画
        /// </summary>
        public void PlayAnimation(Animation animation)
        {
            if (animation == null) return;

            // 如果已经在播放该动画，则不重新开始
            if (_currentAnimation == animation && animation.IsPlaying)
                return;

            _currentAnimation = animation;
            _currentAnimation.Play();
        }

        /// <summary>
        /// 停止当前动画
        /// </summary>
        public void StopAnimation()
        {
            if (_currentAnimation != null)
            {
                _currentAnimation.Stop();
            }
        }

        /// <summary>
        /// 暂停当前动画
        /// </summary>
        public void PauseAnimation()
        {
            if (_currentAnimation != null)
            {
                _currentAnimation.Pause();
            }
        }

        /// <summary>
        /// 恢复动画播放
        /// </summary>
        public void ResumeAnimation()
        {
            if (_currentAnimation != null)
            {
                _currentAnimation.Play();
            }
        }

        /// <summary>
        /// 更新动画（每帧调用）
        /// </summary>
        public void Update(float deltaTime)
        {
            if (_currentAnimation == null) return;

            _currentAnimation.Update(deltaTime);

            // 触发动画完成事件
            if (_currentAnimation.IsFinished())
            {
                OnAnimationFinished?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 获取当前帧
        /// </summary>
        public AnimationFrame GetCurrentFrame()
        {
            if (_currentAnimation == null) return null;
            return _currentAnimation.GetCurrentFrame();
        }

        /// <summary>
        /// 渲染当前动画帧
        /// </summary>
        public void Render(Graphics graphics, int x, int y, int direction = 1)
        {
            if (_currentAnimation == null) return;

            var frame = _currentAnimation.GetCurrentFrame();
            if (frame == null) return;

            try
            {
                // 如果有源矩形，则从贴图上截取特定区域
                if (frame.SourceRect.Width > 0 && frame.SourceRect.Height > 0)
                {
                    // 计算目标矩形
                    int destWidth = frame.SourceRect.Width;
                    int destHeight = frame.SourceRect.Height;

                    // 根据方向翻转渲染（可选）
                    if (direction == -1)
                    {
                        graphics.TranslateTransform(x + destWidth, y);
                        graphics.ScaleTransform(-1, 1);
                        graphics.DrawImage(frame.Image, 0, 0, frame.SourceRect, GraphicsUnit.Pixel);
                        graphics.ResetTransform();
                    }
                    else
                    {
                        graphics.DrawImage(frame.Image, x, y, frame.SourceRect, GraphicsUnit.Pixel);
                    }
                }
                else if (frame.Image != null)
                {
                    // 如果没有源矩形，直接渲染整个图像
                    if (direction == -1)
                    {
                        graphics.TranslateTransform(x + frame.Image.Width, y);
                        graphics.ScaleTransform(-1, 1);
                        graphics.DrawImage(frame.Image, 0, 0);
                        graphics.ResetTransform();
                    }
                    else
                    {
                        graphics.DrawImage(frame.Image, x, y);
                    }
                }
            }
            catch (Exception ex)
            {
                // 异常处理（可以记录日志）
                System.Diagnostics.Debug.WriteLine($"渲染动画帧失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取当前动画
        /// </summary>
        public Animation GetCurrentAnimation()
        {
            return _currentAnimation;
        }

        /// <summary>
        /// 检查是否有动画正在播放
        /// </summary>
        public bool IsPlaying()
        {
            return _currentAnimation != null && _currentAnimation.IsPlaying;
        }
    }
}
