// TestRunner.cs  (sahneyi test ettikten sonra sil)
using System.Collections.Generic;
using UnityEngine;

public class TestRunner : MonoBehaviour
{
    void Start()
    {
        var players = new List<string> { "Alice", "Bob", "Charlie" };
        CardManager.Instance.InitializeGame(players);
        TurnManager.Instance.StartGame(players);

        foreach (var id in players)
        {
            var hand = CardManager.Instance.GetHandCards(id);
            Debug.Log($"{id} eli: {string.Join(", ", hand.ConvertAll(c => $"{c.Element}{c.Value}"))}");
        }
    }
}
