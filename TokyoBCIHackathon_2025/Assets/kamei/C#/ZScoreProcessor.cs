using System;
using System.IO;
using UnityEngine;

public class ZScoreProcessor : MonoBehaviour
{
    [Header("参照元")]
    public UdpReceiver udpReceiver;
    public AsymmetryFeatureCalculator ae;
    [Header("Zスコア（6 bands × 8 electrodes = 48）")]
    public float[] zScores = new float[6 * 8];

    public void LoadAndComputeZScores()
    {
        // このスクリプトのあるフォルダを基準にする
        string scriptFolder = Path.GetDirectoryName(Application.dataPath + "/Kutsukake/C#");
        string dir = Path.Combine(scriptFolder, "CSV");
        Directory.CreateDirectory(dir); // 無ければ作成

        string csvPath = Path.Combine(dir, "bandpower_stats.csv");

        if (!File.Exists(csvPath))
        {
            Debug.LogError($"CSVファイルが見つかりません: {csvPath}");
            return;
        }

        try
        {
            string[] lines = File.ReadAllLines(csvPath);
            if (lines.Length < 2)
            {
                Debug.LogError("CSVに十分な行がありません。");
                return;
            }

            // 最新行を使う（ヘッダーは lines[0]）
            string[] latest = lines[lines.Length - 1].Split(',');

            // mean: index 2〜49, var: 50〜97
            float[] csvMeans = new float[48];
            float[] csvVars = new float[48];

            for (int i = 0; i < 48; i++)
            {
                csvMeans[i] = float.Parse(latest[2 + i]);
                csvVars[i] = float.Parse(latest[2 + 48 + i]);
            }

            for (int i = 0; i < 48; i++)
            {
                float x = udpReceiver.meanValuesFlat[i];
                float mean = csvMeans[i];
                float std = Mathf.Sqrt(csvVars[i]);
                zScores[i] = (std > 1e-6f) ? (x - mean) / std : 0f;
            }

            Debug.Log("✅ Zスコア計算完了（横持ちCSV対応）");

            ae.ComputeLateralizationRatios();
        }
        catch (Exception ex)
        {
            Debug.LogError("CSVの読み取り中にエラーが発生しました: " + ex.Message);
        }
    }

  
}
