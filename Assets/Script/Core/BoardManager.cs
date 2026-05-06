using UnityEngine;
using System.Collections.Generic;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance;

    public List<Tile> tiles;

    private void Awake()
    {
        Instance = this;
    }

    public Vector3 GetTilePosition(int index)
    {
        int count = tiles.Count;
        index = ((index % count) + count) % count;

        return tiles[index].transform.position;
    }

    public Tile GetTile(int index)
    {
        int count = tiles.Count;
        index = ((index % count) + count) % count;

        return tiles[index];
    }
}