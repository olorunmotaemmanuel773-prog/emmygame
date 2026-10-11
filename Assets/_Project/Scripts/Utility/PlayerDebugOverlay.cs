using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Temporary on-screen readout for testing Milestone 1 (FPS, speed, stance).
/// Press F3 to hide/show. Remove or disable before release builds.
/// </summary>
public class PlayerDebugOverlay : MonoBehaviour
{
    [SerializeField] private PlayerController player;

    private bool visible = true;
    private float smoothedDeltaTime = 0.016f;
    private GUIStyle style;

    private void Awake()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        if (player == null) Debug.LogWarning("PlayerDebugOverlay: no PlayerController found.", this);
    }

    private void Update()
    {
        smoothedDeltaTime = Mathf.Lerp(smoothedDeltaTime, Time.unscaledDeltaTime, 0.05f);
        if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame) visible = !visible;
    }

    private void OnGUI()
    {
        if (!visible || player == null) return;

        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(Screen.height * 0.022f) };
            style.normal.textColor = Color.white;
        }

        string state = player.IsCrouching ? "Crouching" : (player.IsSprinting ? "Sprinting" : "Standing/Walking");
        string text =
            $"FPS: {1f / smoothedDeltaTime:0}\n" +
            $"Speed: {player.CurrentSpeed:0.0} m/s\n" +
            $"Grounded: {player.IsGrounded}   State: {state}\n" +
            $"Mouse sensitivity: {player.MouseSensitivity:0.00}  ( [ and ] to change )\n" +
            "Esc: free cursor   Click: recapture   F3: hide this";
        GUI.Label(new Rect(12f, 10f, Screen.width, Screen.height * 0.25f), text, style);
    }
}
