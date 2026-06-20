using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One placed item in a saved scenario. Pose is stored RELATIVE to the room anchor so the
/// drill is architecture-agnostic — it restores to the same spot in any scanned room, not a
/// hardcoded world position. (If no anchor is used, these are plain world-space values.)
/// JsonUtility-friendly: only plain fields, Vector3, and an enum (serialized as its int value).
/// </summary>
[Serializable]
public class SpawnableItemData
{
    public SpawnableItemType type;
    public Vector3 position;
    public Vector3 eulerAngles;
    public Vector3 scale = Vector3.one;

    public SpawnableItemData() { }

    public SpawnableItemData(SpawnableItemType type, Vector3 position, Vector3 eulerAngles, Vector3 scale)
    {
        this.type = type;
        this.position = position;
        this.eulerAngles = eulerAngles;
        this.scale = scale;
    }
}

/// <summary>
/// A whole authored drill: an id plus the list of placed items. This is the JSON that Create
/// mode writes and Run mode reads back. Serialized with UnityEngine.JsonUtility.
/// </summary>
[Serializable]
public class ScenarioData
{
    public string scenarioId = "demo";

    [Tooltip("UUID of the scanned-room anchor (MRUK floor anchor) these poses are relative to. " +
             "Used on load to confirm/select the right physical room. Empty = world space (editor).")]
    public string anchorSpace = "";

    public List<SpawnableItemData> items = new List<SpawnableItemData>();
}
