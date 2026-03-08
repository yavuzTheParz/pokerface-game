using UnityEngine;
using System.Collections.Generic;

public class TurnManager : MonoBehaviour
{
    public List<GameObject> players = new List<GameObject>();

    int currentPlayerIndex = 0;

    public GameObject CurrentPlayer()
    {
        return players[currentPlayerIndex];
    }

    public void NextTurn()
    {
        currentPlayerIndex++;

        if (currentPlayerIndex >= players.Count)
            currentPlayerIndex = 0;
    }
}