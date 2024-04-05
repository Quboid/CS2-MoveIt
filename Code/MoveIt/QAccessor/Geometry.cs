//using Colossal.Mathematics;
//using QCommonLib;
//using Unity.Mathematics;
//using UnityEngine;

//namespace MoveIt.QAccessor
//{
//    public struct Geometry
//    {
//        /// <summary>
//        /// Alter one end of a segment
//        /// </summary>
//        //public struct End
//        //{
//        //    internal static Game.Net.EdgeNodeGeometry MoveEdgeNodeGeometry(Game.Net.EdgeNodeGeometry input, float3 delta, bool isStart, bool isTopLevel)
//        //    {
//        //        input.m_Left = MoveSegment(input.m_Left, delta, isStart, isTopLevel);
//        //        input.m_Right = MoveSegment(input.m_Right, delta, isStart, isTopLevel);
//        //        input.m_Middle = MoveBezier4x3(input.m_Middle, delta, isStart, isTopLevel);
//        //        input.m_Bounds = UpdateBounds3(input);
//        //        return input;
//        //    }

//        //    internal static Game.Net.EdgeNodeGeometry RotateEdgeNodeGeometry(Game.Net.EdgeNodeGeometry input, ref Matrix4x4 matrix, float3 origin, bool isStart, bool isTopLevel)
//        //    {
//        //        input.m_Left = RotateSegment(input.m_Left, ref matrix, origin, isStart, isTopLevel);
//        //        input.m_Right = RotateSegment(input.m_Right, ref matrix, origin, isStart, isTopLevel);
//        //        input.m_Middle = RotateBezier4x3(input.m_Middle, ref matrix, origin, isStart, isTopLevel);
//        //        input.m_Bounds = UpdateBounds3(input);
//        //        return input;
//        //    }

//        //    internal static Game.Net.Segment MoveSegment(Game.Net.Segment input, float3 delta, bool isStart, bool isTopLevel)
//        //    {
//        //        input.m_Left = MoveBezier4x3(input.m_Left, delta, isStart, isTopLevel);
//        //        input.m_Right = MoveBezier4x3(input.m_Right, delta, isStart, isTopLevel);
//        //        return input;
//        //    }

//        //    internal static Game.Net.Segment RotateSegment(Game.Net.Segment input, ref Matrix4x4 matrix, float3 origin, bool isStart, bool isTopLevel)
//        //    {
//        //        input.m_Left = RotateBezier4x3(input.m_Left, ref matrix, origin, isStart, isTopLevel);
//        //        input.m_Right = RotateBezier4x3(input.m_Right, ref matrix, origin, isStart, isTopLevel);
//        //        return input;
//        //    }

//        //    internal static Bezier4x3 MoveBezier4x3(Bezier4x3 input, float3 delta, bool isStart, bool isTopLevel)
//        //    {
//        //        if (isStart)
//        //        {
//        //            input.a += delta;
//        //            //if (!isTopLevel)
//        //            input.b += delta;
//        //        }
//        //        else
//        //        {
//        //            input.d += delta;
//        //            //if (!isTopLevel)
//        //            input.c += delta;
//        //        }
//        //        return input;
//        //    }

//        //    internal static Bezier4x3 RotateBezier4x3(Bezier4x3 input, ref Matrix4x4 matrix, float3 origin, bool isStart, bool isTopLevel)
//        //    {
//        //        //string msg = $"BezRot {origin.DX()} isStart:{isStart} isTopLevel:{isTopLevel} matrx:{matrix}";
//        //        //msg += $"\n    {input.a.DX(),22} {input.b.DX(),22} {input.c.DX(),22} {input.d.DX(),22}";
//        //        if (isStart)
//        //        {
//        //            input.a = (float3)matrix.MultiplyPoint(input.a - origin);
//        //            //if (!isTopLevel)
//        //            input.b = (float3)matrix.MultiplyPoint(input.b - origin);
//        //        }
//        //        else
//        //        {
//        //            input.d = (float3)matrix.MultiplyPoint(input.d - origin);
//        //            //if (!isTopLevel) 
//        //            input.c = (float3)matrix.MultiplyPoint(input.c - origin);
//        //        }
//        //        //msg += $"\n    {input.a.DX(),22} {input.b.DX(),22} {input.c.DX(),22} {input.d.DX(),22}";
//        //        //QLog.Debug(msg);
//        //        return input;
//        //    }
//        //}

