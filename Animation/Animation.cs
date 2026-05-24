using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Game.Animation
{
    /// <summary>
    /// 动画控制器 - 负责管理和播放单个动画
    /// </summary>
    public class Animation
    {
        /// <summary>
        /// 动画名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 动画帧列表
        /// </summary>
        public List<AnimationFrame> Frames { get; private set; }

        /// <summary>
        /// 动画是否循环
        /// </summary>
        public bool IsLooping { get; set; }

        /// <summary>
        /// 当前播放的帧索引
        /// </summary>
        public int CurrentFrameIndex { get; private set; }

        /// <summary>
        /// 当前帧的已用时间
        /// </summary>
        public float CurrentFrameTime { get; private set; }

        /// <summary>
        /// 动画是否正在播放
        /// </summary>
        public bool IsPlaying { get; private set; }

        /// <summary>
        /// 动画总时长
        /// </summary>
        public float TotalDuration { get; private set; }

        public Animation(string name, bool isLooping = true)
        {
            Name = name;
            Frames = new List<AnimationFrame>();
            IsLooping = isLooping;
            CurrentFrameIndex = 0;
            CurrentFrameTime = 0;
            IsPlaying = false;
            TotalDuration = 0;
        }

        /// <summary>
        /// 添加帧到动画
        /// </summary>
        public void AddFrame(AnimationFrame frame)
        {
            Frames.Add(frame);
            TotalDuration += frame.Duration;
        }

        /// <summary>
        /// 添加多个帧
        /// </summary>
        public void AddFrames(params AnimationFrame[] frames)
        {
            foreach (var frame in frames)
            {
                AddFrame(frame);
            }
        }

        /// <summary>
        /// 开始播放动画
        /// </summary>
        public void Play()
        {
            IsPlaying = true;
            CurrentFrameIndex = 0;
            CurrentFrameTime = 0;
        }

        /// <summary>
        /// 暂停动画
        /// </summary>
        public void Pause()
        {
            IsPlaying = false;
        }

        /// <summary>
        /// 停止动画（重置到第一帧）
        /// </summary>
        public void Stop()
        {
            IsPlaying = false;
            CurrentFrameIndex = 0;
            CurrentFrameTime = 0;
        }

        /// <summary>
        /// 更新动画（每帧调用）
        /// </summary>
        public void Update(float deltaTime)
        {
            if (!IsPlaying || Frames.Count == 0)
                return;

            CurrentFrameTime += deltaTime;

            // 检查是否需要切换帧
            var currentFrame = Frames[CurrentFrameIndex];
            if (CurrentFrameTime >= currentFrame.Duration)
            {
                CurrentFrameTime -= currentFrame.Duration;
                CurrentFrameIndex++;

                // 检查是否播放完成
                if (CurrentFrameIndex >= Frames.Count)
                {
                    if (IsLooping)
                    {
                        CurrentFrameIndex = 0;
                    }
                    else
                    {
                        CurrentFrameIndex = Frames.Count - 1;
                        IsPlaying = false;
                    }
                }
            }
        }

        /// <summary>
        /// 获取当前帧
        /// </summary>
        public AnimationFrame GetCurrentFrame()
        {
            if (Frames.Count == 0) return null;
            return Frames[Utils.Clamp(CurrentFrameIndex, 0, Frames.Count - 1)];
        }

        /// <summary>
        /// 获取指定索引的帧
        /// </summary>
        public AnimationFrame GetFrame(int index)
        {
            if (index < 0 || index >= Frames.Count) return null;
            return Frames[index];
        }

        /// <summary>
        /// 检查动画是否播放完成
        /// </summary>
        public bool IsFinished()
        {
            return !IsLooping && !IsPlaying && CurrentFrameIndex >= Frames.Count - 1;
        }

        /// <summary>
        /// 重置动画
        /// </summary>
        public void Reset()
        {
            CurrentFrameIndex = 0;
            CurrentFrameTime = 0;
        }
    }
}
