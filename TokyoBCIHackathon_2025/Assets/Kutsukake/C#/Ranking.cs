using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;              // ★ 追加：LastOrDefault, GroupBy, OrderBy などで必要
using System.Globalization;     // （任意）ログの数値フォーマットに使用
using UnityEngine;


public class Ranking : MonoBehaviour
{
    [SerializeField] private AsymmetryFeatureCalculator afc;
    [SerializeField] public float[] results = new float[6];
    [SerializeField] public int count = 0;
    [SerializeField] private GameManager gameManager;

    // ★ 追加：count → results の履歴を保持
    private readonly Dictionary<int, float[]> resultsByCount = new Dictionary<int, float[]>();

    [Header("ログ設定")]
    [SerializeField] private bool logResultOnCall = true;

    void Start()
    {
        // ---- Swipe履歴の例（既存処理のまま、足りない using を追加） ----
        var hist = gameManager.GetSwipeHistory();

        // 直近の1件
        var last = hist.LastOrDefault();
        if (last != null)
            Debug.Log($"最後の記録: #{last.AppearanceOrder} {last.ImageName} {(last.Liked ? "Like" : "Unlike")}");

        // 全件タイムライン（表示順でソート）
        foreach (var r in hist.OrderBy(r => r.AppearanceOrder))
            Debug.Log($"#{r.AppearanceOrder}  {r.ImageName}  {(r.Liked ? "Like" : "Unlike")}");

        // 画像ごとのLike/Unlike集計
        var stats = hist.GroupBy(r => r.ImageName)
                        .Select(g => new {
                            Name = g.Key,
                            Likes = g.Count(x => x.Liked),
                            Unlikes = g.Count(x => !x.Liked),
                            FirstShown = g.Min(x => x.AppearanceOrder)
                        })
                        .OrderByDescending(s => s.Likes);

        foreach (var s in stats)
            Debug.Log($"{s.Name}: Likes {s.Likes}, Unlikes {s.Unlikes}, 初回表示順 #{s.FirstShown}");
    }

    // ★ 外部やUIボタンから呼びたいなら public にしてOK
    public void Call_result()
    {
        count++;                               // 取得順（1,2,3,...）
        var f = afc.GetFeatures();             // 6要素の特徴量を取得

        // ★ コピーして保持（後で results が上書きされても辞書の中身は不変に）
        var copy = new float[f.Length];
        Array.Copy(f, copy, f.Length);

        results = copy;                        // 最新の結果をフィールドにも反映
        resultsByCount[count] = copy;          // 履歴へ登録

        if (logResultOnCall)
        {
            string line = string.Join(", ", copy.Select(x => x.ToString("G4", CultureInfo.InvariantCulture)));
            Debug.Log($"[Result #{count}] [{line}]");
        }
    }

    // ★ 履歴の取得（外部から安全に参照したいとき）
    public IReadOnlyDictionary<int, float[]> GetResultsHistory()
    {
        return resultsByCount;
    }

    // （任意）全履歴をログ出力
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
            string line = string.Join(", ", kv.Value.Select(x => x.ToString("G4", CultureInfo.InvariantCulture)));
            Debug.Log($"#{kv.Key}: [{line}]");
        }
    }
}
