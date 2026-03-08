using UnityEngine;

public class PenaltySystem : MonoBehaviour
{
    public void DropBiggestCard(PlayerHand player)
    {
        CardData biggest = player.GetBiggestCard();

        player.RemoveCard(biggest);

        Debug.Log("Dropped card: " + biggest.cardName);
    }
}