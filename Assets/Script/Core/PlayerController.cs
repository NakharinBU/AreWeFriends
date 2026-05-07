using Unity.Netcode;
using UnityEngine;
using System.Collections;
using Unity.Netcode.Components;

public class PlayerController : NetworkBehaviour
{
    public float moveSpeed = 50f;
    private int currentTile = 0;
    
    [SerializeField] private Renderer characterRenderer;

    public NetworkVariable<int> coin = new NetworkVariable<int>(0);
    public NetworkVariable<int> hp = new NetworkVariable<int>(100);
    public NetworkVariable<int> currentTileIndex = new NetworkVariable<int>(0);
    public NetworkVariable<TeamType> team = new NetworkVariable<TeamType>(TeamType.None);
    
    private TeamType previousTeam = TeamType.None;
    
    public Transform visualRoot;
    private PlayerState state;

    [SerializeField] private Animator animator;

    private void Awake()
    {
        state = GetComponent<PlayerState>();
    }
    public override void OnNetworkSpawn()
    {
        ApplyTeamColor();

        StartCoroutine(WaitForTeam());

        if (IsServer)
        {
            currentTileIndex.Value = currentTile;
        }
        else
        {
            currentTile = currentTileIndex.Value;
        }

    }

    IEnumerator WaitForTeam()
    {
        yield return new WaitUntil(() => team.Value != TeamType.None);
        SetColor(team.Value);
    }

    [Rpc(SendTo.Server)]
    public void RollDiceRpc()
    {
        if (GameManager.Instance == null) return;

        ulong senderId = OwnerClientId;
        GameManager.Instance.RollDice(senderId);
    }

    public IEnumerator MoveRoutine(int steps)
    {
        if (!IsServer) yield break;


        int totalTiles = BoardManager.Instance.tiles.Count;

        animator.SetBool("isWalking", true);

        for (int i = 0; i < steps; i++)
        {
            int nextTile = (currentTile + 1) % totalTiles;

            Vector3 targetPos = GetTilePositionWithOffset(nextTile);

            Vector3 direction = (targetPos - transform.position).normalized;
            direction.y = 0;

            if (direction != Vector3.zero)
            {
                if (state.currentMode.Value != PlayerMode.Board) yield break;
                Quaternion lookRot =  Quaternion.LookRotation(direction);

                Quaternion finalRot = lookRot * Quaternion.Euler(0, 0, state.modelZRotationOffset);

                transform.rotation = finalRot;
            }

            currentTile = nextTile;
            currentTileIndex.Value = currentTile;

            while (Vector3.Distance(transform.position, targetPos) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPos,
                    moveSpeed * Time.deltaTime
                );

                yield return null;
            }
        }

        animator.SetBool("isWalking", false);

    }

    public bool IsMyTurn()
    {
        if (GameManager.Instance == null) return false;

        return OwnerClientId ==
            GameManager.Instance.GetCurrentPlayerId();
    }

    public void WarpToTile(int tileIndex)
    {
        currentTile = tileIndex;
        currentTileIndex.Value = tileIndex;

        Vector3 pos = GetTilePositionWithOffset(tileIndex);

        int nextTile = (tileIndex + 1) % BoardManager.Instance.tiles.Count;

        Vector3 nextPos = GetTilePositionWithOffset(nextTile);

        Vector3 direction = (nextPos - pos).normalized;
        direction.y = 0;

        Quaternion rot = transform.rotation;

        if (direction != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(direction);
            rot = lookRot * Quaternion.Euler(0, 0, state.modelZRotationOffset);
        }

        if (TryGetComponent(out NetworkTransform netTransform))
        {
            netTransform.Teleport(pos, rot, transform.localScale);
        }
        else
        {
            transform.position = pos;
            transform.rotation = rot;
        }
    }

    void SetSpawnPosition()
    {
        int index = GameManager.Instance.GetPlayerIndex(OwnerClientId);

        if (index >= 0)
        {
            Transform spawn = GameManager.Instance.spawnPoints;

            transform.position = spawn.position;
            transform.rotation = spawn.rotation;
        }
    }

    Vector3 GetTilePositionWithOffset(int tileIndex)
    {
        Vector3 basePos = BoardManager.Instance.GetTilePosition(tileIndex);

        int playerIndex = GameManager.Instance.GetPlayerIndex(OwnerClientId);

        float radius = 0.5f;
        float angle = playerIndex * 120f;

        float rad = angle * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(
            Mathf.Cos(rad),
            0,
            Mathf.Sin(rad)
        ) * radius;

        float heightOffset = characterRenderer.bounds.extents.y;

        return basePos + offset + Vector3.up * heightOffset;
    }

    void ApplyTeamColor()
    {
        team.OnValueChanged += (oldTeam, newTeam) =>
        {
            if (newTeam != TeamType.None)
                SetColor(newTeam);
        };

        if (team.Value != TeamType.None)
            SetColor(team.Value);
    }

    void SetColor(TeamType teamType)
    {
        if (characterRenderer == null) return;

        Color color;

        switch (teamType)
        {
            case TeamType.Red: color = Color.red; break;
            case TeamType.Blue: color = Color.blue; break;
            case TeamType.Green: color = Color.green; break;
            case TeamType.Yellow: color = Color.yellow; break;

            default:
                color = new Color(0.7f, 0.7f, 0.7f);
                break;
        }

        characterRenderer.material.color = color;
    }

    public void SaveCurrentTeam()
    {
        previousTeam = team.Value;
    }


    public void RestorePreviousTeam()
    {
        team.Value = previousTeam;
    }

}