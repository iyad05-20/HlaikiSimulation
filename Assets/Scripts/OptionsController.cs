using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class OptionsController : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private Slider volumeSlider;

    [Header("Graphics")]
    [SerializeField] private Toggle fullscreenToggle;

    private void Start()
    {
        // 1. Charger les réglages sauvegardés (ou mettre des valeurs par défaut)
        if (volumeSlider != null)
        {
            volumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }
    }

    public void SetVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("MasterVolume", value);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        Debug.Log("[Options] Fullscreen: " + isFullscreen);
    }

    public void OnBack()
    {
        // On sauvegarde tout physiquement sur le disque avant de quitter
        PlayerPrefs.Save();
        
        // On utilise la navigation centralisée
        MenuNavigation.CloseOptions();
    }
}
