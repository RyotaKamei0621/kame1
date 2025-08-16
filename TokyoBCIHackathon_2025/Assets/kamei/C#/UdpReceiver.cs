using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using System.Collections.Concurrent;
using System;  // ← これが必要です

public class UdpReceiver : MonoBehaviour
{
    private UdpClient udpClient;
    private Thread receiveThread;
    public int port = 5005;  // 任意のポート番号を設定
    private ConcurrentQueue<string> messageQueue = new ConcurrentQueue<string>();  // 受信メッセージ用キュー
    private volatile bool isRunning = false;  // 受信ループ制御用
    void Start()
    {
        udpClient = new UdpClient(port);
        isRunning = true;
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
        Debug.Log("UDP Receiver started on port " + port);
    }

  void Update()
    {

        while (messageQueue.TryDequeue(out string message))
        {
            Debug.Log("Hello");
            Debug.Log("EEG Data (from Update): " + message); // Unityのメインスレッドで出力
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

            // CSVの項目数チェック
            string[] values = ascii.Split(',');
            if (values.Length == 70)
            {
                Debug.Log("✅ EEG データ70項目を受信しました");
            }
            else
            {
                Debug.LogWarning("⚠️ データ項目数 = " + values.Length + "（期待値: 70）");
            }

            messageQueue.Enqueue(ascii);
        }
        catch (SocketException e)
        {
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
        isRunning = false;
        udpClient.Close();  // Receive()が解除される
        receiveThread?.Join();  // 安全にスレッド終了を待つ
    }
}
