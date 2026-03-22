// CardData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "NewCard", menuName = "Pokerface/Card Data")]
public class CardData : ScriptableObject
{
    public CardElement element;
    [Range(0, 9)] public int value;
    public CardType cardType = CardType.Normal;
    public Sprite cardSprite;
    public Sprite additionalSprite;
    public Sprite wheel;
    public Color elementColor;

    // Element karşıtlığı (Kurban kartı için)
    public CardElement GetOppositeElement()
    {
        return element switch
        {
            CardElement.Water  => CardElement.Fire,
            CardElement.Air    => CardElement.Earth,
            CardElement.Earth  => CardElement.Water,
            CardElement.Fire   => CardElement.Air,
            _ => element
        };
    }
}

// CardElement.cs
public enum CardElement { Fire, Water, Earth, Air }

// CardType.cs
public enum CardType { Normal, Thief, Curse, Sacrifice }
