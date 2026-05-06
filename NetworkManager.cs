using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

// This script handles ALL networking - both server and client mode
// Attach this to "NetworkManager" (Empty Gameobject)
public class NetworkManager : MonoBehaviour
{
    [Header("Network Settings")]
    public bool isServer = true; // TRUE = this instance is the server, FALSE = this instance is the client

    public int port = 9050; // The "channel" everyone connects to (similar to radio freq.)

    [Header("Team Settings (for clients only)")]
    public string myTeam = "Blue"; // "Blue" or "Red" - only matters if this is a client

    // ============ NETWORKING VARIABLES ============
    private UdpClient udpClient; // UDP radio device
    private Thread receiveThread; // Seperate worker that listens for messages (so game doesn't freeze)
    private bool isRunning = true; // False = Everything shuts down
    private IPEndPoint serverEndpoint; // The server's address (for clients to send to)

    // ============ SERVER-ONLY VARIABLES ============
    private Dictionary<IPEndPoint, string> connectedPlayers; // List of all connected clients and their teams
    private Dictionary<string, int> teamCounts; // Track how many plyers on each team

    // ============ CLIENT-ONLY VARIABLES ============
    private string myAssignedTeam = ""; // What team the server actually assigned me.
    private bool isConnected = false; // Succesfully joined the server?

    void Start()
    {
        // Ensure dispatcher exists on main thread BEFORE any background threads run
        var dispatcher = UnityMainThreadDispatcher.Instance;
        if (isServer)
        {
            // Server (Radio Tower) - Listen for everyone
            StartServer();
        }
        else
        {
            // Client (walkie-talkie) - Connect to the Server (Radio Tower)
            StartClient();
        }
    }

    // ============ SERVER FUNCTIONS ============
    void StartServer()
    {
        //Initialize the server's tracking lists
        connectedPlayers = new Dictionary<IPEndPoint, string>();
        teamCounts = new Dictionary<string, int>();
        teamCounts["Blue"] = 0;
        teamCounts["Red"] = 0;

        try
        {
            // Create -> UDP radio and tune into our channel (port)
            udpClient = new UdpClient(port);
            Debug.Log($"Server started on port {port}. Waiting for players.....");

            // Start a seperate thread to listen for messages (so Unity doesn't freeze)
            receiveThread = new Thread(ReceiveMessagesServer);
            receiveThread.Start();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Server error: {e.Message}");
        }
    }

    // This runs on a seperate thread - it just waits for messages
    void ReceiveMessagesServer()
    {
        IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0); // Listen for ANY sender.

