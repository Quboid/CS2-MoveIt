using Colossal.Mathematics;
using MoveIt.QAccessor;
using MoveIt.Tool;
using QCommonLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace MoveIt.Searcher
{
    internal enum Filters
    {
        None = 0,
        Buildings = 1,
        Plants = 2,
        Props = 4,
        Decals = 8,
        AllStatics = 15,
        Nodes = 16,
        Segments = 32,
        ControlPoints = 64,
        AllNets = 112,
        All = 127,
    }

    internal enum SearchTypes
    {
        Point,
        Marquee,
        Bounds,
        Ray,
    }

    internal abstract class SearcherBase : IDisposable, INativeDisposable
    {
        protected static readonly MIT _Tool = MIT.m_Instance;

        protected QLookup _Lookup;

        internal Filters m_Flags;
        internal bool m_IsManipulating;
        internal NativeList<Entity> m_Results;

        internal SearcherBase(Filters flags, bool isManipulating)
        {
            m_Flags = flags;
            m_IsManipulating = isManipulating;
            QLookupFactory.Init(_Tool);
            _Lookup = QLookupFactory.Get();
        }

        internal static Game.Objects.SearchSystem ObjSearch
        {
            get
            {
                _ObjSearch ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Objects.SearchSystem>();
                return _ObjSearch;
            }
        }
        private static Game.Objects.SearchSystem _ObjSearch;

        internal static Game.Net.SearchSystem NetSearch
        {
            get
            {
                _NetSearch ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Net.SearchSystem>();
                return _NetSearch;
            }
        }
        private static Game.Net.SearchSystem _NetSearch;

        internal (Entity e, float d)[] CalculateDistances(float3 center)
        {
            (Entity e, float d)[] data = new (Entity e, float d)[m_Results.Length];
            for (int i = 0; i < m_Results.Length; i++)
            {
                QObjectSimple obj = new(_Tool.EntityManager, ref _Lookup, m_Results[i]);
                float distance = obj.m_Parent.Position.DistanceXZ(center);
                data[i] = (m_Results[i], distance);
            }
            m_Results.Dispose();
            var result = data.OrderBy(pair => pair.d).ToArray();
            //DebugDumpCalculateDistance(result);
            return result;
        }

        public virtual void Dispose()
        {
            m_Results.Dispose();
        }

        public virtual JobHandle Dispose(JobHandle handle)
        {
            return m_Results.Dispose(handle);
        }

        ~SearcherBase()
        {
            m_Results.Dispose();
        }

        internal static Quad2 CalculateBuildingCorners(EntityManager manager, ref QObject obj, Entity prefab, float expand = 0f)
        {
            return CalculateBuildingCorners(manager, obj.m_Parent.Position.XZ(), obj.m_Parent.Rotation, prefab, expand);
        }

        internal static Quad2 CalculateBuildingCorners(EntityManager manager, ref QObjectSimple obj, Entity prefab, float expand = 0f)
        {
            return CalculateBuildingCorners(manager, obj.m_Parent.Position.XZ(), obj.m_Parent.Rotation, prefab, expand);
        }

        internal static Quad2 CalculateBuildingCorners(EntityManager manager, float2 position, quaternion rotation, Entity prefab, float expand = 0f)
        {
            int2 lotSize = manager.GetComponentData<Game.Prefabs.BuildingData>(prefab).m_LotSize;
            float offX = lotSize.x * 4 + expand;
            float offZ = lotSize.y * 4 + expand;

            Quad2 result = new(
                RotateAroundPivot(manager, position, rotation, new(-offX, 0, -offZ)),
                RotateAroundPivot(manager, position, rotation, new(offX, 0, -offZ)),
                RotateAroundPivot(manager, position, rotation, new(offX, 0, offZ)),
                RotateAroundPivot(manager, position, rotation, new(-offX, 0, offZ)));

            return result;
        }

        internal static float2 RotateAroundPivot(EntityManager manager, float2 position, quaternion q, float3 offset)
        {
            float3 newPos = math.mul(q, offset);
            return position + new float2(newPos.x, newPos.z);
        }


        #region Debug

        internal string DebugSearchResults()
        {
            StringBuilder sb = new();
            sb.AppendFormat("Search results: {0}", m_Results.Length);
            Dictionary<string, int> results = new();
            for (int i = 0; i < m_Results.Length; i++)
            {
                string code = QTypes.GetIdentityCode(QTypes.GetEntityIdentity(m_Results[i]));
                if (!results.ContainsKey(code))
                {
                    results[code] = 0;
                }
                results[code]++;
            }
            foreach ((string code, int c) in results)
            {
                sb.AppendFormat("\n    {0}: {1}", code, c);
            }
            return sb.ToString();
        }

        internal void DebugDumpSearchResults(string prefix = "")
        {
            QLog.Debug(prefix + DebugSearchResults());
        }

        internal static void DebugDumpCalculateDistance((Entity e, float d)[] entities)
        {
            StringBuilder sb = new();
            sb.AppendFormat("Nearby entity distances: {0}", entities.Length);
            foreach ((Entity e, float distance) in entities)
            {
                sb.AppendFormat("\n    {0:3.##}: {1}", distance, e.DX());
            }
            MIT.Log.Debug(sb.ToString());
        }

        #endregion
    }
}
