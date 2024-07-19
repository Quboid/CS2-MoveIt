using Colossal.Entities;
using Colossal.Mathematics;
using Game.Tools;
using MoveIt.Actions;
using MoveIt.Moveables;
using QCommonLib;
using System;
using System.Reflection;
using Unity.Entities;
using Unity.Mathematics;

namespace MoveIt.Tool
{
    public partial class MIT : ObjectToolBaseSystem
    {
        //private TransformAction m_CleanUpAction;

        internal void ManageCreation(Actions.Action action)
        {
            if (CreationPhase == CreationPhases.None)
            {
                if (m_TempQuery.IsEmpty) BaseApplyMode = ApplyMode.None;
                return;
            }

            //MIT.Log.Debug($"{UnityEngine.Time.frameCount} ManageCreation ({CreationMode}) <{action.Name}>");

            if (CreationPhase == CreationPhases.Cleanup)
            {
                Cleanup();
                return;
            }

            if (CreationPhase == CreationPhases.Create)
            {
                Create(action);
                return;
            }

            if (!Selection.Any)
            {
                return;
            }

            if (CreationPhase == CreationPhases.Positioning && (action.m_UpdateMove || action.m_UpdateRotate))
            {
                Transform(action);
                return;
            }

            return;
        }

        private void Cleanup()
        {
            UpdateTerrain(Queue.CreationAction.m_TerrainUpdateBounds);

            if (Queue.CreationAction is TransformAction ta)
            {
                int oldC = UpdateNearbyBuildingConnections(EntityManager, ta, ta.m_InitialBounds);
                int newC = UpdateNearbyBuildingConnections(EntityManager, ta, ta.m_FinalBounds);
                //Log.Debug($"UpdateNearbyBldgConns {ta.Name} old:{oldC}, new:{newC}" +
                    //$"\n  old:{oldC} : {m_CleanUpAction.m_InitialBounds.min.DX()} : {m_CleanUpAction.m_InitialBounds.max.DX()}" +
                    //$"\n  new:{newC} : {m_CleanUpAction.m_FinalBounds.min.DX()} : {m_CleanUpAction.m_FinalBounds.max.DX()}" +
                    //"");
                //m_CleanUpAction = null;
            }

            CreationPhase = CreationPhases.None;
            BaseApplyMode = ApplyMode.None;
            Queue.CreationAction = null;
        }

        private void Transform(Actions.Action action)
        {
            if (action is not TransformAction ta)
            {
                Log.Error($"Error: action is {action.Name} during CreationMods.Create");
                CreationPhase = CreationPhases.Cleanup;
                return;
            }

            UpdateTerrain();

            ta.Transform();
        }

        internal void Create(Actions.Action action)
        {
            try
            {
                Transform(action);
                if (action.m_FinalBounds.Equals(default))
                {
                    action.m_FinalBounds = Selection.GetTotalBounds(TERRAIN_UPDATE_MARGIN);
                }

                //m_CleanUpAction = action is TransformAction ta ? ta : null;
            }
            finally
            {
                CreationPhase = CreationPhases.Cleanup;
            }

            #region Vanilla Relocate
            //MIT.Log.Debug($"{UnityEngine.Time.frameCount} OK to apply for {stateData.Length} objects");
            //m_Lookups.Update(this);
            //for (int i = 0; i < stateData.Length; i++)
            //{
            //    //MIT.Log.Debug($"{UnityEngine.Time.frameCount}  Creating {jobData[i]}");
            //    Entity e = stateData[i].m_Entity;
            //    CreateDefinitionsJob(stateData[i], true);
            //    ToggleHidden(e, false);
            //    if (!EntityManager.Exists(e))
            //    {
            //        throw new Exception($"Entity {e.D()} not found");
            //    }
            //    EntityManager.AddComponent<Game.Common.Updated>(e);
            //    EntityManager.AddComponent<Game.Common.BatchesUpdated>(e);
            //}

            //BaseApplyMode = ApplyMode.Apply;
            //CreationMode = CreationModes.Cleanup;
            #endregion
        }


