using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

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
        cam.backgroundColor = new Color(0.02f, 0.02f, 0.05f);
        cam.orthographic = true;
        camGO.tag = "MainCamera";

        // Canvas
        GameObject canvasGO = new GameObject("Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // --- BACKGROUND IMAGE (behind everything) ---
        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        RectTransform bgRT = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(1f, 1f, 1f, 0.35f); // semi-transparent, assign sprite in Inspector

        // --- MAIN PANEL (transparent so background shows) ---
        GameObject mainPanel = new GameObject("MainPanel");
        mainPanel.transform.SetParent(canvasGO.transform, false);
        RectTransform mainRT = mainPanel.AddComponent<RectTransform>();
        mainRT.anchorMin = Vector2.zero;
        mainRT.anchorMax = Vector2.one;
        mainRT.offsetMin = Vector2.zero;
        mainRT.offsetMax = Vector2.zero;
        Image mainImg = mainPanel.AddComponent<Image>();
        mainImg.color = new Color(0f, 0f, 0f, 0f); // fully transparent

        // Title
        var titleGO = CreateTMPText("Title", mainPanel.transform,
            new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.75f), Vector2.zero,
            new Vector2(900f, 110f), "Return of the Labyrinth", 72, TextAlignmentOptions.Center);

        // Subtitle
        var subGO = CreateTMPText("Subtitle", mainPanel.transform,
            new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.65f), Vector2.zero,
            new Vector2(600f, 50f), "There is no escape", 28, TextAlignmentOptions.Center);
        subGO.GetComponent<TextMeshProUGUI>().color = new Color(1f, 1f, 1f, 0.6f);

        // Buttons — Play, Settings, Credits, Quit evenly spaced
        GameObject playBtn     = CreateButton("PlayButton",     mainPanel.transform, new Vector2(0.5f, 0.50f), new Vector2(220f, 60f), "PLAY");
        GameObject settingsBtn = CreateButton("SettingsButton", mainPanel.transform, new Vector2(0.5f, 0.40f), new Vector2(220f, 60f), "SETTINGS");
        GameObject creditsBtn  = CreateButton("CreditsButton",  mainPanel.transform, new Vector2(0.5f, 0.30f), new Vector2(220f, 60f), "CREDITS");
        GameObject quitBtn     = CreateButton("QuitButton",     mainPanel.transform, new Vector2(0.5f, 0.20f), new Vector2(220f, 60f), "QUIT");

        // --- SETTINGS PANEL ---
        GameObject settingsPanel = CreateSolidPanel("SettingsPanel", canvasGO.transform, new Color(0f, 0f, 0f, 0.93f));

        CreateTMPText("SettingsTitle", settingsPanel.transform,
            new Vector2(0.5f, 0.85f), new Vector2(0.5f, 0.85f), Vector2.zero,
            new Vector2(600f, 70f), "SETTINGS", 52, TextAlignmentOptions.Center);

        // Volume
        CreateTMPText("VolumeLabel", settingsPanel.transform,
            new Vector2(0.5f, 0.70f), new Vector2(0.5f, 0.70f), Vector2.zero,
            new Vector2(400f, 40f), "Volume", 30, TextAlignmentOptions.Center);
        Slider slider = CreateSlider("VolumeSlider", settingsPanel.transform, new Vector2(0.5f, 0.63f));

        // Seed
        CreateTMPText("SeedLabel", settingsPanel.transform,
            new Vector2(0.5f, 0.53f), new Vector2(0.5f, 0.53f), Vector2.zero,
            new Vector2(400f, 40f), "Maze Seed", 30, TextAlignmentOptions.Center);
        TMP_InputField seedInput = CreateInputField("SeedInput", settingsPanel.transform, new Vector2(0.5f, 0.46f), "12345");

        // Difficulty
        CreateTMPText("DiffLabel", settingsPanel.transform,
            new Vector2(0.5f, 0.36f), new Vector2(0.5f, 0.36f), Vector2.zero,
            new Vector2(400f, 40f), "Difficulty", 30, TextAlignmentOptions.Center);
        GameObject leftBtn  = CreateButton("DiffLeft",  settingsPanel.transform, new Vector2(0.35f, 0.28f), new Vector2(60f, 50f), "<");
        GameObject diffLabelGO = CreateTMPText("DifficultyValue", settingsPanel.transform,
            new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.28f), Vector2.zero,
            new Vector2(200f, 50f), "Normal", 32, TextAlignmentOptions.Center);
        GameObject rightBtn = CreateButton("DiffRight", settingsPanel.transform, new Vector2(0.65f, 0.28f), new Vector2(60f, 50f), ">");
        GameObject settingsBackBtn = CreateButton("BackButton", settingsPanel.transform, new Vector2(0.5f, 0.12f), new Vector2(220f, 55f), "BACK");

        // --- CREDITS PANEL ---
        GameObject creditsPanel = CreateSolidPanel("CreditsPanel", canvasGO.transform, new Color(0.06f, 0.06f, 0.08f, 0.95f));

        // Credits title
        CreateTMPText("CreditsTitle", creditsPanel.transform,
            new Vector2(0.5f, 0.88f), new Vector2(0.5f, 0.88f), Vector2.zero,
            new Vector2(600f, 60f), "CREDITS", 48, TextAlignmentOptions.Center);

        // Credits text — centered box with proper sizing
        GameObject creditsTextGO = new GameObject("CreditsText");
        creditsTextGO.transform.SetParent(creditsPanel.transform, false);
        RectTransform creditsTextRT = creditsTextGO.AddComponent<RectTransform>();
        creditsTextRT.anchorMin = new Vector2(0.2f, 0.2f);
        creditsTextRT.anchorMax = new Vector2(0.8f, 0.82f);
        creditsTextRT.offsetMin = Vector2.zero;
        creditsTextRT.offsetMax = Vector2.zero;
        TextMeshProUGUI creditsTMP = creditsTextGO.AddComponent<TextMeshProUGUI>();
        creditsTMP.text =
            "Game Design & Development\n" +
            "    Martin Ethan S. Lalas\n\n" +
            "Art Assets\n" +
            "    Asset Credits Here\n\n" +
            "Special Thanks\n" +
            "    Denmarc, Yanni, Zernan, Angelo";
        creditsTMP.fontSize = 26;
        creditsTMP.alignment = TextAlignmentOptions.TopLeft;
        creditsTMP.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        creditsTMP.enableWordWrapping = true;

        // Back button
        GameObject creditsBackBtn = CreateButton("CreditsBackButton", creditsPanel.transform,
            new Vector2(0.5f, 0.1f), new Vector2(220f, 55f), "BACK");

        // --- MENU MANAGER ---
        GameObject managerGO = new GameObject("MainMenuManager");
        MainMenuManager menuManager = managerGO.AddComponent<MainMenuManager>();
        menuManager.mainPanel = mainPanel;
        menuManager.settingsPanel = settingsPanel;
        menuManager.creditsPanel = creditsPanel;
        menuManager.volumeSlider = slider;
        menuManager.seedInputField = seedInput;
        menuManager.difficultyLabel = diffLabelGO.GetComponent<TextMeshProUGUI>();
        menuManager.creditsTextDisplay = creditsTMP;
        menuManager.backgroundImage = bgImg;
        menuManager.backgroundOpacity = 0.35f;
        menuManager.mazeSceneName = "MazeScene";
        menuManager.creditsText =
            "Game Design & Development\n" +
            "    Martin Ethan S. Lalas\n\n" +
            "Art Assets\n" +
            "    Asset Credits Here\n\n" +
            "Special Thanks\n" +
            "    Denmarc, Yanni, Zernan, Angelo";

        // Wire buttons
        WireButton(playBtn,          menuManager, "OnPlayClicked");
        WireButton(settingsBtn,      menuManager, "OnSettingsClicked");
        WireButton(creditsBtn,       menuManager, "OnCreditsClicked");
        WireButton(quitBtn,          menuManager, "OnQuitClicked");
        WireButton(leftBtn,          menuManager, "OnDifficultyLeft");
        WireButton(rightBtn,         menuManager, "OnDifficultyRight");
        WireButton(settingsBackBtn,  menuManager, "OnBackClicked");
        WireButton(creditsBackBtn,   menuManager, "OnBackClicked");

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

        // Save
        string path = "Assets/Scenes/MainMenu.unity";
        EditorSceneManager.SaveScene(scene, path);

        var buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool found = false;
        foreach (var s in buildScenes) if (s.path == path) { found = true; break; }
        if (!found) buildScenes.Insert(0, new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = buildScenes.ToArray();

        Debug.Log("[MainMenuSceneBuilder] Done. Assign your background sprite to Background Image on MainMenuManager.");
    }

    // ---------------------------------------------------------------
    // HELPERS
    // ---------------------------------------------------------------

    private static GameObject CreateSolidPanel(string name, Transform parent, Color color)
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

    private static GameObject CreateButton(string name, Transform parent, Vector2 anchor, Vector2 sizeDelta, string label)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = sizeDelta;
        Image img = go.AddComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);
        Button btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(0.3f, 0.3f, 0.4f);
        cb.pressedColor = new Color(0.1f, 0.1f, 0.15f);
        btn.colors = cb;
        btn.targetGraphic = img;
        // Label child
        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        RectTransform labelRT = labelGO.AddComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero; labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = Vector2.zero; labelRT.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 28;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        return go;
    }

    private static Slider CreateSlider(string name, Transform parent, Vector2 anchor)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor;
        rt.sizeDelta = new Vector2(400f, 30f);
        rt.anchoredPosition = Vector2.zero;
        Slider slider = go.AddComponent<Slider>();
        slider.minValue = 0f; slider.maxValue = 1f; slider.value = 1f;

        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(go.transform, false);
        RectTransform bgRT = bg.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
        bg.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f);

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(go.transform, false);
        RectTransform faRT = fillArea.AddComponent<RectTransform>();
        faRT.anchorMin = Vector2.zero; faRT.anchorMax = Vector2.one;
        faRT.offsetMin = Vector2.zero; faRT.offsetMax = Vector2.zero;
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRT = fill.AddComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = Vector2.zero; fillRT.offsetMax = Vector2.zero;
        fill.AddComponent<Image>().color = new Color(0.9f, 0.7f, 0.2f);
        slider.fillRect = fillRT;

        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(go.transform, false);
        RectTransform haRT = handleArea.AddComponent<RectTransform>();
        haRT.anchorMin = Vector2.zero; haRT.anchorMax = Vector2.one;
        haRT.offsetMin = Vector2.zero; haRT.offsetMax = Vector2.zero;
        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform handleRT = handle.AddComponent<RectTransform>();
        handleRT.sizeDelta = new Vector2(20f, 30f);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;
        slider.handleRect = handleRT;
        slider.targetGraphic = handleImg;
        return slider;
    }

    private static TMP_InputField CreateInputField(string name, Transform parent, Vector2 anchor, string defaultText)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor;
        rt.sizeDelta = new Vector2(300f, 45f);
        rt.anchoredPosition = Vector2.zero;
        go.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);
        TMP_InputField input = go.AddComponent<TMP_InputField>();
        input.contentType = TMP_InputField.ContentType.IntegerNumber;
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        RectTransform textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero; textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(8, 0); textRT.offsetMax = new Vector2(-8, 0);
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = defaultText;
        tmp.fontSize = 26;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        input.textComponent = tmp;
        input.text = defaultText;
        return input;
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
