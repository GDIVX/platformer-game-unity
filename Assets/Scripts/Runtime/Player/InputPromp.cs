using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace Runtime.Player
{
    /// <summary>
    /// Dynamically changes sprite to display the desired input prompt icon
    /// based on the current control scheme and selected input action.
    /// Works in-world (not UI).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class InputPrompt : MonoBehaviour
    {
        [Title("Input Settings")]
        [SerializeField, Required, Tooltip("Input Action Asset containing the maps.")]
        private InputActionAsset _inputActions;

        [SerializeField, ValueDropdown(nameof(GetActionMapNames)), Tooltip("The input action map to use.")]
        private string _actionMapName;

        [SerializeField, ValueDropdown(nameof(GetActionNames)), Tooltip("The input action to display.")]
        private string _actionName;

        [Title("Sprites")]
        [SerializeField, Tooltip("Sprites for each control scheme (e.g., Keyboard, Gamepad).")]
        private List<ControlSchemeSprite> _controlSchemeSprites = new();

        [Title("Debug")]
        [ShowInInspector, ReadOnly] private string _currentScheme;
        [ShowInInspector, ReadOnly] private SpriteRenderer _renderer;
        [ShowInInspector, ReadOnly] private InputAction _action;

        private PlayerInput _playerInput;

        [System.Serializable]
        public class ControlSchemeSprite
        {
            [HorizontalGroup("Row", 0.5f)] public string ControlScheme;
            [HorizontalGroup("Row", 0.5f)] public Sprite Sprite;
        }

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _playerInput = FindFirstObjectByType<PlayerInput>();

            if (_inputActions && !string.IsNullOrEmpty(_actionMapName) && !string.IsNullOrEmpty(_actionName))
            {
                var map = _inputActions.FindActionMap(_actionMapName);
                _action = map?.FindAction(_actionName);
            }
        }

        private void OnEnable()
        {
            if (_playerInput != null)
            {
                _playerInput.onControlsChanged += OnControlsChanged;
                UpdateSprite(_playerInput.currentControlScheme);
            }
        }

        private void OnDisable()
        {
            if (_playerInput != null)
                _playerInput.onControlsChanged -= OnControlsChanged;
        }

        private void OnControlsChanged(PlayerInput input)
        {
            UpdateSprite(input.currentControlScheme);
        }

        private void UpdateSprite(string scheme)
        {
            _currentScheme = scheme;

            var match = _controlSchemeSprites.Find(s => s.ControlScheme == scheme);
            if (match != null && match.Sprite != null)
            {
                _renderer.sprite = match.Sprite;
            }
            else
            {
                Debug.LogWarning($"[InputPrompt] No sprite found for scheme: {scheme}", this);
            }
        }

#if UNITY_EDITOR
        private IEnumerable<string> GetActionMapNames()
        {
            if (_inputActions == null) yield break;
            foreach (var map in _inputActions.actionMaps)
                yield return map.name;
        }

        private IEnumerable<string> GetActionNames()
        {
            if (_inputActions == null || string.IsNullOrEmpty(_actionMapName)) yield break;
            var map = _inputActions.FindActionMap(_actionMapName);
            if (map == null) yield break;

            foreach (var action in map.actions)
                yield return action.name;
        }
#endif
    }
}
