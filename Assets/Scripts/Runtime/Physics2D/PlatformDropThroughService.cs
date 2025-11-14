using System.Collections;
using UnityEngine;

namespace Runtime.Physics2D
{
    /// <summary>
    /// Provides deterministic drop-through handling for one-way platforms.
    /// </summary>
    public class PlatformDropThroughService
    {
        private readonly Collider2D[] _playerColliders;
        private readonly float _disableDuration;
        private readonly MonoBehaviour _defaultRunner;
        private readonly float _raycastDistance;

        private bool _isDropping;
        private Coroutine _restoreCoroutine;
        private Collider2D _activePlatformCollider;
        private MonoBehaviour _activeRunner;

        /// <summary>
        /// Creates a new drop-through service.
        /// </summary>
        /// <param name="playerColliders">All player colliders that should temporarily ignore the platform.</param>
        /// <param name="disableDuration">How long the collision should be disabled for.</param>
        /// <param name="coroutineRunner">Object used to run coroutines.</param>
        /// <param name="raycastDistance">Distance to check for a platform below the player.</param>
        public PlatformDropThroughService(Collider2D[] playerColliders, float disableDuration, MonoBehaviour coroutineRunner, float raycastDistance = 0.5f)
        {
            _playerColliders = playerColliders;
            _disableDuration = Mathf.Max(0f, disableDuration);
            _defaultRunner = coroutineRunner;
            _raycastDistance = Mathf.Max(0f, raycastDistance);
        }

        /// <summary>
        /// Attempts to initiate a drop-through by raycasting for a platform beneath the player.
        /// </summary>
        /// <param name="playerTransform">The player's transform to raycast from.</param>
        /// <param name="platformMask">Layer mask used when searching for platforms.</param>
        /// <param name="runner">Optional override for the coroutine runner.</param>
        /// <returns>True if a drop-through was started, otherwise false.</returns>
        public bool TryDrop(Transform playerTransform, LayerMask platformMask, MonoBehaviour runner)
        {
            if (_isDropping)
                return false;

            if (playerTransform == null)
                return false;

            var coroutineRunner = runner != null ? runner : _defaultRunner;
            if (coroutineRunner == null)
                return false;

            if (_playerColliders == null || _playerColliders.Length == 0)
                return false;

            RaycastHit2D hit = Physics2D.Raycast(playerTransform.position, Vector2.down, _raycastDistance, platformMask);
            if (!hit.collider)
                return false;

            StartDrop(hit.collider, coroutineRunner);
            return true;
        }

        private void StartDrop(Collider2D platformCollider, MonoBehaviour runner)
        {
            _isDropping = true;
            _activePlatformCollider = platformCollider;
            _activeRunner = runner;

            for (int i = 0; i < _playerColliders.Length; i++)
            {
                var playerCollider = _playerColliders[i];
                if (playerCollider == null)
                    continue;

                Physics2D.IgnoreCollision(playerCollider, platformCollider, true);
            }

            if (_restoreCoroutine != null)
            {
                _activeRunner?.StopCoroutine(_restoreCoroutine);
                _restoreCoroutine = null;
            }

            _restoreCoroutine = runner.StartCoroutine(RestoreCollisions(platformCollider, runner));
        }

        private IEnumerator RestoreCollisions(Collider2D platformCollider, MonoBehaviour runner)
        {
            yield return new WaitForSeconds(_disableDuration);

            for (int i = 0; i < _playerColliders.Length; i++)
            {
                var playerCollider = _playerColliders[i];
                if (playerCollider == null)
                    continue;

                Physics2D.IgnoreCollision(playerCollider, platformCollider, false);
            }

            _isDropping = false;
            _restoreCoroutine = null;
            if (_activePlatformCollider == platformCollider)
            {
                _activePlatformCollider = null;
            }
            if (_activeRunner == runner)
            {
                _activeRunner = null;
            }
        }

        /// <summary>
        /// Immediately restores collisions if a drop is currently active.
        /// </summary>
        public void CancelDrop()
        {
            if (!_isDropping || _activePlatformCollider == null)
            {
                return;
            }

            for (int i = 0; i < _playerColliders.Length; i++)
            {
                var playerCollider = _playerColliders[i];
                if (playerCollider == null)
                    continue;

                Physics2D.IgnoreCollision(playerCollider, _activePlatformCollider, false);
            }

            _isDropping = false;

            if (_restoreCoroutine != null)
            {
                _activeRunner?.StopCoroutine(_restoreCoroutine);
                _restoreCoroutine = null;
            }

            _activePlatformCollider = null;
            _activeRunner = null;
        }
    }
}
