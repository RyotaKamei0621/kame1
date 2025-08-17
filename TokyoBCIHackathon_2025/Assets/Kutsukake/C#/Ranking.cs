using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class Ranking : MonoBehaviour
{
    [System.Serializable]
    public struct ResultEntry
    {
        public float[] Features;
        public string ImageName;

        public ResultEntry(float[] features, string imageName)
        {
            Features = features;
            ImageName = imageName;
        }
    }

    [Header("計算")]
    [SerializeField] private AsymmetryFeatureCalculator afc;
    [SerializeField] private GameManager gameManager;

    [Header("最新結果")]
    [SerializeField] public float[] results = new float[6];
    [SerializeField] public int count = 0;

    // count -> 結果（特徴量＋画像名）
    private readonly Dictionary<int, ResultEntry> resultsByCount = new Dictionary<int, ResultEntry>();

    [Header("UI 設定")]
    [SerializeField] private UnityEngine.UI.Text outputText;
    [SerializeField] private int maxLines = 10;
    [SerializeField] private bool showSwipeSummary = true;

    [Header("ログ設定")]
    [SerializeField] private bool logResultOnCall = true;

    public enum SortMetric { SumAbs, Sum, Mean, Max, L2 }
    [SerializeField] private SortMetric sortBy = SortMetric.SumAbs;

    private static readonly CultureInfo CI = CultureInfo.InvariantCulture;

    private void Start()
    {
        UpdateOutputUI();
    }

    public void Call_result()
    {
        if (afc == null)
        {
            Debug.LogWarning("AsymmetryFeatureCalculator が設定されていません。");
            return;
        }

        count++;
        var f = afc.GetFeatures();

        var copy = new float[f.Length];
        Array.Copy(f, copy, f.Length);
        results = copy;

        string imageName = gameManager?.GetCurrentImageName() ?? "(unknown)";
        var entry = new ResultEntry(copy, imageName);
        resultsByCount[count] = entry;

        if (logResultOnCall)
        {
            string line = string.Join(", ", copy.Select(x => x.ToString("G4", CI)));
            Debug.Log($"[Result #{count}] name={imageName} [{line}]");
        }

        UpdateOutputUI();
    }

    public IReadOnlyDictionary<int, ResultEntry> GetResultsHistory() => resultsByCount;

    [ContextMenu("Dump Results History")]
    private void DumpResultsHistory()
    {
        if (resultsByCount.Count == 0)
        {
            Debug.Log("結果履歴は空です。");
            return;
        }
        foreach (var kv in resultsByCount.OrderBy(kv => kv.Key))
        {
            string line = string.Join(", ", kv.Value.Features.Select(v => v.ToString("G4", CI)));
            Debug.Log($"#{kv.Key}: name={kv.Value.ImageName} [{line}]");
        }
    }

    private void UpdateOutputUI()
    {
        if (outputText == null) return;

        var sb = new StringBuilder(1024);

        // // === 最新結果 ===
        // sb.AppendLine("=== Latest Result ===");
        // if (resultsByCount.Count == 0)
        // {
        //     sb.AppendLine("まだ結果がありません（Call_result を実行してください）");
        // }
        // else
        // {
        //     int lastKey = resultsByCount.Keys.Max();
        //     var latest = resultsByCount[lastKey];
        //     sb.AppendLine($"Count: {lastKey}");
        //     sb.AppendLine($"Image: {latest.ImageName}");
        //     sb.AppendLine(FormatVector("Features", latest.Features));
        //     sb.AppendLine($"Score: {ComputeScore(latest.Features).ToString("G4", CI)}");
        // }
        // sb.AppendLine();

        // === 履歴 ===
        sb.AppendLine($"=== History by {sortBy} (top {Mathf.Min(maxLines, resultsByCount.Count)}) ===");
        if (resultsByCount.Count > 0)
        {
            var byScore = resultsByCount
                .Select(kv => new
                {
                    Key = kv.Key,
                    Values = kv.Value.Features,
                    Name = kv.Value.ImageName,
                    Score = ComputeScore(kv.Value.Features)
                })
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Key)
                .Take(maxLines);

            foreach (var x in byScore)
            {
                sb.AppendLine($"{x.Key,4}: score={x.Score.ToString("G4", CI)}  name={x.Name}");
            }
        }
        else
        {
            sb.AppendLine("(empty)");
        }

        // === スワイプ情報 ===
        if (showSwipeSummary && gameManager != null)
        {
            var hist = gameManager.GetSwipeHistory();
            if (hist != null && hist.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("=== Swipe Summary ===");

                var stats = hist.GroupBy(r => r.ImageName)
                                .Select(g => new
                                {
                                    Name = g.Key,
                                    Likes = g.Count(x => x.Liked),
                                    Unlikes = g.Count(x => !x.Liked),
                                    FirstShown = g.Min(x => x.AppearanceOrder)
                                })
                                .OrderByDescending(s => s.Likes)
                                .ThenBy(s => s.FirstShown);

                int shown = 0;
                foreach (var s in stats)
                {
                    sb.AppendLine($"{s.Name}: Likes {s.Likes}, Unlikes {s.Unlikes}, First #{s.FirstShown}");
                    if (++shown >= 10) break;
                }
            }
        }

        outputText.text = sb.ToString();
    }

    private float ComputeScore(float[] arr)
    {
        if (arr == null || arr.Length == 0) return 0f;

        switch (sortBy)
        {
            case SortMetric.Sum:
                return arr.Sum();
            case SortMetric.Mean:
                return arr.Average();
            case SortMetric.Max:
                return arr.Max();
            case SortMetric.L2:
                double sum2 = 0;
                for (int i = 0; i < arr.Length; i++)
                    sum2 += (double)arr[i] * arr[i];
                return (float)Math.Sqrt(sum2);
            case SortMetric.SumAbs:
            default:
                return arr.Select(v => Mathf.Abs(v)).Sum();
        }
    }

    private string FormatVector(string title, float[] arr)
    {
        string[] names = { "Delta", "Theta", "Alpha", "B-Low", "B-Mid", "B-High" };
        var parts = arr.Select((v, i) => $"{names[i]}={v.ToString("G4", CI)}");
        return $"{title}: " + string.Join("  ", parts);
    }

    private string FormatArray(float[] arr)
    {
        return "[" + string.Join(", ", arr.Select(v => v.ToString("G4", CI))) + "]";
    }
}
