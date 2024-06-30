using Colossal.Mathematics;
using MoveIt.QAccessor;
using MoveIt.Tool;
using System;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace MoveIt.Searcher
{
    internal class Marquee : SearcherBase
    {
        internal const int MAX_MARQUEE_RESULTS = 50;

        internal Marquee(Filters flags, bool isManipulating) : base(flags, isManipulating) { }

        /// <summary>
        /// Run the search for marquee
        /// </summary>
        /// <param name="outer">A map-align rectangle for quickly exluding irrelevant condidates</param>
        /// <param name="rect">The area to search within</param>
        internal void Search(Bounds2 outer, Quad2 rect)
        {
            SearchJob(SearchTypes.Marquee, outer, rect);
        }

        /// <summary>
        /// Run the search for bounds rectangle
        /// </summary>
        /// <param name="outer">A map-align rectangle to search</param>
        internal void Search(Bounds2 outer)
        {
            SearchJob(SearchTypes.Bounds, outer, default);
        }

        private void SearchJob(SearchTypes type, Bounds2 outer, Quad2 rect)
        {
            try
            {
                var staticTree = ObjSearch.GetStaticSearchTree(true, out JobHandle objSearchTreeHandle);
                var networkTree = NetSearch.GetNetSearchTree(true, out JobHandle netSearchTreeHandle);
                objSearchTreeHandle.Complete();
                netSearchTreeHandle.Complete();

                m_Results = new NativeList<Entity>(0, Allocator.TempJob);
                var controlPoints = new NativeArray<Components.MIT_ControlPoint>(_Tool.ControlPointManager.GetAllData().ToArray(), Allocator.TempJob);

                QLookupFactory.Init(_Tool);

                JobHandle marqueeHandle = JobHandle.CombineDependencies(objSearchTreeHandle, netSearchTreeHandle);
                MarqueeJob job = new()
                {
                    m_StaticTree        = staticTree,
                    m_NetworkTree       = networkTree,
                    m_ControlPoints     = controlPoints,
                    m_Filters           = m_Flags,
                    m_IsManipulating    = false,
                    m_Lookup            = QLookupFactory.Get(),
                    m_Manager           = World.DefaultGameObjectInjectionWorld.EntityManager,
                    m_Results           = m_Results,
                    m_SearchArea        = rect,
                    m_SearchBounds      = outer,
                    m_SearchType        = type,
                };
                job.Run();

                //string msg = $"MarqueeJob found:{m_Results.Length}";
                //for (int i = 0; i < m_Results.Length; i++)
                //{
                //    msg += $"\n    {m_Results[i].DX(true)}";
                //}
                //QLog.Debug(msg);

                controlPoints.Dispose();
            }
            catch (Exception ex)
            {
                MIT.Log.Error($"Marquee select failed.\n{ex}");
            }
        }
    }
}
