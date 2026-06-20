using UnityEngine;
using TMPro;

public class UDPStatusController : MonoBehaviour
{
    [Header("References")]
    public GameObject udpReceiverObject;
    public TMP_Text status;
    public TMP_Text buretteVolumeText;
    public ParticleSystem flowParticleSystem;

    [Header("Liquid Settings")]
    public int liquidVolume = 50;
    public float liquidDecreaseRate = 0.1f;
    public float potentiometerValueForMaxRate = 100f;

    [Header("Burette Fluid Shader")]
    public Material buretteFluidMaterial;
    public string buretteFluidProperty = "_Burette_Fluid";

    private UDPReceiver udpReceiver;
    private float liquidVolumeRuntime;
    private float fluidLevel = 1f;
    private bool loggedMissingShaderProperty;

    void Start()
    {
        liquidVolumeRuntime = liquidVolume;
        ApplyFluidLevelFromVolume();
        SetParticleFlow(false);

        if (udpReceiverObject != null)
        {
            udpReceiver = udpReceiverObject.GetComponent<UDPReceiver>();
        }

        if (udpReceiver == null)
        {
            Debug.LogError("UDPStatusController: UDPReceiver component not found on the assigned GameObject.");
        }
    }

    void Update()
    {
        if (udpReceiver == null || status == null)
        {
            SetParticleFlow(false);

            liquidVolume = Mathf.Max(0, Mathf.RoundToInt(liquidVolumeRuntime));
            ApplyFluidLevelFromVolume();

            if (buretteVolumeText != null)
            {
                buretteVolumeText.text = "Burette volume: " + liquidVolume + " ml";
            }

            return;
        }

        float potentiometer = udpReceiver.potentiometer;
        float distance = udpReceiver.distance;
        bool isFlowing = false;

        if (distance > 100f && potentiometer > 10f)
        {
            status.text = "Status: spill";
            isFlowing = true;
        }
        else if (distance < 100f && potentiometer > 10f)
        {
            status.text = "Status: titrating";
            isFlowing = true;
        }
        else if (potentiometer > 10f)
        {
            status.text = "Status: shut off";
        }
        else
        {
            status.text = "Status: shut off";
        }

        if (isFlowing)
        {
            float flowMultiplier = GetFlowMultiplier(potentiometer);
            liquidVolumeRuntime = Mathf.Max(0f, liquidVolumeRuntime - (liquidDecreaseRate * flowMultiplier));
        }

        SetParticleFlow(isFlowing);

        liquidVolume = Mathf.Max(0, Mathf.RoundToInt(liquidVolumeRuntime));
        ApplyFluidLevelFromVolume();

        if (buretteVolumeText != null)
        {
            buretteVolumeText.text = "Burette volume: " + liquidVolume + " ml";
        }
    }

    private void ApplyFluidLevelFromVolume()
    {
        // Exact mapping: 50 ml -> 1.0, 0 ml -> 0.5
        float clampedVolume = Mathf.Clamp(liquidVolumeRuntime, 0f, 50f);
        fluidLevel = 0.5f + (clampedVolume / 50f) * 0.5f;

        if (buretteFluidMaterial != null && buretteFluidMaterial.HasProperty(buretteFluidProperty))
        {
            buretteFluidMaterial.SetFloat(buretteFluidProperty, fluidLevel);
        }
        else if (!loggedMissingShaderProperty)
        {
            Debug.LogWarning("UDPStatusController: Material is missing shader property '" + buretteFluidProperty + "'.");
            loggedMissingShaderProperty = true;
        }
    }

    private float GetFlowMultiplier(float potentiometer)
    {
        // 1x at pot=11, linearly rising to 10x at potentiometerValueForMaxRate.
        if (potentiometerValueForMaxRate <= 11f)
        {
            return 10f;
        }

        float t = Mathf.InverseLerp(11f, potentiometerValueForMaxRate, potentiometer);
        return Mathf.Lerp(1f, 10f, t);
    }

    private void SetParticleFlow(bool isFlowing)
    {
        if (flowParticleSystem == null)
        {
            return;
        }

        flowParticleSystem.gameObject.SetActive(isFlowing);
    }
}
