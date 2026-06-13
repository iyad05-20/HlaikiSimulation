using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class DialoguePanel : MonoBehaviour
{
    [Header("Screen Space UI")]
    public GameObject npcInfoPanel;
    public TextMeshProUGUI txtNom;
    public TextMeshProUGUI txtRole;
    public Image avatarImage;

    [Header("World Space Bulles")]
    [HideInInspector] public GameObject currentBulleNPC;
    [HideInInspector] public TextMeshProUGUI currentTxtBulleNPC;
    public GameObject bullePlayer;
    public TextMeshProUGUI txtBullePlayer;

    [Header("Input")]
    public GameObject inputPanel;
    public TMP_InputField playerInputField;

    [Header("Typewriter")]
    public float typewriterSpeed = 0.03f;

    private Coroutine typewriterCoroutine;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    // ─── Show / Hide ───────────────────────────────────────

    public void ShowPanel(string npcName = null, string npcRole = null, Sprite avatar = null, GameObject npcBulle = null, TextMeshProUGUI npcTxtBulle = null)
    {
        currentBulleNPC = npcBulle;
        currentTxtBulleNPC = npcTxtBulle;

        npcInfoPanel.SetActive(true);
        inputPanel.SetActive(true);
        if (currentBulleNPC != null) currentBulleNPC.SetActive(true);
        if (bullePlayer != null) bullePlayer.SetActive(true);

        txtNom.text = npcName != null ? npcName.ToUpper() : "";
        txtRole.text = npcRole;

        if (avatar != null)
            avatarImage.sprite = avatar;

        playerInputField.text = "";
        playerInputField.ActivateInputField();
    }

    public void HidePanel()
    {
        npcInfoPanel.SetActive(false);
        inputPanel.SetActive(false);
        if (currentBulleNPC != null) currentBulleNPC.SetActive(false);
        if (bullePlayer != null) bullePlayer.SetActive(false);
        playerInputField.text = "";
    }
    void LateUpdate()
    {
        if (mainCamera != null)
        {
            if (currentBulleNPC != null) currentBulleNPC.transform.forward = mainCamera.transform.forward;
            if (bullePlayer != null) bullePlayer.transform.forward = mainCamera.transform.forward;
        }
    }

    // ─── Display Dialogue ──────────────────────────────────

    public void DisplayNPCDialogue(string text)
    {
        if (typewriterCoroutine != null)
            StopCoroutine(typewriterCoroutine);

        if (currentTxtBulleNPC != null)
        {
            AdjustBubbleSize(currentTxtBulleNPC, currentBulleNPC, text);
            typewriterCoroutine = StartCoroutine(TypewriterEffect(currentTxtBulleNPC, text));
        }
    }

    public void DisplayPlayerMessage(string text)
    {
        if (txtBullePlayer != null)
        {
            AdjustBubbleSize(txtBullePlayer, bullePlayer, text);
            txtBullePlayer.text = text;
        }
        else
        {
            Debug.LogError("[DialoguePanel] txtBullePlayer is not assigned in the Inspector!");
        }
    }

    private void AdjustBubbleSize(TextMeshProUGUI txt, GameObject bulleObj, string fullText)
    {
        if (txt == null || bulleObj == null) return;

        // Si vous utilisez ContentSizeFitter, on force l'actualisation
        ContentSizeFitter fitter = bulleObj.GetComponent<ContentSizeFitter>();
        if (fitter != null)
        {
            string oldText = txt.text;
            txt.text = fullText;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(bulleObj.GetComponent<RectTransform>());
            txt.text = oldText;
            return;
        }

        // Sinon, redimensionnement manuel de la hauteur de la bulle
        RectTransform txtRect = txt.GetComponent<RectTransform>();
        RectTransform bulleRect = bulleObj.GetComponent<RectTransform>();
        
        if (txtRect != null && bulleRect != null)
        {
            // Calculer la hauteur nécessaire pour le texte complet
            Vector2 preferredSize = txt.GetPreferredValues(fullText, txtRect.rect.width, 0f);
            
            // Padding pour que le texte ne touche pas les bords (à ajuster si besoin)
            float paddingY = 50f; 
            
            // On met à jour uniquement la hauteur de la bulle
            bulleRect.sizeDelta = new Vector2(bulleRect.sizeDelta.x, preferredSize.y + paddingY);
        }
    }

    // ─── Typewriter ────────────────────────────────────────

    private IEnumerator TypewriterEffect(TextMeshProUGUI target, string fullText)
    {
        target.text = "";
        foreach (char c in fullText)
        {
            target.text += c;
            yield return new WaitForSeconds(typewriterSpeed);
        }
    }
}