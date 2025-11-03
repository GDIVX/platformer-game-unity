using System;
using DG.Tweening;
using UnityEngine;

namespace Runtime.World
{
    /// <summary>
    /// Hide a sprite when the player enter it
    /// </summary>
    [ExecuteInEditMode]
    public class FacadeSprite : MonoBehaviour
    {
        [SerializeField, Range(0, 1)] private float _transparencyInEditor;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private bool _showOnPlayerExit = true;
        [SerializeField] private float _transitionTime = 0.5f;
        [SerializeField] private Ease _easeType = Ease.OutCubic;


        private void Update()
        {
#if UNITY_EDITOR
            if (Application.isPlaying) return;

            var color = _spriteRenderer.color;
            _spriteRenderer.color = new Color(color.r, color.g, color.b, _transparencyInEditor);
#endif
        }

        private void Start()
        {
            var color = _spriteRenderer.color;
            _spriteRenderer.color = new Color(color.r, color.g, color.b, 1);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                _spriteRenderer.DOFade(0f, _transitionTime).SetEase(_easeType);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!_showOnPlayerExit) return;
            if (other.CompareTag("Player"))
            {
                _spriteRenderer.DOFade(1, _transitionTime).SetEase(_easeType);
            }
        }
    }
}