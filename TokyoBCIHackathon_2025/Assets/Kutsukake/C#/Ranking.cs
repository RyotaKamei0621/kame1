using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;   // ← コルーチン用

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

    [Header("テキスト出力(従来)")]
    [SerializeField] private Text outputText;
    [SerializeField] private int maxLines = 10;
    [SerializeField] private bool showSwipeSummary = true;

    [Header("ログ設定")]
    [SerializeField] private bool logResultOnCall = true;

    public enum SortMetric { SumAbs, Sum, Mean, Max, L2 }
    [SerializeField] private SortMetric sortBy = SortMetric.SumAbs;

    private static readonly CultureInfo CI = CultureInfo.InvariantCulture;

    // ====== ここからUIリッチ表示のための追加 ======
    [Header("Rich UI: ランキング表示")]
    [SerializeField] private RectTransform rankingField;        // RankingField（親）
    [SerializeField] private RectTransform[] rankSlots;         // Rank1..Rank5 の Transform をサイズ5で割り当て
    [SerializeField] private GameObject rankingItemPrefab;      // 「Ranking Panel」のPrefab
    [SerializeField] private Sprite[] rankBadgeSprites;         // 1〜5位用のバッジ画像（サイズ5）

    [SerializeField] private float slideDuration = 0.45f;
    [SerializeField] private float slideStagger   = 0.10f;      // 順位ごとの遅延(5位→1位へ)

    // 生成した項目を覚えておく（再描画時に破棄）
    private readonly List<GameObject> spawnedItems = new List<GameObject>();
    // ====== 追加ここまで ======

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

        UpdateOutputUI();     // 旧テキスト
        RenderTop5RichUI();   // 新リッチUI
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

    // ====== 旧：テキスト出力（必要なら残す） ======
    private void UpdateOutputUI()
    {
        if (outputText == null) return;

        var sb = new StringBuilder(1024);

        sb.AppendLine($"=== History by {sortBy} (top {Mathf.Min(maxLines, resultsByCount.Count)}) ===");
        if (resultsByCount.Count > 0)
        {
            // Likesの辞書（SwipeHistoryから集計）
            Dictionary<string, int> likeCountDict = null;
            if (gameManager != null)
            {
                var hist = gameManager.GetSwipeHistory();
                if (hist != null)
                {
                    likeCountDict = hist
                        .Where(h => h.Liked)
                        .GroupBy(h => h.ImageName)
                        .ToDictionary(g => g.Key, g => g.Count());
                }
            }

            var byScore = resultsByCount
                .Select(kv => new
                {
                    Key   = kv.Key,
                    Name  = kv.Value.ImageName,
                    Score = ComputeScore(kv.Value.Features)
                })
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Key)
                .Take(maxLines);

            foreach (var x in byScore)
            {
                int likes = 0;
                if (likeCountDict != null) likeCountDict.TryGetValue(x.Name, out likes);
                sb.AppendLine($"{x.Key,4}: score={x.Score.ToString("G4", CI)}  name={x.Name}  Likes={likes}");
            }
        }
        else
        {
            sb.AppendLine("(empty)");
        }

        outputText.text = sb.ToString();
    }
    // ====== 旧ここまで ======

    // ====== 新：Rich UI描画（Top5を右→左にスライド） ======
    private void RenderTop5RichUI()
    {
        if (rankingField == null || rankingItemPrefab == null || rankSlots == null || rankSlots.Length < 5)
            return;
        if (resultsByCount.Count == 0) return;

        // 既存アイテムを破棄
        foreach (var go in spawnedItems) if (go) Destroy(go);
        spawnedItems.Clear();

        // Likesを集計（有無表示/件数に使える）
        var likeDict = BuildLikeCountDict();

        // スコアTop5（降順）
        var top5 = resultsByCount
            .Select(kv => new
            {
                Name  = kv.Value.ImageName,
                Score = ComputeScore(kv.Value.Features),
                Sprite = GetSpriteByName(kv.Value.ImageName)
            })
            .GroupBy(x => x.Name) // 同一画像が複数回ある場合はベストスコアにしたいなら .Max()
            .Select(g => g.OrderByDescending(v => v.Score).First()) // 最高スコアを代表に
            .OrderByDescending(x => x.Score)
            .Take(5)
            .ToList();

        // 右外からスライドインする開始位置（Canvas外のX）
        float offscreenX = rankingField.rect.width * 1.2f;

        // 5位(末尾)から1位(先頭)へ順番に出す
        for (int i = top5.Count - 1, rank = 5; i >= 0; i--, rank--)
        {
            var data = top5[i];
            var slot = rankSlots[rank - 1]; // Rank1→index0, Rank5→index4 の前提

            var go = Instantiate(rankingItemPrefab, rankingField);
            spawnedItems.Add(go);

            // 子参照
            var rt = go.GetComponent<RectTransform>();
            var rankImg   = go.transform.Find("RankImage")   ?.GetComponent<Image>();
            var personImg = go.transform.Find("PersonImage") ?.GetComponent<Image>();
            var nameText  = go.transform.Find("NameText")    ?.GetComponent<Text>();
            var detailTxt = go.transform.Find("DetailText")  ?.GetComponent<Text>();

            // ランクバッジ画像
            if (rankImg != null && rankBadgeSprites != null && rankBadgeSprites.Length >= rank)
                rankImg.sprite = rankBadgeSprites[rank - 1];

            // 人物画像
            if (personImg != null)
                personImg.sprite = data.Sprite;

            // 名前
            if (nameText != null)
                nameText.text = data.Name;

            // 詳細（Score と Likeの有無/件数）
            if (detailTxt != null)
            {
                int likes = 0; likeDict.TryGetValue(data.Name, out likes);
                string likeStr = likes > 0 ? $"♥ {likes}" : "No Like";
                detailTxt.text = $"Score: {data.Score.ToString("G4", CI)}   {likeStr}";
            }

            // 初期位置：右外
            var to = (Vector2)slot.anchoredPosition;
            var from = new Vector2(offscreenX, to.y);
            rt.anchoredPosition = from;

            // アニメ（順位が低いほど先に出す = Rank5→Rank1）
            float delay = slideStagger * (5 - rank);
            StartCoroutine(SlideIn(rt, from, to, slideDuration, delay));
        }
    }

    private IEnumerator SlideIn(RectTransform rt, Vector2 from, Vector2 to, float duration, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // ポーズ中も動かしたいなら unscaled
            float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            rt.anchoredPosition = Vector2.LerpUnclamped(from, to, u);
            yield return null;
        }
        rt.anchoredPosition = to;
    }
    // ====== 新ここまで ======

    private float ComputeScore(float[] arr)
    {
        if (arr == null || arr.Length == 0) return 0f;

        switch (sortBy)
        {
            case SortMetric.Sum:  return arr.Sum();
            case SortMetric.Mean: return arr.Average();
            case SortMetric.Max:  return arr.Max();
            case SortMetric.L2:
                double sum2 = 0;
                for (int i = 0; i < arr.Length; i++) sum2 += (double)arr[i] * arr[i];
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

    // 画像名→Sprite 取得（GameManager から）
    private Sprite GetSpriteByName(string name)
    {
        if (gameManager == null) return null;
        var list = gameManager.GetAllCardSprites();  // GameManager に用意（下記参照）
        if (list == null) return null;
        for (int i = 0; i < list.Count; i++)
            if (list[i] != null && list[i].name == name)
                return list[i];
        return null;
    }

    // Like件数の辞書（画像名→数）
    private Dictionary<string, int> BuildLikeCountDict()
    {
        var dict = new Dictionary<string, int>();
        if (gameManager == null) return dict;
        var hist = gameManager.GetSwipeHistory();
        if (hist == null) return dict;

        foreach (var g in hist.Where(h => h.Liked).GroupBy(h => h.ImageName))
            dict[g.Key] = g.Count();

        return dict;
    }
}
