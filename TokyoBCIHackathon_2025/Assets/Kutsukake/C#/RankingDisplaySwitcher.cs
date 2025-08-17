using UnityEngine;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;
using Debug = UnityEngine.Debug;  // �R���t���N�g���

public class RankingDisplaySwitcher : MonoBehaviour
{
    [Header("�Ώۃ����L���O")]
    public Ranking featureRanking;
    public Ranking_time timeRanking;

    [Header("���ʏo��Text�i�I�v�V�����j")]
    public UnityEngine.UI.Text sharedOutputText;
    public bool disableInactive = true;
    public bool showFeatureOnStart = true;

    void Awake()
    {
        // ���҂�Text�����ʂœn��
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
        featureRanking?.RefreshRankingUI();  // �@�Œǉ��������J�֐�
    }

    public void ShowDecisionTimeRanking()
    {
        if (disableInactive)
        {
            if (featureRanking) featureRanking.enabled = false;
            if (timeRanking) timeRanking.enabled = true;
        }
        timeRanking?.RefreshRanking();  // ������͊���public�֐�����
    }
}
