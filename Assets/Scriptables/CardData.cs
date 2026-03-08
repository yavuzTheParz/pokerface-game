using UnityEngine;

[CreateAssetMenu(fileName = "NewCard", menuName = "Poker/Card")]
public class CardData : ScriptableObject
{
    public string cardName;
    public int value;
    public Sprite artwork;
}