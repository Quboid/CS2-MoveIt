using Colossal.Entities;
using MoveIt.Moveables;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace MoveIt.QAccessor
{
    internal partial struct QEntity
    {
        /// <summary>
        /// This object's entity
        /// </summary>
        internal Entity m_Entity;
        /// <summary>
        /// The ultimate parent entity, what the player sees as selected
        /// </summary>
        internal Entity m_Parent;
        /// <summary>
        /// The direct owner of this object
        /// </summary>
        internal Entity m_Owner;
        internal Identity m_Identity;
        internal ID m_ID;
        internal QLookup m_Lookup;
        internal EntityManager m_Manager;

        internal QEntity(EntityManager manager, ref QLookup lookup, Entity e, Identity identity, Entity parent = default)
        {
            m_Lookup    = lookup;
            m_Entity    = e;
            m_Identity  = identity;
            m_Parent    = parent;
            m_Owner     = default;
            m_Manager   = manager;

            m_ID = identity switch
            {
                Identity.Segment        => parent == Entity.Null ? ID.Seg : ID.Lane,
                Identity.NetLane        => ID.Lane,
                Identity.Node           => ID.Node,
                Identity.ControlPoint   => ID.CP,
                Identity.Surface        => ID.Surface,
                _ => ID.Generic,
            };

            if (m_Manager.TryGetComponent<Game.Common.Owner>(e, out var owner))
            {
                m_Owner = owner.m_Owner;
            }
        }

        internal readonly float3 Position => m_ID switch
        {
            ID.Seg      => Segment_Position,
            ID.Lane     => Lane_Position,
            ID.CP       => ControlPoint_Position,
            ID.Node     => Node_Position,
            ID.Surface  => Surface_Position,
            _           => Generic_Position,
        };

        internal readonly float Angle => m_ID switch
        {
            ID.Seg      => Segment_Angle,
            ID.Lane     => Lane_Angle,
            ID.CP       => ControlPoint_Angle,
            ID.Node     => Node_Angle,
            ID.Surface  => Surface_Angle,
            _           => Generic_Angle,
        };

        internal readonly quaternion Rotation => m_ID switch
        {
            ID.Seg      => Segment_Rotation,
            ID.Lane     => Lane_Rotation,
            ID.CP       => ControlPoint_Rotation,
            ID.Node     => Node_Rotation,
            ID.Surface  => Surface_Rotation,
            _           => Generic_Rotation,
        };


        internal bool SetUpdated()
        {
            return m_ID switch
            {
                ID.Seg      => Segment_SetUpdated(),
                ID.Lane     => Lane_SetUpdated(),
                ID.CP       => ControlPoint_SetUpdated(),
                ID.Node     => Node_SetUpdated(),
                ID.Surface  => Surface_SetUpdated(),
                _           => Generic_SetUpdated(),
            };
        }

        internal readonly void TransformEnd(NativeArray<Entity> allEntities)
        {
            switch (m_ID)
            {
                case ID.Seg:
                    Segment_TransformEnd();
                    break;
                case ID.Lane:
                    Lane_TransformEnd();
                    break;
                case ID.CP:
                    ControlPoint_TransformEnd();
                    break;
                case ID.Node:
                    Node_TransformEnd();
                    break;
                case ID.Surface:
                    Surface_TransformEnd();
                    break;
                default:
                    Generic_TransformEnd(allEntities);
                    break;
            };
        }


        internal bool MoveBy(State state, float3 newPosition, float3 delta)
        {
            return m_ID switch
            {
                ID.Seg      => Segment_MoveBy(state, newPosition, delta),
                ID.Lane     => Lane_MoveBy(state, newPosition, delta),
                ID.CP       => ControlPoint_MoveBy(state, newPosition, delta),
                ID.Node     => Node_MoveBy(state, newPosition, delta),
                ID.Surface  => Surface_MoveBy(state, newPosition, delta),
                _           => Generic_MoveBy(state, newPosition, delta),
            };
        }

        internal bool MoveTo(State state, float3 newPosition, float3 delta)
        {
            return m_ID switch
            {
                ID.Seg      => Segment_MoveTo(state, newPosition, delta),
                ID.Lane     => Lane_MoveTo(state, newPosition, delta),
                ID.CP       => ControlPoint_MoveTo(state, newPosition, delta),
                ID.Node     => Node_MoveTo(state, newPosition, delta),
                ID.Surface  => Surface_MoveTo(state, newPosition, delta),
                _           => Generic_MoveTo(state, newPosition, delta),
            };
        }

        internal bool RotateBy(State state, float delta, ref Matrix4x4 matrix, float3 origin)
        {
            return m_ID switch
            {
                ID.Seg      => Segment_RotateBy(state, delta, ref matrix, origin),
                ID.Lane     => Lane_RotateBy(state, delta, ref matrix, origin),
                ID.CP       => ControlPoint_RotateBy(state, delta, ref matrix, origin),
                ID.Node     => Node_RotateBy(state, delta, ref matrix, origin),
                ID.Surface  => Surface_RotateBy(state, delta, ref matrix, origin),
                _           => Generic_RotateBy(state, delta, ref matrix, origin),
            };
        }

        internal bool RotateTo(State state, quaternion newRotation, ref Matrix4x4 matrix, float3 origin)
        {
            return m_ID switch
            {
                ID.Seg      => Segment_RotateTo(state, newRotation, ref matrix, origin),
                ID.Lane     => Lane_RotateTo(state, newRotation, ref matrix, origin),
                ID.CP       => ControlPoint_RotateTo(state, newRotation, ref matrix, origin),
                ID.Node     => Node_RotateTo(state, newRotation, ref matrix, origin),
                ID.Surface  => Surface_RotateTo(state, newRotation, ref matrix, origin),
                _           => Generic_RotateTo(state, newRotation, ref matrix, origin),
            };
        }
    }
}