        /// <summary>
        /// Update buildings and networks in or near the passed location
        /// </summary>
        /// <param name="manager">an EntityManager</param>
        /// <param name="ta">The TransformAction to update for</param>
        /// <param name="bounds">The outer bounds of the rectangle</param>
        /// <returns>The number of search results found</returns>
        internal int UpdateNearbyBuildingConnections(EntityManager manager, TransformAction ta, Bounds3 bounds)
        {
            bool isRelevant = false;
            foreach (State state in ta.m_Active.m_States)
            {
                if (
                    state.m_Identity == Identity.Building || state.m_Identity == Identity.ServiceUpgrade || state.m_Identity == Identity.Extension ||
                    state.m_Identity == Identity.Node || state.m_Identity == Identity.Segment
                    )
                {
                    isRelevant = true;
                    break;
                }
            }
            if (!isRelevant) return -1;

            //StringBuilder sb = new("UpdateNearbyBuildingConnections");
            Bounds2 outerBounds = new(bounds.min.XZ(), bounds.max.XZ());
            using Searcher.Searcher searcher = new(Searcher.Utils.FilterAllNetworks | Searcher.Filters.Buildings, false, m_PointerPos);
            searcher.SearchBounds(outerBounds);
            //searcher.DebugDumpSearchResults();

            Overlays.DebugBounds.Factory(outerBounds, Overlays.Overlay.DEBUG_TTL, new UnityEngine.Color(0.9f, 0.2f, 0f, 0.6f));

            //sb.AppendFormat("\nStates: {0}", ta.m_Active.m_States.Length);
            foreach (State state in ta.m_Active.m_States)
            {
                if (
                    state.m_Identity == Identity.Building || state.m_Identity == Identity.ServiceUpgrade || state.m_Identity == Identity.Extension ||
                    state.m_Identity == Identity.Node || state.m_Identity == Identity.Segment
                    )
                {
                    state.m_Accessor.UpdateAll();
                    //sb.AppendFormat("\n    {0} (updated: {1})", state.m_Entity.DX(true), c);
                }
            }

            //sb.AppendFormat("\nResults: {0}", searcher.m_Results.Length);
            foreach (Searcher.Result result in searcher.m_Results)
            {
                Entity e = result.m_Entity;
                QAccessor.QObject accessor = new(manager, ref QAccessor.QLookupFactory.Get(), e);
                accessor.UpdateAll();

                if (!Mod.Settings.ShowDebugLines) continue;

                //sb.AppendFormat("\n    {0} - (updated: {1})", e.DX(true), c);

                // Segment
                if (EntityManager.TryGetComponent(e, out Game.Net.Edge edge))
                {
                    float3 posA = EntityManager.GetComponentData<Game.Net.Node>(edge.m_Start).m_Position;
                    float3 posB = EntityManager.GetComponentData<Game.Net.Node>(edge.m_End).m_Position;
                    Overlays.DebugLine.Factory(new(posA, posB), Overlays.Overlay.DEBUG_TTL, new(1f, 0.5f, 0f, 0.8f));

                    if (!searcher.Has(edge.m_Start))
                    {
                        Overlays.DebugCircle.Factory(posA, 8, Overlays.Overlay.DEBUG_TTL, new(1f, 0.4f, 0.3f, 0.4f));
                    }
                    if (!searcher.Has(edge.m_End))
                    {
                        Overlays.DebugCircle.Factory(posB, 8, Overlays.Overlay.DEBUG_TTL, new(1f, 0.4f, 0.3f, 0.4f));
                    }
                }

                // Node
                else if (EntityManager.TryGetComponent(e, out Game.Net.Node node))
                {
                    Overlays.DebugCircle.Factory(node.m_Position, 8, Overlays.Overlay.DEBUG_TTL, new(1f, 0.5f, 0.0f, 0.8f));
                }

                // Building
                else if (EntityManager.TryGetComponent(e, out Game.Objects.Transform tform))
                {
                    if (EntityManager.TryGetComponent(e, out Game.Buildings.Building _))
                    {
                        var prefab = EntityManager.GetComponentData<Game.Prefabs.PrefabRef>(e).m_Prefab;
                        Quad2 corners = Searcher.Utils.CalculateBuildingCorners(EntityManager, ref accessor, prefab, -0.5f);
                        Overlays.DebugQuad.Factory(corners, (int)(Overlays.Overlay.DEBUG_TTL * 1.5), new(0.0f, 0.6f, 0.9f, 0.8f));
                    }
                    else
                    {
                        Overlays.DebugCircle.Factory(tform.m_Position, 8f, (int)(Overlays.Overlay.DEBUG_TTL * 1.5f), new(0.0f, 0.8f, 0.8f, 0.7f));
                    }
                }
            }

            //QLog.Debug(sb.ToString());
            return searcher.Count;
        }

