﻿using Colossal.Entities;
using MoveIt.Moveables;
using MoveIt.Tool;
using QCommonLib;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace MoveIt.QAccessor.QEntity
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
        private readonly ID _ID;
        private QLookup _Lookup;
        private EntityManager _Manager;

        internal QEntity(EntityManager manager, ref QLookup lookup, Entity e, Identity identity, Entity parent = default)
        {
            _Lookup    = lookup;
            m_Entity    = e;
            m_Identity  = identity;
            m_Parent    = parent;
            m_Owner     = default;
            _Manager   = manager;

            _ID = identity switch
            {
                Identity.Prop           => ID.Prop,
                Identity.Decal          => ID.Prop,
                Identity.Plant          => ID.Prop,
                Identity.Segment        => parent == Entity.Null ? ID.Seg : ID.Lane,
                Identity.NetLane        => ID.Lane,
                Identity.Node           => ID.Node,
                Identity.ControlPoint   => ID.CP,
                Identity.Surface        => ID.Surface,
                _ => ID.Generic,
            };

            if (_Manager.TryGetComponent<Game.Common.Owner>(e, out var owner))
            {
                m_Owner = owner.m_Owner;
            }
        }

        internal float3 Position => _ID switch
        {
            ID.Seg      => Segment_Position,
            ID.Lane     => Lane_Position,
            ID.CP       => ControlPoint_Position,
            ID.Node     => Node_Position,
            ID.Surface  => Surface_Position,
            _           => Generic_Position,
        };

        internal float Angle => _ID switch
        {
            ID.Seg      => Segment_Angle,
            ID.Lane     => Lane_Angle,
            ID.CP       => ControlPoint_Angle,
            ID.Node     => Node_Angle,
            ID.Surface  => Surface_Angle,
            _           => Generic_Angle,
        };

        internal quaternion Rotation => _ID switch
        {
            ID.Seg      => Segment_Rotation,
            ID.Lane     => Lane_Rotation,
            ID.CP       => ControlPoint_Rotation,
            ID.Node     => Node_Rotation,
            ID.Surface  => Surface_Rotation,
            _           => Generic_Rotation,
        };

        private bool TryGetElevation(out float elevation)
        {
            elevation = 0f;
            bool result = _ID switch
            {
                ID.Prop     => PropPlant_TryGetElevation(out elevation),
                ID.Seg      => Net_TryGetElevation(out elevation),
                ID.Node     => Net_TryGetElevation(out elevation),
                ID.Lane     => Net_TryGetElevation(out elevation),
                ID.CP       => ControlPoint_TryGetElevation(out elevation),
                _           => Generic_TryGetElevation(out elevation),
            };
            return result;
        }

        private bool TrySetElevation(float elevation)
        {
            bool result = _ID switch
            {
                ID.Prop     => PropPlant_TrySetElevation(elevation),
                ID.Seg      => Net_TrySetElevation(elevation),
                ID.Node     => Net_TrySetElevation(elevation),
                ID.Lane     => Net_TrySetElevation(elevation),
                ID.CP       => ControlPoint_TrySetElevation(elevation),
                _           => Generic_TrySetElevation(elevation),
            };
            return result;
        }


        internal bool SetUpdated()
        {
            return _ID switch
            {
                ID.Seg      => Segment_SetUpdated(),
                ID.Lane     => Lane_SetUpdated(),
                ID.CP       => ControlPoint_SetUpdated(),
                ID.Node     => Node_SetUpdated(),
                ID.Surface  => Surface_SetUpdated(),
                _           => Generic_SetUpdated(),
            };
        }

        internal void TransformEnd(NativeArray<Entity> allEntities)
        {
            switch (_ID)
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
            return _ID switch
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
            return _ID switch
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
            return _ID switch
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
            return _ID switch
            {
                ID.Seg      => Segment_RotateTo(state, newRotation, ref matrix, origin),
                ID.Lane     => Lane_RotateTo(state, newRotation, ref matrix, origin),
                ID.CP       => ControlPoint_RotateTo(state, newRotation, ref matrix, origin),
                ID.Node     => Node_RotateTo(state, newRotation, ref matrix, origin),
                ID.Surface  => Surface_RotateTo(state, newRotation, ref matrix, origin),
                _           => Generic_RotateTo(state, newRotation, ref matrix, origin),
            };
        }

        internal bool UpdateCurve(State state)
        {
            return _ID switch
            {
                ID.Seg      => Segment_UpdateCurve(state),
                _           => Generic_UpdateCurve(state),
            };
        }

        /// <summary>
        /// Set the elevation component of the object, if it exists, and get the new position height value
        /// </summary>
        /// <param name="state">The state of the object to update</param>
        /// <param name="delta">The positional change from the Transform action</param>
        /// <returns>The new position height, adjusted for elevation component</returns>
        private void SetElevation(State state, float3 delta)
        {
            if (!TryGetElevation(out float elevation)) return;

            if (!TrySetElevation(elevation + delta.y)) return;

#if !USE_BURST && IS_DEBUG
            MIT.Log.Debug($"SetElevation({state.m_Entity.DX()}) el:{elevation} -> {elevation + delta.y}");
#endif
        }

        /// <summary>
        /// Create and destroy the Objects.Elevated component as needed
        /// </summary>
        /// <param name="elevation">The new elevation</param>
        /// <returns>Does an Elevated component now exist for this entity?</returns>
        private bool ManageStaticElevation(float elevation)
        {
            if (math.abs(elevation) < 0.0005f)
            {
                if (_Lookup.goElevation.HasComponent(m_Entity))
                {
                    _Manager.RemoveComponent<Game.Objects.Elevation>(m_Entity);
                }
                return false;
            }

            Game.Objects.ElevationFlags flag = elevation < 0f ? Game.Objects.ElevationFlags.Lowered : 0f;
            Game.Objects.Elevation component = new(elevation, flag);

            if (!_Lookup.goElevation.HasComponent(m_Entity))
            {
                _Manager.AddComponentData(m_Entity, component);
            }
            else
            {
                _Lookup.goElevation.GetRefRW(m_Entity).ValueRW = component;
            }

            return true;
        }


        /// <summary>
        /// Create and destroy the Net.Elevated component as needed
        /// </summary>
        /// <param name="elevation">The new elevation</param>
        /// <returns>Does an Elevated component now exist for this entity?</returns>
        private bool ManageNetworkElevation(float2 elevation)
        {
            if (math.abs(elevation.x) < 0.0005f && math.abs(elevation.y) < 0.0005f)
            {
                if (_Lookup.gnElevation.HasComponent(m_Entity))
                {
                    _Manager.RemoveComponent<Game.Net.Elevation>(m_Entity);
                }
                return false;
            }

            Game.Net.Elevation component = new(elevation);

            if (!_Lookup.gnElevation.HasComponent(m_Entity))
            {
                _Manager.AddComponentData(m_Entity, component);
            }
            else
            {
                _Lookup.gnElevation.GetRefRW(m_Entity).ValueRW = component;
            }

            return true;
        }
    }
}
