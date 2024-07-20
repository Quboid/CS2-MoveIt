using Colossal.UI.Binding;

namespace MoveIt.Systems.UIElements
{
    public class PanelState : IJsonWritable
    {
        public TopRowButtonStates m_TopRow;
        public FilterSectionStates m_FilterSection;

        public PanelState()
        {
            m_TopRow = new TopRowButtonStates();
            m_FilterSection = new FilterSectionStates();
        }

        public void Update()
        {
            m_TopRow.Update();
            m_FilterSection.Update();
        }

        public void Write(IJsonWriter writer)
        {
            writer.TypeBegin(GetType().FullName);
            writer.PropertyName("TopRow");
            writer.Write(m_TopRow);
            writer.PropertyName("FilterSection");
            writer.Write(m_FilterSection);
            writer.TypeEnd();
        }

        public override string ToString()
        {
            return "PanelState:\n" + m_TopRow.ToString() + "\n" + m_FilterSection.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj is not PanelState ps) return false;

            return ps.m_TopRow.Equals(m_TopRow) && ps.m_FilterSection.Equals(m_FilterSection);
        }

        public override int GetHashCode() => base.GetHashCode();
    }
}
