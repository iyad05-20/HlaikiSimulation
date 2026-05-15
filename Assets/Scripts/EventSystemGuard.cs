using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Détruit l'EventSystem local s'il en existe déjà un dans la scène parente.
/// Permet d'éviter les conflits lors du chargement de scènes additives.
/// </summary>
public class EventSystemGuard : MonoBehaviour 
{
    private void Awake() 
    {
        EventSystem existing = FindFirstObjectByType<EventSystem>();
        
        // Si un EventSystem existe déjà et que ce n'est pas celui attaché à cet objet
        if (existing != null && existing.gameObject != gameObject) 
        {
            Debug.Log("[EventSystemGuard] EventSystem existant détecté. Suppression du doublon.");
            Destroy(gameObject);
        }
    }
}
