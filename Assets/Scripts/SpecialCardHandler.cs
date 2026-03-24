// SpecialCardHandler.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpecialCardHandler : MonoBehaviour
{
    CardManager cm;

    // Aktif lanetleri tut (birden fazla lanet aynı anda var olabilir)
    readonly List<CurseCondition> activeCurses = new();

    // İstiflenen hırsız talepleri: (hedef, istenenKartSayısı)
    readonly Stack<(string targetId, int cardCount)> thiefStack = new();

    void Awake() => cm = GetComponent<CardManager>();

    // ── Hırsız Kartı ────────────────────────────────────────────

    // Oyuncu hırsız kartını kullanır
    public void UseThiefCard(string userId, string targetId, CardData wantedCard, int stackCount = 1)
    {
        // İstiflenmiş talep sayısı kadar kart talep et
        for (int i = 0; i < stackCount; i++)
            thiefStack.Push((targetId, 1));

        ResolveThiefStack(userId, wantedCard);
    }

    // Hırsız kartını bir başkasına yansıt (katlayarak)
    public void RedirectThief(string fromId, string toId, CardData wantedCard)
    {
        if (thiefStack.Count == 0)
        {
            Debug.LogWarning("Yansıtılacak hırsız talebi yok.");
            return;
        }

        var (_, count) = thiefStack.Pop();
        // Katlanır: bir sonraki yansıtmada 2 kart talep edilecek
        thiefStack.Push((toId, count * 2));

        OnThiefRedirected?.Invoke(fromId, toId, count * 2);
    }

    void ResolveThiefStack(string ultimateRequester, CardData wantedCard)
    {
        int totalCards = 0;
        string finalTarget = "";

        while (thiefStack.Count > 0)
        {
            var (targetId, count) = thiefStack.Pop();
            finalTarget  = targetId;
            totalCards  += count;
        }

        // Tüm istiflenen kartları son hedeften al
        var targetHand = cm.GetHand(finalTarget);
        var taken      = new List<Card>();

        var candidates = targetHand.Cards
            .Where(c => c.data == wantedCard && !c.isInSequence)
            .Take(totalCards)
            .ToList();

        foreach (var card in candidates)
        {
            targetHand.RemoveCard(card);
            card.ownerPlayerId = ultimateRequester;
            cm.GetHand(ultimateRequester).AddCard(card);
            taken.Add(card);
        }

        OnThiefResolved?.Invoke(ultimateRequester, finalTarget, taken);
    }

    // ── Lanet Kartı ─────────────────────────────────────────────

    public void UseCurseCard(string curserId, string cursedId, CurseCondition condition)
    {
        condition.curserId = curserId;
        condition.cursedId = cursedId;
        activeCurses.Add(condition);

        // Sadece laneti yapan koşulu bilir; lanetlenen bilmez
        OnCursePlaced?.Invoke(curserId, cursedId);

        Debug.Log($"[Lanet] {curserId} → {cursedId} | " +
                  $"Element: {condition.requiredElement}, Değer: {condition.requiredValue}");
    }

    // Her dizi kurulduğunda çağrılır — laneti kontrol eder// SpecialCardHandler.cs — CheckCurses ve tur geçişi güncelle

// Mevcut CheckCurses metodunu güncelle
public void CheckCurses(string playerId, CardSequence newSequence)
{
    var triggered = activeCurses
        .FindAll(c => c.cursedId == playerId && c.IsTriggeredBy(newSequence));

    foreach (var curse in triggered)
    {
        TransferSequenceToCurser(curse, newSequence);
        activeCurses.Remove(curse);
    }
}

// Yeni metod — her tur geçişinde çağrılır
public void OnTurnPassed()
{
    var expired = new List<CurseCondition>();

    foreach (var curse in activeCurses)
    {
        curse.OnTurnPassed();
        if (!curse.IsActive)
        {
            expired.Add(curse);
            OnCurseExpired?.Invoke(curse.cursedId);
        }
    }

    foreach (var c in expired)
        activeCurses.Remove(c);
}

public event System.Action<string> OnCurseExpired;

    void TransferSequenceToCurser(CurseCondition curse, CardSequence sequence)
    {
        var cursedHand  = cm.GetHand(curse.cursedId);
        var curserHand  = cm.GetHand(curse.curserId);

        // Diziyi lanetlenen oyuncudan kaldır, lanetleyene ver
        cursedHand.Sequences.Remove(sequence);
        curserHand.Sequences.Add(sequence);

        foreach (var card in sequence.Cards)
            card.ownerPlayerId = curse.curserId;

        OnCurseTriggered?.Invoke(curse.curserId, curse.cursedId, sequence);
    }

    // ── Kurban Etme Kartı ───────────────────────────────────────

    public void UseSacrificeCard(string sacrificerId, string targetId, List<Card> sacrificedCards)
    {
        var targetHand     = cm.GetHand(targetId);
        var sacrificerHand = cm.GetHand(sacrificerId);

        // Feda edilen kartların toplam değeri
        int sacrificeValue = sacrificedCards.Sum(c => c.Value);

        // Feda edilen kartların elementinin karşıtını bul
        var oppositeElements = sacrificedCards
            .Select(c => c.data.GetOppositeElement())
            .Distinct()
            .ToList();

        // Öncelik: dizideki kartları yok et
        var toDestroy = targetHand.Cards
            .Where(c => oppositeElements.Contains(c.Element) && c.isInSequence)
            .OrderByDescending(c => c.Value)
            .ToList();

        // Dizideki yoksa el kartlarından yok et
        if (toDestroy.Count == 0)
        {
            toDestroy = targetHand.Cards
                .Where(c => oppositeElements.Contains(c.Element))
                .OrderByDescending(c => c.Value)
                .ToList();
        }

        // Kurban değeri kadar (veya biraz az) kart yok et
        int destroyed = 0;
        var actuallyDestroyed = new List<Card>();

        foreach (var card in toDestroy)
        {
            if (destroyed >= sacrificeValue) break;
            targetHand.RemoveCard(card);
            // Dizideyse diziyi de temizle
            foreach (var seq in targetHand.Sequences.ToList())
            {
                if (seq.Cards.Contains(card))
                {
                    seq.Cards.Remove(card);
                    if (seq.Cards.Count < 4) // Dizi bozulduysa
                    {
                        // Kalan kartları ele geri ver
                        foreach (var remaining in seq.Cards)
                        {
                            remaining.isInSequence = false;
                            targetHand.AddCard(remaining);
                        }
                        targetHand.Sequences.Remove(seq);
                    }
                }
            }
            actuallyDestroyed.Add(card);
            cm.Deck.Discard(card);
            destroyed += card.Value;
        }

        // Feda edilen kartları kurbanın elinden çıkar
        foreach (var card in sacrificedCards)
        {
            sacrificerHand.RemoveCard(card);
            cm.Deck.Discard(card);
        }

        OnSacrificeResolved?.Invoke(sacrificerId, targetId, sacrificedCards, actuallyDestroyed);
    }

    // ── Olaylar ─────────────────────────────────────────────────

    public event System.Action<string, string, int>                       OnThiefRedirected;
    public event System.Action<string, string, List<Card>>                OnThiefResolved;
    public event System.Action<string, string>                            OnCursePlaced;
    public event System.Action<string, string, CardSequence>              OnCurseTriggered;
    public event System.Action<string, string, List<Card>, List<Card>>    OnSacrificeResolved;
}