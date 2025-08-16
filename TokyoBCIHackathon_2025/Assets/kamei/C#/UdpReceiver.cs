using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Concurrent;

public class UdpReceiver : MonoBehaviour
{
    private UdpClient udpClient;
    private Thread receiveThread;
    public int port = 5005;
    private ConcurrentQueue<string> messageQueue = new ConcurrentQueue<string>();
    private volatile bool isRunning = false;

    // ボタンに紐づけるメソッド
    public void StartReceiving()
    {
        if (isRunning) return;

        udpClient = new UdpClient(port);
        isRunning = true;
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
        Debug.Log("▶ UDP受信開始 (ポート: " + port + ")");
    }

    public void StopReceiving()
    {
        if (!isRunning) return;

        isRunning = false;
        udpClient.Close();  // Receive() が例外で終了
        receiveThread?.Join();  // スレッド終了を待つ
        Debug.Log("■ UDP受信停止");
    }

    void Update()
    {
        while (messageQueue.TryDequeue(out string message))
        {
            Debug.Log("EEG Data (from Update): " + message);
        }
    }

    private void ReceiveData()
    {
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, port);
        while (isRunning)
        {
            try
            {
                byte[] data = udpClient.Receive(ref remoteEndPoint);
                string rawHex = BitConverter.ToString(data);
                Debug.Log("◆ Raw Bytes: " + rawHex);

                string ascii = Encoding.ASCII.GetString(data);
                Debug.Log("◆ ASCII Decoded: " + ascii);

                string[] values = ascii.Split(',');
                if (values.Length == 70)
                    Debug.Log("✅ EEG データ70項目を受信しました");
                else
                    Debug.LogWarning("⚠️ データ項目数 = " + values.Length);

                messageQueue.Enqueue(ascii);
            }
            catch (SocketException e)
            {
                if (isRunning)
                    Debug.LogError("Socket error: " + e.Message);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Receive error: " + e.Message);
            }
        }
    }

    void OnApplicationQuit()
    {
        StopReceiving();
    }
}