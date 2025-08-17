using System;
using System.Collections.Generic;
using System.Linq;              // LINQ
using System.Globalization;     // CultureInfo
using System.Text;              // StringBuilder
using UnityEngine;
using UnityEngine.UI;           // UI.Text

public class Ranking : MonoBehaviour
{
    [Header("�Q��")]
    [SerializeField] private AsymmetryFeatureCalculator afc;
    [SerializeField] private GameManager gameManager;

    [Header("�ŐV����")]
    [SerializeField] public float[] results = new float[6];
    [SerializeField] public int count = 0;

    // count -> features �̗���
    private readonly Dictionary<int, float[]> resultsByCount = new Dictionary<int, float[]>();

    [Header("UI �\��")]
    [SerializeField] private UnityEngine.UI.Text outputText;  // Canvas �� Text �����蓖��
    [SerializeField] private int maxLines = 10;               // �\�����錏��
    [SerializeField] private bool showSwipeSummary = true;

    [Header("���O�ݒ�")]
    [SerializeField] private bool logResultOnCall = true;

    public enum SortMetric { SumAbs, Sum, Mean, Max, L2 }
    [SerializeField] private SortMetric sortBy = SortMetric.SumAbs;

    private static readonly CultureInfo CI = CultureInfo.InvariantCulture;

    private void Start()
    {
        UpdateOutputUI();
    }

    // �O����UI�{�^������Ă�
    public void Call_result()
    {
        if (afc == null)
        {
            Debug.LogWarning("AsymmetryFeatureCalculator �������蓖�Ăł��B");
            return;
        }

        count++;                                // �擾���i1,2,3,...�j
        var f = afc.GetFeatures();              // 6�v�f�̓����ʂ��擾

        // �R�s�[���ĕێ��i��ŏ㏑������Ă������͕s�ςɁj
        var copy = new float[f.Length];
        Array.Copy(f, copy, f.Length);

        results = copy;                         // �ŐV��ێ�
        resultsByCount[count] = copy;           // ����o�^

        if (logResultOnCall)
        {
            string line = string.Join(", ", copy.Select(x => x.ToString("G4", CI)));
            Debug.Log($"[Result #{count}] [{line}]");
        }

        UpdateOutputUI();                       // ��ʍX�V
    }

    // �����擾�i�K�v�Ȃ�O���ցj
    public IReadOnlyDictionary<int, float[]> GetResultsHistory() => resultsByCount;

    [ContextMenu("Dump Results History")]
    private void DumpResultsHistory()
    {
        if (resultsByCount.Count == 0)
        {
            Debug.Log("���ʗ����͋�ł��B");
            return;
        }
        foreach (var kv in resultsByCount.OrderBy(kv => kv.Key))
        {
            string line = string.Join(", ", kv.Value.Select(v => v.ToString("G4", CI)));
            Debug.Log($"#{kv.Key}: [{line}]");
        }
    }

    // ===== UI �`�� =====
    private void UpdateOutputUI()
    {
        if (outputText == null) return;

        var sb = new StringBuilder(1024);

        // �ŐV����
        sb.AppendLine("=== Latest Result ===");
        if (resultsByCount.Count == 0)
        {
            sb.AppendLine("�܂����ʂ�����܂���B�mCollect/Call_result �����s���Ă��������n");
        }
        else
        {
            int lastKey = resultsByCount.Keys.Max();
            sb.AppendLine($"Count: {lastKey}");
            sb.AppendLine(FormatVector("Features", resultsByCount[lastKey]));
        }
        sb.AppendLine();

        // �����i�X�R�A�~���ŏ�� maxLines ���j
        sb.AppendLine($"=== History by {sortBy} (top {Mathf.Min(maxLines, resultsByCount.Count)}) ===");
        if (resultsByCount.Count > 0)
        {
            var byScore = resultsByCount
                .Select(kv => new { Key = kv.Key, Values = kv.Value, Score = ComputeScore(kv.Value) })
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Key)  // ���_�̏ꍇ�� count ����
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

        // �X���C�v�v��i�C�Ӂj
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
                    if (++shown >= 10) break; // �\���������h�~
                }
            }
        }

        outputText.text = sb.ToString();
    }

    // ���בւ��X�R�A
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
