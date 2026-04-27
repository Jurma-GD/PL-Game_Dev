using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Drives the main menu: Play, Settings panel, Quit.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject settingsPanel;

    [Header("Settings Controls")]
    public Slider volumeSlider;
    public TMP_InputField seedInputField;
    public TextMeshProUGUI difficultyLabel;

    [Header("Scene")]
    public string mazeSceneName = "MazeScene";

    private int difficulty = 1;
    private readonly string[] difficultyNames = { "Easy", "Normal", "Hard" };

    private void Start()
    {
        // Load saved settings
        volumeSlider.value = GameSettings.Volume;
        seedInputField.text = GameSettings.MazeSeed.ToString();
        difficulty = GameSettings.Difficulty;
        UpdateDifficultyLabel();

        AudioListener.volume = GameSettings.Volume;

        settingsPanel.SetActive(false);
        mainPanel.SetActive(true);
    }

    // --- Main Panel ---

    public void OnPlayClicked()
    {
        ApplySettings();
        SceneManager.LoadScene(mazeSceneName);
    }

    public void OnSettingsClicked()
    {
        mainPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void OnQuitClicked()
    {
        Application.Quit();
    }

    // --- Settings Panel ---

    public void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
    }

    public void OnDifficultyLeft()
    {
        difficulty = (difficulty - 1 + difficultyNames.Length) % difficultyNames.Length;
        UpdateDifficultyLabel();
    }

    public void OnDifficultyRight()
    {
        difficulty = (difficulty + 1) % difficultyNames.Length;
        UpdateDifficultyLabel();
    }

    public void OnBackClicked()
    {
        ApplySettings();
        settingsPanel.SetActive(false);
        mainPanel.SetActive(true);
    }

    // --- Helpers ---

    private void ApplySettings()
    {
        GameSettings.Volume = volumeSlider.value;
        GameSettings.Difficulty = difficulty;

        if (int.TryParse(seedInputField.text, out int seed))
            GameSettings.MazeSeed = seed;

        AudioListener.volume = GameSettings.Volume;
    }

    private void UpdateDifficultyLabel()
    {
        if (difficultyLabel != null)
            difficultyLabel.text = difficultyNames[difficulty];
    }
}
