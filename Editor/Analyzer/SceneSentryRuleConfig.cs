using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SceneSentry.Editor
{
    [Serializable]
    public class TagComponentRequirement
    {
        public string Id = Guid.NewGuid().ToString("N");
        public string TagId;
        public string TagName;
        public string RequiredComponentTypeName;
        public bool IsEnabled = true;
        public ValidationSeverity Severity = ValidationSeverity.Error;

        public TagComponentRequirement(string tagId, string tagName, string requiredComponentTypeName, ValidationSeverity severity = ValidationSeverity.Error)
        {
            TagId = tagId;
            TagName = tagName;
            RequiredComponentTypeName = requiredComponentTypeName;
            Severity = severity;
        }
    }

    [Serializable]
    public class ComponentTagRequirement
    {
        public string Id = Guid.NewGuid().ToString("N");
        public string ComponentTypeName;
        public string RequiredTagId;
        public string RequiredTagName;
        public bool IsEnabled = true;
        public ValidationSeverity Severity = ValidationSeverity.Warning;

        public ComponentTagRequirement(string componentTypeName, string requiredTagId, string requiredTagName, ValidationSeverity severity = ValidationSeverity.Warning)
        {
            ComponentTypeName = componentTypeName;
            RequiredTagId = requiredTagId;
            RequiredTagName = requiredTagName;
            Severity = severity;
        }
    }

    [Serializable]
    public class ComponentPairRequirement
    {
        public string Id = Guid.NewGuid().ToString("N");
        public string MainComponentTypeName;
        public string RequiredPairComponentTypeName;
        public bool IsEnabled = true;
        public ValidationSeverity Severity = ValidationSeverity.Warning;

        public ComponentPairRequirement(string mainType, string requiredPairType, ValidationSeverity severity = ValidationSeverity.Warning)
        {
            MainComponentTypeName = mainType;
            RequiredPairComponentTypeName = requiredPairType;
            Severity = severity;
        }
    }

    [Serializable]
    public class LayerRequirement
    {
        public string Id = Guid.NewGuid().ToString("N");
        public string TagId;
        public string TagName;
        public int ExpectedLayer;
        public bool IsEnabled = true;
        public ValidationSeverity Severity = ValidationSeverity.Warning;

        public LayerRequirement(string tagId, string tagName, int expectedLayer, ValidationSeverity severity = ValidationSeverity.Warning)
        {
            TagId = tagId;
            TagName = tagName;
            ExpectedLayer = expectedLayer;
            Severity = severity;
        }
    }

    [Serializable]
    public class RuleStateEntry
    {
        public string RuleId;
        public bool IsEnabled = true;
        public ValidationSeverity Severity;
    }

    /// <summary>
    /// Persistent configuration storing user-defined validation rules,
    /// enabling/disabling of rules, and custom component/tag requirements.
    /// </summary>
    [CreateAssetMenu(fileName = "SceneSentryRuleConfig", menuName = "SceneSentry/Rule Config", order = 101)]
    public class SceneSentryRuleConfig : ScriptableObject
    {
        [SerializeField] private List<RuleStateEntry> _ruleStates = new List<RuleStateEntry>();
        [SerializeField] private List<TagComponentRequirement> _tagRequiresComponents = new List<TagComponentRequirement>();
        [SerializeField] private List<ComponentTagRequirement> _componentRequiresTags = new List<ComponentTagRequirement>();
        [SerializeField] private List<ComponentPairRequirement> _requiredComponentPairs = new List<ComponentPairRequirement>();
        [SerializeField] private List<LayerRequirement> _layerRequirements = new List<LayerRequirement>();

        public bool CheckEmptyNames = true;
        public bool CheckSiblingDuplicateNames = true;

        public IReadOnlyList<TagComponentRequirement> TagRequiresComponents => _tagRequiresComponents;
        public IReadOnlyList<ComponentTagRequirement> ComponentRequiresTags => _componentRequiresTags;
        public IReadOnlyList<ComponentPairRequirement> RequiredComponentPairs => _requiredComponentPairs;
        public IReadOnlyList<LayerRequirement> LayerRequirements => _layerRequirements;

        public bool IsRuleEnabled(string ruleId, bool defaultState = true)
        {
            var entry = _ruleStates.Find(e => string.Equals(e.RuleId, ruleId, StringComparison.OrdinalIgnoreCase));
            return entry != null ? entry.IsEnabled : defaultState;
        }

        public void SetRuleEnabled(string ruleId, bool enabled)
        {
            var entry = _ruleStates.Find(e => string.Equals(e.RuleId, ruleId, StringComparison.OrdinalIgnoreCase));
            if (entry == null)
            {
                entry = new RuleStateEntry { RuleId = ruleId, IsEnabled = enabled };
                _ruleStates.Add(entry);
            }
            else
            {
                entry.IsEnabled = enabled;
            }
            EditorUtility.SetDirty(this);
        }

        public ValidationSeverity GetRuleSeverity(string ruleId, ValidationSeverity defaultSeverity)
        {
            var entry = _ruleStates.Find(e => string.Equals(e.RuleId, ruleId, StringComparison.OrdinalIgnoreCase));
            return entry != null ? entry.Severity : defaultSeverity;
        }

        public void SetRuleSeverity(string ruleId, ValidationSeverity severity)
        {
            var entry = _ruleStates.Find(e => string.Equals(e.RuleId, ruleId, StringComparison.OrdinalIgnoreCase));
            if (entry == null)
            {
                entry = new RuleStateEntry { RuleId = ruleId, Severity = severity, IsEnabled = true };
                _ruleStates.Add(entry);
            }
            else
            {
                entry.Severity = severity;
            }
            EditorUtility.SetDirty(this);
        }

        public void AddTagComponentRequirement(string tagId, string tagName, string componentTypeName)
        {
            _tagRequiresComponents.Add(new TagComponentRequirement(tagId, tagName, componentTypeName));
            EditorUtility.SetDirty(this);
        }

        public void RemoveTagComponentRequirementAt(int index)
        {
            if (index >= 0 && index < _tagRequiresComponents.Count)
            {
                _tagRequiresComponents.RemoveAt(index);
                EditorUtility.SetDirty(this);
            }
        }

        public void AddComponentTagRequirement(string componentTypeName, string requiredTagId, string requiredTagName)
        {
            _componentRequiresTags.Add(new ComponentTagRequirement(componentTypeName, requiredTagId, requiredTagName));
            EditorUtility.SetDirty(this);
        }

        public void RemoveComponentTagRequirementAt(int index)
        {
            if (index >= 0 && index < _componentRequiresTags.Count)
            {
                _componentRequiresTags.RemoveAt(index);
                EditorUtility.SetDirty(this);
            }
        }

        public void AddComponentPairRequirement(string mainComponent, string requiredPair)
        {
            _requiredComponentPairs.Add(new ComponentPairRequirement(mainComponent, requiredPair));
            EditorUtility.SetDirty(this);
        }

        public void RemoveComponentPairRequirementAt(int index)
        {
            if (index >= 0 && index < _requiredComponentPairs.Count)
            {
                _requiredComponentPairs.RemoveAt(index);
                EditorUtility.SetDirty(this);
            }
        }

        public void AddLayerRequirement(string tagId, string tagName, int layer)
        {
            _layerRequirements.Add(new LayerRequirement(tagId, tagName, layer));
            EditorUtility.SetDirty(this);
        }

        public void RemoveLayerRequirementAt(int index)
        {
            if (index >= 0 && index < _layerRequirements.Count)
            {
                _layerRequirements.RemoveAt(index);
                EditorUtility.SetDirty(this);
            }
        }
    }
}
