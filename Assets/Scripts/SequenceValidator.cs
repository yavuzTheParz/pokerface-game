// SequenceValidator.cs
using System.Collections.Generic;
using System.Linq;

public static class SequenceValidator
{
    // Ana doğrulama giriş noktası
    public static bool IsValidSequence(List<Card> cards, out CardSequence.SequenceType type)
    {
        type = CardSequence.SequenceType.SameColor;

        if (cards == null || cards.Count < 4) return false;

        if (IsSameColorConsecutive(cards))
        { type = CardSequence.SequenceType.SameColor;  return true; }

        if (IsSameValueAllElements(cards))
        { type = CardSequence.SequenceType.SameValue;  return true; }

        if (IsElementCombo(cards))
        { type = CardSequence.SequenceType.ElementCombo; return true; }

        return false;
    }

    // Aynı elementte ardışık 4 kart (örn: Fire 3,4,5,6)
    static bool IsSameColorConsecutive(List<Card> cards)
    {
        if (cards.Count < 4) return false;
        var element = cards[0].Element;
        if (cards.Any(c => c.Element != element)) return false;

        var values = cards.Select(c => c.Value).OrderBy(v => v).ToList();
        for (int i = 1; i < values.Count; i++)
            if (values[i] != values[i - 1] + 1) return false;

        return true;
    }

    // Farklı elementlerde aynı sayı (örn: Fire 5, Water 5, Earth 5, Air 5)
    static bool IsSameValueAllElements(List<Card> cards)
    {
        if (cards.Count < 4) return false;
        int val = cards[0].Value;
        if (cards.Any(c => c.Value != val)) return false;

        // 4 farklı element olmalı
        var elements = cards.Select(c => c.Element).Distinct().ToList();
        return elements.Count == 4;
    }

    // Özel kombo: Fire+Water+Earth+Air ardışık sayı (örn: 3,4,5,6 farklı elementlerde)
    static bool IsElementCombo(List<Card> cards)
    {
        if (cards.Count < 4) return false;

        var elements = cards.Select(c => c.Element).Distinct().ToList();
        if (elements.Count != 4) return false; // 4 element de olmalı

        var values = cards.Select(c => c.Value).OrderBy(v => v).ToList();
        for (int i = 1; i < values.Count; i++)
            if (values[i] != values[i - 1] + 1) return false;

        return true;
    }
}