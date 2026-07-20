using UnityEngine;

public class DetectObject : MonoBehaviour
{
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float lookDistance = 6f;
    [SerializeField] private float gizmoRadius = 0.2f;
    [SerializeField] private float detectionRadius = 0.25f;

    private void Update()
    {

    }

    public void detectObject()
    {
        if (TryGetLookHit(GetActiveCamera(), out RaycastHit hit))
        {
            if (hit.collider.GetComponent<TransformPlayer>() != null)
            {
                TransformPlayer transformPlayer = hit.collider.GetComponent<TransformPlayer>();
                transformPlayer.TransformPlayerToObject(this.gameObject, hit.collider.gameObject);
            }
        }
        else
        {
            Debug.Log("No object detected");
        }
    }

    private void OnDrawGizmos()
    {
        Camera sceneCamera = GetActiveCamera();
        if (sceneCamera == null)
        {
            return;
        }

        Vector3 startPosition = sceneCamera.transform.position;
        Vector3 lookDirection = sceneCamera.transform.forward;
        Vector3 gizmoCenter = startPosition + lookDirection * lookDistance;

        if (TryGetLookHit(sceneCamera, out RaycastHit hit))
        {
            gizmoCenter = hit.point;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(gizmoCenter, gizmoRadius);
        Gizmos.DrawLine(startPosition, gizmoCenter);
    }

    private bool TryGetLookHit(Camera sourceCamera, out RaycastHit bestHit)
    {
        bestHit = default;

        if (sourceCamera == null)
        {
            return false;
        }

        Ray ray = new Ray(sourceCamera.transform.position, sourceCamera.transform.forward);
        RaycastHit[] hits = Physics.SphereCastAll(ray, detectionRadius, lookDistance);

        float closestDistance = float.MaxValue;
        Transform cameraRoot = sourceCamera.transform.root;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider != null && hit.collider.transform.IsChildOf(cameraRoot))
            {
                continue;
            }

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                bestHit = hit;
            }
        }

        return closestDistance < float.MaxValue;
    }

    private Camera GetActiveCamera()
    {
        if (playerCamera != null)
        {
            return playerCamera;
        }

        if (Camera.main != null)
        {
            return Camera.main;
        }

        return FindFirstObjectByType<Camera>();
    }
}
