using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Game.Animation
{
    /// <summary>
    /// 动画集合 - 管理角色的多个动画状态（如 idle, walk, jump）
    /// </summary>
    public class AnimationSet
    {
        /// <summary>
        /// 存储所有动画的字典
        /// </summary>
        private Dictionary<string, Animation> _animations = new Dictionary<string, Animation>();

        /// <summary>
        /// 添加动画
        /// </summary>
        public void AddAnimation(string stateName, Animation animation)
        {
            if (animation != null)
            {
                _animations[stateName] = animation;
            }
        }

        /// <summary>
        /// 获取动画
        /// </summary>
        public Animation GetAnimation(string stateName)
        {
            if (_animations.ContainsKey(stateName))
            {
                return _animations[stateName];
            }
            return null;
        }

        /// <summary>
        /// 检查是否存在该状态的动画
        /// </summary>
        public bool HasAnimation(string stateName)
        {
            return _animations.ContainsKey(stateName);
        }

        /// <summary>
        /// 移除动画
        /// </summary>
        public void RemoveAnimation(string stateName)
        {
            if (_animations.ContainsKey(stateName))
            {
                _animations.Remove(stateName);
            }
        }

        /// <summary>
        /// 获取所有动画名称
        /// </summary>
        public List<string> GetAnimationNames()
        {
            return _animations.Keys.ToList();
        }

        /// <summary>
        /// 清空所有动画
        /// </summary>
        public void Clear()
        {
            _animations.Clear();
        }
    }
}
