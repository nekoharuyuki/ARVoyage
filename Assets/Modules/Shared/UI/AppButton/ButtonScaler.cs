using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Niantic.ARVoyage
{
    /// <summary>
    /// Component that scales button down on click.
    /// Currently hard-coded to assume an initial scale of 1.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ButtonScaler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        private enum Mode
        {
            // Scale down while the button is held down. Restore scale on release.
            ScaleWhileHeld,
            // Scale down when the button is clicked. Will remain at clickScale until the button is disabled and re-enabled.
            ScaleOnClick
        }

        [Tooltip("Scale to use when the button is scaled down.")]
        [SerializeField] private float targetScale = .95f;
        [Tooltip("How long does it take to run the scale?")]
        [SerializeField] private float scaleDuration = .15f;

        [Tooltip("Scale beahvior. See comments in enum")]
        [SerializeField] private Mode mode = Mode.ScaleWhileHeld;

        private Coroutine scaleRoutine;

        void Awake() { }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (mode == Mode.ScaleWhileHeld)
            {
                ScaleDown();
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (mode == Mode.ScaleWhileHeld)
            {
                ScaleUp();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (mode == Mode.ScaleOnClick)
            {
                if (scaleDuration <= 0)
                {
                    transform.localScale = new Vector3(targetScale, targetScale, targetScale);
                }
                else
                {
                    ScaleDown();
                }
            }
        }

        private void OnEnable()
        {
            transform.localScale = Vector3.one;
        }

        private void OnDisable() { }

        private void ScaleDown()
        {
            StopScaleRoutine();
            if (gameObject.activeInHierarchy)
            {
                scaleRoutine = StartCoroutine(ScaleRoutine(targetScale));
            }
        }

        private void ScaleUp()
        {
            StopScaleRoutine();
            if (gameObject.activeInHierarchy)
            {
                scaleRoutine = StartCoroutine(ScaleRoutine(1f));
            }
        }

        private void StopScaleRoutine()
        {
            if (scaleRoutine != null)
            {
                StopCoroutine(scaleRoutine);
                scaleRoutine = null;
            }
        }

        private IEnumerator ScaleRoutine(float targetScale)
        {
            float startScale = transform.localScale.x;
            float startTime = Time.time;

            while (Time.time - startTime < scaleDuration)
            {
                float percentComplete = Mathf.Clamp01((Time.time - startTime) / scaleDuration);
                float scale = Mathf.Lerp(startScale, targetScale, percentComplete);
                transform.localScale = new Vector3(scale, scale, scale);
                yield return null;
            }

            transform.localScale = new Vector3(targetScale, targetScale, targetScale);

            scaleRoutine = null;

        }
    }
}
