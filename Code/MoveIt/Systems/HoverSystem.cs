//using Game.Common;
//using Game.Net;
//using MoveIt.Tool;
//using QCommonLib;
//using Unity.Collections;
//using Unity.Entities;
//using Unity.Jobs;
//using Unity.Mathematics;

//namespace MoveIt.Systems
//{
//    [UpdateBefore(typeof(MIT))]
//    internal partial class MIT_HoverSystem : MIT_ToolSystem
//    {
//        private EntityCommandBuffer _ECB;
//        private EntityQuery _HoverQuery;
//        private float3 _PointerPos;
//        private RaycastSystem _RaycastSystem;

//        private RaycastTerrain RaycastTerrain => _Tool.m_RaycastTerrain;

//        protected override void OnCreate()
//        {
//            base.OnCreate();

//            _RaycastSystem = World.GetOrCreateSystemManaged<RaycastSystem>();

//            _HoverQuery = SystemAPI.QueryBuilder()
//                .WithAll<MIT_Hover>()
//                .Build();
//        }

//        protected override void OnDestroy()
//        {
//            base.OnDestroy();
//        }


//        protected override JobHandle OnUpdate(JobHandle inputDeps)
//        {
//            _ECB = new EntityCommandBuffer();

//            _PointerPos = RaycastTerrain.HitPosition;

//            Entity actual = GetHit();
//            Entity saved = Get();

//            if (!actual.Equals(Entity.Null)) QLog.Debug($"HoverSys hit:{actual.DX()}");

//            if (!saved.Equals(actual))
//            {
//                Unset();
//                Set(actual);
//            }

//            _ECB.Playback(EntityManager);
//            _ECB.Dispose();
//            return inputDeps;
//        }

//        private (Entity e, float d)[] GetRaycastResults()
//        {
//            NativeArray<RaycastResult> vanillaRaycastResults = _RaycastSystem.GetResult(m_ToolRaycastSystem);
//            if (vanillaRaycastResults.Length > 0) QLog.Debug($"HoverSys vanRes:{vanillaRaycastResults.Length}  {vanillaRaycastResults[0].m_Owner.DX()}");
//            Manipulate f = Manipulate.Parent | (_Tool.m_IsManipulateMode ? Manipulate.Child : Manipulate.Normal);
//            Searcher.Ray searcher = new(Searcher.Filters.All, vanillaRaycastResults, f);
//            vanillaRaycastResults.Dispose();
//            return searcher.OnLine(RaycastTerrain.Line, _PointerPos);
//        }

//        private Entity GetHit()
//        {
//            (Entity e, float d)[] RaycastResults = GetRaycastResults();
//            if (RaycastResults.Length > 0) return RaycastResults[0].e;
//            return Entity.Null;
//        }

//        public Entity Get()
//        {
//            if (_HoverQuery.IsEmpty) return Entity.Null;
//            if (_HoverQuery.CalculateEntityCount() > 0) throw new System.Exception($"MIT_HoverSystem.Get: {_HoverQuery.CalculateEntityCount()} entities selected!");

//            using var list = _HoverQuery.ToEntityArray(Allocator.Temp);
//            QLog.Debug($"HoverSys.Get {list[0].DX()}");
//            return list[0];
//        }

//        private void Unset()
//        {
//            if (_HoverQuery.IsEmpty) return;
//            QLog.Debug($"HoverSys.Unset");
//            _ECB.RemoveComponent<MIT_Hover>(_HoverQuery, EntityQueryCaptureMode.AtRecord);
//        }

//        private void Set(Entity e)
//        {
//            if (e.Equals(Entity.Null)) return;
//            if (!_HoverQuery.IsEmpty) throw new System.Exception($"MIT_HoverSystem.Set: {_HoverQuery.CalculateEntityCount()} entities selected!");
//            QLog.Debug($"HoverSys.Set {e.DX()}");
//            _ECB.AddComponent<MIT_Hover>(e);
//        }


//        //public override void InitializeRaycast()
//        //{
//        //    base.InitializeRaycast();

//        //    m_ToolRaycastSystem.collisionMask = (CollisionMask.OnGround | CollisionMask.Overground | CollisionMask.ExclusiveGround);
//        //    m_ToolRaycastSystem.typeMask = (TypeMask.StaticObjects | TypeMask.Net | TypeMask.Areas | TypeMask.Terrain);// | TypeMask.MovingObjects);
//        //    m_ToolRaycastSystem.raycastFlags = (RaycastFlags)0U;// (RaycastFlags.SubElements | RaycastFlags.Decals | RaycastFlags.Placeholders | RaycastFlags.UpgradeIsMain | RaycastFlags.Outside | RaycastFlags.Cargo | RaycastFlags.Passenger);
//        //    m_ToolRaycastSystem.netLayerMask = (Layer.Road | Layer.Fence | Layer.TrainTrack | Layer.TramTrack | Layer.SubwayTrack | Layer.Pathway);
//        //    m_ToolRaycastSystem.iconLayerMask = Game.Notifications.IconLayerMask.None;

//        //    QLog.Debug($"Hover.InitRay1 {(_RaycastTerrain is null ? "Null" : _RaycastTerrain)}");
//        //    _RaycastTerrain = new RaycastTerrain(World);
//        //    QLog.Debug($"Hover.InitRay2 {(_RaycastTerrain is null ? "Null" : _RaycastTerrain)}");
//        //}

//        public override string toolID => "MIT_Hover";
//        public override Game.Prefabs.PrefabBase GetPrefab() => null;
//        public override bool TrySetPrefab(Game.Prefabs.PrefabBase prefab) => false;
//    }

//    internal struct MIT_Hover : IComponentData
//    { }
//}
