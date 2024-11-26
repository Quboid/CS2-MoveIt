using System.Linq;
using Colossal.UI.Binding;
using MoveIt.Tool;

namespace MoveIt.UI
{
    public class TopRowButtonStates : IJsonWritable
    {
        protected readonly MIT _MIT = MIT.m_Instance;
        private readonly ButtonState[] _Buttons;

        public TopRowButtonStates()
        {
            _Buttons = new ButtonState[]
            {
                new("undo",         false, true),
                new("single",       true, !_MIT.m_IsManipulateMode && !_MIT.m_MarqueeSelect),
                new("marquee",      true, !_MIT.m_IsManipulateMode && _MIT.m_MarqueeSelect),
                new("manipulation", true, _MIT.m_IsManipulateMode),
                new("redo",         false, true),
            };
        }

        public void Update()
        {
            _Buttons[0].Update(_MIT.Queue is not null && _MIT.Queue.CanUndo(), false);
            _Buttons[1].Update(true, !_MIT.m_IsManipulateMode && !_MIT.m_MarqueeSelect);
            _Buttons[2].Update(true, !_MIT.m_IsManipulateMode && _MIT.m_MarqueeSelect);
            _Buttons[3].Update(true, _MIT.m_IsManipulateMode);
            _Buttons[4].Update(_MIT.Queue is not null && _MIT.Queue.CanRedo(), false);
        }

        public void Write(IJsonWriter writer)
        {
            writer.TypeBegin(GetType().FullName);
            writer.PropertyName("ButtonUndo");
            writer.Write(_Buttons[0]);
            writer.PropertyName("ButtonSingle");
            writer.Write(_Buttons[1]);
            writer.PropertyName("ButtonMarquee");
            writer.Write(_Buttons[2]);
            writer.PropertyName("ButtonManipulation");
            writer.Write(_Buttons[3]);
            writer.PropertyName("ButtonRedo");
            writer.Write(_Buttons[4]);
            writer.TypeEnd();
        }

        public override string ToString()
        {
            var msg = $"TopRowButtonStates ({_Buttons.Length}):";
            return _Buttons.Aggregate(msg, (current, t) => current + $"\n    {t}");
        }

        public override bool Equals(object obj)
        {
            if (obj is not TopRowButtonStates trbs) return false;

            return !_Buttons.Where((t, i) => !trbs._Buttons[i].Equals(t)).Any();
        }

        // ReSharper disable once BaseObjectGetHashCodeCallInGetHashCode
        public override int GetHashCode() => base.GetHashCode();
    }
}
