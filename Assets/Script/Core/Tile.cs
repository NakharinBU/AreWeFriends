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

    [Header("Sound")]
    public AudioClip sfx;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void PlaySound()
    {
        if (sfx != null && audioSource != null)
        {
            audioSource.PlayOneShot(sfx);
        }
    }
}