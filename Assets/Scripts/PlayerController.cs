using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using Unity.Services.Analytics;
using Unity.RemoteConfig;
using Unity.Services.Authentication;
using Unity.Services.Core;
using System.Threading.Tasks;

public struct userAttributes{}

public struct appAttributes{}

public class PlayerController : MonoBehaviour
{

    public int speed = 5;
    public bool win = false;
    public TextMeshProUGUI countText;
    public GameObject winTextObject;

    private Rigidbody rb;
    private float movementX;
    private float movementY;
    private int count;

    [SerializeField] int Level = 1;

    // Retrieve and apply the current key-value pairs from the service on Awake:
    async void Awake()
    {
        // initialize Unity's authentication and core services
        await InitializeRemoteConfigAsync();

        // Add a listener to apply settings when successfully retrieved:
        ConfigManager.FetchCompleted += ApplyRemoteSettings;

        // Set the user’s unique ID:
        //ConfigManager.SetCustomUserID("some-user-id");

        // Set the environment ID:
        ConfigManager.SetEnvironmentID("e6aa1740-eb08-41d7-af8c-69cd619a2244");

        var userAttrib = new userAttributes();
        var appAttrib = new appAttributes();
        // Fetch configuration setting from the remote service:
        ConfigManager.FetchConfigs<userAttributes, appAttributes>(userAttrib, appAttrib);
    }

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        count = 0;

        SetCountText();
        winTextObject.SetActive(false);
    }

    async Task InitializeRemoteConfigAsync()
    {
        // initialize handlers for unity game services
        await UnityServices.InitializeAsync();

        // remote config requires authentication for managing environment information
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }



    void ApplyRemoteSettings(ConfigResponse configResponse)
    {
        Debug.Log(Level);
        // Conditionally update settings, depending on the response's origin:
        switch (configResponse.requestOrigin)
        {
            case ConfigOrigin.Default:
                Debug.Log("No settings loaded this session; using default values.");
                break;
            case ConfigOrigin.Cached:
                Debug.Log("No settings loaded this session; using cached values from a previous session.");
                break;
            case ConfigOrigin.Remote:
                Debug.Log("New settings loaded this session; update values accordingly.");
                speed = ConfigManager.appConfig.GetInt("ballSpeed");
                //win = ConfigManager.appConfig.GetString("win");
                break;
        }
    }

    

    void OnMove(InputValue movementValue)
    {
        Vector2 movementVector = movementValue.Get<Vector2>();

        movementX = movementVector.x;
        movementY = movementVector.y;
    }

    void SetCountText()
    {
        countText.text = "Count: " + count.ToString();
        if(count >= 12)
        {
            win = true;
            winTextObject.SetActive(true);
        }
    }

    void FixedUpdate()
    {
        Vector3 movement = new Vector3(movementX, 0.0f, movementY);
        rb.AddForce(movement*speed);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("PickUp"))
        {
            other.gameObject.SetActive(false);
            count++;

            SetCountText();

            // Send custom event
            Dictionary<string, object> parameters = new Dictionary<string, object>()
{
    { "pickUp", count },
    { "levelWin", win },
};
            // The ‘playerDetails’ event will get queued up and sent every minute
            Events.CustomData("playerDetails", parameters);

            // Optional - You can call Events.Flush() to send the event immediately
            Events.Flush();
        }
    }

}
