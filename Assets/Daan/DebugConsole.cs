using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DebugConsole : MonoBehaviour
{
    public static DebugConsole Instance;
    private Canvas debugCanvas;
    private bool isCanvasEnabled;
    private TextMeshProUGUI debugText; // Use TextMeshProUGUI
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateDebugCanvas(); // Create the canvas dynamically
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Create the Canvas and ScrollView dynamically
    private void CreateDebugCanvas()
    {
        // Create the Canvas
        GameObject canvasObject = new GameObject("debugCanvas");
        debugCanvas = canvasObject.AddComponent<Canvas>();
        debugCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // Set Canvas scaling
        CanvasScaler canvasScaler = canvasObject.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);
        canvasScaler.matchWidthOrHeight = 0.5f;

        // Add GraphicRaycaster to the canvas
        canvasObject.AddComponent<GraphicRaycaster>();

        // Make sure the Canvas persists across scenes
        DontDestroyOnLoad(canvasObject);

        // Create a ScrollView object
        GameObject scrollViewObject = new GameObject("ScrollView");
        scrollViewObject.transform.SetParent(debugCanvas.transform, false);

        // Set ScrollView size and position
        RectTransform scrollViewRect = scrollViewObject.AddComponent<RectTransform>();
        scrollViewRect.sizeDelta = new Vector2(960, 570);
        scrollViewRect.anchorMin = new Vector2(1, 0); // Bottom-right corner
        scrollViewRect.anchorMax = new Vector2(1, 0);
        scrollViewRect.pivot = new Vector2(1, 0);     // Pivot point at bottom-right
        scrollViewRect.anchoredPosition = new Vector2(0, 0); // Position based on the pivot

        // Add Image component for background with default black color and opacity
        Image scrollViewBackground = scrollViewObject.AddComponent<Image>();
        scrollViewBackground.color = new Color(0, 0, 0, 150f / 255f); // Black color with 150 opacity (59%)

        // Now create the ScrollView s content
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollViewObject.transform, false);

        RectTransform viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.sizeDelta = new Vector2(960, 570);
        viewportRect.anchorMin = new Vector2(0, 0);
        viewportRect.anchorMax = new Vector2(1, 1);
        viewportRect.pivot = new Vector2(0.5f, 0.5f);

        // Add Mask and Image components to Viewport
        Mask viewportMask = viewport.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false; // Disable Mask graphic to allow proper masking of content
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(0, 0, 0, 250f / 255f); // Black color with 150 opacity (59%)

        // Create Content GameObject (for the scrollable content)
        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);

        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.sizeDelta = new Vector2(960, 570); // Dynamically adjusted
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(0, 1);
        contentRect.pivot = new Vector2(0, 1);

        // Add Vertical Layout Group to Content (to handle dynamic arrangement of logs)
        VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.childControlHeight = true;
        contentLayout.childControlWidth = false;
        contentLayout.childForceExpandHeight = false;
        contentLayout.childForceExpandWidth = false;

        // Add ContentSizeFitter to Content to dynamically resize as new text is added
        ContentSizeFitter contentFitter = content.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Add Scroll Rect to ScrollView
        ScrollRect scrollRect = scrollViewObject.AddComponent<ScrollRect>();
        scrollRect.content = contentRect;
        scrollRect.viewport = viewportRect;
        scrollRect.horizontal = false;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

        // Create Vertical Scrollbar
        GameObject scrollbarObject = new GameObject("Scrollbar Vertical");
        scrollbarObject.transform.SetParent(scrollViewObject.transform, false);
        scrollbarObject.SetActive(true);

        scrollbarObject.AddComponent<CanvasRenderer>();
        RectTransform scrollbarRect = scrollbarObject.AddComponent<RectTransform>();
        scrollbarRect.sizeDelta = new Vector2(10, 570);
        scrollbarRect.anchorMin = new Vector2(1, 0);
        scrollbarRect.anchorMax = new Vector2(1, 1);
        scrollbarRect.pivot = new Vector2(1, 0.5f);

        // Ensure that the scrollbar does not stretch
        scrollbarRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0, scrollbarRect.sizeDelta.x); // Set position
        scrollbarRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, scrollbarRect.sizeDelta.y); // Set height

        // Add Scrollbar component and set its properties
        Scrollbar scrollbar = scrollbarObject.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        // Add Image component for the scrollbar background
        Image scrollbarImage = scrollbarObject.AddComponent<Image>();
        scrollbarImage.color = new Color(0.75f, 0.75f, 0.75f, 1); // Light gray color

        // Create the Sliding Area for the Scrollbar
        GameObject slidingAreaObject = new GameObject("Sliding Area");
        slidingAreaObject.transform.SetParent(scrollbarObject.transform, false);
        RectTransform slidingAreaRect = slidingAreaObject.AddComponent<RectTransform>();
        slidingAreaRect.anchorMin = new Vector2(0, 0);
        slidingAreaRect.anchorMax = new Vector2(1, 1);
        slidingAreaRect.offsetMin = Vector2.zero;
        slidingAreaRect.offsetMax = Vector2.zero;

        // Add Scrollbar Handle
        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(slidingAreaObject.transform, false);

        RectTransform handleRect = handle.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(20, 100); // Handle size
        handleRect.anchorMin = new Vector2(0, 0);
        handleRect.anchorMax = new Vector2(1, 1);
        handleRect.pivot = new Vector2(0.5f, 0.5f);

        // Add Image component for the scrollbar handle
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = new Color(0.5f, 0.5f, 0.5f, 1); // Dark gray color

        // Position the handle on the right side of the scrollbar
        handleRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, handleRect.sizeDelta.y); // Set top position
        handleRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0, handleRect.sizeDelta.x); // Set left position

        scrollbar.targetGraphic = handleImage;
        scrollbar.handleRect = handleRect;

        // Link Scrollbar to ScrollRect
        scrollRect.verticalScrollbar = scrollbar;

        // Create a new GameObject for the TMP_Text component
        GameObject textObject = new GameObject("DebugText");
        textObject.transform.SetParent(content.transform, false);
        TextMeshProUGUI debugTextComponent = textObject.AddComponent<TextMeshProUGUI>();
        debugTextComponent.fontSize = 24;
        debugTextComponent.color = Color.white;
        debugTextComponent.text = "";
        debugTextComponent.alignment = TextAlignmentOptions.Left;
        //debugTextComponent.fontStyle = FontStyles.Bold; // Make it bold

        // Load the font asset from Resources
        TMP_FontAsset fontAsset = Resources.Load<TMP_FontAsset>("Fonts/Amazon Ember Bold SDF");
        if (fontAsset != null)
        {
            debugTextComponent.font = fontAsset; // Apply the font
        }
        else
        {
            Debug.LogWarning("Font asset not found at Resources/Fonts/Amazon Ember Bold SDF");
        }

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(910, 100); // Set the size of the text box

        // Position the text appropriately
        rectTransform.anchoredPosition = Vector2.zero; // Center it within the parent
        debugText = debugTextComponent; // Store a reference to the TMP_Text component

        // Disable the canvas by default
        debugCanvas.gameObject.SetActive(false);

        // Start the coroutine to clear the log after 60 seconds
        StartCoroutine(ContinuousClearLog(60f)); // Start continuous clearing
    }
    private IEnumerator ContinuousClearLog(float delay)
    {
        while (true) // Loop indefinitely
        {
            yield return new WaitForSeconds(delay); // Wait for the specified duration
            if (debugText != null)
            {
                debugText.text = string.Empty; // Clear the text
            }
        }
    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (debugCanvas != null)
            {
                isCanvasEnabled = !isCanvasEnabled;
                debugCanvas.gameObject.SetActive(isCanvasEnabled);
            }
        }
    }

    public void Log(string logMessage)
    {
        debugText.text += logMessage + "\n";
    }

    public void LogWarning(string logMessage)
    {
        debugText.text += $"<color=#ffcc00>{logMessage}</color>\n";
    }

    public void LogError(string logMessage)
    {
        debugText.text += $"<color=#ff0000>{logMessage}</color>\n";
    }

    public void LogSuccess(string logMessage)
    {
        debugText.text += $"<color=#008000>{logMessage}</color>\n";
    }
}

