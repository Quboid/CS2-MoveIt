using Colossal.Mathematics;
using MoveIt.Moveables;
using MoveIt.Tool;
using QCommonLib;
using Unity.Entities;
using Unity.Mathematics;

namespace MoveIt.Overlays
{
    public sealed class Factory
    {
        private static readonly MIT _MIT = MIT.m_Instance;

        /// <summary>
        /// Create an Overlay subclass
        /// </summary>
        /// <typeparam name="T">The subclass to make</typeparam>
        /// <param name="mv">The Moveable this overlay is for</param>
        /// <param name="type">The type of overlay</param>
        /// <returns>The Overlay subclass</returns>
        public static T Create<T>(Moveable mv, OverlayTypes type) where T : Overlay, new()
        {
            T overlay = new()
            {
                m_Type = type,
                m_Moveable = mv,
                m_Owner = mv.m_Entity,
            };

            return overlay;
        }

        //public static bool RemoveOverlay(Overlay overlay)
        //{
        //    if (!_MIT.EntityManager.Exists(overlay.m_Entity)) return false;

        //    return _MIT.EntityManager.AddComponent<Game.Common.Deleted>(overlay.m_Entity);
        //}
    }


    public enum OverlayTypes
    {
        None,
        Bounds,
        Circle,
        Diamond,
        Line,
        Quad,
        Marquee,
        SelectionCenter,
        MVBuilding,
        MVCircle,
        MVControlPoint,
        MVManipControlPoint,
        MVDecal,
        MVManipSegment,
        MVNode,
        MVSegment,
        MVSurface,
    }

    public enum ToolFlags
    {
        None                = 0,
        ManipulationMode    = 1,
        HasShift            = 2,
        IsMarquee           = 4,
        ShowDebug           = 8,
    }

    public enum Projection
    {
        Unset               = 0,
        Ground              = 1,
        Fixed               = 2,
    }

    public struct MIO_Type : IComponentData
    {
        public OverlayTypes m_Type;

        public MIO_Type(OverlayTypes type)
        {
            m_Type = type;
        }
    }

    public struct MIO_Updateable : IComponentData { }
    public struct MIO_SingleFrame : IComponentData { }
    public struct MIO_Debug : IComponentData { }

    public struct MIO_TTL : IComponentData
    {
        public int m_TTL;

        public MIO_TTL(int ttl)
        {
            m_TTL = ttl;
        }
    }

    public struct MIO_Bezier : IComponentData
    {
        public Bezier4x3 Curve;
        public float Width;

        public MIO_Bezier(Bezier4x3 curve, float width)
        {
            Curve = curve;
            Width = width;
        }
    }

    public struct MIO_Beziers : IBufferElementData
    {
        public Bezier4x3 Curve;

        public MIO_Beziers(Bezier4x3 curve)
        {
            Curve = curve;
        }
    }

    public struct MIO_Bounds : IComponentData
    {
        public Bounds3 Bounds;

        public MIO_Bounds(Bounds3 bounds)
        {
            Bounds = bounds;
        }
    }

    public struct MIO_Circle : IComponentData
    {
        public Circle3 Circle;

        public MIO_Circle(Circle3 circle)
        {
            Circle = circle;
        }
    }

    public struct MIO_Circles : IBufferElementData
    {
        public Circle3 Circle;

        public MIO_Circles(Circle3 circle)
        {
            Circle = circle;
        }
    }

    public struct MIO_DashedLines : IBufferElementData
    {
        public Line3.Segment Line;

        public MIO_DashedLines(Line3.Segment line)
        {
            Line = line;
        }
    }

    public struct MIO_Line : IComponentData
    {
        public Line3.Segment Line;

        public MIO_Line(Line3.Segment line)
        {
            Line = line;
        }
    }

    public struct MIO_Lines : IBufferElementData
    {
        public Line3.Segment Line;

        public MIO_Lines(Line3.Segment line)
        {
            Line = line;
        }
    }

    public struct MIO_Quad : IComponentData
    {
        public Quad3 Quad;

        public MIO_Quad(Quad3 quad)
        {
            Quad = quad;
        }
    }

    public struct MIO_SelectionData : IComponentData
    {
        public int m_Count;
        public float3 m_Position;
        public float m_TerrainHeight;

        public MIO_SelectionData(int count, float3 position, float terrainHeight)
        {
            m_Count = count;
            m_Position = position;
            m_TerrainHeight = terrainHeight;
        }
    }

    public struct MIO_Common : IComponentData
    {
        public UnityEngine.Color m_OutlineColor     = default;
        public UnityEngine.Color m_BackgroundColor  = default;
        public InteractionFlags m_Flags             = InteractionFlags.None;
        public bool m_IsManipulatable               = false;
        public bool m_IsManipChild                  = false;
        public Entity m_Owner                       = Entity.Null;
        public Entity m_Prefab                      = Entity.Null;
        public Game.Objects.Transform m_Transform   = default;
        public float m_OutlineWidthGround           = Overlay.LINE_DEFAULT_WIDTH;
        public float m_OutlineWidthFixed            = Overlay.LINE_DEFAULT_WIDTH;
        public float m_TerrainHeight                = 0f;
        public float m_ElevationN                   = 0f;
        public float m_ElevationO                   = 0f;
        public float m_ShadowOpacity                = 0f;

        public readonly bool ShowShadow => m_ShadowOpacity > 0.0001f;

        public MIO_Common()
        { }

        public MIO_Common(Entity owner)
        {
            m_Owner = owner;
        }

        public readonly float GetWidth(Projection projection)
        {
            if (projection == Projection.Unset) return 0f;
            return projection == Projection.Fixed ? m_OutlineWidthFixed : m_OutlineWidthGround;
        }
    }
}
