using UnityEngine;

public class ToggleMaterialColor : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;

    public GameObject BuretteGrabStatus;

    private Material runtimeMaterial;
    private bool enabled = true;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        runtimeMaterial = targetRenderer.material;
        runtimeMaterial.color = Color.blue;
        enabled = true;
        BuretteGrabStatus.SetActive(enabled);
    }

    public void ToggleColor()
    {
        if (runtimeMaterial == null) return;

        runtimeMaterial.color = enabled ? Color.blue : Color.red;
        enabled = !enabled;
        BuretteGrabStatus.SetActive(!enabled);
    }
}