using SBPScripts;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;
using UnityEngine.UI;
using UnityEngine.Video;
using static extOSC.Components.OSCReceiverSkinnedMesh;

public class DaanManager : MonoBehaviour
{
    public List<BicycleController> players = new List<BicycleController>();

    public List<TMP_Text> playerNames;
    public List<TMP_Text> winnerNames;

    public List<TMP_Text> playerPositions;
    public List<TMP_Text> playerDistances;
    public List<TMP_Text> playerPedalCount;
    public List<TMP_Text> playerSpeedText;
    public List<TMP_Text> playerRelaxMessage;
    public TMP_Text[] timerText;
    public List<GameObject> boyCharacterPrefabs;
    public List<GameObject> girlCharacterPrefabs;

    public List<Transform> playerSpawnPositions;

    public List<SplineContainer> splinePaths;
    public List<Camera> cameras;

    public List<Transform> winnerPositions; // Positions for the top 3 winners
    public List<RuntimeAnimatorController> animatorControllers; // List containing animator controllers for 1st, 2nd, and 3rd place
    public List<GameObject> characterPrefabs; // List of all character prefabs (e.g., boyPrefab, girlPrefab, etc.)

    private bool raceStarted = false;
    //Variables for Calculating Speed, Distance & PedalCounts
    public int playerCount = 2;
    public int winnerCount = 1;

    private float[] playerSpeeds; // Store the speed for each player

    private float[] lastDataReceivedTime; // Store the last time data was received for each player
    private int[] totalPedalCounts; // Store total pedal counts for each player

    public const float wheelCircumference = 2.1f;  // Adjust based on the actual bike's wheel
    private const float updateInterval = 1.0f;  // The time interval between receiving cycle data

    // filling bars
    public Image[] progressFilling;
    // UI Canvas Reference
    public GameObject idleScreen;
    public GameObject countdownScreen;
    public GameObject mainScreen;
    public GameObject winnerScreen;
    public GameObject leaderboardScreen;
    public bool isTimerBase = false;
    private float remainingTime;
    public int gameDuration = 60;

    //Leaderboard Variables
    public List<string> playerGenders = new List<string>(); // Add this to store genders
    public Leaderboard_V1 leaderboardManager;

    //Videos Configuraiton
    public VideoPlayer idlePlayer;             // Video player for idle and splash videos (1920x1080)
    public VideoPlayer countdownPlayer;        // Video player for countdown, finish, and congratulation videos (1920x250)
    public VideoPlayer congratulationPlayer;        // Video player for countdown, finish, and congratulation videos (1920x250)

    public VideoClip idleClip;                 // Idle video
    //public VideoClip splashClip;               // Splash video
    public VideoClip countdownClip;            // Countdown video
    public VideoClip finishClip;               // Finish video
    public VideoClip congratulationClip;       // Congratulation video

    private bool sequenceStarted = false;

    public RectTransform countdownRect;        // RectTransform for the countdown video object (UI element)
    public Vector2 offscreenPosition = new Vector2(-1920, 0);  // Starting position offscreen to the left
    public Vector2 onscreenPosition = new Vector2(0, 0);       // Target position on screen

    //Emotions Messages 
    [SerializeField] private TMP_Text[] messageText;
    [SerializeField] private Transform[] emojiSpawnPoints; // Where the emoji will appear (inside UI or near player)
    // Define the messages and corresponding emoji prefabs
    private Dictionary<string, GameObject> emotionMessages = new Dictionary<string, GameObject>();
    public GameObject keepGoingEmojiPrefab;
    public GameObject tiredEmojiPrefab;
    public GameObject relaxedEmojiPrefab;
    public GameObject happyEmojiPrefab;
    public GameObject grinningEmojiPrefab;

    public GameObject[] nameBlock;
    public GameObject[] messageBlock;


    private const int PEDAL_HISTORY_LENGTH = 3; // Store the last 10 pedal states
    private Queue<bool>[] pedalHistory; // Array of queues to store pedal history for each player
    private bool[] isCurrentlyPedaling; // Array to store the current pedaling state of each player

    void OnEnable()
    {
        // Subscribe to OSCManager events
        OSCManager.OnPlayerDataReceived += HandlePlayerData;
        OSCManager.OnGameTargetReceived += HandleGameTarget;
        OSCManager.OnGameStartReceived += HandleGameStart;
        OSCManager.OnGameRestartReceived += HandleGameRestart;
        OSCManager.OnCycleDataReceived += HandleCycleData;
    }
    void OnDisable()
    {
        // Unsubscribe from OSCManager events
        OSCManager.OnPlayerDataReceived -= HandlePlayerData;
        OSCManager.OnGameTargetReceived -= HandleGameTarget;
        OSCManager.OnGameStartReceived -= HandleGameStart;
        OSCManager.OnGameRestartReceived -= HandleGameRestart;
        OSCManager.OnCycleDataReceived -= HandleCycleData;
    }