//        /// <summary>
//        /// Alter a full segment
//        /// </summary>
//        public struct Full
//        {
//            //internal static Game.Net.Segment MoveSegment(Game.Net.Segment input, float3 delta)
//            //{
//            //    input.m_Left = MoveBezier4x3(input.m_Left, delta);
//            //    input.m_Right = MoveBezier4x3(input.m_Right, delta);
//            //    return input;
//            //}

//            //internal static Game.Net.Segment RotateSegment(Game.Net.Segment input, ref Matrix4x4 matrix, float3 origin)
//            //{
//            //    input.m_Left = RotateBezier4x3(input.m_Left, ref matrix, origin);
//            //    input.m_Right = RotateBezier4x3(input.m_Right, ref matrix, origin);
//            //    return input;
//            //}

//            //internal static Bezier4x3 MoveBezier4x3(Bezier4x3 input, float3 delta)
//            //{
//            //    input.a += delta;
//            //    input.b += delta;
//            //    input.c += delta;
//            //    input.d += delta;
//            //    return input;
//            //}

//            //internal static Bezier4x3 RotateBezier4x3(Bezier4x3 input, ref Matrix4x4 matrix, float3 origin)
//            //{
//            //    input.a = (float3)matrix.MultiplyPoint(input.a - origin);
//            //    input.b = (float3)matrix.MultiplyPoint(input.b - origin);
//            //    input.c = (float3)matrix.MultiplyPoint(input.c - origin);
//            //    input.d = (float3)matrix.MultiplyPoint(input.d - origin);
//            //    return input;
//            //}
//        }


//        //internal static Bounds3 MoveBounds3(Bounds3 input, float3 delta)
//        //{
//        //    input.min += delta;
//        //    input.max += delta;
//        //    return input;
//        //}

//        /// <summary>
//        /// Must be called after the m_Left and m_Right values are updated!
//        /// </summary>
//        /// <param name="input">The EdgeNodeGeometry to calculate from</param>
//        /// <returns></returns>
//        //internal static Bounds3 UpdateBounds3(Game.Net.EdgeNodeGeometry input)
//        //{
//        //    Bounds3 leftLeft = MathUtils.Bounds(input.m_Left.m_Left);
//        //    Bounds3 leftRight = MathUtils.Bounds(input.m_Left.m_Right);
//        //    Bounds3 rightLeft = MathUtils.Bounds(input.m_Right.m_Left);
//        //    Bounds3 rightRight = MathUtils.Bounds(input.m_Right.m_Right);
//        //    return leftLeft.Encapsulate(leftRight.Encapsulate(rightLeft.Encapsulate(rightRight)));
//        //}


//        /// <summary>
//        /// Must be called after the m_Start and m_End values are updated!
//        /// </summary>
//        /// <param name="input">The EdgeGeometry to calculate from</param>
//        /// <returns></returns>
//        //internal static Bounds3 UpdateBounds3(Game.Net.EdgeGeometry input)
//        //{
//        //    Bounds3 startLeft = MathUtils.Bounds(input.m_Start.m_Left);
//        //    Bounds3 startRight = MathUtils.Bounds(input.m_Start.m_Right);
//        //    Bounds3 endLeft = MathUtils.Bounds(input.m_End.m_Left);
//        //    Bounds3 endRight = MathUtils.Bounds(input.m_End.m_Right);
//        //    return startLeft.Encapsulate(startRight.Encapsulate(endLeft.Encapsulate(endRight)));
//        //}
//    }
//}
