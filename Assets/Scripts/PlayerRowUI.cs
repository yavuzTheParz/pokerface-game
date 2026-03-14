// PlayerRowUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerRowUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI statusText;
    [SerializeField] Image           background;

    public void Setup(string nickname, bool isReady, bool isHost)
    {
        nameText.text   = isHost ? $"{nickname} (Host)" : nickname;
        statusText.text = isReady ? "Hazır" : "Bekliyor...";
        statusText.color = isReady
            ? new Color(.2f, .7f, .3f)
            : new Color(.7f, .7f, .7f);
    }
}
