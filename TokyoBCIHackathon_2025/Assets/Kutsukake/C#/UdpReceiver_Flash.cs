using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using System.Diagnostics; // Stopwatch
using Debug = UnityEngine.Debug;   // ← 追加
using Application = UnityEngine.Application;

using System.IO;         // CSV書き込み
using UnityEngine;

public class UdpReceiver_Flash : MonoBehaviour
{
    private UdpClient udpClient;
    private Thread receiveThread;
    public int port = 5005;

    // === 直近2秒リングバッファ ===
    private struct Sample
    {
        public float t;          // 受信時の経過秒（Stopwatch基準）
        public float[] values;   // 長さ70
    }
    private readonly object bufferLock = new object();
    private readonly Queue<Sample> buffer2s = new Queue<Sample>();
    private Stopwatch sw;                 // 経過時間源
    public float windowSeconds = 2.0f;    // 保持ウィンドウ秒

    private volatile bool isRunning = false;

    // === 結果のInspector表示用 ===
    [Header("EEG 平均値 [6 bands x 8 electrodes]")]
    public float[,] meanValues = new float[6, 8];

    [Header("EEG 分散値 [6 bands x 8 electrodes]")]
    public float[,] varianceValues = new float[6, 8];

    [Header("Debug View (flattened)")]
    public float[] meanValuesFlat = new float[6 * 8];
    public float[] varianceValuesFlat = new float[6 * 8];

    [Header("CSV 設定")]
    public string csvFileName = "bandpower_stats.csv";  // persistentDataPath配下
    public bool useUnbiasedVariance = false;            // 不偏分散(N-1)にするならtrue

    // ---------------------- 制御 ----------------------
    public void StartReceiving()
    {
        // StartReceiving() の中
        string dir = Path.Combine(Application.persistentDataPath, "CSV");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, csvFileName);
        if (File.Exists(path))
        {
            Debug.LogWarning("⚠ 既存のCSVファイルが見つかりました。起動時にクリアします: " + path);
            File.Delete(path);   // 起動時にクリア

        } 
        if (isRunning) return;

        lock (bufferLock) buffer2s.Clear();

        udpClient = new UdpClient(port);
        isRunning = true;

        sw = new Stopwatch();
        sw.Start();

        receiveThread = new Thread(ReceiveData) { IsBackground = true };
        receiveThread.Start();

