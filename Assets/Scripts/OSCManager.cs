using extOSC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OSCManager : MonoBehaviour
{
    private string receivedPlayerDataAddress = "/players/data";
    private string receivedCycleDataAddress = "/cycles/data";
    private string receivedGameTargetAddress = "/gameTarget";
    private string receivedStartAddress = "/gameStart";
    private string receivedRestartAddress = "/gameRestart";
    private string receivedResetAddress = "/reset";
    private string receivedFloatDataAddress = "/floatData";

    public delegate void PlayerDataReceived(PlayerDataList playerData);
    public delegate void CycleDataReceived(string cycleData);
    public delegate void GameTargetReceived(int target);
    public delegate void GameStartReceived(bool start);
    public delegate void GameRestartReceived(bool restart);
    public delegate void ResetReceived();
    public delegate void FloatDataReceived(float floatData);

    // Events for other scripts to subscribe
    public static event PlayerDataReceived OnPlayerDataReceived;
    public static event CycleDataReceived OnCycleDataReceived;
    public static event GameTargetReceived OnGameTargetReceived;
    public static event GameStartReceived OnGameStartReceived;
    public static event GameRestartReceived OnGameRestartReceived;
    public static event ResetReceived OnResetReceived;
    public static event FloatDataReceived OnFloatDataReceived;


    private OSCReceiver _receiver;
    private OSCTransmitter _transmitter;

    public static OSCManager Instance;

    // Store the address-to-handler mappings (delegates)
    private Dictionary<string, System.Action<OSCMessage>> addressHandlers = new Dictionary<string, System.Action<OSCMessage>>();

    [Header("OSC Settings")]
    public int localPort = 7000; // Port to listen on for OSC messages
    public string RemoteIP = "127.0.0.1";
    public int RemotePort = 57121;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void Start()
    {
        // Initialize the OSC receiver
        _receiver = gameObject.AddComponent<OSCReceiver>();

        // Set the port to listen on
        _receiver.LocalPort = localPort;
        Debug.Log($"OSC Receiver listening on port: {localPort}");

        // Bind receiver to a generic handler
        _receiver.Bind("/*", OnMessageReceive);

        RegisterAddressHandler(receivedPlayerDataAddress, HandlePlayerData);
        RegisterAddressHandler(receivedCycleDataAddress, HandleCycleData);
        RegisterAddressHandler(receivedGameTargetAddress, HandleGameTargetData);
        RegisterAddressHandler(receivedStartAddress, HandleGameStart);
        RegisterAddressHandler(receivedRestartAddress, HandleGameRestart);
        RegisterAddressHandler(receivedResetAddress, HandleResetData);
        RegisterAddressHandler(receivedFloatDataAddress, HandleFloatData);

        // Setting Address for transmisstion
        _transmitter = gameObject.AddComponent<OSCTransmitter>();
        _transmitter.RemoteHost = RemoteIP;
        _transmitter.RemotePort = RemotePort;
    }

    // Register handler for a specific OSC address
    private void RegisterAddressHandler(string address, System.Action<OSCMessage> handler)
    {
        addressHandlers[address] = handler;
    }

    // Generic message handler
    private void OnMessageReceive(OSCMessage message)
    {
        string address = message.Address;
        // Log the address to see if it's received
        //Debug.Log($"Received OSC message with address: {address}");

        // Print the arguments (values) of the OSC message
        foreach (var value in message.Values)
        {
            //Debug.Log($"OSC message value: {value}");
        }

        if (addressHandlers.ContainsKey(address))
        {
            // Call the handler for this specific address
            addressHandlers[address]?.Invoke(message);
        }
        else
        {
            Debug.LogWarning($"No handler registered for address: {address}");
        }
    }
    // Handle player data and pass it via delegate
    private void HandlePlayerData(OSCMessage message)
    {
        string jsonString = message.Values[0].StringValue;
        Debug.Log($"Received Player Data: {jsonString}");
        PlayerDataList playerDataList = JsonUtility.FromJson<PlayerDataList>(jsonString);
        if (playerDataList == null)
        {
            Debug.LogError("Failed to deserialize player data from JSON.");
            return;
        }
        if (playerDataList.players != null)
        {
            foreach (var player in playerDataList.players)
            {
                Debug.Log($"Player Name: {player.name}, Gender: {player.gender}");
            }
        }
        else
        {
            Debug.LogError("playerDataList.players is null.");
        }
        OnPlayerDataReceived?.Invoke(playerDataList);
    }

    // Handle game target data and pass it via delegate
    private void HandleGameTargetData(OSCMessage message)
    {
        int gameTarget = message.Values[0].IntValue;
        Debug.Log($"Received Game Target: {gameTarget}");
        OnGameTargetReceived?.Invoke(gameTarget); // Pass to delegate
    }

    // Handle float data and pass it via delegate
    private void HandleFloatData(OSCMessage message)
    {
        float floatData = message.Values[0].FloatValue;
        Debug.Log($"Received Float Data: {floatData}");
        OnFloatDataReceived?.Invoke(floatData); // Pass to delegate
    }

    // Handle reset data and pass it via delegate
    private void HandleResetData(OSCMessage message)
    {
        Debug.Log($"Received Reset Data");
        OnResetReceived?.Invoke(); // Pass to delegate
    }

    // Handle game start and pass it via delegate
    private void HandleGameStart(OSCMessage message)
    {
        bool gameStart = message.Values[0].BoolValue;
        Debug.Log($"Game Start received: {gameStart}");
        OnGameStartReceived?.Invoke(gameStart); // Pass to delegate
    }

    // Handle game restart and pass it via delegate
    private void HandleGameRestart(OSCMessage message)
    {
        bool gameRestart = message.Values[0].BoolValue;
        Debug.Log($"Game Restart received: {gameRestart}");
        OnGameRestartReceived?.Invoke(gameRestart); // Pass to delegate
    }

    // Handle cycle data and pass it via delegate
    private void HandleCycleData(OSCMessage message)
    {
        string cycleData = message.Values[0].StringValue;
        //Debug.Log($"Received Cycle Data: {cycleData}");
        OnCycleDataReceived?.Invoke(cycleData); // Pass to delegate
    }
    // Method to send OSC message to Node.js server
    public void SendOSCMessage(string address, params object[] values)
    {
        // Convert each object value to OSCValue
        List<OSCValue> oscValues = new List<OSCValue>();
        foreach (object value in values)
        {
            if (value is int intValue)
            {
                oscValues.Add(OSCValue.Int(intValue));
            }
            else if (value is float floatValue)
            {
                oscValues.Add(OSCValue.Float(floatValue));
            }
            else if (value is string stringValue)
            {
                oscValues.Add(OSCValue.String(stringValue));
            }
            else if (value is bool boolValue)
            {
                oscValues.Add(OSCValue.Bool(boolValue));
            }
            else
            {
                Debug.LogWarning($"Unsupported OSC value type: {value.GetType()}");
            }

            Debug.Log("Value sent..!");
        }

        // Create the OSC message with the OSC address and converted OSC values
        var message = new OSCMessage(address, oscValues.ToArray());

        // Send the message via the OSC transmitter
        _transmitter.Send(message);
    }
    // Example usage to send a message from Unity to Node.js (if needed)
    public void SendInputMessage(string inputValue)
    {
        SendOSCMessage("/sendInput", inputValue);
        Debug.Log("Send Value Sent to Server");
    }
    public void SendRestartMessage()
    {
        SendOSCMessage("/sendRestart", "Game Over! You Can Restart");
        Debug.Log("Restart Message Sent to Server");
    }
    [System.Serializable]
    public class PlayerData
    {
        public string name;
        public string gender;
    }
    [System.Serializable]
    public class PlayerDataList
    {
        public PlayerData[] players;
    }
}
