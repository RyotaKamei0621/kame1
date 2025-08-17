using System;
using System.Collections.Generic;
using System.Linq;              // LINQ (OrderBy, GroupBy ��)
using System.Globalization;     // CultureInfo
using System.Text;
using UnityEngine;
using UnityEngine.UI;           // UI.Text
using static System.Net.Mime.MediaTypeNames;


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
    // �t�B�[���h�錾�������
    [SerializeField] private UnityEngine.UI.Text outputText;

    [SerializeField] private int maxLines = 10;   // �����̕\������
    [SerializeField] private bool showSwipeSummary = true;

    [Header("���O�ݒ�")]
    [SerializeField] private bool logResultOnCall = true;

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

        // �����i�V�������� maxLines ���j
        sb.AppendLine($"=== History (latest {Mathf.Min(maxLines, resultsByCount.Count)} entries) ===");
        if (resultsByCount.Count > 0)
        {
            foreach (var kv in resultsByCount.OrderByDescending(k => k.Key).Take(maxLines))
            {
                sb.AppendLine($"{kv.Key,4}: {FormatArray(kv.Value)}");
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
                                .Select(g => new {
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
