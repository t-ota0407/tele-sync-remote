using NativeWebSocket;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class WSCommunicationManager : MonoBehaviour
{
    private WebSocket webSocket;

    private Coroutine sendDataCoroutine;

    [SerializeField] private string myClientId = "";
    [SerializeField] private string targetClientId = "";

    [SerializeField] private GameObject headObj;
    [SerializeField] private GameObject leftHandObj;
    [SerializeField] private GameObject rightHandObj;

    [SerializeField] private PartnersAvatarManager partnersAvatarManager;

    private int registerApiTrials = 0;

    // Start is called before the first frame update
    async void Start()
    {
        InitializeWebSocket();
    }

    // Update is called once per frame
    void Update()
    {
        const int MAX_REGISTER_API_TRIALS = 50;
        if (webSocket != null
            && webSocket.State == WebSocketState.Open
            && registerApiTrials < MAX_REGISTER_API_TRIALS)
        {
            Register();
            registerApiTrials++;

            if (sendDataCoroutine is null && registerApiTrials == MAX_REGISTER_API_TRIALS)
            {
                sendDataCoroutine = StartCoroutine(SendData());
            }
        }

#if !UNITY_WEBGL || UNITY_EDITOR
        webSocket?.DispatchMessageQueue();
#endif   
    }

    async void InitializeWebSocket()
    {
        webSocket = new WebSocket("wss://dev1.t-ota0407.com");

        webSocket.OnOpen += () =>
        {
            Debug.Log("WebSocket Opened");
        };

        webSocket.OnMessage += (bytes) =>
        {
            string message = Encoding.UTF8.GetString(bytes);
            Debug.Log("Message received: " + message);
            OnReceived(message);
        };

        webSocket.OnError += (err) =>
        {
            Debug.Log("WebSocket Error: " + err);
        };

        webSocket.OnClose += (e) =>
        {
            Debug.Log("WebSocket Closed");
        };

        await webSocket.Connect();
    }

    async void Register()
    {
        var registerMsg = new RegisterMessage
        {
            type = "register",
            id = myClientId
        };

        string json = JsonUtility.ToJson(registerMsg);
        await webSocket.SendText(json);
    }

    private async void OnDestroy()
    {
        if (sendDataCoroutine != null)
        {
            StopCoroutine(sendDataCoroutine);
        }

        await CloseWebSocket();
    }

    private async Task CloseWebSocket()
    {
        if (webSocket != null && webSocket.State == WebSocketState.Open)
        {
            await webSocket.Close();
        }
    }

    IEnumerator SendData()
    {
        while (true)
        {
            SendDataAsync();

            yield return new WaitForSeconds(1f / 20f); // 20fps
        }
    }

    private async void SendDataAsync()
    {
        if (webSocket == null || webSocket.State != WebSocketState.Open)
        {
            Debug.LogWarning("WebSocket is not open, skipping send.");

             InitializeWebSocket();
            registerApiTrials = 0;
            
            return;
        }

        var syncDataPacket = new SyncDataPacket
        {
            headPos = headObj.transform.position,
            headRot = headObj.transform.rotation,
            leftHandPos = leftHandObj.transform.position,
            leftHandRot = leftHandObj.transform.rotation,
            rightHandPos = rightHandObj.transform.position,
            rightHandRot = rightHandObj.transform.rotation
        };

        var sendData = new SyncDataUpload
        {
            type = "syncData",
            from = myClientId,
            to = targetClientId,
            data = syncDataPacket
        };

        string json = JsonUtility.ToJson(sendData);

        await webSocket.SendText(json);
    }

    private void OnReceived(string json)
    {
        try
        {
            var wrapper = JsonUtility.FromJson<SyncDataDownload>(json);
            SyncDataPacket data = wrapper.data;

            UsersPosture posture = new();
            posture.head = new();
            posture.head.position = data.headPos;
            posture.head.rotation = data.headRot.eulerAngles;
            posture.leftHand = new();
            posture.leftHand.position = data.leftHandPos;
            posture.leftHand.rotation = data.leftHandRot.eulerAngles;
            posture.rightHand = new();
            posture.rightHand.position = data.rightHandPos;
            posture.rightHand.rotation = data.rightHandRot.eulerAngles;

            partnersAvatarManager.SetPartnersPosture(posture);
        }
        catch (Exception e)
        {
            Debug.LogError("WS message parse error: " + e.Message);
            Debug.LogError(e.StackTrace);
        }
    }

    [Serializable]
    public class RegisterMessage
    {
        public string type;
        public string id;
    }

    [Serializable]
    public class SyncDataUpload
    {
        public string type;
        public string from;
        public string to;
        public SyncDataPacket data;
    }

    [Serializable]
    public class SyncDataDownload
    {
        public string from;
        public SyncDataPacket data;
    }

    [Serializable]
    public class SyncDataPacket
    {
        public Vector3 headPos;
        public Quaternion headRot;
        public Vector3 leftHandPos;
        public Quaternion leftHandRot;
        public Vector3 rightHandPos;
        public Quaternion rightHandRot;
    }
}
