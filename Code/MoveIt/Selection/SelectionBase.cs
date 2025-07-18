using Colossal.IO.AssetDatabase.Internal;
using Colossal.Mathematics;
using MoveIt.Moveables;
using MoveIt.Overlays.Children;
using MoveIt.Tool;
using QCommonLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace MoveIt.Selection
{
    internal struct SelectionBaseData
    {
        internal HashSet<MVDefinition> m_Buffer;
        internal HashSet<MVDefinition> m_BufferFull;
        internal float3 m_Center;
        internal float m_Radius;
    }

    public abstract class SelectionBase
    {
        public const int MAX_SELECTION_SIZE     = 10000;
        public const int MAX_CIRCLECALC_SIZE    = 1000;

        protected static readonly MIT _MIT = MIT.m_Instance;

        internal abstract string Name { get; }
        protected HashSet<MVDefinition> _Buffer;
        protected HashSet<MVDefinition> _BufferFull;

        public int Count => _Buffer.Count;
        public int CountFull => _BufferFull.Count;
        public abstract bool Any { get; }
        public abstract bool IsActive { get; }

        /// <summary>
        /// Get a copy of the current selection definitions
        /// </summary>
        public HashSet<MVDefinition> Definitions => new(_Buffer);

        /// <summary>
        /// Get a the entities of current selection definitions
        /// </summary>
        public HashSet<Entity> Entities
        {
            get
            {
                HashSet<Entity> result = new();
                _Buffer.ForEach(mvd => result.Add(mvd.m_Entity));
                return result;
            }
        }

        public static OverlaySelectionCenter m_Overlay = null;

        /// <summary>
        /// The centre-point of all objects that transform
        /// </summary>
        public float3 Center { get; private set; }

        public float CenterTerrainHeight => _MIT.GetTerrainHeight(Center);
        private float _Radius;

        protected SelectionBase()
        {
            _Buffer = new();
            _BufferFull = new();
            PrepareSelectionCenterOverlay();
        }

        protected SelectionBase(SelectionBase old)
        {
            SelectionBaseData data = old.GetCopy();
            _Buffer = data.m_Buffer;
            _BufferFull = data.m_BufferFull;
            Center = data.m_Center;
            _Radius = data.m_Radius;
            PrepareSelectionCenterOverlay();
        }

        protected SelectionBase(SelectionState state)
        {
            _Buffer = new();
            _BufferFull = new();
            Add(state.CleanDefinitions().Definitions, false);
            PrepareSelectionCenterOverlay();
        }

        public abstract void ProcessAdd(MVDefinition mvd, bool append);

        private static void PrepareSelectionCenterOverlay()
        {
            if (m_Overlay is null)
            {
                m_Overlay = new OverlaySelectionCenter(4);
            }
            else
            {
                m_Overlay.EnqueueUpdate();
            }
        }

        public bool Add(IEnumerable<MVDefinition> definitions, bool fast)
        {
            var result = true;
            var c = 0;
            foreach (MVDefinition mvd in definitions)
            {
                if (!AddFromDefinition(mvd))
                {
                    result = false;
                    break;
                }
                c++;
            }
            if (!fast && c > 0) UpdateFull();
            return result;
        }

        protected bool Add(MVDefinition mvd)
        {
            if (!AddFromDefinition(mvd)) return false;
            UpdateFull();
            return true;
        }

        private bool AddFromDefinition(MVDefinition mvd)
        {
            if (mvd.IsNull) return false;
            if (mvd.IsChild && !_MIT.IsValid(mvd.m_Parent)) return false;
            if (_Buffer.Contains(mvd)) return false;
            if (_Buffer.Count >= MAX_SELECTION_SIZE) return false;

            Moveable mv = _MIT.Moveables.GetOrCreate<Moveable>(mvd);
            if (mv is null) return false;

            _Buffer.Add(mvd);
            mv.OnSelect();
            return true;
        }


        /// <summary>
        /// Refresh this selection when loaded from action history or Move It is opened
        /// Refresh Control Points
        /// Remove invalid Moveables
        /// Add Moveables to MoveablesManager
        /// </summary>
        public virtual void Refresh()
        {
            string msg = $"{Name}.Refresh; Buffer:{_Buffer.Count}, Full:{_BufferFull.Count}";
            string swapping = "";
            string removing = "";
            string readding = "";
            string noupdate = "";
            HashSet<MVDefinition> toRemove = new();
            HashSet<Moveable> toAdd = new();

            try
            {
                foreach (MVDefinition mvd in _Buffer)
                {
                    if (mvd.m_Identity == Identity.ControlPoint)
                    {
                        if (!_MIT.IsValid(mvd.m_Parent))
                        {
                            toRemove.Add(mvd);
                            removing += $"  CP {mvd}";
                        }
                        else
                        {
                            // If CP entity no longer exists (e.g. was cleaned up by Moveables.Refresh()), this will create a new entity
                            var cp = GetMV<MVControlPoint>(mvd);
                            MIT.Log.Debug($"Refreshing CP:\nMVD: {mvd}\n CP: {cp.Definition} <{cp.GetType().Name}>");
                            if (!mvd.m_Entity.Equals(cp.m_Entity))
                            {
                                toRemove.Add(mvd);
                                toAdd.Add(cp);
                                cp.m_Overlay.AddFlag(InteractionFlags.ParentSelected);
                                swapping += $" [{mvd.m_Entity.DX()}=>{cp.m_Entity.DX()}-Olay:{cp.m_Overlay.m_Entity.D()}]";
                            }
                            else
                            {
                                noupdate += $"  CP {mvd}";
                            }
                        }
                    }
                    else if (!_MIT.IsValid(mvd))
                    {
                        toRemove.Add(mvd);
                        removing += $"  {mvd}";
                    }
                    else if (!_MIT.Moveables.Has(mvd))
                    {
                        _MIT.Moveables.GetOrCreate<Moveable>(mvd);
                        readding += $"  {mvd}";
                    }
                    else
                    {
                        noupdate += $"  {mvd}";
                    }
                }
            }
            catch (Exception ex)
            {
                MIT.Log.Error($"SB.Refresh failed {ex}\n{QCommon.GetStackTrace(8)}", "SB01");
            }
            // BUG this removed newly recreated CPs
            try
            {
                foreach (MVDefinition mvd in toRemove)
                {
                    if (_MIT.Moveables.TryGet(mvd, out Moveable mv))
                    {
                        // toAdd may contain this Moveable but with a different child entity; TryGet does not compare child objects' entity
                        if (!toAdd.Contains(mv))
                        {
                            if (mv.OverlayHasFlag(InteractionFlags.Selected))
                            {
                                mv.OnDeselect();
                            }
                            mv.Dispose();
                        }
                    }
                    _Buffer.Remove(mvd);
                }
            }
            catch (Exception ex)
            {
                MIT.Log.Error($"SB.Refresh failed {ex}\n{QCommon.GetStackTrace(8)}", "SB02");
            }

            try
            {
                QLog.Debug($"Adding {toAdd.Count} new moveables");
                foreach (Moveable mv in toAdd)
                {
                    _Buffer.Add(mv.Definition);
                    mv.OnSelect();
                }
            }
            catch (Exception ex)
            {
                MIT.Log.Error($"SB.Refresh failed {ex}\n{QCommon.GetStackTrace(8)}", "SB03");
            }

            MIT.Log.Debug($"{msg}, NewBuffer:{_Buffer.Count}" +
                $"{(swapping.Length > 0 ? $"\n  Swapping: {swapping}" : "")}" +
                $"{(readding.Length > 0 ? $"\n    Adding: {readding}" : "")}" +
                $"{(removing.Length > 0 ? $"\n  Removing: {removing}" : "")}" +
                $"{(noupdate.Length > 0 ? $"\n Unchanged: {noupdate}" : "")}");

            try
            {
                UpdateFull();
            }
            catch (Exception ex)
            {
                MIT.Log.Error($"SB.Refresh failed {ex}\n{QCommon.GetStackTrace(8)}", "SB04");
            }
            //_MIT.Moveables.DebugDumpFull($"{Name}.RefreshFromArchive Full-MoveablesManager-Dump ");
        }

        /// <summary>
        /// Set the selection with required extras
        /// </summary>
        internal virtual void UpdateFull()
        {
            //string start = $"{Name}.UpdateFull; Buffer:{_Buffer.Count}, Full:{_BufferFull.Count}";
            //string msg = "";
            _BufferFull.Clear();

            foreach (MVDefinition mvd in _Buffer)
            {
                _BufferFull.Add(mvd);
                var mv = _MIT.Moveables.GetOrCreate<Moveable>(mvd);
                mv.Refresh();

                foreach (MVDefinition mvdChild in mv.GetAllChildren().Where(mvdChild => !_BufferFull.Contains(mvdChild)))
                {
                    _BufferFull.Add(mvdChild);
                    //msg += $"\n    + {QTypes.GetIdentityCode(mvd.m_Identity)}  {mvdChild}";
                }
                mv.UpdateOverlay();
            }

            CalculateCenter();

            //MIT.Log.Info($"{start}, NewFull:{_BufferFull.Count}{msg}\n{QCommon.GetStackTrace(3)}");
        }

        public void Remove(HashSet<MVDefinition> mvds, bool fast)
        {
            _Buffer.ExceptWith(mvds);
            mvds.ForEach(mvd => GetMV(mvd).OnDeselect());
            if (!fast) UpdateFull();
        }

        public void RemoveIfExists(MVDefinition mvd)
        {
            if (Has(mvd)) Remove(mvd);
        }

        protected void Remove(MVDefinition mvd)
        {
            Moveable mv = GetMV(mvd);
            _Buffer.Remove(mvd);
            mv.OnDeselect();
            UpdateFull();
        }

        public void Clear()
        {
            HashSet<MVDefinition> mvds = new(_Buffer);
            _Buffer.Clear();
            //MIT.DebugDumpDefinitions(mvds, "SelBase.Clear ", true);
            foreach (MVDefinition mvd in mvds.Where(mvd => mvd.m_Identity == Identity.ControlPoint))
            {
                GetMV(mvd).OnDeselect();
            }
            foreach (MVDefinition mvd in mvds.Where(mvd => mvd.m_Identity != Identity.ControlPoint))
            {
                GetMV(mvd).OnDeselect();
            }
            mvds.Clear();
            UpdateFull();
        }

        /// <summary>
        /// Does the selection include the defined object?
        /// </summary>
        /// <param name="mvd">The definition of the object to check</param>
        /// <param name="includeParent">Should it count as included if its parent object is selected?</param>
        /// <param name="includeChildren">Should it count as included if any of its children objects are selected?</param>
        /// <returns>Does the selection have this object?</returns>
        public bool Has(MVDefinition mvd, bool includeParent = true, bool includeChildren = false)
        {
            //if (mvd.m_Identity == Identity.ControlPoint)
            //{
            //    QLog.Debug($"HAS {mvd}:{BufferHas(ref _Buffer, mvd, includeParent, includeChildren)} {QCommon.GetCallerDebug()}");
            //}
            return BufferHas(ref _Buffer, mvd, includeParent, includeChildren);
        }

        public bool HasFull(MVDefinition mvd, bool includeParent = true, bool includeChildren = false)
        {
            return BufferHas(ref _BufferFull, mvd, includeParent, includeChildren);
        }

        protected bool BufferHas(ref HashSet<MVDefinition> buffer, MVDefinition mvd, bool includeParent, bool includeChildren)
        {
            if (includeParent)
            {
                MVDefinition parentDef = new(mvd.m_ParentId, mvd.m_Parent, mvd.m_IsManipulatable);
                if (_BufferFull.Contains(parentDef))
                {
                    return true;
                }
            }

            foreach (MVDefinition bufferDef in buffer)
            {
                if (bufferDef.Equals(mvd))
                {
                    return true;
                }

                if (!includeChildren || !_MIT.Moveables.TryGet(bufferDef, out Moveable mv)) continue;
                
                if (mv.GetAllChildren().Any(mvdChild => mvdChild.Equals(mvd)))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Get the objects that actually transform from the main Buffer
        /// </summary>
        protected virtual HashSet<MVDefinition> GetObjectsToTransform() { return new(); }

        /// <summary>
        /// Get the objects that actually transform from the full Buffer
        /// </summary>
        internal virtual HashSet<MVDefinition> GetObjectsToTransformFull() { return new(); }

        public void CalculateCenter()
        {
            HashSet<MVDefinition> mvds = GetObjectsToTransform();

            if (mvds.Count == 0)
            {
                Center = 0f;
                _Radius = 0f;
                m_Overlay.EnqueueUpdate();
                return;
            }

            using NativeList<float2> mvPosList = new(Allocator.Temp);
            float y = 0;
            try
            {
                foreach (float3 pos in mvds.Select(GetMV).Select(mv => mv.Transform.m_Position))
                {
                    y += pos.y;
                    mvPosList.Add(pos.XZ());
                }

                y /= mvds.Count;
            }
            catch (Exception ex)
            {
                MIT.Log.Warning($"CalculateCenter failed!\n{MIT.DebugDefinitions(mvds)}\n{ex}");
            }

            if (mvds.Count > MAX_CIRCLECALC_SIZE)
            {
                Bounds2 extremes = new(new(float.MaxValue, float.MaxValue), new(float.MinValue, float.MinValue));
                foreach (float2 pos in mvPosList)
                {
                    if (pos.x < extremes.min.x) extremes.min.x = pos.x;
                    if (pos.y < extremes.min.y) extremes.min.y = pos.y;
                    if (pos.x > extremes.max.x) extremes.max.x = pos.x;
                    if (pos.y > extremes.max.y) extremes.max.y = pos.y;
                }
                float2 center2D = extremes.Center();
                Center = new(center2D.x, y, center2D.y);
                _Radius = math.distance(center2D, extremes.max);
            }
            else
            {
                //Profiler.BeginSample("CalculateCenter Welzl");
                Circle2 mec = QMinimumEnclosingCircle.Welzl(mvPosList);
                //Profiler.EndSample();
                Center = new(mec.position.x, y, mec.position.y);
                _Radius = mec.radius;
            }

            m_Overlay.EnqueueUpdate();
        }

        public Quad2 GetTotalRectangle(out Bounds3 bounds, float expand = 0)
        {
            bounds = GetTotalBounds(expand);
            Quad2 rect = new(
                new(bounds.min.x, bounds.min.z),
                new(bounds.max.x, bounds.min.z),
                new(bounds.max.x, bounds.max.z),
                new(bounds.min.x, bounds.max.z));
            return rect;
        }

        public Bounds3 GetTotalBounds(float expand = 0)
        {
            HashSet<MVDefinition> source = GetObjectsToTransformFull();
            Bounds3 totalBounds = new(Center, Center);

            foreach (MVDefinition mvd in source)
            {
                totalBounds = totalBounds.Encapsulate(GetMV(mvd).GetBounds());
            }

            if (expand != 0) totalBounds = totalBounds.Expand(expand);

            return totalBounds;
        }

        private SelectionBaseData GetCopy()
        {
            HashSet<MVDefinition> buffer = new(_Buffer);
            HashSet<MVDefinition> bufferFull = new(_BufferFull);

            return new()
            {
                m_Buffer = buffer,
                m_BufferFull = bufferFull,
                m_Center = Center,
                m_Radius = _Radius,
            };
        }

        internal static Moveable GetMV(MVDefinition mvd)
            => _MIT.Moveables.GetOrCreate<Moveable>(mvd);

        internal static T GetMV<T>(MVDefinition mvd) where T : Moveable
            => _MIT.Moveables.GetOrCreate<T>(mvd);



        public virtual string DebugSelection()
        {
            StringBuilder sb = new("[" + Count.ToString() + "/" + _BufferFull.Count + "]");
            if (_BufferFull.Count <= 0) return sb.ToString();
            
            foreach (MVDefinition mvd in _BufferFull)
            {
                sb.AppendFormat("\n    {0}{1}", _Buffer.Contains(mvd) ? " " : "*", mvd);
            }
            return sb.ToString();
        }

        public void DebugDumpSelection(string prefix = "")
        {
            MIT.Log.Debug(prefix + DebugSelection());
        }
    }
}
