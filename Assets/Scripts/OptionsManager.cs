using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class Options
{
    public int renderDistance;
    public bool showFog;
    public bool showGUI;
    public bool showDebugInfo;
    public float sfxVolume;
    public float musicVolume;
    public float sensitivity;
}

public class OptionsManager : MonoBehaviour
{
    private static string optionsPath;
    private static Options currentOptions;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        optionsPath = string.Format("{0}/options.dat", Application.persistentDataPath);

        BinaryFormatter formatter = new BinaryFormatter();
        if (!File.Exists(optionsPath))
        {
            Options defaultOptions = new Options();
            defaultOptions.renderDistance = 2;
            defaultOptions.showFog = true;
            defaultOptions.showGUI = true;
            defaultOptions.showDebugInfo = false;
            defaultOptions.sfxVolume = 1F;
            defaultOptions.musicVolume = 1F;
            defaultOptions.sensitivity = 0.5F;
            
            using (FileStream fileStream = new FileStream(optionsPath, FileMode.Create, FileAccess.Write))
                formatter.Serialize(fileStream, defaultOptions);

            currentOptions = defaultOptions;
        }
        else
        {
            using (FileStream fileStream = new FileStream(optionsPath, FileMode.Open, FileAccess.Read))
                currentOptions = (Options)formatter.Deserialize(fileStream);
        }
    }

    public static Options GetCurrentOptions()
    {
        return currentOptions;
    }

    public static void UpdateCurrentOptions(Options newOptions)
    {
        currentOptions = newOptions;
        SaveCurrentOptionsToFile();
    }

    private static void SaveCurrentOptionsToFile()
    {
        using (FileStream fileStream = new FileStream(optionsPath, FileMode.Create, FileAccess.Write))
            (new BinaryFormatter()).Serialize(fileStream, currentOptions);
    }
}
