// CardSequence.cs
using System.Collections.Generic;
using System.Linq;

public class CardSequence
{
    public List<Card> Cards { get; private set; } = new();
    public SequenceType Type { get; private set; }

    public enum SequenceType { SameColor, SameValue, ElementCombo }

    public int ScoreValue => Cards.Sum(c => c.Value == 0 ? 1 : c.Value);

    public void AddCard(Card card)
    {
        card.isInSequence = true;
        Cards.Add(card);
    }

    public void RemoveAll()
    {
        foreach (var c in Cards) c.isInSequence = false;
        Cards.Clear();
    }
}