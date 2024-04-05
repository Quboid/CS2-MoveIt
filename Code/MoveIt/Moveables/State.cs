using MoveIt.Components;
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
        public static Entity GetControlPoint(Entity seg, int idx)
        {
            NativeArray<Entity> entities = _Tool.m_ControlPointQuery.ToEntityArray(Allocator.Temp);
            NativeArray<MIT_ControlPoint> data = _Tool.m_ControlPointQuery.ToComponentDataArray<MIT_ControlPoint>(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                if (data[i].m_Segment == seg && data[i].m_CurveKey == idx)
                {
                    return entities[i];
                }
            }
            return Entity.Null;
        }

        private readonly static MIT _Tool = MIT.m_Instance;

        internal Entity m_Entity;
        internal QObject m_Accessor;
        internal Entity m_Prefab;
        internal float3 m_Position;
        internal float3 m_InitialPosition;
        internal float m_Angle;
        internal float m_InitialAngle;
        internal float m_YOffset;
        internal float m_InitialYOffset;
        internal QTypes.Identity m_Identity;
        internal QTypes.ObjectType m_ObjectType;
        internal QTypes.Manipulate m_Manipulatable;
        internal StateDataWrapper m_Data;

        internal State(Moveable mv, SystemBase system)
        {
            if (!_Tool.EntityManager.Exists(mv.m_Entity))
            {
                throw new Exception($"Creating Moveable State for missing entity {mv.m_Entity.D()}");
            }
            if (!_Tool.EntityManager.HasComponent<Game.Prefabs.PrefabRef>(mv.m_Entity))
            {
                throw new Exception($"Creating Moveable State but no PrefabRef found for {mv.m_Entity.D()}");
            }

            m_Entity                    = mv.m_Entity;
            m_Accessor                  = new(m_Entity, system, mv.m_Identity);
            m_Prefab                    = _Tool.EntityManager.GetComponentData<Game.Prefabs.PrefabRef>(m_Entity).m_Prefab;
            m_Position                  = mv.Transform.m_Position;
            m_InitialPosition           = m_Position;
            m_Angle                     = mv.Transform.Y();
            m_InitialAngle              = m_Angle;
            m_YOffset                   = mv.m_YOffset;
            m_InitialYOffset            = mv.m_YOffset;
            m_Identity                  = mv.m_Identity;
            m_ObjectType                = mv.m_ObjectType;
            m_Manipulatable             = mv.m_Manipulatable;

            m_Data = new StateDataWrapper(_Tool.EntityManager, m_Entity, m_Identity);
        }

        internal void UpdateEntity(Entity e, SystemBase system)
        {
            m_Entity = e;
            m_Accessor.Dispose();
            m_Accessor = new(m_Entity, system, m_Identity);
        }

        public void Transform(bool move, bool rotate)
        {
            //if (rotate) QLog.Debug($"State.Trans {m_Entity.D()}-{m_Identity} {m_Position.DX()} - {m_Angle} <{m_Data.Get().GetType()}>");
            //else QLog.Debug($"State.Trans {m_Entity.D()}-{m_Identity} {m_Position.DX()} <{m_Data.Get().GetType()}> {m_Data.Debug()}");

            m_Accessor.Transform(m_Data, m_Position, m_Angle, move, rotate);
        }

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
            return $"{m_Entity.D(),-9} {m_Identity,-12} {m_Position.DX(),22} / {m_Angle,-6:0.##} Yoff:{m_YOffset,-5:0.##} Prefab:{m_Prefab.D(),-10} Data:{m_Data}";
        }

        public readonly void DebugDump()
        {
            QLog.Debug(ToString());
        }
    }
}
