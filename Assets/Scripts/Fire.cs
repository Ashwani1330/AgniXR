using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A fire instance that shrinks while an extinguisher sprays it and grows back to its original
/// (prefab default) scale if the spray stops for a while. Destroys itself once fully out.
///
/// It is driven by <see cref="SprayZone"/>, which calls <see cref="Spray"/> once per frame
/// while this fire is inside the extinguisher's spray trigger AND the extinguisher is firing.
///
/// All live fires register in <see cref="Active"/> so a single FireAudioManager can drive one
/// shared fire-audio source (instead of one audio per fire).
/// </summary>
public class Fire : MonoBehaviour
{
    /// <summary>Every fire currently alive in the scene.</summary>
    public static readonly List<Fire> Active = new List<Fire>();

    /// <summary>Current size, 0 (extinguished) .. 1 (full). Used for total-audio intensity.</summary>
    public float Intensity => size;

    private void OnEnable() => Active.Add(this);
    private void OnDisable() => Active.Remove(this);

    [Header("Extinguish")]
    [Tooltip("How fast the fire shrinks while sprayed, as a fraction of full size per second. " +
             "e.g. 0.5 = fully extinguished in 2 seconds of continuous spray.")]
    public float shrinkRate = 0.5f;

    [Header("Regrow")]
    [Tooltip("Seconds the spray must be paused before the fire starts growing back.")]
    public float regrowDelay = 2f;

    [Tooltip("How fast the fire grows back, as a fraction of full size per second.")]
    public float regrowRate = 0.25f;

    private Vector3 baseScale;            // prefab default scale = full size
    private Vector3 basePosition;         // localPosition at full size
    private Vector3 pivotToBottomLocal;   // full-size offset pivot->bottom, in parent-local space
    private float size = 1f;              // 1 = full, 0 = extinguished
    private float lastSprayTime = -999f;
    private bool captured;                // resting state recorded?

    /// <summary>
    /// Record the resting scale/position the FIRST time we need it — not in Awake, because the
    /// spawner reparents/repositions the fire after Awake (which previously caused a teleport on
    /// the first hit). By first-spray time the fire is at its final resting transform.
    /// </summary>
    private void EnsureCaptured()
    {
        if (captured) return;
        baseScale = transform.localScale;
        basePosition = transform.localPosition;
        pivotToBottomLocal = ComputePivotToBottomLocal();
        captured = true;
    }

    /// <summary>
    /// Offset from the pivot to the bottom-centre of the mesh, at full size, in parent-local
    /// space. Used to keep the base planted while the fire shrinks toward it.
    /// </summary>
    private Vector3 ComputePivotToBottomLocal()
    {
        var rend = GetComponentInChildren<Renderer>();
        if (rend == null) return Vector3.zero;   // no mesh -> falls back to centre shrink
        Vector3 worldBottom = new Vector3(transform.position.x, rend.bounds.min.y, transform.position.z);
        Vector3 worldOffset = worldBottom - transform.position;
        return transform.parent ? transform.parent.InverseTransformVector(worldOffset) : worldOffset;
    }

    /// <summary>Call once per frame while this fire is being actively sprayed.</summary>
    public void Spray()
    {
        EnsureCaptured();
        lastSprayTime = Time.time;
        size -= shrinkRate * Time.deltaTime;

        if (size <= 0f)
        {
            Destroy(gameObject);   // fully extinguished
            return;
        }
        ApplyScale();
    }

    private void Update()
    {
        // Grow back only after the spray has been off for at least regrowDelay seconds.
        if (captured && size < 1f && Time.time - lastSprayTime >= regrowDelay)
        {
            size = Mathf.Min(1f, size + regrowRate * Time.deltaTime);
            ApplyScale();
        }
    }

    private void ApplyScale()
    {
        transform.localScale = baseScale * size;
        // Shift so the base (bottom) stays planted while the top shrinks down toward it.
        transform.localPosition = basePosition + pivotToBottomLocal * (1f - size);
    }
}
