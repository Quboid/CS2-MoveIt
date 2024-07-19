using Colossal.IO.AssetDatabase.Internal;
using Colossal.Mathematics;
using MoveIt.Moveables;
using MoveIt.Overlays;
using MoveIt.Tool;
using QCommonLib;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace MoveIt.Selection
{
    internal record struct SelectionBaseData
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

        protected static readonly MIT _Tool = MIT.m_Instance;

        internal virtual string Name { get; }
        protected HashSet<MVDefinition> _Buffer;
        protected HashSet<MVDefinition> _BufferFull;

        public int Count => _Buffer.Count;
        public int CountFull => _BufferFull.Count;
        public virtual bool Any { get; }
        public virtual bool IsActive { get; }

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
        public float3 Center => _Center;
        public float CenterTerrainHeight => _Tool.GetTerrainHeight(_Center);
        private float3 _Center;
        private float _Radius;

        public SelectionBase()
        {
            _Buffer = new();
            _BufferFull = new();
            PrepareSelectionCenterOverlay();
        }

        public SelectionBase(SelectionBase old)
        {
            SelectionBaseData data = old.GetCopy();
            _Buffer = data.m_Buffer;
            _BufferFull = data.m_BufferFull;
            _Center = data.m_Center;
            _Radius = data.m_Radius;
            PrepareSelectionCenterOverlay();
        }

        public SelectionBase(SelectionState state)
        {
            _Buffer = new();
            _BufferFull = new();
            Add(state.CleanDefinitions().Definitions, false);
            PrepareSelectionCenterOverlay();
        }

        public abstract void ProcessAdd(MVDefinition mvd, bool append);

        protected void PrepareSelectionCenterOverlay()
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
            bool result = true;
            int c = 0;
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

        public bool Add(MVDefinition mvd)
        {
            if (!AddFromDefinition(mvd)) return false;
            UpdateFull();
            return true;
        }

        private bool AddFromDefinition(MVDefinition mvd)
        {
            if (mvd.IsNull) return false;
            if (mvd.IsChild && !_Tool.IsValid(mvd.m_Parent)) return false;
            if (_Buffer.Contains(mvd)) return false;
            if (_Buffer.Count >= MAX_SELECTION_SIZE) return false;

            Moveable mv = _Tool.Moveables.GetOrCreate(mvd);
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

            foreach (var mvd in _Buffer)
            {
                if (mvd.m_Identity == Identity.ControlPoint)
                {
                    if (!_Tool.IsValid(mvd.m_Parent))
                    {
                        toRemove.Add(mvd);
                        removing += $"  CP {mvd}";
                    }
                    else
                    {
                        MVControlPoint oldCP = (MVControlPoint)GetMV(mvd);
                        MVControlPoint newCP = _Tool.ControlPointManager.GetOrCreate(oldCP.Definition);
                        if (!oldCP.Equals(newCP))
                        {
                            toRemove.Add(mvd);
                            toAdd.Add(newCP);
                            newCP.m_Overlay.AddFlag(InteractionFlags.ParentSelected);
                            swapping += $" [{oldCP.m_Entity.D()}=>{newCP.m_Entity.D()}-{newCP.m_Overlay.m_Entity.D()}]";
                        }
                        else
                        {
                            noupdate += $"  CP {mvd}";
                        }
                    }
                }
                else if (!_Tool.IsValid(mvd))
                {
                    toRemove.Add(mvd);
                    removing += $"  {mvd}";
                }
                else if (!_Tool.Moveables.Has(mvd))
                {
                    _Tool.Moveables.GetOrCreate(mvd);
                    readding += $"  {mvd}";
                }
                else
                {
                    noupdate += $"  {mvd}";
                }
            }

            foreach (MVDefinition mvd in toRemove)
            {
                if (_Tool.Moveables.TryGet(mvd, out Moveable mv))
                {
                    if (mv.OverlayHasFlag(InteractionFlags.Selected))
                    {
                        mv.OnDeselect();
                    }
                    mv.Dispose();
                }
                _Buffer.Remove(mvd);
            }

            foreach (Moveable mv in toAdd)
            {
                _Buffer.Add(mv.Definition);
                mv.OnSelect();
            }

            MIT.Log.Debug($"{msg}, NewBuffer:{_Buffer.Count}" +
                $"{(swapping.Length > 0 ? $"\n  Swapping: {swapping}" : "")}" +
                $"{(readding.Length > 0 ? $"\n    Adding: {readding}" : "")}" +
                $"{(removing.Length > 0 ? $"\n  Removing: {removing}" : "")}" +
                $"{(noupdate.Length > 0 ? $"\n Unchanged: {noupdate}" : "")}");

            UpdateFull();
            //_Tool.Moveables.DebugDumpFull($"{Name}.RefreshFromArchive Full-MoveablesManager-Dump ");
        }

        /// <summary>
        /// Set the selection with required extras
        /// </summary>
        internal virtual void UpdateFull()
        {
            //string start = $"{Name}.UpdateFull; Buffer:{_Buffer.Count}, Full:{_BufferFull.Count}";
            //string msg = "";
            _BufferFull.Clear();

            foreach (var mvd in _Buffer)
            {
                _BufferFull.Add(mvd);
                Moveable mv = _Tool.Moveables.GetOrCreate(mvd);
                mv.Refresh();

                foreach (MVDefinition mvdChild in mv.GetAllChildren())
                {
                    if (!_BufferFull.Contains(mvdChild))
                    {
                        _BufferFull.Add(mvdChild);
                        //msg += $"\n    + {QTypes.GetIdentityCode(mvd.m_Identity)}  {mvdChild}";
                    }
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

        public void Remove(MVDefinition mvd)
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
            MIT.DebugDumpDefinitions(mvds, "SelBase.Clear ");
            foreach (MVDefinition mvd in mvds)
            {
                if (mvd.m_Identity != Identity.ControlPoint) continue;
                GetMV(mvd).OnDeselect();
            }
            foreach (MVDefinition mvd in mvds)
            {
                if (mvd.m_Identity == Identity.ControlPoint) continue;
                GetMV(mvd).OnDeselect();
            }

            mvds.Clear();
        }


        public bool Has(MVDefinition mvd)
        {
            foreach (var lhs in _Buffer)
            {
                if (lhs.Equals(mvd))
                {
                    return true;
                }
            }
            return false;
        }

        public bool HasFull(MVDefinition mvd)
        {
            return _BufferFull.Count(lhs => mvd.Equals(lhs)) > 0;
        }

        /// <summary>
        /// Get the objects that actually transform
        /// </summary>
        internal virtual HashSet<MVDefinition> GetObjectsToTransform() { return new(); }

        /// <summary>
        /// Get the objects that actually transform
        /// </summary>
        internal virtual HashSet<MVDefinition> GetObjectsToTransformFull() { return new(); }

        public void CalculateCenter()
        {
            HashSet<MVDefinition> mvds = GetObjectsToTransform();

            if (mvds.Count == 0)
            {
                _Center = 0f;
                _Radius = 0f;
                m_Overlay.EnqueueUpdate();
                return;
            }

            using NativeList<float2> mvPosList = new(Allocator.Temp);
            float y = 0;
            foreach (MVDefinition mvd in mvds)
            {
                Moveable mv = GetMV(mvd);
                float3 pos = mv.Transform.m_Position;
                y += pos.y;
                mvPosList.Add(pos.XZ());
            }
            y /= mvds.Count;

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
                _Center = new(center2D.x, y, center2D.y);
                _Radius = math.distance(center2D, extremes.max);
            }
            else
            {
                //Profiler.BeginSample("CalculateCenter Welzl");
                Circle2 mec = QMinimumEnclosingCircle.Welzl(mvPosList);
                //Profiler.EndSample();
                _Center = new(mec.position.x, y, mec.position.y);
                _Radius = mec.radius;
            }
            //QLog.Debug($"{UnityEngine.Time.frameCount} Welzl mvs:{moveables.Count} (sqr:{math.pow(moveables.Count, 2d)}) r:{_Radius}, pos:{_Center.DX()}");
            //_DebugCirclesCyan.Add(new(mec.radius, _Center, quaternion.identity));
            //_DebugCirclesCyan.Add(new(1f, _Center, quaternion.identity));

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
            Bounds3 totalBounds = new(_Center, _Center);

            foreach (MVDefinition mvd in source)
            {
                totalBounds = totalBounds.Encapsulate(GetMV(mvd).GetBounds());
            }

            if (expand != 0) totalBounds = totalBounds.Expand(expand);

            return totalBounds;
        }

        internal SelectionBaseData GetCopy()
        {
            HashSet<MVDefinition> buffer = new(_Buffer);
            HashSet<MVDefinition> bufferFull = new(_BufferFull);

            return new()
            {
                m_Buffer = buffer,
                m_BufferFull = bufferFull,
                m_Center = _Center,
                m_Radius = _Radius,
            };
        }

        internal static Moveable GetMV(MVDefinition mvd) => _Tool.Moveables.GetOrCreate(mvd);

        //private HashSet<Line3.Segment> _DebugLines = new();
        //private HashSet<Circle3> _DebugCirclesCyan = new();
        //public void AddDebugOverlay()
        //{
        //    List<Moveable> moveables = GetObjectsToTransform();
        //    if (moveables.Count < 1) return;

        //    _DebugLines.ForEach(line => Overlays.DebugOverlays.Line(line));
        //    _DebugCirclesCyan.ForEach(circle => Overlays.DebugOverlays.Circle(circle.position, circle.radius, UnityEngine.Color.cyan));
        //}


        public virtual string DebugSelection()
        {
            StringBuilder sb = new("[" + Count.ToString() + "/" + _BufferFull.Count + "]");
            if (_BufferFull.Count > 0)
            {
                foreach (MVDefinition mvd in _BufferFull)
                {
                    sb.AppendFormat("\n    {0}{1}", _Buffer.Contains(mvd) ? " " : "*", mvd);
                }
            }
            return sb.ToString();
        }

        public void DebugDumpSelection(string prefix = "")
        {
            QLog.Debug(prefix + DebugSelection());
        }
    }
}
