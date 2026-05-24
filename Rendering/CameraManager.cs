using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Rendering
{
    /// <summary>
    /// 相机管理器 - 负责管理游戏视口和镜头跟随
    /// </summary>
    public class CameraManager
    {
        /// <summary>
        /// 相机位置
        /// </summary>
        public float X { get; set; }
        public float Y { get; set; }

        /// <summary>
        /// 相机视口大小
        /// </summary>
        public float ViewportWidth { get; private set; }
        public float ViewportHeight { get; private set; }

        /// <summary>
        /// 相机缩放比例
        /// </summary>
        public float ZoomLevel { get; set; }

        /// <summary>
        /// 要跟随的目标实体
        /// </summary>
        private Entities.BaseEntity _targetEntity;
        private float _followSpeed = 5.0f;

        public CameraManager(float viewportWidth, float viewportHeight)
        {
            ViewportWidth = viewportWidth;
            ViewportHeight = viewportHeight;
            X = 0;
            Y = 0;
            ZoomLevel = 1.0f;
        }

        /// <summary>
        /// 设置相机跟随目标
        /// </summary>
        public void SetFollowTarget(Entities.BaseEntity target, float followSpeed = 5.0f)
        {
            _targetEntity = target;
            _followSpeed = followSpeed;
        }

        /// <summary>
        /// 更新相机位置（每帧调用）
        /// </summary>
        public void Update(float deltaTime)
        {
            if (_targetEntity != null)
            {
                // 计算目标位置（保持目标在屏幕中心）
                float targetX = _targetEntity.Center.X - ViewportWidth / 2;
                float targetY = _targetEntity.Center.Y - ViewportHeight / 2;

                // 平滑过渡到目标位置
                X += (targetX - X) * _followSpeed * deltaTime;
                Y += (targetY - Y) * _followSpeed * deltaTime;

                // 限制相机在世界边界内（可选）
                // X = Math.Max(0, X);
                // Y = Math.Max(0, Y);
            }
        }

        /// <summary>
        /// 将世界坐标转换为屏幕坐标
        /// </summary>
        public void WorldToScreen(float worldX, float worldY, out float screenX, out float screenY)
        {
            screenX = (worldX - X) * ZoomLevel;
            screenY = (worldY - Y) * ZoomLevel;
        }

        /// <summary>
        /// 将屏幕坐标转换为世界坐标
        /// </summary>
        public void ScreenToWorld(float screenX, float screenY, out float worldX, out float worldY)
        {
            worldX = screenX / ZoomLevel + X;
            worldY = screenY / ZoomLevel + Y;
        }

        /// <summary>
        /// 设置相机位置
        /// </summary>
        public void SetPosition(float x, float y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// 缩放相机
        /// </summary>
        public void Zoom(float zoomFactor)
        {
            ZoomLevel = Math.Max(0.1f, ZoomLevel * zoomFactor);
        }
    }
}
