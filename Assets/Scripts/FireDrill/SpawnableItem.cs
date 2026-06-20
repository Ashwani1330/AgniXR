using UnityEngine;

/// <summary>
/// Every authorable item in the fire drill is the SAME object, parameterized by type.
/// "Fire should be scriptable just like alarms." Do not add a separate code path per item.
/// </summary>
public enum SpawnableItemType
{
    Alarm,
    ExtinguisherA,  // Class A extinguisher
    ExtinguisherB,  // Class B extinguisher
    FireA,          // Class A fire (matched by ExtinguisherA)
    FireB,          // Class B fire (matched by ExtinguisherB)
    Endpoint        // safe exit / muster point — carries the end-of-run API trigger
}

/// <summary>
/// The single spawnable prefab for both Create mode and Run mode. One prefab, one component,
/// behaviour driven by <see cref="type"/>. Visuals are swapped by enabling the matching child.
/// </summary>
public class SpawnableItem : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private SpawnableItemType type = SpawnableItemType.FireA;

    [Header("Per-type visuals (optional)")]
    [Tooltip("Child objects shown/hidden based on this item's type. " +
             "Array index must line up with the SpawnableItemType enum order.")]
    [SerializeField] private GameObject[] visualsByType;

    public SpawnableItemType Type => type;

    public bool IsFire => type == SpawnableItemType.FireA || type == SpawnableItemType.FireB;
    public bool IsExtinguisher => type == SpawnableItemType.ExtinguisherA || type == SpawnableItemType.ExtinguisherB;
    public bool IsEndpoint => type == SpawnableItemType.Endpoint;

    private void Awake() => ApplyVisuals();

    /// <summary>Set the item's type at runtime (used when rehydrating a scenario).</summary>
    public void SetType(SpawnableItemType newType)
    {
        type = newType;
        ApplyVisuals();
    }

    /// <summary>The scored matching mechanic: A puts out A, B puts out B.</summary>
    public bool Extinguishes(SpawnableItem fire)
    {
        if (!IsExtinguisher || fire == null || !fire.IsFire) return false;
        return (type == SpawnableItemType.ExtinguisherA && fire.type == SpawnableItemType.FireA)
            || (type == SpawnableItemType.ExtinguisherB && fire.type == SpawnableItemType.FireB);
    }

    private void ApplyVisuals()
    {
        if (visualsByType == null || visualsByType.Length == 0) return;
        for (int i = 0; i < visualsByType.Length; i++)
            if (visualsByType[i] != null)
                visualsByType[i].SetActive(i == (int)type);
    }
}
