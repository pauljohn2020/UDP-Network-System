using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ServerModeController : MonoBehaviour
{
    public NetworkManager networkManager;   // Drag your NetworkManager here
    public Toggle serverToggle;     // Drag the Toggle here

    void Start()
    {
        // Find NetworkManager if not assigned
        if (networkManager == null)
            networkManager = FindObjectOfType<NetworkManager>();
        
        // Set toggle to match current NetworkManager setting
        serverToggle.isOn = networkManager.isServer;
        
        // Add listener for when Carl clicks the toggle
        serverToggle.onValueChanged.AddListener(OnToggleValueChanged);
    }

    void OnToggleValueChanged(bool isOn)
    {
        networkManager.isServer = isOn;
        Debug.Log($"Server mode set to: {isOn}");
        
        if(isOn)
            serverToggle.GetComponentInChildren<Text>().color = Color.green;
        else
            serverToggle.GetComponentInChildren<Text>().color = Color.white;
    }
}
