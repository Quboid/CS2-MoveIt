using Colossal.Mathematics;
using Game.Prefabs;
using MoveIt.Actions.Transform;
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
        protected const int CURVE_CPS = 4;

        protected static readonly MIT _MIT = MIT.m_Instance;

        /// <summary>
        /// The game object (building/node/CP/etc)'s entity
        /// </summary>
        public Entity m_Entity;

        /// <summary>
        /// The game object (building/node/CP/etc)'s parent entity, if any
        /// </summary>
        public Entity m_Parent = Entity.Null;

        /// <summary>
        /// The game object (building/node/CP/etc)'s parent's type, if any
        /// </summary>
        public readonly Identity m_ParentId = Identity.None;

        /// <summary>
        /// How this Moveable is referenced by parent
        /// For curve CPs, 0 = a, 1 = b, etc
        /// </summary>
        public short m_ParentKey = -1;

        public readonly Identity m_Identity;
        public Overlay m_Overlay;
        public string Name => GetType().Name;

        /// <summary>
        /// Is this Moveable used in Manipulation Mode?
        /// </summary>
        public virtual bool IsManipulatable => false;
        /// <summary>
        /// Is this Moveable a child object in Manipulation Mode?
        /// </summary>
        public virtual bool IsManipChild => false;
        /// <summary>
        /// Is this Moveable a child object in Normal Mode?
        /// </summary>
        public virtual bool IsNormalChild => false;
        /// <summary>
        /// Is this Moveable's entity managed by Move It?
        /// </summary>
        public virtual bool IsManaged => false;

        public virtual bool IsValid => _MIT.IsValid(m_Entity);
        public virtual bool IsOverlayValid => m_Overlay is not null;

        public virtual Game.Objects.Transform Transform => _MIT.EntityManager.GetComponentData<Game.Objects.Transform>(m_Entity);
        public virtual MVDefinition Definition => new(m_Identity, m_Entity, IsManipulatable, IsManaged, m_Parent, m_ParentId, m_ParentKey);
        public virtual MVDefinition ParentDefinition => new(QTypes.GetEntityIdentity(m_Parent), m_Parent, IsManipulatable, false, Entity.Null, Identity.None, -1);


        protected Moveable(Entity e, Identity identity)
        {
            m_Entity = e;
            m_Identity = identity;
        }

        /// <summary>
        /// Check if this object's data is up to date
        /// </summary>
        /// <returns>Should this object be kept in the selection?</returns>
        internal virtual bool Refresh()
        {
            if (!IsValid) return false;
            if (!IsOverlayValid) return false;

            //QLog.Debug($"Olay-EnqueueUpdate {GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name} {E()} caller:{QCommon.GetCallingMethodName()}");
            m_Overlay.EnqueueUpdate();
            return true;
        }
        
        protected bool RefreshFromAbstract()
            => Refresh();

        internal virtual void MoveIt(TransformBase action, State state, bool move, bool rotate)
        {
            if (!move && !rotate) return;

            state.Transform(move, rotate);
        }

        internal virtual void UpdateCurve(State state)
        {
            state.UpdateCurve();
        }

        internal void UpdateOverlay()
        {
            m_Overlay?.EnqueueUpdate();
        }

        internal void UpdateOverlayDeferred()
        {
            m_Overlay?.EnqueueUpdateDeferred();
        }

        internal virtual Bounds3 GetBounds()
        {
            try
            {
                Game.Rendering.CullingInfo cullingInfo = _MIT.EntityManager.GetComponentData<Game.Rendering.CullingInfo>(m_Entity);
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
            InteractionFlags flags = InteractionFlags.Hovering | (_MIT.MITState == MITStates.ToolActive ? InteractionFlags.ToolHover : 0);
            m_Overlay.AddFlag(flags);

            flags = InteractionFlags.ParentHovering | (_MIT.MITState == MITStates.ToolActive ? InteractionFlags.ToolParentHover : 0);
            foreach (Moveable mv in GetChildMoveablesForOverlays<Moveable>())
            {
                mv.m_Overlay.AddFlag(flags);
            }

            //MIT.Log.Debug($"{m_Entity.D()} {Name} OnHover {m_Overlay.Common.m_Flags}");
        }

        /// <summary>
        /// Must be called AFTER being removed from hover
        /// </summary>
        public virtual void OnUnhover()
        {
            MIT.Log.Debug($"{m_Entity.D()} {Name} OnUnhover {m_Overlay.Common.m_Flags} {QCommon.GetCallerDebug()}");

            m_Overlay.RemoveFlag(InteractionFlags.Hovering | InteractionFlags.ToolHover);
            OnUnhoverChildren();

            if (m_Overlay.Common.m_Flags == 0)
            {
                _MIT.Moveables.RemoveIfUnused(Definition);
            }
        }

        public virtual void OnUnhoverChildren()
        {
            MIT.Log.Debug($"{m_Entity.D()} {Name} OnUnhoverChildren {QCommon.GetCallerDebug()}");

            // Don't use GetChildMoveablesForOverlays as it will always create the Moveable, creating orphaned CPs
            foreach (MVDefinition mvd in GetAllChildren())
            {
                if (_MIT.Moveables.TryGet<Moveable>(mvd, out Moveable mv))
                {
                    mv.m_Overlay.RemoveFlag(InteractionFlags.ParentHovering | InteractionFlags.ToolParentHover);
                }
            }
        }

        public virtual void OnClick()
        {
            //MIT.Log.Debug($"ONCLICK {m_Entity.D()} {Name}");
        }

        public virtual void OnSelect()
        {
            //MIT.Log.Debug($"{m_Entity.D()} {Name} OnSelect");
            m_Overlay.AddFlag(InteractionFlags.Selected);
            foreach (Moveable mv in GetChildMoveablesForOverlays<Moveable>())
            {
                mv.m_Overlay.AddFlag(IsManipulatable ? InteractionFlags.ParentManipulating : InteractionFlags.ParentSelected);
            }
        }

        /// <summary>
        /// Must be called AFTER being removed from selection
        /// </summary>
        public virtual void OnDeselect()
        {
            MIT.Log.Debug($"{m_Entity.D()} {Name} OnDeselect {QCommon.GetCallerDebug()}");
            m_Overlay.RemoveFlag(InteractionFlags.Selected);
            foreach (Moveable mv in GetChildMoveablesForOverlays<Moveable>())
            {
                mv.m_Overlay.RemoveFlag(IsManipulatable ? InteractionFlags.ParentManipulating : InteractionFlags.ParentSelected);
            }
            _MIT.Moveables.RemoveIfUnused(Definition);
        }

        public bool OverlayHasFlag(InteractionFlags flag)
        {
            if (m_Entity.Equals(Entity.Null)) return false;
            if (m_Overlay is null) return false;
            if (m_Overlay.m_Entity.Equals(Entity.Null)) return false;

            MIO_Common common = _MIT.EntityManager.GetComponentData<MIO_Common>(m_Overlay.m_Entity);
            return (common.m_Flags & flag) != 0;
        }

        internal virtual float GetRadius()
        {
            PrefabRef prefab = _MIT.EntityManager.GetComponentData<PrefabRef>(m_Entity);
            ObjectGeometryData geoData = _MIT.EntityManager.GetComponentData<ObjectGeometryData>(prefab);
            return math.max(math.cmax(new float2(geoData.m_Size.x, geoData.m_Size.z)), 2f) / 2;
        }

        internal virtual List<MVDefinition> GetAllChildren() => new();

        internal virtual List<MVDefinition> GetChildrenToTransform() => new();

        internal virtual List<T> GetChildMoveablesForOverlays<T>() where T : Moveable => new();

        internal bool TryGetBuffer<T>(out DynamicBuffer<T> buffer, bool isReadOnly = false) where T : unmanaged, IBufferElementData
        {
            if (!_MIT.EntityManager.HasBuffer<T>(m_Entity))
            {
                buffer = default;
                return false;
            }

            buffer = _MIT.EntityManager.GetBuffer<T>(m_Entity, isReadOnly);
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
            m_Overlay?.Dispose();
        }


        #region Debug
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

        public string E()
        {
            return m_Entity.DX();
        }
        #endregion
    }
}
