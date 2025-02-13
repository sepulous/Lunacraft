using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MoonSettingsMenu : MonoBehaviour
{
    public Slider treeCover;
    public Slider terrainRoughness;
    public Slider exoticTerrain;
    public Slider wildlifeLevel;
    public ModeButton creativeModeButton;
    public TMP_Text seed;
    public GameObject levelManager;

    public void Launch()
    {
        int moon = PlayerPrefs.GetInt("moon");
        string moonDataFile = string.Format("{0}/moons/moon{1}/moon.dat", Application.persistentDataPath, moon);
        MoonData moonData = new MoonData();
        if (string.IsNullOrEmpty(seed.text.Trim()) || seed.text.Length == 1 && (int)seed.text[0] == 8203) // idk what the fuck this is
        {
            moonData.seed = (ulong)UnityEngine.Random.Range(ulong.MinValue, ulong.MaxValue);
        }
        else
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(seed.text.Trim()));
                byte[] first8Bytes = new byte[8];
                Array.Copy(hashBytes, 0, first8Bytes, 0, 8);
                moonData.seed = (ulong)BitConverter.ToInt64(first8Bytes, 0);
            }
        }
        Debug.Log(moonData.seed);
        moonData.distanceTraveled = 0;
        moonData.worldTime = 0;
        moonData.isCreative = creativeModeButton.IsEnabled();
        moonData.treeCover = treeCover.value;
        moonData.terrainRoughness = terrainRoughness.value;
        moonData.exoticTerrain = exoticTerrain.value;
        moonData.wildlifeLevel = wildlifeLevel.value;
        using (FileStream file = File.Open(moonDataFile, FileMode.Create, FileAccess.Write))
            (new BinaryFormatter()).Serialize(file, moonData);

        levelManager.GetComponent<LevelManager>().GenerateMoon(moon, moonData, true);
    }

    public void Back()
    {
        gameObject.SetActive(false);
    }
}
