using UnityEngine;

public enum TileType
{
    Empty,
    Coin,
    Damage,
    Minigame
}

public class Tile : MonoBehaviour
{
    public TileType tileType;

    public int coinAmount = 10;
    public int damageAmount = 10;
}