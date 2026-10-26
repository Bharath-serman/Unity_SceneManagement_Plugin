using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SceneSentry.Editor
{
    /// <summary>
    /// Coordinates safe remediation actions for scene validation findings.
    /// Enforces non-destructive edits, explicit user confirmation, and full Unity Undo support.
    /// </summary>
    public static class SafeFixManager
    {
        public static SafeFixResult ApplySingleFix(ValidationFinding finding)
        {
            if (finding == null)
            {
                return SafeFixResult.Fail("Finding is null.");
            }

            if (!finding.IsFixable || finding.FixAction == null)
            {
                return SafeFixResult.Fail("This issue does not support an automated safe fix and must be addressed manually.");
            }

            try
            {
                var result = finding.FixAction.Invoke();
                if (result.Success && finding.TargetGameObject != null)
                {
                    MarkObjectSceneDirty(finding.TargetGameObject);
                }
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneSentry] Error executing fix for '{finding.Title}': {ex.Message}");
                return SafeFixResult.Fail($"Fix failed with exception: {ex.Message}");
            }
        }

        public static SafeFixResult ApplyBatchFixes(IReadOnlyList<ValidationFinding> findings)
        {
            if (findings == null || findings.Count == 0)
            {
                return SafeFixResult.Fail("No findings provided.");
            }

            var fixableFindings = new List<ValidationFinding>();
            for (int i = 0; i < findings.Count; i++)
            {
                if (findings[i].IsFixable && findings[i].FixAction != null)
                {
                    fixableFindings.Add(findings[i]);
                }
            }

            if (fixableFindings.Count == 0)
            {
                return SafeFixResult.Fail("None of the selected findings support automated fixes.");
            }

            // Preview Confirmation Dialog
            string dialogMessage = $"SceneSentry is about to apply {fixableFindings.Count} safe fix(es) across your loaded scene(s):\n\n" +
                                   $"- All changes will be recorded in the Unity Undo history.\n" +
                                   $"- Affected scenes will be marked dirty.\n\n" +
                                   $"Do you wish to proceed?";

            bool confirmed = EditorUtility.DisplayDialog(
                "Confirm Batch Safe Fixes",
                dialogMessage,
                "Apply Safe Fixes",
                "Cancel");

            if (!confirmed)
            {
                return SafeFixResult.Fail("Batch fix operation cancelled by user.");
            }

            int successCount = 0;
            int failCount = 0;
            var modifiedScenes = new HashSet<Scene>();

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("SceneSentry Batch Safe Fix");
            int undoGroup = Undo.GetCurrentGroup();

            for (int i = 0; i < fixableFindings.Count; i++)
            {
                var finding = fixableFindings[i];
                try
                {
                    var result = finding.FixAction.Invoke();
                    if (result.Success)
                    {
                        successCount++;
                        if (finding.TargetGameObject != null)
                        {
                            modifiedScenes.Add(finding.TargetGameObject.scene);
                        }
                    }
                    else
                    {
                        failCount++;
                    }
                }
                catch (Exception ex)
                {
                    failCount++;
                    Debug.LogError($"[SceneSentry] Failed fix on '{finding.Title}': {ex.Message}");
                }
            }

            Undo.CollapseUndoOperations(undoGroup);

            foreach (var scene in modifiedScenes)
            {
                if (scene.isLoaded)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                }
            }

            string outcomeMsg = $"Batch fixes completed: {successCount} succeeded, {failCount} failed.";
            return new SafeFixResult(successCount > 0, outcomeMsg, successCount);
        }

        private static void MarkObjectSceneDirty(GameObject go)
        {
            if (go != null && go.scene.isLoaded)
            {
                EditorSceneManager.MarkSceneDirty(go.scene);
            }
        }
    }
}
