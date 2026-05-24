using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Entities
{
    /// <summary>
    /// 终点格子 - 玩家到达时触发完成事件
    /// </summary>
    public class EndTile : InteractiveTile
    {
        /// <summary>
        /// 游戏完成事件
        /// </summary>
        public event EventHandler<CharacterEventArgs> OnGameCompleted;

        /// <summary>
        /// 构造函数 - 纯色格子
        /// </summary>
        public EndTile(float x, float y, float width, float height, Color? color = null)
            : base(x, y, width, height)
        {
            // 终点默认为金色
            TileColor = color ?? Color.Gold;
            TileImage = null;
            _sourceRect = new Rectangle(0, 0, (int)width, (int)height);
        }

        /// <summary>
        /// 构造函数 - 图片格子
        /// </summary>
        public EndTile(float x, float y, float width, float height, Image image)
            : base(x, y, width, height)
        {
            TileImage = image;
            TileColor = Color.White;
            _sourceRect = new Rectangle(0, 0, image.Width, image.Height);
        }

        /// <summary>
        /// 角色进入终点格子时，触发游戏完成事件
        /// </summary>
        public override void OnEnter(Character character)
        {
            if (character.Collider == null) return;

            // 将角色放在格子中心
            PointF tileCenter = GetCenter();
            character.X = tileCenter.X - character.Collider.Width / 2 - character.ColliderOffsetX;
            character.Y = tileCenter.Y - character.Collider.Height / 2 - character.ColliderOffsetY;
            character.SetVelocity(0, 0);

            // 触发游戏完成事件
            OnGameCompleted?.Invoke(this, new CharacterEventArgs(character));
        }

        /// <summary>
        /// 角色离开终点格子时（可选）
        /// </summary>
        public override void OnExit(Character character)
        {
            // 可在此添加离开时的逻辑
        }
    }

    /// <summary>
    /// 角色事件参数
    /// </summary>
    public class CharacterEventArgs : EventArgs
    {
        public Character Character { get; set; }

        public CharacterEventArgs(Character character)
        {
            Character = character;
        }
    }
}
