using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Put this on the extinguisher's spray-cone collider — the cylinder set to "Is Trigger".
/// It tracks every <see cref="Fire"/> currently inside the trigger and, while the extinguisher
/// is actively spraying, shrinks each of them (Fire handles destroy + regrow itself).
///
/// Trigger callbacks must live on the same GameObject as the trigger collider, which is why
/// this is a separate component from FireExtinguisher.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SprayZone : MonoBehaviour
{
    [Tooltip("The extinguisher whose spray state gates the effect. Auto-found in parents if empty.")]
    [SerializeField] private FireExtinguisher extinguisher;

    private readonly HashSet<Fire> firesInside = new HashSet<Fire>();

    private void Awake()
    {
        if (extinguisher == null) extinguisher = GetComponentInParent<FireExtinguisher>();
        if (extinguisher == null) Debug.LogError("[SprayZone] No FireExtinguisher found in parents.");
    }

    private void OnTriggerEnter(Collider other)
    {
        var fire = other.GetComponentInParent<Fire>();
        if (fire != null) firesInside.Add(fire);
    }

    private void OnTriggerExit(Collider other)
    {
        var fire = other.GetComponentInParent<Fire>();
        if (fire != null) firesInside.Remove(fire);
    }

    private void Update()
    {
        if (extinguisher == null || !extinguisher.IsSpraying) return;

        firesInside.RemoveWhere(f => f == null);          // drop fires that got destroyed
        foreach (var fire in firesInside) fire.Spray();
    }
}
