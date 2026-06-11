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

            // Déclencher la fin du cycle avec la touche "=" (ou Key.Equals)
            if (Keyboard.current.equalsKey.wasPressedThisFrame)
            {
                Debug.Log("[EndOfCycleTrigger] Touche '=' pressée : Déclenchement de la fin du cycle !");
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
