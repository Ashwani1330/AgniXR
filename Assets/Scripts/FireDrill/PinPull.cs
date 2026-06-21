using UnityEngine;
using Oculus.Interaction;

/// <summary>
/// Put this on the extinguisher's safety Pin. When the player grabs the pin and pulls it away
/// from its socket past <see cref="pullThreshold"/>, it removes the pin from the extinguisher
/// (FireExtinguisher.PullPin) — which hides the pin and unlocks the spray. Like yanking the pin
/// on a real extinguisher.
///
/// The Pin must be grabbable (its own Grabbable + interactable). Set the extinguisher's
/// FireExtinguisher to: Is Pin Pulled = OFF, Auto Pull Pin On Grab = OFF, so only this manual
/// pull removes it.
/// </summary>
public class PinPull : MonoBehaviour
{
    [Tooltip("The extinguisher this pin belongs to. Auto-found in parents if empty.")]
    [SerializeField] private FireExtinguisher extinguisher;

    [Tooltip("The pin's Grabbable. Auto-found on this object if empty.")]
    [SerializeField] private Grabbable grabbable;

    [Tooltip("How far (metres) the pin must be pulled from where you grabbed it to come out.")]
    [SerializeField] private float pullThreshold = 0.05f;

    private Vector3 grabStartPos;
    private bool tracking;
    private bool pulled;

    private void Awake()
    {
        if (grabbable == null) grabbable = GetComponent<Grabbable>();
        if (extinguisher == null) extinguisher = GetComponentInParent<FireExtinguisher>();
        if (grabbable == null) Debug.LogError("[PinPull] No Grabbable on the pin.");
        if (extinguisher == null) Debug.LogError("[PinPull] No FireExtinguisher found in parents.");
    }

    private void Update()
    {
        if (pulled || grabbable == null) return;

        bool held = grabbable.SelectingPointsCount > 0;
        if (!held)
        {
            tracking = false;   // released before pulling far enough — reset
            return;
        }

        if (!tracking)
        {
            grabStartPos = transform.position;   // where the pin was when grabbed
            tracking = true;
        }

        if (Vector3.Distance(transform.position, grabStartPos) >= pullThreshold)
        {
            pulled = true;
            if (extinguisher != null) extinguisher.PullPin();   // hides pin + unlocks spray
            Debug.Log("[PinPull] Pin yanked out.");
        }
    }
}
