using UnityEngine;

/// <summary>
/// Keeps the Guardian/boundary permanently suppressed so the player can roam the whole room
/// during a fire drill — no fixed play-area limit. Meta's supported "boundaryless" path for
/// passthrough/MR apps.
///
/// It re-asserts every frame, so if the runtime ever turns the boundary back on (e.g. passthrough
/// momentarily drops), it is suppressed again right away. OVRManager applies the request once
/// passthrough is active and the system grants it.
///
/// Requirements:
///  - Passthrough must be ACTIVE (your MR scene has it) — the system refuses suppression in VR.
///  - Recent Quest runtime (OVRPlugin 1.98+); on older runtimes it's a no-op and the boundary stays.
/// </summary>
public class BoundarySuppressor : MonoBehaviour
{
    private void Update()
    {
        var m = OVRManager.instance;
        if (m != null && !m.shouldBoundaryVisibilityBeSuppressed)
            m.shouldBoundaryVisibilityBeSuppressed = true;
    }
}
