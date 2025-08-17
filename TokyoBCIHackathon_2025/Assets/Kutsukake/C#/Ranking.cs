using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;              // �� �ǉ��FLastOrDefault, GroupBy, OrderBy �ȂǂŕK�v
using System.Globalization;     // �i�C�Ӂj���O�̐��l�t�H�[�}�b�g�Ɏg�p
using UnityEngine;


public class Ranking : MonoBehaviour
{
    [SerializeField] private AsymmetryFeatureCalculator afc;
    [SerializeField] public float[] results = new float[6];
    [SerializeField] public int count = 0;
    [SerializeField] private GameManager gameManager;

    // �� �ǉ��Fcount �� results �̗�����ێ�
    private readonly Dictionary<int, float[]> resultsByCount = new Dictionary<int, float[]>();

    [Header("���O�ݒ�")]
    [SerializeField] private bool logResultOnCall = true;

    void Start()
    {
        // ---- Swipe�����̗�i���������̂܂܁A����Ȃ� using ��ǉ��j ----
        var hist = gameManager.GetSwipeHistory();

        // ���߂�1��
        var last = hist.LastOrDefault();
        if (last != null)
            Debug.Log($"�Ō�̋L�^: #{last.AppearanceOrder} {last.ImageName} {(last.Liked ? "Like" : "Unlike")}");

        // �S���^�C�����C���i�\�����Ń\�[�g�j
        foreach (var r in hist.OrderBy(r => r.AppearanceOrder))
            Debug.Log($"#{r.AppearanceOrder}  {r.ImageName}  {(r.Liked ? "Like" : "Unlike")}");

        // �摜���Ƃ�Like/Unlike�W�v
        var stats = hist.GroupBy(r => r.ImageName)
                        .Select(g => new {
                            Name = g.Key,
                            Likes = g.Count(x => x.Liked),
                            Unlikes = g.Count(x => !x.Liked),
                            FirstShown = g.Min(x => x.AppearanceOrder)
                        })
                        .OrderByDescending(s => s.Likes);

        foreach (var s in stats)
            Debug.Log($"{s.Name}: Likes {s.Likes}, Unlikes {s.Unlikes}, ����\���� #{s.FirstShown}");
    }

    // �� �O����UI�{�^������Ăт����Ȃ� public �ɂ���OK
    public void Call_result()
    {
        count++;                               // �擾���i1,2,3,...�j
        var f = afc.GetFeatures();             // 6�v�f�̓����ʂ��擾

        // �� �R�s�[���ĕێ��i��� results ���㏑������Ă������̒��g�͕s�ςɁj
        var copy = new float[f.Length];
        Array.Copy(f, copy, f.Length);

        results = copy;                        // �ŐV�̌��ʂ��t�B�[���h�ɂ����f
        resultsByCount[count] = copy;          // �����֓o�^

        if (logResultOnCall)
        {
            string line = string.Join(", ", copy.Select(x => x.ToString("G4", CultureInfo.InvariantCulture)));
            Debug.Log($"[Result #{count}] [{line}]");
        }
    }

    // �� �����̎擾�i�O��������S�ɎQ�Ƃ������Ƃ��j
    public IReadOnlyDictionary<int, float[]> GetResultsHistory()
    {
        return resultsByCount;
    }

    // �i�C�Ӂj�S���������O�o��
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
            string line = string.Join(", ", kv.Value.Select(x => x.ToString("G4", CultureInfo.InvariantCulture)));
            Debug.Log($"#{kv.Key}: [{line}]");
        }
    }
}
