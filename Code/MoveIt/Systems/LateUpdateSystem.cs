//using Colossal.Entities;
//using Game.Common;
//using Game.Tools;
//using MoveIt.Actions;
//using MoveIt.Tool;
//using QCommonLib;
//using System.Collections.Generic;
//using Unity.Entities;

//namespace MoveIt.Systems
//{
//    [UpdateAfter(typeof(MIT_OverlaySystem))]
//    internal partial class MIT_LateUpdateSystem : MIT_System
//    {
//        //private Dictionary<Entity, bool> _QueuedHideTasks;

//        protected override void OnCreate()
//        {
//            base.OnCreate();
//            //_QueuedHideTasks = new();
//        }

//        protected override void OnUpdate()
//        {
//            //Queue.LateUpdate();
//            //HideTasks();
//        }

//        //internal void EnqueueHideTask(Entity e, bool hide)
//        //{
//        //    if (_QueuedHideTasks.ContainsKey(e))
//        //    {
//        //        MIT.Log.Error($"Error: HideTask for {e.D()} already exists (hide:{_QueuedHideTasks[e]})");
//        //    }
//        //    _QueuedHideTasks.Add(e, hide);
//        //}

//        //private void HideTasks()
//        //{
//        //    if (_QueuedHideTasks.Count > 0)
//        //    {
//        //        foreach ((Entity e, bool hide) in _QueuedHideTasks)
//        //        {
//        //            ToggleHidden(e, hide);
//        //        }
//        //        _QueuedHideTasks.Clear();
//        //    }
//        //}

//        //private void ToggleHidden(Entity entity, bool hide)
//        //{
//        //    //MIT.Log.Debug($"{(hide ? "Hidden" : "Showing")} for {entity.D()}");
//        //    if (hide)
//        //    {
//        //        EntityManager.AddComponent<Hidden>(entity);
//        //    }
//        //    else
//        //    {
//        //        EntityManager.RemoveComponent<Hidden>(entity);
//        //    }
//        //    EntityManager.AddComponent<BatchesUpdated>(entity);

//        //    if (EntityManager.TryGetBuffer<Game.Areas.SubArea>(entity, true, out var buffer))
//        //    {
//        //        MIT.Log.Debug($"{(hide ? "Hiding" : "Showing")} {buffer.Length} subareas");
//        //        for (int i = 0; i < buffer.Length; i++)
//        //        {
//        //            Entity e = buffer[i].m_Area;
//        //            if (EntityManager.TryGetBuffer<Game.Areas.Node>(e, false, out var subarea))
//        //            {
//        //                for (int j = 0; j < subarea.Length; j++)
//        //                {
//        //                    var sub = subarea[j];
//        //                    sub.m_Position += (hide ? 99999f : -99999f);
//        //                    subarea[j] = sub;
//        //                }
//        //            }
//        //            //Game.Areas.Area area = EntityManager.GetComponentData<Game.Areas.Area>(e);
//        //            //area.m_Flags &= ~Game.Areas.AreaFlags.Complete;
//        //            //EntityManager.SetComponentData(e, area);

//        //            //if (hide)
//        //            //{
//        //            //    EntityManager.AddComponent<Hidden>(e);
//        //            //}
//        //            //else
//        //            //{
//        //            //    EntityManager.RemoveComponent<Hidden>(e);
//        //            //}
//        //            EntityManager.AddComponent<Updated>(e);
//        //            //EntityManager.AddComponent<BatchesUpdated>(e);
//        //        }
//        //    }
//        //}
//    }
//}
