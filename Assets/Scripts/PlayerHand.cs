// PlayerHand.cs
using System.Collections.Generic;
using System.Linq;

public class PlayerHand
{
    public string PlayerId { get; }
    public List<Card> Cards { get; } = new();
    public List<CardSequence> Sequences { get; } = new();
    public int TotalScore => Sequences.Sum(s => s.ScoreValue);

    public PlayerHand(string playerId) => PlayerId = playerId;

    public void AddCard(Card card) => Cards.Add(card);

    public bool RemoveCard(Card card) => Cards.Remove(card);

    // Seçili kartları doğrula ve diziye ekle
    public bool TryFormSequence(List<Card> selectedCards)
    {
        if (!SequenceValidator.IsValidSequence(selectedCards, out var type))
            return false;

        var seq = new CardSequence();
        foreach (var c in selectedCards)
        {
            Cards.Remove(c);
            seq.AddCard(c);
        }
        Sequences.Add(seq);
        return true;
    }

    // En büyük diziyi at (Pokerface gülerse)
    public CardSequence RemoveLargestSequence()
    {
        if (Sequences.Count == 0) return null;
        var largest = Sequences.OrderByDescending(s => s.ScoreValue).First();
        Sequences.Remove(largest);
        largest.RemoveAll();
        return largest;
    }

    // Oyuncu kart talep edebilir mi? (kendi eline dizi oluşturmalı)
    public bool CanRequestCard(CardData requested)
    {
        // İstenen kart, eldeki kartlarla geçerli bir dizi kurabilmeli
        var testHand = new List<Card>(Cards) { new Card(requested, PlayerId) };
        for (int i = 0; i < testHand.Count - 3; i++)
        {
            for (int j = i + 1; j < testHand.Count - 2; j++)
            {
                for (int k = j + 1; k < testHand.Count - 1; k++)
                {
                    for (int l = k + 1; l < testHand.Count; l++)
                    {
                        var combo = new List<Card> { testHand[i], testHand[j], testHand[k], testHand[l] };
                        if (combo.Contains(testHand[^1]) &&
                            SequenceValidator.IsValidSequence(combo, out _))
                            return true;
                    }
                }
            }
        }
        return false;
    }
}