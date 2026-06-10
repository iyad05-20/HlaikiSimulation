using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace JemaaGame.UI
{
    public class StoryMenuController : MonoBehaviour
    {
        [Header("UI Elements")]
        [Tooltip("The Content transform of your Scroll View")]
        [SerializeField] private RectTransform listContainer;
        [Tooltip("A Prefab containing two TMP_Text children named 'TitleText' and 'ContentText'")]
        [SerializeField] private GameObject storyItemPrefab;
        
        private PlayerStoriesData currentData;

        private void Start()
        {
            Debug.Log("[StoryMenuController] Start() a été appelé !");

            if (listContainer == null) Debug.LogError("[StoryMenuController] ERREUR : listContainer n'est pas assigné dans l'inspecteur !");
            if (storyItemPrefab == null) Debug.LogError("[StoryMenuController] ERREUR : storyItemPrefab n'est pas assigné dans l'inspecteur !");

            // 1. Charger les histoires sauvegardées
            if (SessionManager.Instance != null)
            {
                currentData = SessionManager.Instance.LoadStories();
                Debug.Log($"[StoryMenuController] Données chargées. Nombre d'histoires : {(currentData?.stories != null ? currentData.stories.Count : 0)}");
                PopulateList();
            }
            else
            {
                Debug.LogWarning("[StoryMenuController] ERREUR : SessionManager n'est pas présent dans la scène.");
            }
        }

        private void Update()
        {
            // Permet de fermer le menu avec la touche Tab ou Echap
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                if (UnityEngine.InputSystem.Keyboard.current.tabKey.wasPressedThisFrame ||
                    UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    CloseMenu();
                }
            }
        }

        private void PopulateList()
        {
            if (currentData == null || currentData.stories.Count == 0) 
            {
                Debug.LogWarning("[StoryMenuController] Aucune histoire à afficher (currentData est null ou vide).");
                return;
            }

            Debug.Log($"[StoryMenuController] Début de l'instanciation de {currentData.stories.Count} prefabs...");

            foreach (var story in currentData.stories)
            {
                if (storyItemPrefab == null || listContainer == null) return; // Sécurité

                GameObject itemObj = Instantiate(storyItemPrefab, listContainer);
                Debug.Log($"[StoryMenuController] Instancié le prefab pour l'histoire : {story.title}");
                
                // Utiliser le nouveau composant pour injecter le texte
                StoryItemUI itemUI = itemObj.GetComponent<StoryItemUI>();
                if (itemUI != null)
                {
                    itemUI.SetStory(story.title, story.content);
                    Debug.Log($"[StoryMenuController] Texte injecté avec succès via StoryItemUI pour : {story.title}");
                }
                else
                {
                    Debug.LogError("[StoryMenuController] ERREUR : Le prefab 'storyItemPrefab' n'a pas le composant 'StoryItemUI' attaché !");
                }
            }
        }

        public void CloseMenu()
        {
            SceneManager.UnloadSceneAsync("StoryCollectorMenu");
            
            // Si vous avez mis le jeu en pause (Time.timeScale = 0), pensez à le remettre à 1 ici
            // Time.timeScale = 1f;
        }
    }
}
