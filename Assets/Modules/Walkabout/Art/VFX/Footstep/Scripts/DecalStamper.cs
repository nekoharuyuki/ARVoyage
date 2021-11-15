using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Niantic.ARVoyage.Walkabout;

/// <summary>
/// For placing footprint decals for Doty
/// </summary>
public class DecalStamper : MonoBehaviour
{
    [SerializeField] Transform leftFootTransform;
    [SerializeField] Transform rightFootTransform;
    [SerializeField] GameObject prefab;

    private int layerMask = 1 << 18;
    private RaycastHit[] results = new RaycastHit[4];

    public void LeftStamp(AnimationEvent animationEvent)
    {
        //Debug.Log("Left");
        Vector3 position = leftFootTransform.position;
        Vector3 scale = new Vector3(-1, 1, 1);
        Stamp(position, scale);
    }

    public void RightStamp(AnimationEvent animationEvent)
    {
        //Debug.Log("Right");
        Vector3 position = rightFootTransform.position;
        Vector3 scale = Vector3.one;
        Stamp(position, scale);
    }

    void Stamp(Vector3 position, Vector3 scale)
    {
        Quaternion rotation = Quaternion.LookRotation(transform.forward);

        // Adjust height to be off surface.
        position.y = transform.position.y + .01f;

        // Raycast down to look for tiles.
        int hitCount = Physics.RaycastNonAlloc(position, -Vector3.up, results, .05f,
                                               layerMask, QueryTriggerInteraction.Collide);
        if (hitCount > 0)
        {
            GameObject decalInstance = Instantiate(prefab, position, rotation);
            decalInstance.transform.localScale = scale;
        }

        // Fire footstep event.
        WalkaboutActor.EventFootstep.Invoke();
    }

    public void Loop()
    {
        //NOP, needed for looping clip.
    }
}