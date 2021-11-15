using System.Collections.Generic;
using UnityEngine;

namespace Niantic.ARVoyage
{
    /// <summary>
    /// Instantiates a Snowball, whose hierarchy has a holder parent with a dynamic hold offset, 
    /// so the snowball can be held forward from the camera if view from first-person POV,
    /// or held directly on the camera if viewed from third-person POV, e.g. other players holding 
    /// a snowball in a multiplayer game like SnowballFight.
    /// SnowballTossButton is wired to call TossSnowball, launching the snowball forward in space.
    /// </summary>
    public class SnowballMaker : MonoBehaviour
    {
        public const float defaultTossAngle = 30f;
        private const float zDistFromButton = 0.4f;
        private const float secsTillNextSnowball = 1f;

        [SerializeField] public GameObject snowballPrefab;
        [SerializeField] private GameObject snowballTossButton;
        private SpriteSequencePlayer snowballTossButtonPlayer;

        private Snowball heldSnowball = null;
        private Vector3 heldSnowballRotationOffset = Vector3.zero;
        private float nextSnowballTime = 0f;
        public float tossAngle = defaultTossAngle;

        void Awake()
        {
            // Start off inactive, since AR may not be ready yet
            this.gameObject.SetActive(false);

            snowballTossButtonPlayer = snowballTossButton.GetComponent<SpriteSequencePlayer>();
        }

        private void Update()
        {
            // Instantiate a held snowball when ready
            if (heldSnowball == null && Time.time > nextSnowballTime)
            {
                // Get/create a snowball from snowball pool
                GameObject snowballInstance = GetFreshSnowball();
                if (snowballInstance != null)
                {
                    // Child the snowball to us
                    snowballInstance.transform.parent = this.transform;
                    snowballInstance.transform.position = this.transform.position;
                    snowballInstance.transform.localRotation = Quaternion.identity;

                    // Init the snowball
                    heldSnowball = snowballInstance.GetComponent<Snowball>();
                    if (heldSnowball != null)
                    {
                        heldSnowball.InitSnowball("SnowballMaker");

                        // Set active
                        heldSnowball.gameObject.SetActive(true);

                        // Listen for held snowball expiration
                        heldSnowball.EventSnowballExpiring.AddListener(OnHeldSnowballExpiring);
                    }
                    else
                    {
                        Debug.LogError("SnowballMaker null snowball");
                    }
                }
            }

            // Position held snowball behind (further out than) toss button on HUD, 
            // so player can see the snowball
            if (heldSnowball != null)
            {
                Vector3 screenspacePos = snowballTossButton.gameObject.transform.position;
                screenspacePos.z = zDistFromButton; // further out from toss button
                Vector3 snowballPos = Camera.main.ScreenToWorldPoint(screenspacePos);

                // put ourselves at the camera
                this.transform.position = Camera.main.transform.position;

                // Orient snowball to match the camera rotation plus local held rotation offset
                heldSnowball.snowballModel.transform.rotation =
                    Camera.main.transform.rotation * Quaternion.Euler(heldSnowballRotationOffset);

                // hold the snowball in front of us
                Vector3 holdOffset = snowballPos - this.transform.position;
                heldSnowball.SetSnowballHoldOffset(holdOffset);
            }
        }

        private void OnDestroy()
        {
            if (heldSnowball != null)
            {
                heldSnowball.EventSnowballExpiring.RemoveListener(OnHeldSnowballExpiring);
            }
        }


        public GameObject GetFreshSnowball()
        {
            GameObject freshSnowball = InstantiatePrefab();
            if (freshSnowball != null)
            {
                // Start snowball off inactive
                freshSnowball.SetActive(false);
            }

            // Choose a random hold rotation, applied in Update()
            heldSnowballRotationOffset = new Vector3(
                UnityEngine.Random.Range(0f, 360f),
                UnityEngine.Random.Range(0f, 360f),
                UnityEngine.Random.Range(0f, 360f));

            return freshSnowball;
        }


        // virtual, to allow for instantiating a different snowball prefab
        public virtual GameObject InstantiatePrefab()
        {
            return Instantiate(snowballPrefab);
        }


        // Called by the scene's tossSnowballButton
        // Pass the message to the instantiated snowball
        public void TossSnowball()
        {
            // Ignore toss button clicks while we have no held snowball, 
            // or the toss button is animating (recharging)
            if (heldSnowball == null || snowballTossButtonPlayer.IsPlaying) return;

            // Make snowball unheld
            heldSnowball.DetachFromParent();

            // Be sure snowball is oriented the same as the camera
            heldSnowball.transform.rotation = Camera.main.transform.rotation;

            // Toss the snowball
            heldSnowball.TossLocallySpawnedSnowball(tossAngle);

            // No held snowball anymore
            heldSnowball = null;

            // Set time till next snowball
            nextSnowballTime = Time.time + secsTillNextSnowball;
        }

        protected virtual void OnSnowbalCollision(Snowball snowball, Collision collision) { }


        public void Expire()
        {
            // If a held snowball, dispose snowball and deactivate button
            if (heldSnowball != null)
            {
                // First, stop listening for the held snowball to expire, since the maker
                // doesn't need to do any cleanup if it triggers the snowball expire
                heldSnowball.EventSnowballExpiring.RemoveListener(OnHeldSnowballExpiring);
                heldSnowball.Expire(destroy: true);
                heldSnowball = null;
            }
        }

        // This will only be called for my held snowball
        private void OnHeldSnowballExpiring(Snowball expiringSnowball)
        {
            // Sanity check to ensure this is my held snowball
            if (heldSnowball != null && heldSnowball == expiringSnowball)
            {
                // Stop listening for the event
                heldSnowball.EventSnowballExpiring.RemoveListener(OnHeldSnowballExpiring);

                // Set time to immediately create a new held snowball
                nextSnowballTime = Time.time;

                // Set the current reference to held snowball to null so a new one will be created
                heldSnowball = null;
            }
        }

        public void SetSnowballHoldOffset(Vector3 positionOffset)
        {
            if (heldSnowball != null)
            {
                heldSnowball.SetSnowballHoldOffset(positionOffset);
            }
        }

    }
}
