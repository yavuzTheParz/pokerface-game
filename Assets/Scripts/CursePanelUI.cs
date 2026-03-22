// CursePanelUI.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CursePanelUI : MonoBehaviour
{
    [SerializeField] Transform  playerButtonContainer;
    [SerializeField] GameObject playerButtonPrefab;
    [SerializeField] Transform  elementButtonContainer;
    [SerializeField] GameObject elementButtonPrefab;
    [SerializeField] Transform  valueButtonContainer;
    [SerializeField] GameObject valueButtonPrefab;
    [SerializeField] Button     confirmBtn;
    [SerializeField] Button     cancelBtn;
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI summaryText;

    string      selectedTargetId;
    CardElement? selectedElement;
    int?        selectedValue;

    void OnEnable()
    {
        selectedTargetId = null;
        selectedElement  = null;
        selectedValue    = null;
        confirmBtn.interactable = false;
        summaryText.text = "Hedef ve koşul seç";
        titleText.text   = "Kimi lanetleyeceksin?";

        confirmBtn.onClick.AddListener(OnConfirm);
        cancelBtn.onClick.AddListener(() => gameObject.SetActive(false));

        BuildPlayerList();
        BuildElementList();
        BuildValueList();
    }

    void OnDisable()
    {
        confirmBtn.onClick.RemoveAllListeners();
        cancelBtn.onClick.RemoveAllListeners();
    }

    void BuildPlayerList()
    {
        foreach (Transform t in playerButtonContainer) Destroy(t.gameObject);
        string localId = NetworkManager.Instance.LocalPlayerId;

        foreach (var pid in NetworkManager.Instance.GetPlayerIds())
        {
            if (pid == localId) continue;

            // Nickname bul
            string nickname = pid;
            foreach (var p in Photon.Pun.PhotonNetwork.PlayerList)
                if (p.ActorNumber.ToString() == pid)
                    nickname = p.NickName;

            var go  = Instantiate(playerButtonPrefab, playerButtonContainer);
            var btn = go.GetComponent<Button>();
            go.GetComponentInChildren<TextMeshProUGUI>().text = nickname;
            var captured = pid;
            btn.onClick.AddListener(() => SelectTarget(captured));
        }
    }

    void BuildElementList()
    {
        foreach (Transform t in elementButtonContainer) Destroy(t.gameObject);

        foreach (CardElement elem in System.Enum.GetValues(typeof(CardElement)))
        {
            var go  = Instantiate(elementButtonPrefab, elementButtonContainer);
            var btn = go.GetComponent<Button>();
            go.GetComponentInChildren<TextMeshProUGUI>().text = elem.ToString();
            var captured = elem;
            btn.onClick.AddListener(() => SelectElement(captured));
        }

        // "Herhangi element" seçeneği
        var anyGo  = Instantiate(elementButtonPrefab, elementButtonContainer);
        var anyBtn = anyGo.GetComponent<Button>();
        anyGo.GetComponentInChildren<TextMeshProUGUI>().text = "Herhangi";
        anyBtn.onClick.AddListener(() => SelectElement(null));
    }

    void BuildValueList()
    {
        foreach (Transform t in valueButtonContainer) Destroy(t.gameObject);

        for (int v = 0; v <= 9; v++)
        {
            var go  = Instantiate(valueButtonPrefab, valueButtonContainer);
            var btn = go.GetComponent<Button>();
            go.GetComponentInChildren<TextMeshProUGUI>().text = v.ToString();
            var captured = v;
            btn.onClick.AddListener(() => SelectValue(captured));
        }

        // "Herhangi değer" seçeneği
        var anyGo  = Instantiate(valueButtonPrefab, valueButtonContainer);
        var anyBtn = anyGo.GetComponent<Button>();
        anyGo.GetComponentInChildren<TextMeshProUGUI>().text = "Herhangi";
        anyBtn.onClick.AddListener(() => SelectValue(null));
    }

    void SelectTarget(string targetId)
    {
        selectedTargetId = targetId;
        UpdateSummary();
        UpdateConfirmButton();
    }

    void SelectElement(CardElement? element)
    {
        selectedElement = element;
        UpdateSummary();
        UpdateConfirmButton();
    }

    void SelectValue(int? value)
    {
        selectedValue = value;
        UpdateSummary();
        UpdateConfirmButton();
    }

    void UpdateSummary()
    {
        string target  = selectedTargetId ?? "?";
        string element = selectedElement?.ToString() ?? "Herhangi element";
        string value   = selectedValue?.ToString()   ?? "herhangi değer";
        summaryText.text = $"{target} → {element} + {value} içeren dizi kurarsa";
    }

    void UpdateConfirmButton()
    {
        // Hedef seçilmişse ve en az element veya değer seçilmişse aktif
        confirmBtn.interactable = selectedTargetId != null
            && (selectedElement != null || selectedValue != null);
    }

    void OnConfirm()
    {
        var condition = new CurseCondition
        {
            requiredElement = selectedElement,
            requiredValue   = selectedValue,
            turnsRemaining  = 5
        };

        string localId = NetworkManager.Instance.LocalPlayerId;
        GetComponent<SpecialCardHandler>()?.UseCurseCard(
            localId, selectedTargetId, condition);

        gameObject.SetActive(false);
        GameUIManager.Instance.RefreshHand();
    }
}