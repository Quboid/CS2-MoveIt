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
    public abstract class Moveable : IEquatable<Moveable>, IEquatable<Entity>, IDisposable
    {
        public const int CURVE_CPS = 4;
        public const int SEGEND_CPS = 2;

        protected static readonly MIT _Tool = MIT.m_Instance;

        public static Moveable Factory(Entity e)
        {
            Moveable result = QTypes.GetEntityIdentity(e) switch
            {
                QTypes.Identity.Building        => new Building(e),
                QTypes.Identity.Plant           => new Plant(e),
                QTypes.Identity.Segment         => new Segment(e),
                QTypes.Identity.Node            => new Node(e),
                QTypes.Identity.Roundabout      => new Roundabout(e),
                QTypes.Identity.ControlPoint    => _Tool.ControlPointManager.GetOrCreate(e),
                _ => new Other(e),
            };
            return result;
        }

        public static Moveable GetOrCreate(Entity e)
        {
            if (_Tool.Hover.Is(e)) return _Tool.Hover.Moveable;
            if (_Tool.Selection.HasFull(e)) return _Tool.Selection.GetFull(e);
            if (_Tool.Manipulation.HasFull(e)) return _Tool.Manipulation.GetFull(e);
            if (_Tool.ControlPointManager.Has(e)) return _Tool.ControlPointManager.Get(e);
            return Factory(e);
        }

        public static T GetOrCreate<T>(Entity e) where T : Moveable
        {
            return (T)GetOrCreate(e);
        }

        public Entity m_Entity;
        public QTypes.Identity m_Identity;
        public QTypes.ObjectType m_ObjectType;
        public QTypes.Manipulate m_Manipulatable;

        public float m_YOffset = 0f;

        public virtual Game.Objects.Transform Transform => _Tool.EntityManager.GetComponentData<Game.Objects.Transform>(m_Entity);
        public virtual Game.Objects.Transform OverlayTransform => Transform;

        public Moveable(Entity e, QTypes.Identity identity, QTypes.ObjectType objectType, QTypes.Manipulate manipulatable = QTypes.Manipulate.Normal)
        {
            m_Entity = e;
            m_Identity = identity;
            m_ObjectType = objectType;
            m_Manipulatable = manipulatable;
            UpdateYOffset();
        }

        internal virtual void MoveIt(TransformAction action, State state, bool move, bool rotate)
        {
            //if (rotate) QLog.Debug($"mv.MoveIt {m_Entity.D()}-{m_Identity} {state.m_Position.DX()} (was:{Transform.m_Position.DX()}) - {state.m_Angle} <{state.m_Data.Get().GetType()}>");
            //else QLog.Debug($"mv.MoveIt {m_Entity.D()}-{m_Identity} {state.m_Position.DX()} (was:{Transform.m_Position.DX()}) <{state.m_Data.Get().GetType()}> {state.m_Data.Debug()}");

            if (!move && !rotate) return;

            state.Transform(move, rotate);
        }

        internal virtual void UpdateYOffset()
        {
            float3 position = Transform.m_Position;
            m_YOffset = position.y - _Tool.GetTerrainHeight(position);
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

        public virtual void OnHover()       { } //QLog.Debug($"{m_Entity.D()} OnHover"); }
        public virtual void OnUnhover()     { } //QLog.Debug($"{m_Entity.D()} OnUnhover"); }
        public virtual void OnClick()       { } //QLog.Debug($"{m_Entity.D()} OnClick"); }
        public virtual void OnSelect()      { } //QLog.Debug($"{m_Entity.D()} OnSelect"); }
        public virtual void OnDeselect()    { if (!_Tool.Hover.Is(m_Entity)) Dispose(); } //QLog.Debug($"{m_Entity.D()} OnDeselect"); }

        /// <summary>
        /// Check if this object's data is up to date
        /// </summary>
        /// <returns>Should this object be kept in the selection?</returns>
        internal virtual bool Refresh()
        {
            return _Tool.IsValid(m_Entity);
        }

        public virtual void Dispose()
        { }

        internal virtual Utils.IOverlay GetOverlay(OverlayFlags flags)
        {
            return new Utils.None();
        }

        internal virtual HashSet<Utils.IOverlay> GetOverlays(OverlayFlags flags)
        {
            return new HashSet<Utils.IOverlay>() { GetOverlay(flags) };
        }

        internal virtual Utils.IOverlay GetManipulationOverlay(OverlayFlags flags)
        {
            return new Utils.None();
        }

        internal virtual HashSet<Utils.IOverlay> GetManipulationOverlays(OverlayFlags flags)
        {
            return new HashSet<Utils.IOverlay>() { GetManipulationOverlay(flags) };
        }

        internal virtual List<T> GetChildren<T>() where T : Moveable
        {
            return new();
        }

        #region Simple component access
        internal void AddComponent<T>() where T : unmanaged, IComponentData
        {
            _Tool.EntityManager.AddComponent<T>(m_Entity);
        }

        internal T GetComponent<T>() where T : unmanaged, IComponentData
        {
            return _Tool.EntityManager.GetComponentData<T>(m_Entity);
        }

        internal void SetComponent<T>(T component) where T : unmanaged, IComponentData
        {
            _Tool.EntityManager.SetComponentData(m_Entity, component);
        }

        internal void RemoveComponent<T>() where T : unmanaged, IComponentData
        {
            _Tool.EntityManager.RemoveComponent<T>(m_Entity);
        }

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
        #endregion

        internal string D()
        {
            return $"{m_Entity.DX()}";
        }

        public bool Equals(Moveable other)
        {
            if (other is null) return false;
            if (m_Entity.Equals(other.m_Entity))
            {
                return true;
            }
            return false;
        }

        public bool Equals(Entity other)
        {
            if (m_Entity.Equals(other))
            {
                return true;
            }
            return false;
        }
    }
}
