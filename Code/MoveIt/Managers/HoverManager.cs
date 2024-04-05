using MoveIt.Moveables;
using MoveIt.Tool;
using System.Text;
using Unity.Entities;

namespace MoveIt.Managers
{
    public class HoverManager : MIT_Manager
    {
        /// <summary>
        /// The hovered object, or Entity.Null if none
        /// </summary>
        public Entity Entity
        {
            get => _Hovered;
            set => _Hovered = value;
        }
        private Entity _Hovered;

        /// <summary>
        /// The hovered object, or last valid object if none
        /// </summary>
        public Entity LastValid
        {
            get => _HoveredHit;
            set => _HoveredHit = value;
        }
        private Entity _HoveredHit = Entity.Null;

        /// <summary>
        /// The hovered object when the pointer button was pressed
        /// </summary>
        public Entity OnPress
        {
            get => _HoveredOnPress;
            set => _HoveredOnPress = value;
        }
        private Entity _HoveredOnPress = Entity.Null;

        /// <summary>
        /// Moveable object for _Hovered, if present
        /// </summary>
        public Moveable Moveable
        {
            get => _HoveredMoveable;
            set => _HoveredMoveable = value;
        }
        private Moveable _HoveredMoveable;

        public bool IsNull => Entity.Equals(Entity.Null);
        public bool IsSelected => !IsNull && (_Tool.Selection.Has(Entity) || _Tool.Manipulation.Has(Entity));
        public bool IsManipulatable => !IsNull && ((Moveable.m_Manipulatable & QTypes.Manipulate.Parent) > 0 || (Moveable.m_Manipulatable & QTypes.Manipulate.Child) > 0);
        public bool IsNormal => !IsNull && (Moveable.m_Manipulatable & QTypes.Manipulate.Normal) > 0;

        public bool Is(Moveable mv)
        {
            return Is(mv.m_Entity);
        }

        public bool Is(Entity e)
        {
            if (e == Entity.Null) return false;
            if (Entity == Entity.Null) return false;

            return e.Equals(Entity);
        }

        /// <summary>
        /// Refresh the hovered object on tool activation
        /// </summary>
        internal void Refresh()
        { }

        internal void Clear()
        {
            Moveable?.OnUnhover();
            Moveable?.Dispose();
            Moveable = null;
            Entity = Entity.Null;
            LastValid = Entity.Null;
            OnPress = Entity.Null;
        }

        internal void Process(Entity to)
        {
            if (Entity == to) return;
            if (_Tool.ToolState == ToolStates.ApplyButtonHeld || _Tool.ToolState == ToolStates.SecondaryButtonHeld) return;

            if (Entity != Entity.Null)
            {
                Moveable.OnUnhover();
                if (!(_Tool.Selection.Has(Entity) || _Tool.Manipulation.Has(Entity)) && Moveable.m_ObjectType != QTypes.ObjectType.Managed)
                {
                    Moveable.Dispose();
                    Moveable = null;
                }
            }

            Entity = Entity.Null;

            if (_Tool.IsValid(to))
            {
                Moveable = Moveable.GetOrCreate(to);
                Entity = to;
                LastValid = to;
                Moveable.OnHover();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            if (Entity.Equals(Entity.Null))
            {
                sb.Append("Nothing Hovered");
            }
            else
            {
                sb.AppendFormat("{0}-{1}", Entity.DX(true), Moveable.m_Manipulatable);
            }

            if (!LastValid.Equals(Entity.Null))
            {
                sb.AppendFormat(", Valid:{0}", LastValid.DX(true));
            }
            if (!OnPress.Equals(Entity.Null))
            {
                sb.AppendFormat(", Press:{0}", OnPress.DX(true));
            }
            return sb.ToString();
        }
    }
}
