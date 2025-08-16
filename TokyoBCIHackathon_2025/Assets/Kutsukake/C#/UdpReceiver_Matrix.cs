using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using System.Collections.Concurrent;


public class UdpReceiver_Matrix : MonoBehaviour
{
    private UdpClient udpClient;
    private Thread receiveThread;
    public int port = 5005;
    private volatile bool isRunning = false;

    // ▼ Inspectorで確認できる最新データ (6×8)
    public float[,] latestMatrix = new float[6, 8];

    // 受信スレッドとメインスレッドで共有する安全なキュー
    private ConcurrentQueue<float[,]> matrixQueue = new ConcurrentQueue<float[,]>();

    public void StartReceiving()
    {
        if (isRunning) return;

        udpClient = new UdpClient(port);
        isRunning = true;
        receiveThread = new Thread(ReceiveData);
        receiveThread.IsBackground = true;
        receiveThread.Start();
        Debug.Log("▶ UDP受信開始 (ポート: " + port + ")");
    }

    public void StopReceiving()
    {
        if (!isRunning) return;
        isRunning = false;
        udpClient.Close();
        receiveThread?.Join();
        Debug.Log("■ UDP受信停止");
    }

    void Update()
    {
        // 毎フレームに1つだけ取り出す
        if (matrixQueue.TryDequeue(out float[,] matrix))
        {
            latestMatrix = matrix;

            // Debug 出力例（1行目だけ）
            string row0 = "";
            for (int j = 0; j < 8; j++) row0 += matrix[0, j].ToString("F2") + " ";
            Debug.Log("最新Matrix Row0: " + row0);
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
                string ascii = Encoding.ASCII.GetString(data);
                string[] values = ascii.Split(',');

                // 必要数あるか確認（6×8=48）
                if (values.Length >= 48)
                {
                    float[,] matrix = new float[6, 8];
                    for (int i = 0; i < 6; i++)
                    {
                        for (int j = 0; j < 8; j++)
                        {
                            int idx = i * 8 + j;
                            if (float.TryParse(values[idx], out float val))
                                matrix[i, j] = val;
                            else
                                matrix[i, j] = float.NaN;
                        }
                    }
                    matrixQueue.Enqueue(matrix);
                }
                else
                {
                    Debug.LogWarning("⚠️ データ不足: " + values.Length);
                }
            }
            catch (Exception e)
            {
                if (isRunning)
                    Debug.LogError("Receive error: " + e.Message);
            }
        }
    }

    void OnApplicationQuit()
    {
        StopReceiving();
    }
}
