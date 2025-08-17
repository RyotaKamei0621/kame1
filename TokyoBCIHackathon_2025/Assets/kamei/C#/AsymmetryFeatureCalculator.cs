using System;
using System.Collections.Generic;
using UnityEngine;

public class AsymmetryFeatureCalculator : MonoBehaviour
{
    [Header("Zスコアデータ")]
    [SerializeField] private float[,] zScores; // [channel, band] → [0–7, 0–5]
    [SerializeField] private float ch7RatioZScore; // ← 単一の値に修正

    [Header("計算された特徴量 (Inspector表示)")]
    [SerializeField] private float[] asymmetryFeatures = new float[6]; // 6バンド分

    private readonly string[] bandNames = { "Delta", "Theta", "Alpha", "BetaLow", "BetaMid", "BetaHigh" };

    public void CalculateAsymmetryFeatures()
    {
        if (zScores == null || zScores.GetLength(0) < 8 || zScores.GetLength(1) < 6)
        {
            Debug.LogError("zScoresのサイズが不正です。電極8×周波数帯6の形式にしてください。");
            return;
        }

        if (Mathf.Abs(ch7RatioZScore) < 1e-6f)
        {
            Debug.LogWarning("Ch7比 (高β/α) のZスコアが0に近いため、0.0001に補正します。");
            ch7RatioZScore = 0.0001f;
        }

        for (int i = 0; i < 6; i++)
        {
            float diffLeftRight = Mathf.Abs((zScores[1, i] - zScores[3, i]) + (zScores[5, i] - zScores[7, i]));
            asymmetryFeatures[i] = diffLeftRight * ch7RatioZScore;
        }

        Debug.Log("✅ 非対称性特徴量を更新しました。");
    }


        public float[] GetFeatures()
        {
            return asymmetryFeatures;
        }
}
