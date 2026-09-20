using UnityEngine;

namespace Platformer
{
    public static class MathUtil
    {
        /// <summary>把任意角度转换成 -180..180。</summary>
        public static float WrapAngle(float angle)
        {
            angle %= 360f;
            if (angle > 180f) angle -= 360f;
            if (angle < -180f) angle += 360f;
            return angle;
        }

        /// <summary>按给定的加/减速度把 current 推向 target。</summary>
        public static float Approach(float current, float target, float accel, float decel, float dt)
        {
            float rate = Mathf.Abs(target) > Mathf.Abs(current) ? accel : decel;
            return Mathf.MoveTowards(current, target, rate * dt);
        }

        /// <summary>由水平方向和上挑角度算出击退速度。</summary>
        public static Vector2 KnockbackVelocity(int facing, float force, float liftAngleDegrees)
        {
            float rad = liftAngleDegrees * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(facing >= 0 ? 1f : -1f, Mathf.Tan(rad)).normalized;
            return dir * force;
        }
    }
}
