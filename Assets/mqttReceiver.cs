using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System;


public class mqttReceiver : MonoBehaviour
{
    public GameObject Sphere;    
    private MqttClient client;
    public string brokerIpAddress = "127.0.0.1"; // MQTT broker IP address. Please update accordingly
    public int brokerPort = 1883; // MQTT broker port. Please update accordingly
    public string topicSubscribe = "movement"; // MQTT topic to subscribe to
    public string topicPublish = "information";
    private Queue<Action> mainThreadQueue = new Queue<Action>(); //alternative to main() function as transform propert of gameobject cant be used in private function.
    private Vector3 previousPosition; // Vector to store previous coordinates of the sphere.
    
    private void Start()
    {
        ConnectToBroker(); //Connecting to mosquitto
        previousPosition = Sphere.transform.position; //storing current coordinates of sphere in a vector
    }

    private void ConnectToBroker()
    {
        client = new MqttClient(brokerIpAddress, brokerPort, false, null, null, MqttSslProtocols.None);
        client.MqttMsgPublishReceived += Client_MqttMsgPublishReceived;

        string clientId = "UnityClient" + UnityEngine.Random.Range(0, 1000); // Unique client ID
        client.Connect(clientId);

        client.Subscribe(new string[] { topicSubscribe }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
    }  // Code from given link to connect to MQTT client from UNITY.


    private void Client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
    {
        string receivedMessage = System.Text.Encoding.UTF8.GetString(e.Message);
        //Debug.Log("Received message: " + receivedMessage); Checking if the message is received from the server
        mainThreadQueue.Enqueue(() => StoreCoordinates(receivedMessage)); //calling the function from queue like main()
    }

   
    private void StoreCoordinates(string message)
    {
        if (message.StartsWith("(") && message.EndsWith(")")) //Checking if the input match (X,Y,Z) format
        {
            
            string trimmedMessage = message.Trim('(', ')', ' '); // Trim parentheses and whitespace
            string[] numbersString = trimmedMessage.Split(',');
            float[] coordinates = new float[numbersString.Length];
            for (int i = 0; i < numbersString.Length; i++)
            {
                int number;
                if (int.TryParse(numbersString[i], out number))
                {
                    coordinates[i] = number; //Storing the coordinates in an array
                }
                else
                {
                    coordinates[i] = 0; // Set missing coordinate values to 0
                }


            }
            //Debug.Log(coordinates[0]);
            Vector3 targetPosition = new Vector3(coordinates[0], coordinates[1], coordinates[2]); // Converitng same array to vector3.

            MoveSphere(targetPosition); // Calling the function to move the sphere to the given coordinates
            string messagetoServer = $"I have moved from (X:{previousPosition[0]},Y:{previousPosition[1]},Z:{previousPosition[2]}) to (X:{targetPosition[0]},Y:{targetPosition[1]},Z:{targetPosition[2]}).";
            Debug.Log(messagetoServer);
            previousPosition = targetPosition; // Updating the current position of the sphere
            client.Publish(topicPublish, System.Text.Encoding.UTF8.GetBytes(messagetoServer), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false); //Publishing new coordinates to information topic
        }
        //Assuming input is in correct format. It will do nothing if wrong format is provided

    }

    private void MoveSphere(Vector3 coordinates)
    {
        Sphere.transform.position = coordinates; // Moving the sphere to given coordinates

    }

    private void Update()
    {
        while (mainThreadQueue.Count > 0)
        {
            Action action = mainThreadQueue.Dequeue();
            action.Invoke();
        }
    }

    private void OnDestroy()
    {
        if (client != null && client.IsConnected)
        {
            client.Unsubscribe(new string[] { topicSubscribe });
            client.Disconnect();
        }
    }
}