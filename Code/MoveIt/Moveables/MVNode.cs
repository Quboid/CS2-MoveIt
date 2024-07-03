using Colossal.Mathematics;
using MoveIt.Actions;
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

        public MVLaneNode(Entity e) : base(e, Identity.Node, ObjectType.Normal)
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
                var netTransform = _Tool.EntityManager.GetComponentData<Game.Net.Node>(m_Entity);
                return new()
                {
                    m_Position = netTransform.m_Position,
                    m_Rotation = netTransform.m_Rotation,
                };
            }
        }

        public MVNode(Entity e) : base(e, Identity.Node, ObjectType.Normal)
        {
            m_Overlay = Factory.Create<OverlayNode>(this, OverlayTypes.MVNode);
            Refresh();
        }

        public MVNode(Entity e, Identity identity, ObjectType objectType) : base(e, identity, objectType)
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
                    bool isStart = _Tool.EntityManager.GetComponentData<Game.Net.Edge>(edge).m_Start.Equals(m_Entity);
                    m_Segments.Add(edge, isStart);

                    MVDefinition mvd = new(Identity.ControlPoint, Entity.Null, false, IsManaged, edge, (short)(isStart ? 0 : 3));
                    MVControlPoint cp = _Tool.ControlPointManager.GetOrCreate(mvd);
                    m_CPDefinitions.Add(cp.Definition);

                    mvd = new(Identity.ControlPoint, Entity.Null, false, IsManaged, edge, (short)(isStart ? 1 : 2));
                    cp = _Tool.ControlPointManager.GetOrCreate(mvd);
                    m_CPDefinitions.Add(cp.Definition);
                }
            }
            m_Overlay.EnqueueUpdate();

            return true;
        }

        internal override List<MVDefinition> GetAllChildren() => m_CPDefinitions;

        internal override List<MVDefinition> GetChildrenToTransform() => m_CPDefinitions;

        internal override void MoveIt(TransformAction action, State nodeState, bool move, bool rotate)
        {
            if (!move && !rotate) return;

            Matrix4x4 matrix = default;
            matrix.SetTRS(action.m_Center + action.MoveDelta, Quaternion.Euler(0f, action.AngleDelta, 0f), Vector3.one);

            for (int i = 0; i < m_CPDefinitions.Count; i++)
            {
                MVControlPoint cp = _Tool.ControlPointManager.GetOrCreate(m_CPDefinitions[i]);
                State cpState = action.GetState(m_CPDefinitions[i]);// new(_Tool.EntityManager, ref QLookupFactory.Get(), cp);

                cpState.m_Position = (float3)matrix.MultiplyPoint(cpState.m_InitialPosition - action.m_Center);
                float3 oldAngles = cpState.m_InitialRotation.ToEulerDegrees();
                cpState.m_Rotation = Quaternion.Euler(oldAngles.x, oldAngles.y + action.AngleDelta, oldAngles.z);

                //action.SetState(m_CPDefinitions[i], cpState);
                //QLog.Debug($"ND.MoveIt {i}/{m_CPDefinitions.Count}/{_Tool.ControlPointManager.Count} {m_Entity.DX()} ({m_CPDefinitions[i]}) CP:{cp.m_Entity.DX()}:{cp.m_ParentKey} stateCP:{cpState.m_Entity.D()}");
                cp.MoveIt(action, cpState, move, rotate);
            }
            //_Tool.ControlPointManager.DebugDumpControlPoints("ND.MoveIt");

            nodeState.Transform(move, rotate);
        }

        internal override Bounds3 GetBounds()
        {
            try
            {
                Game.Rendering.CullingInfo cullingInfo = _Tool.EntityManager.GetComponentData<Game.Rendering.CullingInfo>(m_Entity);
                Bounds3 bounds = cullingInfo.m_Bounds;
                foreach ((Entity e, bool _) in m_Segments)
                {
                    MVSegment seg = _Tool.Moveables.GetOrCreate<MVSegment>(new(Identity.Segment, e, false));
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
            Game.Net.NodeGeometry geoData = _Tool.EntityManager.GetComponentData<Game.Net.NodeGeometry>(m_Entity);
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
            _Tool.ControlPointManager.RemoveIfUnused(m_CPDefinitions);
            base.Dispose();
        }
    }
}
