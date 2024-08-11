using Game.Tools;
using MoveIt.Tool;
using Unity.Entities;

namespace MoveIt.Systems
{
    internal abstract partial class MIT_System : SystemBase
    {
        protected static readonly MIT _MIT = MIT.m_Instance;

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


    internal abstract partial class MIT_ToolSystem : ObjectToolBaseSystem
    {
        protected static MIT _MIT;

        protected override void OnCreate()
        {
            _MIT = World.GetOrCreateSystemManaged<MIT>();
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
