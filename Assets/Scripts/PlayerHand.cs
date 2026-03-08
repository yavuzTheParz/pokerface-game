using System.Collections.Generic;
using UnityEngine;

public class PlayerHand : MonoBehaviour
{
    public List<CardData> hand = new List<CardData>();

    public void AddCard(CardData card)
    {
        hand.Add(card);
    }

    public CardData GetBiggestCard()
    {
        CardData biggest = hand[0];

        foreach (var card in hand)
        {
            if (card.value > biggest.value)
                biggest = card;
        }

        return biggest;
    }

    public void RemoveCard(CardData card)
    {
        hand.Remove(card);
    }
}