// UIManager.cs
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void OnCardSelected(Card card)
    {
        Debug.Log($"Seçildi: {card.Element} {card.Value}");
        // İleride dizi seçim mantığı buraya gelecek
    }
    // UIManager.cs — mevcut dosyaya ekle
    public void ShowTurnIndicator(string playerId, int turnNumber) =>
        Debug.Log($"Sıra: {playerId} | Tur: {turnNumber}");

    public void ShowCardRequest(string from, string to, string cardName) =>
        Debug.Log($"{from} → {to} : {cardName} istedi");

    public void ShowSequenceFormed(string playerId, string[] cardNames) =>
        Debug.Log($"{playerId} dizi kurdu: {string.Join(", ", cardNames)}");

    public void ShowCurseFeedback(string message) =>
        Debug.Log($"[Lanet] {message}");

    public void ShowThiefResult(string from, string to, string[] cards) =>
        Debug.Log($"[Hırsız] {from} → {to} : {string.Join(", ", cards)}");

    public void ShowSacrificeResult(string from, string to,
        string[] sacrificed, string[] destroyed) =>
        Debug.Log($"[Kurban] {from} → {to}");
}