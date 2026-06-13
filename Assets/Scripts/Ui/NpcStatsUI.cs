using UnityEngine;
using TMPro;

public class NpcStatsUI : MonoBehaviour
{
    public static NpcStatsUI Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    [Header("UI References")]
    [SerializeField] private TMP_Text favorabilityText;
    [SerializeField] private TMP_Text reputationText;

    private void Start()
    {
        Hide();
    }

    // Cette méthode peut être appelée directement par votre Manager
    public void UpdateMetrics(int favorability, int reputation)
    {
        if (favorabilityText != null)
        {
            favorabilityText.text = $"Favorabilité : {favorability}";
            favorabilityText.gameObject.SetActive(true);
        }

        if (reputationText != null)
        {
            reputationText.text = $"Réputation : {reputation}";
            reputationText.gameObject.SetActive(true); // Toujours visible
        }
    }

    public void Hide()
    {
        if (favorabilityText != null) favorabilityText.gameObject.SetActive(false);
        // On ne cache pas la réputation, ni l'objet parent, car la réputation est globale
    }
}
