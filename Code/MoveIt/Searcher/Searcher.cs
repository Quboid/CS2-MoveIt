using Colossal.Mathematics;
using Game.Common;
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
    internal class Searcher : IDisposable, INativeDisposable
    {
        protected static readonly MIT _Tool = MIT.m_Instance;

        protected QLookup _Lookup;

        protected NativeList<Entity> _Entities;
        protected bool _DoSort;
        protected Filters _Filters;
        protected bool _IsManipulating;
        protected float3 _TerrainPosition;
        protected NativeArray<RaycastResult> _NetworkResults;
        protected NativeArray<RaycastResult> _SurfaceResults;

        protected SearchTypes _Type;
        protected Quad2 _Marquee        = default;
        protected Bounds2 _Bounds       = default;
        protected float3 _Point         = default;
        protected Line3.Segment _Ray    = default;

        internal NativeArray<Result> m_Results;

        internal Searcher(Filters filters, bool isManipulating, float3 terrainPosition)
        {
            _Filters            = filters;
            _IsManipulating     = isManipulating;
            _TerrainPosition    = terrainPosition;
            _NetworkResults     = new NativeArray<RaycastResult>();
            _SurfaceResults     = new NativeArray<RaycastResult>();

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

        internal static Game.Areas.SearchSystem AreaSearch
        {
            get
            {
                _AreaSearch ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Areas.SearchSystem>();
                return _AreaSearch;
            }
        }
        private static Game.Areas.SearchSystem _AreaSearch;

        /// <summary>
        /// Run the search for marquee
        /// </summary>
        /// <param name="outer">A map-aligned rectangle for quickly exluding irrelevant condidates</param>
        /// <param name="rect">The area to search within</param>
        /// <param name="doSort">Do these results need to be sorted?</param>
        internal void SearchMarquee(Bounds2 outer, Quad2 rect, bool doSort = false)
        {
            _Type           = SearchTypes.Marquee;
            _Bounds         = outer;
            _Marquee        = rect;
            _DoSort         = doSort;
            Execute();
        }

        /// <summary>
        /// Run the search for bounds rectangle
        /// </summary>
        /// <param name="outer">A map-aligned rectangle to search</param>
        /// <param name="doSort">Do these results need to be sorted?</param>
        internal void SearchBounds(Bounds2 outer, bool doSort = false)
        {
            _Type           = SearchTypes.Bounds;
            _Bounds         = outer;
            _DoSort         = doSort;
            Execute();
        }

        /// <summary>
        /// Run the search for a single point
        /// </summary>
        /// <param name="point">A single point in the game world</param>
        /// <param name="doSort">Do these results need to be sorted?</param>
        internal void SearchPoint(float3 point, bool doSort = true)
        {
            _Type           = SearchTypes.Point;
            _Point          = point;
            _DoSort         = doSort;
            Execute();
        }

        /// <summary>
        /// Run the search for a ray
        /// </summary>
        /// <param name="ray">The ray line</param>
        /// <param name="doSort">Do these results need to be sorted?</param>
        internal void SearchRay(Line3.Segment ray, NativeArray<RaycastResult> networkResults, NativeArray<RaycastResult> surfaceResults, bool doSort = true)
        {
            _Type           = SearchTypes.Ray;
            _Ray            = ray;
            _DoSort         = doSort;
            _NetworkResults = networkResults;
            _SurfaceResults = surfaceResults;
            Execute();
        }


        private void Execute()
        {
            try
            {
                var staticTree = ObjSearch.GetStaticSearchTree(true, out JobHandle objSearchTreeHandle);
                var networkTree = NetSearch.GetNetSearchTree(true, out JobHandle netSearchTreeHandle);
                var areaTree = AreaSearch.GetSearchTree(true, out JobHandle netAreaTreeHandle);
                objSearchTreeHandle.Complete();
                netSearchTreeHandle.Complete();
                netAreaTreeHandle.Complete();

                _Entities = new NativeList<Entity>(0, Allocator.TempJob);
                var controlPoints = new NativeArray<Components.MIT_ControlPoint>(_Tool.ControlPointManager.GetAllData(_IsManipulating).ToArray(), Allocator.TempJob);

                QLookupFactory.Init(_Tool);

                // Do the main search job
                JobHandle searchHandle = JobHandle.CombineDependencies(objSearchTreeHandle, netSearchTreeHandle);
                SearcherJob job = new()
                {
                    m_StaticTree        = staticTree,
                    m_NetworkTree       = networkTree,
                    m_AreaTree          = areaTree,
                    m_ControlPoints     = controlPoints,
                    m_Filters           = _Filters,
                    m_IsManipulating    = _IsManipulating,
                    m_Lookup            = QLookupFactory.Get(),
                    m_Manager           = World.DefaultGameObjectInjectionWorld.EntityManager,
                    m_Results           = _Entities,
                    m_TerrainPosition   = _TerrainPosition,
                    m_SearchType        = _Type,
                    m_SearchRect        = _Marquee,
                    m_SearchOuterBounds = _Bounds,
                    m_SearchPoint       = _Point,
                    m_SearchRay         = _Ray,
                };
                job.Run();

                // Add valid vanilla network results
                if (_Type == SearchTypes.Ray && (_Filters & Utils.FilterAllNetworks) != 0)
                {
                    foreach (var result in _NetworkResults)
                    {
                        if (ValidVanillaRaycast(_Tool.EntityManager, result))
                        {
                            _Entities.Add(result.m_Owner);
                        }
                    }
                }

                // Add valid vanilla surface results
                if (_Type == SearchTypes.Ray && (_Filters & Filters.Surfaces) != 0)
                {
                    foreach (var result in _SurfaceResults)
                    {
                        if (ValidVanillaRaycast(_Tool.EntityManager, result))
                        {
                            _Entities.Add(result.m_Owner);
                        }
                    }
                }

                // Register and sort the results
                m_Results = new NativeArray<Result>(Count, Allocator.TempJob);
                if (HasSearchResults())
                {
                    for (int i = 0; i < Count; i++)
                    {
                        Entity e = _Entities[i];
                        Result r = new()
                        {
                            m_Entity    = e,
                            m_Identity  = _DoSort ? QTypes.GetEntityIdentity(e) : Identity.Invalid,
                            m_Distance  = _DoSort ? GetDistance(e) : 0f,
                        };
                        m_Results[i] = r;
                    }
                    if (_DoSort)
                    {
                        m_Results.Sort();
                    }
                }

                // Cleanup
                controlPoints.Dispose();

                //DebugDumpSearchResultsBundle("SEARCH", true);
            }
            catch (Exception ex)
            {
                MIT.Log.Error($"Search {_Type} failed.\n{ex}");
            }
        }

        private bool ValidVanillaRaycast(EntityManager manager, RaycastResult result)
        {
            bool validType = QTypes.GetEntityIdentity(result.m_Owner) switch
            {
                Identity.NetLane => true,
                Identity.Segment => true,
                Identity.Surface => true,
                _ => false,
            };
            if (!validType) return false;

            if (manager.HasComponent<Owner>(result.m_Owner)) return false;

            return true;
        }

        private bool HasSearchResults() =>
            !(Count == 0 || (Count == 1 && _Entities[0].Equals(Entity.Null)));

        internal bool Has(Entity e) => _Entities.Contains(e);

        internal int Count => _Entities.Length;


        /// <summary>
        /// Get this object center's distance from the search area
        /// Marquee/Bounds: distance from edge of search box, 0 if center is inside
        /// Point: distance between points
        /// Ray: distance to closest point on ray line
        /// </summary>
        /// <param name="e">Entity get distance for</param>
        /// <returns>The distance in metres</returns>
        private float GetDistance(Entity e)
        {
            float distance = 0f;

            QObjectSimple accessor = new(_Tool.EntityManager, ref _Lookup, e);
            float3 position = accessor.m_Parent.Position;
            float2 pos2d = new(position.x, position.z);

            switch (_Type)
            {
                case SearchTypes.Marquee:
                    if (!MathUtils.Intersect(_Marquee, pos2d))
                    {
                        distance = math.abs(MathUtils.Distance(_Marquee.ab, pos2d, out _));
                        float d = math.abs(MathUtils.Distance(_Marquee.bc, pos2d, out _));
                        if (d < distance) distance = d;
                        d = math.abs(MathUtils.Distance(_Marquee.cd, pos2d, out _));
                        if (d < distance) distance = d;
                        d = math.abs(MathUtils.Distance(_Marquee.da, pos2d, out _));
                        if (d < distance) distance = d;
                    }
                    break;

                case SearchTypes.Bounds:
                    distance = math.abs(MathUtils.Distance(_Bounds, pos2d));
                    break;

                case SearchTypes.Point:
                    distance = math.abs(math.distance(_Point, position));
                    break;

                case SearchTypes.Ray:
                    distance = math.abs(MathUtils.Distance(_Ray, position, out _));
                    break;
            }

            return distance;
        }


        public virtual void Dispose()
        {
            _Entities.Dispose();
            m_Results.Dispose();
        }

        public virtual JobHandle Dispose(JobHandle handle)
        {
            handle = _Entities.Dispose(handle);
            handle = m_Results.Dispose(handle);
            return handle;
        }

        ~Searcher()
        {
            Dispose();
        }



        #region Debug

        internal string DebugSearchResults(bool full = false)
        {
            int netCount = _NetworkResults.Count(res => ValidVanillaRaycast(_Tool.EntityManager, res));
            int surCount = _SurfaceResults.Count(res => ValidVanillaRaycast(_Tool.EntityManager, res));

            StringBuilder sb = new();
            sb.AppendFormat("Search results: {0}; Network results: {1}/{2}; Surface results: {1}/{2}", Count, netCount, _NetworkResults.Length, surCount, _SurfaceResults.Length);
            if (Count < 1) return sb.ToString();

            sb.Append("\n    ");
            Dictionary<string, int> results = new();
            for (int i = 0; i < Count; i++)
            {
                string code = QTypes.GetIdentityCode(QTypes.GetEntityIdentity(_Entities[i]));
                if (!results.ContainsKey(code))
                {
                    results[code] = 0;
                }
                results[code]++;
            }
            foreach ((string code, int c) in results)
            {
                sb.AppendFormat("{0}:{1},  ", code, c);
            }

            if (full)
            {
                sb.AppendFormat("\nAll {0} results:", m_Results.Length);
                for (int i = 0; i < m_Results.Length; i++)
                {
                    sb.AppendFormat("\n    {0} ({1}m, {2})", m_Results[i].m_Entity.DX(true), m_Results[i].m_Distance, m_Results[i].m_Identity);
                }
            }

            return sb.ToString();
        }

        internal void DebugDumpSearchResults(bool full = false, string prefix = "")
        {
            QLog.Debug(prefix + DebugSearchResults(full));
        }

        internal void DebugDumpSearchResultsBundle(string key, bool full = false, string prefix = "")
        {
            if (HasSearchResults())
            {
                QLog.Bundle(key, prefix + DebugSearchResults(full));
            }
        }

        //internal static void DebugDumpCalculateDistance((Entity e, float d)[] entities)
        //{
        //    StringBuilder sb = new();
        //    sb.AppendFormat("Nearby entity distances: {0}", entities.Length);
        //    foreach ((Entity e, float distance) in entities)
        //    {
        //        sb.AppendFormat("\n    {0:3.##}: {1}", distance, e.DX());
        //    }
        //    MIT.Log.Debug(sb.ToString());
        //}

        #endregion
    }
}
