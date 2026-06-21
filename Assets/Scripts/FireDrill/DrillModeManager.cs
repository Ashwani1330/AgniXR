using UnityEngine;
using TMPro;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;

/// <summary>
/// Switches between CREATE mode (author the scene — place fires/extinguishers) and PLAY mode
/// (run the drill). Entering PLAY:
///   - makes every Fire non-pickable (disables its grab interactables),
///   - starts the drill timer,
///   - (AnchorProbe stops spawning — it checks <see cref="IsInPlayMode"/>).
/// The extinguisher stays grabbable so the player can pick it up and fight the fire.
///
/// Bound to a controller button (default: right thumbstick click).
/// </summary>
public class DrillModeManager : MonoBehaviour
{
    public enum DrillMode { Create, Play }

    [Header("Input")]
    [Tooltip("Button that toggles Create <-> Play.")]
    [SerializeField] private OVRInput.Button toggleButton = OVRInput.Button.Three; // X

    [Header("UI (optional)")]
    [Tooltip("Shows current mode and the running drill timer.")]
    [SerializeField] private TMP_Text statusLabel;

    /// <summary>Static so other scripts (e.g. AnchorProbe) can gate behaviour without a reference.</summary>
    public static bool IsInPlayMode { get; private set; }

    public DrillMode Mode { get; private set; } = DrillMode.Create;
    public float Elapsed => Mode == DrillMode.Play ? Time.time - startTime : 0f;

    private float startTime;

    private void Start() => UpdateLabel();

    private void Update()
    {
        if (OVRInput.GetDown(toggleButton)) Toggle();
        if (Mode == DrillMode.Play) UpdateLabel();   // tick the timer
    }

    public void Toggle()
    {
        if (Mode == DrillMode.Create) EnterPlay();
        else EnterCreate();
    }

    public void EnterPlay()
    {
        Mode = DrillMode.Play;
        IsInPlayMode = true;
        startTime = Time.time;
        SetFiresGrabbable(false);
        UpdateLabel();
        Debug.Log("[Drill] PLAY mode — drill started. Fires locked; go grab the extinguisher!");
    }

    public void EnterCreate()
    {
        Mode = DrillMode.Create;
        IsInPlayMode = false;
        SetFiresGrabbable(true);
        UpdateLabel();
        Debug.Log("[Drill] CREATE mode — authoring enabled.");
    }

    /// <summary>Enable/disable grab on every Fire in the scene (extinguishers are untouched).</summary>
    private void SetFiresGrabbable(bool grabbable)
    {
        foreach (var fire in FindObjectsByType<Fire>(FindObjectsSortMode.None))
        {
            foreach (var hg in fire.GetComponentsInChildren<HandGrabInteractable>(true)) hg.enabled = grabbable;
            foreach (var g in fire.GetComponentsInChildren<GrabInteractable>(true)) g.enabled = grabbable;
        }
    }

    private void UpdateLabel()
    {
        if (statusLabel == null) return;
        statusLabel.text = Mode == DrillMode.Play
            ? $"DRILL RUNNING\n{Elapsed:0.0}s"
            : "CREATE MODE\n(click stick to start)";
    }
}
