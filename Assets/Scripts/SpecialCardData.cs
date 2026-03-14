// SpecialCardData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "NewSpecialCard", menuName = "Pokerface/Special Card Data")]
public class SpecialCardData : CardData
{
    [Header("Özel Kart Ayarları")]
    public SpecialCardEffectType effectType;
    public int stackCount = 1; // Hırsız: kaç kez katlanabilir
}

public enum SpecialCardEffectType { Thief, Curse, Sacrifice }