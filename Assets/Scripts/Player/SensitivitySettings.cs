using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Standalone sensitivity settings widget.
/// Can be placed on the Pause screen or as a floating HUD element.
/// Reads/writes directly to PlayerController.Sensitivity.
/// Persists the chosen sensitivity between sessions via PlayerPrefs.
/// </summary>
public class SensitivitySettings : MonoBehaviour
{
    private const string SensitivityKey = "MazeGame_Sensitivity";

    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI valueLabel;
    [SerializeField] private float minValue = 0.1f;
    [SerializeField] private float maxValue = 5.0f;
    [SerializeField] private float defaultValue = 1.0f;

    private PlayerController _player;

    private void Start()
    {
        _player = FindFirstObjectByType<PlayerController>();

        float saved = PlayerPrefs.GetFloat(SensitivityKey, defaultValue);

        if (slider != null)
        {
            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.value    = saved;
            slider.onValueChanged.AddListener(OnValueChanged);
        }

        if (_player != null) _player.Sensitivity = saved;
        UpdateLabel(saved);
    }

    private void OnValueChanged(float value)
    {
        if (_player != null) _player.Sensitivity = value;
        PlayerPrefs.SetFloat(SensitivityKey, value);
        PlayerPrefs.Save();
        UpdateLabel(value);
    }

    private void UpdateLabel(float value)
    {
        if (valueLabel != null) valueLabel.text = value.ToString("F1") + "x";
    }
}
