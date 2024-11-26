using Colossal.UI.Binding;

namespace MoveIt.UI
{
    public class PanelState : IJsonWritable
    {
        public readonly TopRowButtonStates m_TopRow             = new();
        public readonly FilterSectionState m_FilterSection      = new();
        public readonly ToolboxSectionState m_ToolboxSection    = new();

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
            return "PanelState:\n" + m_TopRow + "\n" + m_FilterSection;
        }

        public override bool Equals(object obj)
        {
            if (obj is not PanelState ps) return false;

            return ps.m_TopRow.Equals(m_TopRow) && (ps.m_FilterSection is not null && ps.m_FilterSection.Equals(m_FilterSection));
        }

        // ReSharper disable once BaseObjectGetHashCodeCallInGetHashCode
        public override int GetHashCode() => base.GetHashCode();
    }
}
