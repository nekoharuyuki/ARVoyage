using UnityEngine;

/// <summary>
/// Utility for billboarding an object to face the camera
/// </summary>
public class Billboard : MonoBehaviour
{
    [SerializeField] bool verticalOnly = false;
    [SerializeField] bool invertDirection = false;
    private Transform cameraTransform;

    void Start()
    {
        if (Camera.main == null)
        {
            Debug.LogError("Couldn't find the main camera: deactivating billboard effect");
            enabled = false;
            return;
        }
        cameraTransform = Camera.main.gameObject.transform;
    }

    void Update()
    {
        Vector3 lookTarget = new Vector3(
            cameraTransform.position.x,
            (!verticalOnly) ? cameraTransform.position.y : transform.position.y,
            cameraTransform.position.z
        );
        Vector3 lookVector = (invertDirection) ?
            (lookTarget - transform.position) :
            (transform.position - lookTarget);

        transform.LookAt(transform.position + lookVector.normalized);
    }
}
