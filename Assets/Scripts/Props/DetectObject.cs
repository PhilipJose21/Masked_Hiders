using UnityEngine;
using Unity.Netcode;

public class DetectObject : NetworkBehaviour
{
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float lookDistance = 6f;
    [SerializeField] private float detectionRadius = 0.25f;

    public void detectObject()
    {
        if (!IsOwner) return;

        if (TryGetLookHit(GetActiveCamera(), out RaycastHit hit))
        {
            TransformPlayer transformPlayer = hit.collider.GetComponent<TransformPlayer>();
            if (transformPlayer != null)
            {
                // Tell the server to transform this player for EVERYONE
                transformPlayer.RequestTransformServerRpc(NetworkObjectId, hit.collider.GetComponent<NetworkObject>().NetworkObjectId);
            }
        }
    }

    private bool TryGetLookHit(Camera sourceCamera, out RaycastHit bestHit)
    {
        bestHit = default;
        if (sourceCamera == null) return false;

        Ray ray = new Ray(sourceCamera.transform.position, sourceCamera.transform.forward);
        RaycastHit[] hits = Physics.SphereCastAll(ray, detectionRadius, lookDistance);

        float closestDistance = float.MaxValue;
        Transform cameraRoot = sourceCamera.transform.root;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider != null && hit.collider.transform.IsChildOf(cameraRoot)) continue;

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
        if (playerCamera != null) return playerCamera;
        if (Camera.main != null) return Camera.main;
        return FindFirstObjectByType<Camera>();
    }
}