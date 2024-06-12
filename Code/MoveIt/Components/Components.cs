using MoveIt.Moveables;
using QCommonLib;
using Unity.Entities;
using Unity.Mathematics;

namespace MoveIt.Components
{
    public struct MIT_Identity : IComponentData
    {
        public Identity m_Identity;

        public MIT_Identity(Identity identity)
        {
            m_Identity = identity;
        }
    }

    public struct MIT_ControlPoint : IComponentData
    {
        public Entity m_Entity;
        public Entity m_Parent;
        public Entity m_Node;
        public float3 m_Position;
        public float m_Diameter;
        public short m_ParentKey;
        public bool m_IsManipulatable;
        public readonly float2 Position2D => m_Position.XZ();

        public readonly MVDefinition Definition => new(Identity.ControlPoint, m_Entity, m_IsManipulatable, true, m_Parent, m_ParentKey);

        public MIT_ControlPoint(Entity e, Entity segment, short parentKey, Entity node, float3 pos, float diameter, bool isManipulatable)
        {
            m_Entity = e;
            m_Parent = segment;
            m_Node = node;
            m_Position = pos;
            m_Diameter = diameter;
            m_ParentKey = parentKey;
            m_IsManipulatable = isManipulatable;
        }

        public readonly override string ToString()
        {
            return $"CPcomp {m_Entity.D()} node:{m_Node.D()} seg:{m_Parent.D()}-{m_ParentKey} pos:{m_Position.D()} dia:{m_Diameter}";
        }
    }
}
