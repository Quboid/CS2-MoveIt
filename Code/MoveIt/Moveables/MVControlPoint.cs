﻿using Colossal.Mathematics;
using MoveIt.Components;
using MoveIt.Overlays;
using MoveIt.Tool;
using QCommonLib;
using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace MoveIt.Moveables
{
    public class MVControlPoint : Moveable
    {
        public override bool IsManipulatable    => false;
        public override bool IsManaged          => true;
        public override bool IsNormalChild      => true;

        internal float m_Diameter;
        internal Entity m_Node;

        internal MVDefinition NodeDefinition => new(Identity.Node, m_Node, IsManipulatable, false, Entity.Null, -1);

        public override Game.Objects.Transform Transform
        {
            get
            {
                if (!_MIT.EntityManager.Exists(m_Entity))
                {
                    m_Entity = _MIT.ControlPointManager.GetOrCreate(new(m_Identity, m_Entity, IsManipulatable, IsManaged, m_Parent, m_ParentKey)).m_Entity;
                }
                return new(_MIT.EntityManager.GetComponentData<MIT_ControlPoint>(m_Entity).m_Position, quaternion.identity);
            }
        }

        internal Bezier4x3 Curve => _MIT.EntityManager.GetComponentData<Game.Net.Curve>(m_Parent).m_Bezier;

        public MVControlPoint(Entity e) : base(e, Identity.ControlPoint)
        {
            m_Overlay = Factory.Create<OverlayControlPoint>(this, OverlayTypes.MVControlPoint);
            Refresh();
        }

        public MVControlPoint(Entity e, Identity identity) : base(e, identity)
        { } // Pass-thru for children

        internal void UpdateComponent()
        {
            MIT_ControlPoint oldData = _MIT.EntityManager.GetComponentData<MIT_ControlPoint>(m_Entity);
            Bezier4x3 curve = _MIT.EntityManager.GetComponentData<Game.Net.Curve>(oldData.m_Parent).m_Bezier;
            float3 position = curve.Get(oldData.m_ParentKey);

            MIT_ControlPoint cpData = new(oldData.m_Entity, oldData.m_Parent, oldData.m_ParentKey, oldData.m_Node, position, Overlay.CP_RADIUS * 2, IsManipulatable);
            _MIT.EntityManager.SetComponentData(m_Entity, cpData);

            Refresh();
        }

        internal override bool Refresh()
        {
            if (!_MIT.EntityManager.Exists(m_Entity))
            {
                m_Entity = _MIT.ControlPointManager.RecreateEntity(new(m_Identity, Entity.Null, IsManipulatable, IsManaged, m_Parent, m_ParentKey));
            }

            if (!IsValid) return false;
            if (!IsOverlayValid) return false;

            MIT_ControlPoint cpData = _MIT.EntityManager.GetComponentData<MIT_ControlPoint>(m_Entity);
            m_Diameter              = cpData.m_Diameter;
            m_Parent                = cpData.m_Parent;
            m_Node                  = cpData.m_Node;
            m_ParentKey             = cpData.m_ParentKey;

            if (!_MIT.EntityManager.Exists(m_Node)) return false;
            if (!_MIT.EntityManager.Exists(m_Parent)) return false;

            m_Overlay.EnqueueUpdate();
            return true;
        }

        public override void OnUnhover()
        {
            //MIT.Log.Debug($"OnUnhover {ToString()}");
            m_Overlay.RemoveFlag(InteractionFlags.Hovering | InteractionFlags.ToolHover);
            DisposeIfUnused();
        }

        public override void OnDeselect()
        {
            //MIT.Log.Debug($"OnDeselect {ToString()}");
            m_Overlay.RemoveFlag(InteractionFlags.Selected);
            DisposeIfUnused();
        }

        internal virtual void DisposeIfUnused()
        {
            if (_MIT.Hover.Is(Definition))                 throw new Exception($"Hovered Moveable is non-manip CP {m_Entity.D()}");
            if (_MIT.Selection.HasFull(Definition))        throw new Exception($"Selected Moveable is non-manip CP {m_Entity.D()}");
            if (_MIT.Selection.HasFull(NodeDefinition))    return;
            if (_MIT.Selection.HasFull(ParentDefinition))  return;
            if (_MIT.Hover.Is(ParentDefinition))           return;

            _MIT.Moveables.RemoveDo(this);
        }

        internal Circle2 GetCircle()
        {
            return new(m_Diameter / 2, Transform.m_Position.XZ());
        }

        public override void Dispose()
        {
            m_Overlay?.Dispose();
            _MIT.EntityManager.DestroyEntity(m_Entity);
            //base.Dispose(); Don't call parent
        }


        public override string ToString()
        {
            return Definition.ToString();
        }
    }
}
