// Card.cs
using System;

[Serializable]
public class Card
{
    public CardData data;
    public string ownerPlayerId;   // Photon ActorNumber string'i
    public bool isInSequence;      // Diziye girdi mi?

    public Card(CardData data, string ownerId)
    {
        this.data = data;
        this.ownerPlayerId = ownerId;
        this.isInSequence = false;
    }

    public int Value    => data.value;
    public CardElement Element => data.element;
    public CardType Type       => data.cardType;
}