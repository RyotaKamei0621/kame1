using System;
using System.Collections.Generic;
using UnityEngine;

public class AsymmetryFeatureCalculator : MonoBehaviour
{

    [Header("Zスコアの参照元")]
    public ZScoreProcessor zScoreProcessor;

    [Header("出力結果: 各周波数帯における比率")]
    [Tooltip("Delta, Theta, Alpha, BetaLow, BetaMid, BetaHigh の順")]
    [SerializeField] public float[] results = new float[6];

    [ContextMenu("計算を実行")]
    public void ComputeLateralizationRatios()
    {
        if (zScoreProcessor == null || zScoreProcessor.zScores == null || zScoreProcessor.zScores.Length != 48)
        {
            Debug.LogError("Zスコアの参照が不正です。");
            return;
        }

        for (int band = 0; band < 6; band++)
        {
            int ch2 = band * 8 + 1;
            int ch4 = band * 8 + 3;
            int ch6 = band * 8 + 5;
            int ch8 = band * 8 + 7;

            float leftRightDiffSum = Mathf.Abs(zScoreProcessor.zScores[ch2] - zScoreProcessor.zScores[ch4])
                                   + Mathf.Abs(zScoreProcessor.zScores[ch6] - zScoreProcessor.zScores[ch8]);

            // ch7の zスコア取得: β高 / α
            int ch7_betaHigh = 5 * 8 + 6;  // β高のch7インデックス
            int ch7_alpha    = 2 * 8 + 6;  // αのch7インデックス

            float betaHighZ = zScoreProcessor.zScores[ch7_betaHigh];
            float alphaZ    = zScoreProcessor.zScores[ch7_alpha];

            float ratio = (Mathf.Abs(alphaZ) > 1e-6f) ? (betaHighZ / alphaZ) : 0f;

            // 最終的な指標（分母が0にならないようチェック）
            results[band] = (Mathf.Abs(ratio) > 1e-6f) ? (leftRightDiffSum / ratio) : 0f;
        }

        Debug.Log("✅ ラテラリゼーション指標計算完了");
    }


        public float[] GetFeatures()
        {
            return results;
        }
}
