using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Adds a Back button to the existing CreditsPanel without rebuilding the whole scene.
/// </summary>
public class CreditsBackButtonAdder
{
    [MenuItem("Maze/Fix Credits Panel")]
    public static void FixCreditsPanel()
    {
        // Find CreditsPanel
        GameObject creditsPanel = GameObject.Find("CreditsPanel");
        if (creditsPanel == null)
        {
            Debug.LogError("CreditsPanel not found in scene.");
            return;
        }

        // Fix CreditsText RectTransform to stretch properly
        GameObject creditsText = creditsPanel.transform.Find("CreditsText")?.gameObject;
        if (creditsText != null)
        {
            RectTransform rt = creditsText.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(40f, 80f);  // leave room at bottom for back button
            rt.offsetMax = new Vector2(-40f, -40f);

            TextMeshProUGUI tmp = creditsText.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.alignment = TextAlignmentOptions.TopLeft;
                tmp.fontSize = 24;
                tmp.enableWordWrapping = true;
            }
        }

        // Fix CreditsPanel background
        Image panelImg = creditsPanel.GetComponent<Image>();
        if (panelImg != null)
            panelImg.color = new Color(0.08f, 0.08f, 0.08f, 0.93f);

        // Remove existing back button if already added
        Transform existing = creditsPanel.transform.Find("CreditsBackButton");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        // Add Back button at bottom center
        GameObject backBtn = new GameObject("CreditsBackButton");
        backBtn.transform.SetParent(creditsPanel.transform, false);
        RectTransform btnRT = backBtn.AddComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.5f, 0f);
        btnRT.anchorMax = new Vector2(0.5f, 0f);
        btnRT.anchoredPosition = new Vector2(0f, 40f);
        btnRT.sizeDelta = new Vector2(200f, 55f);

        Image btnImg = backBtn.AddComponent<Image>();
        btnImg.color = new Color(0.15f, 0.15f, 0.2f);

        Button btn = backBtn.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(0.3f, 0.3f, 0.4f);
        cb.pressedColor = new Color(0.1f, 0.1f, 0.15f);
        btn.colors = cb;
        btn.targetGraphic = btnImg;

        // Label
        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(backBtn.transform, false);
        RectTransform labelRT = labelGO.AddComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;
        TextMeshProUGUI label = labelGO.AddComponent<TextMeshProUGUI>();
        label.text = "BACK";
        label.fontSize = 28;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;

        // Wire to MainMenuManager.OnBackClicked
        MainMenuManager manager = Object.FindFirstObjectByType<MainMenuManager>();
        if (manager != null)
        {
            UnityEditor.Events.UnityEventTools.AddPersistentListener(
                btn.onClick,
                (UnityEngine.Events.UnityAction)System.Delegate.CreateDelegate(
                    typeof(UnityEngine.Events.UnityAction), manager, "OnBackClicked"));
            Debug.Log("[CreditsBackButtonAdder] Back button added and wired.");
        }
        else
        {
            Debug.LogWarning("[CreditsBackButtonAdder] MainMenuManager not found — wire OnBackClicked manually.");
        }

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
}
