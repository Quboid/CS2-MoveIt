using Unity.Entities;

namespace MoveIt.Overlays.Children
{
    public class OverlaySelectionCenter : Overlay
    {
        private static readonly EntityArchetype _Archetype = _MIT.EntityManager.CreateArchetype(
            new ComponentType[] {
                typeof(MIO_Type),
                typeof(MIO_Common),
                typeof(MIO_Circle),
                typeof(MIO_Updateable),
                typeof(MIO_SelectionData)
            });


        public readonly int m_Index;

        public OverlaySelectionCenter(int index) : base(OverlayTypes.SelectionCentralPoint, null)
        {
            m_Index = index;
        }

        protected override bool CreateOverlayEntity()
        {
            Entity owner = new() { Index = m_Index, Version = 1 };
            m_Entity = _MIT.EntityManager.CreateEntity(_Archetype);

            _MIT.EntityManager.SetComponentData<MIO_Type>(m_Entity, new(OverlayTypes.SelectionCentralPoint));
            _MIT.EntityManager.SetComponentData<MIO_Common>(m_Entity, new(owner));
            return true;
        }

        public override bool Update()
        {
            if (!_MIT.Selection.Any)
            {
                if (!m_Entity.Equals(Entity.Null))
                {
                    _MIT.EntityManager.DestroyEntity(m_Entity);
                    m_Entity = Entity.Null;
                }
                return false;
            }

            if (m_Entity.Equals(Entity.Null))
            {
                CreateOverlayEntity();
            }

            MIO_SelectionData selection = new(
                _MIT.Selection.Count,
                _MIT.Selection.Center,
                _MIT.Selection.CenterTerrainHeight);
            _MIT.EntityManager.SetComponentData(m_Entity, selection);

            return true;
        }
    }
}
