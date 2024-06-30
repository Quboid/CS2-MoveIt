using MoveIt.Moveables;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace MoveIt.QAccessor
{
    internal partial struct QEntity
    {
        private readonly EntityManager Manager => World.DefaultGameObjectInjectionWorld.EntityManager;

        internal Entity m_Entity;
        internal Entity m_Parent;
        internal Identity m_Identity;
        internal ID m_ID;
        internal QLookup m_Lookup;

        internal QEntity(ref QLookup lookup, Entity e, Identity identity, Entity parent = default)
        {
            m_Lookup = lookup;
            m_Entity = e;
            m_Identity = identity;
            m_Parent = parent;

            m_ID = identity switch
            {
                Identity.Segment        => parent == Entity.Null ? ID.Seg : ID.Lane,
                Identity.NetLane        => ID.Lane,
                Identity.Node           => ID.Node,
                Identity.ControlPoint   => ID.CP,
                _ => ID.Generic,
            };
        }

        internal readonly float3 Position => m_ID switch
        {
            ID.Seg  => Segment_Position,
            ID.Lane => Lane_Position,
            ID.CP   => ControlPoint_Position,
            ID.Node => Node_Position,
            _       => Generic_Position,
        };

        internal readonly float Angle => m_ID switch
        {
            ID.Seg  => Segment_Angle,
            ID.Lane => Lane_Angle,
            ID.CP   => ControlPoint_Angle,
            ID.Node => Node_Angle,
            _       => Generic_Angle,
        };

        internal readonly quaternion Rotation => m_ID switch
        {
            ID.Seg  => Segment_Rotation,
            ID.Lane => Lane_Rotation,
            ID.CP   => ControlPoint_Rotation,
            ID.Node => Node_Rotation,
            _       => Generic_Rotation,
        };


        internal bool SetUpdated()
        {
            return m_ID switch
            {
                ID.Seg  => Segment_SetUpdated(),
                ID.Lane => Lane_SetUpdated(),
                ID.CP   => ControlPoint_SetUpdated(),
                ID.Node => Node_SetUpdated(),
                _       => Generic_SetUpdated(),
            };
        }


        internal bool MoveBy(State state, float3 newPosition, float3 delta)
        {
            return m_ID switch
            {
                ID.Seg  => Segment_MoveBy(state, newPosition, delta),
                ID.Lane => Lane_MoveBy(state, newPosition, delta),
                ID.CP   => ControlPoint_MoveBy(state, newPosition, delta),
                ID.Node => Node_MoveBy(state, newPosition, delta),
                _       => Generic_MoveBy(state, newPosition, delta),
            };
        }

        internal bool MoveTo(State state, float3 newPosition, float3 delta)
        {
            return m_ID switch
            {
                ID.Seg  => Segment_MoveTo(state, newPosition, delta),
                ID.Lane => Lane_MoveTo(state, newPosition, delta),
                ID.CP   => ControlPoint_MoveTo(state, newPosition, delta),
                ID.Node => Node_MoveBy(state, newPosition, delta),
                _       => Generic_MoveTo(state, newPosition, delta),
            };
        }

        internal bool RotateBy(State state, float delta, ref Matrix4x4 matrix, float3 origin)
        {
            return m_ID switch
            {
                ID.Seg  => Segment_RotateBy(state, delta, ref matrix, origin),
                ID.Lane => Lane_RotateBy(state, delta, ref matrix, origin),
                ID.CP   => ControlPoint_RotateBy(state, delta, ref matrix, origin),
                ID.Node => Node_RotateBy(state, delta, ref matrix, origin),
                _       => Generic_RotateBy(state, delta, ref matrix, origin),
            };
        }

        internal bool RotateTo(State state, quaternion newRotation, ref Matrix4x4 matrix, float3 origin)
        {
            return m_ID switch
            {
                ID.Seg  => Segment_RotateTo(state, newRotation, ref matrix, origin),
                ID.Lane => Lane_RotateTo(state, newRotation, ref matrix, origin),
                ID.CP   => ControlPoint_RotateTo(state, newRotation, ref matrix, origin),
                ID.Node => Node_RotateTo(state, newRotation, ref matrix, origin),
                _       => Generic_RotateTo(state, newRotation, ref matrix, origin),
            };
        }
    }
}
