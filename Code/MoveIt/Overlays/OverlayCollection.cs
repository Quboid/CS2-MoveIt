using QCommonLib;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;

namespace MoveIt.Overlays
{
    internal struct OverlayCollection : IDisposable, INativeDisposable
    {
        private QNativeList<Utils.Circle> m_Circles;
        private QNativeList<Utils.Line> m_Lines;
        private QNativeList<Utils.Bounds> m_Bounds;
        private QNativeList<Utils.Rectangle> m_Rectangles;
        private QNativeList<Utils.Point> m_Points;
        private QNativeList<Utils.Diamond> m_Diamond;
        private QNativeList<Moveables.ControlPoint.OverlayCP> m_ControlPoints;
        private QNativeList<Moveables.ControlPoint.OverlayCPFromParent> m_CPsFromParent;
        private QNativeList<Moveables.Node.OverlayNode> m_Nodes;
        private QNativeList<Moveables.Segment.OverlaySegmentNormal> m_SegmentsNormal;
        private QNativeList<Moveables.Segment.OverlaySegmentManipulating> m_SegmentsManipulate;
        private QNativeList<Moveables.Building.OverlayBuilding> m_Buildings;

        public OverlayCollection()
        {
            m_Circles = new(0, Allocator.TempJob);
            m_Lines = new(0, Allocator.TempJob);
            m_Bounds = new(0, Allocator.TempJob);
            m_Rectangles = new(0, Allocator.TempJob);
            m_Points = new(0, Allocator.TempJob);
            m_Diamond = new(0, Allocator.TempJob);
            m_ControlPoints = new(0, Allocator.TempJob);
            m_CPsFromParent = new(0, Allocator.TempJob);
            m_Nodes = new(0, Allocator.TempJob);
            m_SegmentsNormal = new(0, Allocator.TempJob);
            m_SegmentsManipulate = new(0, Allocator.TempJob);
            m_Buildings = new(0, Allocator.TempJob);
        }

        public readonly void Add(Utils.IOverlay overlay)
        {
            switch (overlay)
            {
                case Utils.Circle circle:
                    m_Circles.Add(circle);
                    break;
                case Utils.Line line:
                    m_Lines.Add(line);
                    break;
                case Utils.Bounds bounds:
                    m_Bounds.Add(bounds);
                    break;
                case Utils.Rectangle rectangle:
                    m_Rectangles.Add(rectangle);
                    break;
                case Utils.Point points:
                    m_Points.Add(points);
                    break;
                case Utils.Diamond diamond:
                    m_Diamond.Add(diamond);
                    break;
                case Moveables.ControlPoint.OverlayCP cp:
                    m_ControlPoints.Add(cp);
                    break;
                case Moveables.ControlPoint.OverlayCPFromParent cpp:
                    m_CPsFromParent.Add(cpp);
                    break;
                case Moveables.Node.OverlayNode node:
                    m_Nodes.Add(node);
                    break;
                case Moveables.Segment.OverlaySegmentNormal segn:
                    m_SegmentsNormal.Add(segn);
                    break;
                case Moveables.Segment.OverlaySegmentManipulating segm:
                    m_SegmentsManipulate.Add(segm);
                    break;
                case Moveables.Building.OverlayBuilding building:
                    m_Buildings.Add(building);
                    break;
            }
        }
        
        public readonly void Add(HashSet<Utils.IOverlay> overlays)
        {
            foreach (var overlay in overlays)
            {
                Add(overlay);
            }
        }

        public readonly int Count =>
            m_Circles.Length + 
            m_Lines.Length + 
            m_Bounds.Length + 
            m_Rectangles.Length + 
            m_Points.Length + 
            m_Diamond.Length +
            m_ControlPoints.Length + 
            m_CPsFromParent.Length +
            m_Nodes.Length + 
            m_SegmentsNormal.Length +
            m_SegmentsManipulate.Length + 
            m_Buildings.Length;

        public readonly Utils.IOverlay Get(int index)
        {
            if (index < m_Circles.Length) return m_Circles[index];
            index -= m_Circles.Length;

            if (index < m_Lines.Length) return m_Lines[index];
            index -= m_Lines.Length;

            if (index < m_Bounds.Length) return m_Bounds[index];
            index -= m_Bounds.Length;

            if (index < m_Rectangles.Length) return m_Rectangles[index];
            index -= m_Rectangles.Length;

            if (index < m_Points.Length) return m_Points[index];
            index -= m_Points.Length;

            if (index < m_Diamond.Length) return m_Diamond[index];
            index -= m_Diamond.Length;

            if (index < m_ControlPoints.Length) return m_ControlPoints[index];
            index -= m_ControlPoints.Length;

            if (index < m_CPsFromParent.Length) return m_CPsFromParent[index];
            index -= m_CPsFromParent.Length;

            if (index < m_Nodes.Length) return m_Nodes[index];
            index -= m_Nodes.Length;

            if (index < m_SegmentsNormal.Length) return m_SegmentsNormal[index];
            index -= m_SegmentsNormal.Length;

            if (index < m_SegmentsManipulate.Length) return m_SegmentsManipulate[index];
            index -= m_SegmentsManipulate.Length;

            if (index < m_Buildings.Length) return m_Buildings[index];

            return null;
        }

        public readonly void Dispose()
        {
            m_Circles.Dispose();
            m_Lines.Dispose();
            m_Bounds.Dispose();
            m_Rectangles.Dispose();
            m_Points.Dispose();
            m_Diamond.Dispose();
            m_ControlPoints.Dispose();
            m_CPsFromParent.Dispose();
            m_Nodes.Dispose();
            m_SegmentsNormal.Dispose();
            m_SegmentsManipulate.Dispose();
            m_Buildings.Dispose();
        }

        public readonly JobHandle Dispose(JobHandle handle)
        {
            handle = m_Circles.Dispose(handle);
            handle = m_Lines.Dispose(handle);
            handle = m_Bounds.Dispose(handle);
            handle = m_Rectangles.Dispose(handle);
            handle = m_Points.Dispose(handle);
            handle = m_Diamond.Dispose(handle);
            handle = m_ControlPoints.Dispose(handle);
            handle = m_CPsFromParent.Dispose(handle);
            handle = m_Nodes.Dispose(handle);
            handle = m_SegmentsNormal.Dispose(handle);
            handle = m_SegmentsManipulate.Dispose(handle);
            return m_Buildings.Dispose(handle);
        }
    }
}
