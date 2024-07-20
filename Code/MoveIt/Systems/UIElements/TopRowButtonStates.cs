using Colossal.UI.Binding;
using MoveIt.Tool;

namespace MoveIt.Systems.UIElements
{
    public class TopRowButtonStates : IJsonWritable
    {
        public readonly MIT _Tool = MIT.m_Instance;
        public ButtonState[] m_Buttons;

        public TopRowButtonStates()
        {
            m_Buttons = new ButtonState[]
            {
                new("undo",         _Tool.Queue is not null && _Tool.Queue.CanUndo(), true),
                new("single",       true, !_Tool.m_IsManipulateMode && !_Tool.m_MarqueeSelect),
                new("marquee",      true, !_Tool.m_IsManipulateMode && _Tool.m_MarqueeSelect),
                new("manipulation", true, _Tool.m_IsManipulateMode),
                new("redo",         _Tool.Queue is not null && _Tool.Queue.CanRedo(), true),
            };
        }

        public void Update()
        {
            m_Buttons[0].Update(_Tool.Queue is not null && _Tool.Queue.CanUndo(), false);
            m_Buttons[1].Update(true, !_Tool.m_IsManipulateMode && !_Tool.m_MarqueeSelect);
            m_Buttons[2].Update(true, !_Tool.m_IsManipulateMode && _Tool.m_MarqueeSelect);
            m_Buttons[3].Update(true, _Tool.m_IsManipulateMode);
            m_Buttons[4].Update(_Tool.Queue is not null && _Tool.Queue.CanRedo(), false);
        }

        public void Write(IJsonWriter writer)
        {
            writer.TypeBegin(GetType().FullName);
            writer.PropertyName("ButtonUndo");
            writer.Write(m_Buttons[0]);
            writer.PropertyName("ButtonSingle");
            writer.Write(m_Buttons[1]);
            writer.PropertyName("ButtonMarquee");
            writer.Write(m_Buttons[2]);
            writer.PropertyName("ButtonManipulation");
            writer.Write(m_Buttons[3]);
            writer.PropertyName("ButtonRedo");
            writer.Write(m_Buttons[4]);
            writer.TypeEnd();
        }

        public override string ToString()
        {
            string msg = $"TopRowButtonStates ({m_Buttons.Length}):";
            for (int i = 0; i < m_Buttons.Length; i++)
            {
                msg += $"\n    {m_Buttons[i]}";
            }
            return msg;
        }

        public override bool Equals(object obj)
        {
            if (obj is not TopRowButtonStates trbs) return false;

            for (int i = 0; i < m_Buttons.Length; i++)
            {
                if (!trbs.m_Buttons[i].Equals(m_Buttons[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode() => base.GetHashCode();
    }
}
