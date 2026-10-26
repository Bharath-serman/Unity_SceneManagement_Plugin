using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SceneSentry.Editor
{
    /// <summary>
    /// Interactive search and exploration window for loaded scenes.
    /// Allows filtering by name, custom tags, Unity tags, layers, and component types with AND/OR logic.
    /// </summary>
    public class SceneExplorerWindow : EditorWindow
    {
        public enum ExplorerViewMode
        {
            HierarchyTree = 0,
            GroupedByTag = 1,
            CompactTable = 2,
            CardList = 3
        }

        private SceneExplorerFilter _filter = new SceneExplorerFilter();
        private List<SceneExplorerResultItem> _results = new List<SceneExplorerResultItem>();
        private TagDatabase _database;

        private Vector2 _scrollPos;
        private bool _showFiltersDrawer = false;
        private bool _needsRefresh = true;

        private ExplorerViewMode _viewMode = ExplorerViewMode.HierarchyTree;
        private readonly HashSet<int> _collapsedParentIds = new HashSet<int>();
        private readonly HashSet<string> _collapsedTagGroups = new HashSet<string>();

        // Pagination
        private int _currentPage = 1;
        private int _pageSizeIndex = 1; // Default 50 per page
        private static readonly int[] s_PageSizes = new int[] { 25, 50, 100, 0 };
        private static readonly string[] s_PageSizeLabels = new string[] { "25 / Page", "50 / Page", "100 / Page", "Show All" };

        [MenuItem("Window/SceneSentry/Scene Explorer", false, 10)]
        public static void Open()
        {
            var window = GetWindow<SceneExplorerWindow>("Scene Explorer");
            window.minSize = new Vector2(520, 420);
            window.Show();
        }

        private void OnEnable()
        {
            _database = TagDatabaseManager.ActiveDatabase;
            _needsRefresh = true;
            TagDatabaseManager.OnDatabaseChanged += HandleDatabaseChanged;
        }

        private void OnDisable()
        {
            TagDatabaseManager.OnDatabaseChanged -= HandleDatabaseChanged;
        }

        private void HandleDatabaseChanged()
        {
            _database = TagDatabaseManager.ActiveDatabase;
            _needsRefresh = true;
            Repaint();
        }

        private void Update()
        {
            if (_needsRefresh)
            {
                RefreshSearch();
                _needsRefresh = false;
                Repaint();
            }
        }

        private void RefreshSearch()
        {
            _database = TagDatabaseManager.ActiveDatabase;
            _results = SceneExplorerEngine.Search(_filter, _database, SceneSentrySettings.MaxExplorerResults);
            _currentPage = 1;
        }

        private void OnGUI()
        {
            _database = TagDatabaseManager.ActiveDatabase;

            DrawToolbar();

            if (_showFiltersDrawer)
            {
                DrawFiltersDrawer();
            }

            DrawResultHeader();
            DrawResultsList();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Search query field
            EditorGUI.BeginChangeCheck();
            GUI.SetNextControlName("SceneExplorerSearchField");
            string newSearch = EditorGUILayout.TextField(_filter.SearchText, EditorStyles.toolbarSearchField, GUILayout.Width(200));
            if (EditorGUI.EndChangeCheck())
            {
                _filter.SearchText = newSearch;
                _needsRefresh = true;
            }

            if (GUILayout.Button("X", EditorStyles.toolbarButton, GUILayout.Width(20)))
            {
                _filter.SearchText = string.Empty;
                GUI.FocusControl(null);
                _needsRefresh = true;
            }

            // Logic Mode toggle (AND / OR)
            EditorGUI.BeginChangeCheck();
            var newMode = (MatchMode)EditorGUILayout.EnumPopup(_filter.LogicMode, EditorStyles.toolbarDropDown, GUILayout.Width(115));
            if (EditorGUI.EndChangeCheck())
            {
                _filter.LogicMode = newMode;
                _needsRefresh = true;
            }

            // Toggle Filters Drawer
            string filterBtnLabel = _filter.HasActiveFilters() ? "Filters (Active)" : "Filters";
            if (GUILayout.Button(filterBtnLabel, EditorStyles.toolbarDropDown, GUILayout.Width(105)))  //Size of the Filters Button in the top bar.
            {
                _showFiltersDrawer = !_showFiltersDrawer;
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Reset", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                _filter.Reset();
                _needsRefresh = true;
            }

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                _needsRefresh = true;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawFiltersDrawer()
        {
            EditorGUILayout.BeginVertical(SceneSentryStyles.CardStyle);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search Criteria & Filter Options", SceneSentryStyles.SectionHeaderStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Clear All Filters", EditorStyles.miniButton, GUILayout.Width(100)))
            {
                _filter.Reset();
                _needsRefresh = true;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);
            EditorGUI.BeginChangeCheck();

            // Balanced 2-Column layout
            float halfWidth = (position.width - 45f) * 0.5f;
            EditorGUILayout.BeginHorizontal();

            // Column 1
            EditorGUILayout.BeginVertical(GUILayout.Width(halfWidth));

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Active State:", GUILayout.Width(100));  //Width of the Active state Dropdown.
            _filter.ActiveFilter = (ActiveStateFilter)EditorGUILayout.EnumPopup(_filter.ActiveFilter);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Custom Tags:", GUILayout.Width(100));
            string tagBtnLabel = _filter.CustomTagIds.Count == 0 ? "Any Tag" : $"{_filter.CustomTagIds.Count} Selected";
            if (GUILayout.Button(tagBtnLabel, EditorStyles.miniButton))
            {
                ShowCustomTagFilterMenu();
            }
            if (_filter.CustomTagIds.Count > 0)
            {
                if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(22)))
                {
                    _filter.CustomTagIds.Clear();
                    _needsRefresh = true;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            GUILayout.Space(12);

            // Column 2
            EditorGUILayout.BeginVertical(GUILayout.Width(halfWidth));

            EditorGUILayout.BeginHorizontal();
            _filter.UseLayerFilter = EditorGUILayout.ToggleLeft("Filter Layer:", _filter.UseLayerFilter, GUILayout.Width(95));
            if (_filter.UseLayerFilter)
            {
                _filter.LayerMask = EditorGUILayout.MaskField(_filter.LayerMask, UnityEditorInternal.InternalEditorUtility.layers);
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField("All Layers");
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Component:", GUILayout.Width(95));
            _filter.ComponentTypeName = EditorGUILayout.TextField(_filter.ComponentTypeName);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);

            // Diagnostics Bar
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Diagnostics:", EditorStyles.boldLabel, GUILayout.Width(90));
            _filter.MissingScriptsOnly = EditorGUILayout.ToggleLeft("Missing Scripts Only", _filter.MissingScriptsOnly, GUILayout.Width(150));
            GUILayout.Space(12);
            _filter.MissingReferencesOnly = EditorGUILayout.ToggleLeft("Missing References Only", _filter.MissingReferencesOnly, GUILayout.Width(165));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                _needsRefresh = true;
            }

            EditorGUILayout.EndVertical();
        }

        private void ShowCustomTagFilterMenu()
        {
            if (_database == null) return;
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Any Tag (Clear)"), _filter.CustomTagIds.Count == 0, () =>
            {
                _filter.CustomTagIds.Clear();
                _needsRefresh = true;
            });
            menu.AddSeparator(string.Empty);

            var tags = _database.Tags;
            for (int i = 0; i < tags.Count; i++)
            {
                var t = tags[i];
                if (t == null) continue;
                bool isSelected = _filter.CustomTagIds.Contains(t.Id);
                menu.AddItem(new GUIContent(t.Name), isSelected, () =>
                {
                    if (isSelected) _filter.CustomTagIds.Remove(t.Id);
                    else _filter.CustomTagIds.Add(t.Id);
                    _needsRefresh = true;
                });
            }
            menu.ShowAsContext();
        }

        private void DrawResultHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            string countText = $"{_results.Count} object(s)";
            if (_results.Count >= SceneSentrySettings.MaxExplorerResults)
            {
                countText += $" (Capped)";
            }
            EditorGUILayout.LabelField(countText, EditorStyles.miniBoldLabel, GUILayout.Width(110));

            GUILayout.Space(4);

            // Display Mode Selector
            _viewMode = (ExplorerViewMode)EditorGUILayout.EnumPopup(_viewMode, EditorStyles.toolbarDropDown, GUILayout.Width(135));

            if (_viewMode == ExplorerViewMode.HierarchyTree)
            {
                if (GUILayout.Button("Expand All", EditorStyles.toolbarButton, GUILayout.Width(70)))
                {
                    _collapsedParentIds.Clear();
                }
                if (GUILayout.Button("Collapse All", EditorStyles.toolbarButton, GUILayout.Width(75)))
                {
                    CollapseAllHierarchyNodes();
                }
            }
            else if (_viewMode == ExplorerViewMode.GroupedByTag)
            {
                if (GUILayout.Button("Expand All", EditorStyles.toolbarButton, GUILayout.Width(70)))
                {
                    _collapsedTagGroups.Clear();
                }
                if (GUILayout.Button("Collapse All", EditorStyles.toolbarButton, GUILayout.Width(75)))
                {
                    CollapseAllTagGroups();
                }
            }

            GUILayout.FlexibleSpace();

            // Pagination Controls
            if (_results.Count > 0 && s_PageSizes[_pageSizeIndex] > 0)
            {
                int pageSize = s_PageSizes[_pageSizeIndex];
                int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)_results.Count / pageSize));
                _currentPage = Mathf.Clamp(_currentPage, 1, totalPages);

                EditorGUI.BeginDisabledGroup(_currentPage <= 1);
                if (GUILayout.Button("<", EditorStyles.toolbarButton, GUILayout.Width(20)))
                {
                    _currentPage--;
                }
                EditorGUI.EndDisabledGroup();

                GUILayout.Label($"{_currentPage}/{totalPages}", EditorStyles.miniLabel, GUILayout.Width(45));

                EditorGUI.BeginDisabledGroup(_currentPage >= totalPages);
                if (GUILayout.Button(">", EditorStyles.toolbarButton, GUILayout.Width(20)))
                {
                    _currentPage++;
                }
                EditorGUI.EndDisabledGroup();

                GUILayout.Space(4);
            }

            // Page Size Selector
            _pageSizeIndex = EditorGUILayout.Popup(_pageSizeIndex, s_PageSizeLabels, EditorStyles.toolbarDropDown, GUILayout.Width(85));

            EditorGUILayout.EndHorizontal();
        }

        private List<SceneExplorerResultItem> GetCurrentPageItems()
        {
            if (_results == null || _results.Count == 0) return new List<SceneExplorerResultItem>();
            int pageSize = s_PageSizes[_pageSizeIndex];
            if (pageSize <= 0) return _results;

            int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)_results.Count / pageSize));
            _currentPage = Mathf.Clamp(_currentPage, 1, totalPages);
            int start = (_currentPage - 1) * pageSize;
            int count = Mathf.Min(pageSize, _results.Count - start);
            return _results.GetRange(start, count);
        }

        private void DrawResultsList()
        {
            if (_results.Count == 0)
            {
                DrawEmptyState();
                return;
            }

            var itemsToDraw = GetCurrentPageItems();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            switch (_viewMode)
            {
                case ExplorerViewMode.HierarchyTree:
                    DrawHierarchyTreeView(itemsToDraw);
                    break;
                case ExplorerViewMode.GroupedByTag:
                    DrawGroupedByTagView(itemsToDraw);
                    break;
                case ExplorerViewMode.CompactTable:
                    DrawCompactTableView(itemsToDraw);
                    break;
                case ExplorerViewMode.CardList:
                default:
                    DrawCardListView(itemsToDraw);
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        private class HierarchyTreeNode
        {
            public SceneExplorerResultItem Item;
            public Transform Transform;
            public List<HierarchyTreeNode> Children = new List<HierarchyTreeNode>();
        }

        private void DrawHierarchyTreeView(List<SceneExplorerResultItem> items)
        {
            if (items == null || items.Count == 0) return;

            // 1. Build lookup for all items in the current view
            var nodeLookup = new Dictionary<Transform, HierarchyTreeNode>();
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                if (it.GameObject == null) continue;
                var t = it.GameObject.transform;
                if (!nodeLookup.ContainsKey(t))
                {
                    nodeLookup[t] = new HierarchyTreeNode { Item = it, Transform = t };
                }
            }

            // 2. Build parent-child relationships using nearest ancestor in nodeLookup
            var rootNodes = new List<HierarchyTreeNode>();
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                if (it.GameObject == null) continue;
                var t = it.GameObject.transform;
                if (!nodeLookup.TryGetValue(t, out var node)) continue;

                // Find nearest ancestor in nodeLookup
                Transform parentInLookup = null;
                Transform curr = t.parent;
                while (curr != null)
                {
                    if (nodeLookup.ContainsKey(curr))
                    {
                        parentInLookup = curr;
                        break;
                    }
                    curr = curr.parent;
                }

                if (parentInLookup != null)
                {
                    nodeLookup[parentInLookup].Children.Add(node);
                }
                else
                {
                    rootNodes.Add(node);
                }
            }

            // 3. Render tree recursively starting from rootNodes
            int rowIndex = 0;
            for (int i = 0; i < rootNodes.Count; i++)
            {
                DrawHierarchyTreeNode(rootNodes[i], 0, ref rowIndex);
            }
        }

        private void DrawHierarchyTreeNode(HierarchyTreeNode node, int indentLevel, ref int rowIndex)
        {
            if (node == null || node.Item == null || node.Item.GameObject == null) return;

            bool hasChildren = node.Children.Count > 0;
            int instanceId = node.Transform.GetInstanceID();
            bool isCollapsed = _collapsedParentIds.Contains(instanceId);

            if (hasChildren)
            {
                var headerRect = EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

                if (indentLevel > 0)
                {
                    GUILayout.Space(indentLevel * 16f);
                }

                // Active dot indicator
                var dotRect = GUILayoutUtility.GetRect(8, 14, GUILayout.Width(8));
                dotRect.y += 3;
                dotRect.height = 8;
                EditorGUI.DrawRect(dotRect, node.Item.IsActive ? SceneSentryStyles.SuccessColor : Color.gray);
                GUILayout.Space(4);

                string countLabel = node.Children.Count == 1 ? "1 child" : $"{node.Children.Count} children";
                bool newExpanded = EditorGUILayout.Foldout(!isCollapsed, $"{node.Item.Name} ({countLabel})", true, EditorStyles.foldout);
                if (newExpanded == isCollapsed)
                {
                    if (newExpanded) _collapsedParentIds.Remove(instanceId);
                    else _collapsedParentIds.Add(instanceId);
                }

                // Render miniature tag chips if any
                if (node.Item.CustomTagIds.Count > 0 && _database != null)
                {
                    for (int t = 0; t < node.Item.CustomTagIds.Count && t < 3; t++)
                    {
                        var tagDef = _database.GetTagById(node.Item.CustomTagIds[t]);
                        if (tagDef != null)
                        {
                            var oldBg = GUI.backgroundColor;
                            GUI.backgroundColor = tagDef.Color;
                            GUILayout.Label(tagDef.Name, EditorStyles.miniButton, GUILayout.Height(14));
                            GUI.backgroundColor = oldBg;
                        }
                    }
                }

                GUILayout.FlexibleSpace();

                GUILayout.Label(node.Item.SceneName, EditorStyles.miniLabel, GUILayout.Width(85));

                if (GUILayout.Button("Ping", EditorStyles.toolbarButton, GUILayout.Width(45)))
                {
                    EditorGUIUtility.PingObject(node.Item.GameObject);
                }
                if (GUILayout.Button("Select", EditorStyles.toolbarButton, GUILayout.Width(50)))
                {
                    Selection.activeGameObject = node.Item.GameObject;
                    EditorGUIUtility.PingObject(node.Item.GameObject);
                }
                EditorGUILayout.EndHorizontal();

                // Double click to frame
                if (Event.current.type == EventType.MouseDown && headerRect.Contains(Event.current.mousePosition))
                {
                    if (Event.current.clickCount == 2)
                    {
                        Selection.activeGameObject = node.Item.GameObject;
                        EditorGUIUtility.PingObject(node.Item.GameObject);
                        if (SceneView.lastActiveSceneView != null)
                        {
                            SceneView.lastActiveSceneView.FrameSelected();
                        }
                        Event.current.Use();
                    }
                }

                if (!isCollapsed)
                {
                    for (int c = 0; c < node.Children.Count; c++)
                    {
                        DrawHierarchyTreeNode(node.Children[c], indentLevel + 1, ref rowIndex);
                    }
                }
            }
            else
            {
                DrawCompactResultRow(node.Item, indent: indentLevel * 16f, isEven: rowIndex++ % 2 == 0);
            }
        }

        private void DrawGroupedByTagView(List<SceneExplorerResultItem> items)
        {
            var tagGroups = new Dictionary<string, List<SceneExplorerResultItem>>();
            var untagged = new List<SceneExplorerResultItem>();

            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                if (it.CustomTagIds.Count == 0)
                {
                    untagged.Add(it);
                }
                else
                {
                    for (int t = 0; t < it.CustomTagIds.Count; t++)
                    {
                        string tagId = it.CustomTagIds[t];
                        if (!tagGroups.TryGetValue(tagId, out var list))
                        {
                            list = new List<SceneExplorerResultItem>();
                            tagGroups[tagId] = list;
                        }
                        list.Add(it);
                    }
                }
            }

            // Draw Tag Groups
            foreach (var kvp in tagGroups)
            {
                string tagId = kvp.Key;
                var groupItems = kvp.Value;
                var tagDef = _database != null ? _database.GetTagById(tagId) : null;
                string tagName = tagDef != null ? tagDef.Name : tagId;
                Color tagColor = tagDef != null ? tagDef.Color : Color.gray;

                bool isCollapsed = _collapsedTagGroups.Contains(tagId);

                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                GUILayout.Space(4);

                // Draw tag color swatch before the foldout so it doesn't overlap text
                var dotRect = GUILayoutUtility.GetRect(10, 16, GUILayout.Width(10));
                dotRect.x += 1;
                dotRect.y += 4;
                dotRect.width = 8;
                dotRect.height = 8;
                EditorGUI.DrawRect(dotRect, tagColor);
                GUILayout.Space(4);

                string foldoutLabel = $"{tagName}  ({groupItems.Count} objects)";
                bool newExpanded = EditorGUILayout.Foldout(!isCollapsed, foldoutLabel, true, EditorStyles.foldout);
                if (newExpanded == isCollapsed)
                {
                    if (newExpanded) _collapsedTagGroups.Remove(tagId);
                    else _collapsedTagGroups.Add(tagId);
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                if (!isCollapsed)
                {
                    for (int g = 0; g < groupItems.Count; g++)
                    {
                        DrawCompactResultRow(groupItems[g], indent: 16f, isEven: g % 2 == 0);
                    }
                }
            }

            // Draw Untagged Group
            if (untagged.Count > 0)
            {
                bool isCollapsed = _collapsedTagGroups.Contains("__untagged__");

                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                GUILayout.Space(4);
                bool newExpanded = EditorGUILayout.Foldout(!isCollapsed, $"Untagged Objects ({untagged.Count} objects)", true, EditorStyles.foldout);
                if (newExpanded == isCollapsed)
                {
                    if (newExpanded) _collapsedTagGroups.Remove("__untagged__");
                    else _collapsedTagGroups.Add("__untagged__");
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                if (!isCollapsed)
                {
                    for (int u = 0; u < untagged.Count; u++)
                    {
                        DrawCompactResultRow(untagged[u], indent: 16f, isEven: u % 2 == 0);
                    }
                }
            }
        }

        private void DrawCompactTableView(List<SceneExplorerResultItem> items)
        {
            // Table Header
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Space(14);
            EditorGUILayout.LabelField("Name", EditorStyles.miniBoldLabel, GUILayout.Width(170));
            EditorGUILayout.LabelField("Hierarchy Path", EditorStyles.miniBoldLabel, GUILayout.MinWidth(140));
            EditorGUILayout.LabelField("Tags", EditorStyles.miniBoldLabel, GUILayout.Width(140));
            EditorGUILayout.LabelField("Layer", EditorStyles.miniBoldLabel, GUILayout.Width(75));
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Actions", EditorStyles.miniBoldLabel, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item.GameObject == null) continue;
                DrawCompactResultRow(item, indent: 0f, isEven: i % 2 == 0);
            }
        }

        private void DrawCardListView(List<SceneExplorerResultItem> items)
        {
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item.GameObject == null) continue;
                DrawResultRow(item, i % 2 == 0);
            }
        }

        private void DrawCompactResultRow(SceneExplorerResultItem item, float indent = 0f, bool isEven = true)
        {
            if (item.GameObject == null) return;

            var rowRect = EditorGUILayout.BeginHorizontal(isEven ? EditorStyles.helpBox : EditorStyles.textArea, GUILayout.Height(22));

            if (indent > 0)
            {
                GUILayout.Space(indent);
            }

            // Active dot
            var activeRect = GUILayoutUtility.GetRect(8, 14, GUILayout.Width(8));
            activeRect.y += 4;
            activeRect.height = 7;
            EditorGUI.DrawRect(activeRect, item.IsActive ? SceneSentryStyles.SuccessColor : Color.gray);

            GUILayout.Space(4);

            // Name
            EditorGUILayout.LabelField(item.Name, EditorStyles.boldLabel, GUILayout.Width(170));

            // Path
            EditorGUILayout.LabelField(item.HierarchyPath, EditorStyles.miniLabel, GUILayout.MinWidth(140));

            // Tags
            EditorGUILayout.BeginHorizontal(GUILayout.Width(140));
            if (item.CustomTagIds.Count > 0 && _database != null)
            {
                int maxShow = Mathf.Min(2, item.CustomTagIds.Count);
                for (int t = 0; t < maxShow; t++)
                {
                    var tagDef = _database.GetTagById(item.CustomTagIds[t]);
                    if (tagDef != null)
                    {
                        SceneSentryStyles.DrawTagChip(tagDef.Name, tagDef.Color);
                    }
                }
                if (item.CustomTagIds.Count > 2)
                {
                    GUILayout.Label($"+{item.CustomTagIds.Count - 2}", EditorStyles.miniLabel, GUILayout.Width(20));
                }
            }
            EditorGUILayout.EndHorizontal();

            // Layer
            string layerName = LayerMask.LayerToName(item.Layer);
            if (string.IsNullOrEmpty(layerName)) layerName = $"L{item.Layer}";
            EditorGUILayout.LabelField(layerName, EditorStyles.miniLabel, GUILayout.Width(75));

            // Warnings badges
            if (item.HasMissingScript)
            {
                var badgeRect = GUILayoutUtility.GetRect(75, 16, GUILayout.Width(75));
                SceneSentryStyles.DrawBadge(badgeRect, "Missing Script", SceneSentryStyles.ErrorColor);
            }
            else if (item.HasMissingReference)
            {
                var badgeRect = GUILayoutUtility.GetRect(65, 16, GUILayout.Width(65));
                SceneSentryStyles.DrawBadge(badgeRect, "Missing Ref", SceneSentryStyles.WarningColor);
            }

            GUILayout.FlexibleSpace();

            // Action buttons
            if (GUILayout.Button("Ping", EditorStyles.miniButton, GUILayout.Width(42), GUILayout.Height(18)))
            {
                EditorGUIUtility.PingObject(item.GameObject);
            }

            if (GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(48), GUILayout.Height(18)))
            {
                Selection.activeGameObject = item.GameObject;
                EditorGUIUtility.PingObject(item.GameObject);
            }

            EditorGUILayout.EndHorizontal();

            // Double click to frame
            if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
            {
                if (Event.current.clickCount == 2)
                {
                    Selection.activeGameObject = item.GameObject;
                    EditorGUIUtility.PingObject(item.GameObject);
                    if (SceneView.lastActiveSceneView != null)
                    {
                        SceneView.lastActiveSceneView.FrameSelected();
                    }
                    Event.current.Use();
                }
            }
        }

        private void DrawResultRow(SceneExplorerResultItem item, bool isEven)
        {
            var rowRect = EditorGUILayout.BeginHorizontal(isEven ? EditorStyles.helpBox : EditorStyles.textArea);

            // Active indicator dot
            var activeRect = GUILayoutUtility.GetRect(8, 16, GUILayout.Width(8));
            activeRect.y += 4;
            activeRect.height = 8;
            EditorGUI.DrawRect(activeRect, item.IsActive ? SceneSentryStyles.SuccessColor : Color.gray);

            // GameObject Name & Path
            EditorGUILayout.BeginVertical(GUILayout.MinWidth(180));
            EditorGUILayout.LabelField(item.Name, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(item.HierarchyPath, EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            // Scene badge
            GUILayout.Label(item.SceneName, EditorStyles.miniLabel, GUILayout.Width(80));

            // Tag chips
            EditorGUILayout.BeginHorizontal(GUILayout.Width(140));
            if (item.CustomTagIds.Count > 0 && _database != null)
            {
                int maxShow = Mathf.Min(2, item.CustomTagIds.Count);
                for (int t = 0; t < maxShow; t++)
                {
                    var tagDef = _database.GetTagById(item.CustomTagIds[t]);
                    if (tagDef != null)
                    {
                        SceneSentryStyles.DrawTagChip(tagDef.Name, tagDef.Color);
                    }
                }
                if (item.CustomTagIds.Count > 2)
                {
                    GUILayout.Label($"+{item.CustomTagIds.Count - 2}", EditorStyles.miniLabel);
                }
            }
            EditorGUILayout.EndHorizontal();

            // Warnings badges (missing scripts / missing refs)
            if (item.HasMissingScript)
            {
                var badgeRect = GUILayoutUtility.GetRect(80, 18, GUILayout.Width(80));
                SceneSentryStyles.DrawBadge(badgeRect, "Missing Script", SceneSentryStyles.ErrorColor);
            }
            else if (item.HasMissingReference)
            {
                var badgeRect = GUILayoutUtility.GetRect(70, 18, GUILayout.Width(70));
                SceneSentryStyles.DrawBadge(badgeRect, "Missing Ref", SceneSentryStyles.WarningColor);
            }

            GUILayout.FlexibleSpace();

            // Action buttons
            if (GUILayout.Button("Ping", EditorStyles.miniButton, GUILayout.Width(45)))
            {
                EditorGUIUtility.PingObject(item.GameObject);
            }

            if (GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(50)))
            {
                Selection.activeGameObject = item.GameObject;
                EditorGUIUtility.PingObject(item.GameObject);
            }

            EditorGUILayout.EndHorizontal();

            // Handle row click
            if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
            {
                if (Event.current.clickCount == 2)
                {
                    Selection.activeGameObject = item.GameObject;
                    EditorGUIUtility.PingObject(item.GameObject);
                    if (SceneView.lastActiveSceneView != null)
                    {
                        SceneView.lastActiveSceneView.FrameSelected();
                    }
                    Event.current.Use();
                }
            }
        }

        private void CollapseAllHierarchyNodes()
        {
            _collapsedParentIds.Clear();
            for (int i = 0; i < _results.Count; i++)
            {
                var go = _results[i].GameObject;
                if (go != null && go.transform.parent != null)
                {
                    _collapsedParentIds.Add(go.transform.parent.GetInstanceID());
                }
                if (go != null && go.transform.childCount > 0)
                {
                    _collapsedParentIds.Add(go.transform.GetInstanceID());
                }
            }
        }

        private void CollapseAllTagGroups()
        {
            _collapsedTagGroups.Clear();
            if (_database != null)
            {
                for (int i = 0; i < _database.Tags.Count; i++)
                {
                    _collapsedTagGroups.Add(_database.Tags[i].Id);
                }
            }
            _collapsedTagGroups.Add("__untagged__");
        }

        private void DrawEmptyState()
        {
            EditorGUILayout.Space(30);
            EditorGUILayout.BeginVertical();
            GUILayout.FlexibleSpace();

            GUILayout.Label("No GameObjects Matched the Search Criteria", SceneSentryStyles.CenteredHeaderStyle);
            GUILayout.Label("Try clearing filters or searching with different terms.", SceneSentryStyles.CenteredSubtitleStyle);

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Reset All Filters", GUILayout.Width(140), GUILayout.Height(26)))
            {
                _filter.Reset();
                _needsRefresh = true;
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
        }
    }
}
