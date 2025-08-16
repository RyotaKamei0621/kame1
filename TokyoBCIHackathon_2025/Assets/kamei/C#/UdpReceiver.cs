using System;
using System.Collections.Generic;
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
    private float collectionStartTime;
    private bool isCollecting = false;
    private List<float[]> collectedData = new List<float[]>(); // 各行が70要素（float）のデータ

    [Header("EEG 平均値 [6 bands x 8 electrodes]")]
    public float[,] meanValues = new float[6, 8];

    [Header("EEG 分散値 [6 bands x 8 electrodes]")]
    public float[,] varianceValues = new float[6, 8];

    [Header("Debug View (flattened)")]
    public float[] meanValuesFlat = new float[6 * 8];
    public float[] varianceValuesFlat = new float[6 * 8];


    // ボタンに紐づけるメソッド
    public void StartReceiving()
    {
        if (isRunning) return;

        collectedData.Clear();
        udpClient = new UdpClient(port);
        isRunning = true;
        isCollecting = true;
        collectionStartTime = Time.time;

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
        if (isCollecting && Time.time - collectionStartTime >= 2.0f)
        {
            isCollecting = false;
            ProcessCollectedData();
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
                string[] stringValues = ascii.Split(',');

                if (stringValues.Length != 70) continue;

                float[] values = new float[70];
                for (int i = 0; i < 70; i++)
                {
                    if (!float.TryParse(stringValues[i], out values[i]))
                        values[i] = float.NaN;
                }

                if (isCollecting)
                {
                    lock (collectedData)
                        collectedData.Add(values);
                }
            }
            catch { }
        }
    }

    private void ProcessCollectedData()
    {
    int numSamples;
    float[,] sum = new float[6, 8];
    float[,] sumSq = new float[6, 8];

    lock (collectedData)
    {
        numSamples = collectedData.Count;

        foreach (var sample in collectedData)
        {
            for (int band = 0; band < 6; band++)
            {
                for (int ch = 0; ch < 8; ch++)
                {
                    int idx = ch + band * 8; // データインデックスは band×8 + ch
                    float v = sample[idx];
                    if (!float.IsNaN(v))
                    {
                        sum[band, ch] += v;
                        sumSq[band, ch] += v * v;
                    }
                }
            }
        }
    }

    for (int band = 0; band < 6; band++)
    {
        for (int ch = 0; ch < 8; ch++)
        {
            float mean = sum[band, ch] / numSamples;
            float var = (sumSq[band, ch] / numSamples) - (mean * mean);
            meanValues[band, ch] = mean;
            varianceValues[band, ch] = var;

            int idx = band * 8 + ch;
            meanValuesFlat[idx] = mean;
            varianceValuesFlat[idx] = var;
        }
    }

    Debug.Log("✅ 平均・分散（band-row, electrode-column）計算完了");
    }


    void OnApplicationQuit()
    {
        StopReceiving();
    }
}