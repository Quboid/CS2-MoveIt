using MoveIt.Tool;
using System;
using Unity.Entities;

namespace MoveIt.Moveables
{
    public readonly record struct MVDefinition : IEquatable<MVDefinition>, IEquatable<Moveable>
    {
        public readonly Identity m_Identity     = Identity.Invalid;
        public readonly Entity m_Entity         = Entity.Null;
        public readonly bool m_IsManipulatable  = false;
        public readonly bool m_IsManaged        = false;
        public readonly Entity m_Parent         = Entity.Null;
        public readonly Identity m_ParentId     = Identity.Invalid;
        public readonly short m_ParentKey       = -1;

        public readonly bool IsNull             => m_Entity.Equals(Entity.Null) && m_Parent.Equals(Entity.Null);
        public readonly bool IsChild            => !m_Parent.Equals(Entity.Null);

        public MVDefinition(Identity identity, Entity e, bool isManipulatable, bool isManaged, Entity parent, Identity parentId, short parentKey)
        {
            m_Identity          = identity;
            m_Entity            = e;
            m_IsManipulatable   = isManipulatable;
            m_IsManaged         = isManaged;
            m_Parent            = parent;
            m_ParentId          = parentId;
            m_ParentKey         = parentKey;
        }

        public MVDefinition(Identity identity, Entity e, bool isManipulatable)
        {
            m_Identity = identity;
            m_Entity = e;
            m_IsManipulatable = isManipulatable;
        }

        // Why on earth does C# not use my default values unless I have this?!
        public MVDefinition()
        { }

        public readonly bool Equals(MVDefinition rhs)
        {
            if (IsChild)
            {
                if (!m_Parent.Equals(rhs.m_Parent)) return false;
                if (m_ParentKey != rhs.m_ParentKey) return false;
                if (m_IsManipulatable != rhs.m_IsManipulatable) return false;
                return true;
            }
            if (!m_Entity.Equals(rhs.m_Entity)) return false;
            if (m_IsManipulatable != rhs.m_IsManipulatable) return false;
            if (m_IsManaged != rhs.m_IsManaged) return false;
            return true;
        }

        public readonly bool Equals(Moveable rhs)
        {
            if (rhs is null) return false;
            
            if (IsChild)
            {
                if (rhs is not MVControlPoint cp) return false;
                if (!m_Parent.Equals(cp.m_Parent)) return false;
                if (m_ParentKey != cp.m_ParentKey) return false;
                return m_IsManipulatable == cp.IsManipulatable;
            }

            if (!m_Entity.Equals(rhs.m_Entity)) return false;
            if (m_IsManipulatable != rhs.IsManipulatable) return false;
            return m_IsManaged == rhs.IsManaged;
        }

        public readonly override int GetHashCode()
        {
            unchecked
            {
                return (m_Entity.Index << 2) + (m_IsManaged ? 2 : 0) + (m_IsManipulatable ? 1 : 0);
            }
        }


        public override string ToString()
        {
            var msg = $"{m_Entity.DX()}{(m_IsManipulatable ? "-Manip" : "")}{(m_IsManaged ? "-Managed" : "")}";
            if (m_Parent.Equals(Entity.Null) && !m_Entity.Equals(Entity.Null)) return msg;
            return m_Parent.Equals(Entity.Null) ? $"{msg}-NullParent" : $"{msg}-Parent:{m_Parent.DX()}-{m_ParentKey}";
        }

        public string E()
        {
            return m_Entity.DX();
        }
    }
}
