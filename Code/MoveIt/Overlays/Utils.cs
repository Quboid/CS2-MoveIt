using System;
using Colossal.Mathematics;
using MoveIt.Tool;
using Unity.Entities;
using Unity.Mathematics;

namespace MoveIt.Overlays
{
    public enum OverlayTypes
    {
        None,
        Bounds,
        Circle,
        Diamond,
        Line,
        Quad,
        Marquee,
        SelectionCentralPoint,
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

    [Flags]
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
        public readonly OverlayTypes m_Type;

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
        /// <summary>
        /// The main colour of this overlay
        /// </summary>
        public UnityEngine.Color m_OutlineColor;
        /// <summary>
        /// The secondary colour used to accent the m_OutlineColor (optional)
        /// </summary>
        public UnityEngine.Color m_BackgroundColor;
        /// <summary>
        /// How this overlay is being used
        /// </summary>
        public InteractionFlags m_Flags;
        public bool m_IsManipulatable;
        public bool m_IsManipChild;
        /// <summary>
        /// The object that this overlay is rendered for, for selection/hover/tool-hover/etc
        /// </summary>
        public Entity m_Owner;
        /// <summary>
        /// The prefab of the object that this overlay is rendered for (unused at this time)
        /// </summary>
        public Entity m_Prefab;
        /// <summary>
        /// This overlay's position and rotation
        /// </summary>
        public Game.Objects.Transform m_Transform;
        /// <summary>
        /// The width of this overlay's lines at ground level
        /// </summary>
        public float m_OutlineWidthGround;
        /// <summary>
        /// The width of this overlay's lines at the object's position
        /// </summary>
        public float m_OutlineWidthFixed;
        /// <summary>
        /// The terrain height where this overlay is being rendered
        /// </summary>
        public float m_TerrainHeight;
        /// <summary>
        /// The opacity of the shadow overlay, 0f if none
        /// </summary>
        public float m_ShadowOpacity;

        /// <summary>
        /// Is there a shadow overlay for this object?
        /// </summary>
        public readonly bool ShowShadow => m_ShadowOpacity > 0.0001f;

        // Create new struct with default values, needs pointless parameter in C# 9.0
        public MIO_Common(bool _)
        {
            m_OutlineColor = default;
            m_BackgroundColor = default;
            m_Flags = InteractionFlags.None;
            m_IsManipulatable = false;
            m_IsManipChild = false;
            m_Owner = Entity.Null;
            m_Prefab = Entity.Null;
            m_Transform = default;
            m_OutlineWidthGround = Overlay.LINE_DEFAULT_WIDTH;
            m_OutlineWidthFixed = Overlay.LINE_DEFAULT_WIDTH;
            m_TerrainHeight = 0f;
            m_ShadowOpacity = 0f;
        }

        public MIO_Common(Entity owner)
        {
            m_OutlineColor = default;
            m_BackgroundColor = default;
            m_Flags = InteractionFlags.None;
            m_IsManipulatable = false;
            m_IsManipChild = false;
            m_Prefab = Entity.Null;
            m_Transform = default;
            m_OutlineWidthGround = Overlay.LINE_DEFAULT_WIDTH;
            m_OutlineWidthFixed = Overlay.LINE_DEFAULT_WIDTH;
            m_TerrainHeight = 0f;
            m_ShadowOpacity = 0f;

            m_Owner = owner;
        }

        /// <summary>
        /// Get the line width for a line's actual draw call
        /// </summary>
        /// <param name="projection">The type of projection this line uses</param>
        /// <returns></returns>
        public readonly float GetWidth(Projection projection)
        {
            if (projection == Projection.Unset) return 0f;
            return projection == Projection.Fixed ? m_OutlineWidthFixed : m_OutlineWidthGround;
        }
    }
}
