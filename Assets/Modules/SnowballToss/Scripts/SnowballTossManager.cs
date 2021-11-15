using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Niantic.ARDK.Extensions.Meshing;

namespace Niantic.ARVoyage.SnowballToss
{
    /// <summary>
    /// Constants, GameObjects, game state, and helper methods used by 
    /// various State classes in the SnowballToss demo
    /// </summary>
    public class SnowballTossManager : MonoBehaviour, ISceneDependency
    {
        public const int minVictoryPoints = scoreIncrementPerRing * 4;
        private const int scoreIncrementPerRing = 100;
        public const int gameDuration = 45;
        public const int nearGameEndDuration = 5;
        private const int maxNumSnowrings = 3;

        // gets added to SnowballMaker.defaultTossAngle
        private const float snowballTossAngleDegOffset = 10f;

        [Header("Snowball Toss")]
        [SerializeField] public SnowballMaker snowballMaker;
        [SerializeField] public GameObject snowringPrefab;

        [Header("AR Mesh")]
        [SerializeField] public ARMeshManager _ARMeshUpdater = null;
        protected const int minVertexesForValidMesh = 1000;

        [Header("GUI")]
        [SerializeField] private GameTimeAndScore gameTimeAndScoreGUI;
        public int gameScore { get; private set; } = 0;

        public List<Snowring> snowrings { get; private set; } = new List<Snowring>();
        private Snowring newSnowringSearchingForPlacement;
        private int snowringCtr = 0;
        private float nextSnowringStartTime = 0f;
        public int lastDestroyedSnowringSector { get; private set; } = -1;

        private const float secsTillFirstSnowring = 0f;
        private const float secsTillNextSnowring = 2.5f;
        private const float secsTillReplacementSnowring = 0f;


        void OnEnable()
        {
            if (Application.isEditor)
            {
                _ARMeshUpdater.UseInvisibleMaterial = false;
            }

            snowballMaker.tossAngle = SnowballMaker.defaultTossAngle + snowballTossAngleDegOffset;
        }


        public void InitTossGame()
        {
            gameScore = 0;
            snowringCtr = 0;
            snowrings.Clear();
            newSnowringSearchingForPlacement = null;
            nextSnowringStartTime = Time.time + secsTillFirstSnowring;
        }


        public void UpdateTossGame()
        {
            // Instantiate a snowring when ready, up to maxNumSnowrings
            if (snowrings.Count < maxNumSnowrings && Time.time >= nextSnowringStartTime)
            {
                // if we haven't instantiated a new snowring, do so
                if (newSnowringSearchingForPlacement == null)
                {
                    GameObject snowringInstance = Instantiate(snowringPrefab);
                    newSnowringSearchingForPlacement = snowringInstance.GetComponent<Snowring>();

                    // update sector information for all existing snowrings
                    foreach (Snowring existingSnowring in snowrings)
                    {
                        existingSnowring.UpdateCurrentSector();
                    }
                }

                // try placing the new snowring
                if (newSnowringSearchingForPlacement != null)
                {
                    bool isValidNewSnowring = newSnowringSearchingForPlacement.InitSnowring(this);

                    if (isValidNewSnowring)
                    {
                        snowrings.Add(newSnowringSearchingForPlacement);
                        newSnowringSearchingForPlacement.gameObject.SetActive(true);
                        newSnowringSearchingForPlacement = null;

                        ++snowringCtr;
                        nextSnowringStartTime = Time.time +
                            (snowringCtr >= maxNumSnowrings ? secsTillReplacementSnowring : secsTillNextSnowring);
                    }
                }
                else
                {
                    Debug.LogError("SnowballTossManager null snowring");
                }
            }
        }

        public void SnowRingSucceeded()
        {
            gameScore += scoreIncrementPerRing;

            gameTimeAndScoreGUI.IncrementScore(scoreIncrementPerRing);
        }

        public void SnowRingDestroyed(Snowring snowring, int currentSector)
        {
            lastDestroyedSnowringSector = currentSector;

            if (!snowrings.Remove(snowring))
            {
                Debug.LogError("Couldn't find snowring in list");
            }

            // if we're not already in the middle of placing a new snowring,
            // choose the next new snowring time
            if (newSnowringSearchingForPlacement == null)
            {
                nextSnowringStartTime = Time.time + secsTillReplacementSnowring;
            }
        }

        public void ExpireAllSnowrings()
        {
            foreach (Snowring snowring in snowrings)
            {
                snowring.Expire();
            }
        }
    }
}