        private void UpdateTerrain(Bounds3 area = default)
        {
            Bounds3 bounds = Selection.GetTotalBounds();
            if (area.Equals(default))
            {
                area.min = bounds.min - TERRAIN_UPDATE_MARGIN;
                area.max = bounds.max + TERRAIN_UPDATE_MARGIN;
            }
            else
            {
                area.min = math.min(area.min, bounds.min - TERRAIN_UPDATE_MARGIN);
                area.max = math.max(area.max, bounds.max + TERRAIN_UPDATE_MARGIN);
            }
            Queue.Current.m_TerrainUpdateBounds = area;

            //Overlays.DebugBounds.Factory(area, Overlays.Overlay.DEBUG_TTL, new(0.1f, 0.1f, 0.8f, 0.6f));
            
            SetUpdateAreaField(area);
        }

        private void SetUpdateAreaField(Bounds3 bounds)
        {
            if (bounds.min.x == bounds.max.x || bounds.min.z == bounds.max.z) return;
            float4 area = new(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z);
            FieldInfo field = m_TerrainSystem.GetType().GetField("m_UpdateArea", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new Exception("Failed to find TerrainSystem.m_UpdateArea");
            field.SetValue(m_TerrainSystem, area);
        }

        //private void Positioning(Actions.Action action)
        //{
        //    if (action is not TransformAction ta)
        //    {
        //        Log.Error($"Error: action is {action.Name} during CreationMods.Positioning");
        //        CreationMode = CreationModes.Cleanup;
        //        return;
        //    }

        //    ta.Transform();
            
        //    //m_Lookups.Update(this);
        //    //BaseApplyMode = ApplyMode.Clear;
        //    //m_InputDeps = DestroyDefinitions(GetDefinitionQuery(), m_ToolOutputBarrier, m_InputDeps);

        //    //ref QNativeArray<Moveables.State> stateData = ref ta.GetStateDataForJobs();
        //    //for (int i = 0; i < stateData.Length; i++)
        //    //{
        //    //    CreateDefinitionsJob(stateData[i], true);
        //    //}
        //}

        //internal void CreateDefinitionsJob(Moveables.State stateData, bool isRelocate)
        //{
        //    //if (CreationMode == CreationModes.Create) Log.Debug($"{UnityEngine.Time.frameCount} Enqueuing {stateData}");

        //    float3 position = stateData.m_Position;
        //    position.y = GetTerrainHeight(position) + stateData.m_YOffset;

        //    DefinitionsData data = new()
        //    {
        //        original = isRelocate ? stateData.m_Entity : Entity.Null,
        //        objectPrefab = GetPrefabInfo(stateData.m_Entity).m_Entity,

        //        controlPoints = new(1, Allocator.TempJob)
        //        {
        //            [0] = new()
        //            {
        //                m_OriginalEntity = Entity.Null,
        //                m_Position = position,
        //                m_Rotation = Quaternion.Euler(0f, stateData.m_Angle, 0f),
        //                m_ElementIndex = new(-1, -1),
        //                m_Elevation = 0f,
        //            },
        //        },
        //    };

        //    m_InputDeps = CreateDefinitions(
        //        data.objectPrefab,
        //        data.transformPrefab,
        //        data.brushPrefab,
        //        data.owner,
        //        data.original,
        //        data.laneEditor,
        //        data.theme,
        //        data.controlPoints,
        //        data.attachmentPrefab,
        //        data.editorMode,
        //        data.lefthandTraffic,
        //        data.removing,
        //        data.stamping,
        //        data.brushSize,
        //        data.brushAngle,
        //        data.brushStrength,
        //        data.deltaTime,
        //        data.randomSeed,
        //        data.snap,
        //        m_InputDeps
        //    );

        //    data.Dispose(m_InputDeps);
        //}

        //private struct DefinitionsData
        //{
        //    /// <summary>
        //    /// Makes default container for the CreateDefinitions call. Must set controlPoints immediately.
        //    /// </summary>
        //    /// <param name="toolSystem">Any ToolSystem should do, it's just to check whether we're in the editor</param>
        //    public DefinitionsData(ToolSystem toolSystem)
        //    {
        //        objectPrefab = Entity.Null;
        //        transformPrefab = Entity.Null;
        //        brushPrefab = Entity.Null;
        //        owner = Entity.Null;
        //        original = Entity.Null;
        //        laneEditor = Entity.Null;
        //        theme = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.City.CityConfigurationSystem>().defaultTheme;
        //        controlPoints = new();
        //        attachmentPrefab = new NativeReference<AttachmentData>(Allocator.TempJob, NativeArrayOptions.ClearMemory);
        //        editorMode = toolSystem.actionMode.IsEditor();
        //        lefthandTraffic = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.City.CityConfigurationSystem>().leftHandTraffic;
        //        removing = false;
        //        stamping = false;
        //        brushSize = 0f;
        //        brushAngle = 0f;
        //        brushStrength = 0f;
        //        deltaTime = UnityEngine.Time.deltaTime;
        //        randomSeed = Game.Common.RandomSeed.Next();
        //        snap = Snap.None;
        //    }

        //    public void Dispose(JobHandle dependency)
        //    {
        //        attachmentPrefab.Dispose(dependency);
        //        controlPoints.Dispose(dependency);
        //    }

        //    /// <summary>
        //    /// The building/tree/etc prefab
        //    /// </summary>
        //    public Entity objectPrefab;

        //    /// <summary>
        //    /// Unknown
        //    /// null
        //    /// </summary>
        //    public Entity transformPrefab;

        //    /// <summary>
        //    /// Placement brush, e.g. for trees. Not relevent.
        //    /// null
        //    /// </summary>
        //    public Entity brushPrefab;

        //    /// <summary>
        //    /// The entity being upgraded (growable level, not the player adding extensions)? Or maybe subobjects' owner? Both, depending on context?
        //    /// </summary>
        //    public Entity owner;

        //    /// <summary>
        //    /// The entity that is being relocated
        //    /// Entity.Null to create new
        //    /// </summary>
        //    public Entity original;

        //    /// <summary>
        //    /// Unknown, not relevent
        //    /// null
        //    /// </summary>
        //    public Entity laneEditor;

        //    /// <summary>
        //    /// Map theme
        //    /// World.GetOrCreateSystemManaged<CityConfigurationSystem>().defaultTheme
        //    /// </summary>
        //    public Entity theme;

        //    /// <summary>
        //    /// Struct of various data:
        //    ///     m_OriginalEntity = Entity.Null - setting an Entity here is related to attaching extensions
        //    ///     m_Position = m_RaycastTerrain.HitPosition - The location for the object to be placed
        //    ///     m_Rotation = transform.m_Rotation - the angle of the object to be placed
        //    ///     m_ElementIndex = new (-1, -1) - unknown, probably irrelevant
        //    ///     m_Elevation = 0f - unknown, probably offset from m_Position's Y axis
        //    /// ControlPoint object
        //    /// </summary>
        //    public NativeList<ControlPoint> controlPoints;

        //    /// <summary>
        //    /// Unknown
        //    /// new NativeReference<ObjectToolBaseSystem.AttachmentData>(Allocator.TempJob, NativeArrayOptions.ClearMemory)
        //    /// </summary>
        //    public NativeReference<AttachmentData> attachmentPrefab;

        //    /// <summary>
        //    /// Are we in the editor?
        //    /// m_ToolSystem.actionMode.IsEditor()
        //    /// </summary>
        //    public bool editorMode;

        //    /// <summary>
        //    /// Traffic side
        //    /// World.GetOrCreateSystemManaged<CityConfigurationSystem>().leftHandTraffic
        //    /// </summary>
        //    public bool lefthandTraffic;

        //    /// <summary>
        //    /// Is this loss?
        //    /// </summary>
        //    public bool removing;

        //    /// <summary>
        //    /// Is this a prop stamp (template of props)?
        //    /// </summary>
        //    public bool stamping;

        //    /// <summary>
        //    /// Placement brush setting, not relevent
        //    /// 0f
        //    /// </summary>
        //    public float brushSize;

        //    /// <summary>
        //    /// Placement brush setting, not relevent
        //    /// 0f
        //    /// </summary>
        //    public float brushAngle;

        //    /// <summary>
        //    /// Placement brush setting, not relevent
        //    /// 0f
        //    /// </summary>
        //    public float brushStrength;

        //    /// <summary>
        //    /// Frame time
        //    /// UnityEngine.Time.deltaTime
        //    /// </summary>
        //    public float deltaTime;

        //    /// <summary>
        //    /// Random number
        //    /// RandomSeed.Next()
        //    /// </summary>
        //    public Game.Common.RandomSeed randomSeed;

        //    /// <summary>
        //    /// Flags for object snapping
        //    /// Snap.None
        //    /// </summary>
        //    public Snap snap;
        //}
    }
}
