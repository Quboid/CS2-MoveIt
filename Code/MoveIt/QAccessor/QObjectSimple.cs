using System;
using Unity.Entities;

namespace MoveIt.QAccessor
{
    /// <summary>
    /// Additional accessor for entities, does not include children
    /// </summary>
    public struct QObjectSimple : IDisposable
    {
        public Entity m_Entity;
        public QEntity m_Parent;
        public QNode m_ParentNode;
        public QControlPoint m_ParentCP;
        public QSegment m_ParentSegment;
        internal QTypes.Identity m_Identity;

        internal QObjectSimple(Entity e, SystemBase system)
        {
            if (e == Entity.Null) throw new ArgumentNullException("Creating QObject with null entity");

            m_Entity = e;
            m_Identity = QTypes.GetEntityIdentity(e);
            m_Parent = new(e, system, m_Identity);
            m_ParentNode = new(e, system, m_Identity);
            m_ParentCP = new(e, system, m_Identity);
            m_ParentSegment = new(e, system, m_Identity);
        }

        public readonly IQEntity Parent => m_Identity switch
        {
            QTypes.Identity.Node => m_ParentNode,
            QTypes.Identity.ControlPoint => m_ParentCP,
            QTypes.Identity.Segment => m_ParentSegment,
            _ => m_Parent,
        };

        public readonly T GetComponent<T>() where T : unmanaged, IComponentData
        {
            return Parent.GetComponent<T>();
        }

        public readonly bool TryGetComponent<T>(out T component) where T : unmanaged, IComponentData
        {
            return Parent.TryGetComponent<T>(out component);
        }

        public readonly DynamicBuffer<T> GetBuffer<T>(bool isReadOnly = false) where T : unmanaged, IBufferElementData
        {
            return Parent.GetBuffer<T>(isReadOnly);
        }

        public readonly bool TryGetBuffer<T>(out DynamicBuffer<T> buffer, bool isReadOnly = false) where T : unmanaged, IBufferElementData
        {
            return Parent.TryGetBuffer<T>(out buffer, isReadOnly);
        }

        public readonly void Dispose() {}
    }
}
