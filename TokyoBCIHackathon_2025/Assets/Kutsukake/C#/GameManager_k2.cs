using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;

using System;

public class GameManager_k2 : MonoBehaviour
{
    [Header("�J�[�h�ݒ�")]
    [SerializeField] private List<Sprite> cardSprites;
    [SerializeField] private CardController_v2 cardController;

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

    // �摜���Ƃ�Like�J�E���g
    private Dictionary<string, int> likeCounts = new Dictionary<string, int>();

    // �V���b�t�����ƌ��݈ʒu
    private List<int> shuffledIndices = new List<int>();
    private int currentIndexInShuffle = 0;

    // ������������������ �����L���O�̋L�^�i�ǉ��j������������������
    [System.Serializable]
    public struct RankEntry
    {
        public int rank;     // 1��, 2��, �c
        public string name;  // �摜���iSprite.name�j
        public int likes;    // Like��
    }

    // ���߂ŕ\�����������L���O����ێ�
    private List<RankEntry> lastRanking = new List<RankEntry>();

    // �O���Q�Ɨp�i�ǂݎ���p�j
    public IReadOnlyList<RankEntry> LastRanking => lastRanking;
    // ��������������������������������������������������������������������������������������

    void Start()
    {
        likeButton.onClick.AddListener(OnLikeButtonClicked);
        ummButton.onClick.AddListener(OnUmmButtonClicked);
        restartButton.onClick.AddListener(OnRestartButtonClicked);
        showResultButton.onClick.AddListener(OnShowResultButtonClicked);
        backButton.onClick.AddListener(OnBackButtonClicked);

        // ���ׂẲ摜���������ɏ����o�^�i0�ŏ������j
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
            likeCounts[imageName]++;
            Debug.Log($"Like Count for {imageName}: {likeCounts[imageName]}");
        }

        cardController.StartSwipe(direction);

        currentIndexInShuffle++;
        // �� �����Ŏ��J�[�h���o�����ǂ����͊����̋����ɍ��킹�ĕύX���܂���B
        //   �i�A�j�������C�x���g��R���[�`���� ShowNextCard() ���ĂԂ̂����z�j
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
        // �\�����̋L�^�𖈉񃊃Z�b�g
        lastRanking.Clear();

        if (likeCounts.Count == 0)
        {
            rankingText.text = "�摜������܂���B";
            return;
        }

        // �����������肷��悤�A���O�œ񎟃L�[
        var sortedLikes = likeCounts
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key)
            .Take(5)
            .ToList();

        int rank = 1;
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (var pair in sortedLikes)
        {
            // �\�������L�^
            lastRanking.Add(new RankEntry
            {
                rank = rank,
                name = pair.Key,
                likes = pair.Value
            });

            sb.AppendLine($"{rank}��: {pair.Key} ({pair.Value} Likes)");
            rank++;
        }

        rankingText.text = sb.ToString();
    }
}
