using MoveIt.Tool;
using QCommonLib;
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
        public readonly short m_ParentKey       = -1;

        public readonly bool IsNull             => m_Entity.Equals(Entity.Null) && m_Parent.Equals(Entity.Null);
        public readonly bool IsChild            => !m_Parent.Equals(Entity.Null);

        public MVDefinition(Identity identity, Entity e, bool isManipulatable, bool isManaged, Entity parent, short parentKey)
        {
            m_Identity          = identity;
            m_Entity            = e;
            m_IsManipulatable   = isManipulatable;
            m_IsManaged         = isManaged;
            m_Parent            = parent;
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
            if (IsChild)
            {
                if (rhs is MVControlPoint cp)
                {
                    if (!m_Parent.Equals(cp.m_Parent)) return false;
                    if (m_ParentKey != cp.m_ParentKey) return false;
                    if (m_IsManipulatable != cp.IsManipulatable) return false;
                    return true;
                }
                return false;
            }

            if (!m_Entity.Equals(rhs.m_Entity)) return false;
            if (m_IsManipulatable != rhs.IsManipulatable) return false;
            if (m_IsManaged != rhs.IsManaged) return false;
            return true;
        }

        public override readonly int GetHashCode()
        {
            unchecked
            {
                return (m_Entity.Index << 2) + (m_IsManaged ? 2 : 0) + (m_IsManipulatable ? 1 : 0);
            }
        }


        public override string ToString()
        {
            string msg = $"{m_Identity}-{m_Entity.DX()}{(m_IsManipulatable ? "-Manip" : "")}{(m_IsManaged ? "-Managed" : "")}{(IsChild ? "-Child" : "")}";
            if (m_Parent.Equals(Entity.Null) && !m_Entity.Equals(Entity.Null)) return msg;
            return $"{msg}-{m_Parent.DX()}-{m_ParentKey}";
        }
    }
}
