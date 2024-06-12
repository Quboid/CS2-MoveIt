using Colossal.Mathematics;
using MoveIt.Actions;
using MoveIt.Overlays;
using MoveIt.Tool;
using QCommonLib;
using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace MoveIt.Moveables
{
    public abstract class Moveable : IEquatable<Moveable>, IDisposable
    {
        public const int CURVE_CPS = 4;

        protected static readonly MIT _Tool = MIT.m_Instance;

        /// <summary>
        /// The game object (building/node/CP/etc)'s entity
        /// </summary>
        public Entity m_Entity;

        /// <summary>
        /// The game object (building/node/CP/etc)'s parent entity, if any
        /// </summary>
        public Entity m_Parent = Entity.Null;

        /// <summary>
        /// How this Moveable is referenced by parent
        /// For curve CPs, 0 = a, 1 = b, etc
        /// </summary>
        public short m_ParentKey = -1;

        public Identity m_Identity;
        public ObjectType m_ObjectType;
        public Overlay m_Overlay;
        public string Name => GetType().Name;
        public virtual bool IsManipulatable => false;
        public virtual bool IsChild => false;
        public virtual bool IsManaged => false;

        public float m_YOffset = 0f;
        public virtual bool IsValid => _Tool.IsValid(m_Entity);
        public virtual bool IsOverlayValid => m_Overlay is not null;

        public virtual Game.Objects.Transform Transform => _Tool.EntityManager.GetComponentData<Game.Objects.Transform>(m_Entity);
        public virtual MVDefinition Definition => new(m_Identity, m_Entity, IsManipulatable, IsManaged, m_Parent, m_ParentKey);

        public Moveable(Entity e, Identity identity, ObjectType objectType)
        {
            m_Entity = e;
            m_Identity = identity;
            m_ObjectType = objectType;
            UpdateYOffset();
        }

        /// <summary>
        /// Check if this object's data is up to date
        /// </summary>
        /// <returns>Should this object be kept in the selection?</returns>
        internal virtual bool Refresh()
        {
            if (!IsValid) return false;
            if (!IsOverlayValid) return false;

            m_Overlay.EnqueueUpdate();
            return true;
        }

        internal virtual void UpdateYOffset()
        {
            float3 position = Transform.m_Position;
            m_YOffset = position.y - _Tool.GetTerrainHeight(position);
        }

        internal virtual void MoveIt(TransformAction action, State state, bool move, bool rotate)
        {
            if (!move && !rotate) return;

            state.Transform(move, rotate);
        }

        internal void UpdateOverlay()
        {
            m_Overlay?.EnqueueUpdate();
        }

        internal Bounds3 GetBounds()
        {
            try
            {
                Game.Rendering.CullingInfo cullingInfo = _Tool.EntityManager.GetComponentData<Game.Rendering.CullingInfo>(m_Entity);
                Bounds3 bounds = cullingInfo.m_Bounds;
                return bounds;
            }
            catch (Exception ex)
            {
                MIT.Log.Error($"Failed to get CullingInfo on {m_Entity.D()} for GetBounds ({ex.Message})");
                return new Bounds3(Vector3.zero, Vector3.zero);
            }
        }

        public virtual void OnHover()
        {
            //QLog.Debug($"{m_Entity.D()} {Name} OnHover {m_Overlay.Common.m_Flags}");

            m_Overlay.AddFlag(InteractionFlags.Hovering);
            foreach (var mv in GetChildMoveablesForOverlays<Moveable>())
            {
                mv.m_Overlay.AddFlag(InteractionFlags.ParentHovering);
            }
        }

        /// <summary>
        /// Must be called AFTER being removed from hover
        /// </summary>
        public virtual void OnUnhover()
        {
            //QLog.Debug($"{m_Entity.D()} {Name} OnUnhover {m_Overlay.Common.m_Flags}");

            m_Overlay.RemoveFlag(InteractionFlags.Hovering);
            foreach (var mv in GetChildMoveablesForOverlays<Moveable>())
            {
                mv.m_Overlay.RemoveFlag(InteractionFlags.ParentHovering);
            }

            if (m_Overlay.Common.m_Flags == 0)
            {
                _Tool.Moveables.RemoveIfUnused(Definition);
            }
        }

        public virtual void OnClick()
        {
            //QLog.Debug($"{m_Entity.D()} {Name} OnClick");
        }

        public virtual void OnSelect()
        {
            //QLog.Debug($"{m_Entity.D()} {Name} OnSelect");
            m_Overlay.AddFlag(InteractionFlags.Selected);
            foreach (var mv in GetChildMoveablesForOverlays<Moveable>())
            {
                mv.m_Overlay.AddFlag(IsManipulatable ? InteractionFlags.ParentManipulating : InteractionFlags.ParentSelected);
            }
        }

        /// <summary>
        /// Must be called AFTER being removed from selection
        /// </summary>
        public virtual void OnDeselect()
        {
            //QLog.Debug($"{m_Entity.D()} {Name} OnDeselect");
            m_Overlay.RemoveFlag(InteractionFlags.Selected);
            foreach (var mv in GetChildMoveablesForOverlays<Moveable>())
            {
                mv.m_Overlay.RemoveFlag(IsManipulatable ? InteractionFlags.ParentManipulating : InteractionFlags.ParentSelected);
            }
            _Tool.Moveables.RemoveIfUnused(Definition);
        }

        public bool OverlayHasFlag(InteractionFlags flag)
        {
            if (m_Entity.Equals(Entity.Null)) return false;
            if (m_Overlay is null) return false;
            if (m_Overlay.m_Entity.Equals(Entity.Null)) return false;

            MIO_Common common = _Tool.EntityManager.GetComponentData<MIO_Common>(m_Overlay.m_Entity);
            return (common.m_Flags & flag) != 0;
        }

        internal virtual float GetRadius()
        {
            Game.Prefabs.PrefabRef prefab = _Tool.EntityManager.GetComponentData<Game.Prefabs.PrefabRef>(m_Entity);
            Game.Prefabs.ObjectGeometryData geoData = _Tool.EntityManager.GetComponentData<Game.Prefabs.ObjectGeometryData>(prefab);
            return math.max(math.cmax(new float2(geoData.m_Size.x, geoData.m_Size.z)), 3f) / 2;
        }

        internal virtual List<MVDefinition> GetAllChildren() => new();

        internal virtual List<MVDefinition> GetChildrenToTransform() => new();

        internal virtual List<T> GetChildMoveablesForOverlays<T>() where T : Moveable => new();

        internal bool TryGetBuffer<T>(out DynamicBuffer<T> buffer, bool isReadOnly = false) where T : unmanaged, IBufferElementData
        {
            if (!_Tool.EntityManager.HasBuffer<T>(m_Entity))
            {
                buffer = default;
                return false;
            }

            buffer = _Tool.EntityManager.GetBuffer<T>(m_Entity, isReadOnly);
            return true;
        }

        public bool Equals(Moveable other)
        {
            if (other is null) return false;
            if (m_Entity.Equals(other.m_Entity) && IsManipulatable == other.IsManipulatable)
            {
                return true;
            }
            return false;
        }

        public virtual void Dispose()
        {
            m_Overlay.Dispose();
        }


        internal string D(bool full = false)
        {
            return $"{(IsManipulatable ? "M" : "n")}-{m_Entity.DX(full)}";
        }

        internal string D_Overlay()
        {
            return $"{(m_Overlay is null ? "(Null Overlay)" : m_Overlay.m_Entity.D() + ":" + m_Overlay.Name + "-" + m_Overlay.m_Type)}";
        }

        public override string ToString()
        {
            return D(true);
        }
    }
}
