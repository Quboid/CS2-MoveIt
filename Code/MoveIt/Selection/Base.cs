using Colossal.IO.AssetDatabase.Internal;
using Colossal.Mathematics;
using MoveIt.Managers;
using MoveIt.Moveables;
using MoveIt.Tool;
using QCommonLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;

namespace MoveIt.Selection
{
    public abstract class Base : IEnumerable<KeyValuePair<Entity, Moveable>>
    {
        protected readonly MIT _Tool = MIT.m_Instance;

        protected Dictionary<Entity, Moveable> _Buffer;
        protected HashSet<Moveable> _BufferFull;

        public int Count => _Buffer.Count;
        public int CountFull => _BufferFull.Count;
        public bool Exists => Count > 0;

        internal List<Entity> Entities => _Buffer.Keys.ToList();
        internal List<Moveable> Moveables => _Buffer.Values.ToList();
        internal HashSet<Moveable> FullSelection => _BufferFull;

        protected readonly Dictionary<CPDefinition, ControlPoint> _ControlPoints;

        /// <summary>
        /// The centre-point of all objects that transform
        /// </summary>
        public float3 Center => GetCenterPoint();

        public Base()
        {
            _Buffer = new Dictionary<Entity, Moveable>();
            _BufferFull = new();
            _ControlPoints = new();
        }

        public Base(Base old)
        {
            _Buffer = new Dictionary<Entity, Moveable>();
            _BufferFull = new();
            _ControlPoints = new();

            foreach ((Entity e, Moveable mv) in old)
            {
                if (mv.m_Identity == QTypes.Identity.ControlPoint)
                {
                    AddExistingMoveable(e, mv);
                    mv.Refresh();
                }
            }
            foreach ((Entity e, Moveable mv) in old)
            {
                if (!(mv.m_Identity == QTypes.Identity.ControlPoint))
                {
                    AddExistingMoveable(e, mv);
                    mv.Refresh();
                }
            }
            UpdateFull();
        }

        public abstract void ProcessAdd(Entity e, bool append);

        public bool Add(Entity e)
        {
            bool result = AddNewMoveable(e);
            UpdateFull();
            return result;
        }

        private bool AddNewMoveable(Entity e)
        {
            if (e == Entity.Null) return false;
            if (_Buffer.ContainsKey(e)) return false;

            Moveable mv = Moveable.GetOrCreate(e);

            bool result = AddExistingMoveable(e, mv);
            mv.OnSelect();
            return result;
        }

        internal bool AddExistingMoveable(Entity e, Moveable mv)
        {
            _Buffer.Add(e, mv);
            return true;
        }


        /// <summary>
        /// Update selected objects' data, unselect invalid ones
        /// </summary>
        public virtual void Refresh()
        {
            for (int i = 0; i < _ControlPoints.Count; i++)
            {
                (CPDefinition cpd, ControlPoint oldCP) = _ControlPoints.ElementAt(i);
                ControlPoint newCP = _Tool.ControlPointManager.GetOrCreate(cpd);
                _ControlPoints[cpd] = newCP;

                if (_Buffer.Remove(oldCP.m_Entity))
                {
                    _Buffer.Add(newCP.m_Entity, newCP);
                }
            }

            HashSet<Entity> toRemove = new();
            foreach ((Entity e, Moveable mv) in _Buffer)
            {
                if (!mv.Refresh())
                {
                    toRemove.Add(e);
                    continue;
                }
            }
            foreach (Entity e in toRemove)
            {
                Remove(e);
            }

            UpdateFull();
        }

        /// <summary>
        /// Set the selection with required extras
        /// </summary>
        protected virtual void UpdateFull()
        {
            _ControlPoints.Clear();
            foreach ((Entity _, Moveable mv) in _Buffer)
            {
                if (mv is not ControlPoint cp) continue;

                _ControlPoints.Add(new(cp), cp);
            }

            _BufferFull.Clear();
            foreach (Moveable mv in Moveables)
            {
                _BufferFull.Add(mv);
                if (mv is Segment seg)
                {
                    for (int i = 0; i < Moveable.CURVE_CPS; i++)
                    {
                        ControlPoint cp = _Tool.ControlPointManager.GetOrCreate(seg.m_CPDefinitions[i]);
                        if (!_BufferFull.Contains(cp))
                        {
                            _BufferFull.Add(cp);
                        }
                    }
                }
                else if (mv is Node node)
                {
                    for (int i = 0; i < node.m_CPDefinitions.Count; i++)
                    {
                        ControlPoint cp = _Tool.ControlPointManager.GetOrCreate(node.m_CPDefinitions[i]);
                        if (!_BufferFull.Contains(cp))
                        {
                            _BufferFull.Add(cp);
                        }
                    }
                }
                else if (mv is Building b)
                {
                    foreach (var upgrade in b.GetInstalledUpgrades())
                    {
                        if (!_BufferFull.Contains(upgrade))
                        {
                            _BufferFull.Add(upgrade);
                        }
                    }
                }
            }
        }

