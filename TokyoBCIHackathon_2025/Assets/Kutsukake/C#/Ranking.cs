using System;
using System.Collections.Generic;
using System.Linq;              // LINQ
using System.Globalization;     // CultureInfo
using System.Text;              // StringBuilder
using UnityEngine;
using UnityEngine.UI;           // UI.Text

public class Ranking : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private AsymmetryFeatureCalculator afc;
    [SerializeField] private GameManager gameManager;

    [Header("最新結果")]
    [SerializeField] public float[] results = new float[6];
    [SerializeField] public int count = 0;

    // count -> features の履歴
    private readonly Dictionary<int, float[]> resultsByCount = new Dictionary<int, float[]>();

    [Header("UI 表示")]
    [SerializeField] private UnityEngine.UI.Text outputText;  // Canvas の Text を割り当て
    [SerializeField] private int maxLines = 10;               // 表示する件数
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

    // 外部やUIボタンから呼ぶ
    public void Call_result()
    {
        if (afc == null)
        {
            Debug.LogWarning("AsymmetryFeatureCalculator が未割り当てです。");
            return;
        }

        count++;                                // 取得順（1,2,3,...）
        var f = afc.GetFeatures();              // 6要素の特徴量を取得

        // コピーして保持（後で上書きされても履歴は不変に）
        var copy = new float[f.Length];
        Array.Copy(f, copy, f.Length);

        results = copy;                         // 最新を保持
        resultsByCount[count] = copy;           // 履歴登録

        if (logResultOnCall)
        {
            string line = string.Join(", ", copy.Select(x => x.ToString("G4", CI)));
            Debug.Log($"[Result #{count}] [{line}]");
        }

        UpdateOutputUI();                       // 画面更新
    }

    // 履歴取得（必要なら外部へ）
    public IReadOnlyDictionary<int, float[]> GetResultsHistory() => resultsByCount;

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
            string line = string.Join(", ", kv.Value.Select(v => v.ToString("G4", CI)));
            Debug.Log($"#{kv.Key}: [{line}]");
        }
    }

    // ===== UI 描画 =====
    private void UpdateOutputUI()
    {
        if (outputText == null) return;

        var sb = new StringBuilder(1024);

        // 最新結果
        sb.AppendLine("=== Latest Result ===");
        if (resultsByCount.Count == 0)
        {
            sb.AppendLine("まだ結果がありません。［Collect/Call_result を実行してください］");
        }
        else
        {
            int lastKey = resultsByCount.Keys.Max();
            sb.AppendLine($"Count: {lastKey}");
            sb.AppendLine(FormatVector("Features", resultsByCount[lastKey]));
        }
        sb.AppendLine();

        // 履歴（スコア降順で上位 maxLines 件）
        sb.AppendLine($"=== History by {sortBy} (top {Mathf.Min(maxLines, resultsByCount.Count)}) ===");
        if (resultsByCount.Count > 0)
        {
            var byScore = resultsByCount
                .Select(kv => new { Key = kv.Key, Values = kv.Value, Score = ComputeScore(kv.Value) })
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Key)  // 同点の場合は count 昇順
                .Take(maxLines);

            foreach (var x in byScore)
            {
                sb.AppendLine($"{x.Key,4}: score={x.Score.ToString("G4", CI)}  {FormatArray(x.Values)}");
            }
        }
        else
        {
            sb.AppendLine("(empty)");
        }

        // スワイプ要約（任意）
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
                    if (++shown >= 10) break; // 表示しすぎ防止
                }
            }
        }

        outputText.text = sb.ToString();
    }

    // 並べ替えスコア
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
