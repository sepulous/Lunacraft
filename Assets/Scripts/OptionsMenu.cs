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
        //DontDestroyOnLoad(gameObject);
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

    public void UpdateOptions(bool closeMenu)
    {
        Options options = new Options();
        options.renderDistance = (int)renderDistance.value;
        options.showFog = showFog.IsEnabled();
        options.showGUI = showGUI.IsEnabled();
        options.showDebugInfo = debugInfo.IsEnabled();
        options.sfxVolume = sfxVolume.value;
        options.musicVolume = musicVolume.value;
        options.sensitivity = sensitivity.value;
        OptionsManager.UpdateCurrentOptions(options);
        gameObject.SetActive(!closeMenu);
    }

    public void ToggleFog()
    {
        showFog.ToggleEnabled();
        RenderSettings.fogColor = new Color(RenderSettings.fogColor.r, RenderSettings.fogColor.g, RenderSettings.fogColor.b, showFog.IsEnabled() ? 1 : 0);
        UpdateOptions(false);
    }

    public void ToggleGUI()
    {
        showGUI.ToggleEnabled();
        UpdateOptions(false);
    }
}
