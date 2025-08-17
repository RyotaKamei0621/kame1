using UnityEngine;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;
using Debug = UnityEngine.Debug;  // コンフリクト回避

public class RankingDisplaySwitcher : MonoBehaviour
{
    [Header("対象ランキング")]
    public Ranking featureRanking;
    public Ranking_time timeRanking;

    [Header("共通出力Text（オプション）")]
    public UnityEngine.UI.Text sharedOutputText;
    public bool disableInactive = true;
    public bool showFeatureOnStart = true;

    void Awake()
    {
        // 両者にTextを共通で渡す
        if (sharedOutputText != null)
        {
            featureRanking?.SetOutputText(sharedOutputText);
            if (timeRanking != null) timeRanking.outputText = sharedOutputText;
        }
    }

    void Start()
    {
        if (showFeatureOnStart) ShowFeatureRanking();
        else ShowDecisionTimeRanking();
    }

    public void ShowFeatureRanking()
    {
        if (disableInactive)
        {
            if (timeRanking) timeRanking.enabled = false;
            if (featureRanking) featureRanking.enabled = true;
        }
        featureRanking?.RefreshRankingUI();  // ①で追加した公開関数
    }

    public void ShowDecisionTimeRanking()
    {
        if (disableInactive)
        {
            if (featureRanking) featureRanking.enabled = false;
            if (timeRanking) timeRanking.enabled = true;
        }
        timeRanking?.RefreshRanking();  // こちらは既にpublic関数あり
    }
}
