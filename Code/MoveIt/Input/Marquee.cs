using Colossal.Mathematics;
using MoveIt.Actions.Select;
using MoveIt.Tool;
using QCommonLib;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace MoveIt.Input
{
    internal class Marquee
    {
        protected static readonly MIT _MIT = MIT.m_Instance;

        internal float3 m_StartPosition;
        internal Quad3 m_SelectArea;
        internal Quad3 m_LastSelectArea;
        internal HashSet<Entity> m_Entities;
        internal HashSet<Entity> m_EntitiesPrev;
        internal Bounds3 m_LastBounds;
        internal bool m_HasMoved;

        private readonly Overlays.OverlayMarquee _Overlay;

        internal Marquee(float3 position)
        {
            m_StartPosition = position;
            m_SelectArea = default;
            m_LastSelectArea = default;
            m_Entities = null;
            m_EntitiesPrev = null;
            m_LastBounds = new(float.MaxValue, float.MaxValue);
            m_HasMoved = false;

            _Overlay = Overlays.OverlayMarquee.HandlerFactory(2);
        }

        internal bool CheckIfMoved(float3 position)
        {
            if (m_HasMoved) return true;
            if (position.Equals(m_StartPosition)) return false;

            _MIT.Queue.Push(new SelectMarqueeAction(QKeyboard.Shift));
            _MIT.Queue.Do();
            m_HasMoved = true;
            return true;
        }

        internal bool Update(float3 position)
        {
            if (!m_HasMoved) return false;
            if (position.Equals(default)) return false;

            m_LastSelectArea = m_SelectArea;
            m_SelectArea.a = m_StartPosition;
            m_SelectArea.c = position;

            if (m_SelectArea.a.x == m_SelectArea.c.x && m_SelectArea.a.z == m_SelectArea.c.z)
            {
                m_SelectArea = default;
                return false;
            }

            float angle = Camera.main.transform.localEulerAngles.y * Mathf.Deg2Rad;
            float3 down = new Vector3(Mathf.Cos(angle), 0, -Mathf.Sin(angle));
            float3 right = new Vector3(-down.z, 0, down.x);

            float3 a = m_SelectArea.c - m_SelectArea.a;
            float dotDown = Vector3.Dot(a, down);
            float dotRight = Vector3.Dot(a, right);

            if ((dotDown > 0 && dotRight > 0) || (dotDown <= 0 && dotRight <= 0))
            {
                m_SelectArea.b = m_SelectArea.a + dotDown * down;
                m_SelectArea.d = m_SelectArea.a + dotRight * right;
            }
            else
            {
                m_SelectArea.b = m_SelectArea.a + dotRight * right;
                m_SelectArea.d = m_SelectArea.a + dotDown * down;
            }

            _Overlay.Update(m_SelectArea);

            return true;
        }

        internal Bounds3 GetBounds()
        {
            return new(m_SelectArea.Min(), m_SelectArea.Max());
        }

        internal void Dispose()
        {
            _Overlay.Dispose();
        }
    }
}
