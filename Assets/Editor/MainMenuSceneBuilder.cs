using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Menu > Maze > Build Main Menu Scene
/// </summary>
public class MainMenuSceneBuilder
{
    [MenuItem("Maze/Build Main Menu Scene")]
    public static void BuildMainMenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera
        GameObject camGO = new GameObject("Main Camera");
        Camera cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.05f, 0.1f);
        cam.orthographic = true;
        camGO.tag = "MainCamera";

        // Canvas
        GameObject canvasGO = new GameObject("Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // --- MAIN PANEL ---
        GameObject mainPanel = CreatePanel("MainPanel", canvasGO.transform, new Color(0f, 0f, 0f, 0.85f));

        // Title
        CreateTMPText("Title", mainPanel.transform,
            new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.75f), Vector2.zero,
            new Vector2(800f, 100f), "MEGA MAZE", 72, TextAlignmentOptions.Center);

        // Subtitle
        var sub = CreateTMPText("Subtitle", mainPanel.transform,
            new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.65f), Vector2.zero,
            new Vector2(600f, 50f), "Can you escape before you fall asleep?", 28, TextAlignmentOptions.Center);
        sub.GetComponent<TextMeshProUGUI>().color = new Color(1f, 1f, 1f, 0.6f);

        // Buttons
        GameObject playBtn = CreateButton("PlayButton", mainPanel.transform,
            new Vector2(0.5f, 0.45f), new Vector2(200f, 60f), "PLAY");
        GameObject settingsBtn = CreateButton("SettingsButton", mainPanel.transform,
            new Vector2(0.5f, 0.33f), new Vector2(200f, 60f), "SETTINGS");
        GameObject quitBtn = CreateButton("QuitButton", mainPanel.transform,
            new Vector2(0.5f, 0.21f), new Vector2(200f, 60f), "QUIT");

        // --- SETTINGS PANEL ---
        GameObject settingsPanel = CreatePanel("SettingsPanel", canvasGO.transform, new Color(0f, 0f, 0f, 0.92f));

        CreateTMPText("SettingsTitle", settingsPanel.transform,
            new Vector2(0.5f, 0.82f), new Vector2(0.5f, 0.82f), Vector2.zero,
            new Vector2(600f, 70f), "SETTINGS", 52, TextAlignmentOptions.Center);

        // Volume
        CreateTMPText("VolumeLabel", settingsPanel.transform,
            new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.68f), Vector2.zero,
            new Vector2(400f, 40f), "Volume", 30, TextAlignmentOptions.Center);
        GameObject sliderGO = new GameObject("VolumeSlider");
        sliderGO.transform.SetParent(settingsPanel.transform, false);
        RectTransform sliderRT = sliderGO.AddComponent<RectTransform>();
        sliderRT.anchorMin = new Vector2(0.5f, 0.61f);
        sliderRT.anchorMax = new Vector2(0.5f, 0.61f);
        sliderRT.sizeDelta = new Vector2(400f, 30f);
        sliderRT.anchoredPosition = Vector2.zero;
        Slider slider = sliderGO.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        // Slider background
        GameObject sliderBg = new GameObject("Background");
        sliderBg.transform.SetParent(sliderGO.transform, false);
        RectTransform bgRT = sliderBg.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
        Image bgImg = sliderBg.AddComponent<Image>();
        bgImg.color = new Color(0.3f, 0.3f, 0.3f);
        // Slider fill area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderGO.transform, false);
        RectTransform fillAreaRT = fillArea.AddComponent<RectTransform>();
        fillAreaRT.anchorMin = Vector2.zero; fillAreaRT.anchorMax = Vector2.one;
        fillAreaRT.offsetMin = Vector2.zero; fillAreaRT.offsetMax = Vector2.zero;
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRT = fill.AddComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = Vector2.zero; fillRT.offsetMax = Vector2.zero;
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.9f, 0.7f, 0.2f);
        slider.fillRect = fillRT;
        // Slider handle
        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderGO.transform, false);
        RectTransform handleAreaRT = handleArea.AddComponent<RectTransform>();
        handleAreaRT.anchorMin = Vector2.zero; handleAreaRT.anchorMax = Vector2.one;
        handleAreaRT.offsetMin = Vector2.zero; handleAreaRT.offsetMax = Vector2.zero;
        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform handleRT = handle.AddComponent<RectTransform>();
        handleRT.sizeDelta = new Vector2(20f, 30f);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;
        slider.handleRect = handleRT;
        slider.targetGraphic = handleImg;

        // Seed
        CreateTMPText("SeedLabel", settingsPanel.transform,
            new Vector2(0.5f, 0.50f), new Vector2(0.5f, 0.50f), Vector2.zero,
            new Vector2(400f, 40f), "Maze Seed", 30, TextAlignmentOptions.Center);
        GameObject seedGO = new GameObject("SeedInput");
        seedGO.transform.SetParent(settingsPanel.transform, false);
        RectTransform seedRT = seedGO.AddComponent<RectTransform>();
        seedRT.anchorMin = new Vector2(0.5f, 0.43f);
        seedRT.anchorMax = new Vector2(0.5f, 0.43f);
        seedRT.sizeDelta = new Vector2(300f, 45f);
        seedRT.anchoredPosition = Vector2.zero;
        Image seedBg = seedGO.AddComponent<Image>();
        seedBg.color = new Color(0.2f, 0.2f, 0.2f);
        TMP_InputField seedInput = seedGO.AddComponent<TMP_InputField>();
        seedInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        GameObject seedTextGO = new GameObject("Text");
        seedTextGO.transform.SetParent(seedGO.transform, false);
        RectTransform seedTextRT = seedTextGO.AddComponent<RectTransform>();
        seedTextRT.anchorMin = Vector2.zero; seedTextRT.anchorMax = Vector2.one;
        seedTextRT.offsetMin = new Vector2(8, 0); seedTextRT.offsetMax = new Vector2(-8, 0);
        TextMeshProUGUI seedTMP = seedTextGO.AddComponent<TextMeshProUGUI>();
        seedTMP.text = "12345";
        seedTMP.fontSize = 26;
        seedTMP.alignment = TextAlignmentOptions.Center;
        seedTMP.color = Color.white;
        seedInput.textComponent = seedTMP;
        seedInput.text = "12345";

        // Difficulty
        CreateTMPText("DiffLabel", settingsPanel.transform,
            new Vector2(0.5f, 0.33f), new Vector2(0.5f, 0.33f), Vector2.zero,
            new Vector2(400f, 40f), "Difficulty", 30, TextAlignmentOptions.Center);
        // Left arrow
        GameObject leftBtn = CreateButton("DiffLeft", settingsPanel.transform,
            new Vector2(0.35f, 0.25f), new Vector2(60f, 50f), "<");
        // Difficulty label
        GameObject diffLabelGO = CreateTMPText("DifficultyValue", settingsPanel.transform,
            new Vector2(0.5f, 0.25f), new Vector2(0.5f, 0.25f), Vector2.zero,
            new Vector2(200f, 50f), "Normal", 32, TextAlignmentOptions.Center);
        // Right arrow
        GameObject rightBtn = CreateButton("DiffRight", settingsPanel.transform,
            new Vector2(0.65f, 0.25f), new Vector2(60f, 50f), ">");

        // Back button
        GameObject backBtn = CreateButton("BackButton", settingsPanel.transform,
            new Vector2(0.5f, 0.12f), new Vector2(200f, 55f), "BACK");

        // --- MENU MANAGER ---
        GameObject managerGO = new GameObject("MainMenuManager");
        MainMenuManager menuManager = managerGO.AddComponent<MainMenuManager>();
        menuManager.mainPanel = mainPanel;
        menuManager.settingsPanel = settingsPanel;
        menuManager.volumeSlider = slider;
        menuManager.seedInputField = seedInput;
        menuManager.difficultyLabel = diffLabelGO.GetComponent<TextMeshProUGUI>();
        menuManager.mazeSceneName = "MazeScene";

        // Wire buttons
        WireButton(playBtn, menuManager, "OnPlayClicked");
        WireButton(settingsBtn, menuManager, "OnSettingsClicked");
        WireButton(quitBtn, menuManager, "OnQuitClicked");
        WireButton(leftBtn, menuManager, "OnDifficultyLeft");
        WireButton(rightBtn, menuManager, "OnDifficultyRight");
        WireButton(backBtn, menuManager, "OnBackClicked");

        // Volume slider OnValueChanged
        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            slider.onValueChanged,
            (UnityEngine.Events.UnityAction<float>)menuManager.OnVolumeChanged);

        // Event system
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // Save scene
        string path = "Assets/Scenes/MainMenu.unity";
        EditorSceneManager.SaveScene(scene, path);

        // Add to build settings
        var buildScenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool found = false;
        foreach (var s in buildScenes) if (s.path == path) { found = true; break; }
        if (!found) buildScenes.Insert(0, new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = buildScenes.ToArray();

        Debug.Log("[MainMenuSceneBuilder] Main menu scene built at " + path);
    }

    private static GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        Image img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    private static GameObject CreateTMPText(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos,
        Vector2 sizeDelta, string text, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        return go;
    }

    private static GameObject CreateButton(string name, Transform parent,
        Vector2 anchor, Vector2 sizeDelta, string label)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = sizeDelta;
        Image img = go.AddComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.2f);
        Button btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(0.3f, 0.3f, 0.4f);
        cb.pressedColor = new Color(0.1f, 0.1f, 0.15f);
        btn.colors = cb;
        btn.targetGraphic = img;
        CreateTMPText("Label", go.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, sizeDelta, label, 28, TextAlignmentOptions.Center);
        return go;
    }

    private static void WireButton(GameObject btnGO, MainMenuManager manager, string method)
    {
        Button btn = btnGO.GetComponent<Button>();
        if (btn == null) return;
        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            btn.onClick,
            (UnityEngine.Events.UnityAction)System.Delegate.CreateDelegate(
                typeof(UnityEngine.Events.UnityAction), manager, method));
    }
}