        while (isRunning)
        {
            try
            {
                // Wait for a message to arrive (this line Blocks until a message comes)
                byte[] data = udpClient.Receive(ref anyIP);
                string message = Encoding.UTF8.GetString(data); // Convert bytes to readable text

                // *** Unity can't run game code from seperate thread
                // So we queue the message to be processed on the main thread
                UnityMainThreadDispatcher.Instance.Enqueue(() =>
                {
                    ProcessMessage(message, anyIP); // Calls SERVER version
                });
            }
            catch (System.Exception e)
            {
                if (isRunning)
                    Debug.LogError($"Receive error: {e.Message}");
            }
        }
    }
    
    void ReceiveMessagesClient()
    {
        IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0); // Listen for ANY sender.

        while (isRunning)
        {
            try
            {
                // Wait for a message to arrive (this line Blocks until a message comes)
                byte[] data = udpClient.Receive(ref anyIP);
                string message = Encoding.UTF8.GetString(data); // Convert bytes to readable text

                // *** Unity can't run game code from seperate thread
                // So we queue the message to be processed on the main thread
                UnityMainThreadDispatcher.Instance.Enqueue(() =>
                {
                    ProcessClientMessage(message); // Calls CLIENT version
                });
            }
            catch (System.Exception e)
            {
                if (isRunning)
                    Debug.LogError($"Receive error: {e.Message}");
            }
        }
    }

    // This runs on the MAIN thread - safe to access Unity stuff
    void ProcessMessage(string message, IPEndPoint sender)
    {
        string[] parts = message.Split('|');
        
        // If message is empty, ignore it
        if (parts.Length == 0)
        {
            Debug.Log($"Empty message received");
            return;
        }
        string messageType = parts[0];

        switch (messageType)
        {
            case "JOIN":
                // Check: JOIN\TeamName (needs 2 parts)
                if (parts.Length < 2)
                {
                    Debug.Log($"Malformed JOIN message: {message}");
                    return;
                }
                // A new player wants to join
                HandleJoin(sender, parts[1]); // parts[1] is their requested team
                break;

            case "POS":
                //Check: POS|x|y|z (needs 4 parts)
                if (parts.Length < 4)
                {
                    Debug.Log($"Malformed POS message: {message}");
                    return;
                }
                // A player sent their position
                // parts[1]=x, parts[2]=y, parts[3]=z
                HandlePosition(sender, parts[1], parts[2], parts[3]);
                break;

            case "SPOTTED":
                // Check: SPOTTED|x|y|z (needs 4 parts)
                if (parts.Length < 4)
                {
                    Debug.Log($"Malformed SPOTTED message: {message}");
                    return;
                }
                // A player spotted an enemy!
                // parts[1]=x, parts[2]=y, parts[3]=z
                HandleSpotted(sender, parts[1], parts[2], parts[3]);
                break;

            case "LEAVE":
                // A player is disconnecting
                HandleLeave(sender);
                break;

            default:
                Debug.Log($"Unknown message: {message}");
                break;
        }
    }
    
    // Runs on the CLIENT to handle messages from the server
    void ProcessClientMessage(string message)
    {
        string[] parts = message.Split('|');
        string messageType = parts[0];

        switch (messageType)
        {
            case "JOINED":
                if (parts.Length < 2)
                {
                    Debug.Log($"Malformed JOINED message: {message}");
                    return;
                }
                myAssignedTeam = parts[1];
                isConnected = true;
                Debug.Log($"Joined team: {myAssignedTeam}");
                break;
            
            case "SYSTEM":
                if (parts.Length < 2)
                {
                    Debug.Log($"Malformed SYSTEM message: {message}");
                    return;
                }
                Debug.Log($"{parts[1]}");
                break;
            
            case "POS":
                if (parts.Length < 5)  // POS|team|x|y|z = 5 parts
                {
                    Debug.Log($"Malformed POS message: {message}");
                    return;
                }
                Debug.Log($"Teammate at ({parts[2]}, {parts[3]}, {parts[4]}");
                break;
            
            case "SPOTTED":
                if (parts.Length < 4)  // SPOTTED|x|y|z = 4 parts
                {
                    Debug.Log($"Malformed SPOTTED message: {message}");
                    return;
                }
                Debug.Log($"ENEMY SPOTTED at ({parts[1]}, {parts[2]}, {parts[3]})");
                break;
            
            default:
                Debug.Log($"Unknown message: {message}");
                break;
        }
    }

    void HandleJoin(IPEndPoint sender, string requestedTeam)
    {
        // Auto-balance teams: put player on the team with fewer players
        string assignedTeam = requestedTeam;
        if (teamCounts["Blue"] > teamCounts["Red"])
            assignedTeam = "Red";
        else if (teamCounts["Red"] > teamCounts["Blue"])
            assignedTeam = "Blue";

        // Add player to our tracking lists
        connectedPlayers[sender] = assignedTeam;
        teamCounts[assignedTeam]++;

        Debug.Log(
            $"Player {sender.Address} joined {assignedTeam} team! (Blue: {teamCounts["Blue"]}, Red:{teamCounts["Red"]}");

        // Send confirmation back to the player
        SendMessage($"JOINED|{assignedTeam}", sender);

        //Announce to all players that someone joined (for chat log)
        BroadcastToAll($"SYSTEM|A player joined the {assignedTeam} team");
    }

    void HandlePosition(IPEndPoint sender, string x, string y, string z)
    {
        // Make sure this player is actually connected
        if (!connectedPlayers.ContainsKey(sender)) return;

        string team = connectedPlayers[sender];
        string positionMsg = $"POS|{team}|{x}|{y}|{z}";

        // Send position ONLY TO TEAMMATES (not including the sender themselves)
        BroadcastToTeam(positionMsg, team, sender, excludeSelf: true);
    }

    void HandleSpotted(IPEndPoint sender, string x, string y, string z)
    {
        if (!connectedPlayers.ContainsKey(sender)) return;

        string team = connectedPlayers[sender];
        string spottedMsg = $"SPOTTED|{x}|{y}|{z}";

        Debug.Log($"ENEMY SPOTTED! Team {team} at ({x}, {y}, {z})");

        // Send "Enemy Spotted" ONLY to teammates (excluding the spotter)
        BroadcastToTeam(spottedMsg, team, sender, excludeSelf: true);
    }

    void HandleLeave(IPEndPoint sender)
    {
        if (!connectedPlayers.ContainsKey(sender)) return;

        string team = connectedPlayers[sender];
        connectedPlayers.Remove(sender);
        teamCounts[team]--;

        Debug.Log($"Player {sender.Address} left the game");
        BroadcastToAll($"SYSTEM|A player left the game");
    }

    // Send a message to everyone on a specific team
    void BroadcastToTeam(string message, string team, IPEndPoint excludeSender = null, bool excludeSelf = false)
    {
        foreach (var player in connectedPlayers)
        {
            // Skip if this player is not on the target team
            if (player.Value != team) continue;

            // Skip if we need to exclude the sender
            if (excludeSelf && excludeSender != null && player.Key.Equals(excludeSender)) continue;

            SendMessage(message, player.Key);
        }
    }

    // Send a message to Every connected player
    void BroadcastToAll(string message, IPEndPoint excludeSender = null)
    {
        foreach (var player in connectedPlayers)
        {
            if (excludeSender != null && player.Key.Equals(excludeSender)) continue;
            SendMessage(message, player.Key);
        }
    }

    // Send a message to a specific player
    void SendMessage(string message, IPEndPoint target)
    {
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            udpClient.Send(data, data.Length, target);
            Debug.Log($"Send to {target.Address}: {message}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Send error: {e.Message}");
        }
    }


    // ============ CLIENT FUNCTIONS ============

    void StartClient()
    {
        try
        {
            udpClient = new UdpClient(); // Create client (walkie-talkie)
            udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, 0));    //Bind client to a random available port.
            // Set the server's address (127.0.0.1 = "this same PC")
            serverEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);
            Debug.Log($"Client started. Sending to server at {serverEndpoint.Address}:{port}");

            // Start listening for messages from the server
            receiveThread = new Thread(ReceiveMessagesClient);
            receiveThread.Start();

            // Tell the server we want to join
            SendMessage($"JOIN|{myTeam}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Client error: {e.Message}");
        }
    }

    // Client version of ReceiveMEssages - listens for server responses
    // (Same as server version, but processes differently)

    // Call this from PlayerController to send position
    public void SendMyPosition(float x, float y, float z)
    {
        if (!isConnected) return;
        SendMessage($"POS|{x:F2}|{y:F2}|{z:F2}");
    }

    // Call this from PlayerController when pressing Space
    public void SendSpotted(float x, float y, float z)
    {
        if (!isConnected) return;
        SendMessage($"SPOTTED|{x:F2}|{y:F2}|{z:F2}");
        Debug.Log($"You spotted an enemy at ({x:F2},{y:F2},{z:F2})!");
    }

    // Client sends a message to the server
    void SendMessage(string message)
    {
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            udpClient.Send(data, data.Length, serverEndpoint);
            Debug.Log($"Sent to server: {message}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Client send error: {e.Message}");
        }
    }

    void OnDestroy()
    {
        isRunning = false;
        if (isConnected)
            SendMessage("LEAVE");
        receiveThread?.Join(500);
        udpClient?.Close();
    }
}
