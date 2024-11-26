using Colossal.Entities;
using Colossal.Mathematics;
using Game.Tools;
using MoveIt.Actions.Transform;
using MoveIt.Moveables;
using System;
using System.Reflection;
using Unity.Entities;
using Unity.Mathematics;

namespace MoveIt.Tool
{
    public partial class MIT : ObjectToolBaseSystem
    {
        //private TransformAction m_CleanUpAction;

        //internal void ManageCreation(Actions.Action action)
        //{
        //    if (CreationPhase == CreationPhases.None)
        //    {
        //        if (m_TempQuery.IsEmpty) BaseApplyMode = ApplyMode.None;
        //        return;
        //    }

        //    //MIT.Log.Debug($"{UnityEngine.Time.frameCount} ManageCreation ({CreationMode}) <{action.Name}>");

        //    if (CreationPhase == CreationPhases.Cleanup)
        //    {
        //        Cleanup();
        //        return;
        //    }

        //    if (CreationPhase == CreationPhases.Create)
        //    {
        //        Create(action);
        //        return;
        //    }

        //    if (!Selection.Any)
        //    {
        //        return;
        //    }

        //    if (CreationPhase == CreationPhases.Positioning && (action.m_UpdateMove || action.m_UpdateRotate))
        //    {
        //        Transform(action);
        //        return;
        //    }

        //    return;
        //}

        //private void Cleanup()
        //{
        //    UpdateTerrain(Queue.CreationAction.m_TerrainUpdateBounds);

        //    if (Queue.CreationAction is TransformBase ta)
        //    {
        //        int oldC = UpdateNearbyBuildingConnections(EntityManager, ta, ta.m_InitialBounds);
        //        int newC = UpdateNearbyBuildingConnections(EntityManager, ta, ta.m_FinalBounds);
        //        //Log.Debug($"UpdateNearbyBldgConns {ta.Name} old:{oldC}, new:{newC}" +
        //            //$"\n  old:{oldC} : {m_CleanUpAction.m_InitialBounds.min.DX()} : {m_CleanUpAction.m_InitialBounds.max.DX()}" +
        //            //$"\n  new:{newC} : {m_CleanUpAction.m_FinalBounds.min.DX()} : {m_CleanUpAction.m_FinalBounds.max.DX()}" +
        //            //"");
        //        //m_CleanUpAction = null;
        //    }

        //    CreationPhase = CreationPhases.None;
        //    BaseApplyMode = ApplyMode.None;
        //    Queue.CreationAction = null;
        //}

        //private void Transform(Actions.Action action)
        //{
        //    if (action is not TransformBase ta)
        //    {
        //        Log.Error($"Error: action is {action.Name} during CreationMods.Create");
        //        CreationPhase = CreationPhases.Cleanup;
        //        return;
        //    }

        //    UpdateTerrain();

        //    ta.Transform();
        //}

        //internal void Create(Actions.Action action)
        //{
        //    try
        //    {
        //        Transform(action);
        //        if (action.m_FinalBounds.Equals(default))
        //        {
        //            action.m_FinalBounds = Selection.GetTotalBounds(TERRAIN_UPDATE_MARGIN);
        //        }

        //        //m_CleanUpAction = action is TransformAction ta ? ta : null;
        //    }
        //    finally
        //    {
        //        CreationPhase = CreationPhases.Cleanup;
        //    }

        //    #region Vanilla Relocate
        //    //MIT.Log.Debug($"{UnityEngine.Time.frameCount} OK to apply for {stateData.Length} objects");
        //    //m_Lookups.Update(this);
        //    //for (int i = 0; i < stateData.Length; i++)
        //    //{
        //    //    //MIT.Log.Debug($"{UnityEngine.Time.frameCount}  Creating {jobData[i]}");
        //    //    Entity e = stateData[i].m_Entity;
        //    //    CreateDefinitionsJob(stateData[i], true);
        //    //    ToggleHidden(e, false);
        //    //    if (!EntityManager.Exists(e))
        //    //    {
        //    //        throw new Exception($"Entity {e.D()} not found");
        //    //    }
        //    //    EntityManager.AddComponent<Game.Common.Updated>(e);
        //    //    EntityManager.AddComponent<Game.Common.BatchesUpdated>(e);
        //    //}

        //    //BaseApplyMode = ApplyMode.Apply;
        //    //CreationMode = CreationModes.Cleanup;
        //    #endregion
        //}


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
