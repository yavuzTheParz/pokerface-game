// GameStarter.cs
using System.Collections.Generic;
using UnityEngine;

public class GameStarter : MonoBehaviour
{
    [SerializeField] bool useTestHand = true;

    void Start()
    {
        var playerIds = new List<string> { "1" };
        CardManager.Instance.InitializeGame(playerIds);
        TurnManager.Instance.StartGame(playerIds);

        if (useTestHand)
            GiveTestHand("1");

        GameUIManager.Instance.RefreshHand();
    }

    void GiveTestHand(string playerId)
    {
        var hand = CardManager.Instance.GetHand(playerId);
        hand.Cards.Clear();



        // Elde kalan kartlar: Su7, Hava7, Toprak7
        hand.AddCard(GetCard(CardElement.Water, 7));
        hand.AddCard(GetCard(CardElement.Air,   7));
        hand.AddCard(GetCard(CardElement.Earth, 7));
        hand.AddCard(GetCard(CardElement.Fire, 4));
        hand.AddCard(GetCard(CardElement.Fire, 5));
        hand.AddCard(GetCard(CardElement.Fire, 6));
        hand.AddCard(GetCard(CardElement.Fire, 7));

    }

    Card GetCard(CardElement element, int value)
    {
        foreach (var data in CardManager.Instance.AllCardData)
            if (data.element == element && data.value == value)
                return new Card(data, "1");

        Debug.LogWarning($"Kart bulunamadı: {element} {value}");
        return null;
    }
}