    void Start()
    {
        // Initialize the total pedal count array for each player
        totalPedalCounts = new int[playerCount];
        lastDataReceivedTime = new float[playerCount];
        lastCycleValues = new int[playerCount];

        playerSpeeds = new float[playerCount]; // Initialize the array to store player speeds

        //leaderboardManager = GetComponent<Leaderboard_V1>();
        //leaderboardManager.ShowLeaderboard();

        //Setting all screen disable except Idle Screen
        idleScreen.SetActive(true);
        countdownScreen.SetActive(false);
        mainScreen.SetActive(false);
        winnerScreen.SetActive(false);
        leaderboardScreen.SetActive(false);

        // Register event handlers for the video players
        idlePlayer.loopPointReached += OnVideoEnd;
        countdownPlayer.loopPointReached += OnVideoEnd;
        congratulationPlayer.loopPointReached += OnVideoEnd;

        // Start playing the idle video by default
        PlayIdle();
        gameTimer = 0f; // Reset the timer when the race starts

        // Initialize the message and emoji mappings
        emotionMessages.Add("Keep Going", keepGoingEmojiPrefab);
        emotionMessages.Add("Pedal! Pedal! Pedal!", tiredEmojiPrefab);
        emotionMessages.Add("Almost There", grinningEmojiPrefab);
        emotionMessages.Add("Kudos", happyEmojiPrefab);
        emotionMessages.Add("Halfway Through", relaxedEmojiPrefab);

        pedalHistory = new Queue<bool>[playerCount];
        isCurrentlyPedaling = new bool[playerCount];
        for (int i = 0; i < playerCount; i++)
        {
            pedalHistory[i] = new Queue<bool>();
            isCurrentlyPedaling[i] = false;
        }
        InitializePlayerStats(playerCount);
        if (isTimerBase)
        {
            remainingTime = gameDuration; // Initialize the timer
        }
    }
    void InitializePlayerStats(int playerCount)
    {
        playerStats = new List<PlayerStats>();
        playersPaused = new bool[playerCount];
        lastValueWasOne = new bool[playerCount];

        for (int i = 0; i < playerCount; i++)
        {
            playerStats.Add(new PlayerStats
            {
                totalRotations = 0,
                rotationsInCurrentSecond = 0,
                distanceCovered = 0,
                carbonFootprintReduction = 0,
                energyGeneration = 0,
                caloriesBurned = 0
            });
        }
    }
    // Event handler for when a video ends (loop point reached)
    void OnVideoEnd(VideoPlayer vp)
    {
        if (vp == countdownPlayer && countdownPlayer.clip == countdownClip)
        {
            countdownScreen.SetActive(false);
            mainScreen.SetActive(true);
            StartRace();
            OSCManager.Instance.SendInputMessage("s");
        }
        if (vp == congratulationClip) { 
        
        }
    }

    void PlayIdle()
    {
        idlePlayer.isLooping = true;
        idlePlayer.clip = idleClip;
        idlePlayer.Play();
    }
    //void PlaySplash()
    //{
    //    idlePlayer.isLooping = false;  // Stop looping for splash
    //    idlePlayer.clip = splashClip;
    //    idlePlayer.Play();
    //    Invoke("AnimateCountdownIn", 3f);
    //}
    void PlayFinish()
    {
        countdownScreen.SetActive(true);
        countdownPlayer.clip = finishClip;
        countdownPlayer.Play();
        Invoke("EndRace", 3f);
    }
    void PlayCongratulation()
    {
        congratulationPlayer.clip = congratulationClip;
        congratulationPlayer.Play();
        
        congratulationPlayer.isLooping=true;
    }
    void AnimateCountdownIn()
    {
        idleScreen.SetActive(false);
        countdownScreen.SetActive(true);
        countdownRect.anchoredPosition = offscreenPosition;  // Start off-screen
        StartCoroutine(MoveCountdownToPosition(onscreenPosition, .5f));  // Animate to the center

    }

