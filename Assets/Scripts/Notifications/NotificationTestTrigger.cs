using UnityEngine;
using UnityEngine.InputSystem;

namespace JemaaGame.UI
{
    public class NotificationTestTrigger : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private Sprite testIcon;

        private void Update()
        {
            // Verify that the Keyboard device is currently active
            if (Keyboard.current == null) return;

            // F1: Friendship ( Emerald Green )
            if (Keyboard.current[Key.F1].wasPressedThisFrame)
            {
                NotificationManager.Instance.Show(
                    "AMITIÉ ACQUISE", 
                    "Driss apprécie votre honnêteté. Affinité +5", 
                    NotificationType.Friendship, 
                    testIcon
                );
            }

            // F2: Reputation ( Indigo )
            if (Keyboard.current[Key.F2].wasPressedThisFrame)
            {
                NotificationManager.Instance.Show(
                    "RÉPUTATION HAUTE", 
                    "Votre réputation à Jamaa El Fna a augmenté au Niveau 2 !", 
                    NotificationType.Reputation, 
                    testIcon
                );
            }

            // F3: Fragment ( Doré / Gold ) via le Story Collector
            if (Keyboard.current[Key.F3].wasPressedThisFrame)
            {
                if (SessionManager.Instance != null)
                {
                    SessionManager.Instance.CaptureStory(
                        "moussa_musicien",
                        "Le Passé de Moussa",
                        "Cet homme Moussa... tu as vu ses mains ? Ces mains ont porté un guembri pendant vingt ans. Mais il y a une histoire derrière ce silence."
                    );
                }
                else
                {
                    Debug.LogWarning("SessionManager non trouvé pour capturer l'histoire.");
                }
            }

            // F4: System ( Gris Slate )
            if (Keyboard.current[Key.F4].wasPressedThisFrame)
            {
                NotificationManager.Instance.Show(
                    "SYSTÈME", 
                    "Partie sauvegardée automatiquement.", 
                    NotificationType.System, 
                    null
                );
            }

            // F5: Warning ( Rouge Alerte )
            if (Keyboard.current[Key.F5].wasPressedThisFrame)
            {
                NotificationManager.Instance.Show(
                    "ERREUR GROQ API", 
                    "Impossible de joindre le modèle LLM. Code: 503 Service Unavailable", 
                    NotificationType.Warning, 
                    null,
                    4.5f // Longer display for warnings
                );
            }

            // F6: Spam Test ( 10 notifications at once )
            if (Keyboard.current[Key.F6].wasPressedThisFrame)
            {
                Debug.Log("[TestTrigger] Lancement du test de spam : 10 notifications envoyées simultanément !");
                
                for (int i = 1; i <= 10; i++)
                {
                    NotificationManager.Instance.Show(
                        $"SPAM TEST #{i}",
                        $"Ceci est la notification de test en rafale numéro {i}.",
                        (NotificationType)(i % 5), // Cycle through all types
                        null,
                        2.5f
                    );
                }
            }
        }
    }
}
