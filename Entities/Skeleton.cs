namespace Game.Entities
{
    public class Skeleton : Enemy
    {
        public Skeleton(float x, float y, float width = 64, float height = 64)
            : base(x, y, width, height, 14, 14, 24, 33)
        {
            VisionRadius = 190f;
            ChaseSpeed = 115f;
            WanderSpeed = 65f;
            RestAfterSeconds = 10f;
            RestSeconds = 2f;
        }
    }
}
