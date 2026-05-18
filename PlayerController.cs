using UnityEngine;
using UnityEngine.UI;

// For player character

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float movementSpeed = 5f;
    
    [Header("UI (optional)")]
    public Text statusText;    // Create and assign a UI Text here to see messages

    private NetworkManager networkManager;
    private Vector3 lastSentPosition;

    void Start()
    {
        // Find NetworkManager in the scene
        networkManager = FindObjectOfType<NetworkManager>();

        if (networkManager == null)
        {
            Debug.LogError("No NetworkManager found in scene.!");
        }
        lastSentPosition = transform.position;
    }

    void Update()
    {
        // ======== MOVEMENT (WASD) ========
        float horizontal = Input.GetAxis("Horizontal");     //Left/Right
        float vertical = Input.GetAxis("Vertical");     // Up/Down
        
        Vector3 movement = new Vector3(horizontal, 0, vertical) * movementSpeed * Time.deltaTime;
        transform.Translate(movement);
        
        // ======== SEND POSITION (only if moved enough) ========
        float distanceMoved = Vector3.Distance(transform.position, lastSentPosition);
        if (distanceMoved > 0.1f) // Don't spam if barely moving
        {
            lastSentPosition = transform.position;
            networkManager.SendMyPosition(transform.position.x, transform.position.y, transform.position.z);
        }
        
        // ======== SPOT ENEMY (Press Space) ========
        if (Input.GetKeyDown(KeyCode.Space))
        {
            networkManager.SendSpotted(transform.position.x, transform.position.y, transform.position.z);
            
            // Visual feedback: brief GREEN flash (you send a ping, not you were hit)
            GetComponent<Renderer>().material.color = Color.green;
            Invoke(nameof(ResetColor), 0.2f);
        }
        
        // ======== UPDATE UI ========
        if (statusText != null)
        {
            statusText.text = $"Position: ({transform.position.x:F1}, {transform.position.y:F1}, {transform.position.z:F1})\nPress SPACE to spot enemy";
        }
    }

    void ResetColor()
    {
        GetComponent<Renderer>().material.color = Color.white;
    }
}
