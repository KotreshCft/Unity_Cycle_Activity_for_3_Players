using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Represents a leaderboard, containing a list of entries.
/// </summary>
[System.Serializable]
public class Leaderboard
{
    public List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
}

/// <summary>
/// Represents an entry in the leaderboard.
/// </summary>
[System.Serializable]
public class LeaderboardEntry
{
    public string teamName;
    public string activityTime;
    public string distanceTraveled;

    public LeaderboardEntry(string teamName, string activityTime, string distanceTraveled)
    {
        this.teamName = teamName;
       // this.activityTime = activityTime;
        this.activityTime = distanceTraveled;
        this.distanceTraveled = distanceTraveled;
    }
}

/// <summary>
/// Manages the display and management of a leaderboard, including adding entries, sorting, and clearing the leaderboard.
/// </summary>
public class Leaderboard_V1 : MonoBehaviour
{
    [Header("UI Components")]
    public GameObject leaderboardScreen;  // The screen that displays the leaderboard.
    public Transform entryContainer;      // The container for leaderboard entries.
    public Transform entryTemplate;       // The template used to create leaderboard entries.

    [Header("Rank Images")]
    public Sprite firstRankImage;         // Image for the first rank.
    public Sprite secondRankImage;        // Image for the second rank.
    public Sprite thirdRankImage;         // Image for the third rank.
    public Sprite defaultRankImage;       // Default image for other ranks.

    public float templateHeight = 80f;

