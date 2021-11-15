using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Niantic.ARVoyage
{
    /// <summary>
    /// Utility methods for demo scenes, including:
    ///  Fading in and out objects/GUIs, either via "bubble"-style scaling or alpha.
    ///  Various geometry and trigonometry methods.
    /// </summary>
    public static class DemoUtil
    {
        public const bool bubbleScaleGUIs = true;
        public const float defaultFadeDuration = 0.5f;
        public static float minUIDisplayDuration = 2f;

        public static IEnumerator FadeInGUI(GameObject gui,
                                            Fader fader,
                                            float fadeDuration = defaultFadeDuration,
                                            float initialDelay = 0f)
        {
            if (DemoUtil.bubbleScaleGUIs)
            {
                gui.transform.localScale = Vector3.zero;
                yield return DemoUtil.DisplayWithBubbleScaleRoutine(gui, targetScale: 1f, show: true,
                    animationType: BubbleScaleAnimationType.Quick, initialDelay: initialDelay);
            }
            else
            {
#pragma warning disable 0162
                gui.SetActive(true);
                CanvasGroup guiCanvasGroup = gui.GetComponent<CanvasGroup>();
                guiCanvasGroup.alpha = 0f;
                fader.Fade(guiCanvasGroup, alpha: 1f, duration: fadeDuration, initialDelay: initialDelay);
#pragma warning restore 0162
            }
        }


        public static IEnumerator FadeOutGUI(GameObject gui,
                                                Fader fader,
                                                float fadeDuration = defaultFadeDuration)
        {
            if (DemoUtil.bubbleScaleGUIs)
            {
                yield return DemoUtil.DisplayWithBubbleScaleRoutine(gui, targetScale: 0f, show: false,
                    animationType: BubbleScaleAnimationType.Quick, postDelay: 0.383f);
            }
            else
            {
#pragma warning disable 0162
                CanvasGroup guiCanvasGroup = gui.GetComponent<CanvasGroup>();
                yield return fader.Fade(guiCanvasGroup, alpha: 0f, duration: fadeDuration);
#pragma warning restore 0162
            }
        }


        public static IEnumerator DisplayWithBubbleScaleRoutine(GameObject gameObj, bool show,
                                                    float targetScale = 1f,
                                                    BubbleScaleAnimationType animationType = BubbleScaleAnimationType.Default,
                                                    float initialDelay = 0f,
                                                    float postDelay = 0f,
                                                    Action onComplete = null)
        {
            yield return DisplayWithBubbleScale(gameObj, show, targetScale, animationType, initialDelay, postDelay, onComplete);
        }

        public static Coroutine DisplayWithBubbleScale(GameObject gameObj, bool show,
                                                    float targetScale = 1f,
                                                    BubbleScaleAnimationType animationType = BubbleScaleAnimationType.Default,
                                                    float preWait = 0,
                                                    float postWait = 0,
                                                    Action onComplete = null)
        {
            if (show)
            {
                gameObj.transform.localScale = Vector3.zero;
                return BubbleScaleUtil.ScaleUp(gameObj,
                                        targetScale: targetScale,
                                        animationType: animationType,
                                        preWait: preWait,
                                        postWait: postWait,
                                        onComplete: onComplete,
                                        activateTargetOnStart: true);
            }

            else
            {
                return BubbleScaleUtil.ScaleDown(gameObj,
                                            animationType: animationType,
                                            preWait: preWait,
                                            postWait: postWait,
                                            onComplete: onComplete,
                                            deactivateTargetOnComplete: true);
            }
        }


        public static void FaceNPCToPlayer(GameObject npc)
        {
            float angle = GetAngleBetweenPoints(npc.gameObject.transform.position, Camera.main.transform.position);
            angle = NormalizeAngle(angle);
            npc.gameObject.transform.eulerAngles = new Vector3(0f, angle, 0f);
        }

        public static float GetXZDistance(Vector3 v1, Vector3 v2)
        {
            return Vector2.Distance(new Vector2(v1.x, v1.z), new Vector2(v2.x, v2.z));
        }

        public static float GetAngleBetweenPoints(Vector3 pt1, Vector3 pt2)
        {
            Vector3 diff = pt1 - pt2;
            float angle = -((Mathf.Atan2(diff.z, diff.x) * 360f / (2f * Mathf.PI)) + 90f);
            return angle;
        }

        public static float NormalizeAngle(float angle)
        {
            angle %= 360f;
            if (angle < 0f) angle += 360f;
            return angle;
        }

        public static float NormalizeAngleNeg180To180(float angle)
        {
            angle %= 360f;
            if (angle > 180f) angle -= 360f;
            if (angle < -180f) angle += 360f;
            return angle;
        }


        public static List<List<Vector3>> CalculateARPlanesGridElements(List<ARPlane> arPlanes,
                                                                        float planeGridElementSize)
        {
            List<List<Vector3>> arPlanesGridElements = new List<List<Vector3>>();

            foreach (ARPlane arPlane in arPlanes)
            {
                List<Vector3> gridElementPts = new List<Vector3>();

                Vector3 pos = arPlane.gameObject.transform.position;
                Vector3 size = arPlane.gameObject.transform.localScale;

                // if the plane isn't subdividable
                if (size.x < planeGridElementSize * 2f &&
                    size.z < planeGridElementSize * 2f)
                {
                    gridElementPts.Add(pos);
                }

                else
                {
                    int numXGridElements = (int)(size.x / planeGridElementSize);
                    int numZGridElements = (int)(size.z / planeGridElementSize);

                    for (int xCtr = 0; xCtr < numXGridElements; xCtr++)
                    {
                        for (int zCtr = 0; zCtr < numZGridElements; zCtr++)
                        {
                            Vector3 gridElementPt = new Vector3(
                                ((float)xCtr / (float)numXGridElements) - 0.5f + (0.5f / numXGridElements),
                                0f,
                                ((float)zCtr / (float)numZGridElements) - 0.5f + (0.5f / numZGridElements)
                            );

                            Vector3 orientedGridElementPt = arPlane.gameObject.transform.TransformPoint(gridElementPt);
                            gridElementPts.Add(orientedGridElementPt);
                        }
                    }
                }

                arPlanesGridElements.Add(gridElementPts);
            }

            return arPlanesGridElements;
        }


        // XZ plane (2D frustrum) version of FindCameraVisibleGridElements
        //  Needed because players will often point the camera upwards,
        //  and not literally see ground grid elements in front of them
        private const float inFrontAngleRange = 25f;
        public static List<Vector3> FindInFrontGridElements(List<List<Vector3>> arPlanesGridElements, Transform fromTransform)
        {
            List<Vector3> inFrontGridElements = new List<Vector3>();

            float fromYRotation = NormalizeAngleNeg180To180(fromTransform.eulerAngles.y);

            foreach (List<Vector3> arPlaneGridElements in arPlanesGridElements)
            {
                foreach (Vector3 arPlaneGridElement in arPlaneGridElements)
                {
                    Vector3 fromPosition = fromTransform.position;
                    Vector3 gridPosition = arPlaneGridElement;
                    gridPosition.y = 0;

                    float angleToGridPos = GetAngleBetweenPoints(fromTransform.position, arPlaneGridElement);
                    float angleDiff = NormalizeAngleNeg180To180(angleToGridPos - fromYRotation);

                    if (Mathf.Abs(angleDiff) < inFrontAngleRange)
                    {
                        inFrontGridElements.Add(arPlaneGridElement);
                    }
                }
            }

            return inFrontGridElements;
        }


        public static List<Vector3> FindCameraVisibleGridElements(List<List<Vector3>> arPlanesGridElements, Transform playerTransform)
        {
            List<Vector3> visibleGridElements = new List<Vector3>();

            // in editor, are we a MOCK "other player"?
            bool mockOtherPlayer = false;
            if (Application.isEditor)
            {
                // proxy determination for if we are a mock other player: if we're not at the camera
                Vector3 cameraPosition = Camera.main.transform.position;
                mockOtherPlayer = GetXZDistance(cameraPosition, playerTransform.position) > 0.1f;
            }

            // normal case (on device with no mock players)
            if (!mockOtherPlayer)
            {
                Plane[] cameraFrustrumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
                foreach (List<Vector3> arPlaneGridElements in arPlanesGridElements)
                {
                    visibleGridElements.AddRange(FindVisibleGivenPointsBetweenFrustrumPlanes(arPlaneGridElements, cameraFrustrumPlanes));
                }
            }

            // IN EDITOR with mock players 
            // Since mock other players AREN'T at the camera, just do a nearby radius check 
            // (even though this means elements can be chosen behind a mock player)
            else
            {
                foreach (List<Vector3> arPlaneGridElements in arPlanesGridElements)
                {
                    foreach (Vector3 arPlaneGridElement in arPlaneGridElements)
                    {
                        if (GetXZDistance(arPlaneGridElement, playerTransform.position) < 4f)
                        {
                            visibleGridElements.Add(arPlaneGridElement);
                        }
                    }
                }
            }

            return visibleGridElements;
        }


        public static List<Vector3> FindVisiblePointsFromList(List<Vector3> points)
        {
            // get camera's frustrum planes
            Plane[] cameraFrustrumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);

            return FindVisibleGivenPointsBetweenFrustrumPlanes(points, cameraFrustrumPlanes);
        }

        public static List<Vector3> FindVisibleGivenPointsBetweenFrustrumPlanes(List<Vector3> points, Plane[] cameraFrustrumPlanes)
        {
            List<Vector3> visiblePoints = new List<Vector3>();

            foreach (Vector3 point in points)
            {
                // raise point to eye level
                Vector3 eyeLevelPoint = point;
                eyeLevelPoint.y = Camera.main.transform.position.y;

                if (IsPointVisibleBetweenFrustrumPlanes(eyeLevelPoint, cameraFrustrumPlanes))
                {
                    visiblePoints.Add(point);
                }
            }

            return visiblePoints;
        }

        public static bool IsPointVisibleBetweenFrustrumPlanes(Vector3 point, Plane[] cameraFrustrumPlanes)
        {
            // filter out grid elements above the camera
            bool tooHighUp = point.y > Camera.main.transform.position.y;

            return !tooHighUp &&
                    GeometryUtility.TestPlanesAABB(cameraFrustrumPlanes, new Bounds(point, Vector3.one * 0.01f));
        }

        public static bool IsNullOrIsDestroyedUnityObject(object obj)
        {
            if (obj == null)
            {
                return true;
            }

            // If it's a unity object, check the overloaded equality operator to see if it's destroyed
            if (obj is UnityEngine.Object unityObj)
            {
                return unityObj == null;
            }

            return false;
        }
    }
}
