using UnityEngine;
using TMPro;
using UnityEngine.UI;
using JemaaGame.NPC;

namespace JemaaGame.UI
{
    /// <summary>
    /// Gère l'affichage des résultats de la Halka à la fin du cycle.
    /// Lit les données depuis HalkaOrchestrator.LastResult.
    /// </summary>
    public class HalkaResultDisplay : MonoBehaviour
    {
        [Header("UI References - Score")]
        [SerializeField] private TextMeshProUGUI gradeText; // S, A, B, C, D, E
        [SerializeField] private TextMeshProUGUI totalScoreText;
        [SerializeField] private TextMeshProUGUI detailsText; // Cohérence, Profondeur, Audience

        [Header("UI References - Narration")]
        [SerializeField] private TextMeshProUGUI narrationText;
        [SerializeField] private UnityEngine.UI.ScrollRect narrationScrollRect;
        [SerializeField] private TextMeshProUGUI reactionText;

        [Header("UI References - Navigation")]
        [SerializeField] private Button continueButton;

        private void Start()
        {
            // Débloquer et afficher la souris pour pouvoir cliquer sur l'interface
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueClicked);
            }

            DisplayResults();
        }

        private void DisplayResults()
        {
            // Récupérer le résultat de la dernière Halka
            var result = HalkaOrchestrator.LastResult;

            if (result == null)
            {
                Debug.LogWarning("[HalkaResultDisplay] Aucun résultat trouvé. Affichage par défaut.");
                if (gradeText != null) gradeText.text = "?";
                if (narrationText != null) narrationText.text = "La place Jemaa el-Fna est silencieuse ce soir...";
                return;
            }

            // Calcul du Grade (S, A, B, C, D, E)
            string grade = CalculateGrade(result.totalScore);
            
            // Mise à jour des textes
            if (gradeText != null) 
            {
                gradeText.text = grade;
                SetGradeColor(grade);
            }

            if (totalScoreText != null) 
                totalScoreText.text = $"Score Total : {result.totalScore}";

            if (detailsText != null)
            {
                detailsText.text = $"Cohérence : {result.coherenceScore} | " +
                                   $"Profondeur : {result.depthScore} | " +
                                   $"Audience : {result.audienceSize}";
            }

            if (reactionText != null)
                reactionText.text = result.reactionLabel;

            if (narrationText != null)
            {
                narrationText.text = result.narration;
                
                // Si on a un ScrollRect, on force la barre de défilement tout en haut
                if (narrationScrollRect != null)
                {
                    Canvas.ForceUpdateCanvases(); // Force Unity à recalculer la taille du texte
                    narrationScrollRect.verticalNormalizedPosition = 1f;
                }
            }
        }

        private string CalculateGrade(int score)
        {
            if (score >= 100) return "S";
            if (score >= 80) return "A";
            if (score >= 60) return "B";
            if (score >= 40) return "C";
            if (score >= 20) return "D";
            return "E";
        }

        private void SetGradeColor(string grade)
        {
            if (gradeText == null) return;

            switch (grade)
            {
                case "S": gradeText.color = new Color(1f, 0.84f, 0f); break; // Doré
                case "A": gradeText.color = new Color(0.2f, 0.8f, 0.2f); break; // Vert
                case "B": gradeText.color = new Color(0.2f, 0.6f, 1f); break; // Bleu
                case "C": gradeText.color = new Color(1f, 0.6f, 0f); break; // Orange
                case "D": gradeText.color = new Color(0.8f, 0.4f, 0f); break; // Marron clair
                case "E": gradeText.color = new Color(0.8f, 0.2f, 0.2f); break; // Rouge
            }
        }

        private void OnContinueClicked()
        {
            // Reset le cycle dans GameManager pour la prochaine journée
            GameManager.IsNewGame = false; // Continuer le jeu
            GameManager.ResetCycle();
            
            // Retourner à la scène principale
            UnityEngine.SceneManagement.SceneManager.LoadScene("JamaaLFnaSimulator");
        }
    }
}
