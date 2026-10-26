# SceneSentry User Guide

Welcome to **SceneSentry: Find it. Check it. Fix it.**

This guide covers everything you need to know to organize your scenes with custom multi-tagging, locate GameObjects with precision, and validate your scenes against configuration mistakes before they become runtime bugs.

---

## 1. The Dashboard (`Window > SceneSentry`)

The SceneSentry Dashboard serves as your mission control center:
- **Hero Overview**: Displays current toolkit status and quick links to all tools.
- **Scene Health**: Shows an immediate breakdown of issues detected during the latest scene scan (Errors, Warnings, Info) with a one-click button to rescan.
- **Project Metrics**: Displays the total count of custom tags in your project database and active tagged objects currently loaded.
- **Toolkit Navigation**: Direct access to **Scene Explorer**, **Scene Analyzer**, **Tag Manager**, and **Project Settings**.

---

## 2. Smart Tagging System

Unity's built-in tag system has a fundamental limitation: each GameObject can only have a single tag. SceneSentry solves this by allowing **multiple custom tags per GameObject** without touching or modifying Unity's built-in tag or layer fields.

### Creating and Managing Tags

Open the Tag Manager via:
**Window > SceneSentry > Tag Manager**

From this window, you can:
- **Create Tags**: Click `+ New Tag`, specify a unique name, optional description, display color, and optional parent category.
- **Tag Hierarchy**: Group tags hierarchically (e.g., `Environment / Foliage / Tree`). SceneSentry automatically prevents circular hierarchies (a tag cannot be parented to its own child or descendant).
- **Renaming Tags**: Changing a tag's name updates its display everywhere while preserving its internal stable GUID. References on GameObjects will never break when renaming tags.
- **Deleting Tags**: When deleting a tag, SceneSentry asks for confirmation and automatically reparents direct child tags to avoid broken references.

### Assigning Tags to GameObjects

There are three ways to assign tags to GameObjects:

1. **Custom Inspector**:
   - Select any GameObject and add the `SceneSentryTags` component (**Add Component > SceneSentry > SceneSentry Tags**).
   - Click `+ Add Tag...` to open the search popup.
   - Use the **Quick Tag Creator** text box at the bottom to type a new tag name and immediately assign it with one click.
   - Click the `×` on any tag chip to remove it.
2. **Hierarchy Context Menu**:
   - Right-click any GameObject in the Hierarchy window.
   - Select **SceneSentry > Add SceneSentry Tags**.
3. **Multi-Object Tagging**:
   - Select multiple GameObjects in the Hierarchy.
   - The Inspector displays tags common to all selected objects.
   - Click `+ Add Tag to All Selected...` to bulk-assign tags across your entire selection simultaneously.

### Visual Hierarchy Badges

When enabled in Project Settings (**Edit > Project Settings > SceneSentry**), SceneSentry renders small colored indicator dots on the right side of Hierarchy rows for tagged objects, allowing you to identify tagged actors at a glance.

---

## 3. Scene Explorer (`Window > SceneSentry > Scene Explorer`)

Scene Explorer provides instant searching and combinatorial filtering across all active and loaded scenes.

### Filter Criteria

- **Name / Path**: Fast search-as-you-type matching GameObject name or full hierarchy path.
- **Custom Tags**: Filter objects carrying one or more custom SceneSentry tags.
- **Built-in Tag & Layer**: Filter objects by Unity's native tag or layer mask.
- **Component Type**: Filter objects that contain specific component types (e.g. `Light`, `AudioSource`, `Camera`).
- **Active State**: Toggle between `All`, `Active Only`, or `Inactive Only`.
- **Special Quick Toggles**:
  - `Missing Scripts Only`: Immediately locates broken/deleted script components.
  - `Missing References Only`: Locates components where serialized object references were lost.

### Combinatorial Logic (AND / OR)

Toggle the logic dropdown between:
- **MatchAll (AND)**: The GameObject must satisfy all active search conditions.
- **MatchAny (OR)**: The GameObject matches if it satisfies any of the specified conditions.

### Navigating Results

- **Ping**: Highlights the GameObject in the Hierarchy.
- **Select**: Selects the GameObject in the Inspector.
- **Double-Click Row**: Selects the object and automatically frames the Scene View camera onto it (`F`).

---

## 4. Scene Analyzer (`Window > SceneSentry > Scene Analyzer`)

Scene Analyzer scans your active scenes and flags common configuration errors, missing components, broken references, and rule violations.

### Severity Levels

- 🔴 **Error**: High-confidence critical problem likely to cause runtime exceptions or gameplay failures (e.g., missing MonoBehaviour scripts, duplicate unique identifiers).
- 🟡 **Warning**: Suspicious configuration that may produce unexpected behavior (e.g., missing serialized object references, duplicate tags, layer mismatches).
- 🔵 **Info**: Helpful observation or naming suggestion (e.g., sibling GameObjects sharing identical names).

### Built-in Rules

1. **Missing Script Components**: Finds GameObjects with deleted or missing MonoBehaviour scripts.
2. **Missing Serialized References**: Inspects serialized fields for broken references where an object was expected.
3. **Duplicate Tag Assignments**: Identifies redundant identical tag IDs on the same object.
4. **Invalid Tag References**: Finds tag IDs on GameObjects that no longer exist in the TagDatabase.
5. **Tag Requires Component**: Enforces that tagged GameObjects contain mandatory components (e.g., Tag "Interactive" requires a `Collider`).
6. **Component Requires Tag**: Enforces that objects with specific components carry designated tags (e.g., `Light` requires "Lighting").
7. **Component Combinations**: Validates companion components (e.g., `Rigidbody` paired with a `Collider`).
8. **Naming Conventions**: Flags empty names and duplicate sibling names.
9. **Layer Conventions**: Ensures tagged GameObjects are placed on their expected layers.
10. **Unique Component Identifiers**: Enforces unique GUIDs across components implementing `IIdentifiableObject`.

---

## 5. Safe Fix System

SceneSentry prioritizes data safety:
- **Never Deletes GameObjects**: Automated fixes will never destroy GameObjects.
- **Never Guesses Missing References**: Ambiguous decisions require manual user assignment.
- **Full Undo Integration**: Every automated fix is registered in Unity's Undo stack (`Ctrl+Z` / `Cmd+Z`).
- **Confirmation Preview**: Batch fixes display a confirmation dialog showing how many items will be modified before applying changes.
- **Marks Scenes Dirty**: Modified scenes are marked dirty so you won't accidentally close the Editor without saving.

To apply a fix:
- Click the **Fix** button on any individual finding card in the Analyzer.
- Or click **Batch Fix Safe Issues** on the toolbar to apply all eligible fixes at once.

---

## 6. Practical Tutorial: Tagging an Interactive Door

Here is a practical example showing how SceneSentry prevents configuration bugs:

1. In **Tag Manager**, create a custom tag named `Interactive` with a green color.
2. In **Scene Analyzer > Rule Configuration**, add a new Tag-Component rule:
   - Tag: `Interactive`
   - Required Component: `BoxCollider`
3. In your scene, select a Door GameObject and assign the `Interactive` tag.
4. If you forgot to attach a `BoxCollider`, run **Scene Analyzer**.
5. Scene Analyzer immediately flags:
   `Missing 'BoxCollider' on Tagged 'Door'`
6. Click **Fix** right on the finding card. SceneSentry automatically attaches the `BoxCollider` and marks the scene dirty!
