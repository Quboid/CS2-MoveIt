using Colossal.Entities;
using Colossal.Mathematics;
using Colossal.UI;
using Game.Common;
using Game.Tools;
using MoveIt.QAccessor;
using MoveIt.Tool;
using QCommonLib;
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace MoveIt.Moveables
{
    public struct State : IDisposable, INativeDisposable
    {
        /// <summary>
        /// The entity that this state represents
        /// </summary>
        internal Entity m_Entity;
        /// <summary>
        /// The QAccessor struct for entity access
        /// </summary>
        internal QObject m_Accessor;
        /// <summary>
        /// This entity's ultimate parent object
        /// </summary>
        internal Entity m_Parent;
        /// <summary>
        /// How this object relates to the parent (optional)
        /// </summary>
        internal short m_ParentKey;
        /// <summary>
        /// This object's prefab entity
        /// </summary>
        internal Entity m_Prefab;
        /// <summary>
        /// This object's final position in the game world
        /// </summary>
        internal float3 m_Position;
        /// <summary>
        /// This object's position at the start of the current action
        /// </summary>
        internal float3 m_InitialPosition;
        /// <summary>
        /// This object's final rotation in the game world
        /// </summary>
        internal quaternion m_Rotation;
        /// <summary>
        /// This object's rotation at the start of the current action
        /// </summary>
        internal quaternion m_InitialRotation;
        /// <summary>
        /// This object's identity (building/prop/plant/etc)
        /// </summary>
        internal Identity m_Identity;
        /// <summary>
        /// Is this object manipulatable?
        /// </summary>
        internal bool m_IsManipulatable;
        /// <summary>
        /// Is this object managed by Move It
        /// </summary>
        internal bool m_IsManaged;
        /// <summary>
        /// How far the object has moved this action
        /// </summary>
        internal float3 m_MoveDelta;
        /// <summary>
        /// How much the object has rotated this action (degrees, Y-axis)
        /// </summary>
        internal float m_AngleDelta;
        /// <summary>
        /// The initial center-point of this action
        /// </summary>
        internal float3 m_InitialCenter;
        /// <summary>
        /// The object's curve at the start of the current action (optional)
        /// </summary>
        internal Bezier4x3 m_InitialCurve;

        internal readonly MVDefinition Definition => new(m_Identity, m_Entity, m_IsManipulatable, m_IsManaged, m_Parent, m_ParentKey);

        internal State(EntityManager manager, ref QLookup lookup, Moveable mv, float3 moveDelta, float angleDelta, float3 centerPoint)
        {
            if (!manager.Exists(mv.m_Entity))
            {
                throw new Exception($"Creating Moveable State for missing entity {mv.m_Entity.D()}");
            }
            if (!manager.HasComponent<Game.Prefabs.PrefabRef>(mv.m_Entity))
            {
                throw new Exception($"Creating Moveable State but no PrefabRef found for {mv.m_Entity.D()}");
            }

            m_Entity            = mv.m_Entity;
            m_Accessor          = new(manager, ref lookup, m_Entity, mv.m_Identity);
            m_Parent            = mv.m_Parent;
            m_ParentKey         = mv.m_ParentKey;
            m_Prefab            = manager.GetComponentData<Game.Prefabs.PrefabRef>(m_Entity).m_Prefab;
            m_Position          = mv.Transform.m_Position;
            m_InitialPosition   = m_Position;
            m_Rotation          = mv.Transform.m_Rotation;
            m_InitialRotation   = m_Rotation;
            m_Identity          = mv.m_Identity;
            m_IsManipulatable   = mv.IsManipulatable;
            m_IsManaged         = mv.IsManaged;
            m_MoveDelta         = moveDelta;
            m_AngleDelta        = angleDelta;
            m_InitialCenter     = centerPoint;
            m_InitialCurve      = default;

            if (m_Identity == Identity.Segment || m_Identity == Identity.NetLane)
            {
                m_InitialCurve = manager.GetComponentData<Game.Net.Curve>(m_Entity).m_Bezier;
            }
        }

        /// <summary>
        /// Get a copy of this state with a new QAccessor
        /// </summary>
        /// <param name="manager">An EntityManager</param>
        /// <param name="lookup">Ref to the lookup table</param>
        /// <returns>The fresh copy</returns>
        internal State GetCopy(EntityManager manager, ref QLookup lookup)
        {
            return new State()
            {

                m_Entity            = this.m_Entity,
                m_Accessor          = new(manager, ref lookup, m_Entity, this.m_Identity),
                m_Parent            = this.m_Parent,
                m_ParentKey         = this.m_ParentKey,
                m_Prefab            = this.m_Prefab,
                m_Position          = this.m_Position,
                m_InitialPosition   = this.m_Position,
                m_Rotation          = this.m_Rotation,
                m_InitialRotation   = this.m_Rotation,
                m_Identity          = this.m_Identity,
                m_IsManipulatable   = this.m_IsManipulatable,
                m_IsManaged         = this.m_IsManaged,
                m_MoveDelta         = this.m_MoveDelta,
                m_AngleDelta        = this.m_AngleDelta,
                m_InitialCenter     = this.m_InitialCenter,
                m_InitialCurve      = this.m_InitialCurve,
            };
        }

        internal void UpdateEntity(EntityManager manager, ref QLookup lookup, Entity e)
        {
            m_Entity = e;
            m_Accessor.Dispose();
            m_Accessor = new(manager, ref lookup, m_Entity, m_Identity);
        }

        public void Transform(bool move, bool rotate)
        {
            m_Accessor.Transform(this, m_Position, m_Rotation, move, rotate);
        }

        public void TransformEnd(NativeArray<Entity> all)
        {
            m_Accessor.TransformEnd(all);
        }

        /// <summary>
        /// States can have owners, if they are extensions or service upgrades
        /// </summary>
        /// <param name="manager">An EntityManager struct</param>
        /// <param name="e">The Entity that the state refers to</param>
        /// <returns>Is it valid?</returns>
        public static bool IsValid(EntityManager manager, Entity e)
        {
            if (e.Equals(Entity.Null)) return false;
            if (!manager.Exists(e)) return false;
            if (!manager.HasComponent<Game.Prefabs.PrefabRef>(e)) return false;
            if (manager.HasComponent<Temp>(e)) return false;
            if (manager.HasComponent<Terrain>(e)) return false;
            if (manager.TryGetComponent<Game.Objects.Attached>(e, out var comp))
            {
                if (!comp.m_Parent.Equals(Entity.Null)) return false;
            }
            if (!(
                manager.HasComponent<Game.Objects.Transform>(e) ||
                manager.HasComponent<Game.Net.Edge>(e) ||
                manager.HasComponent<Game.Net.Node>(e) ||
                manager.HasComponent<Game.Areas.Surface>(e) ||
                manager.HasComponent<Game.Areas.Space>(e) ||
                manager.HasComponent<Components.MIT_ControlPoint>(e)
                )) return false;

            return true;
        }
        public readonly bool IsValid(EntityManager manager) => IsValid(manager, m_Entity);

        public void Dispose()
        {
            m_Accessor.Dispose();
        }


        public JobHandle Dispose(JobHandle handle)
        {
            return m_Accessor.Dispose(handle);
        }

        public override readonly string ToString()
        {
            return $"{m_Entity.D(),-9} {m_Identity,-12} {m_Position.DX(),22} / {m_Rotation.Y(),-6:0.##}";
        }

        public readonly string ToStringLong()
        {
            return $"{m_Entity.D(),-9} {m_Identity,-12} {m_Position.DX(),22} / {m_Rotation.Y(),-6:0.##} Prefab:{m_Prefab.D(),-10}";
        }

        public readonly void DebugDump()
        {
            MIT.Log.Debug(ToString());
        }
    }
}
