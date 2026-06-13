using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using JemaaGame.NPC;

namespace JemaaGame.UI
{
    public class HalkaSceneController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI gradeText;
        [SerializeField] private TextMeshProUGUI narrationText;
        [SerializeField] private TextMeshProUGUI reactionText;
        [SerializeField] private TextMeshProUGUI detailsText;
        [SerializeField] private Button backToGameButton;

        private void Start()
        {
            if (backToGameButton != null)
            {
                backToGameButton.onClick.AddListener(ReturnToMainScene);
            }

            var result = HalkaOrchestrator.LastResult;
            if (result == null)
            {
                Debug.LogWarning("[HalkaSceneController] Aucun résultat de Halka trouvé ! Test mode.");
                DisplayFallback();
                return;
            }

            DisplayResult(result);
        }

        private void DisplayResult(HalkaCompositionEngine.CompositionResult result)
        {
            // 1. Déterminer le Rang
            string grade = CalculateGrade(result.totalScore);
            if (gradeText != null)
            {
                gradeText.text = $"RANG: {grade}";
                gradeText.color = GetGradeColor(grade);
            }

            // 2. Afficher la narration générée
            if (narrationText != null)
            {
                narrationText.text = string.IsNullOrEmpty(result.narration) ? "Le silence s'installe..." : result.narration;
            }

            // 3. Réaction de la foule
            if (reactionText != null)
            {
                reactionText.text = $"Réaction : {result.reactionLabel}";
            }

            // 4. Détails du score
            if (detailsText != null)
            {
                string details = $"Cohérence : {result.coherenceScore}\n" +
                                 $"Profondeur : {result.depthScore}\n" +
                                 $"Audience : {result.audienceSize}\n" +
                                 $"Score Total : {result.totalScore}";

                if (result.exposesSensitiveStory)
                {
                    details += "\n\n<color=red>⚠️ Une histoire intime a été révélée devant la foule !</color>";
                }
                
                detailsText.text = details;
            }
        }

        private string CalculateGrade(int score)
        {
            if (score >= 90) return "S";
            if (score >= 75) return "A";
            if (score >= 50) return "B";
            if (score >= 35) return "C";
            if (score >= 20) return "D";
            return "E";
        }

        private Color GetGradeColor(string grade)
        {
            switch (grade)
            {
                case "S": return new Color(1f, 0.84f, 0f); // Or / Gold
                case "A": return new Color(0.2f, 0.8f, 0.2f); // Vert vif
                case "B": return new Color(0.2f, 0.6f, 1f); // Bleu
                case "C": return Color.white;
                case "D": return new Color(1f, 0.5f, 0f); // Orange
                case "E": return Color.red;
                default: return Color.gray;
            }
        }

        private void DisplayFallback()
        {
            if (gradeText != null) gradeText.text = "RANG: ?";
            if (narrationText != null) narrationText.text = "Lance une vraie Halka depuis le jeu pour voir les résultats.";
        }

        private void ReturnToMainScene()
        {
            // Recharger la scène principale ou passer au jour suivant
            SceneManager.LoadScene("JamaaLFnaSimulator");
        }
    }
}