    IEnumerator MoveCountdownToPosition(Vector2 targetPosition, float duration)
    {
        Vector2 startPosition = countdownRect.anchoredPosition;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            countdownRect.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, elapsedTime / duration);
            yield return null;
        }
        countdownRect.anchoredPosition = targetPosition;  // Ensure exact position at the end
        // After reaching target position, play the countdown video
        PlayCountdown();
    }

    void PlayCountdown()
    {
        idlePlayer.Stop();
        countdownPlayer.clip = countdownClip;
        countdownPlayer.Play();
    }
    // Define a target pedal count for completion
    int targetPedalCount;
    void StartRace()
    {
        raceStarted = true;
        StartCoroutine(CalculateSpeedRoutine());

        foreach (var player in players)
        {
            player.StartMovement();
        }
    }

    //void Update()
    //{
    //    if (raceStarted && !isTimerBase)
    //    {
    //        // Increment the timer
    //        gameTimer += Time.deltaTime;
    //        foreach (var timer in timerText)
    //        {
    //            timer.text = gameTimer.ToString("F0");
    //        }
    //    }

    //    if (isTimerBase && raceStarted)
    //    {
    //        remainingTime -= Time.deltaTime;
    //        foreach (var timer in timerText)
    //        {
    //            timer.text = remainingTime.ToString("F0");
    //        }
    //        if (remainingTime <= 0)
    //        {
    //            HandleGameOverTimerBased();
    //        }
    //    }

    //}

    void Update()
    {
        if (raceStarted && !isTimerBase)
        {
            // Increment the timer
            gameTimer += Time.deltaTime;

            foreach (var timer in timerText)
            {
                timer.text = gameTimer.ToString("F0");
            }

            // Update player speeds
            UpdatePlayerSpeeds(gameTimer);
        }

        if (isTimerBase && raceStarted)
        {
            remainingTime -= Time.deltaTime;

            foreach (var timer in timerText)
            {
                timer.text = remainingTime.ToString("F0");
            }

            if (remainingTime <= 0)
            {
                //isTimerBase = true;
                HandleGameOverTimerBased();
                
            }

            // Update player speeds based on the remaining time
            UpdatePlayerSpeeds(gameDuration - remainingTime); // Assuming targetTime is the total time for the race
        }
    }

    //private void HandleGameTarget(int target)
    //{
    //    Debug.LogError($"GameController received game target: {target}");
    //    targetPedalCount = target;

    //    for (int i = 0; i < playerPedalCount.Count; i++)
    //    {
    //        playerPedalCount[i].text = $"00/ {targetPedalCount} <size=30%> Pedals </size >";
    //    }
    //}
    private void UpdatePlayerSpeeds(float elapsedTime)
    {
        for (int i = 0; i < playerPedalCount.Count; i++)
        {
            // Calculate speed (Speed = distance / time)
            float distance = totalPedalCounts[i]; // 1 pedal = 1 meter
            float speed = elapsedTime > 0 ? distance / elapsedTime : 0;

            // Update speed text for each player
            playerSpeedText[i].text = $"{speed:F2} "; // Speed formatted to 2 decimal places

            UpdateSpeedometer(i, speed);

        }
    }


    //spedomerter logic 
    public List<RectTransform> playerArrows; // List of arrow RectTransforms
    public List<float> playerMaxSpeeds; // Max speeds for each player (in km/h)


    public float minSpeedArrowAngle = 0.0f; // Min arrow angle (rotate counter-clockwise)
    public float maxSpeedArrowAngle = -170.0f; // Max arrow angle (rotate clockwise)
    private void UpdateSpeedometer(int playerIndex, float speed)
    {
        // Assuming playerMaxSpeeds contains the max speeds in km/h for each player
        if (playerArrows.Count > playerIndex && playerMaxSpeeds.Count > playerIndex)
        {
            // Convert speed to km/h (if speed is in m/s)
            //speed *= 3.6f;

            // Normalize speed to fit into the range of the speedometer
            float normalizedSpeed = Mathf.Clamp(speed, 0, playerMaxSpeeds[playerIndex]);

            // Update the arrow's rotation based on the speed
            float normalizedAngle = Mathf.Lerp(minSpeedArrowAngle, maxSpeedArrowAngle, normalizedSpeed / playerMaxSpeeds[playerIndex]);
            playerArrows[playerIndex].localEulerAngles = new Vector3(0, 0, normalizedAngle);
        }
    }
    private float gameTimer = 0f;
    // Handle Event Functions
    private void HandlePlayerData(OSCManager.PlayerDataList playerDataList)
    {
        Debug.LogWarning($"GameController received player data: {JsonUtility.ToJson(playerDataList)}");
        //PlaySplash();
        Invoke("AnimateCountdownIn", .1f);

        // Ensure we have the same number of players in the data as in the scene
        if (playerDataList.players.Length != players.Count)
        {
            Debug.LogWarning("Mismatch between the number of players in data and the number of players in the scene.");
            return;
        }

        if (boyCharacterPrefabs == null || girlCharacterPrefabs == null || boyCharacterPrefabs.Count == 0 || girlCharacterPrefabs.Count == 0)
        {
            Debug.LogError("Character prefabs are not initialized or empty.");
            return;
        }
        players.Clear(); // Clear the list to avoid duplicates
        playerGenders.Clear(); // Clear previous genders
        
        // Create temporary lists to track available models
        List<GameObject> availableBoyModels = new List<GameObject>(boyCharacterPrefabs);
        List<GameObject> availableGirlModels = new List<GameObject>(girlCharacterPrefabs);

        for (int i = 0; i < playerDataList.players.Length; i++)
        {
            var playerData = playerDataList.players[i];
            playerNames[i].text = playerData.name; // Set player name UI
            playerGenders.Add(playerData.gender); // Store the gender

            // Determine which prefab to instantiate based on gender and available prefabs
            GameObject characterPrefab = null;
            if (playerData.gender.ToLower() == "male" && availableBoyModels.Count > 0)
            {
                // Select a random boy model from the list
                int randomIndex = Random.Range(0, availableBoyModels.Count);
                characterPrefab = availableBoyModels[randomIndex];
                availableBoyModels.RemoveAt(randomIndex); // Remove the selected model to ensure uniqueness
                Debug.LogWarning($"Spawning {characterPrefab.name} for player {playerData.name} (Male)");
            }

            else if (playerData.gender.ToLower() == "female" && availableGirlModels.Count > 0)
            {
                // Select a random girl model from the list
                int randomIndex = Random.Range(0, availableGirlModels.Count);
                characterPrefab = availableGirlModels[randomIndex];
                availableGirlModels.RemoveAt(randomIndex); // Remove the selected model to ensure uniqueness

                Debug.LogWarning($"Spawning {characterPrefab.name} for player {playerData.name} (Female)");
            }
            else
            {
                Debug.LogWarning($"Unknown or insufficient gender '{playerData.gender}' models for player '{playerData.name}'");
                continue;
            }
            // Instantiate the character model at the spawn position
            if (characterPrefab != null)
            {
                Transform spawnPosition = playerSpawnPositions[i];
                GameObject character = Instantiate(characterPrefab, spawnPosition.position, spawnPosition.rotation);
                character.transform.SetParent(playerSpawnPositions[i].transform);

                Debug.Log($"Player '{playerData.name}' spawned at position {i} ({spawnPosition.position}) with character '{characterPrefab.name}'");

                // Create a Quaternion that represents the rotation
                Quaternion newRotation = Quaternion.Euler(0, 90, 0);
                character.transform.localRotation = newRotation;
                // Fetch the BicycleController from the spawned character and add it to the list
                BicycleController bicycleController = character.GetComponent<BicycleController>();
                if (bicycleController != null)
                {
                    players.Add(bicycleController);
                }
                else
                {
                    Debug.LogError($"BicycleController missing on character prefab: {characterPrefab.name}");
                }

                // Set the corresponding camera's target to the spawned character's transform
                if (i < cameras.Count)
                {
                    cameras[i].GetComponent<BicycleCamera>().target = character.transform;
                }
                // Assign the corresponding spline path to the player's splineContainer
                if (i < splinePaths.Count)
                {
                    players[i].splineContainer = splinePaths[i];
                }
            }
        }
        // Start coroutines for each player when the game starts
        for (int i = 0; i < playerCount; i++)
        {
            Coroutine playerCoroutine = StartCoroutine(EvaluatePlayerPerformanceCoroutine(i));
            playerCoroutines.Add(playerCoroutine); // Keep track of the coroutines
        }
    }
    private bool isDisplayingMessage = false; // Ensure messages don't overlap
    private List<Coroutine> playerCoroutines = new List<Coroutine>(); // Store the coroutines for players

    private void HandleGameTarget(int target)
    {
        Debug.LogError($"GameController received game target: {target}");
        targetPedalCount = target;
        for (int i = 0; i < playerPedalCount.Count; i++)
        {
            playerPedalCount[i].text = $" {targetPedalCount} ";
        }
    }
    int winnerId = -1; // Track the player with the highest count
    public float speedMultiplier = 2f;
    private void HandleGameStart(bool start)
    {
        Debug.LogWarning($"GameController received game start: {start}");
    }
    private void HandleGameRestart(bool restart)
    {
        Debug.LogWarning($"GameController received game restart: {restart}");
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }
    public bool isMagnetic = false;
    private int[] lastCycleValues; // Last received cycle data (to track transitions)

    /* private void HandleCycleData(string cycleData)
     {
         Debug.Log($"GameController received cycle data: {cycleData}");
         DebugConsole.Instance.LogWarning($"{cycleData}");
         
        if (!raceStarted) return;
        
        // Split the received string by the ':' delimiter
         string[] playerDataArray = cycleData.Split(':');

         // Determine how many players' data to process
         int dataToProcess = Mathf.Min(playerCount, playerDataArray.Length);
         bool playerCompleted = false;
         // Process data for the specified number of players
         for (int i = 0; i < dataToProcess; i++)
         {
             // Update pedal count based on whether isMagnetic is true or false
             UpdatePedalCount(i, playerDataArray[i]);

             // Update the time when data was last received for this player
             lastDataReceivedTime[i] = Time.time;

             // Handle player movement and progress
             //HandlePlayerMovementAndProgress(i);

             // Check for a winner and handle race completion
             HandleRaceCompletion(ref playerCompleted);
         }
     }

     private void UpdatePedalCount(int playerIndex, string playerData)
     {
         if (isMagnetic)
         {
             // Magnetic logic: Parse the pedal count
             int pedalCount = int.Parse(playerData);
             totalPedalCounts[playerIndex] += pedalCount;

         }
         else
         {
             Debug.Log("CALLING ROTARY FUNCTION");

             // Non-magnetic logic: Parse the cycle value (assuming it's sent as a string representing 0 or 1)
             int currentCycleValue = int.Parse(playerData);

             // Check for a valid pedal count transition (1 to 0)
             if (lastCycleValues[playerIndex] == 1 && currentCycleValue == 0)
             {
                 totalPedalCounts[playerIndex]++;
                 Debug.LogWarning($"Player {playerIndex + 1} pedal count incremented: {totalPedalCounts[playerIndex]}");
             }

             // Update the last cycle value for the player
             lastCycleValues[playerIndex] = currentCycleValue;
         }
     }
     // Coroutine to calculate speed every second
     private IEnumerator CalculateSpeedRoutine()
     {
         while (true)
         {
             // Calculate speed for all players every second
             for (int i = 0; i < playerCount; i++)
             {
                 // If data was not received in the last second, set speed to 0
                 if (Time.time - lastDataReceivedTime[i] > 1f)
                 {
                     // No data received in the last second, set speed to 0
                     playerSpeeds[i] = 0;
                     players[i].StopBicycleMovement();
                     Debug.LogWarning($"Player {i + 1} has stopped due to no data. Speed set to 0.");
                 }
                 else
                 {
                     // If data was received, calculate the speed
                     HandlePlayerMovementAndProgress(i);
                 }
             }

             // Wait for 1 second before recalculating
             yield return new WaitForSeconds(1f);
         }
     }
 */

    //enew cide
    /*private void HandleCycleData(string cycleData)
    {
        if (!raceStarted) return;

        string[] playerDataArray = cycleData.Split(':'); // split cycle data 1:0

        int dataToProcess = Mathf.Min(playerCount, playerDataArray.Length); // [1,0]

        bool playerCompleted = false;

        for (int i = 0; i < dataToProcess; i++)
        {
            UpdatePedalCount(i, playerDataArray[i]);
            lastDataReceivedTime[i] = Time.time;

            // Try to parse the rotation value from the received data
            if (int.TryParse(playerDataArray[i], out int rotationValue))
            {
                // Process data for the player using the rotationValue (1 = rotation, 0 = no rotation)
                ProcessPlayerData(rotationValue, i);  // Pass the player index along with the rotation value
            }
        }
        HandleRaceCompletion(ref playerCompleted);
    }*/
    private void HandleCycleData(string cycleData)
    {
        if (!raceStarted) return;

        string[] playerDataArray = cycleData.Split(':'); // split cycle data 1:0
        int dataToProcess = Mathf.Min(playerCount, playerDataArray.Length); // [1,0]

        for (int i = 0; i < dataToProcess; i++)
        {
            UpdatePedalCount(i, playerDataArray[i]);
            lastDataReceivedTime[i] = Time.time;

            if (int.TryParse(playerDataArray[i], out int rotationValue))
            {
                ProcessPlayerData(rotationValue, i); // Pass the player index along with the rotation value
            }
        }

        // Only handle race completion if not timer-based
        if (!isTimerBase)
        {
            bool playerCompleted = false;
            HandleRaceCompletion(ref playerCompleted);
        }
    }
    private void HandleGameOverTimerBased()
    {
        raceStarted = false;
        Debug.LogWarning("Time is up! The game is over.");

        // Determine the winner based on the highest pedal count
        int highestPedalCount = -1;
        //int winnerId = -1;

        for (int i = 0; i < playerCount; i++)
        {
            if (totalPedalCounts[i] > highestPedalCount)
            {
                highestPedalCount = totalPedalCounts[i];
                winnerId = i;
               
            }
        }

        if (winnerId != -1)
        {
            DebugConsole.Instance.LogWarning($"Player {winnerId + 1} is the winner with {highestPedalCount} pedal counts!");
        }
        else
        {
            DebugConsole.Instance.LogWarning("No winner! Time ran out before anyone reached the target.");
        }

        // Stop all players
        foreach (var player in players)
        {
            player.StopBicycleMovement();
            player.hasFinished = true;
        }

        SendingGameOver();
    }

    private IEnumerator CalculateSpeedRoutine()
    {
        while (true)
        {
            for (int i = 0; i < playerCount; i++)
            {
                if (Time.time - lastDataReceivedTime[i] > 1f)
                {
                    playerSpeeds[i] = 0;
                    players[i].StopBicycleMovement();
                }
            }
            yield return new WaitForSeconds(1f);
        }
    }


    private void UpdatePedalCount(int playerIndex, string playerData)
    {
        bool currentPedalState = false;

        if (isMagnetic)
        {
            int pedalCount = int.Parse(playerData);
            totalPedalCounts[playerIndex] += pedalCount;
            currentPedalState = pedalCount > 0;
        }
        else
        {
            int currentCycleValue = int.Parse(playerData);
            if (lastCycleValues[playerIndex] == 1 && currentCycleValue == 0)
            {
                totalPedalCounts[playerIndex]++;
                currentPedalState = true;
            }
            lastCycleValues[playerIndex] = currentCycleValue;
        }

        // Update pedal history
        //if (pedalHistory[playerIndex].Count >= PEDAL_HISTORY_LENGTH)
        //{
        //    pedalHistory[playerIndex].Dequeue(); // 1 1 0 
        //}
        //pedalHistory[playerIndex].Enqueue(currentPedalState);


        // Determine if player is considered to be pedaling based on recent history

        int pedalingCount = pedalHistory[playerIndex].Count(p => p);

        isCurrentlyPedaling[playerIndex] = pedalingCount > PEDAL_HISTORY_LENGTH / 2;

        Debug.Log($"Player{playerIndex} :" + isCurrentlyPedaling[playerIndex]);
    }
    //private void HandlePlayerMovementAndProgress(int playerIndex)
    //{

    //    // Calculate speed and update the player's movement
    //    float speed = (totalPedalCounts[playerIndex] * wheelCircumference) / 10; //
    //    float newSpeed = speed * speedMultiplier;
    //    // Update player speed UI
    //    playerSpeeds[playerIndex] = newSpeed;
    //    playerPedalCount[playerIndex].text = $"{totalPedalCounts[playerIndex].ToString()}/{targetPedalCount}<size=30%>PEDALS</size>";
    //    progressFilling[playerIndex].fillAmount = Mathf.Clamp01((float)totalPedalCounts[playerIndex] / targetPedalCount);

    //    // Stop or start the bicycle based on the calculated speed
    //    if (speed <= 1)
    //    {
    //        players[playerIndex].StopBicycleMovement();
    //        Debug.Log($"Player {playerIndex + 1} has stopped. Speed: {newSpeed} m/s");
    //    }
    //    else
    //    {
    //        players[playerIndex].StartBicycleMovement();
    //        players[playerIndex].SetSpeed(newSpeed);
    //        players[playerIndex].UpdateSplinePosition(newSpeed * Time.deltaTime / players[playerIndex].splineContainer.Spline.GetLength());
    //        Debug.Log($"Player {playerIndex + 1} is moving. Speed: {newSpeed} m/s");
    //    }
    //}
    private bool[] playersPaused;
    private bool[] lastValueWasOne;

    private List<PlayerStats> playerStats;
    // Player stats class
    public class PlayerStats
    {
        public int totalRotations;
        public int rotationsInCurrentSecond;
        public float distanceCovered;
        public float currentSpeed;
        public float carbonFootprintReduction;
        public float energyGeneration;
        public float caloriesBurned;
    }
    // Time taken to decelerate to a stop (in seconds)
    public float decelerationTime = 2.0f;

    // Function to process the player data and calculate speed
    void ProcessPlayerData(int rotationValue, int playerIndex)
    {
        // Process if player is pedaling and not paused
        if (rotationValue == 1 && !playersPaused[playerIndex])
        {
            playerStats[playerIndex].totalRotations++;
            playerStats[playerIndex].rotationsInCurrentSecond++;

            // Calculate distance (wheelCircumference per rotation)
            float distanceThisSecond = wheelCircumference;
            playerStats[playerIndex].distanceCovered += distanceThisSecond;

            // Calculate speed for this second (based on rotations received this second)
            float speedThisSecond = playerStats[playerIndex].rotationsInCurrentSecond * wheelCircumference; // meters per second
            playerStats[playerIndex].currentSpeed = speedThisSecond;  // Store the speed in stats

            // Reset deceleration timer if player is pedaling
            players[playerIndex].decelerationTimer = 0f;

            // Update the player movement with the new speed
            //playerPedalCount[playerIndex].text = $"{playerStats[playerIndex].totalRotations.ToString()}/{targetPedalCount}<size=30%>Pedals</size>";
            playerPedalCount[playerIndex].text = $"{playerStats[playerIndex].totalRotations.ToString()}";
            progressFilling[playerIndex].fillAmount = Mathf.Clamp01((float)playerStats[playerIndex].totalRotations / targetPedalCount);
            players[playerIndex].StartBicycleMovement();
            players[playerIndex].SetSpeed(playerStats[playerIndex].currentSpeed);
            players[playerIndex].UpdateSplinePosition(playerStats[playerIndex].currentSpeed * speedMultiplier * Time.deltaTime / players[playerIndex].splineContainer.Spline.GetLength());

            // Log player data including speed
            Debug.Log($"Player {playerIndex + 1}: Rotations: {playerStats[playerIndex].totalRotations}, " +
                      $"Distance: {playerStats[playerIndex].distanceCovered:F2} meters, " +
                      $"Speed: {playerStats[playerIndex].currentSpeed:F2} m/s, " +
                      $"CO2 Saved: {playerStats[playerIndex].carbonFootprintReduction:F4} kg, " +
                      $"Energy: {playerStats[playerIndex].energyGeneration:F2} Wh, " +
                      $"Calories: {playerStats[playerIndex].caloriesBurned} kcal");
        }
        else
        {
            // If no rotation or player is paused, gradually reduce speed
            if (playerStats[playerIndex].currentSpeed > 0)
            {
                // Increment the deceleration timer
                players[playerIndex].decelerationTimer += Time.deltaTime;

                // Use Lerp to smoothly reduce the speed over time
                float speedReductionFactor = Mathf.Lerp(playerStats[playerIndex].currentSpeed, 0f, players[playerIndex].decelerationTimer / decelerationTime);
                playerStats[playerIndex].currentSpeed = Mathf.Max(speedReductionFactor, 0f); // Prevent negative speed

                // Update the player movement with the reduced speed
                players[playerIndex].SetSpeed(playerStats[playerIndex].currentSpeed);

                // If speed reaches zero, stop the bicycle movement
                if (playerStats[playerIndex].currentSpeed <= 0f)
                {
                    players[playerIndex].StopBicycleMovement();
                    Debug.Log($"Player {playerIndex + 1}: Bicycle has stopped.");
                }
            }

            // Log deceleration process
            Debug.Log($"Player {playerIndex + 1}: No rotation or paused. Speed reducing to {playerStats[playerIndex].currentSpeed:F2} m/s.");
        }

        // Reset the number of rotations in the current second after each processing (for the next second)
        playerStats[playerIndex].rotationsInCurrentSecond = 0;
    }

    public float interpolateSpeed = 2;
    private void HandlePlayerMovementAndProgress(int playerIndex)
    {
        float targetSpeed = (totalPedalCounts[playerIndex] * wheelCircumference) / 10 * speedMultiplier;

        // Smoothly interpolate towards the target speed
        playerSpeeds[playerIndex] = Mathf.Lerp(playerSpeeds[playerIndex], targetSpeed, Time.deltaTime * interpolateSpeed);

        // Update player speed UI and other UI elements
        playerPedalCount[playerIndex].text = $"{totalPedalCounts[playerIndex].ToString()}/{targetPedalCount}<size=30%>Pedals</size>";
        progressFilling[playerIndex].fillAmount = Mathf.Clamp01((float)totalPedalCounts[playerIndex] / targetPedalCount);

        if (playerSpeeds[playerIndex] > 0.1f) // Small threshold to prevent jitter
        {
            players[playerIndex].StartBicycleMovement();
            players[playerIndex].SetSpeed(playerSpeeds[playerIndex]);
            players[playerIndex].UpdateSplinePosition(playerSpeeds[playerIndex] * Time.deltaTime / players[playerIndex].splineContainer.Spline.GetLength());
        }
        else
        {
            players[playerIndex].StopBicycleMovement();
        }
    }
    private void HandleRaceCompletion(ref bool playerCompleted)
    {
        if (isTimerBase) return; // Skip race completion handling for timer-based games

        // Determine if any player has reached the target pedal count
        int highestPedalCount = -1;

        for (int i = 0; i < playerCount; i++)
        {
            if (totalPedalCounts[i] >= targetPedalCount && totalPedalCounts[i] > highestPedalCount)
            {
                highestPedalCount = totalPedalCounts[i];
                winnerId = i; // Set the player with the highest pedal count as the winner
            }
        }

        // If a winner has been found and the race is still ongoing, complete the race
        if (winnerId != -1 && !playerCompleted)
        {
            Debug.LogWarning($"Player {winnerId + 1} is the winner with {highestPedalCount} pedal counts!");
            playerCompleted = true;

            SendingGameOver();
        }
    }
    public void SendingGameOver()
    {
        DebugConsole.Instance.LogWarning(winnerId.ToString());
        OSCManager.Instance.SendInputMessage("f");
        mainScreen.SetActive(false);

        foreach (var player in players)
        {
            Debug.LogError("Game is Finished. Stopping all bicycles.");
            player.StopBicycleMovement();
            player.hasFinished = true;
        }

        PlayFinish();
        raceStarted = false;

        // Send data for both players to the server
        SendBothPlayersDataToServer(
            playerNames[0].text, playerGenders[0], totalPedalCounts[0] * wheelCircumference, totalPedalCounts[0], winnerId == 0,
            playerNames[1].text, playerGenders[1], totalPedalCounts[1] * wheelCircumference, totalPedalCounts[1], winnerId == 1,
            playerNames[2].text, playerGenders[2], totalPedalCounts[2] * wheelCircumference, totalPedalCounts[2], winnerId == 2

        );
        UpdateLeaderboard(playerNames[winnerId].text, totalPedalCounts[winnerId]);
    }
    void EndRace()
    {
        countdownScreen.SetActive(false);
        winnerScreen.SetActive(true);
        PlayCongratulation();
        if (winnerNames[0] != null)
        {
            winnerNames[0].text = $"{playerNames[winnerId].text}"; // <br><size=50%>{gameTimer.ToString("F0")} Seconds</size>
            DebugConsole.Instance.Log(playerNames[winnerId].text);
        }
        else
        {
            DebugConsole.Instance.LogError("Winner Text is Not Assigned");
        }
        // Get the BicycleController of the winning player
        BicycleController winningPlayerController = players[winnerId];

        // Get the GameObject of the winning player
        GameObject winningPlayerObject = winningPlayerController.gameObject;

        // Get the name of the winning player's GameObject
        string winningPlayerName = winningPlayerObject.name.Replace("(Clone)", "").Trim(); ;

        // Find the corresponding prefab in the characterPrefabs list
        GameObject matchedPrefab = characterPrefabs.Find(prefab => prefab.name == winningPlayerName);
        Debug.Log("Attempting to instantiate prefab: " + matchedPrefab.name);

        if (matchedPrefab != null)
            {
            Debug.LogError($"Matched prefab '{matchedPrefab.name}' for player {winningPlayerName} (Rank 1).");

            if (winnerPositions.Count > 0)
            {
                // Instantiate the matched character at the winner position
                GameObject winnerCharacter = Instantiate(matchedPrefab, winnerPositions[0].position, winnerPositions[0].rotation);
                Debug.LogError($"Instantiated '{winnerCharacter.name}' for player {winningPlayerName} at winner position 1.");

                // Ensure the instantiated object is positioned correctly in the world space
                winnerCharacter.transform.SetParent(winnerPositions[0].transform);

                // Set the z position of the winnerCharacter to 1
                //Vector3 position = winnerCharacter.transform.position;
                //winnerCharacter.transform.position = position;

                // Set the Y-axis rotation of the winnerCharacter to -180 degrees
                Quaternion rotation = winnerCharacter.transform.rotation;
                rotation = Quaternion.Euler(rotation.eulerAngles.x, -90f, rotation.eulerAngles.z);
                winnerCharacter.transform.rotation = rotation;

                // Get the Animator component from the instantiated character GameObject
                Animator animator = winnerCharacter.GetComponent<Animator>();

                if (animator != null && animatorControllers.Count > 0)
                {
                    // Assign the appropriate animator controller based on the rank
                    animator.runtimeAnimatorController = animatorControllers[0];
                    Debug.Log($"Assigned AnimatorController to '{winnerCharacter.name}' object of player {winningPlayerName}.");
                }
                else
                {
                    Debug.LogWarning("Animator not found or AnimatorController not set.");
                }
            }

            else
            {
                    Debug.LogWarning("No winner position set. Cannot instantiate character.");
                }
            }
            else
            {
                Debug.LogWarning($"No matching prefab found for '{winningPlayerName}' in the characterPrefabs list.");
            }

        Debug.Log("Race Over!");

        foreach (var spawnposition in playerSpawnPositions)
        {
            spawnposition.gameObject.SetActive(false);
        }

        //StartCoroutine(WaitAndReloadScene());
        StartCoroutine(DelayShowLeaderboard());
    }
    public float leaderboarDuration = 3f;
    public float reloadDuraion = 3f;
    IEnumerator DelayShowLeaderboard()
    {
        yield return new WaitForSeconds(leaderboarDuration);

        winnerScreen.SetActive(false);
        leaderboardScreen.SetActive(true);

        StartCoroutine(WaitAndReloadScene());
    }
    
    //public float leaderboarDuration = 3f;
    //public float reloadDuraion = 3f;
    IEnumerator WaitAndReloadScene()
    {
        yield return new WaitForSeconds(reloadDuraion);
        OSCManager.Instance.SendRestartMessage();
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }


    public void UpdateLeaderboard(string teamName, float distanceTraveled)
    {
        Debug.LogError($"Update Leaderboard: TeamName = {teamName}; DistanceTraveled = {distanceTraveled}");

        LeaderboardEntry newEntry = new LeaderboardEntry(teamName, gameDuration.ToString(), distanceTraveled.ToString());

        // Load existing leaderboard data
        string jsonString = PlayerPrefs.GetString("Leaderboard", "{}");
        Debug.LogError($"Loaded JSON from PlayerPrefs: {jsonString}");

        Leaderboard leaderboard;
        try
        {
            leaderboard = JsonUtility.FromJson<Leaderboard>(jsonString);
            if (leaderboard == null)
            {
                throw new System.Exception("Leaderboard is null after deserialization");
            }
            Debug.LogError("Deserialization successful");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to deserialize leaderboard. Initializing new leaderboard. Error: " + e.Message);
            leaderboard = new Leaderboard();
            Debug.LogError("New leaderboard initialized");
        }

        // Add the new entry
        leaderboard.entries.Add(newEntry);
        Debug.LogError($"Added new entry to leaderboard. Total entries: {leaderboard.entries.Count}");

        // Sort leaderboard by distance (largest first)
        leaderboard.entries.Sort((x, y) => y.distanceTraveled.CompareTo(x.distanceTraveled));
        Debug.LogError("Leaderboard sorted");

        // Save updated leaderboard data
        string updatedJsonString = JsonUtility.ToJson(leaderboard);
        Debug.LogError($"Updated JSON: {updatedJsonString}");
        PlayerPrefs.SetString("Leaderboard", updatedJsonString);
        PlayerPrefs.Save();
        Debug.LogError("Leaderboard saved to PlayerPrefs");

        // Show the updated leaderboard
        leaderboardManager.ShowLeaderboard();
    }


    void SendBothPlayersDataToServer(
    string player1Name, string player1Gender, float player1Distance, int player1PedalCount, bool player1IsWinner,
    string player2Name, string player2Gender, float player2Distance, int player2PedalCount, bool player2IsWinner,
    string player3Name, string player3Gender, float player3Distance, int player3PedalCount, bool player3IsWinner)
    {
        // Send Player 1 data
        SendPlayerDataToServer(player1Name, player1Gender, player1PedalCount, gameTimer, player1IsWinner);

        // Send Player 2 data
        SendPlayerDataToServer(player2Name, player2Gender, player2PedalCount, gameTimer, player2IsWinner);

        // Send Player 3 data
        SendPlayerDataToServer(player3Name, player3Gender, player3PedalCount, gameTimer, player3IsWinner);

    }
    

    void SendPlayerDataToServer(string playerName, string playerGender, int pedalCount, float distanceTraveled, bool isWinner)
    {
        //Debug.LogWarning($"{playerName}, {playerGender}, {distanceTraveled}, {isWinner}");
        OSCManager.Instance.SendOSCMessage("/userGameData", playerName, playerGender, pedalCount,distanceTraveled, isWinner);
    }
    // Coroutine to evaluate player performance at random intervals
    private IEnumerator EvaluatePlayerPerformanceCoroutine(int playerIndex)
    {
        while (true)
        {
            // Wait for a random time between 3 and 5 seconds
            float randomInterval = Random.Range(3f, 5f);
            yield return new WaitForSeconds(randomInterval);

            // Check if a message is already being displayed (prevents overlap)
            if (!isDisplayingMessage)
            {
                isDisplayingMessage = true;
                // Call EvaluatePlayerPerformance using the calculated speed
                float speed = playerSpeeds[playerIndex];
                EvaluatePlayerPerformance(speed, playerIndex);

                // Wait a bit to allow the message to display
                yield return new WaitForSeconds(2f); // Assuming message display takes 2 seconds

                isDisplayingMessage = false;
            }
        }
    }
    private void EvaluatePlayerPerformance(float speed, int playerIndex)
    {
        string selectedMessage = "";
        GameObject selectedEmoji = null;

        // Define speed thresholds
        float almostThreshold = 10.0f; // Example threshold for fast speed
        float overfastThreshold = 10.0f; // Example threshold for fast speed
        float fastSpeedThreshold = 5.0f; // Example threshold for fast speed
        float moderateSpeedThreshold = 2.0f; // Example threshold for moderate speed

        // Determine the message based on the speed
        if (speed > almostThreshold)
        {
            selectedMessage = "Kudos";
            selectedEmoji = emotionMessages[selectedMessage]; // Get corresponding emoji prefab
        }
        else if (speed > overfastThreshold && speed <= fastSpeedThreshold)
        {
            selectedMessage = "Halfway Through";
            selectedEmoji = emotionMessages[selectedMessage];
        }
        else if (speed > fastSpeedThreshold && speed <= overfastThreshold)
        {
            selectedMessage = "Almost There";
            selectedEmoji = emotionMessages[selectedMessage]; // Get corresponding emoji prefab
        }
        else if (speed > moderateSpeedThreshold && speed <= fastSpeedThreshold)
        {
            selectedMessage = "Keep Going";
            selectedEmoji = emotionMessages[selectedMessage];
        }
        else if (speed > 1 && speed <= moderateSpeedThreshold)
        {
            selectedMessage = "Pedal! Pedal! Pedal!"; // Add another message for zero speed if needed
            selectedEmoji = emotionMessages[selectedMessage];
        }

        // Log and display the message
        if (!string.IsNullOrEmpty(selectedMessage))
        {
            //Debug.Log($"Player {playerIndex + 1}: Current Speed: {speed:F2} m/s - Message: {selectedMessage}");

            // Update the message text inside the block (if you have a Text component inside the block)
            messageText[playerIndex].text = selectedMessage;

            // Start the sliding animation for the entire block
            StartCoroutine(SlideBlockAndPlayerName(playerIndex));

            // Spawn the emoji at the player's spawn point
            SpawnEmoji(selectedEmoji, emojiSpawnPoints[playerIndex]);
        }
    }
    private IEnumerator SlideBlockAndPlayerName(int playerIndex)
    {
        float slideDuration = 0.5f;
        float displayDuration = 2.0f;
        RectTransform messageBlockRect = messageBlock[playerIndex].GetComponent<RectTransform>();
        RectTransform playerNameRect = nameBlock[playerIndex].GetComponent<RectTransform>();

        Vector2 messageBlockOriginalPos = new Vector2(170 * (playerIndex == 0 ? 1 : -1), messageBlockRect.anchoredPosition.y);
        Vector2 playerNameOriginalPos = playerNameRect.anchoredPosition;

        float messageBlockWidth = messageBlockRect.rect.width;
        float playerNameWidth = playerNameRect.rect.width;

        Vector2 messageBlockOffScreenPos = playerIndex == 0
            ? new Vector2(-messageBlockWidth, messageBlockOriginalPos.y)
            : new Vector2(1920, messageBlockOriginalPos.y);

        Vector2 playerNameOffScreenPos = playerIndex == 0
            ? new Vector2(-playerNameWidth, playerNameOriginalPos.y)
            : new Vector2(1920, playerNameOriginalPos.y);

        // 1. Slide player name out while message block slides in
        float elapsedTime = 0f;
        while (elapsedTime < slideDuration)
        {
            float t = elapsedTime / slideDuration;
            messageBlockRect.anchoredPosition = Vector2.Lerp(messageBlockOffScreenPos, messageBlockOriginalPos, t);
            playerNameRect.anchoredPosition = Vector2.Lerp(playerNameOriginalPos, playerNameOffScreenPos, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        messageBlockRect.anchoredPosition = messageBlockOriginalPos;
        playerNameRect.anchoredPosition = playerNameOffScreenPos;

        yield return new WaitForSeconds(displayDuration);

        // 2. Slide message block out while player name slides back in
        elapsedTime = 0f;
        while (elapsedTime < slideDuration)
        {
            float t = elapsedTime / slideDuration;
            messageBlockRect.anchoredPosition = Vector2.Lerp(messageBlockOriginalPos, messageBlockOffScreenPos, t);
            playerNameRect.anchoredPosition = Vector2.Lerp(playerNameOffScreenPos, playerNameOriginalPos, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        messageBlockRect.anchoredPosition = messageBlockOffScreenPos;
        playerNameRect.anchoredPosition = playerNameOriginalPos;
    }
    private void SpawnEmoji(GameObject emojiPrefab, Transform spawnPoint)
    {
        StartCoroutine(SpawnEmojiEffect(emojiPrefab, spawnPoint));
    }
    private IEnumerator SpawnEmojiEffect(GameObject emojiPrefab, Transform spawnPoint)
    {
        const int emojiCount = 5; // Number of emojis to spawn
        const float spawnDelay = 0.1f; // Delay between each spawn
        const float moveDistance = 500; // Distance to move upwards
        const float fadeDuration = 2.0f; // Time to fade out/in

        // Get the panel's RectTransform to determine random spawn positions
        RectTransform panelRect = spawnPoint.GetComponent<RectTransform>();
        Vector2 panelSize = panelRect.sizeDelta;

        // Calculate the local position bounds for spawning
        Vector2 halfSize = panelSize / 2;

        for (int i = 0; i < emojiCount; i++)
        {
            // Create a random position within the panel's bounds (local space)
            Vector3 randomPosition = new Vector3(
                Random.Range(-halfSize.x, halfSize.x), // Random x position within the width
                Random.Range(-halfSize.y, halfSize.y), // Random y position within the height
                0); // Keep z as 0 for 2D

            // Instantiate the emoji at the calculated random position
            GameObject emojiInstance = Instantiate(emojiPrefab, spawnPoint);

            // Set the local position to the random position
            emojiInstance.transform.localPosition = randomPosition;

            float randomScale = Random.Range(0.5f, 1.5f); // You can adjust min and max size
            emojiInstance.transform.localScale = Vector3.one * randomScale;

            // Set the starting position as the emoji's position and target moveDistance upwards
            Vector3 startPosition = emojiInstance.transform.localPosition;
            Vector3 targetPosition = startPosition + new Vector3(0, moveDistance, 0); // Move up by 100 units

            // Start the movement and fading coroutine (fade in first)
            StartCoroutine(MoveAndFadeEmoji(emojiInstance, startPosition, targetPosition, fadeDuration));

            // Wait before spawning the next emoji
            yield return new WaitForSeconds(spawnDelay);
        }
    }
    private IEnumerator MoveAndFadeEmoji(GameObject emoji, Vector3 startPosition, Vector3 targetPosition, float fadeDuration)
    {
        float elapsedTime = 0f;

        // Fade in and out effect
        CanvasGroup canvasGroup = emoji.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = emoji.AddComponent<CanvasGroup>(); // Add CanvasGroup for fading
        }

        // Start with fully transparent (alpha = 0)
        canvasGroup.alpha = 0;

        // Fade-in phase
        while (elapsedTime < fadeDuration)
        {
            // Move the emoji upwards as it fades in
            emoji.transform.localPosition = Vector3.Lerp(startPosition, targetPosition, elapsedTime / (fadeDuration * 2)); // Move gradually
            canvasGroup.alpha = Mathf.Lerp(0, 1, elapsedTime / fadeDuration); // Fade in

            elapsedTime += Time.deltaTime;
            yield return null; // Wait for the next frame
        }

        // Ensure it's fully visible and at the final position after the fade-in
        canvasGroup.alpha = 1;
        emoji.transform.localPosition = targetPosition;

        elapsedTime = 0f;

        // Fade-out phase
        while (elapsedTime < fadeDuration)
        {
            // Fade out and keep moving upwards
            canvasGroup.alpha = Mathf.Lerp(1, 0, elapsedTime / fadeDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Destroy the emoji instance after fading out
        Destroy(emoji);
    }

}
