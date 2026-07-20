using UnityEngine;

public class TransformPlayer : MonoBehaviour
{
    public void TransformPlayerToObject(GameObject player, GameObject targetObject)
    {
        MeshFilter targetMeshFilter = targetObject.GetComponent<MeshFilter>();
        MeshRenderer targetMeshRenderer = targetObject.GetComponent<MeshRenderer>();
        Collider targetCollider = targetObject.GetComponent<Collider>();

        if (targetMeshFilter == null || targetMeshFilter.sharedMesh == null || targetCollider == null)
        {
            return;
        }

        ReplacePlayerColliders(player, targetCollider);

        // --- Match mesh ---
        MeshFilter playerMeshFilter = player.GetComponentInChildren<MeshFilter>();
        if (playerMeshFilter != null)
        {
            playerMeshFilter.sharedMesh = targetMeshFilter.sharedMesh;
        }

        SkinnedMeshRenderer playerSkinnedMeshRenderer = player.GetComponentInChildren<SkinnedMeshRenderer>();
        if (playerSkinnedMeshRenderer != null)
        {
            playerSkinnedMeshRenderer.sharedMesh = targetMeshFilter.sharedMesh;
        }

        // --- Match materials ---
        MeshRenderer playerMeshRenderer = player.GetComponentInChildren<MeshRenderer>();
        if (playerMeshRenderer != null && targetMeshRenderer != null)
        {
            playerMeshRenderer.sharedMaterials = targetMeshRenderer.sharedMaterials;
        }

        SnapToGround(player, targetCollider);
    }

    private void ReplacePlayerColliders(GameObject player, Collider targetCollider)
    {
        // 1. Destroy existing colliders
        Collider[] existingColliders = player.GetComponentsInChildren<Collider>();
        foreach (Collider existingCollider in existingColliders)
        {
            if (existingCollider != null)
            {
                Destroy(existingCollider);
            }
        }

        // 2. Add the matching new collider type
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
            playerCollider.sharedMesh = meshCollider.sharedMesh != null ? meshCollider.sharedMesh : targetObjectMesh(targetCollider);
            playerCollider.convex = meshCollider.convex;
            playerCollider.cookingOptions = meshCollider.cookingOptions;
        }
    }

    private Mesh targetObjectMesh(Collider targetCollider)
    {
        MeshFilter meshFilter = targetCollider.GetComponent<MeshFilter>();
        return meshFilter != null ? meshFilter.sharedMesh : null;
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