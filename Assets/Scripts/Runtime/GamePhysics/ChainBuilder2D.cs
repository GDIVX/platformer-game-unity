using System.Collections.Generic;
using UnityEngine;

namespace Runtime.GamePhysics
{
    /// <summary>
    /// Builds a modular 2D hinge chain starting from THIS object as the anchor.
    /// You can define the anchor attach point and the link attach point so the chain
    /// actually lines up with your sprites.
    /// </summary>
    [ExecuteAlways]
    public class ChainBuilder2D : MonoBehaviour
    {
        [Header("Chain Setup")]
        [Tooltip("Prefabs to instantiate in order and connect as chain elements.")]
        [SerializeField]
        private List<GameObject> _chainPrefabs = new List<GameObject>();

        [Tooltip("World-space offset from the anchor object where the first link should start.")] [SerializeField]
        private Vector2 _anchorAttachOffset = Vector2.zero;

        [Tooltip("Offset to apply from one link to the next (direction and spacing).")] [SerializeField]
        private Vector2 _linkStep = new Vector2(0f, -0.5f);

        [Tooltip("Local anchor on each link (the HingeJoint2D.anchor).")] [SerializeField]
        private Vector2 _linkLocalAnchor = Vector2.zero;

        [Tooltip("Automatically build at runtime.")] [SerializeField]
        private bool _buildOnStart = true;

        [Header("Gizmos")] [SerializeField] private Color _gizmoColor = new(1f, 0.7f, 0f, 0.75f);

        private readonly List<GameObject> _spawned = new();

        private void Start()
        {
            if (_buildOnStart && Application.isPlaying)
                BuildChain();
        }

        [ContextMenu("Build Chain")]
        public void BuildChain()
        {
            ClearChain();

            if (_chainPrefabs == null || _chainPrefabs.Count == 0)
            {
                Debug.LogWarning("ChainBuilder2D: no chain prefabs assigned.");
                return;
            }

            // this object is the anchor
            var anchorBody = EnsureRigidbody(gameObject);

            // starting point in world space
            Vector2 currentPos = (Vector2)transform.position + _anchorAttachOffset;
            Rigidbody2D prevBody = anchorBody;

            foreach (var prefab in _chainPrefabs)
            {
                currentPos += _linkStep;

                var instance = Instantiate(prefab, currentPos, Quaternion.identity, transform);
                _spawned.Add(instance);

                var body = EnsureRigidbody(instance);
                var joint = EnsureJoint(instance);

                // configure joint to connect to previous body
                joint.connectedBody = prevBody;
                joint.autoConfigureConnectedAnchor = _chainPrefabs.IndexOf(prefab) != 0;

                // where on the link it rotates
                joint.anchor = _linkLocalAnchor;


                prevBody = body;
            }
        }

        [ContextMenu("Clear Chain")]
        public void ClearChain()
        {
            for (int i = _spawned.Count - 1; i >= 0; i--)
            {
                if (_spawned[i] != null)
#if UNITY_EDITOR
                    DestroyImmediate(_spawned[i]);
#else
                    Destroy(_spawned[i]);
#endif
            }

            _spawned.Clear();
        }

        private static Rigidbody2D EnsureRigidbody(GameObject obj)
        {
            var rb = obj.GetComponent<Rigidbody2D>();
            return rb != null ? rb : obj.AddComponent<Rigidbody2D>();
        }

        private static HingeJoint2D EnsureJoint(GameObject obj)
        {
            var joint = obj.GetComponent<HingeJoint2D>();
            return joint != null ? joint : obj.AddComponent<HingeJoint2D>();
        }

        private void OnDrawGizmos()
        {
            if (_chainPrefabs == null || _chainPrefabs.Count == 0)
                return;

            Vector3 anchorPoint = transform.position + (Vector3)_anchorAttachOffset;
            Gizmos.color = _gizmoColor;
            Gizmos.DrawWireSphere(anchorPoint, GetPrefabRadius(gameObject));

            Vector3 current = anchorPoint;

            for (int i = 0; i < _chainPrefabs.Count; i++)
            {
                Vector3 next = current + (Vector3)_linkStep;
                Gizmos.DrawLine(current, next);

                float radius = GetPrefabRadius(_chainPrefabs[i]);
                Gizmos.DrawWireSphere(next, radius);

                current = next;
            }

            Gizmos.color = new Color(_gizmoColor.r, _gizmoColor.g, _gizmoColor.b, 0.2f);
            Vector3 total = (Vector3)(_linkStep * _chainPrefabs.Count);
            Vector3 center = anchorPoint + total * 0.5f;
            Gizmos.DrawWireCube(center,
                new Vector3(Mathf.Abs(total.x) + 0.1f,
                    Mathf.Abs(total.y) + 0.1f,
                    0f));
        }

        private static float GetPrefabRadius(GameObject prefab)
        {
            if (prefab == null)
                return 0.1f;

            // Try to get a sprite renderer directly on the prefab
            var sr = prefab.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                // Sprite.bounds are given in local units (not world), scale them properly
                Vector2 size = sr.sprite.bounds.size;
                Vector2 scaled = Vector2.Scale(size, prefab.transform.localScale);
                return Mathf.Max(scaled.x, scaled.y) * 0.5f;
            }

            // Try to get a collider2D shape
            var col = prefab.GetComponentInChildren<Collider2D>();
            if (col != null)
            {
                Bounds b = col.bounds;
                return Mathf.Max(b.extents.x, b.extents.y);
            }

            // No shape info? Return small default
            return 0.1f;
        }
    }
}