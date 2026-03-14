// Deck.cs
using System.Collections.Generic;
using UnityEngine;

public class Deck
{
    readonly List<Card> drawPile = new();
    readonly List<Card> discardPile = new();

    // Tüm CardData asset'lerini Inspector'dan ver
    public void Initialize(List<CardData> allCardData, string noOwner = "DECK")
    {
        drawPile.Clear();
        foreach (var data in allCardData)
            drawPile.Add(new Card(data, noOwner));
        Shuffle();
    }

    public void Shuffle()
    {
        for (int i = drawPile.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (drawPile[i], drawPile[j]) = (drawPile[j], drawPile[i]);
        }
    }

    public Card Draw()
    {
        if (drawPile.Count == 0) Reshuffle();
        if (drawPile.Count == 0) return null;
        var card = drawPile[^1];
        drawPile.RemoveAt(drawPile.Count - 1);
        return card;
    }

    public void Discard(Card card) => discardPile.Add(card);

    void Reshuffle()
    {
        drawPile.AddRange(discardPile);
        discardPile.Clear();
        Shuffle();
    }

    public int RemainingCards => drawPile.Count;
}