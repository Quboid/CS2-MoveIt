using MoveIt.Moveables;
using MoveIt.Tool;
using QCommonLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Entities;

namespace MoveIt.Managers
{
    public class MoveablesManager : MIT_Manager
    {
        private readonly HashSet<Moveable> _Moveables = new();

        public int Count => _Moveables.Count;

        public int CountOf<T>() where T : Moveable
        {
            return _Moveables.Count(mv => mv is T);
        }


        public Moveable Factory(MVDefinition mvd, Identity forced = Identity.None)
        {
            Moveable result;
            Entity e = mvd.m_Entity;
            Identity id = (forced == Identity.None ? QTypes.GetEntityIdentity(e) : forced);

            try
            {
                if (mvd.m_IsManipulatable)
                {
                    result = id switch
                    {
                        Identity.Segment        => new MVManipSegment(e),
                        Identity.NetLane        => new MVManipSegment(e),
                        Identity.ControlPoint   => new MVManipControlPoint(mvd),
                        _ => throw new Exception($"Trying to create ManipulateMoveable of type {id}"),
                    };
                }
                else
                {
                    result = id switch
                    {
                        Identity.Building       => new MVBuilding(e),
                        Identity.Extension      => new MVExtension(e),
                        Identity.ServiceUpgrade => new MVServiceUpgrade(e),
                        Identity.Plant          => new MVPlant(e),
                        Identity.Segment        => new MVSegment(e),
                        Identity.NetLane        => new MVSegment(e),
                        Identity.ControlPoint   => new MVControlPoint(mvd),
                        Identity.Prop           => new MVProp(e),
                        Identity.Decal          => new MVDecal(e),
                        Identity.Surface        => new MVSurface(e),
                        Identity.Node           => _MIT.EntityManager.HasComponent<Game.Net.NodeGeometry>(e) ?
                                                    new MVNode(e) :
                                                    new MVLaneNode(e),
                        _ => new MVOther(e),
                    };
                }

                _Moveables.Add(result);
                //QLog.Debug($"FACTORY: {result.D()} - {result.Definition}");

                return result;
            }
            catch (Exception ex)
            {
                MIT.Log.Error($"Factory failed to create {id} {mvd}\n{ex}");
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
            StringBuilder sb = new();
            sb.AppendFormat("MM.Refresh {0}", QCommon.GetCallerDebug());
            sb.AppendFormat("\n{0}\nRefresh count:{1}", DebugFull(), _Moveables.Count);

            // Clear up Control Points first
            HashSet<Moveable> buffer = new(_Moveables);
            foreach (Moveable mv in buffer)
            {
                if (mv is not MVControlPoint cp) continue;
                sb.AppendFormat("\n  CP {0} <{1}>", cp.E(), cp.Name);
                if (!cp.Refresh())
                {
                    RemoveDo(cp);
                    sb.AppendFormat(" Remove:FailedRefresh");
                    continue;
                }
                if (cp.IsManipulatable == _MIT.m_IsManipulateMode && !IsInUse(cp.Definition) && !IsInUse(cp.ParentDefinition))
                {
                    RemoveDo(cp);
                    sb.AppendFormat(" Remove:NotUsed");
                    continue;
                }
                sb.AppendFormat(" Kept");
            }

            // Clear everything else
            buffer = new(_Moveables);
            foreach (Moveable mv in buffer)
            {
                if (mv is MVControlPoint) continue;
                sb.AppendFormat("\n     {0} <{1}>", mv.E(), mv.Name);
                if (!mv.Refresh())
                {
                    RemoveDo(mv);
                    sb.AppendFormat(" Remove:FailedRefresh");
                    continue;
                }
                if (mv.IsManipulatable == _MIT.m_IsManipulateMode && !IsInUse(mv.Definition))
                {
                    RemoveDo(mv);
                    sb.AppendFormat(" Remove:NotUsed");
                    continue;
                }
                sb.AppendFormat(" Kept");
            }
            QLog.Debug(sb.ToString());

            _MIT.Hover.Refresh();
        }

        public bool IsInUse(MVDefinition mvd)
        {
            if (_MIT.Hover.Is(mvd))             return true;
            if (_MIT.Selection.Has(mvd))        return true;
            if (_MIT.Queue.Current.Uses(mvd))   return true;
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
        /// <summary>
        /// Get the Moveable based on the passed definition, creating the Moveable if it doesn't exist.
        /// </summary>
        /// <typeparam name="T">The Moveable child type to get</typeparam>
        /// <param name="mvd">Definition of the Moveable to get</param>
        /// <returns>The Moveable</returns>
        public T GetOrCreate<T>(MVDefinition mvd) where T : Moveable
        {
            if (TryGet(mvd, out T result)) return result;

            Moveable mv = Factory(mvd);
            if (mv is not T)
            {
                throw new Exception($"MM.GetOrCre - Attempted to create <{typeof(T)}>, created <{(mv is null ? "null" : mv.Name)}> for {mvd}.");
            }
            return mv as T;
        }

        /// <summary>
        /// Get the Moveable based on the passed definition. Returns whether or not the Moveable exists.
        /// </summary>
        /// <typeparam name="T">The Moveable child type to get</typeparam>
        /// <param name="mvd">Definition of the Moveable to get</param>
        /// <param name="result">The Moveable, or Default if not found</param>
        /// <returns>Was the Moveable found?</returns>
        public bool TryGet<T>(MVDefinition mvd, out T result) where T : Moveable
        {
            result = _Moveables.FirstOrDefault(mv => mvd.Equals(mv) && mv is T) as T;
            return result != default;
        }

        /// <summary>
        /// Get the Moveable based on the passed definition. Returns Null if it doesn't exist.
        /// </summary>
        /// <typeparam name="T">The Moveable child type to get</typeparam>
        /// <param name="mvd">Definition of the Moveable to get</param>
        /// <returns>The Moveable or Null</returns>
        public T GetIfExists<T>(MVDefinition mvd) where T : Moveable
        {
            T result = _Moveables.FirstOrDefault(mv => mvd.Equals(mv) && mv is T) as T;
            return result == default ? null : result;
        }

        /// <summary>
        /// Get the Moveable based on the passed definition. Throws InvalidOperationException if it doesn't exist.
        /// </summary>
        /// <typeparam name="T">The Moveable child type to get</typeparam>
        /// <param name="mvd">Definition of the Moveable to get</param>
        /// <returns>The Moveable</returns>
        /// <exception cref="InvalidOperationException">No matching Moveable exists</exception>
        public T Get<T>(MVDefinition mvd) where T : Moveable
        {
            return _Moveables.First(lhs => lhs.Definition.Equals(mvd) && lhs is T) as T;
        }

        /// <summary>
        /// Get all Moveables of type T
        /// </summary>
        /// <typeparam name="T">The Moveable type to get</typeparam>
        /// <returns>HashSet of all Moveables of type T</returns>
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
        /// <summary>
        /// Check if the defined Moveable exists
        /// </summary>
        /// <param name="mvd">The definition to check for</param>
        /// <returns>Does the Moveable exist?</returns>
        public bool Has(MVDefinition mvd)
        {
            return TryGet(mvd, out Moveable _);
        }
        #endregion

        #region Remove
        /// <summary>
        /// Must be called AFTER caller removes from hovered/selection/whatever
        /// </summary>
        /// <param name="mvd">Definition of Moveable to check</param>
        /// <returns>False if MVD is in use</returns>
        public bool RemoveIfUnused(MVDefinition mvd)
        {
            if (IsInUse(mvd)) return false;
            Remove(mvd);
            QLog.Debug($"MM.RemoveIfUnused {mvd} {QCommon.GetCallerDebug()}");
            return true;
        }

        /// <summary>
        /// Should already be removed from Selection and Hover
        /// </summary>
        /// <param name="mvd">Definition of Moveable to remove, if it exists</param>
        /// <returns>Did this Moveable exist?</returns>
        public bool TryRemove(MVDefinition mvd)
        {
            if (!TryGet(mvd, out Moveable mv))
            {
                return false;
            }

            //QLog.Debug($"TryRemove: {mv.D()}\n{QCommon.GetStackTrace(4)}");
            _Moveables.Remove(mv);
            mv.Dispose();
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

            //QLog.Debug($"Remove: {mv.D()}\n{QCommon.GetStackTrace(4)}");
            _Moveables.Remove(mv);
            mv.Dispose();
            return null;
        }

        internal void RemoveDo(Moveable mv)
        {
            MIT.Log.Info($"Removing {mv} {QCommon.GetCallerDebug()}", "MI-MMREMDO-01");
            //QLog.Debug($"MM.RemoveDo: {mv.D()}");
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

        public void DebugDumpFullBundle(string key, string prefix = "")
        {
            MIT.Log.Bundle(key, prefix + DebugFull());
        }
        #endregion
    }
}
