using Colossal.UI.Binding;

namespace MoveIt.UI
{
    public class PanelState : IJsonWritable
    {
        public TopRowButtonStates m_TopRow;
        public FilterSectionState m_FilterSection;// => MIT.m_Instance?.Filtering?.m_StatesData;
        public ToolboxSectionState m_ToolboxSection;

        public PanelState()
        {
            m_TopRow = new TopRowButtonStates();
            m_FilterSection = new FilterSectionState();
            m_ToolboxSection = new ToolboxSectionState();
        }

        public void Update()
        {
            m_TopRow.Update();
            m_FilterSection.Update();
            m_ToolboxSection.Update();
        }

        public void Write(IJsonWriter writer)
        {
            writer.TypeBegin(GetType().FullName);
            writer.PropertyName("TopRow");
            writer.Write(m_TopRow);
            writer.PropertyName("FilterSection");
            writer.Write(m_FilterSection);
            writer.PropertyName("ToolboxSection");
            writer.Write(m_ToolboxSection);
            writer.TypeEnd();
        }

        public override string ToString()
        {
            return "PanelState:\n" + m_TopRow.ToString() + "\n" + m_FilterSection.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj is not PanelState ps) return false;

            return ps.m_TopRow.Equals(m_TopRow) && (ps.m_FilterSection is not null && ps.m_FilterSection.Equals(m_FilterSection));
        }

        public override int GetHashCode() => base.GetHashCode();
    }
}
