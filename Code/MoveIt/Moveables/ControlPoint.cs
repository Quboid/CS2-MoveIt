using Colossal.Mathematics;
using MoveIt.Components;
using MoveIt.Managers;
using MoveIt.Overlays;
using MoveIt.Tool;
using QCommonLib;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MoveIt.Moveables
{
    public class ControlPoint : Moveable
    {
        internal float m_Diameter;
        internal Entity m_Segment;
        internal Entity m_Node;
        internal short m_CurveKey;
        internal CPStatus m_Status;

        public override Game.Objects.Transform Transform
        {
            get
            {
                return new (_Tool.EntityManager.GetComponentData<MIT_ControlPoint>(m_Entity).m_Position, quaternion.identity);
            }
        }

        internal Bezier4x3 Curve => _Tool.EntityManager.GetComponentData<Game.Net.Curve>(m_Segment).m_Bezier;

        public ControlPoint(Entity e) : base(e, QTypes.Identity.ControlPoint, QTypes.ObjectType.Managed, QTypes.Manipulate.Child)
        {
            Refresh();
        }

        internal void RefreshComponent()
        {
            MIT_ControlPoint oldData = _Tool.EntityManager.GetComponentData<MIT_ControlPoint>(m_Entity);
            float segmentWidth = Segment.GetDefaultWidth(oldData.m_Segment);
            Bezier4x3 curve = _Tool.EntityManager.GetComponentData<Game.Net.Curve>(oldData.m_Segment).m_Bezier;
            float3 position = curve.Get(oldData.m_CurveKey);

            MIT_ControlPoint cpData = new(oldData.m_Entity, oldData.m_Segment, oldData.m_Node, position, math.max(segmentWidth / 4, 2f), oldData.m_CurveKey);
            _Tool.EntityManager.SetComponentData(m_Entity, cpData);

            Refresh();
        }

        internal override bool Refresh()
        {
            if (!base.Refresh()) return false;

            MIT_ControlPoint cpData = _Tool.EntityManager.GetComponentData<MIT_ControlPoint>(m_Entity);
            m_Diameter = cpData.m_Diameter;
            m_Segment = cpData.m_Segment;
            m_Node = cpData.m_Node;
            m_CurveKey = cpData.m_CurveKey;
            m_Status = CPStatus.Visible;

            if (!_Tool.EntityManager.Exists(m_Node)) return false;
            if (!_Tool.EntityManager.Exists(m_Segment)) return false;

            return true;
        }

        public override void OnHover()
        {
            base.OnHover();
            m_Status |= CPStatus.Hovering;
        }

        public override void OnUnhover()
        {
            base.OnUnhover();
            m_Status &= ~CPStatus.Hovering;
            DisposeIfUnused();
        }

        public override void OnSelect()
        {
            base.OnSelect();
            m_Status |= CPStatus.Selected;
        }

        public override void OnDeselect()
        {
            m_Status &= ~CPStatus.Selected;
            DisposeIfUnused();
        }

        internal void DisposeIfUnused()
        {
            if (_Tool.Selection.HasFull(this)) return;
            if (_Tool.Selection.HasFull(m_Node)) return;
            if (_Tool.Selection.HasFull(m_Segment)) return;
            if (_Tool.Manipulation.HasFull(this)) return;
            if (_Tool.Manipulation.HasFull(m_Node)) return;
            if (_Tool.Manipulation.HasFull(m_Segment)) return;

            Dispose();
        }

        internal Circle2 GetCircle()
        {
            return new(m_Diameter / 2, Transform.m_Position.XZ());
        }

        internal Node GetNode()
        {
            Game.Net.Edge edge = _Tool.EntityManager.GetComponentData<Game.Net.Edge>(m_Segment);
            Entity e = m_CurveKey.IsNodeA() ? edge.m_Start : edge.m_End;
            return GetOrCreate<Node>(e);
        }

        /// <summary>
        /// Get the overlay for when this moveable is in control
        /// </summary>
        internal override Utils.IOverlay GetManipulationOverlay(OverlayFlags flags)
        {
            return new OverlayCP(Transform.m_Position, m_Diameter, m_Status, flags);
        }

        /// <summary>
        /// Get the overlay for when the parent is in control
        /// </summary>
        internal Utils.IOverlay GetOverlayFromParent(OverlayFlags flags)
        {
            return new OverlayCPFromParent(Transform.m_Position, m_Diameter, m_Status, flags);
        }


        public struct OverlayCP : Utils.IOverlay
        {
            public Utils.OverlayCommon Common { get; set; }

            public float m_Diameter;
            public Color m_CustomColor;
            internal CPStatus m_Status;

            public OverlayCP(float3 position, float diameter, CPStatus status, OverlayFlags flags = OverlayFlags.None)
            {
                Common = new()
                {
                    Transform = new(position, default),
                    Flags = flags,
                    Manipulatable = QTypes.Manipulate.Child,
                };

                m_Diameter = diameter;
                m_Status = status;
                m_CustomColor = default;
            }

            public readonly void Draw(ToolFlags toolFlags)
            {
                Color fg = Colors.Get(Common, Colors.Styles.Foreground);
                Color bg = Colors.Get(Common, Colors.Styles.Background);

                if (Common.Flags != OverlayFlags.Hovering) // Applies when only Hovering is active, not selected/etc as well
                {
                    bg = new(fg.r, fg.g, fg.b, fg.a * 0.2f);
                }

                Common.DrawTool.CircleFill(Common, fg, bg, m_Diameter);
            }

            public readonly void Dispose()
            { }

            public readonly JobHandle Dispose(JobHandle handle)
            {
                return handle;
            }
        }


        public struct OverlayCPFromParent : Utils.IOverlay
        {
            public Utils.OverlayCommon Common { get; set; }

            public float m_Diameter;
            public Color m_CustomColor;
            internal CPStatus m_Status;

            public OverlayCPFromParent(float3 position, float diameter, CPStatus status, OverlayFlags flags = OverlayFlags.None)
            {
                Common = new()
                {
                    Transform = new(position, default),
                    Flags = flags,
                    Manipulatable = QTypes.Manipulate.Parent,
                };

                m_Diameter = diameter;
                m_Status = status;
                m_CustomColor = default;
            }

            public readonly void Draw(ToolFlags toolFlags)
            {
                if (m_Status > CPStatus.Visible) return;

                Color fg = Colors.Get(Common, Colors.Styles.Foreground);
                Color bg = Colors.Get(Common, Colors.Styles.Background);

                //float startFade = m_TTL - 0.3f;
                //if (!m_IsPersistant && Time.time > startFade)
                //{
                //    float t = 1 - (m_TTL - Time.time) / (m_TTL - startFade);
                //    fg.a = math.lerp(fg.a, 0, t);
                //    bg.a = math.lerp(bg.a, 0, t);
                //}

                if ((Common.Flags & OverlayFlags.Selected) == 0)
                {
                    Common.DrawTool.CircleFill(Common, bg, Color.clear, m_Diameter);
                }
                else
                {
                    Common.DrawTool.CircleDashedFill(Common, fg, bg, m_Diameter);
                }
            }

            public readonly void Dispose()
            { }

            public readonly JobHandle Dispose(JobHandle handle)
            {
                return handle;
            }
        }


        public override void Dispose()
        {
            _Tool.ControlPointManager.RemoveFromList(this);
            if (_Tool.Selection.Has(m_Entity)) _Tool.Selection.Remove(m_Entity);
            if (_Tool.Manipulation.Has(m_Entity)) _Tool.Manipulation.Remove(m_Entity);
            _Tool.EntityManager.DestroyEntity(m_Entity);
            base.Dispose();
        }
    }
}