        Debug.Log("▶ UDP受信開始 (ポート: " + port + ")");
    }

    public void StopReceiving()
    {
        if (!isRunning) return;
        isRunning = false;

        try { udpClient?.Close(); } catch { }
        try { receiveThread?.Join(); } catch { }
        try { sw?.Stop(); } catch { }

        Debug.Log("■ UDP受信停止");
    }

    void OnApplicationQuit()
    {
        StopReceiving();
    }

    // ---------------------- 受信スレッド ----------------------
    private void ReceiveData()
    {
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, port);

        while (isRunning)
        {
            try
            {
                byte[] data = udpClient.Receive(ref remoteEndPoint);
                string ascii = Encoding.ASCII.GetString(data);
                string[] tokens = ascii.Split(',');

                if (tokens.Length != 70) continue;

                float[] values = new float[70];
                for (int i = 0; i < 70; i++)
                {
                    if (!float.TryParse(tokens[i], out values[i]))
                        values[i] = float.NaN;
                }

                float t = (float)sw.Elapsed.TotalSeconds;
                var s = new Sample { t = t, values = values };

                // 追加＆古いものを間引いて2秒維持
                lock (bufferLock)
                {
                    buffer2s.Enqueue(s);
                    float cutoff = t - windowSeconds;
                    while (buffer2s.Count > 0 && buffer2s.Peek().t < cutoff)
                        buffer2s.Dequeue();
                }
            }
            catch { /* 無言で継続 */ }
        }
    }

    // ---------------------- スナップショット ＋ CSV追記 ----------------------
    // UIボタンから呼ぶ
    public void SaveWindowStatsToCsv()
    {
        // 直近2秒のスナップショットをコピー（ロック時間短縮）
        Sample[] snapshot;
        float nowSec = (float)sw.Elapsed.TotalSeconds;
        float cutoff = nowSec - windowSeconds;

        lock (bufferLock)
        {
            if (buffer2s.Count == 0)
            {
                Debug.LogWarning("⚠ 直近2秒のバッファが空です。受信があるか確認してください。");
                return;
            }

            List<Sample> tmp = new List<Sample>(buffer2s.Count);
            foreach (var s in buffer2s)
            {
                if (s.t >= cutoff) tmp.Add(s);
            }
            snapshot = tmp.ToArray();
        }

        int N = snapshot.Length;
        if (N == 0)
        {
            Debug.LogWarning("⚠ 直近2秒に有効サンプルがありません。");
            return;
        }

        // 6x8だけ集計（先頭48列をBandPowerと仮定）
        float[,] sum = new float[6, 8];
        float[,] sumSq = new float[6, 8];
        int[,] count = new int[6, 8];

        for (int n = 0; n < N; n++)
        {
            var vals = snapshot[n].values;
            for (int band = 0; band < 6; band++)
            {
                for (int ch = 0; ch < 8; ch++)
                {
                    int idx = band * 8 + ch; // 0..47 をBandPowerと想定
                    float v = vals[idx];
                    if (!float.IsNaN(v))
                    {
                        sum[band, ch] += v;
                        sumSq[band, ch] += v * v;
                        count[band, ch] += 1;
                    }
                }
            }
        }

        // 平均・分散を計算し、Inspector配列にも反映
        for (int band = 0; band < 6; band++)
        {
            for (int ch = 0; ch < 8; ch++)
            {
                int c = Mathf.Max(count[band, ch], 1); // 0割防止
                float mean = sum[band, ch] / c;

                // 母分散 or 不偏分散
                float var;
                float meanSq = sumSq[band, ch] / c;
                float popVar = Mathf.Max(0f, meanSq - mean * mean);

                if (useUnbiasedVariance && c > 1)
                {
                    // s^2 = Σ(x-μ)^2 / (N-1) = N/(N-1) * popVar
                    var = popVar * (c / (float)(c - 1));
                }
                else
                {
                    var = popVar;
                }

                meanValues[band, ch] = mean;
                varianceValues[band, ch] = var;

                int flat = band * 8 + ch;
                meanValuesFlat[flat] = mean;
                varianceValuesFlat[flat] = var;
            }
        }

        // CSVに追記
        try
        {
            // このスクリプトのあるフォルダを基準にする
            string scriptFolder = Path.GetDirectoryName(Application.dataPath + "/Kutsukake/C#");
            string dir = Path.Combine(scriptFolder, "CSV");
            Directory.CreateDirectory(dir); // 無ければ作成

            string path = Path.Combine(dir, csvFileName);

            bool writeHeader = !File.Exists(path);

            using (var swr = new StreamWriter(path, append: true, Encoding.UTF8))
            {
                if (writeHeader)
                {
                    List<string> header = new List<string> { "time_sec", "samples_in_window" };
                    for (int b = 0; b < 6; b++)
                        for (int c = 0; c < 8; c++)
                            header.Add($"mean_b{b}_ch{c}");
                    for (int b = 0; b < 6; b++)
                        for (int c = 0; c < 8; c++)
                            header.Add($"var_b{b}_ch{c}");
                    swr.WriteLine(string.Join(",", header));
                }

                List<string> row = new List<string> { nowSec.ToString("F3"), N.ToString() };
                for (int i = 0; i < 48; i++) row.Add(meanValuesFlat[i].ToString("G9"));
                for (int i = 0; i < 48; i++) row.Add(varianceValuesFlat[i].ToString("G9"));

                swr.WriteLine(string.Join(",", row));
            }

            Debug.Log($"✅ CSV保存完了: {path}");
        }
        catch (Exception e)
        {
            Debug.LogError("CSV書き込みエラー: " + e.Message);
        }

    }
}