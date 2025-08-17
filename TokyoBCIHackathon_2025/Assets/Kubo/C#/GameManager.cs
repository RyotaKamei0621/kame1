using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SocialPlatforms.Impl;

// �� �ǉ�: �X���C�v�̏����܂Ƃ߂ċL�^���邽�߂̃N���X
public class SwipeRecord
{
    public string ImageName;
    public bool Liked;
    public int AppearanceOrder; // �S�̂ŉ��Ԗڂɕ\�����ꂽ��

    public SwipeRecord(string name, bool liked, int order)
    {
        ImageName = name;
        Liked = liked;
        AppearanceOrder = order;
        
    }
}

public class GameManager : MonoBehaviour
{
    [Header("�J�[�h�ݒ�")]
    [SerializeField] private List<Sprite> cardSprites;
    [SerializeField] private CardController cardController;

    [Header("���C��UI�v�f")]
    [SerializeField] private Button likeButton;
    [SerializeField] private Button ummButton;

    [Header("�����UI")]
    [SerializeField] private GameObject roundEndPanel;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button showResultButton;

    [Header("�����L���OUI")]
    [SerializeField] private GameObject rankingPanel;
    [SerializeField] private Text rankingText;
    [SerializeField] private Button backButton;

    private Dictionary<string, int> likeCounts = new Dictionary<string, int>();
    private List<int> shuffledIndices = new List<int>();
    private int currentIndexInShuffle = 0;

    // �� �ǉ�: �S�Ă̕]��������ۑ����郊�X�g
    private List<SwipeRecord> swipeHistory = new List<SwipeRecord>();
    // �� �ǉ�: ���E���h���܂����ŃJ�E���g��������A�ʎZ�̕\�����J�E���^�[
    private int overallAppearanceCount = 0;


    void Start()
    {
        likeButton.onClick.AddListener(OnLikeButtonClicked);
        ummButton.onClick.AddListener(OnUmmButtonClicked);
        restartButton.onClick.AddListener(OnRestartButtonClicked);
        showResultButton.onClick.AddListener(OnShowResultButtonClicked);
        backButton.onClick.AddListener(OnBackButtonClicked);

        foreach (var sprite in cardSprites)
        {
            if (!likeCounts.ContainsKey(sprite.name))
            {
                likeCounts.Add(sprite.name, 0);
            }
        }

        rankingPanel.SetActive(false);
        roundEndPanel.SetActive(false);

        StartNewRound();
    }

    private void StartNewRound()
    {
        ShuffleCards();
        currentIndexInShuffle = 0;

        cardController.gameObject.SetActive(true);
        likeButton.gameObject.SetActive(true);
        ummButton.gameObject.SetActive(true);
        roundEndPanel.SetActive(false);
        rankingPanel.SetActive(false);

        ShowNextCard();
    }

    private void ShuffleCards()
    {
        shuffledIndices = Enumerable.Range(0, cardSprites.Count).ToList();
        for (int i = 0; i < shuffledIndices.Count; i++)
        {
            int temp = shuffledIndices[i];
            int randomIndex = Random.Range(i, shuffledIndices.Count);
            shuffledIndices[i] = shuffledIndices[randomIndex];
            shuffledIndices[randomIndex] = temp;
        }
    }

    public void ShowNextCard()
    {
        if (currentIndexInShuffle >= shuffledIndices.Count)
        {
            EndRound();
            return;
        }

        // �� �ύX�_: �V�����J�[�h��\������O�ɁA�ʎZ�J�E���^�[��1���₷
        overallAppearanceCount++;

        cardController.ResetCard();
        int cardIndex = shuffledIndices[currentIndexInShuffle];
        cardController.GetComponent<Image>().sprite = cardSprites[cardIndex];

        SetButtonsInteractable(true);
    }

    private void OnLikeButtonClicked()
    {
        HandleSwipe(Vector2.right, true);
    }

    private void OnUmmButtonClicked()
    {
        HandleSwipe(Vector2.left, false);
    }

    private void HandleSwipe(Vector2 direction, bool isLike)
    {
        SetButtonsInteractable(false);

        int cardIndex = shuffledIndices[currentIndexInShuffle];
        string imageName = cardSprites[cardIndex].name;

        if (isLike)
        {
            likeCounts[imageName]++;
        }

        // �� �ύX�_: �V����SwipeRecord���쐬���ė������X�g�ɒǉ�
        var record = new SwipeRecord(imageName, isLike, overallAppearanceCount);
        swipeHistory.Add(record);

        // �� �ύX�_: �R���\�[���ɕ\������L�^�̓��e�����ڍׂɂ���
        Debug.Log($"�L�^ #{record.AppearanceOrder}: �u{record.ImageName}�v���u{(record.Liked ? "Like" : "Unlike")}�v���܂����B");

        cardController.StartSwipe(direction);

        currentIndexInShuffle++;
    }

    private void SetButtonsInteractable(bool interactable)
    {
        likeButton.interactable = interactable;
        ummButton.interactable = interactable;
    }

    private void EndRound()
    {
        cardController.gameObject.SetActive(false);
        likeButton.gameObject.SetActive(false);
        ummButton.gameObject.SetActive(false);

        roundEndPanel.SetActive(true);
    }

    private void OnRestartButtonClicked()
    {
        StartNewRound();
    }

    private void OnShowResultButtonClicked()
    {
        CalculateAndShowRanking();
        rankingPanel.SetActive(true);
        roundEndPanel.SetActive(false);
    }

    private void OnBackButtonClicked()
    {
        rankingPanel.SetActive(false);
        roundEndPanel.SetActive(true);
    }

    private void CalculateAndShowRanking()
    {
        var sortedLikes = likeCounts.OrderByDescending(pair => pair.Value);

        string rankingStr = "";
        int rank = 1;
        foreach (var pair in sortedLikes)
        {
            rankingStr += $"{rank}��: {pair.Key} ({pair.Value} Likes)\n";
            rank++;
            if (rank > 5)
            {
                break;
            }
        }

        if (string.IsNullOrEmpty(rankingStr))
        {
            rankingStr = "�摜������܂���B";
        }

        // rankingText.text = rankingStr;
    }
    // �������O���Ɍ��J�i�ǂݎ���p�j
    public System.Collections.Generic.IReadOnlyList<SwipeRecord> GetSwipeHistory()
    {
        return swipeHistory.AsReadOnly();  // List��ReadOnlyCollection �ɂ��ĕԂ�
    }

    // 画像リスト（Rankingの人物画像表示に使う）
    public List<Sprite> GetAllCardSprites() => cardSprites;

    public string GetCurrentImageName()
    {
        if (currentIndexInShuffle >= shuffledIndices.Count)
        {
            return "(end)";
        }

        int cardIndex = shuffledIndices[currentIndexInShuffle];
        return cardSprites[cardIndex].name;
    }


}