using UnityEngine;
using UnityEngine.Rendering;
using System;

public class PauseManager : MonoBehaviour
{
    [SerializeField] private GameInputManager gameInputManager;
    [SerializeField] private Volume pausePostProcessVolume;
    
    private bool isPaused = false;

    private void Start()
    {
        if (gameInputManager != null)
        {
            gameInputManager.OnOptions += GameInputManager_OnOptions;
        }

        // On s'abonne à la fermeture du menu (ex: via bouton Retour dans l'UI)
        MenuNavigation.OnMenuClosed += HandleMenuClosed;
    }

    private void OnDestroy()
    {
        if (gameInputManager != null)
        {
            gameInputManager.OnOptions -= GameInputManager_OnOptions;
        }

        MenuNavigation.OnMenuClosed -= HandleMenuClosed;
    }

    private void HandleMenuClosed()
    {
        // Si le menu a été fermé par l'UI alors qu'on était en pause, on relance le jeu
        if (isPaused)
        {
            Resume();
        }
    }

    private void GameInputManager_OnOptions(object sender, EventArgs e)
    {
        TogglePause();
    }

    public void TogglePause()
    {
        if (isPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        
        // Activer l'effet de flou
        if (pausePostProcessVolume != null)
            pausePostProcessVolume.gameObject.SetActive(true);

        // On ne désactive plus la map entière car on a besoin de la touche Options (ESC) pour Resume !
        // Plus tard, on pourra désactiver seulement le mouvement dans PlayerLogic si besoin.

        MenuNavigation.OpenOptions();
        Debug.Log($"[PauseManager] Game Paused, isPaused: {isPaused}");
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;

        // Désactiver l'effet de flou
        if (pausePostProcessVolume != null)
            pausePostProcessVolume.gameObject.SetActive(false);

        // On s'assure que le menu est fermé (cas où on fait Resume via ESC)
        MenuNavigation.CloseOptions();
        
        Debug.Log($"[PauseManager] Game Resumed, isPaused: {isPaused}");
    }
}
