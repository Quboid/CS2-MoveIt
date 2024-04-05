using MoveIt.Tool;
using System.Collections.Generic;
using UnityEngine;

namespace MoveIt.Overlays
{
    public static class Colors
    {
        public enum Styles
        {
            None,
            Background,
            Foreground,
        }

        public static Dictionary<string, Dictionary<Styles, Dictionary<OverlayFlags, Color>>> s_Themes = new()
        {
            {
                "Default", new()
            {
                { Styles.None, new() {
                    { OverlayFlags.None,            new Color32(255, 0, 0, 220) },
                    { OverlayFlags.Hovering,        new Color32(255, 0, 0, 220) },
                    { OverlayFlags.Selected,        new Color32(255, 0, 0, 220) },
                    { OverlayFlags.Moving,          new Color32(255, 0, 0, 220) },
                    { OverlayFlags.Unselect,        new Color32(255, 0, 0, 220) },
                    { OverlayFlags.Tool,            new Color32(255, 0, 0, 220) },
                } },
                { Styles.Background, new() {
                    { OverlayFlags.None,            new Color32(165, 165, 180, 120) },
                } },
                { Styles.Foreground, new() {
                    { OverlayFlags.Hovering,        new Color32(0, 181, 255, 250) },
                    { OverlayFlags.Selected,        new Color32(95, 166, 0, 244) },
                    { OverlayFlags.Moving,          new Color32(95, 166, 0, 44) },
                    { OverlayFlags.Unselect,        new Color32(255, 160, 47, 191) },
                    { OverlayFlags.Tool,            new Color32(245, 25, 250, 160) },
                } },
            } },
            {
                "ManipulateParent", new()
            {
                { Styles.Foreground, new() {
                    { OverlayFlags.Hovering,        new Color32(240, 140, 255, 190) },
                    { OverlayFlags.Selected,        new Color32(240, 140, 255, 150) },
                } },
            } },
            {
                "ManipulateChild", new()
            {
                { Styles.Foreground, new() {
                    { OverlayFlags.Hovering,        new Color32(215, 145, 255, 230) },
                    { OverlayFlags.Selected,        new Color32(200, 130, 240, 180) },
                } },
            } },
        };

        public static Color GetForced(OverlayFlags flag, Styles style = Styles.Foreground, string theme = "Default")
        {
            return s_Themes[theme][style][flag];
        }

        public static Color Get(Utils.OverlayCommon common, Styles style)
        {
            Color result = GetBase(common, style);

            if (MIT.m_Instance.Manipulating)
            {
                if (common.Manipulatable == QTypes.Manipulate.Normal)
                {
                    result.a *= 0.25f;
                }
            }

            return result;
        }

        public static Color GetBase(Utils.OverlayCommon common, Styles style)
        {
            if (common.PrimaryFlag == OverlayFlags.Custom)
            {
                return common.CustomColor;
            }

            string themeName = GetThemeName(common);

            if (s_Themes[themeName].ContainsKey(style))
            {
                if (s_Themes[themeName][style].ContainsKey(common.PrimaryFlag))
                {
                    return s_Themes[themeName][style][common.PrimaryFlag];
                }
                if (s_Themes[themeName][style].ContainsKey(OverlayFlags.None))
                {
                    return s_Themes[themeName][style][OverlayFlags.None];
                }
            }

            // Fallback to Default
            if (s_Themes["Default"].ContainsKey(style))
            {
                if (s_Themes["Default"][style].ContainsKey(common.PrimaryFlag))
                {
                    return s_Themes["Default"][style][common.PrimaryFlag];
                }
                if (s_Themes["Default"][style].ContainsKey(OverlayFlags.None))
                {
                    return s_Themes["Default"][style][OverlayFlags.None];
                }
            }
            if (s_Themes["Default"][Styles.None].ContainsKey(common.PrimaryFlag))
            {
                return s_Themes["Default"][Styles.None][common.PrimaryFlag];
            }
            return s_Themes["Default"][Styles.None][OverlayFlags.None];
        }

        private static string GetThemeName(Utils.OverlayCommon common)
        {
            string themeName = "Default";
            if (common.Manipulatable == QTypes.Manipulate.Parent)
            {
                themeName = "ManipulateParent";
            }
            else if (common.Manipulatable == QTypes.Manipulate.Child)
            {
                themeName = "ManipulateChild";
            }

            if (!s_Themes.ContainsKey(themeName)) throw new System.Exception($"{themeName} is not in palette");
            return themeName;
        }
    }
}
