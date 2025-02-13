using UnityEngine;

public class WorldClock : MonoBehaviour
{
    public Material skybox;
    public Light mainLight;

    private bool timePasses = true;
    private int phaseCount = 8;
    private int secondsPerPhase = 30;
    private int startAngle;
    private float omega;
    private float gameTimeInSeconds = 0;

    void Start()
    {
        // TODO: gameTimeInSeconds = savedGameTimeInSeconds
        startAngle = 90;
        omega = 360F / (phaseCount * secondsPerPhase);
    }

    void FixedUpdate()
    {
        if (timePasses)
        {
            gameTimeInSeconds += Time.fixedDeltaTime;

            // Update skybox
            float skyboxAngle = (startAngle + omega*gameTimeInSeconds) % 360F;
            skybox.SetFloat("_RotationAngle", skyboxAngle);

            // Update lighting
            int phase = (int)(gameTimeInSeconds / secondsPerPhase) % phaseCount; // TODO: Fix phase calculation. It should be tied to the sun's position, not whatever the in-game time happens to be

            float lightAngle = startAngle - 45*phase;
            if (lightAngle < -360)
                lightAngle = -360 - lightAngle;
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
}
