using Unity.Netcode;
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class OrbSpawner : NetworkBehaviour
{
    public GameObject orbPrefab;
    public Transform[] spawnPoints;

    public float spawnInterval = 3f;
    public int maxSpawn = 5;
    public int currentSpawn = 0;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        if (SceneManager.GetActiveScene().name == "Minigame_DoubleAgent")
        {
            StartCoroutine(SpawnRoutine());
        }

    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            if (SceneManager.GetActiveScene().name != "Minigame_DoubleAgent")
            {
                yield return null;
                continue;
            }

            if (currentSpawn < maxSpawn)
            {
                Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];

                var orb = Instantiate(orbPrefab, point.position, Quaternion.identity);
                orb.GetComponent<NetworkObject>().Spawn();

                currentSpawn++;
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }
}