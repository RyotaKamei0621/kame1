using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

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

    void Start()
    {
        likeButton.onClick.AddListener(OnLikeButtonClicked);
        ummButton.onClick.AddListener(OnUmmButtonClicked);
        restartButton.onClick.AddListener(OnRestartButtonClicked);
        showResultButton.onClick.AddListener(OnShowResultButtonClicked);
        backButton.onClick.AddListener(OnBackButtonClicked);

        // �� �ύX�_: �S�Ẳ摜��Like�J�E���g��0�ŏ���������
        // ����ɂ��A��x��Like����Ă��Ȃ��摜�������L���O�̑ΏۂɂȂ�
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
            // �����͕ύX�Ȃ� (�������ς݂Ȃ̂ŁA�L�[�����݂��Ȃ��P�[�X�͍l���s�v)
            likeCounts[imageName]++;
            Debug.Log($"Like Count for {imageName}: {likeCounts[imageName]}");
        }

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
        // �� �ύX�_: ���W�b�N���͕̂ύX�s�v�����A
        // likeCounts�ɑS�摜�̃f�[�^�������Ă��邽�߁A0�_�̂��̂��\�[�g�ΏۂɂȂ�
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

        rankingText.text = rankingStr;
    }
}