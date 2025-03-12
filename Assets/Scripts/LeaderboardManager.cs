using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
//[System.Serializable]
//public class LeaderboardEntry
//{
//    public string teamName;
//    public float distanceTraveled; 

//    public LeaderboardEntry(string teamName, float distanceTraveled)
//    {
//        this.teamName = teamName;
//        this.distanceTraveled = distanceTraveled;
//    }
//}


//[System.Serializable]
//public class Leaderboard
//{
//    public List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
//}

public class LeaderboardManager : MonoBehaviour
{
    //public Transform entryContainer;
    //public Transform entryTemplate;

    //public Sprite firstRankImage;
    //public Sprite secondRankImage;
    //public Sprite thirdRankImage;
    //public Sprite defaultRankImage;

    //private List<Transform> highscoreEntryTransformList;
    //void Update()
    //{
    //    if (Input.GetKeyDown(KeyCode.Space))
    //    {
    //        ClearLeaderboard();
    //    }
    //}

    //public void ShowLeaderboard()
    //{
    //    string jsonString = PlayerPrefs.GetString("Leaderboard", "{}");
    //    Debug.Log("Loaded JSON for displaying leaderboard: " + jsonString);

    //    Leaderboard leaderboard;
    //    try
    //    {
    //        leaderboard = JsonUtility.FromJson<Leaderboard>(jsonString);
    //        if (leaderboard == null)
    //        {
    //            throw new System.Exception("Leaderboard is null after deserialization");
    //        }
    //    }
    //    catch (System.Exception e)
    //    {
    //        Debug.LogError("Failed to deserialize leaderboard. Initializing new leaderboard. Error: " + e.Message);
    //        leaderboard = new Leaderboard();
    //        string defaultJsonString = JsonUtility.ToJson(leaderboard);
    //        PlayerPrefs.SetString("Leaderboard", defaultJsonString);
    //        PlayerPrefs.Save();
    //        jsonString = PlayerPrefs.GetString("Leaderboard", "{}");
    //        leaderboard = JsonUtility.FromJson<Leaderboard>(jsonString);
    //    }

    //    leaderboard.entries.Sort((x, y) => y.distanceTraveled.CompareTo(x.distanceTraveled)); // Sort by distance

    //    highscoreEntryTransformList = new List<Transform>();

    //    for (int i = 0; i < leaderboard.entries.Count && i < 10; i++)
    //    {
    //        LeaderboardEntry entry = leaderboard.entries[i];
    //        Debug.LogError($"Leaderboard Entry: TeamName = {entry.teamName}; DistanceTraveled = {entry.distanceTraveled}");

    //        // Format distance for display
    //        string formattedDistance = entry.distanceTraveled.ToString("F2");
    //        CreateHighscoreEntryTransform(entry, entryContainer, highscoreEntryTransformList, i + 1, formattedDistance);
    //    }
    //}


    //private void CreateHighscoreEntryTransform(LeaderboardEntry entry, Transform container, List<Transform> transformList, int rank, string formattedDistance)
    //{
    //    if (entryTemplate == null || container == null || transformList == null)
    //    {
    //        Debug.LogError("Missing references in CreateHighscoreEntryTransform");
    //        return;
    //    }

    //    float templateHeight = 80f;
    //    Transform entryTransform = Instantiate(entryTemplate, container);

    //    if (entryTransform == null)
    //    {
    //        Debug.LogError("Failed to instantiate entry template");
    //        return;
    //    }

    //    RectTransform entryRectTransform = entryTransform.GetComponent<RectTransform>();
    //    entryRectTransform.anchoredPosition = new Vector2(0, -templateHeight * transformList.Count);
    //    entryTransform.gameObject.SetActive(true);

    //    TMP_Text rankText = entryTransform.Find("rankText")?.GetComponent<TMP_Text>();
    //    if (rankText != null)
    //    {
    //        rankText.text = $"{rank}{GetOrdinalSuffix(rank)}";
    //    }

    //    TMP_Text teamText = entryTransform.Find("nameText")?.GetComponent<TMP_Text>();
    //    if (teamText != null)
    //    {
    //        teamText.text = entry.teamName;
    //    }

    //    TMP_Text distanceText = entryTransform.Find("timeText")?.GetComponent<TMP_Text>();
    //    if (distanceText != null)
    //    {
    //        distanceText.text = formattedDistance + "km";
    //        Debug.LogError($"Displayed Distance: {formattedDistance}");
    //    }

    //    Image backgroundPanel = entryTransform.Find("Panel")?.GetComponent<Image>();
    //    if (backgroundPanel != null)
    //    {
    //        switch (rank)
    //        {
    //            case 1:
    //                backgroundPanel.sprite = firstRankImage;
    //                break;
    //            case 2:
    //                backgroundPanel.sprite = secondRankImage;
    //                break;
    //            case 3:
    //                backgroundPanel.sprite = thirdRankImage;
    //                break;
    //            default:
    //                backgroundPanel.sprite = defaultRankImage;
    //                break;
    //        }

    //        backgroundPanel.color = (rank % 2 == 1) ? Color.white : new Color(0.9f, 0.9f, 0.9f);
    //    }

    //    transformList.Add(entryTransform);
    //}


    //private string GetOrdinalSuffix(int number)
    //{
    //    if (number <= 0) return "";

    //    switch (number % 100)
    //    {
    //        case 11:
    //        case 12:
    //        case 13:
    //            return "th";
    //        default:
    //            switch (number % 10)
    //            {
    //                case 1: return "st";
    //                case 2: return "nd";
    //                case 3: return "rd";
    //                default: return "th";
    //            }
    //    }
    //}
    //public void ClearLeaderboard()
    //{
    //    // Create a new empty leaderboard
    //    Leaderboard emptyLeaderboard = new Leaderboard();

    //    // Serialize the empty leaderboard to JSON
    //    string emptyJson = JsonUtility.ToJson(emptyLeaderboard);

    //    // Save the empty JSON to PlayerPrefs
    //    PlayerPrefs.SetString("Leaderboard", emptyJson);
    //    PlayerPrefs.Save();

    //    Debug.Log("Leaderboard cleared.");

    //    // Optionally, you can call ShowLeaderboard() to refresh the display
    //    ShowLeaderboard();
    //}

}
