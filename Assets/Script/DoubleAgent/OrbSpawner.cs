using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class OrbSpawner : NetworkBehaviour
{
    public GameObject orbPrefab;
    public Transform[] spawnPoints;

    public float spawnInterval = 3f;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];

            var orb = Instantiate(orbPrefab, point.position, Quaternion.identity);
            orb.GetComponent<NetworkObject>().Spawn();
        }
    }
}