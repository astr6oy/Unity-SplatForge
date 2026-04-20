using System;
using UnityEngine;
using SplatForge.Network;

namespace SplatForge.Metadata
{
    /// <summary>
    /// Metadata for a 3DGS object in the scene
    /// </summary>
    [Serializable]
    public class ObjectMetadata
    {
        [SerializeField] private string _objectId;
        [SerializeField] private string _objectName;
        [SerializeField] private string _category;
        [SerializeField] private string[] _tags = Array.Empty<string>();
        [SerializeField] private string _sourcePrompt;
        [SerializeField] private Vector3 _localBoundsMin;
        [SerializeField] private Vector3 _localBoundsMax;
        [SerializeField] private long _createdAtTicks;
        [SerializeField] private string _notes;

        public string ObjectId
        {
            get => _objectId;
            set => _objectId = value;
        }

        public string ObjectName
        {
            get => _objectName;
            set => _objectName = value;
        }

        public string Category
        {
            get => _category;
            set => _category = value;
        }

        public string[] Tags
        {
            get => _tags;
            set => _tags = value ?? Array.Empty<string>();
        }

        public string SourcePrompt
        {
            get => _sourcePrompt;
            set => _sourcePrompt = value;
        }

        public Vector3 LocalBoundsMin
        {
            get => _localBoundsMin;
            set => _localBoundsMin = value;
        }

        public Vector3 LocalBoundsMax
        {
            get => _localBoundsMax;
            set => _localBoundsMax = value;
        }

        public Vector3 LocalBoundsSize => _localBoundsMax - _localBoundsMin;
        public Vector3 LocalBoundsCenter => (_localBoundsMin + _localBoundsMax) * 0.5f;

        public DateTime CreatedAt
        {
            get => new DateTime(_createdAtTicks, DateTimeKind.Utc);
            set => _createdAtTicks = value.Ticks;
        }

        public string Notes
        {
            get => _notes;
            set => _notes = value;
        }

        public ObjectMetadata()
        {
            _objectId = Guid.NewGuid().ToString("N").Substring(0, 8);
            _createdAtTicks = DateTime.UtcNow.Ticks;
        }

        public ObjectMetadata(ObjectMetadataData data)
        {
            _objectId = data.objectId;
            _objectName = data.objectName;
            _category = data.category;
            _tags = data.tags ?? Array.Empty<string>();
            _sourcePrompt = data.sourcePrompt;
            _localBoundsMin = data.boundsMin;
            _localBoundsMax = data.boundsMax;
            _createdAtTicks = data.createdAt.Ticks;
        }

        public ObjectMetadataData ToData()
        {
            return new ObjectMetadataData
            {
                objectId = _objectId,
                objectName = _objectName,
                category = _category,
                tags = _tags,
                boundsMin = _localBoundsMin,
                boundsMax = _localBoundsMax,
                sourcePrompt = _sourcePrompt,
                createdAt = CreatedAt
            };
        }

        public Bounds GetLocalBounds()
        {
            var bounds = new Bounds();
            bounds.SetMinMax(_localBoundsMin, _localBoundsMax);
            return bounds;
        }

        public Bounds GetWorldBounds(Transform transform)
        {
            var localBounds = GetLocalBounds();
            var worldCenter = transform.TransformPoint(localBounds.center);

            // Transform bounds considering rotation and scale
            var extents = localBounds.extents;
            var axisX = transform.right * extents.x;
            var axisY = transform.up * extents.y;
            var axisZ = transform.forward * extents.z;

            var worldExtents = new Vector3(
                Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x),
                Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y),
                Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z)
            );

            return new Bounds(worldCenter, worldExtents * 2f);
        }

        public bool HasTag(string tag)
        {
            if (_tags == null) return false;
            foreach (var t in _tags)
            {
                if (string.Equals(t, tag, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        public void AddTag(string tag)
        {
            if (HasTag(tag)) return;
            var newTags = new string[_tags.Length + 1];
            Array.Copy(_tags, newTags, _tags.Length);
            newTags[_tags.Length] = tag;
            _tags = newTags;
        }

        public void RemoveTag(string tag)
        {
            if (!HasTag(tag)) return;
            var newTags = new string[_tags.Length - 1];
            int index = 0;
            foreach (var t in _tags)
            {
                if (!string.Equals(t, tag, StringComparison.OrdinalIgnoreCase))
                    newTags[index++] = t;
            }
            _tags = newTags;
        }
    }
}
