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
        [Tooltip("The Content transform of your Scroll View (la liste à droite)")]
        [SerializeField] private RectTransform listContainer;
        [Tooltip("Le prefab de ton bouton (qui contient un TextMeshPro pour le titre)")]
        [SerializeField] private GameObject storyItemPrefab;
        [Tooltip("The Text UI element that will display the selected story's content (le grand parchemin)")]
        [SerializeField] private TMP_Text storyContentText;
        
        private PlayerStoriesData currentData;

        private void Start()
        {
            Debug.Log("[StoryMenuController] Start() a été appelé !");

            if (listContainer == null) Debug.LogError("[StoryMenuController] ERREUR : listContainer n'est pas assigné dans l'inspecteur !");
            if (storyItemPrefab == null) Debug.LogError("[StoryMenuController] ERREUR : storyItemPrefab n'est pas assigné dans l'inspecteur !");
            if (storyContentText == null) Debug.LogError("[StoryMenuController] ERREUR : storyContentText n'est pas assigné dans l'inspecteur !");

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
            // Vider le conteneur existant au cas où
            foreach (Transform child in listContainer)
            {
                Destroy(child.gameObject);
            }

            // --- MOCK DATA FORCE ---
            // On ignore temporairement les données sauvegardées pour forcer l'affichage des 3 vraies histoires
            currentData = new PlayerStoriesData();
            currentData.stories.Add(new CollectedStory {
                id = "test_1",
                title = "Le Charmeur de Serpents",
                content = "La place Jemaa el-Fna est réputée pour ses charmeurs de serpents. Au son de la ghaita (une sorte de flûte), les cobras et couleuvres semblent danser. C'est en fait le mouvement de l'instrument et les vibrations du sol qui captivent l'animal, plus que la musique elle-même. Cette tradition ancestrale fascine les visiteurs depuis des siècles.",
                dateUnlocked = System.DateTime.UtcNow.ToString("o")
            });
            currentData.stories.Add(new CollectedStory {
                id = "test_2",
                title = "Le Conteur de Légendes",
                content = "Au crépuscule, la place se transforme. Les halqa (cercles de spectateurs) se forment autour des conteurs (hlaiqis). Ils racontent des épopées, des fables morales et des légendes anciennes. Leur art oratoire, souvent accompagné de gestes théâtraux, est un pilier du patrimoine oral marocain, transmis de génération en génération.",
                dateUnlocked = System.DateTime.UtcNow.ToString("o")
            });
            currentData.stories.Add(new CollectedStory {
                id = "test_3",
                title = "L'Eau et les Porteurs d'Eau",
                content = "Autrefois essentiels pour la survie sous le soleil de Marrakech, les porteurs d'eau (Guerrab) parcourent la place avec leurs costumes colorés, leurs chapeaux à franges et leurs clochettes en cuivre. Bien qu'aujourd'hui leur rôle soit plus folklorique, ils rappellent l'importance vitale de l'eau dans cette région désertique.",
                dateUnlocked = System.DateTime.UtcNow.ToString("o")
            });
            // ------------------------

            if (currentData == null || currentData.stories.Count == 0) 
            {
                Debug.LogWarning("[StoryMenuController] Aucune histoire à afficher.");
                storyContentText.text = "Vous n'avez pas encore collecté d'histoires...";
                return;
            }

            for (int i = 0; i < currentData.stories.Count; i++)
            {
                var story = currentData.stories[i];
                if (storyItemPrefab == null || listContainer == null) return;

                // Instancier le prefab
                GameObject itemObj = Instantiate(storyItemPrefab, listContainer);
                
                // Mettre le texte (seulement le titre)
                StoryItemUI itemUI = itemObj.GetComponent<StoryItemUI>();
                if (itemUI != null)
                {
                    itemUI.SetTitle(story.title); 
                }

                // S'assurer qu'il y a un composant Button et lui ajouter un événement de clic
                Button btn = itemObj.GetComponent<Button>();
                if (btn == null) 
                {
                    btn = itemObj.AddComponent<Button>();
                }
                
                int index = i; // Obligatoire pour la closure dans la boucle
                btn.onClick.AddListener(() => OnStorySelected(index));
            }

            // Afficher manuellement le contenu de la première histoire au démarrage
            OnStorySelected(0);
        }

        public void OnStorySelected(int index)
        {
            if (currentData != null && index >= 0 && index < currentData.stories.Count)
            {
                // Mettre à jour le texte du parchemin avec le contenu complet de l'histoire
                storyContentText.text = currentData.stories[index].content;
            }
        }

        public void CloseMenu()
        {
            SceneManager.UnloadSceneAsync("StoryCollectorMenu");
            
            // Si vous avez mis le jeu en pause (Time.timeScale = 0), pensez à le remettre à 1 ici
            Time.timeScale = 1f;
        }

        public void OnMainMenu()
        {
            // On charge le menu principal via notre script centralisé
            MenuNavigation.GoToMainMenu();
            
            // On s'assure que le temps reprend si le jeu était en pause
            Time.timeScale = 1f;
        }
    }
}
