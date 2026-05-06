using UnityEngine;
using TMPro;
using System.Collections;

public class DiceAnimator : MonoBehaviour
{
    public TextMeshProUGUI diceText;

    public void PlayDiceAnimation(int finalValue)
    {
        StartCoroutine(RollAnimation(finalValue));
    }

    IEnumerator RollAnimation(int finalValue)
    {
        float duration = 1.5f;
        float timer = 0f;

        while (timer < duration)
        {
            int randomValue = Random.Range(1, 7);
            diceText.text = "Dice: " + randomValue;

            yield return new WaitForSeconds(0.1f);
            timer += 0.1f;
        }

        diceText.text = "Dice: " + finalValue;
    }
}