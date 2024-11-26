using Colossal.Mathematics;
using Game.Common;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace MoveIt
{
    internal class RaycastTerrain : RaycastBase
    {
        internal RaycastTerrain(World gameWorld) : base(gameWorld)
        { }

        protected override RaycastInput GetInput()
        {
            RaycastInput result = default;
            result.m_Line = Line;
            result.m_Offset = default;
            result.m_TypeMask = TypeMask.Terrain;
            return result;
        }
    }

    internal class RaycastSurface : RaycastBase
    {
        internal RaycastSurface(World gameWorld) : base(gameWorld)
        { }

        protected override RaycastInput GetInput()
        {
            RaycastInput result = default;
            result.m_Line = Line;
            result.m_Offset = default;
            result.m_TypeMask = TypeMask.Areas;
            result.m_AreaTypeMask = Game.Areas.AreaTypeMask.Surfaces | Game.Areas.AreaTypeMask.Spaces | Game.Areas.AreaTypeMask.Lots;
            return result;
        }
    }


    internal abstract class RaycastBase
    {
        private readonly RaycastSystem _RaycastSystem;

        // Will be float.MaxValue,float.MaxValue,float.MaxValue when tool is activated
        internal float3 HitPosition => GetHit().m_HitPosition;

        internal static Line3.Segment Line => ToolRaycastSystem.CalculateRaycastLine(Camera.main);

        protected abstract RaycastInput GetInput();

        protected RaycastBase(World gameWorld)
        {
            _RaycastSystem = gameWorld.GetOrCreateSystemManaged<RaycastSystem>();

            RaycastInput input = GetInputFromAbstract();

            _RaycastSystem.AddInput(this, input);
        }
        
        private RaycastInput GetInputFromAbstract()
            => GetInput();

        public NativeArray<RaycastResult> GetResults()
        {
            return _RaycastSystem.GetResult(this);
        }

        private RaycastHit GetHit()
        {
            NativeArray<RaycastResult> result = GetResults();
            if (!result.IsCreated || result.Length == 0)
            {
                RaycastHit res = new()
                {
                    m_HitPosition = new(float.MaxValue, float.MaxValue, float.MaxValue)
                };
                return res;
                //throw new System.Exception($"Failed to get raycast result");
            }
            RaycastHit hit = result[0].m_Hit;
            result.Dispose();

            return hit;
        }
    }
}
