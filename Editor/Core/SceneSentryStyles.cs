using System;
using UnityEditor;
using UnityEngine;

namespace SceneSentry.Editor
{
    /// <summary>
    /// Centralized UI styles, color palettes, and drawing utilities for SceneSentry Editor windows.
    /// Provides consistent, theme-aware visuals adhering to modern Unity 6 design standards.
    /// </summary>
    public static class SceneSentryStyles
    {
        // Theme Colors
        public static readonly Color ErrorColor = new Color(0.95f, 0.3f, 0.3f, 1f);
        public static readonly Color WarningColor = new Color(1.0f, 0.72f, 0.2f, 1f);
        public static readonly Color InfoColor = new Color(0.2f, 0.72f, 0.95f, 1f);
        public static readonly Color SuccessColor = new Color(0.3f, 0.8f, 0.45f, 1f);
        public static readonly Color AccentColor = new Color(0.35f, 0.65f, 1.0f, 1f);

        public static Color BackgroundCardColor => EditorGUIUtility.isProSkin
            ? new Color(0.22f, 0.22f, 0.22f, 1f)
            : new Color(0.88f, 0.88f, 0.88f, 1f);

        public static Color HeaderCardColor => EditorGUIUtility.isProSkin
            ? new Color(0.18f, 0.18f, 0.18f, 1f)
            : new Color(0.92f, 0.92f, 0.92f, 1f);

        public static Color BorderColor => EditorGUIUtility.isProSkin
            ? new Color(0.14f, 0.14f, 0.14f, 1f)
            : new Color(0.75f, 0.75f, 0.75f, 1f);

        private static GUIStyle s_CardStyle;
        private static GUIStyle s_HeaderTitleStyle;
        private static GUIStyle s_HeaderSubtitleStyle;
        private static GUIStyle s_BadgeStyle;
        private static GUIStyle s_ChipStyle;
        private static GUIStyle s_ChipRemoveBtnStyle;
        private static GUIStyle s_SectionHeaderStyle;
        private static GUIStyle s_FindingTitleStyle;
        private static GUIStyle s_FindingExplanationStyle;
        private static GUIStyle s_FindingSuggestionStyle;
        private static GUIStyle s_CenteredHeaderStyle;
        private static GUIStyle s_CenteredSubtitleStyle;

        public static GUIStyle CardStyle
        {
            get
            {
                if (s_CardStyle == null)
                {
                    s_CardStyle = new GUIStyle(GUI.skin.box)
                    {
                        padding = new RectOffset(12, 12, 10, 10),
                        margin = new RectOffset(4, 4, 4, 6)
                    };
                }
                return s_CardStyle;
            }
        }

        private static GUIStyle s_ErrorCardStyle;
        private static GUIStyle s_WarningCardStyle;
        private static GUIStyle s_InfoCardStyle;

        private static Texture2D s_ErrorBgTex;
        private static Texture2D s_WarningBgTex;
        private static Texture2D s_InfoBgTex;
        private static bool s_LastIsProSkin;

