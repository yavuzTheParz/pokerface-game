// CurseCondition.cs
using System;

[Serializable]
public class CurseCondition
{
    public string curserId;
    public string cursedId;

    public CardElement? requiredElement; // null = herhangi element
    public int?        requiredValue;   // null = herhangi değer
    public int         turnsRemaining = 5;
    public bool        IsActive => turnsRemaining > 0;

    public void OnTurnPassed() => turnsRemaining--;

    public bool IsTriggeredBy(CardSequence sequence)
    {
        if (!IsActive) return false;

        foreach (var card in sequence.Cards)
        {
            bool elementMatch = requiredElement == null || card.Element == requiredElement;
            bool valueMatch   = requiredValue   == null || card.Value   == requiredValue;
            if (elementMatch && valueMatch) return true;
        }
        return false;
    }
}