    private List<Transform> highscoreEntryTransformList;
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) {
            ClearLeaderboard();
        }
    }
    /// <summary>
    /// Loads and displays the leaderboard entries.
    /// </summary>
    /*public void ShowLeaderboard()
    {
        // Load the leaderboard data from PlayerPrefs.
        string jsonString = PlayerPrefs.GetString("Leaderboard", "{}");
        Debug.Log("Loaded JSON for displaying leaderboard: " + jsonString);

        Leaderboard leaderboard = LoadLeaderboard(jsonString);

        // Sort the leaderboard entries by activity time.
        leaderboard.entries.Sort((x, y) => x.activityTime.CompareTo(y.activityTime));

        // Clear previous leaderboard entries in the list.
        highscoreEntryTransformList = new List<Transform>();

        // Display the top 7 leaderboard entries with a delay between each.
        StartCoroutine(DisplayLeaderboardEntries(leaderboard.entries));

        // Display the top 7 leaderboard entries.
        for (int i = 0; i < leaderboard.entries.Count && i < 7; i++)
        {
            LeaderboardEntry entry = leaderboard.entries[i];
            CreateHighscoreEntryTransform(entry, entryContainer, highscoreEntryTransformList, i + 1);
        }
    }*/
    /// <summary>
    /// Loads and displays the leaderboard entries.
    /// </summary>
    public void ShowLeaderboard()
    {
        // Load the leaderboard data from PlayerPrefs.
        string jsonString = PlayerPrefs.GetString("Leaderboard", "{}");
        Debug.Log("Loaded JSON for displaying leaderboard: " + jsonString);

        Leaderboard leaderboard = LoadLeaderboard(jsonString);

        // Sort the leaderboard entries by activity time.
        leaderboard.entries.Sort((x, y) => x.activityTime.CompareTo(y.activityTime));

        // Clear previous leaderboard entries in the list.
        highscoreEntryTransformList = new List<Transform>();

        // Display the top 7 leaderboard entries with a delay between each.
        StartCoroutine(DisplayLeaderboardEntries(leaderboard.entries));
    }


    private IEnumerator DisplayLeaderboardEntries(List<LeaderboardEntry> entries)
    {
        for (int i = 0; i < entries.Count && i < 7; i++)
        {
            LeaderboardEntry entry = entries[i];
            CreateHighscoreEntryTransform(entry, entryContainer, highscoreEntryTransformList, i + 1);
            yield return new WaitForSeconds(0.2f); // Wait before showing the next entry
        }
    }

    /// <summary>
    /// Creates and displays a leaderboard entry in the UI.
    /// </summary>
    /// <param name="entry">The leaderboard entry to display.</param>
    /// <param name="container">The container to hold the entry.</param>
    /// <param name="transformList">The list of transforms representing the displayed entries.</param>
    /// <param name="rank">The rank of the entry.</param>
    private void CreateHighscoreEntryTransform(LeaderboardEntry entry, Transform container, List<Transform> transformList, int rank)
    {
        if (entryTemplate == null || container == null || transformList == null)
        {
            Debug.LogError("Missing references in CreateHighscoreEntryTransform");
            return;
        }

        float templateHeight = 80f;
        Transform entryTransform = Instantiate(entryTemplate, container);
        if (entryTransform == null)
        {
            Debug.LogError("Failed to instantiate entry template");
            return;
        }

        RectTransform entryRectTransform = entryTransform.GetComponent<RectTransform>();
        entryRectTransform.anchoredPosition = new Vector2(0, -templateHeight * transformList.Count);
        entryTransform.gameObject.SetActive(true);

        // Set the rank text with an ordinal suffix.
        TMP_Text rankText = entryTransform.Find("rankText")?.GetComponent<TMP_Text>();
        if (rankText != null)
        {
            rankText.text = $"{rank}{GetOrdinalSuffix(rank)}";
        }

        // Set the team name text.
        TMP_Text teamText = entryTransform.Find("nameText")?.GetComponent<TMP_Text>();
        if (teamText != null)
        {
            teamText.text = entry.teamName;
        }

        // Set the activity time text.
        TMP_Text activityTimeText = entryTransform.Find("timeText")?.GetComponent<TMP_Text>();
        if (activityTimeText != null)
        {
            activityTimeText.text = entry.activityTime;
        }

        // Set the background panel image based on the rank.
        Image backgroundPanel = entryTransform.Find("Panel")?.GetComponent<Image>();
        if (backgroundPanel != null)
        {
            switch (rank)
            {
                case 1:
                    backgroundPanel.sprite = firstRankImage;
                    break;
                case 2:
                    backgroundPanel.sprite = secondRankImage;
                    break;
                case 3:
                    backgroundPanel.sprite = thirdRankImage;
                    break;
                default:
                    backgroundPanel.sprite = defaultRankImage;
                    break;
            }

            // Alternate row color for readability.
            backgroundPanel.color = (rank % 2 == 1) ? Color.white : new Color(0.9f, 0.9f, 0.9f);
        }

        // Add the entry transform to the list.
        transformList.Add(entryTransform);

        // Start the transition (slide-in and fade-in)
        StartCoroutine(AnimateEntry(entryRectTransform, entryTransform.GetComponent<CanvasGroup>()));
    }
    private IEnumerator AnimateEntry(RectTransform rectTransform, CanvasGroup canvasGroup)
    {
        float duration = 0.5f; // Duration of the slide-in and fade-in
        float elapsedTime = 0f;
        Vector2 startPosition = rectTransform.anchoredPosition;
        Vector2 endPosition = new Vector2(0, startPosition.y); // Slide to the center position

        canvasGroup.alpha = 0f; // Start fully transparent

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, endPosition, t);
            canvasGroup.alpha = t;

            yield return null;
        }

        rectTransform.anchoredPosition = endPosition;
        canvasGroup.alpha = 1f;
    }

    /// <summary>
    /// Returns the appropriate ordinal suffix for a given number.
    /// </summary>
    /// <param name="number">The number to get the suffix for.</param>
    /// <returns>The ordinal suffix ("st", "nd", "rd", "th").</returns>
    private string GetOrdinalSuffix(int number)
    {
        if (number <= 0) return "";

        switch (number % 100)
        {
            case 11:
            case 12:
            case 13:
                return "th";
            default:
                switch (number % 10)
                {
                    case 1: return "st";
                    case 2: return "nd";
                    case 3: return "rd";
                    default: return "th";
                }
        }
    }

    /// <summary>
    /// Clears the leaderboard data from PlayerPrefs.
    /// </summary>
    public void ClearLeaderboard()
    {
        // Create a new empty leaderboard.
        Leaderboard emptyLeaderboard = new Leaderboard();

        // Serialize the empty leaderboard to JSON and save it to PlayerPrefs.
        string emptyJson = JsonUtility.ToJson(emptyLeaderboard);
        PlayerPrefs.SetString("Leaderboard", emptyJson);
        PlayerPrefs.Save();

        Debug.Log("Leaderboard cleared.");

        // Refresh the leaderboard display.
        ShowLeaderboard();
    }

    /// <summary>
    /// Loads the leaderboard data from a JSON string.
    /// </summary>
    /// <param name="jsonString">The JSON string representing the leaderboard.</param>
    /// <returns>The deserialized leaderboard object.</returns>
    private Leaderboard LoadLeaderboard(string jsonString)
    {
        Leaderboard leaderboard;

        try
        {
            leaderboard = JsonUtility.FromJson<Leaderboard>(jsonString);
            if (leaderboard == null)
            {
                throw new System.Exception("Leaderboard is null after deserialization");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to deserialize leaderboard. Initializing new leaderboard. Error: " + e.Message);
            leaderboard = new Leaderboard();
            string defaultJsonString = JsonUtility.ToJson(leaderboard);
            PlayerPrefs.SetString("Leaderboard", defaultJsonString);
            PlayerPrefs.Save();
            jsonString = PlayerPrefs.GetString("Leaderboard", "{}");
            leaderboard = JsonUtility.FromJson<Leaderboard>(jsonString);
        }

        return leaderboard;
    }
}