        public static GUIStyle GetFindingCardStyle(ValidationSeverity severity)
        {
            if (s_LastIsProSkin != EditorGUIUtility.isProSkin)
            {
                s_LastIsProSkin = EditorGUIUtility.isProSkin;
                ResetFindingCardStyles();
            }

            switch (severity)
            {
                case ValidationSeverity.Error:
                    if (s_ErrorCardStyle == null || s_ErrorBgTex == null || s_ErrorCardStyle.normal.background == null)
                    {
                        Color bg = EditorGUIUtility.isProSkin
                            ? new Color(0.32f, 0.12f, 0.12f, 0.92f)
                            : new Color(1.0f, 0.88f, 0.88f, 1.0f);
                        Color border = EditorGUIUtility.isProSkin
                            ? new Color(0.85f, 0.25f, 0.25f, 0.50f)
                            : new Color(0.90f, 0.35f, 0.35f, 0.70f);
                        s_ErrorBgTex = CreateCardTexture(bg, border, ErrorColor);
                        s_ErrorCardStyle = CreateFindingCardStyle(s_ErrorBgTex);
                    }
                    return s_ErrorCardStyle;

                case ValidationSeverity.Warning:
                    if (s_WarningCardStyle == null || s_WarningBgTex == null || s_WarningCardStyle.normal.background == null)
                    {
                        Color bg = EditorGUIUtility.isProSkin
                            ? new Color(0.30f, 0.22f, 0.07f, 0.92f)
                            : new Color(1.0f, 0.95f, 0.80f, 1.0f);
                        Color border = EditorGUIUtility.isProSkin
                            ? new Color(0.85f, 0.65f, 0.15f, 0.50f)
                            : new Color(0.85f, 0.65f, 0.15f, 0.70f);
                        s_WarningBgTex = CreateCardTexture(bg, border, WarningColor);
                        s_WarningCardStyle = CreateFindingCardStyle(s_WarningBgTex);
                    }
                    return s_WarningCardStyle;

                case ValidationSeverity.Info:
                default:
                    if (s_InfoCardStyle == null || s_InfoBgTex == null || s_InfoCardStyle.normal.background == null)
                    {
                        Color bg = EditorGUIUtility.isProSkin
                            ? new Color(0.12f, 0.22f, 0.34f, 0.92f)
                            : new Color(0.88f, 0.94f, 1.0f, 1.0f);
                        Color border = EditorGUIUtility.isProSkin
                            ? new Color(0.20f, 0.60f, 0.85f, 0.50f)
                            : new Color(0.25f, 0.65f, 0.90f, 0.70f);
                        s_InfoBgTex = CreateCardTexture(bg, border, InfoColor);
                        s_InfoCardStyle = CreateFindingCardStyle(s_InfoBgTex);
                    }
                    return s_InfoCardStyle;
            }
        }

        private static GUIStyle CreateFindingCardStyle(Texture2D bgTex)
        {
            var style = new GUIStyle
            {
                padding = new RectOffset(14, 12, 10, 10),
                margin = new RectOffset(4, 4, 4, 6),
                border = new RectOffset(6, 3, 3, 3)
            };
            style.normal.background = bgTex;
            style.normal.scaledBackgrounds = new Texture2D[] { bgTex };
            return style;
        }

        private static void ResetFindingCardStyles()
        {
            if (s_ErrorBgTex != null) UnityEngine.Object.DestroyImmediate(s_ErrorBgTex);
            if (s_WarningBgTex != null) UnityEngine.Object.DestroyImmediate(s_WarningBgTex);
            if (s_InfoBgTex != null) UnityEngine.Object.DestroyImmediate(s_InfoBgTex);

            s_ErrorBgTex = null;
            s_WarningBgTex = null;
            s_InfoBgTex = null;

            s_ErrorCardStyle = null;
            s_WarningCardStyle = null;
            s_InfoCardStyle = null;
        }

        private static Texture2D CreateCardTexture(Color bgColor, Color borderColor, Color accentColor)
        {
            const int width = 24;
            const int height = 24;
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.DontSave
            };

            var pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color c;

                    // Left accent bar: columns 0 to 3 (4 pixels wide)
                    if (x < 4)
                    {
                        c = accentColor;
                    }
                    // 1px border on top, bottom, and right
                    else if (x == width - 1 || y == 0 || y == height - 1)
                    {
                        c = borderColor;
                    }
                    else
                    {
                        // Tinted card body
                        c = bgColor;
                    }

                    pixels[y * width + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private static Texture2D MakeSolidColorTexture(Color color)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.DontSave;
            var pixels = new Color[] { color, color, color, color };
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        public static GUIStyle HeaderTitleStyle
        {
            get
            {
                if (s_HeaderTitleStyle == null)
                {
                    s_HeaderTitleStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 17,
                        fontStyle = FontStyle.Bold,
                        wordWrap = true
                    };
                }
                return s_HeaderTitleStyle;
            }
        }

