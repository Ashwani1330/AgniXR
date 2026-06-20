using System.Collections.Generic;
using UnityEngine;
using Meta.XR.MRUtilityKit;

/// <summary>
/// Closes the round-trip: SAVE the items currently in the scene to JSON, and LOAD a scenario
/// back, restoring every item at the exact pose it was authored at — relative to the room
/// anchor, so it lands in the right spot in any scanned space.
///
/// De-risk milestone: assign <see cref="itemPrefab"/>, call "Write Demo Scenario File", then
/// "Load Scenario" — one (or more) object appears at its authored position. Once that works,
/// the hardest problem in the build is solved.
///
/// Context-menu actions (right-click the component header in the Inspector) let you exercise
/// the whole round-trip without any UI:
///   Write Demo Scenario File  -> generates a sample JSON
///   Load Scenario             -> spawns items from JSON (Run mode)
///   Save Current Items        -> serializes scene items to JSON (Create mode capture)
///   Clear Spawned             -> removes items this runner spawned
/// </summary>
public class ScenarioRunner : MonoBehaviour
{
    [Header("Spawning")]
    [Tooltip("The single SpawnableItem prefab, parameterized by type.")]
    [SerializeField] private SpawnableItem itemPrefab;

    [Header("Room anchoring (the architecture-agnostic part)")]
    [Tooltip("When on, roomAnchor is auto-bound to the scanned room's MRUK floor anchor once " +
             "the scene loads — a tracked anchor MRUK keeps locked to the physical room. This is " +
             "what makes content stick to the ROOM, not the headset. Turn off only for flat " +
             "editor testing with a hand-assigned anchor (or none).")]
    [SerializeField] private bool useMrukRoomAnchor = true;

    [Tooltip("Scanned-room anchor that all item poses are stored relative to. Auto-filled from " +
             "MRUK when 'useMrukRoomAnchor' is on. Leave null for world space (editor testing only).")]
    [SerializeField] private Transform roomAnchor;

    [Tooltip("Parent for spawned items. Defaults to roomAnchor when left null.")]
    [SerializeField] private Transform spawnParent;

    [Header("Scenario")]
    [SerializeField] private string scenarioId = "demo";

    [Tooltip("Load the scenario automatically on Start (Run mode behaviour).")]
    [SerializeField] private bool loadOnStart = false;

    private readonly List<SpawnableItem> spawned = new List<SpawnableItem>();

    private void Start()
    {
        if (useMrukRoomAnchor && MRUK.Instance != null)
        {
            // Wait for MRUK to load & localize the scanned room, THEN bind the anchor and spawn.
            // Spawning before the room is localized would place items against an un-positioned anchor.
            MRUK.Instance.RegisterSceneLoadedCallback(OnRoomReady);
        }
        else if (loadOnStart)
        {
            LoadScenario();
        }
    }

    private void OnRoomReady()
    {
        BindRoomAnchorFromMruk();
        if (loadOnStart) LoadScenario();
    }

    /// <summary>
    /// Point <see cref="roomAnchor"/> at the scanned room's floor anchor — a tracked MRUK anchor
    /// that re-localizes to the same physical spot every session. This is the difference between
    /// "stuck to the room" and "stuck to the headset's boot position."
    /// </summary>
    public void BindRoomAnchorFromMruk()
    {
        var room = MRUK.Instance != null ? MRUK.Instance.GetCurrentRoom() : null;
        if (room == null || room.FloorAnchor == null)
        {
            Debug.LogWarning("[ScenarioRunner] No MRUK room/floor anchor yet — falling back to world space.");
            return;
        }
        roomAnchor = room.FloorAnchor.transform;
        Debug.Log($"[ScenarioRunner] Bound room anchor to floor of room '{room.name}' (uuid {CurrentRoomAnchorUuid()}).");
    }

    private string CurrentRoomAnchorUuid()
    {
        var room = MRUK.Instance != null ? MRUK.Instance.GetCurrentRoom() : null;
        return room != null && room.FloorAnchor != null ? room.FloorAnchor.Anchor.Uuid.ToString() : "";
    }

    // ---------------------------------------------------------------- Run mode: load + place

