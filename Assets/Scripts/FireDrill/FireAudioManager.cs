using UnityEngine;

/// <summary>
/// ONE shared fire-audio source for the whole scene (singleton) — no matter how many fires exist,
/// there is a single looping fire sound. Its volume scales with the TOTAL fire intensity (the sum
/// of every live fire's size), so it swells as fire spreads and dims as fires are extinguished.
/// When no fire remains it fades out and stops completely.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class FireAudioManager : MonoBehaviour
{
    public static FireAudioManager Instance { get; private set; }

    [Header("Audio")]
    [SerializeField] private AudioSource source;

    [Tooltip("Loudest the fire can get (volume at/above 'Intensity For Max').")]
    [SerializeField, Range(0f, 1f)] private float maxVolume = 1f;

    [Tooltip("Total fire intensity (sum of fire sizes) that maps to max volume.")]
    [SerializeField] private float intensityForMax = 2f;

    [Tooltip("How fast the volume eases toward its target (higher = snappier).")]
    [SerializeField] private float fadeSpeed = 2f;

    [Header("Positioning (optional, for 3D audio)")]
    [Tooltip("Move the source to the nearest fire so it sounds like it comes from the flames.")]
    [SerializeField] private bool followNearestFire = false;

    [Tooltip("Listener (camera) used to find the nearest fire. Auto-uses Camera.main if empty.")]
    [SerializeField] private Transform listener;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }   // enforce singleton
        Instance = this;

        if (source == null) source = GetComponent<AudioSource>();
        source.loop = true;
        source.playOnAwake = false;
        source.volume = 0f;

        if (listener == null && Camera.main != null) listener = Camera.main.transform;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        // Sum intensity across all live fires; track the nearest for optional 3D positioning.
        float total = 0f;
        Fire nearest = null;
        float bestSqr = float.MaxValue;

        foreach (var fire in Fire.Active)
        {
            if (fire == null) continue;
            total += fire.Intensity;

            if (followNearestFire && listener != null)
            {
                float d = (fire.transform.position - listener.position).sqrMagnitude;
                if (d < bestSqr) { bestSqr = d; nearest = fire; }
            }
        }

        // Volume tracks total fire intensity, eased so it dims smoothly while extinguishing.
        float target = total <= 0f
            ? 0f
            : Mathf.Clamp01(total / Mathf.Max(0.001f, intensityForMax)) * maxVolume;
        source.volume = Mathf.MoveTowards(source.volume, target, fadeSpeed * Time.deltaTime);

        if (followNearestFire && nearest != null)
            transform.position = nearest.transform.position;

        // Play while audible; stop entirely once dimmed out and no fire remains.
        if (source.volume > 0.001f)
        {
            if (!source.isPlaying) source.Play();
        }
        else if (source.isPlaying && total <= 0f)
        {
            source.Stop();
        }
    }
}
