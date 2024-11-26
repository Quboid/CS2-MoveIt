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
        public const float SELECT_SCALE_MULTIPLIER  = 0.5f;
        public const float SELECT_BASE_RADIUS       = 0.5f;
        public const float CP_RADIUS                = 1.5f;
        public const int   DEBUG_TTL                = 100;

        /// <summary>
        /// How to process this overlay
        /// </summary>
        public OverlayTypes m_Type;
        /// <summary>
        /// The entity that has a Moveable which this overlay represents, if any
        /// </summary>
        public Entity m_Owner;
        /// <summary>
        /// The actual overlay that gets rendered
        /// </summary>
        public Entity m_Entity      = Entity.Null;
        /// <summary>
        /// The name of the method that called for the update, only when IS_DEBUG is set
        /// </summary>
        protected string m_Caller   = string.Empty;
        /// <summary>
        /// The Moveable which this overlay represents, if any
        /// </summary>
        protected Moveable _Moveable;

        protected T GetMoveable<T>() where T : Moveable => _Moveable as T;

        public string Name => GetType().Name;

        protected virtual Game.Objects.Transform Transform => _Moveable.Transform;
        internal MIO_Common Common => _MIT.EntityManager.GetComponentData<MIO_Common>(m_Entity);

        public override string ToString()
        {
            return $"[Overlay {Name.Substring(7, Name.Length - 7)}, " +
                $"ent:{(m_Entity.Equals(Entity.Null) ? "Null" : m_Entity.D() + "/" + _MIT.EntityManager.GetComponentData<MIO_Type>(m_Entity).m_Type + "/" + Common.m_Flags)}, " +
                $"owner:{_Moveable?.Name} {(_Moveable is null ? "null" : $"{_Moveable.D(true)}")}]";
        }

        protected Overlay(OverlayTypes type, Moveable mv)
        {
            m_Type = type;
            _Moveable = mv;
            m_Owner = mv?.m_Entity ?? Entity.Null;
            if (type != OverlayTypes.None) CreateOverlayEntityFromAbstract();
        }

        private void CreateOverlayEntityFromAbstract()
        {
            CreateOverlayEntity();
        }

        protected void UpdateCommon(ref MIO_Common common)
        {
            common.m_Transform = Transform;
            common.m_TerrainHeight = _MIT.GetTerrainHeight(common.m_Transform.m_Position);

            var elevation = 0f;

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
            float terrainOffsetAbs = math.abs(terrainOffset) - 2f; // Shadow never appears if height offset is less than 2m
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
#if IS_DEBUG
            m_Caller = QCommon.GetStackTrace(15);
#endif
            _MIT.QueueOverlayUpdate(this);
        }

        public virtual void EnqueueUpdateDeferred()
        {
            _MIT.QueueOverlayUpdateDeferred(this);
        }

        public virtual bool Update()
        {
            if (_Moveable is null) return false;
            if (!m_Entity.Exists(_MIT.EntityManager))
            {
                QLog.Debug($"OvUp {Name} {m_Entity.D()} (for {m_Owner.DX()}) doesn't exist!"
#if IS_DEBUG
                    + $" Caller: {(m_Caller.Equals(string.Empty) ? "<null>" : "\n" + m_Caller)}"
#endif
                    );
                return false;
            }

            var common = _MIT.EntityManager.GetComponentData<MIO_Common>(m_Entity);
            UpdateCommon(ref common);

            if (_MIT.EntityManager.HasComponent<MIO_Circle>(m_Entity))
            {
                var circle = _MIT.EntityManager.GetComponentData<MIO_Circle>(m_Entity);
                circle.Circle.position = common.m_Transform.m_Position;
                _MIT.EntityManager.SetComponentData(m_Entity, circle);
            }

            _MIT.EntityManager.SetComponentData(m_Entity, common);
            return true;
        }
        
        public virtual void AddFlag(InteractionFlags flags)
        {
            //MIT.Log.Debug($"Adding flag {flags} to olay {m_Entity.D()} of {m_Owner.DX()}");
            if (!m_Owner.Exists(_MIT.EntityManager))
            {
                MIT.Log.Error($"{this} does not have a CP entity! ({m_Owner.DX()}, exists:{_MIT.EntityManager.Exists(m_Owner)})");
                return;
            }
            if (!m_Entity.Exists(_MIT.EntityManager))
            {
                MIT.Log.Error($"{this} does not have an overlay entity!");
                return;
            }

            var common = _MIT.EntityManager.GetComponentData<MIO_Common>(m_Entity);

            common.m_Flags |= flags;
            _MIT.EntityManager.SetComponentData(m_Entity, common);
        }

        public virtual void RemoveFlag(InteractionFlags flags)
        {
            if (!m_Entity.Exists(_MIT.EntityManager))
            {
                //MIT.Log.Debug($"Overlay.RemoveFlag {m_Entity.D()} (for {m_Owner.DX()}) doesn't exist!\n{QCommon.GetStackTrace()}");
                return;
            }

            var common = _MIT.EntityManager.GetComponentData<MIO_Common>(m_Entity);

            common.m_Flags &= ~flags;
            //MIT.Log.Debug($"Removing flag {flags} from overlay {m_Entity.D()} of {m_Owner.DX()}, remaining flags:{common.m_Flags}");

            _MIT.EntityManager.SetComponentData(m_Entity, common);
        }


        protected abstract bool CreateOverlayEntity();

        protected virtual bool DestroyOverlayEntity()
        {
            if (!m_Entity.Exists(_MIT.EntityManager))
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

        public string E()
        {
            return m_Entity.DX();
        }
    }
}
