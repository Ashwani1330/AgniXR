using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TMPro;

public class UDPReceiver : MonoBehaviour
{
    [Header("Connection Settings")]
    public int port = 5000;

    [Header("Live Sensor Data")]
    public float potentiometer;
    public float distance;

    [Header("UI Elements")]
    public TMP_Text potentiometer_ui;
    public TMP_Text ToF_ui;

    // UDP Objects
    private Thread receiveThread;
    private UdpClient client;
    
    // Data buffer
    private string lastReceivedJSON = "";
    private bool isTxStarted = false;

    // JSON Helper Class (Must match the Arduino keys exactly!)
    [System.Serializable]
    public class SensorData
    {
        public int pot;
        public int dist;
    }

    void Start()
    {
        // 1. Start the background thread
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    void Update()
    {
        // 2. Check if we have new data to parse
        // We do this here because JsonUtility is NOT thread-safe
        if (!string.IsNullOrEmpty(lastReceivedJSON))
        {
            try
            {
                SensorData data = JsonUtility.FromJson<SensorData>(lastReceivedJSON);
                
                // Update the public variables
                if (data != null)
                {
                    potentiometer = data.pot;
                    distance = data.dist;
                    Debug.Log("Potentiometer: " + potentiometer + " Distance: " + distance);

                    // Update UI
                    if (potentiometer_ui != null)
                        potentiometer_ui.text = "Potentiometer: " + potentiometer.ToString("F2");
                    if (ToF_ui != null)
                        ToF_ui.text = "Laser: " + distance.ToString("F2");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("JSON Parse Error: " + e.Message);
            }

            // Optional: Clear string to ensure we don't re-parse old data 
            // (though in this high-speed setup, we usually just overwrite it)
            // lastReceivedJSON = ""; 
        }
    }

    private void ReceiveData()
    {
        try
        {
            // Close any existing client before creating a new one
            if (client != null)
            {
                try
                {
                    client.Close();
                }
                catch { }
            }

            // Create UDP client with socket reuse option
            client = new UdpClient();
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            client.Client.Bind(new IPEndPoint(IPAddress.Any, port));
            
            Debug.Log("UDP Receiver started on port " + port);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to start UDP Receiver on port " + port + ": " + e.Message);
            return;
        }

        while (true)
        {
            try
            {
                // 3. Receive Bytes
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = client.Receive(ref anyIP);

                // 4. Convert to String
                string text = Encoding.UTF8.GetString(data);
                
                // Store for the Update() loop to handle
                lastReceivedJSON = text;
            }
            catch (System.Exception err)
            {
                Debug.Log(err.ToString());
            }
        }
    }

    // 5. Cleanup to prevent Unity freezing on stop
    void OnApplicationQuit()
    {
        StopReceiver();
    }

    void OnDisable()
    {
        StopReceiver();
    }

    void OnDestroy()
    {
        StopReceiver();
    }

    private void StopReceiver()
    {
        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Abort();
            receiveThread = null;
        }

        if (client != null)
        {
            try
            {
                client.Close();
                client = null;
            }
            catch { }
        }
    }
}