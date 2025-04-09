using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class WorldClock : MonoBehaviour
{
    public Material skybox;
    public Light mainLight;

    private bool timePasses = true;
    private int phaseCount = 8;
    private int secondsPerPhase = 30;
    private float startAngle = 90;
    private float omega;
    private float gameTimeInSeconds = 0;

    void Start()
    {
        int moon = PlayerPrefs.GetInt("moon");
        string moonDataFile = string.Format("{0}/moons/moon{1}/moon.dat", Application.persistentDataPath, moon);
        MoonData moonData;
        using (FileStream file = File.Open(moonDataFile, FileMode.Open))
            moonData = (MoonData)(new BinaryFormatter()).Deserialize(file);
        gameTimeInSeconds = (float)moonData.worldTime;
        omega = 360F / (phaseCount * secondsPerPhase);
    }

    void FixedUpdate()
    {
        if (timePasses)
        {
            gameTimeInSeconds += Time.fixedDeltaTime;

            // Update skybox
            float skyboxAngle = (startAngle + omega*gameTimeInSeconds);
            skybox.SetFloat("_RotationAngle", skyboxAngle);

            // Update lighting
            int phase = (int)(((skyboxAngle / (360F / phaseCount)) - 2) % phaseCount);

            float lightAngle;
            if (phase == 3 || phase == 4 || phase == 5)
                lightAngle = -90;
            else
                lightAngle = 90 - phase*45;
            mainLight.transform.localRotation = Quaternion.Euler(lightAngle, 0, 0);

            if (phase == 0)
            {
                RenderSettings.ambientIntensity = 1;
                mainLight.intensity = 1;
            }
            else if (phase == 1 || phase == 7)
            {
                RenderSettings.ambientIntensity = 0.8F;
                mainLight.intensity = 0.6F;
            }
            else if (phase == 2 || phase == 6)
            {
                RenderSettings.ambientIntensity = 0.6F;
                mainLight.intensity = 0.2F;
            }
            else
            {
                RenderSettings.ambientIntensity = 0.4F;
                mainLight.intensity = 0;
            }
        }
    }

    public void SaveWorldTime()
    {
        int moon = PlayerPrefs.GetInt("moon");
        string moonDataFile = string.Format("{0}/moons/moon{1}/moon.dat", Application.persistentDataPath, moon);

        MoonData moonData;
        using (FileStream file = File.Open(moonDataFile, FileMode.Open))
            moonData = (MoonData)(new BinaryFormatter()).Deserialize(file);
        moonData.worldTime = (long)gameTimeInSeconds;

        using (FileStream fileStream = new FileStream(moonDataFile, FileMode.Open, FileAccess.Write))
            (new BinaryFormatter()).Serialize(fileStream, moonData);
    }

    public void Speedup()
    {
        secondsPerPhase = 10;
        float newOmega = 360F / (phaseCount * secondsPerPhase);
        startAngle += (omega - newOmega)*gameTimeInSeconds;
        omega = newOmega;
    }

    public void Slowdown()
    {
        secondsPerPhase = 30;
        float newOmega = 360F / (phaseCount * secondsPerPhase);
        startAngle += (omega - newOmega)*gameTimeInSeconds;
        omega = newOmega;
    }

    public void Reverse()
    {
        secondsPerPhase = 11;
        float newOmega = 360F / (phaseCount * secondsPerPhase);
        startAngle += (omega - newOmega)*gameTimeInSeconds;
        omega = newOmega;

        startAngle += 2*omega*gameTimeInSeconds;
        omega = -omega;
    }

    public void Unreverse()
    {
        if (secondsPerPhase != 10)
        {
            startAngle += 2*omega*gameTimeInSeconds;
            omega = -omega;

            secondsPerPhase = 30;
            float newOmega = 360F / (phaseCount * secondsPerPhase);
            startAngle += (omega - newOmega)*gameTimeInSeconds;
            omega = newOmega;
        }
    }
}
