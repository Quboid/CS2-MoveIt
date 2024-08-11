using Game.Rendering;
using MoveIt.Tool;
using QCommonLib;
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MoveIt.Overlays
{
    internal partial class MIT_OverlaySystem : Systems.MIT_System
    {
        private OverlayRenderSystem _OverlayRenderSystem;
        private EntityQuery _DrawQuery;
        private EntityQuery _TTLTickQuery;
        private EntityQuery _CleanupQuery;

        /// <summary>
        /// Freeze overlays for closer inspection
        /// For the first frame after freezing, stay thawed until just before cleanup
        /// </summary>
        internal bool DebugFreeze
        {
            get => _DebugFreeze && !_DebugFirstFrozen;
            set
            {
                _DebugFreeze = value;
                if (value)
                {
                    _DebugFirstFrozen = true;
                }
            }
        }
        private bool _DebugFreeze = false;
        private bool _DebugFirstFrozen = false;

        protected override void OnCreate()
        {
            base.OnCreate();

            _OverlayRenderSystem = World.GetOrCreateSystemManaged<OverlayRenderSystem>();
            //_DebugOverlays = new();

            ColorData.Init();

            _DrawQuery = SystemAPI.QueryBuilder()
                .WithAll<MIO_Type>()
                .WithNone<Game.Common.Deleted>()
                .Build();

            _TTLTickQuery = SystemAPI.QueryBuilder()
                .WithAll<MIO_Type, MIO_TTL>()
                .WithNone<Game.Common.Deleted>()
                .Build();

            _CleanupQuery = SystemAPI.QueryBuilder()
                .WithAll<MIO_Type, MIO_SingleFrame>()
                .WithNone<Game.Common.Deleted>()
                .Build();

            RequireForUpdate(_DrawQuery);
        }

        internal override void Start()
        {
            base.Start();
        }

        internal override void End()
        {
            base.End();
        }

        protected override void OnDestroy()
        {
            ColorData.Dispose();
            Colors.Dispose();
        }

        protected override void OnUpdate()
        {
            if (_MIT.IsLowSensitivity) return;

            //string msg = $"\nOverlays:{_DrawQuery.CalculateEntityCount()}";
            //var all = _DrawQuery.ToEntityArray(Allocator.Temp);
            //foreach (var olay in all)
            //{
            //    var t = _MIT.EntityManager.GetComponentData<MIO_Type>(olay).m_Type;
            //    msg += $"\n    [{olay.D()}-{t}]";
            //}
            //msg += $"\n{_MIT.Moveables.DebugFull()}";
            //QLog.Bundle("OVRL", msg);
            //MIT.Log.Debug($"Draw:{_DrawQuery.CalculateEntityCount()}, Cleanup:{_CleanupQuery.CalculateEntityCount()}, TTLTick:{_TTLTickQuery.CalculateEntityCount()}, Freeze:{DebugFreeze}({_DebugFreeze},{_DebugFirstFrozen})");

            ToolFlags toolFlags = ToolFlags.None;
            if (_MIT.IsManipulating)                            toolFlags |= ToolFlags.ManipulationMode;
            if (QKeyboard.Shift)                                toolFlags |= ToolFlags.HasShift;
            if (_MIT.MITState == MITStates.DrawingSelection)    toolFlags |= ToolFlags.IsMarquee;
            if (Mod.Settings.ShowDebugLines)                    toolFlags |= ToolFlags.ShowDebug;

            #region old overlays
            //    // Action overlays
            //    Add(ref overlays, Actions.Queue.Current.GetOverlays(toolFlags));
            #endregion

            JobHandle updateOverlaysHandle = Dependency;
            try
            {
                UpdateOverlaysJob updateOverlays = new()
                {
                    m_ToolFlags                 = toolFlags,
                    m_IsManipMode               = _MIT.m_IsManipulateMode,
                    m_CameraPosition            = (float3)Camera.main.transform.position,
                    cth_Overlay                 = GetComponentTypeHandle<MIO_Type>(),
                    cth_CommonData              = GetComponentTypeHandle<MIO_Common>(),
                    cth_Circle                  = GetComponentTypeHandle<MIO_Circle>(),
                    cth_SelectionData           = GetComponentTypeHandle<MIO_SelectionData>(),
                    cth_Quad                    = GetComponentTypeHandle<MIO_Quad>(),
                    bth_Circles                 = GetBufferTypeHandle<MIO_Circles>(),
                    bth_Lines                   = GetBufferTypeHandle<MIO_Lines>(),
                    bth_DashedLines             = GetBufferTypeHandle<MIO_DashedLines>(),

                    blu_AreasNode               = GetBufferLookup<Game.Areas.Node>(true),
                    clu_BuildingData            = GetComponentLookup<Game.Prefabs.BuildingData>(true),
                    clu_BuildingExtensionData   = GetComponentLookup<Game.Prefabs.BuildingExtensionData>(true),
                    clu_ObjectGeometryData      = GetComponentLookup<Game.Prefabs.ObjectGeometryData>(true),
                    clu_PrefabRefs              = GetComponentLookup<Game.Prefabs.PrefabRef>(true),
                };
                updateOverlaysHandle = updateOverlays.ScheduleParallelByRef(_DrawQuery, updateOverlaysHandle);
                updateOverlaysHandle.Complete();
            }
            catch (Exception ex)
            {
                MIT.Log.Error($"Failed on UpdateOverlaysJob:\n{ex}");
            }

            try
            {
                OverlayRenderSystem.Buffer buffer = _OverlayRenderSystem.GetBuffer(out JobHandle overlayRenderBufferHandle);

                DrawOverlaysJob drawOverlays = new()
                {
                    m_OverlayRenderBuffer   = buffer,
                    m_ToolFlags             = toolFlags,
                    m_IsManipMode           = _MIT.m_IsManipulateMode,
                    m_CameraPosition        = (float3)Camera.main.transform.position,
                    cth_Overlay             = GetComponentTypeHandle<MIO_Type>(true),
                    cth_CommonData          = GetComponentTypeHandle<MIO_Common>(true),
                    cth_Bezier              = GetComponentTypeHandle<MIO_Bezier>(true),
                    cth_Bounds              = GetComponentTypeHandle<MIO_Bounds>(true),
                    cth_Circle              = GetComponentTypeHandle<MIO_Circle>(true),
                    cth_Line                = GetComponentTypeHandle<MIO_Line>(true),
                    cth_Quad                = GetComponentTypeHandle<MIO_Quad>(true),
                    cth_Debug               = GetComponentTypeHandle<MIO_Debug>(true),
                    bth_Beziers             = GetBufferTypeHandle<MIO_Beziers>(true),
                    bth_Circles             = GetBufferTypeHandle<MIO_Circles>(true),
                    bth_DashedLines         = GetBufferTypeHandle<MIO_DashedLines>(true),
                    bth_Lines               = GetBufferTypeHandle<MIO_Lines>(true),
                };
                JobHandle drawMoveableHandle = drawOverlays.ScheduleByRef(_DrawQuery, JobHandle.CombineDependencies(updateOverlaysHandle, overlayRenderBufferHandle));

                _OverlayRenderSystem.AddBufferWriter(drawMoveableHandle);
                drawMoveableHandle.Complete();
                Dependency = drawMoveableHandle;
            }
            catch (Exception ex)
            {
                MIT.Log.Error($"Failed on DrawOverlaysJob:\n{ex}");
            }


            // If DebugFreeze is on, end now so they aren't cleaned up
            if (_DebugFreeze)
            {
                _DebugFirstFrozen = false;
                return;
            }

            EntityManager.AddComponent<Game.Common.Deleted>(_CleanupQuery);

            NativeArray<Entity> ttlEntities = _TTLTickQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < ttlEntities.Length; i++)
            {
                Entity e = ttlEntities[i];
                MIO_TTL ttl = EntityManager.GetComponentData<MIO_TTL>(e);
                ttl.m_TTL--;
                if (ttl.m_TTL > 0)
                {
                    EntityManager.SetComponentData(e, ttl);
                }
                else
                {
                    EntityManager.RemoveComponent<MIO_TTL>(e);
                    EntityManager.AddComponent<MIO_SingleFrame>(e);
                }
            }
        }

        internal void DestroyAllEntities()
        {
            EntityManager.DestroyEntity(_DrawQuery);
        }

        internal string DebugDrawQuery()
        {
            _DrawQuery.CompleteDependency();
            string msg = $"Overlays:{_DrawQuery.CalculateEntityCount()}\n";
            var all = _DrawQuery.ToEntityArray(Allocator.Temp);
            foreach (Entity olay in all)
            {
                var t = _MIT.EntityManager.GetComponentData<MIO_Type>(olay).m_Type;
                msg += $"  [{olay.D()}-{t}]";
            }
            return msg + $"\n{_MIT.Moveables.DebugFull()}";
        }
    }
}
