using Unity.Netcode;
using UnityEngine;

public class SnowballSpawner : NetworkBehaviour
{
    public GameObject snowballPrefab;
    public Transform redSpawn;
    public Transform blueSpawn;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        if (!UnityEngine.SceneManagement.SceneManager
            .GetActiveScene().name.Contains("Snow"))
            return;

        SpawnBall(TeamType.Red, redSpawn.position);
        SpawnBall(TeamType.Blue, blueSpawn.position);
    }

    void SpawnBall(TeamType team, Vector3 pos)
    {
        var obj = Instantiate(snowballPrefab, pos, Quaternion.identity);
        var ball = obj.GetComponent<Snowball>();

        ball.team = team;

        obj.GetComponent<NetworkObject>().Spawn();
    }
}