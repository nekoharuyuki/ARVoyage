using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARVoyage
{
    /// <summary>
    /// "Safe area" refers to the subset of the screen usable for all supported device aspect ratios. 
    /// Placed alongside a canvas to properly set the scale of the safe area child transform.
    /// </summary>
    [RequireComponent(typeof(Canvas), typeof(CanvasScaler))]
    public class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform canvasTransform;
        private CanvasScaler canvasScaler;

        [SerializeField] private RectTransform safeAreaParentTransform;

        private Rect safeArea;
        private float canvasScaleX;
        private float canvasScaleY;

        // Waits until start to allow for canvas scaling
        private void Start()
        {
            canvasTransform = transform as RectTransform;
            canvasScaler = GetComponent<CanvasScaler>();

            UpdateSafeAreaSize();

#if UNITY_EDITOR
            // Poll for updates in editor
            StartCoroutine(EditorPollRoutine());
#endif
        }

        private void UpdateSafeAreaSize()
        {
            // Get the screen safe area and canvas scales
            safeArea = Screen.safeArea;
            canvasScaleX = canvasTransform.localScale.x;
            canvasScaleY = canvasTransform.localScale.y;

            // Get our reference resolution from the scaler
            Vector2 referenceResolution = canvasScaler.referenceResolution;

            // Scale the safe area to fit to the screen safe height, compensating for any canvas scaling
            float safeAreaX = safeArea.width / canvasScaleX;
            float safeAreaY = safeArea.height / canvasScaleY;

            // Determine the scale to match the y reference resolution. this will be applied to x and y to keep uniform scale
            float scaleToMatchReferenceResolutionY = safeAreaY / referenceResolution.y;
            safeAreaParentTransform.localScale = new Vector3(scaleToMatchReferenceResolutionY, scaleToMatchReferenceResolutionY, 1);

            // Determine the x value to use to compensate for this scaling
            float scaledSafeAreaX = safeAreaX / scaleToMatchReferenceResolutionY;

            // Set the size of the safe area parent
            safeAreaParentTransform.sizeDelta = new Vector2(scaledSafeAreaX, referenceResolution.y);

            Debug.LogFormat("UpdateSafeAreaSize [screen size {0}] [canvas size {1}] [canvas scale {2}] [total safe area {3}] [scaled safe area {4}]",
                Screen.width + " x " + Screen.height,
                canvasTransform.sizeDelta,
                canvasScaleX + ", " + canvasScaleY,
                safeArea.width + " x " + safeArea.height,
                safeAreaParentTransform.sizeDelta);
        }

#if UNITY_EDITOR
        // In editor, poll for new safe area size triggered by switching devices
        private IEnumerator EditorPollRoutine()
        {
            WaitForSeconds pollingWait = new WaitForSeconds(.5f);

            while (true)
            {
                if (Screen.safeArea != safeArea ||
                    canvasScaleX != canvasTransform.localScale.x ||
                    canvasScaleY != canvasTransform.localScale.y)
                {
                    UpdateSafeAreaSize();
                }
                yield return pollingWait;
            }
        }
#endif
    }
}
