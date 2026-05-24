using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace Game.Core
{
    /// <summary>
    /// 游戏循环管理器 - 负责驱动游戏的 Update 和 Render 周期
    /// </summary>
    internal class GameLoop
    {
        private Timer _gameTimer;
        private int _targetFPS = 60;
        private int _frameInterval;
        
        // 委托和事件
        public delegate void GameUpdateDelegate(float deltaTime);
        public delegate void GameRenderDelegate(Graphics graphics);
        
        public event GameUpdateDelegate OnUpdate;
        public event GameRenderDelegate OnRender;
        
        private long _lastFrameTime = 0;
        private float _deltaTime = 0;

        public GameLoop(int targetFPS = 60)
        {
            _targetFPS = targetFPS;
            _frameInterval = 1000 / targetFPS; // 毫秒
            _gameTimer = new Timer();
            _gameTimer.Interval = _frameInterval;
            _gameTimer.Tick += GameTimer_Tick;
        }

        /// <summary>
        /// 启动游戏循环
        /// </summary>
        public void Start()
        {
            _lastFrameTime = DateTime.Now.Ticks;
            _gameTimer.Start();
        }

        /// <summary>
        /// 停止游戏循环
        /// </summary>
        public void Stop()
        {
            _gameTimer.Stop();
        }

        /// <summary>
        /// 游戏循环的 Tick 事件
        /// </summary>
        private void GameTimer_Tick(object sender, EventArgs e)
        {
            // 计算 deltaTime
            long currentTime = DateTime.Now.Ticks;
            _deltaTime = (float)(currentTime - _lastFrameTime) / 10000000f; // 转换为秒
            _lastFrameTime = currentTime;

            // 调用 Update 事件
            OnUpdate?.Invoke(_deltaTime);
        }

        /// <summary>
        /// 触发渲染事件
        /// </summary>
        public void Render(Graphics graphics)
        {
            OnRender?.Invoke(graphics);
        }

        /// <summary>
        /// 获取当前 FPS
        /// </summary>
        public int GetTargetFPS()
        {
            return _targetFPS;
        }

        /// <summary>
        /// 获取 deltaTime（上一帧的时间间隔）
        /// </summary>
        public float GetDeltaTime()
        {
            return _deltaTime;
        }

        /// <summary>
        /// 销毁游戏循环
        /// </summary>
        public void Dispose()
        {
            Stop();
            _gameTimer?.Dispose();
        }
    }
}

