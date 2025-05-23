using UnityEngine;

public class SkyboxAndFog : MonoBehaviour
{
    public Material skyboxMaterial;

    void Awake()
    {
        int color = UnityEngine.Random.Range(1, 6);
        Cubemap texture = Resources.Load<Cubemap>($"Textures/lc_skybox_fog{color}");
        skyboxMaterial.SetTexture("_MainTex", texture);
        if (color == 1)
            RenderSettings.fogColor = new Color(0.067F, 0.208F, 0.314F, 1);
        else if (color == 2)
            RenderSettings.fogColor = new Color(0.314F, 0.067F, 0.31F, 1);
        else if (color == 3)
            RenderSettings.fogColor = new Color(0.067F, 0.314F, 0.188F, 1);
        else if (color == 4)
            RenderSettings.fogColor = new Color(0.067F, 0.094F, 0.314F, 1);
        else
            RenderSettings.fogColor = new Color(0.239F, 0.067F, 0.314F, 1);
    }
}
