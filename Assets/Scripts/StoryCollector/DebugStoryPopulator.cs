using UnityEngine;
using UnityEngine.InputSystem;

namespace JemaaGame.UI
{
    public class DebugStoryPopulator : MonoBehaviour
    {
        private void Update()
        {
            if (Keyboard.current == null) return;

            // Appuie sur F9 pour injecter de vraies "junk stories" dans le menu
            if (Keyboard.current.f9Key.wasPressedThisFrame)
            {
                if (SessionManager.Instance != null)
                {
                    Debug.Log("[DebugStoryPopulator] Injection de fragments de test...");

                    SessionManager.Instance.CaptureStory(
                        "frag_hamid_1",
                        "Le Secret de Hamid",
                        "J'ai toujours vendu ces objets, mais en réalité, ce sont des artefacts trouvés près des ruines. Je ne l'ai jamais dit à personne, mais tu m'as l'air digne de confiance."
                    );

                    SessionManager.Instance.CaptureStory(
                        "frag_fatima_1",
                        "Les Souvenirs de Lalla Fatima",
                        "La place n'a pas toujours été aussi bruyante. Avant, on s'y asseyait pour écouter le silence du désert... Mais les temps changent."
                    );

                    SessionManager.Instance.CaptureStory(
                        "frag_moussa_1",
                        "La Rancune de Moussa",
                        "Pourquoi je suis toujours en colère ? Parce que j'ai perdu mon stand il y a 10 ans à cause d'un riche marchand. Je n'oublierai jamais."
                    );

                    Debug.Log("[DebugStoryPopulator] 3 histoires ajoutées ! Ouvre le menu avec Tab pour vérifier.");
                }
                else
                {
                    Debug.LogWarning("[DebugStoryPopulator] SessionManager introuvable !");
                }
            }
        }
    }
}
