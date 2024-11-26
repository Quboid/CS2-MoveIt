using Unity.Burst;
using Unity.Collections;
using UnityEngine;

namespace MoveIt.Overlays
{
    public abstract class ColorData
    {
        public enum Contexts
        {
            None,
            Hovering,
            Selected,
            Deselect,
            ToolHover,
            ToolParentHover,
            Shadow,
            Background,
            ManipParentHovering,
            ManipParentSelected,
            ManipChildHovering,
            ManipChildSelected,
            Other,
        }

        public static readonly SharedStatic<NativeHashMap<int, Color>> s_Lookup = SharedStatic<NativeHashMap<int, Color>>.GetOrCreate<ColorData, LookupKey>();
        // ReSharper disable once ClassNeverInstantiated.Local
        private class LookupKey { }

        public static void Init()
        {
            s_Lookup.Data = new(12, Allocator.Persistent)
            {
                { (int)Contexts.None,                   new Color32(0, 0, 0, 0) },
                { (int)Contexts.Hovering,               new Color32(0, 181, 255, 250) },
                { (int)Contexts.Selected,               new Color32(95, 166, 0, 244) },
                { (int)Contexts.Deselect,               new Color32(255, 160, 47, 191) },
                { (int)Contexts.ToolHover,              new Color32(250, 250, 255, 200) },
                { (int)Contexts.ToolParentHover,        new Color32(230, 230, 240, 150) },
                { (int)Contexts.Shadow,                 new Color32(165, 165, 170, 50) },
                { (int)Contexts.Background,             new Color32(165, 165, 180, 150) },
                { (int)Contexts.ManipParentHovering,    new Color32(215, 100, 250, 220) },
                { (int)Contexts.ManipParentSelected,    new Color32(215, 100, 250, 145) },
                { (int)Contexts.ManipChildHovering,     new Color32(200, 200, 255, 255) },
                { (int)Contexts.ManipChildSelected,     new Color32(200, 210, 255, 200) },
            };
        }

        public static void Dispose()
        {
            s_Lookup.Data.Dispose();
        }
    }

    public readonly struct Colors
    {
        public static Color Get(MIO_Common common, ToolFlags toolFlags, float opacity = 1f)
        {
            if ((common.m_Flags & Tool.InteractionFlags.Static) != 0) return common.m_OutlineColor;

            ColorData.Contexts context = GetContext(common, toolFlags);
            Color c = Get(context);
            c.a *= opacity;
            return c;
        }

        public static Color Get(ColorData.Contexts context)
        {
            if (!ColorData.s_Lookup.Data.ContainsKey((int)context)) return new(1f, 0f, 0f, 0.75f);

            return ColorData.s_Lookup.Data[(int)context];
        }

        public static void Dispose()
        {
            ColorData.Dispose();
        }

        public static ColorData.Contexts GetContext(MIO_Common common, ToolFlags toolFlags)
        {
            if ((common.m_Flags & Tool.InteractionFlags.Static) != 0)           return ColorData.Contexts.Other;
            if ((common.m_Flags & Tool.InteractionFlags.ToolHover) != 0)        return ColorData.Contexts.ToolHover;
            if ((common.m_Flags & Tool.InteractionFlags.ToolParentHover) != 0)  return ColorData.Contexts.ToolParentHover;

            if (((toolFlags & ToolFlags.HasShift) != 0) && ((toolFlags & ToolFlags.IsMarquee) == 0) && ((common.m_Flags & Tool.InteractionFlags.Hovering) != 0) && ((common.m_Flags & Tool.InteractionFlags.Selected) != 0))
            {
                return ColorData.Contexts.Deselect;
            }

            return ((toolFlags & ToolFlags.ManipulationMode) > 0) ? GetContextManip(common) : GetContextNormal(common);
        }

        private static ColorData.Contexts GetContextNormal(MIO_Common common)
        {
            if (common.m_IsManipulatable) return ColorData.Contexts.None;

            if ((common.m_Flags & Tool.InteractionFlags.ToolHover) != 0)        return ColorData.Contexts.ToolHover;
            if ((common.m_Flags & Tool.InteractionFlags.ToolParentHover) != 0)  return ColorData.Contexts.ToolParentHover;
            if ((common.m_Flags & Tool.InteractionFlags.Hovering) != 0)         return ColorData.Contexts.Hovering;
            if ((common.m_Flags & Tool.InteractionFlags.ParentHovering) != 0)   return ColorData.Contexts.Hovering;
            if ((common.m_Flags & Tool.InteractionFlags.Selected) != 0)         return ColorData.Contexts.Selected;
            if ((common.m_Flags & Tool.InteractionFlags.ParentSelected) != 0)   return ColorData.Contexts.Selected;

            return ColorData.Contexts.None;
        }

        private static ColorData.Contexts GetContextManip(MIO_Common common)
        {
            if (!common.m_IsManipulatable) return ColorData.Contexts.None;

            if (common.m_IsManipChild)
            {
                if ((common.m_Flags & Tool.InteractionFlags.ToolHover) != 0)            return ColorData.Contexts.ToolHover;
                if ((common.m_Flags & Tool.InteractionFlags.ToolParentHover) != 0)      return ColorData.Contexts.ToolParentHover;
                if ((common.m_Flags & Tool.InteractionFlags.Hovering) != 0)             return ColorData.Contexts.ManipChildHovering;
                if ((common.m_Flags & Tool.InteractionFlags.Selected) != 0)             return ColorData.Contexts.ManipChildSelected;
                if ((common.m_Flags & Tool.InteractionFlags.ParentHovering) != 0)       return ColorData.Contexts.ManipParentHovering;
                if ((common.m_Flags & Tool.InteractionFlags.ParentManipulating) != 0)   return ColorData.Contexts.ManipParentSelected;
            }
            else
            {
                if ((common.m_Flags & Tool.InteractionFlags.Hovering) != 0)             return ColorData.Contexts.ManipParentHovering;
                if ((common.m_Flags & Tool.InteractionFlags.Selected) != 0)             return ColorData.Contexts.ManipParentSelected;
            }

            return ColorData.Contexts.None;
        }
    }
}
