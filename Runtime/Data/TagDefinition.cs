using System;
using UnityEngine;

namespace SceneSentry
{
    /// <summary>
    /// Represents a custom tag definition in SceneSentry.
    /// Each tag has a persistent, unique GUID identifier, allowing tags to be renamed
    /// without breaking existing scene assignments.
    /// </summary>
    [Serializable]
    public class TagDefinition : IEquatable<TagDefinition>
    {
        [SerializeField] private string _id;
        [SerializeField] private string _name;
        [SerializeField] private string _description;
        [SerializeField] private Color _color;
        [SerializeField] private string _parentId;

        public string Id => _id;

        public string Name
        {
            get => _name;
            set => _name = value?.Trim() ?? string.Empty;
        }

        public string Description
        {
            get => _description;
            set => _description = value ?? string.Empty;
        }

        public Color Color
        {
            get => _color;
            set => _color = value;
        }

        public string ParentId
        {
            get => _parentId;
            set => _parentId = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        public bool HasParent => !string.IsNullOrEmpty(_parentId);

        public TagDefinition()
        {
            _id = Guid.NewGuid().ToString("N");
            _name = "New Tag";
            _description = string.Empty;
            _color = GetDefaultColor();
            _parentId = null;
        }

        public TagDefinition(string id, string name, string description = "", Color? color = null, string parentId = null)
        {
            _id = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString("N") : id;
            _name = name?.Trim() ?? string.Empty;
            _description = description ?? string.Empty;
            _color = color ?? GetDefaultColor();
            _parentId = string.IsNullOrWhiteSpace(parentId) ? null : parentId.Trim();
        }

        public static TagDefinition Create(string name, string description = "", Color? color = null, string parentId = null)
        {
            return new TagDefinition(Guid.NewGuid().ToString("N"), name, description, color, parentId);
        }

        private static Color GetDefaultColor()
        {
            // Pleasant teal default accent
            return new Color(0.2f, 0.7f, 0.85f, 1f);
        }

        public TagDefinition Clone()
        {
            return new TagDefinition(_id, _name, _description, _color, _parentId);
        }

        public bool Equals(TagDefinition other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(_id, other._id, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            return obj is TagDefinition other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _id != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(_id) : 0;
        }

        public override string ToString()
        {
            return $"TagDefinition({_name}, ID: {_id})";
        }
    }
}
