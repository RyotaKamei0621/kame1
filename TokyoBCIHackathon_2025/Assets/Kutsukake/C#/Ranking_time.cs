using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Liked（いいね）された画像だけを対象に、判断にかかった時間（DecisionTimeSec）が短い順でランキング表示するコンポーネント。
/// 
/// 使い方：
/// 1) 本スクリプトを空の GameObject に追加
/// 2) Output Text に UI.Text をアサイン
/// 3) History Provider に、ISwipeHistoryProvider を実装したコンポーネント（例：GameManager）をアサイン
///    （例）public class GameManager : MonoBehaviour, Ranking_time.ISwipeHistoryProvider { ... }
/// 4) Button などから RefreshRanking() を呼べば、ランキングが更新されます。
/// 
/// メモ：同着のタイブレークは (a) AppearanceOrder が小さい（早く出た）→ (b) ImageName の辞書順 です。
/// groupByImageON の場合は、同一画像が複数回記録されていても「最短の DecisionTimeSec の 1件のみ」を代表として採用します。
/// </summary>
public class Ranking_time : MonoBehaviour
{
    // ====== 公開インタフェース（GameManager 等がこれを実装） ======
    public interface ISwipeHistoryProvider
    {
        /// <summary>履歴のスナップショットを返してください。</summary>
        List<SwipeHistoryItem> GetSwipeHistory();
    }

    [Serializable]
    public class SwipeHistoryItem
    {
        public string ImageName;        // 画像ファイル名やキー
        public bool Liked;              // いいね判定
        public int AppearanceOrder;     // その画像が最初に表示された順序（1,2,3,...）
        public float DecisionTimeSec;   // 判断にかかった時間（秒）。0 以上を推奨

        public SwipeHistoryItem() { }
        public SwipeHistoryItem(string imageName, bool liked, int appearanceOrder, float decisionTimeSec)
        {
            ImageName = imageName;
            Liked = liked;
            AppearanceOrder = appearanceOrder;
            DecisionTimeSec = decisionTimeSec;
        }
    }

    // ====== Inspector ======
    [Header("References")]
    [Tooltip("ISwipeHistoryProvider を実装したコンポーネント（例：GameManager）")]
    public MonoBehaviour historyProvider; // ISwipeHistoryProvider を実装していること

    [Tooltip("ランキング表示先の UI.Text")]
    public Text outputText;

    [Header("Ranking Options")]
    [Tooltip("ランキングの最大表示件数")] public int maxLines = 20;

    [Tooltip("同一 ImageName を 1件に代表化する（最短 DecisionTime を採用）")]
    public bool groupByImage = true;

    [Tooltip("同着時の優先：true=先に表示された画像（AppearanceOrder が小さい）を優先")]
    public bool preferFirstShown = true;

    [Header("Debug")]
    public bool showDebugLog = false;

    [Tooltip("Provider が未設定のときなどに使えるデバッグ用履歴")]
    public List<SwipeHistoryItem> debugHistory = new List<SwipeHistoryItem>();

    private ISwipeHistoryProvider Provider => historyProvider as ISwipeHistoryProvider;

    // ====== Public API ======
    [ContextMenu("Refresh Ranking Now")]
    public void RefreshRanking()
    {
        var culture = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();

        // 1) 履歴の取得
        var history = GetHistorySnapshot();
        if (history == null || history.Count == 0)
        {
            WriteOutput("(No history yet) まだ履歴がありません。");
            return;
        }

        // 2) いいねのみ抽出、必要なら画像名でグルーピング
        IEnumerable<SwipeHistoryItem> liked = history
            .Where(x => x != null && x.Liked && !string.IsNullOrEmpty(x.ImageName))
            .Select(x => Normalize(x));

        if (!liked.Any())
        {
            WriteOutput("(No liked items yet) いいねされた画像がまだありません。");
            return;
        }

        if (groupByImage)
        {
            liked = liked
                .GroupBy(x => x.ImageName)
                .Select(g => g
                    .OrderBy(x => x.DecisionTimeSec) // 最短を代表
                    .ThenBy(x => preferFirstShown ? x.AppearanceOrder : int.MaxValue)
                    .ThenBy(x => x.ImageName, StringComparer.Ordinal)
                    .First());
        }

        // 3) ランキング：DecisionTimeSec 昇順 → AppearanceOrder 昇順 → ImageName 昇順
        var ranked = liked
            .OrderBy(x => x.DecisionTimeSec)
            .ThenBy(x => preferFirstShown ? x.AppearanceOrder : int.MaxValue)
            .ThenBy(x => x.ImageName, StringComparer.Ordinal)
            .Take(Mathf.Max(1, maxLines))
            .ToList();

        // 4) 出力 
        sb.AppendLine($"=== Liked Images by Shortest Decision Time (top {ranked.Count}) ===");
        sb.AppendLine("(昇順：判断時間が短いほど上位 / 同着は早く出た順→名前)");
        sb.AppendLine();

        int idx = 1;
        foreach (var r in ranked)
        {
            var ms = Mathf.RoundToInt(r.DecisionTimeSec * 1000f);
            sb.AppendLine($"{idx,2}. {r.ImageName}  |  t={ms} ms ({r.DecisionTimeSec.ToString("F3", culture)} s)  |  shown#{r.AppearanceOrder}");
            idx++;
        }

        WriteOutput(sb.ToString());

        if (showDebugLog)
        {
            Debug.Log(sb.ToString());
        }
    }

    // ====== Helpers ======
    private List<SwipeHistoryItem> GetHistorySnapshot()
    {
        // Provider 優先。無ければ debugHistory を採用
        if (Provider != null)
        {
            try
            {
                var list = Provider.GetSwipeHistory();
                if (list != null) return new List<SwipeHistoryItem>(list);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Ranking_time] Provider.GetSwipeHistory() で例外: {e.Message}");
            }
        }
        return new List<SwipeHistoryItem>(debugHistory ?? new List<SwipeHistoryItem>());
    }

    private static SwipeHistoryItem Normalize(SwipeHistoryItem x)
    {
        // 防御的クランプ：NaN/負値を 0 に
        if (float.IsNaN(x.DecisionTimeSec) || x.DecisionTimeSec < 0f)
            x.DecisionTimeSec = 0f;
        if (x.AppearanceOrder < 0) x.AppearanceOrder = 0;
        return x;
    }

    private void WriteOutput(string text)
    {
        if (outputText != null)
        {
            outputText.text = text;
        }
        else
        {
            if (showDebugLog) Debug.Log(text);
        }
    }

    // ====== デバッグデータ生成（任意） ======
    [ContextMenu("Debug: Populate Sample History")]
    private void PopulateSampleHistory()
    {
        debugHistory = new List<SwipeHistoryItem>
        {
            new SwipeHistoryItem("img_apple",  true,  3, 0.62f),
            new SwipeHistoryItem("img_banana", true,  1, 0.48f),
            new SwipeHistoryItem("img_cherry", false, 2, 0.40f),
            new SwipeHistoryItem("img_date",   true,  4, 0.80f),
            new SwipeHistoryItem("img_apple",  true,  8, 0.41f), // 同一画像の再登場（最短 0.41s を採用）
            new SwipeHistoryItem("img_elder",  true,  6, 0.48f),
        };
        RefreshRanking();
    }
}
