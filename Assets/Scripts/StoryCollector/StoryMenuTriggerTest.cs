using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace JemaaGame.UI
{
    public class StoryMenuTriggerTest : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("The exact name of the Story Menu Scene")]
        public string storyMenuSceneName = "StoryCollectorMenu";

        private void Update()
        {
            if (Keyboard.current == null) return;

            // 1. Ouvrir le journal avec Tab
            if (Keyboard.current.tabKey.wasPressedThisFrame)
            {
                // Vérifier si la scène n'est pas déjà chargée
                if (!IsSceneLoaded(storyMenuSceneName))
                {
                    Debug.Log("[StoryMenuTriggerTest] Chargement additif du Codex...");
                    SceneManager.LoadSceneAsync(storyMenuSceneName, LoadSceneMode.Additive);
                    
                    // Si tu veux figer le jeu quand le menu s'ouvre, décommente la ligne suivante :
                    // Time.timeScale = 0f;
                }
            }

            // 2. Simuler la capture d'une histoire par le LLM (ex: touche F10)
            if (Keyboard.current.f10Key.wasPressedThisFrame)
            {
                if (SessionManager.Instance != null)
                {
                    Debug.Log("[StoryMenuTriggerTest] Capture d'une histoire factice générée...");
                    SessionManager.Instance.CaptureStory(
                        "fake_test_" + Random.Range(1000, 9999), 
                        "L'Histoire de Test", 
                        "Voici un texte généré aléatoirement pour vérifier que le prefab et la ScrollView s'agrandissent correctement et affichent bien le titre et le contenu sur une seule page. L'architecture a été simplifiée comme demandé."
                    );
                }
                else
                {
                    Debug.LogWarning("[StoryMenuTriggerTest] SessionManager introuvable dans la scène !");
                }
            }
        }

        private bool IsSceneLoaded(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.name == sceneName && scene.isLoaded)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
