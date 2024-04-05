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
    public class Main : Base
    {
        public Main() : base() { }
        public Main(Main old) : base(old) { }

        public override void ProcessAdd(Entity e, bool append)
        {
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
            else if (!_Tool.Selection.Has(e))
            {
                Clear();
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
                for (int j = i + 1; j < Count; j++)
                {
                    float dist = MIT.GetDistanceBetween2D(Moveables[i], Moveables[j]);
                    if (dist > furthest)
                    {
                        a = Moveables[i];
                        b = Moveables[j];
                        furthest = dist;
                    }
                }
            }

            return furthest;
        }

        internal override int GetCountForCenter()
        {
            return Count;
        }

        internal override List<Moveable> GetObjectsToTransform()
        {
            bool isSegmentMove = Moveables.Count(mv => mv.m_Identity != QTypes.Identity.Segment) == 0;

            return Moveables.Where(mv => 
                (mv.m_Identity == QTypes.Identity.Segment) == isSegmentMove
            ).ToList();
        }

        internal override List<Moveable> GetObjectsToTransformFull()
        {
            bool isSegmentMove = Moveables.Count(mv => mv.m_Identity != QTypes.Identity.Segment) == 0;

            var moveables = FullSelection.Where(mv =>
                (mv.m_Identity == QTypes.Identity.Segment) == isSegmentMove
            ).ToList();


            int max = moveables.Count;

            for (int i = 0; i < max; i++)
            {
                var mv = moveables[i];
                var list = mv.GetChildren<Moveable>();

                foreach (var child in list)
                {
                    if (!moveables.Contains(child))
                    {
                        moveables.Add(child);
                    }
                }
            }

            return moveables;
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
                    sb.AppendFormat("{0} {1}, ", mv.D(), mv.m_Identity);
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