        public static GUIStyle HeaderSubtitleStyle
        {
            get
            {
                if (s_HeaderSubtitleStyle == null)
                {
                    s_HeaderSubtitleStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        fontSize = 11,
                        wordWrap = true
                    };
                }
                return s_HeaderSubtitleStyle;
            }
        }

        public static GUIStyle CenteredHeaderStyle
        {
            get
            {
                if (s_CenteredHeaderStyle == null)
                {
                    s_CenteredHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 15,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter,
                        wordWrap = true
                    };
                }
                return s_CenteredHeaderStyle;
            }
        }

        public static GUIStyle CenteredSubtitleStyle
        {
            get
            {
                if (s_CenteredSubtitleStyle == null)
                {
                    s_CenteredSubtitleStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        fontSize = 11,
                        alignment = TextAnchor.MiddleCenter,
                        wordWrap = true
                    };
                }
                return s_CenteredSubtitleStyle;
            }
        }

        public static GUIStyle FindingTitleStyle
        {
            get
            {
                if (s_FindingTitleStyle == null)
                {
                    s_FindingTitleStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 12,
                        fontStyle = FontStyle.Bold,
                        wordWrap = true
                    };
                }
                return s_FindingTitleStyle;
            }
        }

        public static GUIStyle FindingExplanationStyle
        {
            get
            {
                if (s_FindingExplanationStyle == null)
                {
                    s_FindingExplanationStyle = new GUIStyle(EditorStyles.label)
                    {
                        fontSize = 11,
                        wordWrap = true
                    };
                }
                return s_FindingExplanationStyle;
            }
        }

        public static GUIStyle FindingSuggestionStyle
        {
            get
            {
                if (s_FindingSuggestionStyle == null)
                {
                    s_FindingSuggestionStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        fontSize = 11,
                        wordWrap = true
                    };
                }
                return s_FindingSuggestionStyle;
            }
        }

        public static GUIStyle SectionHeaderStyle
        {
            get
            {
                if (s_SectionHeaderStyle == null)
                {
                    s_SectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 13,
                        margin = new RectOffset(4, 4, 8, 4),
                        wordWrap = true
                    };
                }
                return s_SectionHeaderStyle;
            }
        }

        public static GUIStyle BadgeStyle
        {
            get
            {
                if (s_BadgeStyle == null)
                {
                    s_BadgeStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontStyle = FontStyle.Bold,
                        fontSize = 10,
                        normal = { textColor = Color.white }
                    };
                }
                return s_BadgeStyle;
            }
        }

        public static GUIStyle ChipStyle
        {
            get
            {
                if (s_ChipStyle == null)
                {
                    s_ChipStyle = new GUIStyle(EditorStyles.miniButton)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        fontSize = 11,
                        fontStyle = FontStyle.Normal,
                        fixedHeight = 22,
                        padding = new RectOffset(8, 8, 2, 2)
                    };
                }
                return s_ChipStyle;
            }
        }

        public static GUIStyle ChipRemoveBtnStyle
        {
            get
            {
                if (s_ChipRemoveBtnStyle == null)
                {
                    s_ChipRemoveBtnStyle = new GUIStyle(EditorStyles.label)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 11,
                        fontStyle = FontStyle.Bold,
                        normal = { textColor = new Color(0.9f, 0.9f, 0.9f, 0.8f) },
                        hover = { textColor = new Color(1f, 0.4f, 0.4f, 1f) }
                    };
                }
                return s_ChipRemoveBtnStyle;
            }
        }

        public static void DrawBanner(string title, string subtitle, string badgeText = null)
        {
            EditorGUILayout.BeginVertical(CardStyle);
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.BeginVertical();
            GUILayout.Label(title, HeaderTitleStyle);
            if (!string.IsNullOrEmpty(subtitle))
            {
                GUILayout.Label(subtitle, HeaderSubtitleStyle);
            }
            EditorGUILayout.EndVertical();

            if (!string.IsNullOrEmpty(badgeText))
            {
                GUILayout.FlexibleSpace();
                var badgeContent = new GUIContent(badgeText);
                var size = BadgeStyle.CalcSize(badgeContent);
                var rect = GUILayoutUtility.GetRect(size.x + 16, 22);
                DrawBadge(rect, badgeText, AccentColor);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        public static void DrawBadge(Rect rect, string text, Color color)
        {
            Color prevColor = GUI.color;
            GUI.color = color;
            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
            GUI.color = prevColor;

            GUI.Label(rect, text, BadgeStyle);
        }

        public static void DrawTagChip(string name, Color color, Action onRemove = null, Action onClick = null)
        {
            string displayName = string.IsNullOrEmpty(name) ? "Untitled Tag" : name;
            var content = new GUIContent(displayName);
            var labelSize = EditorStyles.miniBoldLabel.CalcSize(content);

            float bodyWidth = labelSize.x + 24f; // dot (9px) + padding
            float removeWidth = onRemove != null ? 22f : 0f;
            float totalWidth = bodyWidth + removeWidth;

            var chipRect = GUILayoutUtility.GetRect(totalWidth, 22, GUILayout.ExpandWidth(false));

            // Theme-aware dark/light pill background
            Color pillBg = EditorGUIUtility.isProSkin
                ? new Color(0.19f, 0.19f, 0.19f, 1f)
                : new Color(0.88f, 0.88f, 0.88f, 1f);

            Color borderCol = EditorGUIUtility.isProSkin
                ? new Color(Mathf.Clamp01(color.r * 0.8f + 0.1f), Mathf.Clamp01(color.g * 0.8f + 0.1f), Mathf.Clamp01(color.b * 0.8f + 0.1f), 0.7f)
                : new Color(0.6f, 0.6f, 0.6f, 1f);

            EditorGUI.DrawRect(chipRect, pillBg);
            DrawRectOutline(chipRect, borderCol);

            // Body Rect (clicking body opens manager/inspects)
            var bodyRect = new Rect(chipRect.x, chipRect.y, bodyWidth, chipRect.height);
            if (onClick != null)
            {
                EditorGUIUtility.AddCursorRect(bodyRect, MouseCursor.Link);
                if (GUI.Button(bodyRect, GUIContent.none, GUIStyle.none))
                {
                    onClick.Invoke();
                }
            }

            // Colored tag dot
            var dotRect = new Rect(chipRect.x + 6, chipRect.y + 6, 9, 9);
            EditorGUI.DrawRect(dotRect, color);

            // Tag name text
            var labelRect = new Rect(chipRect.x + 19, chipRect.y + 2, labelSize.x + 4, 18);
            GUI.Label(labelRect, displayName, EditorStyles.miniBoldLabel);

            // Remove Button (100% separate, zero overlap)
            if (onRemove != null)
            {
                var removeRect = new Rect(chipRect.x + bodyWidth, chipRect.y, removeWidth, chipRect.height);
                EditorGUIUtility.AddCursorRect(removeRect, MouseCursor.Link);

                bool isHovering = removeRect.Contains(Event.current.mousePosition);
                if (isHovering)
                {
                    EditorGUI.DrawRect(removeRect, new Color(0.9f, 0.2f, 0.2f, 0.35f));
                }

                Color prevColor = GUI.contentColor;
                GUI.contentColor = isHovering ? new Color(1f, 0.4f, 0.4f, 1f) : new Color(0.8f, 0.8f, 0.8f, 0.9f);

                if (GUI.Button(removeRect, new GUIContent("X", $"Remove tag '{displayName}'"), ChipRemoveBtnStyle))
                {
                    onRemove.Invoke();
                    GUI.changed = true;
                    Event.current.Use();
                }

                GUI.contentColor = prevColor;
            }
        }

        private static void DrawRectOutline(Rect rect, Color color)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1, rect.width, 1), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1, rect.y, 1, rect.height), color);
        }
    }
}
