using MoveIt.Tool;
using QCommonLib;
using System;
using Unity.Entities;

namespace MoveIt.QAccessor
{
    /// <summary>
    /// Additional accessor for entities, does not include children
    /// </summary>
    public struct QObjectSimple : IDisposable
    {
        internal EntityManager m_Manager;
        public Entity m_Entity;
        internal QEntity m_Parent;
        internal Identity m_Identity;

        internal QObjectSimple(EntityManager manager, ref QLookup lookup, Entity e)
        {
            if (e == Entity.Null) throw new ArgumentNullException("Creating QObject with null entity");

            m_Manager   = manager;
            m_Entity    = e;
            m_Identity  = QTypes.GetEntityIdentity(manager, e);
            m_Parent    = new(m_Manager, ref lookup, e, m_Identity);

            //DebugDumpFullObject();
        }

        public readonly bool TryGetComponent<T>(out T component) where T : unmanaged, IComponentData
        {
            return m_Parent.TryGetComponent<T>(out component);
        }

        public readonly bool TryGetBuffer<T>(out DynamicBuffer<T> buffer, bool isReadOnly = false) where T : unmanaged, IBufferElementData
        {
            return m_Parent.TryGetBuffer<T>(out buffer, isReadOnly);
        }

        public readonly void Dispose() { }


        public readonly override string ToString()
        {
            return $"{m_Identity}/{m_Entity.D()}";
        }


#if USE_BURST
        internal readonly void DebugDumpFullObject()
        { } // Do nothing if in burst mode

        internal readonly string DebugFullObject() => ""; // Do nothing if in burst mode
#else
        internal readonly string DebugFullObject()
        {
            return ToString();
        }

        internal readonly void DebugDumpFullObject(string prefix = "")
        {
            QLog.Debug(prefix + DebugFullObject());
        }
#endif
    }
}
