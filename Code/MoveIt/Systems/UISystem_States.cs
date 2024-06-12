using Colossal.UI.Binding;
using MoveIt.Tool;

namespace MoveIt.Systems
{
    internal partial class MIT_UISystem
    {
        public class ButtonState : IJsonWritable
        {
            public string m_Id;
            public bool m_Enabled;
            public bool m_Active;

            private bool _Changed;

            public ButtonState(string id, bool enabled, bool active)
            {
                m_Id = id;
                m_Enabled = enabled;
                m_Active = active;

                _Changed = true;
            }

            public void Update(bool enabled, bool active)
            {
                if (m_Enabled == enabled && m_Active == active)
                {
                    return;
                }

                m_Enabled = enabled;
                m_Active = active;
                _Changed = true;
            }

            public void Write(IJsonWriter writer)
            {
                writer.TypeBegin(GetType().FullName);
                writer.PropertyName("Id");
                writer.Write(m_Id);
                writer.PropertyName("IsEnabled");
                writer.Write(m_Enabled);
                writer.PropertyName("IsActive");
                writer.Write(m_Active);
                writer.TypeEnd();
            }

            public override string ToString()
            {
                return $"{m_Id} E:{m_Enabled}, A:{m_Active}";
            }

            public override bool Equals(object obj)
            {
                if (obj is not ButtonState) return false;

                if (_Changed)
                {
                    _Changed = false;
                    return false;
                }

                return true;
            }

            public override int GetHashCode() => base.GetHashCode();
        }


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
                m_Buttons[0].Update(_Tool.Queue.CanUndo(), false);
                m_Buttons[1].Update(true, !_Tool.m_IsManipulateMode && !_Tool.m_MarqueeSelect);
                m_Buttons[2].Update(true, !_Tool.m_IsManipulateMode && _Tool.m_MarqueeSelect);
                m_Buttons[3].Update(true, _Tool.m_IsManipulateMode);
                m_Buttons[4].Update(_Tool.Queue.CanRedo(), false);
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


        public class PanelState : IJsonWritable
        {
            public TopRowButtonStates m_TopRow;

            public PanelState()
            {
                m_TopRow = new TopRowButtonStates();
            }

            public void Update()
            {
                m_TopRow.Update();
            }

            public void Write(IJsonWriter writer)
            {
                writer.TypeBegin(GetType().FullName);
                writer.PropertyName("TopRow");
                writer.Write(m_TopRow);
                writer.TypeEnd();
            }

            public override string ToString()
            {
                return m_TopRow.ToString();
            }

            public override bool Equals(object obj)
            {
                if (obj is not PanelState ps) return false;

                return ps.m_TopRow.Equals(m_TopRow);
            }

            public override int GetHashCode() => base.GetHashCode();
        }
    }
}
