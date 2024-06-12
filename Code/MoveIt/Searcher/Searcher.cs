using Colossal.Mathematics;
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
        AllObjects = 3,
        Nodes = 4,
        Segments = 8,
        ControlPoints = 16,
        AllNets = 28,
        All = 31,
    }

    internal enum SelectionTypes
    {
        Point,
        Marquee,
        Ray,
    }

    internal abstract class Base : IDisposable, INativeDisposable
    {
        protected static readonly MIT _Tool = MIT.m_Instance;

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

        internal Filters m_Flags;
        internal bool m_IsManipulating;
        internal NativeList<Entity> m_Results;

        internal Base(Filters flags, bool isManipulating)
        {
            m_Flags = flags;
            m_IsManipulating = isManipulating;
        }

        internal (Entity e, float d)[] CalculateDistances(float3 center)
        {
            (Entity e, float d)[] data = new (Entity e, float d)[m_Results.Length];
            for (int i = 0; i < m_Results.Length; i++)
            {
                QAccessor.QObjectSimple obj = new(m_Results[i], _Tool);
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

        ~Base()
        {
            m_Results.Dispose();
        }


        #region Debug

        internal static void DebugDumpSearchResults(HashSet<Entity> list, Bounds3 bounds)
        {
            (int b, int p, int n, int s, int u) count = (0, 0, 0, 0, 0);

            //_Tool.AddDebugBounds(bounds, Overlays.Colors.GetForced(Overlays.OverlayFlags.Deselect));

            StringBuilder sb = new();
            sb.AppendFormat("Nearby entities: {0}", list.Count);
            foreach (Entity e in list)
            {
                if (_Tool.EntityManager.HasComponent<Game.Buildings.Building>(e))
                {
                    count.b++;
                }
                else if (_Tool.EntityManager.HasComponent<Game.Objects.Plant>(e))
                {
                    count.p++;
                }
                else if (_Tool.EntityManager.HasComponent<Game.Net.Node>(e))
                {
                    count.n++;
                }
                else if (_Tool.EntityManager.HasComponent<Game.Net.Edge>(e))
                {
                    count.s++;
                }
                else
                {
                    count.u++;
                }
            }
            sb.AppendFormat(": b:{0}, p:{1}, n:{2}, s:{3}, ?:{4}", count.b, count.p, count.n, count.s, count.u);
            MIT.Log.Debug(sb.ToString());
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
