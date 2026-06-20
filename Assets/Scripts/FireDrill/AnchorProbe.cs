using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Android;

/// <summary>
/// STEP 1 de-risk test — no UI, no SpawnableItem yet. Proves a single OVRSpatialAnchor persists
/// across an app restart, in the same physical spot.
///
/// Controls (right Touch controller):
///   Right Trigger  -> create + save an anchor at the controller's position (drops a marker)
///   A (Button.One) -> load every saved anchor and drop a marker at each
///   B (Button.Two) -> forget the saved UUIDs (local list only; doesn't erase from device)
///
/// UUIDs are stored in PlayerPrefs, so the test is: place one or two, QUIT the app, relaunch,
/// press A — the markers should reappear exactly where you left them in the real room.
/// </summary>
public class AnchorProbe : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("OVRCameraRig > TrackingSpace > RightControllerAnchor (or RightHandAnchor).")]
    [SerializeField] private Transform rightController;

    [Tooltip("Any small prefab to mark an anchor (a cube is fine).")]
    [SerializeField] private GameObject markerPrefab;

    [Header("Behaviour")]
    [Tooltip("Automatically reload saved anchors once permission is granted (on app start).")]
    [SerializeField] private bool autoLoadOnStart = true;

    private const string PrefKey = "anchor_probe_uuids";

    // Spatial-data storage (anchor persistence) is gated behind this RUNTIME permission.
    // Declaring it in AndroidManifest.xml is not enough — it must be granted at launch.
    private const string ScenePermission = "com.oculus.permission.USE_SCENE";

    private void Start()
    {
        if (Permission.HasUserAuthorizedPermission(ScenePermission))
        {
            Debug.Log("[Probe] Spatial Data permission already granted.");
            if (autoLoadOnStart) _ = LoadMarkers();
            return;
        }

        Debug.Log("[Probe] Requesting Spatial Data permission...");
        var callbacks = new PermissionCallbacks();
        callbacks.PermissionGranted += permission =>
        {
            Debug.Log("[Probe] Spatial Data permission GRANTED.");
            if (autoLoadOnStart) _ = LoadMarkers();
        };
        callbacks.PermissionDenied += permission =>
            Debug.LogError("[Probe] Spatial Data permission DENIED — anchors cannot persist. " +
                           "Grant it via Quest Settings > Apps > (this app) > Permissions.");
        Permission.RequestUserPermission(ScenePermission, callbacks);
    }

    private async void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger))
            await PlaceAnchor();

        if (OVRInput.GetDown(OVRInput.Button.One))
            await LoadMarkers();

        if (OVRInput.GetDown(OVRInput.Button.Two))
            ClearSaved();
    }

    private async Task PlaceAnchor()
    {
        if (markerPrefab == null || rightController == null)
        {
            Debug.LogError("[Probe] Assign rightController and markerPrefab.");
            return;
        }

        // Spawn FIRST — this always happens so the trigger gives immediate visual feedback,
        // independent of permissions/anchoring. If no cylinder appears here, the problem is
        // input (controllers asleep / hand-tracking active / stale build), not anchoring.
        var go = Instantiate(markerPrefab, rightController.position, rightController.rotation);
        Debug.Log("[Probe] Trigger -> spawned marker at controller.");

        // Anchoring/persistence is the only part that needs the runtime permission.
        if (!Permission.HasUserAuthorizedPermission(ScenePermission))
        {
            Debug.LogError("[Probe] Spatial Data permission NOT granted — cylinder spawned but will NOT anchor/persist.");
            return;
        }

        var uuid = await SpatialAnchorStore.CreateAndSaveAsync(go);
        if (uuid.HasValue) Remember(uuid.Value);
    }

    private async Task LoadMarkers()
    {
        var uuids = LoadSaved();
        Debug.Log($"[Probe] LoadMarkers: {uuids.Count} UUID(s) in PlayerPrefs.");
        if (uuids.Count == 0) { Debug.Log("[Probe] Nothing to load (PlayerPrefs empty)."); return; }

        await SpatialAnchorStore.LoadAndBindAsync(uuids, anchor =>
        {
            var m = Instantiate(markerPrefab, anchor.transform);
            m.transform.localPosition = Vector3.zero;
            m.transform.localRotation = Quaternion.identity;
            Debug.Log($"[Probe] Re-spawned marker on anchor {anchor.Uuid}.");
        });
    }

    // ----------------------------------------------- tiny UUID persistence (PlayerPrefs)

    private void Remember(Guid uuid)
    {
        var list = LoadSaved();
        list.Add(uuid);
        var joined = string.Join(",", list);
        PlayerPrefs.SetString(PrefKey, joined);
        PlayerPrefs.Save();
        Debug.Log($"[Probe] Remembered {uuid}. PlayerPrefs now: \"{joined}\" ({list.Count} total).");
    }

    private List<Guid> LoadSaved()
    {
        var list = new List<Guid>();
        var s = PlayerPrefs.GetString(PrefKey, "");
        Debug.Log($"[Probe] PlayerPrefs raw value: \"{s}\".");
        if (!string.IsNullOrEmpty(s))
            foreach (var part in s.Split(','))
                if (Guid.TryParse(part, out var g)) list.Add(g);
        return list;
    }

    private void ClearSaved()
    {
        PlayerPrefs.DeleteKey(PrefKey);
        PlayerPrefs.Save();
        Debug.Log("[Probe] Forgot all saved UUIDs.");
    }
}
