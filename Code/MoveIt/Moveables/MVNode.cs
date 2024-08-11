using Colossal.Mathematics;
using MoveIt.Actions.Transform;
using MoveIt.Overlays;
using MoveIt.QAccessor;
using MoveIt.Tool;
using QCommonLib;
using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace MoveIt.Moveables
{
    public class MVLaneNode : MVNode
    {
        private const float RADIUS = 2f;

        public MVLaneNode(Entity e) : base(e, Identity.Node)
        {
            m_Overlay = Factory.Create<OverlayNode>(this, OverlayTypes.MVNode);
            Refresh();
        }

        internal override float GetRadius()
        {
            return RADIUS;
        }
    }

    public class MVNode : Moveable
    {
        /// <summary>
        /// The segments (edges) attached to this segment; Entity -> IsStart
        /// IsStart = true when this node is that edge's m_Start node, false for m_End
        /// </summary>
        internal Dictionary<Entity, bool> m_Segments;
        internal List<MVDefinition> m_CPDefinitions;

        public override Game.Objects.Transform Transform
        {
            get
            {
                var netTransform = _MIT.EntityManager.GetComponentData<Game.Net.Node>(m_Entity);
                return new()
                {
                    m_Position = netTransform.m_Position,
                    m_Rotation = netTransform.m_Rotation,
                };
            }
        }

        public MVNode(Entity e) : base(e, Identity.Node)
        {
            m_Overlay = Factory.Create<OverlayNode>(this, OverlayTypes.MVNode);
            Refresh();
        }

        public MVNode(Entity e, Identity identity) : base(e, identity)
        { } // Pass-thru for children

        internal override bool Refresh()
        {
            if (!IsValid) return false;
            if (!IsOverlayValid) return false;

            m_Segments = new();
            m_CPDefinitions = new();

            if (TryGetBuffer<Game.Net.ConnectedEdge>(out var buffer, true))
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    var edge = buffer[i].m_Edge;
                    bool isStart = _MIT.EntityManager.GetComponentData<Game.Net.Edge>(edge).m_Start.Equals(m_Entity);
                    m_Segments.Add(edge, isStart);

                    MVDefinition mvd = new(Identity.ControlPoint, Entity.Null, false, IsManaged, edge, (short)(isStart ? 0 : 3));
                    MVControlPoint cp = _MIT.ControlPointManager.GetOrCreate(mvd);
                    m_CPDefinitions.Add(cp.Definition);

                    mvd = new(Identity.ControlPoint, Entity.Null, false, IsManaged, edge, (short)(isStart ? 1 : 2));
                    cp = _MIT.ControlPointManager.GetOrCreate(mvd);
                    m_CPDefinitions.Add(cp.Definition);
                }
            }
            m_Overlay.EnqueueUpdate();

            return true;
        }

        internal override List<MVDefinition> GetAllChildren() => m_CPDefinitions;

        internal override List<MVDefinition> GetChildrenToTransform() => m_CPDefinitions;

        internal override void MoveIt(TransformBase action, State nodeState, bool move, bool rotate)
        {
            if (!move && !rotate) return;

            //Matrix4x4 matrix = default;
            //matrix.SetTRS(nodeState.m_InitialCenter + nodeState.m_MoveDelta, Quaternion.Euler(0f, nodeState.m_AngleDelta, 0f), Vector3.one);

            //for (int i = 0; i < m_CPDefinitions.Count; i++)
            //{
            //    MVControlPoint cp = _MIT.ControlPointManager.GetOrCreate(m_CPDefinitions[i]);
            //    State cpState = action.GetState(m_CPDefinitions[i]);// new(_MIT.EntityManager, ref QLookupFactory.Get(), cp);

            //    cpState.m_Position = (float3)matrix.MultiplyPoint(cpState.m_InitialPosition - nodeState.m_InitialCenter);
            //    float3 oldAngles = cpState.m_InitialRotation.ToEulerDegrees();
            //    cpState.m_Rotation = Quaternion.Euler(oldAngles.x, oldAngles.y + nodeState.m_AngleDelta, oldAngles.z);

            //    cp.MoveIt(action, cpState, move, rotate);
            //}

            nodeState.Transform(move, rotate);
        }

        internal override Bounds3 GetBounds()
        {
            try
            {
                Game.Rendering.CullingInfo cullingInfo = _MIT.EntityManager.GetComponentData<Game.Rendering.CullingInfo>(m_Entity);
                Bounds3 bounds = cullingInfo.m_Bounds;
                foreach ((Entity e, bool _) in m_Segments)
                {
                    MVSegment seg = _MIT.Moveables.GetOrCreate<MVSegment>(new(Identity.Segment, e, false));
                    bounds = bounds.Encapsulate(seg.GetBounds());
                }
                return bounds;
            }
            catch (Exception ex)
            {
                MIT.Log.Error($"Failed to get CullingInfo on node {m_Entity.D()} for GetBounds ({ex.Message})");
                return new Bounds3(Vector3.zero, Vector3.zero);
            }
        }

        internal override float GetRadius()
        {
            Game.Net.NodeGeometry geoData = _MIT.EntityManager.GetComponentData<Game.Net.NodeGeometry>(m_Entity);
            float x = geoData.m_Bounds.max.x - geoData.m_Bounds.min.x;
            float z = geoData.m_Bounds.max.z - geoData.m_Bounds.min.z;
            return math.max(6f, math.min(x, z)) / 2;
        }

        internal static Circle2 GetCircle(Game.Net.NodeGeometry geoData)
        {
            float x = geoData.m_Bounds.max.x - geoData.m_Bounds.min.x;
            float z = geoData.m_Bounds.max.z - geoData.m_Bounds.min.z;
            float radius = math.max(6f, math.min(x, z)) / 2;
            return new(radius, geoData.m_Bounds.xz.Center());
        }

        internal static Circle2 GetCircle(Game.Net.Node node)
        {
            float radius = 3f;
            return new(radius, node.m_Position.XZ());
        }

        internal string DebugNode() => $"{m_Entity.DX()} CPs:{m_CPDefinitions.Count}";

        public override void Dispose()
        {
            _MIT.ControlPointManager.RemoveIfUnused(m_CPDefinitions);
            base.Dispose();
        }
    }
}
