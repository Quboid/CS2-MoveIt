using MoveIt.Moveables;
using MoveIt.Tool;
using QCommonLib;
using System.Text;
using Unity.Collections;
using Unity.Entities;

namespace MoveIt.Managers
{
    public class HoverManager : MIT_Manager
    {
        //private readonly MIT_HoverSystem _HoverSystem;

        /// <summary>
        /// The hovered object, or default MVDefinition if none
        /// </summary>
        public MVDefinition Definition
        {
            get => _Definition;
            set => _Definition = value;
        }
        private MVDefinition _Definition;

        /// <summary>
        /// The hovered object, or last valid object if none
        /// </summary>
        public MVDefinition LastValid
        {
            get => _HoveredHit;
            set => _HoveredHit = value;
        }
        private MVDefinition _HoveredHit = new();

        /// <summary>
        /// The hovered object when the pointer button was pressed
        /// </summary>
        public MVDefinition OnPress
        {
            get => _HoveredOnPress;
            set => _HoveredOnPress = value;
        }
        private MVDefinition _HoveredOnPress = new();

        /// <summary>
        /// Moveable object for _Hovered, if present
        /// </summary>
        public Moveable MV
        {
            get => _HoveredMoveable;
            set => _HoveredMoveable = value;
        }
        private Moveable _HoveredMoveable;

        public bool IsNull          => Definition.m_Entity.Equals(Entity.Null) && Definition.m_Parent.Equals(Entity.Null);
        public bool IsSelected      => !IsNull && _Tool.Selection.Has(Definition);
        public bool IsManipulatable => !IsNull && MV.IsManipulatable;

        //public HoverManager()
        //{
        //    _HoverSystem = _Tool.World.GetOrCreateSystemManaged<MIT_HoverSystem>();
        //}

        public bool Is(MVDefinition mvd)
        {
            return mvd.Equals(Definition);
        }

        /// <summary>
        /// Refresh the hovered object on tool activation
        /// </summary>
        internal void Refresh()
        { }

        /// <summary>
        /// Set hovered to nothing
        /// </summary>
        internal void Clear()
        {
            if (MV is not null)
            {
                Unset();
            }
            Definition  = new();
            LastValid   = new();
            OnPress     = new();
        }

        internal void Process(Game.Tools.ToolRaycastSystem toolRaycastSystem)
        {
            NativeArray<Game.Common.RaycastResult> vanillaRaycastResults = _Tool.m_RaycastSystem.GetResult(toolRaycastSystem);

            Searcher.Ray searcher = new(Searcher.Filters.All, vanillaRaycastResults, _Tool.IsManipulating);
            (Entity e, float d)[] RaycastResults = searcher.OnLine(_Tool.m_RaycastTerrain.Line, _Tool.m_PointerPos);

            MVDefinition to = new();
            if (RaycastResults.Length > 0)
            {
                Entity e = RaycastResults[0].e;
                Identity id = QTypes.GetEntityIdentity(e);
                Entity parent = Entity.Null;
                short parentKey = -1;
                if (id == Identity.ControlPoint)
                {
                    var component = _Tool.EntityManager.GetComponentData<Components.MIT_ControlPoint>(e);
                    parent = component.m_Parent;
                    parentKey = component.m_ParentKey;
                }
                to = new(id, e, QTypes.IsManipulationPredict(id, _Tool.IsManipulating), false, parent, parentKey);
            }


            if (Definition.Equals(to)) return;

            if (_Tool.ToolState == ToolStates.ApplyButtonHeld || _Tool.ToolState == ToolStates.SecondaryButtonHeld) return;

            if (!Definition.m_Entity.Equals(Entity.Null))
            {
                Unset();
            }

            Definition = new();

            if (_Tool.IsValid(to))
            {
                if (_Tool.IsManipulating != to.m_IsManipulatable) return;
                //if (_Tool.IsManipulating && !_Tool.CanManipulate(to.m_Entity)) return;
                //if (!_Tool.IsManipulating && _Tool.CanOnlyManipulate(to)) return;

                MV          = _Tool.Moveables.GetOrCreate<Moveable>(to);
                Definition  = to;
                LastValid   = to;
                MV.OnHover();
            }
        }

        private void Unset()
        {
            Moveable mv = MV;
            Definition = new();
            MV = null;
            mv.OnUnhover();
        }


        public override string ToString()
        {
            StringBuilder sb = new();
            if (Definition.IsNull)
            {
                sb.Append("Nothing Hovered");
            }
            else
            {
                sb.AppendFormat("{0}-{1}", Definition.m_Entity.DX(true), (MV.IsManipulatable ? (MV.IsChild ? "C" : "P") : "N"));
            }

            if (!LastValid.Equals(Entity.Null))
            {
                sb.AppendFormat(", Valid:{0}", LastValid.m_Entity.DX(true));
            }
            if (!OnPress.Equals(Entity.Null))
            {
                sb.AppendFormat(", Press:{0}", OnPress.m_Entity.DX(true));
            }
            return sb.ToString();
        }
    }
}
