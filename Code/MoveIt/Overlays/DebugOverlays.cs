using Colossal.Mathematics;
using Unity.Mathematics;

namespace MoveIt.Overlays
{
    internal abstract class DebugOverlays
    {
        //internal static void Bounds(Bounds3 bounds, int ttl = 0, UnityEngine.Color color = default)
        //{
        //    DebugBounds.Factory(bounds, ttl, color);
        //}

        internal static void Circle(float3 center, float radius, UnityEngine.Color color = default)
        {
            if (radius < 0.1f) return;

            DebugCircle.Factory(center, radius, color);
        }

        internal static void Line(Line3.Segment line, UnityEngine.Color color = default)
        {
            DebugLine.Factory(line, color);
        }
    }
}
