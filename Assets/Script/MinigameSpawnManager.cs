using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode.Components;

public class MinigameSpawnManager : NetworkBehaviour
{
    public static MinigameSpawnManager Instance;

    public Transform[] spawnPoints;

    private bool hasSpawned = false;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
    }

    public void SpawnPlayers()
    {
        if (hasSpawned) return;

        var clients = NetworkManager.Singleton.ConnectedClientsList;

        for (int i = 0; i < clients.Count; i++)
        {
            var client = clients[i];
            var playerObj = client.PlayerObject;

            if (playerObj == null) continue;
            
            Transform spawn = spawnPoints[i % spawnPoints.Length];

            var netObj = playerObj.GetComponent<NetworkObject>();

            if (!netObj.IsSpawned)
            {
                netObj.SpawnWithOwnership(client.ClientId);
            }
            else
            {
                if (netObj.OwnerClientId != client.ClientId) netObj.ChangeOwnership(client.ClientId);

            }
            var rb = playerObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
                rb.useGravity = false;
            }
            var collsion = playerObj.GetComponent<Collider>();
            if (collsion != null)
            {
                collsion.isTrigger = false;
            }
            
            ApplySpawn(client.ClientId, client.PlayerObject, spawn);
        }
    }

    void ApplySpawn(ulong clientId, NetworkObject playerObj, Transform spawn)
    {
        var netTransform = playerObj.GetComponent<NetworkTransform>();

        Vector3 spawnPos = spawn.position + Vector3.up * 1.5f;

        if (netTransform != null)
        {
            netTransform.Teleport(
                spawnPos,
                spawn.rotation,
                playerObj.transform.localScale
            );
        }
        else
        {
            playerObj.transform.position = spawnPos;
        }

        var da = playerObj.GetComponent<PlayerDoubleAgent>();
        var pm = playerObj.GetComponent<PlayerMinigame>();
        if (pm != null)
        {
            pm.isAlive = true;
            pm.SetCanMove(false);
            if (MinigameManager.Instance != null)
                MinigameManager.Instance.RegisterPlayer(pm);

        }
        else if (da != null)
        {
            MinigameManager.Instance.RegisterPlayer(da);
        }
    }
}