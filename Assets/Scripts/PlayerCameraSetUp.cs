using Unity.Netcode;
using UnityEngine;

public class PlayerCameraSetup : NetworkBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioListener audioListener;

    public override void OnNetworkSpawn()
    {
        // IsOwner checks if this prefab instance belongs to the local machine
        if (IsOwner)
        {
            // Enable local player's camera & audio listener
            if (playerCamera != null) playerCamera.enabled = true;
            if (audioListener != null) audioListener.enabled = true;

            // Optional: Disable the scene's default main camera if you have one
            if (Camera.main != null && Camera.main != playerCamera)
            {
                Camera.main.gameObject.SetActive(false);
            }
        }
        else
        {
            // Disable camera & audio listener for remote players
            if (playerCamera != null) playerCamera.enabled = false;
            if (audioListener != null) audioListener.enabled = false;
        }
    }
}