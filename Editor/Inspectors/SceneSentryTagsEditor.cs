using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SceneSentry.Editor
{
    /// <summary>
    /// Custom Inspector for the SceneSentryTags component.
    /// Provides tag chips with custom colors, quick tag creation, multi-object editing,
    /// and automated detection and correction of duplicate or invalid tag IDs.
    /// </summary>
    [CustomEditor(typeof(SceneSentryTags))]
    [CanEditMultipleObjects]
    public class SceneSentryTagsEditor : UnityEditor.Editor
    {
        private TagDatabase _database;
        private string _quickAddName = string.Empty;

        private void OnEnable()
        {
            _database = TagDatabaseManager.ActiveDatabase;
            TagDatabaseManager.OnDatabaseChanged += Repaint;
        }

        private void OnDisable()
        {
            TagDatabaseManager.OnDatabaseChanged -= Repaint;
        }

        public override void OnInspectorGUI()
        {
            _database = TagDatabaseManager.ActiveDatabase;

            EditorGUILayout.Space(2);
            DrawInspectorHeader();

            bool isMultiSelection = targets.Length > 1;

            if (isMultiSelection)
            {
                DrawMultiSelectionGUI();
            }
            else
            {
                var targetTags = (SceneSentryTags)target;
                DrawSingleSelectionGUI(targetTags);
            }

            DrawQuickAddSection();
        }

        private void DrawInspectorHeader()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Assigned SceneSentry Tags", EditorStyles.boldLabel);
            if (GUILayout.Button("Tag Manager", EditorStyles.miniButton, GUILayout.Width(90)))
            {
                TagManagerWindow.Open();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(2);
        }

        private void DrawSingleSelectionGUI(SceneSentryTags targetTags)
        {
            var tagIds = targetTags.TagIds;

            // Health check: duplicate tags
            int duplicateCount = GetDuplicateCount(tagIds);
            if (duplicateCount > 0)
            {
                EditorGUILayout.HelpBox($"Detected {duplicateCount} duplicate tag assignment(s).", MessageType.Warning);
                if (GUILayout.Button("Deduplicate Tags", EditorStyles.miniButton))
                {
                    Undo.RecordObject(targetTags, "Deduplicate SceneSentry Tags");
                    targetTags.RemoveDuplicates();
                    EditorUtility.SetDirty(targetTags);
                }
            }

            // Health check: invalid tag IDs
            int invalidCount = GetInvalidCount(tagIds);
            if (invalidCount > 0)
            {
                EditorGUILayout.HelpBox($"Found {invalidCount} tag ID(s) not in current TagDatabase.", MessageType.Warning);
                if (GUILayout.Button("Clean Invalid Tags", EditorStyles.miniButton))
                {
                    Undo.RecordObject(targetTags, "Clean Invalid SceneSentry Tags");
                    targetTags.CleanInvalidTags(_database);
                    EditorUtility.SetDirty(targetTags);
                }
            }

            // Tag chips
            if (tagIds.Count == 0)
            {
                EditorGUILayout.HelpBox("No tags assigned to this GameObject. Click '+ Add Tag' below to attach one.", MessageType.None);
            }
            else
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                // Draw chips in flex wrap rows
                EditorGUILayout.BeginHorizontal();
                float currentLineWidth = 0f;
                float maxWidth = EditorGUIUtility.currentViewWidth - 40f;

                for (int i = 0; i < tagIds.Count; i++)
                {
                    string id = tagIds[i];
                    var tagDef = _database != null ? _database.GetTagById(id) : null;

                    string displayName = tagDef != null ? tagDef.Name : $"<Unknown: {id.Substring(0, Math.Min(6, id.Length))}>";
                    Color tagColor = tagDef != null ? tagDef.Color : Color.gray;

                    // Calculate width
                    var size = SceneSentryStyles.ChipStyle.CalcSize(new GUIContent(displayName));
                    float chipWidth = size.x + 30f;

                    if (currentLineWidth + chipWidth > maxWidth && currentLineWidth > 0)
                    {
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                        currentLineWidth = 0f;
                    }

                    currentLineWidth += chipWidth;

                    int capturedIndex = i;
                    SceneSentryStyles.DrawTagChip(displayName, tagColor, () =>
                    {
                        Undo.RecordObject(targetTags, $"Remove Tag '{displayName}'");
                        targetTags.RemoveTag(id);
                        if (tagDef != null)
                        {
                            targetTags.RemoveTag(tagDef.Name);
                        }
                        EditorUtility.SetDirty(targetTags);
                        if (targetTags.gameObject.scene.isLoaded)
                        {
                            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(targetTags.gameObject.scene);
                        }
                        GUIUtility.ExitGUI();
                    }, () =>
                    {
                        if (tagDef != null)
                        {
                            TagManagerWindow.OpenAndSelect(tagDef.Id);
                        }
                    });
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(4);

            // Add Tag Button
            var addBtnRect = GUILayoutUtility.GetRect(new GUIContent("+ Add Tag..."), EditorStyles.miniButton);
            if (GUI.Button(addBtnRect, "+ Add Tag...", EditorStyles.miniButton))
            {
                var popup = new TagPickerPopup(_database, (selectedTag) =>
                {
                    Undo.RecordObject(targetTags, $"Add Tag '{selectedTag.Name}'");
                    targetTags.AddTag(selectedTag.Id);
                    EditorUtility.SetDirty(targetTags);
                }, targetTags.TagIds);

                PopupWindow.Show(addBtnRect, popup);
            }
        }

        private void DrawMultiSelectionGUI()
        {
            EditorGUILayout.HelpBox($"Editing {targets.Length} selected GameObjects simultaneously.", MessageType.Info);

            // Collect all unique tag IDs across selection
            var allTagIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var commonTagIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool first = true;

            for (int i = 0; i < targets.Length; i++)
            {
                var comp = (SceneSentryTags)targets[i];
                if (comp == null) continue;

                var currentIds = new HashSet<string>(comp.TagIds, StringComparer.OrdinalIgnoreCase);
                foreach (var id in currentIds) allTagIds.Add(id);

                if (first)
                {
                    commonTagIds = currentIds;
                    first = false;
                }
                else
                {
                    commonTagIds.IntersectWith(currentIds);
                }
            }

            EditorGUILayout.LabelField($"Common Tags ({commonTagIds.Count}):", EditorStyles.boldLabel);
            if (commonTagIds.Count == 0)
            {
                EditorGUILayout.HelpBox("Selected GameObjects share no common tags.", MessageType.None);
            }
            else
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();

                foreach (var id in commonTagIds)
                {
                    var tagDef = _database != null ? _database.GetTagById(id) : null;
                    string displayName = tagDef != null ? tagDef.Name : id;
                    Color color = tagDef != null ? tagDef.Color : Color.gray;

                    SceneSentryStyles.DrawTagChip(displayName, color, () =>
                    {
                        Undo.RecordObjects(targets, $"Remove Tag '{displayName}' from Selection");
                        for (int i = 0; i < targets.Length; i++)
                        {
                            var comp = (SceneSentryTags)targets[i];
                            comp.RemoveTag(id);
                            if (tagDef != null) comp.RemoveTag(tagDef.Name);
                            EditorUtility.SetDirty(comp);
                            if (comp.gameObject.scene.isLoaded)
                            {
                                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(comp.gameObject.scene);
                            }
                        }
                        GUIUtility.ExitGUI();
                    });
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(6);

            // Add Tag to All Button
            var addBtnRect = GUILayoutUtility.GetRect(new GUIContent("+ Add Tag to All Selected..."), EditorStyles.miniButton);
            if (GUI.Button(addBtnRect, "+ Add Tag to All Selected...", EditorStyles.miniButton))
            {
                var popup = new TagPickerPopup(_database, (selectedTag) =>
                {
                    Undo.RecordObjects(targets, $"Add Tag '{selectedTag.Name}' to Selection");
                    for (int i = 0; i < targets.Length; i++)
                    {
                        var comp = (SceneSentryTags)targets[i];
                        comp.AddTag(selectedTag.Id);
                        EditorUtility.SetDirty(comp);
                    }
                }, commonTagIds);

                PopupWindow.Show(addBtnRect, popup);
            }
        }

        private void DrawQuickAddSection()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Quick Tag Creator", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            _quickAddName = EditorGUILayout.TextField(_quickAddName, EditorStyles.textField);

            bool canCreate = !string.IsNullOrWhiteSpace(_quickAddName) &&
                             _database != null &&
                             !_database.ContainsName(_quickAddName.Trim());

            EditorGUI.BeginDisabledGroup(!canCreate);
            if (GUILayout.Button("Create & Assign", EditorStyles.miniButton, GUILayout.Width(110)))
            {
                string newTagName = _quickAddName.Trim();
                var createdTag = TagDatabaseManager.CreateTagWithUndo(newTagName);
                if (createdTag != null)
                {
                    Undo.RecordObjects(targets, $"Assign Newly Created Tag '{newTagName}'");
                    for (int i = 0; i < targets.Length; i++)
                    {
                        var comp = (SceneSentryTags)targets[i];
                        comp.AddTag(createdTag.Id);
                        EditorUtility.SetDirty(comp);
                    }
                    _quickAddName = string.Empty;
                    GUI.FocusControl(null);
                }
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrWhiteSpace(_quickAddName) && _database != null && _database.ContainsName(_quickAddName.Trim()))
            {
                EditorGUILayout.HelpBox($"A tag named '{_quickAddName.Trim()}' already exists.", MessageType.Info);
            }
        }

        private int GetDuplicateCount(IReadOnlyList<string> tagIds)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int dups = 0;
            for (int i = 0; i < tagIds.Count; i++)
            {
                if (!set.Add(tagIds[i])) dups++;
            }
            return dups;
        }

        private int GetInvalidCount(IReadOnlyList<string> tagIds)
        {
            if (_database == null) return 0;
            int invalid = 0;
            for (int i = 0; i < tagIds.Count; i++)
            {
                if (!_database.ContainsId(tagIds[i])) invalid++;
            }
            return invalid;
        }
    }
}
