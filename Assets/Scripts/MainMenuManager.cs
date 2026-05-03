using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Drives the main menu: Play, Settings, Credits, Quit.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject settingsPanel;
    public GameObject creditsPanel;

    [Header("Settings Controls")]
    public Slider volumeSlider;
    public TMP_InputField seedInputField;
    public TextMeshProUGUI difficultyLabel;

    [Header("Credits")]
    [TextArea(5, 20)]
    public string creditsText = "Game Design & Development\nYour Name\n\nArt Assets\nAsset Credits Here\n\nSpecial Thanks\nEveryone who helped";
    public TextMeshProUGUI creditsTextDisplay;

    [Header("Background")]
    public Image backgroundImage;
    public Sprite menuBackgroundSprite;
    [Range(0f, 1f)]
    public float backgroundOpacity = 0.4f;

    [Header("Scene")]
    public string mazeSceneName = "MazeScene";

    private int difficulty = 1;
    private readonly string[] difficultyNames = { "Easy", "Normal", "Hard" };

    private void Start()
    {
        // Apply background image if assigned
        if (backgroundImage != null && menuBackgroundSprite != null)
        {
            backgroundImage.sprite = menuBackgroundSprite;
            backgroundImage.color = new Color(1f, 1f, 1f, backgroundOpacity);
            backgroundImage.type = Image.Type.Simple;
            backgroundImage.preserveAspect = false;
        }

        // Load saved settings
        if (volumeSlider != null) volumeSlider.value = GameSettings.Volume;
        if (seedInputField != null) seedInputField.text = GameSettings.MazeSeed.ToString();
        difficulty = GameSettings.Difficulty;
        UpdateDifficultyLabel();

        AudioListener.volume = GameSettings.Volume;

        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);

        // Set credits text
        if (creditsTextDisplay != null)
            creditsTextDisplay.text = creditsText;
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

    public void OnCreditsClicked()
    {
        mainPanel.SetActive(false);
        creditsPanel.SetActive(true);
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
        if (settingsPanel != null && settingsPanel.activeSelf)
            settingsPanel.SetActive(false);
        if (creditsPanel != null && creditsPanel.activeSelf)
            creditsPanel.SetActive(false);
        mainPanel.SetActive(true);
    }

    // --- Helpers ---

    private void ApplySettings()
    {
        if (volumeSlider != null) GameSettings.Volume = volumeSlider.value;
        GameSettings.Difficulty = difficulty;

        if (seedInputField != null && int.TryParse(seedInputField.text, out int seed))
            GameSettings.MazeSeed = seed;

        AudioListener.volume = GameSettings.Volume;
    }

    private void UpdateDifficultyLabel()
    {
        if (difficultyLabel != null)
            difficultyLabel.text = difficultyNames[difficulty];
    }
}
