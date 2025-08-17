using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Liked�i�����ˁj���ꂽ�摜������ΏۂɁA���f�ɂ����������ԁiDecisionTimeSec�j���Z�����Ń����L���O�\������R���|�[�l���g�B
/// 
/// �g�����F
/// 1) �{�X�N���v�g����� GameObject �ɒǉ�
/// 2) Output Text �� UI.Text ���A�T�C��
/// 3) History Provider �ɁAISwipeHistoryProvider �����������R���|�[�l���g�i��FGameManager�j���A�T�C��
///    �i��jpublic class GameManager : MonoBehaviour, Ranking_time.ISwipeHistoryProvider { ... }
/// 4) Button �Ȃǂ��� RefreshRanking() ���Ăׂ΁A�����L���O���X�V����܂��B
/// 
/// �����F�����̃^�C�u���[�N�� (a) AppearanceOrder ���������i�����o���j�� (b) ImageName �̎����� �ł��B
/// groupByImageON �̏ꍇ�́A����摜��������L�^����Ă��Ă��u�ŒZ�� DecisionTimeSec �� 1���̂݁v���\�Ƃ��č̗p���܂��B
/// </summary>
public class Ranking_time : MonoBehaviour
{
    // ====== ���J�C���^�t�F�[�X�iGameManager ��������������j ======
    public interface ISwipeHistoryProvider
    {
        /// <summary>�����̃X�i�b�v�V���b�g��Ԃ��Ă��������B</summary>
        List<SwipeHistoryItem> GetSwipeHistory();
    }

    [Serializable]
    public class SwipeHistoryItem
    {
        public string ImageName;        // �摜�t�@�C������L�[
        public bool Liked;              // �����˔���
        public int AppearanceOrder;     // ���̉摜���ŏ��ɕ\�����ꂽ�����i1,2,3,...�j
        public float DecisionTimeSec;   // ���f�ɂ����������ԁi�b�j�B0 �ȏ�𐄏�

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
    [Tooltip("ISwipeHistoryProvider �����������R���|�[�l���g�i��FGameManager�j")]
    public MonoBehaviour historyProvider; // ISwipeHistoryProvider ���������Ă��邱��

    [Tooltip("�����L���O�\����� UI.Text")]
    public Text outputText;

    [Header("Ranking Options")]
    [Tooltip("�����L���O�̍ő�\������")] public int maxLines = 20;

    [Tooltip("���� ImageName �� 1���ɑ�\������i�ŒZ DecisionTime ���̗p�j")]
    public bool groupByImage = true;

    [Tooltip("�������̗D��Ftrue=��ɕ\�����ꂽ�摜�iAppearanceOrder ���������j��D��")]
    public bool preferFirstShown = true;

    [Header("Debug")]
    public bool showDebugLog = false;

    [Tooltip("Provider �����ݒ�̂Ƃ��ȂǂɎg����f�o�b�O�p����")]
    public List<SwipeHistoryItem> debugHistory = new List<SwipeHistoryItem>();

    private ISwipeHistoryProvider Provider => historyProvider as ISwipeHistoryProvider;

    // ====== Public API ======
    [ContextMenu("Refresh Ranking Now")]
    public void RefreshRanking()
    {
        var culture = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();

        // 1) �����̎擾
        var history = GetHistorySnapshot();
        if (history == null || history.Count == 0)
        {
            WriteOutput("(No history yet) �܂�����������܂���B");
            return;
        }

        // 2) �����˂̂ݒ��o�A�K�v�Ȃ�摜���ŃO���[�s���O
        IEnumerable<SwipeHistoryItem> liked = history
            .Where(x => x != null && x.Liked && !string.IsNullOrEmpty(x.ImageName))
            .Select(x => Normalize(x));

        if (!liked.Any())
        {
            WriteOutput("(No liked items yet) �����˂��ꂽ�摜���܂�����܂���B");
            return;
        }

        if (groupByImage)
        {
            liked = liked
                .GroupBy(x => x.ImageName)
                .Select(g => g
                    .OrderBy(x => x.DecisionTimeSec) // �ŒZ���\
                    .ThenBy(x => preferFirstShown ? x.AppearanceOrder : int.MaxValue)
                    .ThenBy(x => x.ImageName, StringComparer.Ordinal)
                    .First());
        }

        // 3) �����L���O�FDecisionTimeSec ���� �� AppearanceOrder ���� �� ImageName ����
        var ranked = liked
            .OrderBy(x => x.DecisionTimeSec)
            .ThenBy(x => preferFirstShown ? x.AppearanceOrder : int.MaxValue)
            .ThenBy(x => x.ImageName, StringComparer.Ordinal)
            .Take(Mathf.Max(1, maxLines))
            .ToList();

        // 4) �o�� 
        sb.AppendLine($"=== Liked Images by Shortest Decision Time (top {ranked.Count}) ===");
        sb.AppendLine("(�����F���f���Ԃ��Z���قǏ�� / �����͑����o���������O)");
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
        // Provider �D��B������� debugHistory ���̗p
        if (Provider != null)
        {
            try
            {
                var list = Provider.GetSwipeHistory();
                if (list != null) return new List<SwipeHistoryItem>(list);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Ranking_time] Provider.GetSwipeHistory() �ŗ�O: {e.Message}");
            }
        }
        return new List<SwipeHistoryItem>(debugHistory ?? new List<SwipeHistoryItem>());
    }

    private static SwipeHistoryItem Normalize(SwipeHistoryItem x)
    {
        // �h��I�N�����v�FNaN/���l�� 0 ��
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

    // ====== �f�o�b�O�f�[�^�����i�C�Ӂj ======
    [ContextMenu("Debug: Populate Sample History")]
    private void PopulateSampleHistory()
    {
        debugHistory = new List<SwipeHistoryItem>
        {
            new SwipeHistoryItem("img_apple",  true,  3, 0.62f),
            new SwipeHistoryItem("img_banana", true,  1, 0.48f),
            new SwipeHistoryItem("img_cherry", false, 2, 0.40f),
            new SwipeHistoryItem("img_date",   true,  4, 0.80f),
            new SwipeHistoryItem("img_apple",  true,  8, 0.41f), // ����摜�̍ēo��i�ŒZ 0.41s ���̗p�j
            new SwipeHistoryItem("img_elder",  true,  6, 0.48f),
        };
        RefreshRanking();
    }
}
