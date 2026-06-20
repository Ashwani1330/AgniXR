using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Design B core. Wraps Meta's OVRSpatialAnchor async API so the rest of the app never touches
/// the raw calls. Each placed item owns ONE spatial anchor; persistence = remembering its UUID.
/// Meta re-localizes that anchor to the exact same physical spot across sessions — that is what
/// makes content stick to the room rather than the headset.
///
/// Only two operations are needed to de-risk Design B: create+save, and load+bind.
/// (Erase comes later, once persistence is proven.)
/// </summary>
public static class SpatialAnchorStore
{
    /// <summary>
    /// Attach a spatial anchor to <paramref name="target"/>, wait for it to localize, and save it.
    /// Returns the anchor's UUID on success, or null on failure.
    /// </summary>
    public static async Task<Guid?> CreateAndSaveAsync(GameObject target)
    {
        var anchor = target.GetComponent<OVRSpatialAnchor>();
        if (anchor == null) anchor = target.AddComponent<OVRSpatialAnchor>();

        Debug.Log("[Anchor] Created component, waiting to localize...");
        if (!await anchor.WhenLocalizedAsync())
        {
            Debug.LogError("[Anchor] New anchor failed to localize (look around the room and retry).");
            return null;
        }
        Debug.Log($"[Anchor] Localized {anchor.Uuid}, saving to device store...");

        var result = await OVRSpatialAnchor.SaveAnchorsAsync(new[] { anchor });
        if (!result.Success)
        {
            Debug.LogError($"[Anchor] Save FAILED: {result.Status} (uuid {anchor.Uuid}).");
            return null;
        }

        Debug.Log($"[Anchor] Save OK: {anchor.Uuid} (status {result.Status}).");
        return anchor.Uuid;
    }

    /// <summary>
    /// Load anchors by UUID, localize each, bind it to a fresh GameObject, and hand that bound
    /// anchor to <paramref name="onBound"/> so the caller can attach/position its own content
    /// (a marker now, a SpawnableItem later). Anchors that fail to localize are skipped.
    /// </summary>
    public static async Task LoadAndBindAsync(IEnumerable<Guid> uuids, Action<OVRSpatialAnchor> onBound)
    {
        var requested = new List<Guid>(uuids);
        Debug.Log($"[Anchor] Fetching {requested.Count} anchor(s) from device store...");

        var unbound = new List<OVRSpatialAnchor.UnboundAnchor>();
        var result = await OVRSpatialAnchor.LoadUnboundAnchorsAsync(requested, unbound);
        if (!result.Success)
        {
            Debug.LogError($"[Anchor] Fetch FAILED: {result.Status}");
            return;
        }
        Debug.Log($"[Anchor] Fetch OK: {unbound.Count}/{requested.Count} found in store (status {result.Status}).");

        foreach (var ub in unbound)
        {
            if (!await ub.LocalizeAsync())
            {
                Debug.LogWarning($"[Anchor] Could not localize {ub.Uuid} (insufficient view?).");
                continue;
            }

            var go = new GameObject($"Anchor_{ub.Uuid}");
            var anchor = go.AddComponent<OVRSpatialAnchor>();
            ub.BindTo(anchor);
            onBound?.Invoke(anchor);
        }

        Debug.Log($"[Anchor] Loaded/bound {unbound.Count} anchor(s).");
    }
}
