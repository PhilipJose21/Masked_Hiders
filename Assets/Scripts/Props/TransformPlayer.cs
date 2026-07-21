using UnityEngine;
using Unity.Netcode;

public class TransformPlayer : NetworkBehaviour
{
    [ServerRpc(RequireOwnership = false)]
    public void RequestTransformServerRpc(ulong playerNetworkObjectId, ulong targetNetworkObjectId)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerNetworkObjectId, out NetworkObject playerNetObj)) return;
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out NetworkObject targetNetObj)) return;

        GameObject player = playerNetObj.gameObject;
        GameObject targetObject = targetNetObj.gameObject;

        // Server-authoritative guard: don't bother transforming (or broadcasting)
        // if the player already matches this target's mesh + scale.
        if (IsAlreadyDisguisedAs(player, targetObject)) return;

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

    private bool IsAlreadyDisguisedAs(GameObject player, GameObject targetObject)
    {
        MeshFilter playerMeshFilter = player.GetComponentInChildren<MeshFilter>();
        MeshFilter targetMeshFilter = targetObject.GetComponent<MeshFilter>();

        if (playerMeshFilter == null || targetMeshFilter == null) return false;
        if (playerMeshFilter.sharedMesh != targetMeshFilter.sharedMesh) return false;

        // Compare world-space scale, since that's what actually determines visible size,
        // with a small tolerance for float imprecision.
        Vector3 playerScale = player.transform.lossyScale;
        Vector3 targetScale = targetObject.transform.lossyScale;

        return Vector3.Distance(playerScale, targetScale) < 0.001f;
    }

    private void ExecuteTransform(GameObject player, GameObject targetObject)
    {
        MeshFilter targetMeshFilter = targetObject.GetComponent<MeshFilter>();
        MeshRenderer targetMeshRenderer = targetObject.GetComponent<MeshRenderer>();
        Collider targetCollider = targetObject.GetComponent<Collider>();

        if (targetMeshFilter == null || targetMeshFilter.sharedMesh == null || targetCollider == null) return;

        Rigidbody rb = player.GetComponent<Rigidbody>();
        bool wasKinematic = false;
        if (rb != null)
        {
            // Prevent physics from reacting mid-transform (e.g. falling through
            // thin colliders during the brief window colliders are being swapped).
            wasKinematic = rb.isKinematic;
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
        }

        // --- Match scale FIRST ---
        MatchScale(player, targetObject);

        // --- Compute and apply correct ground position BEFORE adding the new collider ---
        SnapToGround(player, targetCollider);

        // --- Now safe to swap colliders, since the player is already positioned correctly ---
        ReplacePlayerColliders(player, targetCollider);

        // --- Match mesh ---
        MeshFilter playerMeshFilter = player.GetComponentInChildren<MeshFilter>();
        if (playerMeshFilter != null)
        {
            playerMeshFilter.sharedMesh = targetMeshFilter.sharedMesh;
        }

        // --- Match materials ---
        MeshRenderer playerMeshRenderer = player.GetComponentInChildren<MeshRenderer>();
        if (playerMeshRenderer != null && targetMeshRenderer != null)
        {
            playerMeshRenderer.sharedMaterials = targetMeshRenderer.sharedMaterials;
        }

        if (rb != null)
        {
            rb.isKinematic = wasKinematic;
        }
    }

    private void MatchScale(GameObject player, GameObject targetObject)
    {
        // If the player has no scaled parent, world scale == local scale, and this
        // is a direct copy. If parents ARE scaled, divide out the parent's scale so
        // the player's resulting WORLD scale still matches the target's world scale.
        Vector3 targetWorldScale = targetObject.transform.lossyScale;

        Transform parent = player.transform.parent;
        if (parent != null)
        {
            Vector3 parentScale = parent.lossyScale;
            player.transform.localScale = new Vector3(
                targetWorldScale.x / parentScale.x,
                targetWorldScale.y / parentScale.y,
                targetWorldScale.z / parentScale.z
            );
        }
        else
        {
            player.transform.localScale = targetWorldScale;
        }
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
        // Get the target's local-space bottom offset (independent of where the
        // target object happens to sit in the scene), then reapply it relative
        // to the player using the player's own (already-updated) scale.
        float localBottomY = GetLocalBottomOffset(targetCollider);
        float worldBottomOffset = localBottomY * player.transform.lossyScale.y;

        Vector3 castOrigin = player.transform.position + Vector3.up * 2f; // cast from safely above

        if (Physics.Raycast(castOrigin, Vector3.down, out RaycastHit hit, 20f))
        {
            Vector3 pos = player.transform.position;
            pos.y = hit.point.y - worldBottomOffset;
            player.transform.position = pos;
        }
    }

    private float GetLocalBottomOffset(Collider collider)
    {
        if (collider is BoxCollider box)
            return box.center.y - box.size.y * 0.5f;
        if (collider is SphereCollider sphere)
            return sphere.center.y - sphere.radius;
        if (collider is CapsuleCollider capsule)
            return capsule.center.y - capsule.height * 0.5f;
        if (collider is MeshCollider mesh && mesh.sharedMesh != null)
            return mesh.sharedMesh.bounds.min.y;

        return 0f;
    }
}