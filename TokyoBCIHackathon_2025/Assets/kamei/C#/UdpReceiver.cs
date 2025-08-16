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

    [Header("Debug View (flattened)")]
    public float[] meanValuesFlat = new float[8 * 6];
    public float[] varianceValuesFlat = new float[8 * 6];

    [Header("EEG 平均値 [8 electrodes x 6 bands]")]
    public float[,] meanValues = new float[8, 6];

    [Header("EEG 分散値 [8 electrodes x 6 bands]")]
    public float[,] varianceValues = new float[8, 6];

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
        float[,] sum = new float[8, 6];
        float[,] sumSq = new float[8, 6];

        lock (collectedData)
        {
            numSamples = collectedData.Count;

            foreach (var sample in collectedData)
            {
                for (int ch = 0; ch < 8; ch++)
{
    for (int band = 0; band < 6; band++)
    {
        float mean = sum[ch, band] / numSamples;
        float var = (sumSq[ch, band] / numSamples) - (mean * mean);
        meanValues[ch, band] = mean;
        varianceValues[ch, band] = var;

        int idx = ch * 6 + band;
        meanValuesFlat[idx] = mean;
        varianceValuesFlat[idx] = var;
    }
}

            }
        }

        for (int ch = 0; ch < 8; ch++)
        {
            for (int band = 0; band < 6; band++)
            {
                float mean = sum[ch, band] / numSamples;
                float var = (sumSq[ch, band] / numSamples) - (mean * mean);
                meanValues[ch, band] = mean;
                varianceValues[ch, band] = var;
            }
        }

        Debug.Log("✅ 平均・分散を計算完了しました");
    }

    void OnApplicationQuit()
    {
        StopReceiving();
    }
}