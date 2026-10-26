using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SceneSentry.Editor
{
    /// <summary>
    /// Interactive Tag Manager window for managing the project's custom tag taxonomy.
    /// Supports hierarchical tag organization, color configuration, validation checks,
    /// and circular reference prevention.
    /// </summary>
    public class TagManagerWindow : EditorWindow
    {
        private TagDatabase _database;
        private string _selectedTagId;
        private string _searchFilter = string.Empty;

        // Group selection for batch operations
        private readonly HashSet<string> _selectedTagIdsForBatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _tagsToDeleteQueue = new List<string>();

        // Edit form fields
        private string _editName = string.Empty;
        private string _editDescription = string.Empty;
        private Color _editColor = Color.white;
        private string _editParentId = null;

        // New tag creation fields
        private bool _isCreatingNew = false;
        private string _newName = string.Empty;
        private string _newDescription = string.Empty;
        private Color _newColor = new Color(0.2f, 0.7f, 0.85f, 1f);
        private string _newParentId = null;

        private Vector2 _treeScrollPos;
        private Vector2 _formScrollPos;

        [MenuItem("Window/SceneSentry/Tag Manager", false, 12)]
        public static void Open()
        {
            var window = GetWindow<TagManagerWindow>("Tag Manager");
            window.minSize = new Vector2(580, 420);
            window.Show();
        }

        public static void OpenAndSelect(string tagId)
        {
            var window = GetWindow<TagManagerWindow>("Tag Manager");
            window.minSize = new Vector2(580, 420);
            window.SelectTag(tagId);
            window.Show();
        }

        private void OnEnable()
        {
            _database = TagDatabaseManager.ActiveDatabase;
            TagDatabaseManager.OnDatabaseChanged += HandleDatabaseChanged;

            if (_database != null && _database.Count > 0 && string.IsNullOrEmpty(_selectedTagId))
            {
                SelectTag(_database.Tags[0].Id);
            }
        }

        private void OnDisable()
        {
            TagDatabaseManager.OnDatabaseChanged -= HandleDatabaseChanged;
        }

        private void HandleDatabaseChanged()
        {
            _database = TagDatabaseManager.ActiveDatabase;
            if (!string.IsNullOrEmpty(_selectedTagId) && _database != null && !_database.ContainsId(_selectedTagId))
            {
                _selectedTagId = null;
            }
            if (_database != null)
            {
                _selectedTagIdsForBatch.RemoveWhere(id => !_database.ContainsId(id));
            }
            Repaint();
        }

        private void SelectTag(string id)
        {
            _selectedTagId = id;
            _isCreatingNew = false;

            if (_database != null)
            {
                var tag = _database.GetTagById(id);
                if (tag != null)
                {
                    _editName = tag.Name;
                    _editDescription = tag.Description;
                    _editColor = tag.Color;
                    _editParentId = tag.ParentId;
                }
            }
        }

        private void OnGUI()
        {
            _database = TagDatabaseManager.ActiveDatabase;

            DrawHeader();

            EditorGUILayout.BeginHorizontal();

            // Left column: Tag hierarchy tree & search
            DrawTagHierarchyColumn();

            // Right column: Tag editor / creator form
            DrawTagDetailsColumn();

            EditorGUILayout.EndHorizontal();

            // Defer deletion processing outside layout passes to avoid any GUILayout state mismatch
            ProcessPendingDeletions();
        }

        private void ProcessPendingDeletions()
        {
            if (_tagsToDeleteQueue.Count > 0)
            {
                for (int i = 0; i < _tagsToDeleteQueue.Count; i++)
                {
                    string id = _tagsToDeleteQueue[i];
                    TagDatabaseManager.DeleteTagWithUndo(id);
                    if (string.Equals(_selectedTagId, id, StringComparison.OrdinalIgnoreCase))
                    {
                        _selectedTagId = null;
                    }
                    _selectedTagIdsForBatch.Remove(id);
                }
                _tagsToDeleteQueue.Clear();
                Repaint();
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(SceneSentryStyles.CardStyle);
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical();
            GUILayout.Label("Tag Manager", SceneSentryStyles.HeaderTitleStyle);
            GUILayout.Label("Organize, color-code, and structure your project's custom tag taxonomy.", SceneSentryStyles.HeaderSubtitleStyle);
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("New Tag", GUILayout.Width(110), GUILayout.Height(28)))
            {
                _isCreatingNew = true;
                _selectedTagId = null;
                _newName = string.Empty;
                _newDescription = string.Empty;
                _newColor = new Color(UnityEngine.Random.Range(0.2f, 0.9f), UnityEngine.Random.Range(0.4f, 0.9f), UnityEngine.Random.Range(0.4f, 0.9f), 1f);
                _newParentId = null;
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawTagHierarchyColumn()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(280));

            // Search bar
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField);
            if (GUILayout.Button("X", EditorStyles.toolbarButton, GUILayout.Width(20)))
            {
                _searchFilter = string.Empty;
            }
            EditorGUILayout.EndHorizontal();

            // Group Selection Toolbar
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Select All", EditorStyles.toolbarButton, GUILayout.Width(65)))
            {
                if (_database != null)
                {
                    for (int i = 0; i < _database.Tags.Count; i++)
                    {
                        var t = _database.Tags[i];
                        if (string.IsNullOrWhiteSpace(_searchFilter) ||
                            t.Name.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            t.Description.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            _selectedTagIdsForBatch.Add(t.Id);
                        }
                    }
                }
            }

            if (GUILayout.Button("Deselect", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                _selectedTagIdsForBatch.Clear();
            }

            GUILayout.FlexibleSpace();

            if (_selectedTagIdsForBatch.Count > 0)
            {
                Color prevColor = GUI.backgroundColor;
                GUI.backgroundColor = SceneSentryStyles.ErrorColor;
                if (GUILayout.Button($"Delete ({_selectedTagIdsForBatch.Count})", EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    if (EditorUtility.DisplayDialog(
                        "Delete Selected Tags",
                        $"Are you sure you want to delete {_selectedTagIdsForBatch.Count} selected tag(s)?\n\nAny direct child tags will be safely reparented.",
                        "Delete Selected",
                        "Cancel"))
                    {
                        _tagsToDeleteQueue.AddRange(_selectedTagIdsForBatch);
                        _selectedTagIdsForBatch.Clear();
                    }
                }
                GUI.backgroundColor = prevColor;
            }
            EditorGUILayout.EndHorizontal();

            _treeScrollPos = EditorGUILayout.BeginScrollView(_treeScrollPos);

            if (_database != null)
            {
                if (string.IsNullOrWhiteSpace(_searchFilter))
                {
                    // Render hierarchy tree starting from roots
                    var roots = _database.GetRootTags();
                    if (roots.Count == 0 && _database.Count == 0)
                    {
                        EditorGUILayout.LabelField("No tags defined yet.", EditorStyles.miniLabel);
                    }
                    else
                    {
                        for (int i = 0; i < roots.Count; i++)
                        {
                            DrawTagNode(roots[i], 0);
                        }
                    }
                }
                else
                {
                    // Render flat filtered list
                    var tags = _database.Tags;
                    for (int i = 0; i < tags.Count; i++)
                    {
                        var t = tags[i];
                        if (t.Name.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            t.Description.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            DrawTagRow(t, isSelected: string.Equals(t.Id, _selectedTagId, StringComparison.OrdinalIgnoreCase));
                        }
                    }
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawTagNode(TagDefinition tag, int indentLevel)
        {
            if (tag == null) return;

            bool isSelected = string.Equals(tag.Id, _selectedTagId, StringComparison.OrdinalIgnoreCase);

            EditorGUILayout.BeginHorizontal();
            if (indentLevel > 0)
            {
                GUILayout.Space(indentLevel * 14f);
            }

            DrawTagRow(tag, isSelected);
            EditorGUILayout.EndHorizontal();

            // Render children
            var children = _database.GetChildren(tag.Id);
            for (int i = 0; i < children.Count; i++)
            {
                DrawTagNode(children[i], indentLevel + 1);
            }
        }

        private void DrawTagRow(TagDefinition tag, bool isSelected)
        {
            Color prevBg = GUI.backgroundColor;
            if (isSelected) GUI.backgroundColor = SceneSentryStyles.AccentColor;

            EditorGUILayout.BeginHorizontal(isSelected ? EditorStyles.helpBox : EditorStyles.textArea);

            // Group Selection Checkbox
            bool isBatchSelected = _selectedTagIdsForBatch.Contains(tag.Id);
            bool newBatchSelected = EditorGUILayout.Toggle(isBatchSelected, GUILayout.Width(18));
            if (newBatchSelected != isBatchSelected)
            {
                if (newBatchSelected) _selectedTagIdsForBatch.Add(tag.Id);
                else _selectedTagIdsForBatch.Remove(tag.Id);
            }

            // Color indicator
            var dotRect = GUILayoutUtility.GetRect(10, 14, GUILayout.Width(10));
            dotRect.y += 2;
            dotRect.height = 10;
            EditorGUI.DrawRect(dotRect, tag.Color);

            if (GUILayout.Button(tag.Name, EditorStyles.label))
            {
                SelectTag(tag.Id);
            }

            EditorGUILayout.EndHorizontal();
            GUI.backgroundColor = prevBg;
        }

        private void DrawTagDetailsColumn()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _formScrollPos = EditorGUILayout.BeginScrollView(_formScrollPos);

            if (_selectedTagIdsForBatch.Count > 1)
            {
                EditorGUILayout.BeginVertical(SceneSentryStyles.CardStyle);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{_selectedTagIdsForBatch.Count} tags selected for batch operations", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                Color prevBg = GUI.backgroundColor;
                GUI.backgroundColor = SceneSentryStyles.ErrorColor;
                if (GUILayout.Button($"Delete {_selectedTagIdsForBatch.Count} Tags", GUILayout.Width(130)))
                {
                    if (EditorUtility.DisplayDialog(
                        "Delete Selected Tags",
                        $"Are you sure you want to delete {_selectedTagIdsForBatch.Count} selected tag(s)?\n\nAny direct child tags will be safely reparented.",
                        "Delete Selected",
                        "Cancel"))
                    {
                        _tagsToDeleteQueue.AddRange(_selectedTagIdsForBatch);
                        _selectedTagIdsForBatch.Clear();
                    }
                }
                GUI.backgroundColor = prevBg;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(6);
            }

            if (_isCreatingNew)
            {
                DrawCreateTagForm();
            }
            else if (!string.IsNullOrEmpty(_selectedTagId) && _database != null)
            {
                var tag = _database.GetTagById(_selectedTagId);
                if (tag != null)
                {
                    DrawEditTagForm(tag);
                }
                else
                {
                    EditorGUILayout.HelpBox("Selected tag not found.", MessageType.Warning);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Select a tag from the left column or click '+ New Tag' to begin.", MessageType.Info);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawCreateTagForm()
        {
            EditorGUILayout.LabelField("Create New Custom Tag", SceneSentryStyles.HeaderTitleStyle);
            EditorGUILayout.Space(6);

            _newName = EditorGUILayout.TextField("Tag Name:", _newName);
            _newDescription = EditorGUILayout.TextField("Description:", _newDescription);
            _newColor = EditorGUILayout.ColorField("Display Color:", _newColor);

            _newParentId = DrawParentSelector("Parent Category:", _newParentId, null);

            EditorGUILayout.Space(12);

            bool canCreate = !string.IsNullOrWhiteSpace(_newName) &&
                             _database != null &&
                             !_database.ContainsName(_newName.Trim());

            EditorGUI.BeginDisabledGroup(!canCreate);
            if (GUILayout.Button("Create Tag", GUILayout.Height(30)))
            {
                var created = TagDatabaseManager.CreateTagWithUndo(_newName.Trim(), _newDescription, _newColor, _newParentId);
                if (created != null)
                {
                    SelectTag(created.Id);
                }
            }
            EditorGUI.EndDisabledGroup();

            if (!string.IsNullOrWhiteSpace(_newName) && _database != null && _database.ContainsName(_newName.Trim()))
            {
                EditorGUILayout.HelpBox($"A tag named '{_newName.Trim()}' already exists.", MessageType.Error);
            }
        }

        private void DrawEditTagForm(TagDefinition tag)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Edit Tag: {tag.Name}", SceneSentryStyles.HeaderTitleStyle);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Delete Tag", EditorStyles.miniButton, GUILayout.Width(90)))
            {
                bool confirm = EditorUtility.DisplayDialog(
                    "Delete Tag",
                    $"Are you sure you want to delete tag '{tag.Name}'?\n\nAny direct children will be safely reparented.",
                    "Delete",
                    "Cancel");

                if (confirm)
                {
                    _tagsToDeleteQueue.Add(tag.Id);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField($"Stable ID: {tag.Id}", EditorStyles.miniLabel);

            EditorGUILayout.Space(6);
            _editName = EditorGUILayout.TextField("Tag Name:", _editName);
            _editDescription = EditorGUILayout.TextField("Description:", _editDescription);
            _editColor = EditorGUILayout.ColorField("Display Color:", _editColor);

            _editParentId = DrawParentSelector("Parent Category:", _editParentId, tag.Id);

            EditorGUILayout.Space(12);

            bool isNameUnique = string.Equals(_editName.Trim(), tag.Name, StringComparison.OrdinalIgnoreCase) ||
                                !_database.ContainsName(_editName.Trim());

            bool canSave = !string.IsNullOrWhiteSpace(_editName) && isNameUnique;

            EditorGUI.BeginDisabledGroup(!canSave);
            if (GUILayout.Button("Save Changes", GUILayout.Height(30)))
            {
                TagDatabaseManager.UpdateTagWithUndo(tag.Id, _editName.Trim(), _editDescription, _editColor, _editParentId);
            }
            EditorGUI.EndDisabledGroup();

            if (!isNameUnique)
            {
                EditorGUILayout.HelpBox($"A tag named '{_editName.Trim()}' already exists.", MessageType.Error);
            }
        }

        private string DrawParentSelector(string label, string currentParentId, string currentTagId)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(label);

            string currentParentName = "None (Root)";
            if (!string.IsNullOrEmpty(currentParentId) && _database != null)
            {
                var p = _database.GetTagById(currentParentId);
                if (p != null) currentParentName = p.Name;
            }

            if (GUILayout.Button(currentParentName, EditorStyles.popup))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("None (Root)"), string.IsNullOrEmpty(currentParentId), () =>
                {
                    if (currentTagId != null) _editParentId = null;
                    else _newParentId = null;
                });

                if (_database != null)
                {
                    var allTags = _database.Tags;
                    for (int i = 0; i < allTags.Count; i++)
                    {
                        var candidate = allTags[i];
                        if (candidate == null) continue;

                        // Circular prevention: cannot parent under itself or its descendants
                        if (!string.IsNullOrEmpty(currentTagId))
                        {
                            if (string.Equals(candidate.Id, currentTagId, StringComparison.OrdinalIgnoreCase) ||
                                _database.IsDescendantOf(candidate.Id, currentTagId))
                            {
                                continue;
                            }
                        }

                        string menuPath = _database.GetHierarchyPath(candidate.Id, "/");
                        bool isCurrent = string.Equals(candidate.Id, currentParentId, StringComparison.OrdinalIgnoreCase);

                        menu.AddItem(new GUIContent(menuPath), isCurrent, () =>
                        {
                            if (currentTagId != null) _editParentId = candidate.Id;
                            else _newParentId = candidate.Id;
                        });
                    }
                }

                menu.ShowAsContext();
            }

            EditorGUILayout.EndHorizontal();
            return currentTagId != null ? _editParentId : _newParentId;
        }
    }
}
