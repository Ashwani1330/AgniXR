using System.IO;
using UnityEngine;

/// <summary>
/// The scenario JSON round-trip. Build and test THIS first (the de-risk milestone): one object
/// saved and then placed back at its exact authored position. Files live under
/// Application.persistentDataPath so they survive on-device on the Quest.
/// </summary>
public static class ScenarioSerializer
{
    public static string PathFor(string scenarioId) =>
        Path.Combine(Application.persistentDataPath, $"scenario_{scenarioId}.json");

    public static void Save(ScenarioData scenario)
    {
        if (scenario == null) { Debug.LogError("[Scenario] Save called with null scenario."); return; }
        string path = PathFor(scenario.scenarioId);
        File.WriteAllText(path, ToJson(scenario));
        Debug.Log($"[Scenario] Saved {scenario.items.Count} item(s) -> {path}");
    }

    public static ScenarioData Load(string scenarioId)
    {
        string path = PathFor(scenarioId);
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[Scenario] No scenario file at {path}");
            return null;
        }
        var data = FromJson(File.ReadAllText(path));
        Debug.Log($"[Scenario] Loaded {data?.items.Count ?? 0} item(s) <- {path}");
        return data;
    }

    public static bool Exists(string scenarioId) => File.Exists(PathFor(scenarioId));

    public static string ToJson(ScenarioData scenario) => JsonUtility.ToJson(scenario, true);
    public static ScenarioData FromJson(string json) => JsonUtility.FromJson<ScenarioData>(json);
}
