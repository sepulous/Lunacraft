using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OptionsMenu : MonoBehaviour
{
    public Slider renderDistance;
    public ToggleButton showFog;
    public ToggleButton showGUI;
    public ToggleButton debugInfo;
    public Slider sfxVolume;
    public Slider musicVolume;
    public Slider sensitivity;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    // OptionsManager loads these values in Awake()
    void Start()
    {
        Options options = OptionsManager.GetCurrentOptions();
        renderDistance.value = options.renderDistance;
        showFog.SetEnabled(options.showFog);
        showGUI.SetEnabled(options.showGUI);
        debugInfo.SetEnabled(options.showDebugInfo);
        sfxVolume.value = options.sfxVolume;
        musicVolume.value = options.musicVolume;
        sensitivity.value = options.sensitivity;
    }

    public void UpdateOptions()
    {
        Options newOptions = new Options();
        newOptions.renderDistance = (int)renderDistance.value;
        newOptions.showFog = showFog.IsEnabled();
        newOptions.showGUI = showGUI.IsEnabled();
        newOptions.showDebugInfo = debugInfo.IsEnabled();
        newOptions.sfxVolume = sfxVolume.value;
        newOptions.musicVolume = musicVolume.value;
        newOptions.sensitivity = sensitivity.value;
        OptionsManager.UpdateCurrentOptions(newOptions);
        gameObject.SetActive(false);
    }
}
