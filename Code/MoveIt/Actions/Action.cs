using MoveIt.Overlays;
using MoveIt.Tool;
using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;

namespace MoveIt.Actions
{
    internal class ActionState : IDisposable
    {
        protected readonly MIT _Tool = MIT.m_Instance;

        public virtual void Dispose() { }

        public override string ToString()
        {
            return string.Empty;
        }
    }

    internal abstract class Action
    {
        protected readonly MIT _Tool = MIT.m_Instance;

        public virtual string Name => "Action";
        public int m_InitialFrame;
        public (bool from, bool to) m_IsManipulate;
        internal float4 m_UpdateArea;

        /// <summary>
        /// Has anything moved for the creation engine to deal with this frame?
        /// </summary>
        internal bool m_UpdateMove = false;

        /// <summary>
        /// Has anything rotated for the creation engine to deal with this frame?
        /// </summary>
        internal bool m_UpdateRotate = false;

        public Action()
        {
            m_InitialFrame = UnityEngine.Time.frameCount;
            m_IsManipulate.from = _Tool.Manipulating;
        }

        public virtual HashSet<Utils.IOverlay> GetOverlays()
        {
            return new();
        }

        public virtual ActionState GetActionState() => new();

        public virtual void Do() { m_IsManipulate.to = _Tool.Manipulating; }
        public virtual void Undo() { _Tool.SetManipulationMode(m_IsManipulate.from); }
        public virtual void Redo() { _Tool.SetManipulationMode(m_IsManipulate.to); }
        public abstract void UpdateEntityReferences(Dictionary<Entity, Entity> toUpdate);

        internal virtual void OnHold() { }
        internal virtual void OnHoldEnd() { }
    }
}
