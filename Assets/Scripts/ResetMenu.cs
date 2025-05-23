using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class ResetMenu : MonoBehaviour
{
    public void ShowResetMenu(int moon)
    {
        PlayerPrefs.SetInt("MoonToReset", moon);
        gameObject.SetActive(true);
    }

    public void HideResetMenu()
    {
        gameObject.SetActive(false);
    }

    public void ResetMoon()
    {
        int moon = PlayerPrefs.GetInt("MoonToReset");
        string moonFolder = string.Format("{0}/moons/moon{1}", Application.persistentDataPath, moon);
        File.Delete(moonFolder + "/moon.dat");
        File.Delete(moonFolder + "/player.dat");
        Directory.Delete(moonFolder + "/chunks", true);
        Directory.CreateDirectory(moonFolder + "/chunks");
        Directory.Delete(moonFolder + "/mobs", true);
        Directory.CreateDirectory(moonFolder + "/mobs");
        if (moon == 0)
            GameObject.Find("Moon A").transform.Find("Text").GetComponent<Text>().text = "Moon A -Unexplored-";
        else if (moon == 1)
            GameObject.Find("Moon B").transform.Find("Text").GetComponent<Text>().text = "Moon B -Unexplored-";
        else if (moon == 2)
            GameObject.Find("Moon C").transform.Find("Text").GetComponent<Text>().text = "Moon C -Unexplored-";
        else
            GameObject.Find("Moon D").transform.Find("Text").GetComponent<Text>().text = "Moon D -Unexplored-";
        HideResetMenu();
    }
}
