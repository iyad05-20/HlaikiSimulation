using UnityEngine;
using UnityEngine.InputSystem;
using JemaaGame.NPC;

namespace JemaaGame.UI
{
    public class EndOfCycleTrigger : MonoBehaviour
    {
        private void Update()
        {
            if (Keyboard.current == null) return;

            // Déclencher la fin du cycle avec "=", "F8" ou "Entrée" (pour éviter les bugs de clavier AZERTY)
            if (Keyboard.current.equalsKey.wasPressedThisFrame || 
                Keyboard.current.f8Key.wasPressedThisFrame || 
                Keyboard.current.enterKey.wasPressedThisFrame)
            {
                Debug.Log("[EndOfCycleTrigger] Touche pressée : Déclenchement de la fin du cycle !");
                if (HalkaOrchestrator.Instance != null)
                {
                    HalkaOrchestrator.Instance.TriggerEndOfCycleHalka();
                }
                else
                {
                    Debug.LogWarning("[EndOfCycleTrigger] HalkaOrchestrator est introuvable dans la scène !");
                }
            }
        }
    }
}
