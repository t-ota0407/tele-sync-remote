using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class UDPCommunicationManager : MonoBehaviour
{
    private const int APPLICATION_UDP_PORT = 32001;

    [SerializeField] private string partnersIP;

    [SerializeField] private GameObject headObject;
    [SerializeField] private GameObject leftHandObject;
    [SerializeField] private GameObject rightHandObject;

    [SerializeField] private PartnersAvatarManager partnersAvatarManager;

    private UdpClient udpClient;
    private IPEndPoint partnersEndPoint;

    // Start is called before the first frame update
    void Start()
    {
        udpClient = new(APPLICATION_UDP_PORT);
        partnersEndPoint = new(IPAddress.Parse(partnersIP), APPLICATION_UDP_PORT);
        udpClient.Connect(partnersEndPoint);

        udpClient.BeginReceive(PartnersPostureReceived, null);
    }

    // Update is called once per frame
    void Update()
    {
        SendUsersPosture();
    }

    void OnDestroy()
    {
        udpClient.Close();
    }

    private void SendUsersPosture()
    {
        try
        {
            UsersPosture usersPosture = new();
            if (headObject is not null)
                usersPosture.head = MakePosture(headObject);
            if (leftHandObject is not null)
                usersPosture.leftHand = MakePosture(leftHandObject);
            if (rightHandObject is not null)
                usersPosture.rightHand = MakePosture(rightHandObject);

            string jsonMessage = JsonUtility.ToJson(usersPosture);
            var byteMessage = Encoding.UTF8.GetBytes(jsonMessage);

            udpClient.Send(byteMessage, byteMessage.Length);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error sending UDP message: {e.Message}");
        }
    }

    private Posture MakePosture(GameObject gameObject)
    {
        Vector3 position = gameObject.transform.position;
        Vector3 rotation = gameObject.transform.rotation.eulerAngles;

        Posture posture = new();
        posture.position = position;
        posture.rotation = rotation;
        return posture;
    }

    private void PartnersPostureReceived(IAsyncResult result)
    {
        IPEndPoint senderEndPoint = new(IPAddress.Any, 0);
        var byteMessage = udpClient.EndReceive(result, ref senderEndPoint);
        string message = Encoding.UTF8.GetString(byteMessage);
        Debug.Log(message);

        try
        {
            UsersPosture partnersPosture = JsonUtility.FromJson<UsersPosture>(message);
            partnersAvatarManager.SetPartnersPosture(partnersPosture);
        }
        catch (Exception e)
        {
            Debug.LogError("UDP message parse error: " + e.Message);
            Debug.LogError(e.StackTrace);
        }

        udpClient.BeginReceive(PartnersPostureReceived, udpClient);
    }
}
