using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Android;
using TMPro;

/// <summary>
/// Controller spawner + anchor test. Spawns one of two prefabs (Fire / Extinguisher) at the
/// right controller and persists each as an OVRSpatialAnchor so they return after a restart.
///
/// Controls (face buttons only — the index trigger is reserved for extinguisher spray):
///   A (Button.One)   -> spawn the SELECTED prefab at the right controller and anchor it
///   B (Button.Two)   -> switch which prefab is selected (Fire <-> Extinguisher)
///   X (Button.Three) -> reload all saved anchors (each respawns its own prefab)
///   Y (Button.Four)  -> forget saved UUIDs (local list only)
///
/// Each saved entry stores "uuid|prefabIndex", so reload knows whether to respawn Fire or
/// Extinguisher. UUIDs persist in PlayerPrefs.
/// </summary>
public class AnchorProbe : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("OVRCameraRig > TrackingSpace > RightControllerAnchor (or RightHandAnchor).")]
    [SerializeField] private Transform rightController;

    [Header("Spawnable prefabs (drag yours in)")]
    [Tooltip("Index 0 — spawned when 'Fire' is selected.")]
    [SerializeField] private GameObject firePrefab;
    [Tooltip("Index 1 — spawned when 'Extinguisher' is selected.")]
    [SerializeField] private GameObject extinguisherPrefab;

    [Header("UI")]
    [Tooltip("TMP text childed on the controller; shows the current selection and how to switch.")]
    [SerializeField] private TMP_Text selectionLabel;

    [Header("Behaviour")]
    [Tooltip("Automatically reload saved anchors once permission is granted (on app start).")]
    [SerializeField] private bool autoLoadOnStart = true;

    private const string PrefKey = "anchor_probe_entries";
    private const string ScenePermission = "com.oculus.permission.USE_SCENE";

    // 0 = Fire, 1 = Extinguisher
    private int selectedIndex = 0;

    private GameObject SelectedPrefab => selectedIndex == 0 ? firePrefab : extinguisherPrefab;
    private string SelectedName => selectedIndex == 0 ? "Fire" : "Extinguisher";
    private string OtherName => selectedIndex == 0 ? "Extinguisher" : "Fire";

    private void Start()
    {
        UpdateLabel();

        if (Permission.HasUserAuthorizedPermission(ScenePermission))
        {
            Debug.Log("[Probe] Spatial Data permission already granted.");
            if (autoLoadOnStart) _ = LoadSaved();
            return;
        }

        Debug.Log("[Probe] Requesting Spatial Data permission...");
        var callbacks = new PermissionCallbacks();
        callbacks.PermissionGranted += permission =>
        {
            Debug.Log("[Probe] Spatial Data permission GRANTED.");
            if (autoLoadOnStart) _ = LoadSaved();
        };
        callbacks.PermissionDenied += permission =>
            Debug.LogError("[Probe] Spatial Data permission DENIED — anchors cannot persist. " +
                           "Grant it via Quest Settings > Apps > (this app) > Permissions.");
        Permission.RequestUserPermission(ScenePermission, callbacks);
    }

    private async void Update()
    {
        // NOTE: the index trigger is reserved for gameplay (extinguisher spray), so all
        // authoring/probe actions live on face buttons to avoid input conflicts.
        if (OVRInput.GetDown(OVRInput.Button.One))    // A (right) -> spawn selected
            await PlaceSelected();

        if (OVRInput.GetDown(OVRInput.Button.Two))    // B (right) -> switch prefab
            ToggleSelection();

        if (OVRInput.GetDown(OVRInput.Button.Three))  // X (left)  -> reload saved
            await LoadSaved();

        if (OVRInput.GetDown(OVRInput.Button.Four))   // Y (left)  -> clear saved
            ClearSaved();
    }

    // ---------------------------------------------------------------- selection + label

    private void ToggleSelection()
    {
        selectedIndex = 1 - selectedIndex;
        UpdateLabel();
        Debug.Log($"[Probe] Selected prefab: {SelectedName}.");
    }

    private void UpdateLabel()
    {
        if (selectionLabel != null)
            selectionLabel.text = $"A = Spawn {SelectedName}\nB = switch to {OtherName}";
    }

    // ---------------------------------------------------------------- spawn + anchor

    private async Task PlaceSelected()
    {
        var prefab = SelectedPrefab;
        if (prefab == null || rightController == null)
        {
            Debug.LogError($"[Probe] Assign rightController and the {SelectedName} prefab.");
            return;
        }

        // Spawn first for immediate feedback, regardless of permission/anchoring.
        var go = Instantiate(prefab, rightController.position, rightController.rotation);
        Debug.Log($"[Probe] Trigger -> spawned {SelectedName}.");

        if (!Permission.HasUserAuthorizedPermission(ScenePermission))
        {
            Debug.LogError("[Probe] Spatial Data permission NOT granted — spawned but will NOT persist.");
            return;
        }

        var uuid = await SpatialAnchorStore.CreateAndSaveAsync(go);
        if (uuid.HasValue) Remember(uuid.Value, selectedIndex);
    }

    // ---------------------------------------------------------------- load

    private async Task LoadSaved()
    {
        var entries = LoadEntries();
        Debug.Log($"[Probe] LoadSaved: {entries.Count} entry(s) in PlayerPrefs.");
        if (entries.Count == 0) return;

        // Map uuid -> prefab index so each anchor respawns the prefab it was saved as.
        var indexByUuid = new Dictionary<Guid, int>();
        var uuids = new List<Guid>();
        foreach (var e in entries) { indexByUuid[e.Key] = e.Value; uuids.Add(e.Key); }

        await SpatialAnchorStore.LoadAndBindAsync(uuids, anchor =>
        {
            int idx = indexByUuid.TryGetValue(anchor.Uuid, out var i) ? i : 0;
            var prefab = idx == 0 ? firePrefab : extinguisherPrefab;
            if (prefab == null) return;

            var m = Instantiate(prefab, anchor.transform);
            m.transform.localPosition = Vector3.zero;
            m.transform.localRotation = Quaternion.identity;
            Debug.Log($"[Probe] Re-spawned {(idx == 0 ? "Fire" : "Extinguisher")} on anchor {anchor.Uuid}.");
        });
    }

    // ---------------------------------------------------------------- persistence (uuid|index)

    private void Remember(Guid uuid, int index)
    {
        var entries = LoadEntries();
        entries[uuid] = index;
        PlayerPrefs.SetString(PrefKey, Serialize(entries));
        PlayerPrefs.Save();
        Debug.Log($"[Probe] Remembered {uuid} as index {index}. ({entries.Count} total).");
    }

    private Dictionary<Guid, int> LoadEntries()
    {
        var dict = new Dictionary<Guid, int>();
        var s = PlayerPrefs.GetString(PrefKey, "");
        if (string.IsNullOrEmpty(s)) return dict;

        foreach (var pair in s.Split(','))
        {
            var parts = pair.Split('|');
            if (parts.Length == 2 && Guid.TryParse(parts[0], out var g) && int.TryParse(parts[1], out var idx))
                dict[g] = idx;
        }
        return dict;
    }

    private string Serialize(Dictionary<Guid, int> entries)
    {
        var parts = new List<string>(entries.Count);
        foreach (var kv in entries) parts.Add($"{kv.Key}|{kv.Value}");
        return string.Join(",", parts);
    }

    private void ClearSaved()
    {
        PlayerPrefs.DeleteKey(PrefKey);
        PlayerPrefs.Save();
        Debug.Log("[Probe] Forgot all saved entries.");
    }
}
