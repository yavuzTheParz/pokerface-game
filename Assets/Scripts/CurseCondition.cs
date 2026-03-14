// CurseCondition.cs
using System;

[Serializable]
public class CurseCondition
{
    public string curserId;       // Laneti yapan
    public string cursedId;       // Lanetlenen

    // Koşul: belirli bir element + belirli bir değer içeren dizi kurarsa
    public CardElement? requiredElement; // null = herhangi
    public int?        requiredValue;   // null = herhangi
    public bool        mustBeConsecutive;

    // Lanet aktif mi?
    public bool IsActive { get; private set; } = true;

    public void Deactivate() => IsActive = false;

    // Kurulan diziyi koşulla karşılaştır
    public bool IsTriggedBy(CardSequence sequence)
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