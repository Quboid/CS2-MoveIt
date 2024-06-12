using Colossal.Mathematics;
using Game.Common;
using Game.Tools;
using MoveIt.Moveables;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace MoveIt.Tool
{
    public partial class MIT : ObjectToolBaseSystem
    {
        #region General validity checks
        internal bool IsValid(MVDefinition mvd)
        {
            return IsValid(mvd.m_Entity);
        }

        internal bool IsValid(Entity e)
        {
            if (!IsValidBase(e)) return false;
            if (!(
                EntityManager.HasComponent<Game.Objects.Transform>(e) ||
                EntityManager.HasComponent<Game.Net.Edge>(e) ||
                EntityManager.HasComponent<Game.Net.Node>(e) ||
                EntityManager.HasComponent<Components.MIT_ControlPoint>(e)
                )) return false;
            return true;
        }

        internal bool IsValidNetwork(Entity e)
        {
            if (!IsValidBase(e)) return false;
            if (!(
                EntityManager.HasComponent<Game.Net.Edge>(e) ||
                EntityManager.HasComponent<Game.Net.Node>(e) ||
                EntityManager.HasComponent<Components.MIT_ControlPoint>(e)
                )) return false;
            return true;
        }

        internal bool IsValidObject(Entity e)
        {
            if (!IsValidBase(e)) return false;
            if (!EntityManager.HasComponent<Game.Objects.Transform>(e)) return false;
            return true;
        }

        internal bool IsValidBase(Entity e)
        {
            if (e.Equals(Entity.Null)) return false;
            if (!EntityManager.Exists(e)) return false;
            if (!EntityManager.HasComponent<Game.Prefabs.PrefabRef>(e)) return false;
            if (EntityManager.HasComponent<Temp>(e)) return false;
            if (EntityManager.HasComponent<Terrain>(e)) return false;
            if (EntityManager.HasComponent<Owner>(e)) return false;
            if (EntityManager.HasComponent<Game.Objects.Attached>(e)) return false;
            return true;
        }

        /// <summary>
        /// Can this MVDefinition be used for manipulation?
        /// </summary>
        /// <param name="e"></param>
        /// <returns>Bool of whether the entity can only be used for manipulation</returns>
        //internal bool CanManipulate(Entity e)
        //{
        //    if (EntityManager.HasComponent<Components.MIT_ControlPoint>(e)) return true;
        //    if (EntityManager.HasComponent<Game.Net.Edge>(e)) return true;
        //    return false;
        //}

        /// <summary>
        /// Can this entity _only_ be used for manipulation?
        /// </summary>
        /// <param name="e"></param>
        /// <returns>Bool of whether the entity can only be used for manipulation</returns>
        //internal bool CanOnlyManipulate(Entity e)
        //{
        //    if (EntityManager.HasComponent<Components.MIT_ControlPoint>(e)) return true;
        //    return false;
        //}
        #endregion

        internal void UpdateMarqueeList(Input.Marquee marquee)
        {
            if (!marquee.Update(m_PointerPos))
            {
                marquee.m_Entities = new();
            }

            Bounds3 bounds = marquee.GetBounds();

            //GenerateUpdateGrid(bounds, out NativeArray<Bounds3> grid, out NativeArray<SelectionGridModes> mode);

            using Searcher.Marquee searcher = new(Searcher.Filters.All & ~Searcher.Filters.Segments, m_IsManipulateMode);
            searcher.Search(marquee.m_SelectArea.xz, bounds.xz);

            if (!marquee.m_LastBounds.Equals(bounds)) marquee.m_LastBounds = bounds;
            HashSet<Entity> result = new();
            for (int i = 0; i < math.min(searcher.m_Results.Length, MoveIt.Selection.SelectionBase.MAX_SELECTION_SIZE); i++)
            {
                if (IsValid(searcher.m_Results[i]))
                {
                    result.Add(searcher.m_Results[i]);
                }
            }
            marquee.m_Entities = result;
        }

        /// <summary>
        /// Generates a 3x3 grid for what marquee selection areas need updated per frame; Ignore, Add, or Remove
        /// Center tile would always be Ignore, so it is skipped
        /// </summary>
        /// <param name="bounds">The cartesian bounds of the new selection area</param>
        /// <param name="grid">The bounds of the resulting grid tiles (bottom left, bottom center, bottom right, mid left, etc)</param>
        /// <param name="mode">The SelectionGridModes mode of each tile</param>
        //private void GenerateUpdateGrid(Input.Marquee marquee, Bounds3 bounds, out NativeArray<Bounds3> grid, out NativeArray<SelectionGridModes> mode)
        //{
        //    float4 outer, inner;
        //    Bounds3 prev = marquee.m_LastBounds;
        //    grid = new(8, Allocator.Temp);
        //    mode = new(8, Allocator.Temp);

        //    (outer.x, inner.x) = (bounds.min.x < prev.min.x) ? (bounds.min.x, prev.min.x) : (prev.min.x, bounds.min.x); // Left
        //    (outer.y, inner.y) = (bounds.max.x > prev.max.x) ? (bounds.max.x, prev.max.x) : (prev.max.x, bounds.max.x); // Right
        //    (outer.z, inner.z) = (bounds.min.z < prev.min.z) ? (bounds.min.z, prev.min.z) : (prev.min.z, bounds.min.z); // Bottom
        //    (outer.w, inner.w) = (bounds.max.z > prev.max.z) ? (bounds.max.z, prev.max.z) : (prev.max.z, bounds.max.z); // Top

        //    grid[0] = new(new(outer.x, 0, outer.z), new(inner.x, 0, inner.z));
        //    grid[1] = new(new(inner.x, 0, outer.z), new(inner.y, 0, inner.z));
        //    grid[2] = new(new(inner.y, 0, outer.z), new(outer.y, 0, inner.z));

        //    grid[3] = new(new(outer.x, 0, inner.z), new(inner.x, 0, inner.w));
        //    //grid[4] = new(new(inner.x, 0, inner.z), new(inner.y, 0, inner.w));
        //    grid[4] = new(new(inner.y, 0, inner.z), new(outer.y, 0, inner.w));

        //    grid[5] = new(new(outer.x, 0, inner.w), new(inner.x, 0, outer.w));
        //    grid[6] = new(new(inner.x, 0, inner.w), new(inner.y, 0, outer.w));
        //    grid[7] = new(new(inner.y, 0, inner.w), new(outer.y, 0, outer.w));

        //    for (int i = 0; i < 9; i++)
        //    {
        //        mode[i] = SelectionGridModes.Unprocessed;
        //    }
        //    //mode[4] = SelectionGridModes.Ignore;
        //    if (bounds.min.x == prev.min.x)
        //    {
        //        mode[0] = SelectionGridModes.Ignore;
        //        mode[3] = SelectionGridModes.Ignore;
        //        mode[5] = SelectionGridModes.Ignore;
        //    }
        //    if (bounds.min.z == prev.min.z)
        //    {
        //        mode[0] = SelectionGridModes.Ignore;
        //        mode[1] = SelectionGridModes.Ignore;
        //        mode[2] = SelectionGridModes.Ignore;
        //    }
        //    if (bounds.max.x == prev.max.x)
        //    {
        //        mode[2] = SelectionGridModes.Ignore;
        //        mode[4] = SelectionGridModes.Ignore;
        //        mode[7] = SelectionGridModes.Ignore;
        //    }
        //    if (bounds.max.z == prev.max.z)
        //    {
        //        mode[5] = SelectionGridModes.Ignore;
        //        mode[6] = SelectionGridModes.Ignore;
        //        mode[7] = SelectionGridModes.Ignore;
        //    }

        //    if (mode[0] != SelectionGridModes.Ignore && !PointInRectangle(marquee.m_SelectArea, inner.x, inner.z) && !PointInRectangle(marquee.m_LastSelectArea, inner.x, inner.z)) mode[0] = SelectionGridModes.Ignore;
        //    if (mode[2] != SelectionGridModes.Ignore && !PointInRectangle(marquee.m_SelectArea, inner.y, inner.z) && !PointInRectangle(marquee.m_LastSelectArea, inner.y, inner.z)) mode[2] = SelectionGridModes.Ignore;
        //    if (mode[5] != SelectionGridModes.Ignore && !PointInRectangle(marquee.m_SelectArea, inner.x, inner.w) && !PointInRectangle(marquee.m_LastSelectArea, inner.x, inner.w)) mode[5] = SelectionGridModes.Ignore;
        //    if (mode[7] != SelectionGridModes.Ignore && !PointInRectangle(marquee.m_SelectArea, inner.y, inner.w) && !PointInRectangle(marquee.m_LastSelectArea, inner.y, inner.w)) mode[7] = SelectionGridModes.Ignore;

        //    if (mode[0] != SelectionGridModes.Ignore) mode[0] = (bounds.min.x < prev.min.x || bounds.min.z < prev.min.z) ? SelectionGridModes.Add : SelectionGridModes.Remove;
        //    if (mode[1] != SelectionGridModes.Ignore) mode[1] = (bounds.min.z < prev.min.z) ? SelectionGridModes.Add : SelectionGridModes.Remove;
        //    if (mode[2] != SelectionGridModes.Ignore) mode[2] = (bounds.max.x > prev.max.x || bounds.min.z < prev.min.z) ? SelectionGridModes.Add : SelectionGridModes.Remove;
        //    if (mode[3] != SelectionGridModes.Ignore) mode[3] = (bounds.min.x < prev.min.x) ? SelectionGridModes.Add : SelectionGridModes.Remove;
        //    if (mode[4] != SelectionGridModes.Ignore) mode[5] = (bounds.max.x > prev.max.x) ? SelectionGridModes.Add : SelectionGridModes.Remove;
        //    if (mode[5] != SelectionGridModes.Ignore) mode[6] = (bounds.min.x < prev.min.x || bounds.max.z > prev.max.z) ? SelectionGridModes.Add : SelectionGridModes.Remove;
        //    if (mode[6] != SelectionGridModes.Ignore) mode[7] = (bounds.max.z > prev.max.z) ? SelectionGridModes.Add : SelectionGridModes.Remove;
        //    if (mode[7] != SelectionGridModes.Ignore) mode[8] = (bounds.max.x > prev.max.x || bounds.max.z > prev.max.z) ? SelectionGridModes.Add : SelectionGridModes.Remove;

        //    //ClearDebugOverlays();
        //    //Dictionary<SelectionGridModes, Color> selectionGridColours = new Dictionary<SelectionGridModes, Color>()
        //    //{
        //    //    { SelectionGridModes.Unprocessed,   new(1f, 0f, 0f, 0.8f) },
        //    //    { SelectionGridModes.Ignore,        new(1f, 1f, 1f, 0.3f) },
        //    //    { SelectionGridModes.Add,           new(0f, 1f, 0f, 0.7f) },
        //    //    { SelectionGridModes.Remove,        new(0f, 0f, 1f, 0.7f) },
        //    //};
        //    ////if (!m_LastMarqueeBounds.Equals(new(float.MaxValue, float.MaxValue))) AddDebugBounds(m_LastMarqueeBounds, Overlay.Colors.Deselect);
        //    //if (mode[0] != SelectionGridModes.Ignore) AddDebugBounds(grid[0], selectionGridColours[mode[0]]);
        //    //if (mode[1] != SelectionGridModes.Ignore) AddDebugBounds(grid[1], selectionGridColours[mode[1]]);
        //    //if (mode[2] != SelectionGridModes.Ignore) AddDebugBounds(grid[2], selectionGridColours[mode[2]]);
        //    //if (mode[3] != SelectionGridModes.Ignore) AddDebugBounds(grid[3], selectionGridColours[mode[3]]);
        //    //if (mode[4] != SelectionGridModes.Ignore) AddDebugBounds(grid[4], selectionGridColours[mode[4]]);
        //    //if (mode[5] != SelectionGridModes.Ignore) AddDebugBounds(grid[5], selectionGridColours[mode[5]]);
        //    //if (mode[6] != SelectionGridModes.Ignore) AddDebugBounds(grid[6], selectionGridColours[mode[6]]);
        //    //if (mode[7] != SelectionGridModes.Ignore) AddDebugBounds(grid[7], selectionGridColours[mode[7]]);
        //}


        internal bool PointInRectangle(Quad3 rectangle, float x, float z)
        {
            return PointInRectangle(rectangle, new(x, 0f, z));
        }

        internal bool PointInRectangle(Quad3 rectangle, float3 p)
        {
            return IsLeft(rectangle.a, rectangle.b, p) && IsLeft(rectangle.b, rectangle.c, p) && IsLeft(rectangle.c, rectangle.d, p) && IsLeft(rectangle.d, rectangle.a, p);
        }

        private bool IsLeft(float3 p0, float3 p1, float3 p2)
        {
            return ((p1.x - p0.x) * (p2.z - p0.z) - (p2.x - p0.x) * (p1.z - p0.z)) > 0;
        }
    }
}
