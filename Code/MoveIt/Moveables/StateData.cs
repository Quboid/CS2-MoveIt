using Colossal.Mathematics;
using MoveIt.Components;
using MoveIt.Tool;
using QCommonLib;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace MoveIt.Moveables
{
    public interface IStateData : INativeDisposable
    {
        public Entity m_Entity { get; set; }
    }


    public struct StateOther : IStateData
    {
        public Entity m_Entity { get; set; }

        internal StateOther(EntityManager entityManager, Entity e, QTypes.Identity identity)
        {
            m_Entity = e;
        }

        public override readonly string ToString()
        {
            return $"{m_Entity.D()}O";
        }

        public void Dispose() { }
        public JobHandle Dispose(JobHandle handle) => handle;
    }

    public struct StateSegment : IStateData
    {
        private readonly static MIT _Tool = MIT.m_Instance;

        public Entity m_Entity { get; set; }
        internal Bezier4x3 m_InitialCurve;

        internal StateSegment(EntityManager entityManager, Entity e)
        {
            m_Entity = e;

            m_InitialCurve = entityManager.GetComponentData<Game.Net.Curve>(e).m_Bezier;
        }

        public override readonly string ToString()
        {
            return $"{m_Entity.D()}S-([{m_InitialCurve.a.D()}],[{m_InitialCurve.b.D()}],[{m_InitialCurve.c.D()}],[{m_InitialCurve.d.D()}])";
        }

        public readonly void Dispose() { }
        public readonly JobHandle Dispose(JobHandle handle) => handle;
    }

    public struct StateBuilding : IStateData
    {
        private readonly static MIT _Tool = MIT.m_Instance;

        public Entity m_Entity { get; set; }
        internal Entity m_OldRoad;

        internal StateBuilding(EntityManager entityManager, Entity e)
        {
            m_Entity = e;

            m_OldRoad = entityManager.GetComponentData<Game.Buildings.Building>(e).m_RoadEdge;
        }

        public override readonly string ToString()
        {
            return $"{m_Entity.D()}S-({m_OldRoad.DX(true)})";
        }

        public readonly void Dispose() { }
        public readonly JobHandle Dispose(JobHandle handle) => handle;
    }

    public struct StateControlPoint : IStateData
    {
        private readonly static MIT _Tool = MIT.m_Instance;

        public Entity m_Entity { get; set; }
        internal short m_Curvekey;
        internal Entity m_Segment;
        internal int m_EntityIndex;
        internal int m_EntityVersion;

        internal StateControlPoint(EntityManager entityManager, Entity e)
        {
            m_Entity = e;
            m_EntityIndex = e.Index;
            m_EntityVersion = e.Version;

            m_Curvekey = entityManager.GetComponentData<MIT_ControlPoint>(e).m_CurveKey;
            m_Segment = entityManager.GetComponentData<MIT_ControlPoint>(e).m_Segment;
        }

        public override readonly string ToString()
        {
            string exists = _Tool.EntityManager.Exists(m_Entity) ? "Valid" : "Invalid";
            return $"cp:{m_Entity.DX()} {exists} (E{m_EntityIndex}:{m_EntityVersion}) s:{m_Segment.D()}:{m_Curvekey}";
        }

        public readonly void Dispose() { }
        public readonly JobHandle Dispose(JobHandle handle) => handle;
    }


    public struct StateDataWrapper
    {
        internal QTypes.Identity m_Identity;
        internal EntityManager m_EntityManager;
        internal Entity m_Entity;

        internal readonly StateOther Other => _Other;
        internal readonly StateControlPoint ControlPoint => _ControlPoint;
        internal readonly StateSegment Segment => _Segment;
        internal readonly StateBuilding Building => _Building;
        private readonly StateOther _Other;
        private readonly StateControlPoint _ControlPoint;
        private readonly StateSegment _Segment;
        private readonly StateBuilding _Building;

        public StateDataWrapper(EntityManager entityManager, Entity e, QTypes.Identity identity)
        {
            m_EntityManager = entityManager;
            m_Entity        = e;
            m_Identity      = identity;

            _Other          = new(m_EntityManager, m_Entity, identity);
            _ControlPoint   = new(m_EntityManager, m_Entity);
            _Segment        = new(m_EntityManager, m_Entity);
            _Building       = new(m_EntityManager, m_Entity);
        }

        public readonly IStateData Get() => m_Identity switch
            {
                QTypes.Identity.Segment         => _Segment,
                QTypes.Identity.ControlPoint    => _ControlPoint,
                QTypes.Identity.Building        => _Building,
                _ => _Other,
            };

        public override readonly string ToString() => Get().ToString();
    }
}
