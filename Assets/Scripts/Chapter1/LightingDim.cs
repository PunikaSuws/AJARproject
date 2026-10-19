using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class LightingDim : MonoBehaviour
{
    [Header("Directional")]
    [SerializeField] Light directionalLight;

    [Header("ไฟจุดอื่น (Point/Spot ในบ้าน) ที่จะสว่างขึ้น")]
    [SerializeField] Light[] brightenLights;
    [SerializeField] float brightenMultiplier = 2f;   // 2 = สว่างขึ้น 2 เท่า

    [Header("Skybox")]
    [SerializeField, Range(0f, 1f)] float targetSkyExposure = 0.1f;
    [SerializeField, Range(0f, 1f)] float targetAmbient = 0.15f;
    [SerializeField, Range(0f, 1f)] float targetReflection = 0.15f;

    [Header("Timing")]
    [SerializeField] float duration = 2f;

    Material skyInstance;
    static readonly int Exposure = Shader.PropertyToID("_Exposure");

    public void Dim()
    {
        Debug.Log("Dim called");
        StopAllCoroutines();
        StartCoroutine(DimRoutine());
    }

    IEnumerator DimRoutine()
    {
        // ทำสำเนา Skybox เพื่อไม่แก้ไฟล์ material ต้นฉบับในโปรเจกต์
        Material sky = RenderSettings.skybox;
        bool canFadeSky = sky != null && sky.HasProperty(Exposure);
        if (canFadeSky)
        {
            skyInstance = new Material(sky);
            RenderSettings.skybox = skyInstance;
        }

        float startDir = directionalLight != null ? directionalLight.intensity : 0f;
        float startSky = canFadeSky ? skyInstance.GetFloat(Exposure) : 0f;
        float startAmbient = RenderSettings.ambientIntensity;
        float startReflect = RenderSettings.reflectionIntensity;

        float[] startLights = new float[brightenLights != null ? brightenLights.Length : 0];
        for (int i = 0; i < startLights.Length; i++)
            if (brightenLights[i] != null) startLights[i] = brightenLights[i].intensity;

        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / duration);

            if (directionalLight != null)
                directionalLight.intensity = Mathf.Lerp(startDir, 0f, k);

            for (int i = 0; i < startLights.Length; i++)
                if (brightenLights[i] != null)
                    brightenLights[i].intensity = Mathf.Lerp(startLights[i], startLights[i] * brightenMultiplier, k);

            if (canFadeSky)
                skyInstance.SetFloat(Exposure, Mathf.Lerp(startSky, targetSkyExposure, k));

            RenderSettings.ambientIntensity = Mathf.Lerp(startAmbient, targetAmbient, k);
            RenderSettings.reflectionIntensity = Mathf.Lerp(startReflect, targetReflection, k);

            yield return null;
        }

        if (directionalLight != null)
        {
            directionalLight.intensity = 0f;
            directionalLight.enabled = false;
        }
        for (int i = 0; i < startLights.Length; i++)
            if (brightenLights[i] != null)
                brightenLights[i].intensity = startLights[i] * brightenMultiplier;

        if (canFadeSky) skyInstance.SetFloat(Exposure, targetSkyExposure);
        RenderSettings.ambientIntensity = targetAmbient;
        RenderSettings.reflectionIntensity = targetReflection;
        DynamicGI.UpdateEnvironment();
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L)) Dim();
    }
}