    [ContextMenu("Load Scenario")]
    public void LoadScenario()
    {
        var data = ScenarioSerializer.Load(scenarioId);
        if (data != null) Spawn(data);
    }

    public void Spawn(ScenarioData data)
    {
        if (itemPrefab == null) { Debug.LogError("[ScenarioRunner] itemPrefab is not assigned."); return; }
        if (data == null) return;

        ClearSpawned();
        Transform parent = spawnParent != null ? spawnParent : roomAnchor;

        foreach (var d in data.items)
        {
            var item = Instantiate(itemPrefab, parent);
            item.SetType(d.type);
            item.transform.SetPositionAndRotation(AnchorToWorldPos(d.position), AnchorToWorldRot(d.eulerAngles));
            item.transform.localScale = d.scale;
            spawned.Add(item);
        }
        Debug.Log($"[ScenarioRunner] Spawned {spawned.Count} item(s) for scenario '{data.scenarioId}'.");
    }

    // ----------------------------------------------------- Create-mode capture (round-trip test)

    [ContextMenu("Save Current Items")]
    public void SaveCurrentItems()
    {
        var data = new ScenarioData
        {
            scenarioId = scenarioId,
            anchorSpace = CurrentRoomAnchorUuid()
        };

        foreach (var item in FindObjectsByType<SpawnableItem>(FindObjectsSortMode.None))
        {
            data.items.Add(new SpawnableItemData(
                item.Type,
                WorldToAnchorPos(item.transform.position),
                WorldToAnchorEuler(item.transform.rotation),
                item.transform.localScale));
        }
        ScenarioSerializer.Save(data);
    }

    public void ClearSpawned()
    {
        foreach (var s in spawned)
            if (s != null) Destroy(s.gameObject);
        spawned.Clear();
    }

    // ---------------------------------------------------------------------- anchor <-> world

    private Vector3 AnchorToWorldPos(Vector3 anchorPos) =>
        roomAnchor != null ? roomAnchor.TransformPoint(anchorPos) : anchorPos;

    private Quaternion AnchorToWorldRot(Vector3 anchorEuler) =>
        roomAnchor != null ? roomAnchor.rotation * Quaternion.Euler(anchorEuler) : Quaternion.Euler(anchorEuler);

    private Vector3 WorldToAnchorPos(Vector3 worldPos) =>
        roomAnchor != null ? roomAnchor.InverseTransformPoint(worldPos) : worldPos;

    private Vector3 WorldToAnchorEuler(Quaternion worldRot) =>
        roomAnchor != null ? (Quaternion.Inverse(roomAnchor.rotation) * worldRot).eulerAngles : worldRot.eulerAngles;

    // ------------------------------------------------------------------- demo data generator

    [ContextMenu("Write Demo Scenario File")]
    public void WriteDemoScenarioFile()
    {
        var data = new ScenarioData { scenarioId = scenarioId, anchorSpace = CurrentRoomAnchorUuid() };
        data.items.Add(new SpawnableItemData(SpawnableItemType.Alarm,         new Vector3(-1.5f, 1.2f, 2.0f), Vector3.zero, Vector3.one));
        data.items.Add(new SpawnableItemData(SpawnableItemType.FireA,         new Vector3( 0.0f, 0.0f, 2.5f), Vector3.zero, Vector3.one));
        data.items.Add(new SpawnableItemData(SpawnableItemType.ExtinguisherA, new Vector3( 1.2f, 0.0f, 1.0f), Vector3.zero, Vector3.one));
        data.items.Add(new SpawnableItemData(SpawnableItemType.FireB,         new Vector3( 2.0f, 0.0f, 3.0f), Vector3.zero, Vector3.one));
        data.items.Add(new SpawnableItemData(SpawnableItemType.ExtinguisherB, new Vector3( 1.8f, 0.0f, 1.2f), Vector3.zero, Vector3.one));
        data.items.Add(new SpawnableItemData(SpawnableItemType.Endpoint,      new Vector3(-2.0f, 0.0f, 0.0f), Vector3.zero, Vector3.one));
        ScenarioSerializer.Save(data);
    }
}
