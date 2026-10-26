using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SceneSentry.Editor
{
    /// <summary>
    /// Interactive search-as-you-type popup for browsing, selecting, and quickly creating tags.
    /// Used by SceneSentryTags inspector and Explorer filters.
    /// </summary>
    public class TagPickerPopup : PopupWindowContent
    {
        private readonly TagDatabase _database;
        private readonly Action<TagDefinition> _onTagSelected;
        private readonly HashSet<string> _excludedTagIds;
        private string _searchQuery = string.Empty;
        private Vector2 _scrollPos;

        public TagPickerPopup(TagDatabase database, Action<TagDefinition> onTagSelected, IEnumerable<string> excludedTagIds = null)
        {
            _database = database ?? TagDatabaseManager.ActiveDatabase;
            _onTagSelected = onTagSelected;
            _excludedTagIds = excludedTagIds != null ? new HashSet<string>(excludedTagIds, StringComparer.OrdinalIgnoreCase) : new HashSet<string>();
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(280, 320);
        }

        public override void OnGUI(Rect rect)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Search header
            EditorGUILayout.BeginHorizontal();
            GUI.SetNextControlName("TagPickerSearchField");
            _searchQuery = EditorGUILayout.TextField(_searchQuery, EditorStyles.toolbarSearchField);
            if (GUILayout.Button("X", EditorStyles.toolbarButton, GUILayout.Width(20)))
            {
                _searchQuery = string.Empty;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            if (_database != null)
            {
                var tags = _database.Tags;
                int matchesCount = 0;

                for (int i = 0; i < tags.Count; i++)
                {
                    var tag = tags[i];
                    if (tag == null) continue;

                    // Skip already assigned tags if excluded
                    if (_excludedTagIds.Contains(tag.Id)) continue;

                    // Match filter
                    if (!string.IsNullOrEmpty(_searchQuery))
                    {
                        if (tag.Name.IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) < 0 &&
                            tag.Description.IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            continue;
                        }
                    }

                    matchesCount++;

                    // Render selectable row
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                    // Color dot
                    var dotRect = GUILayoutUtility.GetRect(12, 16, GUILayout.Width(12));
                    dotRect.y += 2;
                    dotRect.height = 12;
                    EditorGUI.DrawRect(dotRect, tag.Color);

                    // Name and hierarchy path
                    string path = _database.GetHierarchyPath(tag.Id);
                    string label = string.IsNullOrEmpty(path) ? tag.Name : path;

                    if (GUILayout.Button(new GUIContent(label, tag.Description), EditorStyles.label))
                    {
                        _onTagSelected?.Invoke(tag);
                        editorWindow.Close();
                    }

                    EditorGUILayout.EndHorizontal();
                }

                if (matchesCount == 0)
                {
                    EditorGUILayout.HelpBox("No matching tags found.", MessageType.None);

                    // Offer quick creation if query is valid and doesn't exist
                    if (!string.IsNullOrWhiteSpace(_searchQuery) && !_database.ContainsName(_searchQuery.Trim()))
                    {
                        EditorGUILayout.Space(6);
                        if (GUILayout.Button($"+ Create Tag '{_searchQuery.Trim()}'", EditorStyles.miniButton))
                        {
                            var created = TagDatabaseManager.CreateTagWithUndo(_searchQuery.Trim());
                            if (created != null)
                            {
                                _onTagSelected?.Invoke(created);
                                editorWindow.Close();
                            }
                        }
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("TagDatabase not available.", MessageType.Warning);
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(4);
            if (GUILayout.Button("Manage All Tags...", EditorStyles.miniButton))
            {
                TagManagerWindow.Open();
                editorWindow.Close();
            }

            EditorGUILayout.EndVertical();
        }
    }
}
