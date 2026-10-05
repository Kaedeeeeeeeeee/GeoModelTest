using UnityEngine;

namespace UISystem
{
    public static class ToolWheelLayout
    {
        // 角度从12点起顺时针增长，边界归入顺时针的下一个扇区。
        public static int SectorAtAngle(float angle, int count, float distance, float deadRadius)
        {
            if (count < 1 || distance <= deadRadius) return -1;
            float span = 360f / count;
            return Mathf.FloorToInt(Mathf.Repeat(angle + span * 0.5f, 360f) / span);
        }

        public static Vector2 Direction(float angle)
        {
            float radians = angle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
        }
    }
}
