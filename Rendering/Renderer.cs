using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace Game.Rendering
{
    /// <summary>
    /// 渲染器 - 负责将游戏实体渲染到屏幕
    /// </summary>
    public class Renderer
    {
        private Control _renderTarget;
        private CameraManager _camera;

        public Renderer(Control renderTarget)
        {
            _renderTarget = renderTarget;
            _camera = new CameraManager(renderTarget.Width, renderTarget.Height);
        }

        /// <summary>
        /// 渲染所有实体
        /// </summary>
        public void RenderEntities(Graphics graphics, List<Entities.BaseEntity> entities)
        {
            // 按 Y 轴坐标排序（实现遮挡排序）
            var sortedEntities = entities.Where(e => e.IsVisible)
                                        .OrderBy(e => e.RenderLayer)
                                        .ThenBy(e => e.Y + e.Height)
                                        .ToList();

            // 渲染每个实体
            foreach (var entity in sortedEntities)
            {
                entity.Render(graphics);
            }

            // 可选：绘制调试信息
            //DrawDebugInfo(graphics, entities);
        }

        /// <summary>
        /// 绘制调试信息
        /// </summary>
        private void DrawDebugInfo(Graphics graphics, List<Entities.BaseEntity> entities)
        {
            // 可以添加 FPS、实体数量等调试信息
            using (Font font = new Font("Arial", 10))
            {
                string debugText = $"Entities: {entities.Count}";
                graphics.DrawString(debugText, font, Brushes.White, 10, 10);
            }
        }

        /// <summary>
        /// 获取相机管理器
        /// </summary>
        public CameraManager GetCamera()
        {
            return _camera;
        }

        /// <summary>
        /// 设置渲染目标的背景颜色
        /// </summary>
        public void SetBackgroundColor(Color color)
        {
            _renderTarget.BackColor = color;
        }
    }
}
