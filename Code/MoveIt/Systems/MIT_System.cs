using MoveIt.Tool;
using Unity.Entities;

namespace MoveIt.Systems
{
    internal abstract partial class MIT_System : SystemBase
    {
        protected readonly MIT _Tool = MIT.m_Instance;

        protected override void OnCreate()
        {
            Enabled = false;
        }

        internal virtual void Start()
        {
            Enabled = true;
        }

        internal virtual void Start(Actions.Action action, int actionIndex)
        {
            Enabled = true;
        }

        internal virtual void End()
        {
            Enabled = false;
        }
    }
}
