using QCommonLib;
using Unity.Entities;
using Unity.Mathematics;

namespace MoveIt.Components
{
    public static class Components
    {
        public static void Configure(EntityManager em, SystemHandle systemHandle)
        {
            em.AddComponent<MIT_ControlPoint>(systemHandle);
        }
    }

    public struct MIT_ControlPoint : IComponentData
    {
        public Entity m_Entity;
        public Entity m_Segment;
        public Entity m_Node;
        public float3 m_Position;
        public float m_Diameter;
        public short m_CurveKey;
        public readonly float2 Position2D => m_Position.XZ();

        public MIT_ControlPoint(Entity e, Entity segment, Entity node, float3 pos, float diameter, short curveKey)
        {
            m_Entity = e;
            m_Segment = segment;
            m_Node = node;
            m_Position = pos;
            m_Diameter = diameter;
            m_CurveKey = curveKey;
        }

        public readonly override string ToString()
        {
            return $"CPcomp {m_Entity.D()} node:{m_Node.D()} seg:{m_Segment.D()} pos:{m_Position.D()} dia:{m_Diameter} idx:{m_CurveKey}";
        }
    }
}
