using Colossal.Mathematics;
using Colossal.PSI.Common;
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
        internal Entity m_Entity;
        internal QObject m_Accessor;
        internal Entity m_Parent;
        internal short m_ParentKey;
        internal Entity m_Prefab;
        internal float3 m_Position;
        internal float3 m_InitialPosition;
        internal quaternion m_Rotation;
        internal quaternion m_InitialRotation;
        internal float m_YOffset;
        internal float m_InitialYOffset;
        internal Identity m_Identity;
        internal ObjectType m_ObjectType;
        internal bool m_IsManipulatable;
        internal bool m_IsManaged;
        internal Bezier4x3 m_InitialCurve;

        internal readonly MVDefinition Definition => new(m_Identity, m_Entity, m_IsManipulatable, m_IsManaged, m_Parent, m_ParentKey);

        internal State(EntityManager manager, ref QLookup lookup, Moveable mv)
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
            m_YOffset           = mv.m_YOffset;
            m_InitialYOffset    = mv.m_YOffset;
            m_Identity          = mv.m_Identity;
            m_ObjectType        = mv.m_ObjectType;
            m_IsManipulatable   = mv.IsManipulatable;
            m_IsManaged         = mv.IsManaged;
            m_InitialCurve      = default;

            if (m_Identity == Identity.Segment || m_Identity == Identity.NetLane)
            {
                m_InitialCurve = manager.GetComponentData<Game.Net.Curve>(m_Entity).m_Bezier;
            }
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
            if (manager.HasComponent<Game.Objects.Attached>(e)) return false;
            if (!(
                manager.HasComponent<Game.Objects.Transform>(e) ||
                manager.HasComponent<Game.Net.Edge>(e) ||
                manager.HasComponent<Game.Net.Node>(e) ||
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
            return $"{m_Entity.D(),-9} {m_Identity,-12} {m_Position.DX(),22} / {m_Rotation.Y(),-6:0.##} Yoff:{m_YOffset,-5:0.##}";
        }

        public readonly string ToStringLong()
        {
            return $"{m_Entity.D(),-9} {m_Identity,-12} {m_Position.DX(),22} / {m_Rotation.Y(),-6:0.##} Yoff:{m_YOffset,-5:0.##} Prefab:{m_Prefab.D(),-10}";
        }

        public readonly void DebugDump()
        {
            QLog.Debug(ToString());
        }
    }
}
