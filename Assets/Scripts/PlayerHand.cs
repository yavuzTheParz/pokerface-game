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

        // Seçili kartlar hangi dizilerde? Onları boz
        var affectedSequences = new List<CardSequence>();
        foreach (var card in selectedCards)
        {
            foreach (var seq in Sequences)
            {
                if (seq.Cards.Contains(card) && !affectedSequences.Contains(seq))
                    affectedSequences.Add(seq);
            }
        }

        // Etkilenen dizileri boz — kartları ele geri ver
        foreach (var seq in affectedSequences)
        {
            foreach (var card in seq.Cards)
            {
                card.isInSequence = false;
                // Seçili kartlar zaten yeni diziye gidecek,
                // diğerleri ele döner
                if (!selectedCards.Contains(card))
                    Cards.Add(card);
            }
            seq.Cards.Clear();
            Sequences.Remove(seq);
        }

        // Yeni diziyi kur
        var newSeq = new CardSequence();
        foreach (var card in selectedCards)
        {
            Cards.Remove(card); // elden çıkar (dizide olanlar zaten elde değil ama guard olarak)
            newSeq.AddCard(card);
        }
        Sequences.Add(newSeq);
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

    // Oyuncu kart talep edebilir mi? (kendi eline dizi oluşturmalı)// PlayerHand.cs — CanRequestCard'ı tamamen yeniden yaz
    public bool CanRequestCard(CardData requested)
    {
        var testCards = new List<Card>(Cards);
        testCards.Add(new Card(requested, PlayerId));

        // 3 veya daha fazla kartla potansiyel dizi var mı?
        for (int i = 0; i < testCards.Count - 2; i++)
        for (int j = i + 1; j < testCards.Count - 1; j++)
        for (int k = j + 1; k < testCards.Count; k++)
        {
            var trio = new List<Card>
            {
                testCards[i], testCards[j], testCards[k]
            };

            // İstenen kart bu grupta olmalı
            if (!trio.Exists(c => c.data == requested)) continue;
            if (trio.Exists(c => c.isInSequence)) continue;

            // Bu 3 kart aynı element mi? (ardışık dizi için potansiyel)
            bool sameElement = trio.TrueForAll(c => c.Element == trio[0].Element);
            if (sameElement) return true;

            // Aynı değer mi? (4 elementli dizi için potansiyel)
            bool sameValue = trio.TrueForAll(c => c.Value == trio[0].Value);
            if (sameValue) return true;

            // Ardışık değerler mi?
            var vals = trio.ConvertAll(c => c.Value);
            vals.Sort();
            bool consecutive = vals[1] == vals[0] + 1 && vals[2] == vals[1] + 1;
            if (consecutive) return true;
        }
        return false;
    }
}