//using Game.Rendering;
//using Unity.Collections;
//using Unity.Mathematics;

//namespace MoveIt.Overlays
//{
//    public partial struct Overlay
//    {
//        public struct Diamond : IData
//        {
//            public MIT_CommonData Common { get; } = new();
//            public float m_Diameter = 0f;
//            public NativeArray<float3> m_Vertices = new();
//            public float m_Width = s_DefaultLineWidth;

//            public Diamond()
//            {
//                m_Vertices = new(8, Allocator.Persistent);
//                float r = m_Diameter / 2;
//                float w = m_Width;
//                float h = w / 2;

//                m_Vertices[0] = new(-r, 0f, r - h);
//                m_Vertices[1] = new(r - w, 0f, r - h);
//                m_Vertices[2] = new(r - h, 0f, r);
//                m_Vertices[3] = new(r - h, 0f, -r + w);
//                m_Vertices[4] = new(r, 0f, -r + h);
//                m_Vertices[5] = new(-r + w, 0f, -r + h);
//                m_Vertices[6] = new(-r + h, 0f, -r);
//                m_Vertices[7] = new(-r + h, 0f, r - w);
//            }
//        }

//        internal void Add(Diamond data)
//        {
//            m_Type = OverlayTypes.Diamond;
//            m_Common = data.Common;
//            m_Vertices = data.m_Vertices;
//        }

//        internal void DrawDiamond(OverlayRenderSystem.Buffer buffer)
//        {

//        }
//    }
//}