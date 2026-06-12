using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Classe utilitaire statique pour gérer la navigation entre les menus.
/// Permet d'ouvrir et fermer les scènes additives (comme les Options) depuis n'importe où.
/// </summary>
public static class MenuNavigation
{
    private const string OptionsSceneName = "OptionsMenu";
    public static event System.Action OnMenuClosed;

    public static void OpenOptions()
    {
        // On vérifie si la scène n'est pas déjà chargée pour éviter d'en avoir 15 exemplaires
        if (!IsSceneLoaded(OptionsSceneName))
        {
            SceneManager.LoadScene(OptionsSceneName, LoadSceneMode.Additive);
            Debug.Log("[MenuNavigation] Loading Options Menu additively.");
        }
        else
        {
            Debug.Log("[MenuNavigation] Options Menu is already open.");
        }
    }

    public static void OpenStoryCollector()
    {
        string storyScene = "StoryCollectorMenu";
        if (!IsSceneLoaded(storyScene))
        {
            SceneManager.LoadScene(storyScene, LoadSceneMode.Additive);
            Debug.Log("[MenuNavigation] Loading Story Collector additively.");
        }
        else
        {
            Debug.Log("[MenuNavigation] Story Collector is already open.");
        }
    }

    public static void GoToMainMenu()
    {
        // LoadSceneMode.Single est le comportement par défaut, ça décharge toutes les autres scènes
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
        Debug.Log("[MenuNavigation] Loading Main Menu and unloading everything else.");
    }

    public static void CloseOptions()
    {
        if (IsSceneLoaded(OptionsSceneName))
        {
            SceneManager.UnloadSceneAsync(OptionsSceneName);
            OnMenuClosed?.Invoke();
            Debug.Log("[MenuNavigation] Unloading Options Menu.");
        }
    }

    private static bool IsSceneLoaded(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.name == sceneName)
            {
                return true;
            }
        }
        return false;
    }
}
