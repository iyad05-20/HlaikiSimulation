using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;

public class MainMenuController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button btnContinuer;

    // The name of the game scene to load
    private const string GameSceneName = "JamaaLFnaSimulator";

    private void Start()
    {
        // On vérifie s'il y a des sauvegardes pour activer/désactiver le bouton Continuer
        CheckForSaveFiles();
    }

    private void CheckForSaveFiles()
    {
        if (btnContinuer == null) return;

        string saveDir = Path.Combine(Application.persistentDataPath, "sessions");
        bool hasSaves = false;

        if (Directory.Exists(saveDir))
        {
            string[] files = Directory.GetFiles(saveDir, "*_session.json");
            if (files.Length > 0)
            {
                hasSaves = true;
            }
        }

        btnContinuer.interactable = hasSaves;
    }

    public void OnNouveauJeu()
    {
        // 1. On définit la variable statique pour "Nouveau Jeu"
        GameManager.IsNewGame = true;
        
        // 2. On supprime les sessions AVANT de charger la scène
        if (SessionManager.Instance != null)
        {
            Debug.Log("[MainMenu] Nouveau Jeu ! Effacement des anciennes sessions.");
            SessionManager.Instance.DeleteAllSessions();
        }

        // 3. On charge la scène
        SceneManager.LoadScene(GameSceneName);
    }


    public void OnContinuer()
    {
        // 1. On définit la variable statique pour "Continuer"
        GameManager.IsNewGame = false;
        Debug.Log("[MainMenu] Continuer ! GameManager.IsNewGame = " + GameManager.IsNewGame);

        // 2. On charge la scène par son nom
        SceneManager.LoadScene(GameSceneName);
    }

    public void OnOptions()
    {
        MenuNavigation.OpenOptions();
    }

    public void OnQuit()
    {
        Debug.Log("[MainMenu] Quit");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
