using UnityEngine;
using Unity.Netcode;

public class TransformPlayer : NetworkBehaviour
{
    [ServerRpc(RequireOwnership = false)]
    public void RequestTransformServerRpc(ulong playerNetworkObjectId, ulong targetNetworkObjectId)
    {
        // Broadcast the transformation to all clients
        TransformClientRpc(playerNetworkObjectId, targetNetworkObjectId);
    }

    [ClientRpc]
    private void TransformClientRpc(ulong playerNetworkObjectId, ulong targetNetworkObjectId)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerNetworkObjectId, out NetworkObject playerNetObj)) return;
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out NetworkObject targetNetObj)) return;

        GameObject player = playerNetObj.gameObject;
        GameObject targetObject = targetNetObj.gameObject;

        ExecuteTransform(player, targetObject);
    }

    private void ExecuteTransform(GameObject player, GameObject targetObject)
    {
        MeshFilter targetMeshFilter = targetObject.GetComponent<MeshFilter>();
        MeshRenderer targetMeshRenderer = targetObject.GetComponent<MeshRenderer>();
        Collider targetCollider = targetObject.GetComponent<Collider>();

        if (targetMeshFilter == null || targetMeshFilter.sharedMesh == null || targetCollider == null) return;

        ReplacePlayerColliders(player, targetCollider);

        // --- Match mesh ---
        MeshFilter playerMeshFilter = player.GetComponentInChildren<MeshFilter>();
        if (playerMeshFilter != null)
        {
            playerMeshFilter.sharedMesh = targetMeshFilter.sharedMesh;
        }

        // --- Match materials (Ensures colors sync on Client) ---
        MeshRenderer playerMeshRenderer = player.GetComponentInChildren<MeshRenderer>();
        if (playerMeshRenderer != null && targetMeshRenderer != null)
        {
            playerMeshRenderer.sharedMaterials = targetMeshRenderer.sharedMaterials;
        }

        SnapToGround(player, targetCollider);
    }

    private void ReplacePlayerColliders(GameObject player, Collider targetCollider)
    {
        Collider[] existingColliders = player.GetComponentsInChildren<Collider>();
        foreach (Collider existingCollider in existingColliders)
        {
            if (existingCollider != null) Destroy(existingCollider);
        }

        if (targetCollider is BoxCollider boxCollider)
        {
            BoxCollider playerCollider = player.AddComponent<BoxCollider>();
            playerCollider.center = boxCollider.center;
            playerCollider.size = boxCollider.size;
        }
        else if (targetCollider is SphereCollider sphereCollider)
        {
            SphereCollider playerCollider = player.AddComponent<SphereCollider>();
            playerCollider.center = sphereCollider.center;
            playerCollider.radius = sphereCollider.radius;
        }
        else if (targetCollider is CapsuleCollider capsuleCollider)
        {
            CapsuleCollider playerCollider = player.AddComponent<CapsuleCollider>();
            playerCollider.center = capsuleCollider.center;
            playerCollider.radius = capsuleCollider.radius;
            playerCollider.height = capsuleCollider.height;
            playerCollider.direction = capsuleCollider.direction;
        }
        else if (targetCollider is MeshCollider meshCollider)
        {
            MeshCollider playerCollider = player.AddComponent<MeshCollider>();
            MeshFilter targetFilter = targetCollider.GetComponent<MeshFilter>();
            playerCollider.sharedMesh = meshCollider.sharedMesh != null ? meshCollider.sharedMesh : (targetFilter != null ? targetFilter.sharedMesh : null);
            playerCollider.convex = meshCollider.convex;
            playerCollider.cookingOptions = meshCollider.cookingOptions;
        }
    }

    private void SnapToGround(GameObject player, Collider targetCollider)
    {
        Bounds bounds = targetCollider.bounds;
        float bottomOffset = bounds.center.y - (bounds.extents.y);
        Vector3 castOrigin = player.transform.position + Vector3.up * 0.1f;

        if (Physics.Raycast(castOrigin, Vector3.down, out RaycastHit hit, 5f))
        {
            float targetY = hit.point.y - bottomOffset;
            Vector3 pos = player.transform.position;
            pos.y = targetY;
            player.transform.position = pos;
        }
    }
}