        public void Remove(HashSet<Moveable> mvs)
        {
            foreach (var mv in mvs)
            {
                _Buffer.Remove(mv.m_Entity);
                mv.OnDeselect();
            }
            UpdateFull();
        }

        public void Remove(Entity e)
        {
            Moveable mv = _Buffer[e];
            _Buffer.Remove(e);
            mv.OnDeselect();
            UpdateFull();
        }

        public void Clear()
        {
            HashSet<Moveable> mvs = _Buffer.Values.ToHashSet();
            _Buffer.Clear();
            mvs.ForEach(mv => mv.OnDeselect());
            UpdateFull();
            mvs.Clear();
        }


        public bool Has(Entity e)
        {
            return _Buffer.ContainsKey(e);
        }

        public bool Has(Moveable mv)
        {
            return _Buffer.ContainsValue(mv);
        }

        public bool HasFull(Entity e)
        {
            return _BufferFull.Count(mv => mv.m_Entity == e) > 0;
        }

        public bool HasFull(Moveable mv)
        {
            return _BufferFull.Contains(mv);
        }

        public Moveable Get(Entity e)
        {
            return _Buffer[e];
        }

        public T Get<T>(Entity e) where T : Moveable
        {
            return (T)_Buffer[e];
        }

        public Moveable GetFull(Entity e)
        {
            return _BufferFull.First(mv => mv.m_Entity == e);
        }

        public T GetFull<T>(Entity e) where T : Moveable
        {
            return (T)_BufferFull.First(mv => mv.m_Entity == e);
        }

        /// <summary>
        /// Get the objects that actually transform
        /// </summary>
        internal abstract List<Moveable> GetObjectsToTransform();

        /// <summary>
        /// Get the objects that actually transform
        /// </summary>
        internal abstract List<Moveable> GetObjectsToTransformFull();

        public float3 GetCenterPoint()
        {
            int c = GetCountForCenter();
            if (c == 0) return new float3(0, 0, 0);
            if (c == 1)
            {
                foreach (Moveable mv in GetObjectsToTransform())
                {
                    return mv.Transform.m_Position;
                }
            }

            if (GetFurthestMoveables(out Moveable A, out Moveable B) < 0) return new float3(0, 0, 0);
            return math.lerp(A.Transform.m_Position, B.Transform.m_Position, 0.5f);
        }

        public abstract float GetFurthestMoveables(out Moveable a, out Moveable b);

        /// <summary>
        /// How many selected objects are relevant for calculating center point?
        /// </summary>
        internal abstract int GetCountForCenter();

        public Quad2 GetTotalRectangle(out Bounds3 bounds, float expand = 0, bool getFull = false)
        {
            bounds = GetTotalBounds(getFull);
            Quad2 rect = new(
                new(bounds.min.x - expand, bounds.min.z - expand),
                new(bounds.max.x + expand, bounds.min.z - expand),
                new(bounds.min.x - expand, bounds.max.z + expand),
                new(bounds.max.x + expand, bounds.max.z + expand));
            return rect;
        }

        public Bounds3 GetTotalBounds(bool getFull = true)
        {
            Bounds3 totalBounds = default;
            bool first = true;

            List<Moveable> source = getFull ? _BufferFull.ToList() : GetObjectsToTransform();

            foreach (Moveable mv in source)
            {
                if (first)
                {
                    totalBounds = mv.GetBounds();
                    first = false;
                }
                else
                {
                    totalBounds = totalBounds.Encapsulate(mv.GetBounds());
                }
            }

            return totalBounds;
        }

        public abstract string DebugSelection();


        #region Enumeration
        public IEnumerator<KeyValuePair<Entity, Moveable>> GetEnumerator() { return new Enumeration(_Buffer); }
        IEnumerator IEnumerable.GetEnumerator() { return new Enumeration(_Buffer); }

        private class Enumeration : IEnumerator<KeyValuePair<Entity, Moveable>>
        {
            private readonly Dictionary<Entity, Moveable> _List;
            private int _Position = -1;

            public Enumeration(Dictionary<Entity, Moveable> list)
            {
                _List = list;
            }

            public void Dispose()
            { }

            public bool MoveNext()
            {
                _Position++;
                return _Position < _List.Count;
            }

            public void Reset()
            {
                _Position = -1;
            }

            public KeyValuePair<Entity, Moveable> Current => (KeyValuePair<Entity, Moveable>)_Current;

            object IEnumerator.Current => _Current;

            private object _Current
            {
                get
                {
                    try
                    {
                        return _List.ElementAt(_Position);
                    }
                    catch (IndexOutOfRangeException)
                    {
                        throw new InvalidOperationException("Selection.Enumeration._current");
                    }
                }
            }
        }
        #endregion
    }
}
