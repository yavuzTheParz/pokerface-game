using UnityEngine;

public class GameManager : MonoBehaviour
{
    public void CheckGameEnd(PlayerHand player)
    {
        if (player.hand.Count == 0)
        {
            Debug.Log("Player Wins!");
        }
    }
}