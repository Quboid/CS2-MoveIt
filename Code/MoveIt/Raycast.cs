﻿using Colossal.Mathematics;
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

    internal abstract class RaycastBase
    {
        private readonly RaycastSystem _RaycastSystem;
        private readonly World _World;

        internal float3 HitPosition => GetHit().m_HitPosition;

        internal Line3.Segment Line => ToolRaycastSystem.CalculateRaycastLine(Camera.main);

        protected abstract RaycastInput GetInput();

        public RaycastBase(World gameWorld)
        {
            _World = gameWorld;
            _RaycastSystem = _World.GetOrCreateSystemManaged<RaycastSystem>();

            RaycastInput input = GetInput();

            _RaycastSystem.AddInput(this, input);
        }

        public RaycastHit GetHit()
        {
            NativeArray<RaycastResult> result = _RaycastSystem.GetResult(this);
            if (result == null || result.Length == 0)
            {
                throw new System.Exception($"Failed to get raycast result");
            }
            RaycastHit hit = result[0].m_Hit;
            result.Dispose();

            return hit;
        }
    }
}