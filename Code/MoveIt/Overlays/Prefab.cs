//using Colossal.Mathematics;
//using Game.Prefabs;
//using MoveIt.QAccessor;
//using System;
//using Unity.Collections;
//using Unity.Entities;
//using Unity.Jobs;
//using Unity.Mathematics;

//namespace MoveIt.Overlays
//{
//    internal struct Prefab : IDisposable, INativeDisposable
//    {
//        private readonly static EntityManager EM = World.DefaultGameObjectInjectionWorld.EntityManager;
//        private const float MIN_DIAMETER = 3f;

//        public Entity m_Entity;
//        public Identity m_Identity;

//        public float m_Diameter = 0f;

//        public NativeArray<Bezier4x3>       m_Curves = new();
//        public NativeArray<Line3.Segment>   m_Lines = new();
//        public NativeArray<float3>          m_Vertices = new();

//        public Prefab(Entity e)
//        {
//            m_Entity = e;
//            m_Identity = QTypes.GetEntityIdentity(e);

//            switch (m_Identity)
//            {
//                case Identity.Plant:
//                case Identity.Roundabout:
//                case Identity.Other:
//                    ObjectGeometryData geoData = EM.GetComponentData<ObjectGeometryData>(m_Entity);
//                    m_Diameter = math.max(math.cmax(new float2(geoData.m_Size.x, geoData.m_Size.z)), MIN_DIAMETER);
//                    break;

//                case Identity.Building:
//                    break;

//                case Identity.ControlPoint:
//                case Identity.Segment:
//                case Identity.Node:
//                case Identity.SubEntity:
//                case Identity.Invalid:
//                case Identity.None:
//                default:
//                    break;
//            }

//            if (m_Identity == Identity.None || m_Identity == Identity.Invalid) return;


//        }


//        public void Dispose()
//        {
//            m_Curves.Dispose();
//            m_Lines.Dispose();
//            m_Vertices.Dispose();
//        }

//        public JobHandle Dispose(JobHandle handle)
//        {
//            handle = m_Curves.Dispose(handle);
//            handle = m_Lines.Dispose(handle);
//            handle = m_Vertices.Dispose(handle);
//            return handle;
//        }
//    }
//}
