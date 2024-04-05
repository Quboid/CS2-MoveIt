﻿using Unity.Entities;

namespace MoveIt.QAccessor
{
    public interface IQLookupContainer
    {
        void Init(SystemBase system);
        void Update(SystemBase system);
    }

    public struct QLookup : IQLookupContainer
    {
        public static QLookup m_Lookup;
        private static bool _Initialized;

        public static void Reset()
        {
            _Initialized = false;
        }

        public static ref QLookup Get(SystemBase system)
        {
            if (!_Initialized)
            {
                m_Lookup = new();
                m_Lookup.Init(system);
                _Initialized = true;
            }

            return ref m_Lookup;
        }


        internal BufferLookup<Game.Areas.Node> gaNode;
        //internal BufferLookup<Game.Net.ConnectedEdge> gnConnectedEdge;
        internal ComponentLookup<Game.Areas.Geometry> gaGeometry;
        internal ComponentLookup<Game.Net.Aggregated> gnAggregated;
        internal ComponentLookup<Game.Net.Curve> gnCurve;
        internal ComponentLookup<Game.Net.Edge> gnEdge;
        //internal ComponentLookup<Game.Net.EdgeGeometry> gnEdgeGeometry;
        //internal ComponentLookup<Game.Net.EndNodeGeometry> gnEndNodeGeometry;
        internal ComponentLookup<Game.Net.Node> gnNode;
        //internal ComponentLookup<Game.Net.NodeGeometry> gnNodeGeometry;
        //internal ComponentLookup<Game.Net.StartNodeGeometry> gnStartNodeGeometry;
        internal ComponentLookup<Game.Objects.Transform> goTransform;
        //internal ComponentLookup<Game.Prefabs.ObjectGeometryData> gpObjectGeometryData;
        //internal ComponentLookup<Game.Rendering.CullingInfo> grCullingInfo;
        internal ComponentLookup<Components.MIT_ControlPoint> MIT_ControlPoint;

        internal int test;

        public void Init(SystemBase system)
        {
            gaNode = system.GetBufferLookup<Game.Areas.Node>();
            gaGeometry = system.GetComponentLookup<Game.Areas.Geometry>();
            gnAggregated = system.GetComponentLookup<Game.Net.Aggregated>();
            gnCurve = system.GetComponentLookup<Game.Net.Curve>();
            gnEdge = system.GetComponentLookup<Game.Net.Edge>();
            gnNode = system.GetComponentLookup<Game.Net.Node>();
            goTransform = system.GetComponentLookup<Game.Objects.Transform>();
            MIT_ControlPoint = system.GetComponentLookup<Components.MIT_ControlPoint>();
        }

        public void Update(SystemBase system)
        {
            gaNode.Update(system);
            gaGeometry.Update(system);
            gnAggregated.Update(system);
            gnCurve.Update(system);
            gnEdge.Update(system);
            gnNode.Update(system);
            goTransform.Update(system);
            MIT_ControlPoint.Update(system);
        }
    }
}