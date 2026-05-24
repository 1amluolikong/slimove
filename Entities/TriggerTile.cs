using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Entities
{
    /// <summary>
    /// 触发格子 - 玩家进入后停止，可选择新的移动方向
    /// </summary>
    public class TriggerTile : InteractiveTile
    {
        /// <summary>
        /// 构造函数 - 纯色格子
        /// </summary>
        public TriggerTile(float x, float y, float width, float height, Color? color = null)
            : base(x, y, width, height)
        {
            TileColor = color ?? Color.CornflowerBlue;
            TileImage = null;
            _sourceRect = new Rectangle(0, 0, (int)width, (int)height);
        }

        /// <summary>
        /// 构造函数 - 图片格子
        /// </summary>
        public TriggerTile(float x, float y, float width, float height, Image image)
            : base(x, y, width, height)
        {
            TileImage = image;
            TileColor = Color.White;
            _sourceRect = new Rectangle(0, 0, image.Width, image.Height);
        }

        /// <summary>
        /// 角色进入触发格子时，把角色放在格子中心
        /// </summary>
        public override void OnEnter(Character character)
        {
            if (character.Collider == null) return;

            // 将角色放在格子中心，停止移动
            PointF tileCenter = GetCenter();
            character.X = tileCenter.X - character.Collider.Width / 2 - character.ColliderOffsetX;
            character.Y = tileCenter.Y - character.Collider.Height / 2 - character.ColliderOffsetY;
            character.SetVelocity(0, 0);
        }

        /// <summary>
        /// 角色离开触发格子时（可选，目前暂无特殊处理）
        /// </summary>
        public override void OnExit(Character character)
        {
            // 可在此添加离开时的逻辑
        }
    }
}

