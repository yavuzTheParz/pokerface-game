// CardManager.cs
using System.Collections.Generic;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    public static CardManager Instance { get; private set; }

    [Header("Kart Varlıkları")]
    [SerializeField] List<CardData> allNormalCards;
    [SerializeField] List<CardData> allSpecialCards;

    [Header("Referanslar")]
    [SerializeField] SpecialCardHandler specialCardHandler;

    public Deck Deck { get; private set; } = new();
    Dictionary<string, PlayerHand> playerHands = new();
    public IReadOnlyList<CardData> AllCardData => allNormalCards;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Başlangıç ───────────────────────────────────────────────

    public void InitializeGame(List<string> playerIds)
    {
        var allCards = new List<CardData>(allNormalCards);
        allCards.AddRange(allSpecialCards);
        Deck.Initialize(allCards);

        playerHands.Clear();
        foreach (var id in playerIds)
        {
            playerHands[id] = new PlayerHand(id);
            DealStartingHand(id, 5); // Başlangıçta 5 kart
        }
    }

    void DealStartingHand(string playerId, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var card = Deck.Draw();
            if (card == null) return;
            card.ownerPlayerId = playerId;
            playerHands[playerId].AddCard(card);
        }
    }

    // ── Tur Aksiyonları ─────────────────────────────────────────

    // Tek turda: ortadan kart ver
    public Card DrawFromDeck(string playerId)
    {
        var card = Deck.Draw();
        if (card == null) return null;
        card.ownerPlayerId = playerId;
        playerHands[playerId].AddCard(card);
        OnCardDrawn?.Invoke(playerId, card);
        return card;
    }

    // Çift turda: başka oyuncudan kart iste
    // Döner: gerçekten alındı mı (ortadan mı, elden mi)
    public CardRequestResult RequestCard(string requesterId, CardData requestedData)
    {
        var requester = GetHand(requesterId);

        if (!requester.CanRequestCard(requestedData))
            return CardRequestResult.InvalidRequest;

        // Tüm oyuncuları tara — 2+ aynı kart var mı?
        foreach (var kvp in playerHands)
        {
            if (kvp.Key == requesterId) continue;

            var matching = kvp.Value.Cards.FindAll(
                c => c.data == requestedData && !c.isInSequence);

            if (matching.Count >= 2)
            {
                var taken = matching[0];
                kvp.Value.RemoveCard(taken);
                taken.ownerPlayerId = requesterId;
                requester.AddCard(taken);
                OnCardRequestedFromPlayer?.Invoke(requesterId, kvp.Key, taken);
                return CardRequestResult.TakenFromPlayer;
            }
        }

        // Kimde 2+ yoksa desteden ver
        var card = Deck.Draw();
        if (card == null) return CardRequestResult.DeckEmpty;
        card.ownerPlayerId = requesterId;
        requester.AddCard(card);
        OnCardDrawnFromDeck?.Invoke(requesterId, card);
        return CardRequestResult.TakenFromDeck;
    }

    // Dizi kurma girişimi
    public bool TryFormSequence(string playerId, List<Card> selectedCards)
    {
        var hand = GetHand(playerId);
        bool success = hand.TryFormSequence(selectedCards);
        if (success)
        {
            var newSeq = hand.Sequences[^1];
            OnSequenceFormed?.Invoke(playerId, newSeq);

            // Lanet kontrolü — YENİ SATIR
            specialCardHandler.CheckCurses(playerId, newSeq);

            CheckWinCondition(playerId);
        }
        return success;
    }


    // ── Kazanma ─────────────────────────────────────────────────

    void CheckWinCondition(string playerId)
    {
        if (GetHand(playerId).TotalScore >= 100)
            OnPlayerWon?.Invoke(playerId);
    }

    // ── Yardımcılar ─────────────────────────────────────────────

    public PlayerHand GetHand(string playerId) =>
        playerHands.TryGetValue(playerId, out var h) ? h : null;

    public List<Card> GetHandCards(string playerId) =>
        GetHand(playerId)?.Cards ?? new List<Card>();

    // ── Olaylar (UI ve Network bunları dinler) ───────────────────

    public event System.Action<string, Card>         OnCardDrawn;
    public event System.Action<string, string, Card> OnCardRequestedFromPlayer;
    public event System.Action<string, Card>         OnCardDrawnFromDeck;
    public event System.Action<string, CardSequence> OnSequenceFormed;
    public event System.Action<string>               OnPlayerWon;
}

public enum CardRequestResult { TakenFromPlayer, TakenFromDeck, InvalidRequest, DeckEmpty }