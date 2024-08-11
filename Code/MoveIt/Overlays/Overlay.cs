using MoveIt.Moveables;
using MoveIt.Tool;
using QCommonLib;
using System;
using Unity.Entities;
using Unity.Mathematics;

namespace MoveIt.Overlays
{
    public abstract class Overlay : IDisposable
    {
        protected static readonly MIT _MIT = MIT.m_Instance;

        public const float LINE_MIN_WIDTH           = 0.1f;
        public const float LINE_MAX_WIDTH           = 3f;
        public const int   LINE_MAX_DISTANCE        = 4000;
        public const float LINE_DEFAULT_WIDTH       = 0.3f;
        public const float SELECT_SCALE_MULTIPLYER  = 0.5f;
        public const float SELECT_BASE_RADIUS       = 0.5f;
        public const float CP_RADIUS                = 1.5f;
        public const int   DEBUG_TTL                = 100;

        /// <summary>
        /// How to process this overlay
        /// </summary>
        public OverlayTypes m_Type  = OverlayTypes.None;
        /// <summary>
        /// The entity that has a Moveable which this overlay represents, if any
        /// </summary>
        public Entity m_Owner       = Entity.Null;
        /// <summary>
        /// The actual overlay that gets rendered
        /// </summary>
        public Entity m_Entity      = Entity.Null;
        /// <summary>
        /// The Moveable which this overlay represents, if any
        /// </summary>
        public Moveable m_Moveable  = null;
        public T GetMoveable<T>() where T : Moveable => m_Moveable as T;

        public string Name => GetType().Name;

        protected virtual Game.Objects.Transform Transform => m_Moveable.Transform;
        internal MIO_Common Common => _MIT.EntityManager.GetComponentData<MIO_Common>(m_Entity);

        public override string ToString()
        {
            return $"[Overlay {Name.Substring(7, Name.Length - 7)}, " +
                $"ent:{(m_Entity.Equals(Entity.Null) ? "Null" : m_Entity.D() + "/" + _MIT.EntityManager.GetComponentData<MIO_Type>(m_Entity).m_Type + "/" + Common.m_Flags)}, " +
                $"owner:{m_Moveable?.Name} {(m_Moveable is null ? "null" : $"{m_Moveable.D(true)}")}]";
        }

        public Overlay(OverlayTypes type)
        {
            m_Type = type;
        }

        protected void UpdateCommon(ref MIO_Common common)
        {
            common.m_Transform = Transform;
            common.m_TerrainHeight = _MIT.GetTerrainHeight(common.m_Transform.m_Position);

            float elevation = 0f;

            if (_MIT.EntityManager.HasComponent<Game.Net.Elevation>(m_Owner))
            {
                Game.Net.Elevation el = _MIT.EntityManager.GetComponentData<Game.Net.Elevation>(m_Owner);
                elevation = (el.m_Elevation.y + el.m_Elevation.x) / 2;
            }
            else if (_MIT.EntityManager.HasComponent<Game.Objects.Elevation>(m_Owner))
            {
                Game.Objects.Elevation el = _MIT.EntityManager.GetComponentData<Game.Objects.Elevation>(m_Owner);
                elevation = el.m_Elevation;
            }

            float terrainOffset = (common.m_Transform.m_Position.y + elevation) - common.m_TerrainHeight;
            float terrainOffsetAbs = math.abs(terrainOffset) - 2f;
            if (terrainOffsetAbs < 0f)
            {
                common.m_ShadowOpacity = 0f;
            }
            else if (terrainOffsetAbs > 4f)
            {
                common.m_ShadowOpacity = 1f;
            }
            else
            {
                common.m_ShadowOpacity = math.lerp(0f, 1f, terrainOffsetAbs / 4f);
            }
        }

        public virtual void EnqueueUpdate()
        {
            _MIT.QueueOverlayUpdate(this);
        }

        public virtual void EnqueueUpdateDeferred()
        {
            _MIT.QueueOverlayUpdateDeferred(this);
        }

        public virtual bool Update()
        {
            if (m_Moveable is null) return false;
            if (m_Entity.Equals(Entity.Null)) return false;

            MIO_Common common = _MIT.EntityManager.GetComponentData<MIO_Common>(m_Entity);
            UpdateCommon(ref common);

            if (_MIT.EntityManager.HasComponent<MIO_Circle>(m_Entity))
            {
                MIO_Circle circle = _MIT.EntityManager.GetComponentData<MIO_Circle>(m_Entity);
                circle.Circle.position = common.m_Transform.m_Position;
                _MIT.EntityManager.SetComponentData(m_Entity, circle);
            }

            _MIT.EntityManager.SetComponentData(m_Entity, common);
            return true;
        }
        
        public virtual void AddFlag(InteractionFlags flags)
        {
            //MIT.Log.Debug($"Adding flag {flags} to overlay {m_Entity.D()} of {m_Owner.DX()}");
            if (m_Entity.Equals(Entity.Null))
            {
                if (!CreateOverlayEntity())
                {
                    return;
                }
            }

            MIO_Common common = _MIT.EntityManager.GetComponentData<MIO_Common>(m_Entity);

            common.m_Flags |= flags;
            _MIT.EntityManager.SetComponentData(m_Entity, common);
        }

        public virtual void RemoveFlag(InteractionFlags flags)
        {
            //MIT.Log.Debug($"Removing flag {flags} to overlay {m_Entity.D()} of {m_Owner.DX()}");
            if (m_Entity.Equals(Entity.Null)) return;

            MIO_Common common = _MIT.EntityManager.GetComponentData<MIO_Common>(m_Entity);

            common.m_Flags &= ~flags;

            if (common.m_Flags == InteractionFlags.None)
            {
                DestroyOverlayEntity();
            }

            _MIT.EntityManager.SetComponentData(m_Entity, common);
        }


        public virtual bool CreateOverlayEntity()
        {
            if (!m_Entity.Equals(Entity.Null))
            {
                MIT.Log.Error($"Creating overlay when one already exists for {m_Owner.DX(true)} ({m_Entity.D()}).", "MIT01");
                return false;
            }
            return true;
        }

        public virtual bool DestroyOverlayEntity()
        {
            if (m_Entity.Equals(Entity.Null))
            {
                MIT.Log.Error($"Destroying overlay when none exists for {m_Owner.DX(true)}.", "MIT02");
                return false;
            }

            //MIT.Log.Debug($"Overlay.DestroyOlayEnt {m_Entity.D()}/{m_Type}, owner:{m_Moveable?.D()}, exists:{_MIT.EntityManager.Exists(m_Entity)} hasDel:{_MIT.EntityManager.HasComponent<Game.Common.Deleted>(m_Entity)}");
            _MIT.EntityManager.AddComponent<Game.Common.Deleted>(m_Entity);

            m_Entity = Entity.Null;
            return true;
        }

        public void Dispose()
        {
            if (m_Entity.Equals(Entity.Null)) return;
            DestroyOverlayEntity();
        }
    }
}
