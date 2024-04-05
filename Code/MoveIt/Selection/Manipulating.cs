using MoveIt.Managers;
using MoveIt.Moveables;
using MoveIt.Tool;
using QCommonLib;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Entities;

namespace MoveIt.Selection
{
    public class Manipulating : Base
    {
        public Manipulating() : base() { }
        public Manipulating(Manipulating old) : base(old) { }

        public override void ProcessAdd(Entity e, bool append)
        {
            Moveable mv = Moveable.GetOrCreate(e);
            if (append)
            {
                if (Has(e))
                {
                    Remove(e);
                }
                else
                {
                    Add(e);
                }
            }
            else if (!_Tool.Manipulation.Has(e))
            {
                if ((mv.m_Manipulatable & QTypes.Manipulate.Child) != 0)
                {
                    HashSet<Entity> toRemove = new();
                    foreach ((Entity e1, Moveable mv1) in _Buffer)
                    {
                        if ((mv1.m_Manipulatable & QTypes.Manipulate.Parent) == 0)
                        {
                            toRemove.Add(e1);
                        }
                    }
                    foreach (Entity e1 in toRemove)
                    {
                        _Buffer.Remove(e1);
                    }
                }
                else
                {
                    Clear();
                }
                Add(e);
            }
        }

        public override float GetFurthestMoveables(out Moveable a, out Moveable b)
        {
            a = null;
            b = null;
            float furthest = -1f;
            if (Count == 0)
            {
                return 0;
            }
            if (Count == 1)
            {
                a = Moveables[0];
                return 0;
            }

            for (int i = 0; i < Count - 1; i++)
            {
                if ((Moveables[i].m_Manipulatable & QTypes.Manipulate.Child) == 0) continue;
                for (int j = i + 1; j < Count; j++)
                {
                    if ((Moveables[j].m_Manipulatable & QTypes.Manipulate.Child) == 0) continue;
                    float dist = MIT.GetDistanceBetween2D(Moveables[i], Moveables[j]);
                    if (dist > furthest)
                    {
                        a = Moveables[i];
                        b = Moveables[j];
                        furthest = dist;
                    }
                }
            }

            //MIT.Log.Debug($"Furthest: {a.m_Entity.D()} - {b.m_Entity.D()} ({furthest}m)");
            return furthest;
        }

        internal override int GetCountForCenter()
        {
            return GetObjectsToTransform().Count;
        }

        internal override List<Moveable> GetObjectsToTransform()
        {
            return _Buffer.Values.Where(mv =>
                (mv.m_Manipulatable & QTypes.Manipulate.Child) > 0 &&
                Has(mv.m_Entity)
            ).ToList();
        }

        internal override List<Moveable> GetObjectsToTransformFull()
        {
            return _BufferFull.Where(mv =>
                (mv.m_Manipulatable & QTypes.Manipulate.Child) > 0 &&
                Has(mv.m_Entity)
            ).ToList();
        }

        public int GetCountOfType(QTypes.Manipulate manipulate)
        {
            return _Buffer.Count(kvp => kvp.Value.m_Manipulatable == manipulate);
        }

        public override string DebugSelection()
        {
            StringBuilder sb = new("[" + Count.ToString() + "/" + _BufferFull.Count);
            if (_BufferFull.Count > 0)
            {
                sb.Append(' ');
                foreach (Moveable mv in _BufferFull)
                {
                    if (!_Buffer.ContainsValue(mv)) sb.Append("*");
                    sb.Append(mv.D() + ", ");
                }
                sb.Remove(sb.Length - 2, 2);
            }
            if (_BufferFull.Count > 0 && _ControlPoints.Count > 0)
            {
                sb.Append(" |");
            }
            if (_ControlPoints.Count > 0)
            {
                sb.Append(' ');
                foreach ((CPDefinition _, ControlPoint cp) in _ControlPoints)
                {
                    sb.Append(cp.m_Entity.D() + ", ");
                }
                sb.Remove(sb.Length - 2, 2);
            }
            sb.Append(']');
            return sb.ToString();
        }
    }
}
