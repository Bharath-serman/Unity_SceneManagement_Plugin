# SceneSentry

> **Find it. Check it. Fix it.**  
> *The complete scene organization and automated validation toolkit for Unity 6.*

[![Unity](https://img.shields.io/badge/Unity-6000.0+-black.svg?style=flat&logo=unity)](https://unity.com/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Release](https://img.shields.io/badge/version-1.0.0-green.svg)](CHANGELOG.md)

---

## Overview

In complex Unity projects, scenes quickly accumulate hundreds or thousands of GameObjects. Unity's built-in tag system permits only a single tag per object, making sophisticated categorization difficult. At the same time, missing scripts, broken serialized references, layer mismatches, and configuration oversights are among the most common causes of runtime bugs.

**SceneSentry** bridges scene organization and scene validation into one unified, production-ready Editor toolkit:

1. **Smart Tagging**: Assign multiple custom tags to any GameObject with colors, hierarchies, and stable identifiers.
2. **Scene Explorer**: Find GameObjects instantly using combinatorial filters (tags, components, names, layers, active states).
3. **Scene Analyzer**: Automatically detect common scene configuration mistakes before they become runtime bugs.
4. **Safe Fix System**: Non-destructive, single-click and batch remediation with full Unity Undo support.

---

## Key Features

- **Multi-Tagging System**:
  - Assign unlimited custom tags per GameObject without modifying Unity's native tag field.
  - Persistent, version-controlled `TagDatabase` ScriptableObject.
  - Hierarchical tag taxonomy with parent-child categories and circular relationship prevention.
  - Stable GUID identifiers so renaming tags never breaks existing references.
  - High-performance, O(1) runtime registry (`SceneSentryRegistry` and `SceneSentryAPI`).
- **Scene Explorer**:
  - Multi-scene searching with AND / OR combinatorial filtering.
  - Filter by Name, Custom Tags, Built-in Tag, LayerMask, Component Type, and Active State.
  - Quick toggles for **Missing Scripts** and **Missing References**.
  - Ping, select, and frame GameObjects directly from search results.
  - Cached search engine prevents UI lag and avoids rescanning on Editor repaints.
- **Scene Analyzer (Core Differentiating Feature)**:
  - 10 built-in validation rules covering missing scripts, broken references, duplicate tags, invalid tag references, tag-component requirements, naming conventions, layer conventions, and unique component IDs.
  - Three distinct severity levels: 🔴 **Error**, 🟡 **Warning**, and 🔵 **Info**.
  - Detailed finding reports including affected object, component, rule name, reason, and actionable guidance.
  - Configurable rule system persisted via `SceneSentryRuleConfig`.
  - Extensible: write custom validation rules in C# with automatic reflection discovery.
- **Safe Fix System**:
  - Remediate supported findings with a single click or in batch.
  - User confirmation preview dialog before applying batch changes.
  - Full Unity Undo integration (`Ctrl+Z`).
  - Never deletes GameObjects or makes destructive guesses.
- **Unified Dashboard**:
  - Accessible via `Window > SceneSentry`.
  - Overview cards, scene health metrics, one-click scan, and documentation links.

---

## Unity Compatibility

- **Target Engine**: Unity 6 (6000.0 or later).
- **Assembly Separation**: Strict separation between `SceneSentry.Runtime` and `SceneSentry.Editor`.
- **Dependencies**: None. Pure C# utilizing Unity's built-in engine and editor capabilities.

---

## Installation

### Via Unity Package Manager (Local Disk)
1. In Unity 6, open **Window > Package Manager**.
2. Click **+ > Add package from disk...**.
3. Select the `package.json` file inside the `SceneSentry` package folder.

### Via Git URL
1. In Unity, open **Window > Package Manager**.
2. Click **+ > Add package from git URL...**.
3. Enter your repository URL (e.g. `https://github.com/your-org/scenesentry.git`).

---

## Quick Start Guide

### 1. Opening the Dashboard
Select **Window > SceneSentry** from the top menu. The dashboard provides quick access to all toolkit windows.

### 2. Creating and Assigning Tags
1. Open **Window > SceneSentry > Tag Manager**.
2. Click **+ New Tag**, enter a name (e.g. `Interactive`), pick a color, and click **Create Tag**.
3. Select any GameObject in your scene and attach the **SceneSentry Tags** component (`Add Component > SceneSentry > SceneSentry Tags`).
4. Click **+ Add Tag...** and select `Interactive`.

### 3. Searching in Scene Explorer
1. Open **Window > SceneSentry > Scene Explorer**.
2. Type in the search box or open the **Filters** drawer to filter by custom tags, layers, or components.
3. Click **Select** or **Ping** on any result to inspect it immediately.

### 4. Running Scene Analysis & Safe Fixes
1. Open **Window > SceneSentry > Scene Analyzer**.
2. Click **Run Analysis** to scan loaded scenes.
3. Review detected findings categorized by severity.
4. Click **Fix** on individual findings, or click **Batch Fix Safe Issues** to apply fixes across the entire scene.
5. All fixes can be undone with standard `Ctrl+Z` / `Cmd+Z`.

---

## Practical Example: Interactive Door Workflow

Here is how SceneSentry eliminates configuration bugs in game production:

1. **Tag**: In Tag Manager, create tag `Interactive`.
2. **Rule**: In Scene Analyzer > Rule Configuration, add a rule:  
   `Tag 'Interactive' requires component 'Collider'`.
3. **Assignment**: Assign tag `Interactive` to your `Door_Wood` GameObject.
4. **Validation**: If a level designer forgets to add a Collider, Scene Analyzer immediately flags:
   > 🔴 **Missing 'Collider' on Tagged 'Door_Wood'**  
   > *GameObject is tagged 'Interactive' which requires component 'Collider'.*
5. **Remediation**: Click **Fix**. SceneSentry automatically adds the Collider component and marks the scene dirty.

---

## Runtime API Quick Reference

Gameplay scripts can query custom tags with zero GC allocations using `SceneSentryAPI`:

```csharp
using SceneSentry;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 3f))
            {
                // Check if hit object has the "Interactive" tag
                if (SceneSentryAPI.HasTag(hit.collider.gameObject, "Interactive"))
                {
                    Debug.Log($"Interacted with {hit.collider.name}!");
                }
            }
        }
    }
}
```

For full API documentation, see [Documentation~/ApiReference.md](Documentation~/ApiReference.md).

---

## Authoring Custom Validation Rules

Create a new script in an Editor folder and inherit from `SceneValidationRuleBase`:

```csharp
using System.Collections.Generic;
using SceneSentry.Editor;
using UnityEngine;

public class LightIntensityRule : SceneValidationRuleBase
{
    public override string RuleId => "custom.light-intensity";
    public override string RuleName => "Light Intensity Check";
    public override string Description => "Warns if a light has negative intensity.";
    public override ValidationCategory Category => ValidationCategory.Custom;
    public override ValidationSeverity DefaultSeverity => ValidationSeverity.Warning;

    public override IEnumerable<ValidationFinding> Analyze(SceneValidationContext context)
    {
        foreach (var go in context.AllGameObjects)
        {
            if (go.TryGetComponent<Light>(out var light) && light.intensity < 0f)
            {
                yield return CreateFinding(
                    title: $"Negative Light Intensity on '{go.name}'",
                    explanation: "Light intensity is less than zero.",
                    targetGameObject: go,
                    targetComponent: light,
                    reason: "Intensity < 0",
                    suggestedCorrection: "Set light intensity to 0 or higher.");
            }
        }
    }
}
```
SceneSentry will automatically discover and run your rule during analysis!

---

## Safe Package Removal

To remove SceneSentry from a project:
1. Open **Window > Package Manager** and click **Remove** on SceneSentry.
2. If you also want to remove generated tag database and rule configuration assets, delete `Assets/SceneSentryData/` from your project.

---

## License

This project is licensed under the [MIT License](LICENSE).
