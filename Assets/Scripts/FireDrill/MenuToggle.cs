using UnityEngine;

/// <summary>
/// Shows/hides a UI object (e.g. the Text_UI canvas) with the left-controller menu button, and
/// while shown makes it follow the controller — driven directly from OVRInput each frame, so it
/// works regardless of the rig's prefab hierarchy (parenting to controller-visual nodes is
/// unreliable in the OVRInteractionComprehensive rig).
///
/// Button.Start is the left menu button — the right Oculus/home button is reserved by the system.
/// </summary>
public class MenuToggle : MonoBehaviour
{
    [Tooltip("The UI to toggle on/off — your Text_UI canvas.")]
    [SerializeField] private GameObject menuUI;

    [Tooltip("Left-controller menu button by default.")]
    [SerializeField] private OVRInput.Button toggleButton = OVRInput.Button.Start;

    [Header("Follow controller")]
    [Tooltip("Which controller the menu rides on.")]
    [SerializeField] private OVRInput.Controller controller = OVRInput.Controller.LTouch;

    [Tooltip("Position offset from the controller (in controller space).")]
    [SerializeField] private Vector3 positionOffset = new Vector3(0f, 0.05f, 0.05f);

    [Tooltip("Rotation offset (euler) from the controller.")]
    [SerializeField] private Vector3 rotationOffset = new Vector3(45f, 0f, 0f);

    private Transform trackingSpace;

    private void Awake()
    {
        var rig = FindFirstObjectByType<OVRCameraRig>();
        if (rig != null) trackingSpace = rig.trackingSpace;
        if (trackingSpace == null)
            Debug.LogError("[MenuToggle] No OVRCameraRig/trackingSpace found — menu can't follow the controller.");
    }

    private void Update()
    {
        if (OVRInput.GetDown(toggleButton) && menuUI != null)
            menuUI.SetActive(!menuUI.activeSelf);
    }

    private void LateUpdate()
    {
        if (menuUI == null || !menuUI.activeSelf || trackingSpace == null) return;

        // Controller pose is reported in tracking-space local coords; convert to world.
        Vector3 localPos = OVRInput.GetLocalControllerPosition(controller);
        Quaternion localRot = OVRInput.GetLocalControllerRotation(controller);
        Vector3 worldPos = trackingSpace.TransformPoint(localPos);
        Quaternion worldRot = trackingSpace.rotation * localRot;

        menuUI.transform.SetPositionAndRotation(
            worldPos + worldRot * positionOffset,
            worldRot * Quaternion.Euler(rotationOffset));
    }
}
