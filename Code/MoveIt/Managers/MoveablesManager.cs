using MoveIt.Moveables;
using MoveIt.Tool;
using QCommonLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;

namespace MoveIt.Managers
{
    public class MoveablesManager : MIT_Manager
    {
        private readonly HashSet<Moveable> _Moveables;

        public int Count => _Moveables.Count;

        public MoveablesManager()
        {
            _Moveables = new();
        }

        public int CountOf<T>() where T : Moveable
        {
            return _Moveables.Count(mv => mv is T);
        }


        public Moveable Factory(MVDefinition mvd) 
        {
            try
            {
                Moveable result;
                Entity e = mvd.m_Entity;

                if (mvd.m_IsManipulatable)
                {
                    if (mvd.IsChild)
                    {
                        e = _MIT.ControlPointManager.RecreateEntity(mvd);
                    }
                    result = QTypes.GetEntityIdentity(e) switch
                    {
                        Identity.Segment        => new MVManipSegment(e),
                        Identity.NetLane        => new MVManipSegment(e),
                        Identity.ControlPoint   => new MVManipControlPoint(e),
                        _ => throw new Exception($"Trying to create ManipulateMoveable of type {QTypes.GetEntityIdentity(e)}"),
                    };
                }
                else
                {
                    result = QTypes.GetEntityIdentity(e) switch
                    {
                        Identity.Building       => new MVBuilding(e),
                        Identity.Extension      => new MVExtension(e),
                        Identity.ServiceUpgrade => new MVServiceUpgrade(e),
                        Identity.Plant          => new MVPlant(e),
                        Identity.Segment        => new MVSegment(e),
                        Identity.NetLane        => new MVSegment(e),
                        Identity.ControlPoint   => new MVControlPoint(e),
                        Identity.Prop           => new MVProp(e),
                        Identity.Decal          => new MVDecal(e),
                        Identity.Surface        => new MVSurface(e),
                        Identity.Node           => _MIT.EntityManager.HasComponent<Game.Net.NodeGeometry>(e) ?
                                                    new MVNode(e) :
                                                    new MVLaneNode(e),
                        _ => new MVOther(e),
                    };
                }

                result.m_Parent = mvd.m_Parent;
                result.m_ParentKey = mvd.m_ParentKey;

                _Moveables.Add(result);
                //DebugDumpFull($"FACTORY {result.D()}\n    ");

                return result;
            }
            catch (Exception ex)
            {
                MIT.Log.Error($"Factory failed to create {mvd}\n{ex}");
                return null;
            }
        }

        public void Clear()
        {
            _MIT.Selection.Clear();
            _MIT.Hover.Clear();

            MIT.Log.Debug($"Moveables.Clear Removing all (Manager has {Count} entries before removal)");
            _Moveables.Clear();
        }

        public void Refresh()
        {
            _MIT.ControlPointManager.Refresh();

            // Clear up Control Points first
            HashSet<Moveable> buffer = new(_Moveables);
            foreach (var mv in buffer)
            {
                if (mv is not MVControlPoint cp) continue;
                if (!cp.Refresh()) RemoveDo(cp);
                if (cp.IsManipulatable == _MIT.m_IsManipulateMode && !IsInUse(cp.Definition) && !IsInUse(cp.ParentDefinition)) RemoveDo(cp);
            }

            // Clear everything else
            buffer = new(_Moveables);
            foreach (var mv in buffer)
            {
                if (mv is MVControlPoint) continue;
                if (!mv.Refresh())
                {
                    RemoveDo(mv);
                }
            }

            _MIT.Hover.Refresh();
        }

        public bool IsInUse(MVDefinition mvd)
        {
            if (_MIT.Hover.Is(mvd))        return true;
            if (_MIT.Selection.Has(mvd))   return true;
            return false;
        }

        public void UpdateAllControlPoints()
        {
            foreach (Moveable mv in _Moveables)
            {
                if (mv is not MVControlPoint cp) continue;
                cp.UpdateComponent();
            }
        }

        public void UpdateAllOverlays()
        {
            foreach (Moveable mv in _Moveables)
            {
                mv.m_Overlay?.EnqueueUpdate();
            }
        }

        #region Get/Create
        public T GetOrCreate<T>(MVDefinition mvd) where T : Moveable
        {
            if (TryGet(mvd, out T result)) return result;

            Moveable mv = Factory(mvd);
            if (mv is not T)
            {
                throw new Exception($"Attempted to create Moveable <{typeof(T)}> but created <{mv.Name}>");
            }
            return mv as T;
        }

        public Moveable GetOrCreate(MVDefinition mvd)
        {
            if (TryGet(mvd, out Moveable result)) return result;

            return Factory(mvd);
        }

        public bool TryGet<T>(MVDefinition mvd, out T result) where T : Moveable
        {
            result = _Moveables.FirstOrDefault(mv => mvd.Equals(mv) && mv is T) as T;
            return result != default;
        }

        public T Get<T>(MVDefinition mvd) where T : Moveable
        {
            return _Moveables.First(lhs => lhs.Definition.Equals(mvd) && lhs is T) as T;
        }

        public HashSet<T> GetAllOf<T>() where T : Moveable
        {
            HashSet<T> result = new();
            foreach (var mv in _Moveables.Where(mv => mv is T))
            {
                result.Add(mv as T);
            }
            return result;
        }
        #endregion

        #region Has
        public bool Has(MVDefinition mvd)
        {
            return TryGet(mvd, out Moveable _);
        }
        #endregion

        #region Remove
        /// <summary>
        /// Must be called AFTER caller removes from hovered/selection/whatever
        /// </summary>
        /// <param name="mvd">The MVDefinition to check</param>
        /// <returns>False if MVD is in use</returns>
        public bool RemoveIfUnused(MVDefinition mvd)
        {
            if (IsInUse(mvd)) return false;
            Remove(mvd);
            return true;
        }

        /// <summary>
        /// Should already be removed from Selection and Hover
        /// </summary>
        /// <param name="mvd">Definition of Moveable to remove</param>
        /// <returns>Null</returns>
        public Moveable Remove(MVDefinition mvd)
        {
            if (!TryGet(mvd, out Moveable mv))
            {
                throw new Exception($"Trying to remove moveable {mvd.m_Entity.D()} but it isn't in manager");
            }

            _Moveables.Remove(mv);

            mv.Dispose();
            return null;
        }

        internal void RemoveDo(Moveable mv)
        {
            _Moveables.Remove(mv);
            mv.Dispose();
        }
        #endregion

        #region LINQ wrappers
        public bool Any<T>() where T : Moveable
            => _Moveables.Any(mv => mv is T);

        public bool Any<T>(Func<T, bool> predicate) where T : Moveable
            => _Moveables.Any(mv => mv is T obj && predicate(obj));

        public T First<T>() where T : Moveable
            => _Moveables.First(mv => mv is T) as T;

        public T First<T>(Func<T, bool> predicate) where T : Moveable
            => _Moveables.First(mv => mv is T obj && predicate(obj)) as T;
        #endregion

        #region Debug
        public override string ToString() => DebugFull();

        public string DebugFull()
        {
            string msg = $"Moveables Manager entries:{_Moveables.Count}";

            foreach (var mv in _Moveables)
            {
                msg += $"\n  {(mv.IsManipulatable ? "M-" : "  ")}{mv.m_Entity.DX(true, true)} [{mv.Definition}] olay:{mv.D_Overlay()}";
            }

            return msg;
        }

        public void DebugDumpFull(string prefix = "")
        {
            MIT.Log.Debug(prefix + DebugFull());
        }
        #endregion
    }
}
