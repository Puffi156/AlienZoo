using System.Collections.Generic;
using UnityEngine;
using AlienZoo.Player;

namespace AlienZoo.Level
{
    /// <summary>
    /// Alien Cornfield. A soft trigger volume that does NOT block movement but:
    ///   - slows the local player very slightly while pushing through,
    ///   - plays a rustle SFX on enter/exit, and
    ///   - blocks line-of-sight (registers with <see cref="FoliageRegistry"/> so AI perception
    ///     can treat the volume as a vision blocker; the dense meshes block the camera visually).
    /// Attach to a GameObject with a trigger Collider. Non-networked scene component.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class FoliageTrigger : MonoBehaviour
    {
        [Header("Slow")]
        [Range(0f, 1f)]
        [Tooltip("Gentle drag while pushing through (0.85 = 15% slower).")]
        [SerializeField] private float _speedMultiplier = 0.85f;

        [Header("Rustle")]
        [SerializeField] private AudioSource _rustleSource;
        [SerializeField] private AudioClip[] _rustleClips;

        [Header("Vision")]
        [SerializeField] private bool _blocksLineOfSight = true;

        private Collider _col;

        private void Reset()
        {
            var c = GetComponent<Collider>();
            if (c != null) c.isTrigger = true;
        }

        private void Awake() => _col = GetComponent<Collider>();
        private void OnEnable() { if (_blocksLineOfSight) FoliageRegistry.Register(this); }
        private void OnDisable() => FoliageRegistry.Unregister(this);

        public Bounds WorldBounds => _col != null ? _col.bounds : new Bounds(transform.position, Vector3.one);

        private void OnTriggerEnter(Collider other)
        {
            var pc = other.GetComponentInParent<PlayerController>();
            if (pc == null || !pc.IsOwner) return; // only affect the local player
            pc.SetSpeedModifier(this, _speedMultiplier);
            PlayRustle();
        }

        private void OnTriggerExit(Collider other)
        {
            var pc = other.GetComponentInParent<PlayerController>();
            if (pc == null || !pc.IsOwner) return;
            pc.ClearSpeedModifier(this);
            PlayRustle();
        }

        private void PlayRustle()
        {
            if (_rustleSource == null || _rustleClips == null || _rustleClips.Length == 0) return;
            _rustleSource.PlayOneShot(_rustleClips[Random.Range(0, _rustleClips.Length)]);
        }
    }

    /// <summary>
    /// Global registry of sight-blocking foliage. AI perception calls
    /// <see cref="BlocksLineOfSight"/> so creatures can't "see" players through the cornfield.
    /// </summary>
    public static class FoliageRegistry
    {
        private static readonly List<FoliageTrigger> _volumes = new();

        public static void Register(FoliageTrigger f) { if (!_volumes.Contains(f)) _volumes.Add(f); }
        public static void Unregister(FoliageTrigger f) => _volumes.Remove(f);

        /// <summary>Rough AABB test: true if the segment from→to passes through any foliage volume.</summary>
        public static bool BlocksLineOfSight(Vector3 from, Vector3 to)
        {
            Vector3 dir = to - from;
            float sqrLen = dir.sqrMagnitude;
            var ray = new Ray(from, dir);

            for (int i = 0; i < _volumes.Count; i++)
            {
                if (_volumes[i] == null) continue;
                if (_volumes[i].WorldBounds.IntersectRay(ray, out float dist) && dist * dist <= sqrLen)
                    return true;
            }
            return false;
        }
    }
}
