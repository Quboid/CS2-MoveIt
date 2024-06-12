using Unity.Entities;

namespace MoveIt.Overlays
{
    public class OverlaySelectionCenter : Overlay
    {
        private static EntityArchetype _Archetype = _Tool.EntityManager.CreateArchetype(
            new ComponentType[] {
                typeof(MIO_Type),
                typeof(MIO_Common),
                typeof(MIO_Circle),
                typeof(MIO_Updateable),
                typeof(MIO_SelectionData)
            });

        public static Entity Factory(Entity owner)
        {
            Entity e = _Tool.EntityManager.CreateEntity(_Archetype);

            _Tool.EntityManager.SetComponentData<MIO_Type>(e, new(OverlayTypes.SelectionCenter));
            _Tool.EntityManager.SetComponentData<MIO_Common>(e, new(owner));
            return e;
        }


        public int m_Index;

        public OverlaySelectionCenter(int index) : base(OverlayTypes.SelectionCenter)
        {
            m_Index = index;
        }

        public override bool CreateOverlayEntity()
        {
            Entity owner = new() { Index = m_Index, Version = 1 };
            m_Entity = OverlaySelectionCenter.Factory(owner);
            return true;
        }

        public override bool Update()
        {
            if (!_Tool.Selection.Any)
            {
                if (!m_Entity.Equals(Entity.Null))
                {
                    _Tool.EntityManager.DestroyEntity(m_Entity);
                    m_Entity = Entity.Null;
                }
                return false;
            }

            if (m_Entity.Equals(Entity.Null))
            {
                CreateOverlayEntity();
            }

            MIO_SelectionData selection = new(
                _Tool.Selection.Count,
                _Tool.Selection.Center,
                _Tool.Selection.CenterTerrainHeight);
            _Tool.EntityManager.SetComponentData(m_Entity, selection);

            return true;
        }
    }
}
