using UnityEngine;
using Oculus.Interaction;

/// <summary>
/// Meta XR SDK version of the extinguisher (converted from XR Interaction Toolkit).
///
/// Grab is detected IN CODE by polling the Grabbable's selection state — no
/// PointableUnityEventWrapper wiring required. (The public OnExtinguisherGrabbed/Released
/// methods are still here so you CAN wire an event wrapper too, but it's optional.)
///
/// Spray is read from OVRInput (index trigger), not an InputActionReference.
/// </summary>
public class FireExtinguisher : MonoBehaviour
{
    [Header("Fire Suppression System")]
    public ParticleSystem fireSuppressant;
    public float maxSprayDuration = 10f;

    [Header("Spray Trigger (OVR)")]
    [Tooltip("Spray while either index trigger is held past this value (0..1).")]
    public float triggerThreshold = 0.5f;

    [Header("Fire Detection")]
    public LayerMask fireLayer;
    public float sprayRadius = 2f;

    [Header("Audio")]
    public AudioSource sprayAudio;

    [Header("Pin Mechanics")]
    public GameObject pinObject;
    public bool isPinPulled = false;
    [Tooltip("For quick testing: count the pin as pulled the moment the extinguisher is grabbed.")]
    public bool autoPullPinOnGrab = true;

    [Header("Grab")]
    [Tooltip("The Grabbable to read held-state from. Auto-found on this object if left empty.")]
    [SerializeField] private Grabbable grabbable;

    private float sprayTimer = 0f;
    private bool isSpraying = false;
    private bool isHoldingExtinguisher = false;

    /// <summary>True while actively spraying — SprayZone reads this to gate the extinguish effect.</summary>
    public bool IsSpraying => isSpraying;
   // private PerformanceTracker performanceTracker;

    private void Start()
    {
        //performanceTracker = FindObjectOfType<PerformanceTracker>();
        //if (performanceTracker == null) Debug.LogWarning("[Extinguisher] PerformanceTracker not found in scene.");

        if (fireSuppressant != null) fireSuppressant.Stop();
        else Debug.LogError("[Extinguisher] fireSuppressant (ParticleSystem) is not assigned!");

        if (sprayAudio == null) Debug.LogWarning("[Extinguisher] sprayAudio (AudioSource) is not assigned.");

        if (grabbable == null) grabbable = GetComponent<Grabbable>();
        if (grabbable == null) Debug.LogError("[Extinguisher] No Grabbable found — grab detection won't work.");
    }

    private void Update()
    {
        UpdateHeldState();
        HandleSpray();
    }

    /// <summary>Poll the Grabbable so grab/release fire without any Inspector event wiring.</summary>
    private void UpdateHeldState()
    {
        if (grabbable == null) return;
        bool held = grabbable.SelectingPointsCount > 0;
        if (held && !isHoldingExtinguisher) OnExtinguisherGrabbed();
        else if (!held && isHoldingExtinguisher) OnExtinguisherReleased();
    }

    // ---- Grab callbacks: wire to PointableUnityEventWrapper (WhenSelect / WhenUnselect) ----

    public void OnExtinguisherGrabbed()
    {
        isHoldingExtinguisher = true;
        if (autoPullPinOnGrab) PullPin();
        //if (performanceTracker != null)
        //    performanceTracker.OnExtinguisherFound(Time.time - performanceTracker.startTime);
        Debug.Log("[Extinguisher] Grabbed.");
    }

    public void OnExtinguisherReleased()
    {
        isHoldingExtinguisher = false;
        StopSpraying();
        Debug.Log("[Extinguisher] Released.");
    }

    /// <summary>Pull the safety pin (call from a poke/button, or via autoPullPinOnGrab).</summary>
    public void PullPin()
    {
        if (isPinPulled) return;
        isPinPulled = true;
        if (pinObject != null) pinObject.SetActive(false);
        Debug.Log("[Extinguisher] Pin pulled.");
    }

    private void HandleSpray()
    {
        bool triggerHeld =
            OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger) > triggerThreshold ||
            OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger) > triggerThreshold;

        if (isHoldingExtinguisher && isPinPulled && triggerHeld && sprayTimer < maxSprayDuration)
        {
            if (!isSpraying)
            {
                isSpraying = true;
                if (fireSuppressant != null && !fireSuppressant.isPlaying) fireSuppressant.Play();
                if (sprayAudio != null && !sprayAudio.isPlaying) sprayAudio.Play();
                Debug.Log("[Extinguisher] Spraying!");
            }
            sprayTimer += Time.deltaTime;
            DetectFireInSpray();
        }
        else
        {
            StopSpraying();
        }
    }

    private void StopSpraying()
    {
        if (fireSuppressant != null) fireSuppressant.Stop();
        if (sprayAudio != null && sprayAudio.isPlaying) sprayAudio.Stop();
        isSpraying = false;
    }

    /// <summary>
    /// Hook for the scoring/extinguish logic. Finds fire colliders within the spray cone.
    /// Later this ties into the SpawnableItem fire/extinguisher A-B matching.
    /// </summary>
    private void DetectFireInSpray()
    {
        if (fireSuppressant == null) return;
        var hits = Physics.OverlapSphere(fireSuppressant.transform.position, sprayRadius, fireLayer);
        // TODO: for each hit, resolve its SpawnableItem and extinguish if the class matches.
        if (hits.Length > 0) Debug.Log($"[Extinguisher] Spray hitting {hits.Length} fire collider(s).");
    }
}
