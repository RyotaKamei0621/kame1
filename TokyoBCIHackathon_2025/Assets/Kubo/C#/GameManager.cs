using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// ����ۑ��p�̃N���X
public class SwipeRecord
{
    public string ImageName;
    public bool Liked;
    public int AppearanceOrder;

    public SwipeRecord(string name, bool liked, int order)
    {
        ImageName = name;
        Liked = liked;
        AppearanceOrder = order;
    }
}

// �� �ύX: �����L���O�p�̃N���X���ȗ����i���x�֘A�̕ϐ����폜�j
public class RankingData
{
    public string ImageName;
    public int LikeCount = 0;

    public RankingData(string name)
    {
        ImageName = name;
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
    [SerializeField] private Button backButton;
    [SerializeField] private GameObject rankingEntryPrefab;
    [SerializeField] private Transform entryContainer;

    private Dictionary<string, RankingData> rankingData = new Dictionary<string, RankingData>();
    private List<int> shuffledIndices = new List<int>();
    private int currentIndexInShuffle = 0;

    private List<SwipeRecord> swipeHistory = new List<SwipeRecord>();
    private int overallAppearanceCount = 0;

    // �� �폜: ���Ԃ��L�^���Ă����ϐ����폜
    // private float cardDisplayTimestamp;


    void Start()
    {
        likeButton.onClick.AddListener(OnLikeButtonClicked);
        ummButton.onClick.AddListener(OnUmmButtonClicked);
        restartButton.onClick.AddListener(OnRestartButtonClicked);
        showResultButton.onClick.AddListener(OnShowResultButtonClicked);
        backButton.onClick.AddListener(OnBackButtonClicked);

        foreach (var sprite in cardSprites)
        {
            if (!rankingData.ContainsKey(sprite.name))
            {
                rankingData.Add(sprite.name, new RankingData(sprite.name));
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

        overallAppearanceCount++;
        // �� �폜: �\�������̋L�^�������폜
        // cardDisplayTimestamp = Time.time;

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

        // �� �ύX: �����ː��̃J�E���g�������ȗ���
        if (isLike)
        {
            RankingData data = rankingData[imageName];
            data.LikeCount++;
        }

        var record = new SwipeRecord(imageName, isLike, overallAppearanceCount);
        swipeHistory.Add(record);
        Debug.Log($"�L�^ #{record.AppearanceOrder}: �u{record.ImageName}�v���u{(record.Liked ? "Like" : "UMM")}�v���܂����B");

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
        rankingPanel.SetActive(true);
        roundEndPanel.SetActive(false);
        StartCoroutine(AnimateRankingDisplay());
    }

    private void OnBackButtonClicked()
    {
        rankingPanel.SetActive(false);
        roundEndPanel.SetActive(true);
    }

    private IEnumerator AnimateRankingDisplay()
    {
        foreach (Transform child in entryContainer)
        {
            Destroy(child.gameObject);
        }
        yield return null;

        // �� �ύX: ���ёւ��̃��[�����u�����ː��v�݂̂Ɋȗ���
        var sortedData = rankingData.Values
            .OrderByDescending(data => data.LikeCount)
            .ToList();

        for (int i = 0; i < sortedData.Count && i < 5; i++)
        {
            GameObject entryGO = Instantiate(rankingEntryPrefab, entryContainer);

            Text rankText = entryGO.transform.Find("RankText").GetComponent<Text>();
            Image profileImage = entryGO.transform.Find("ProfileImage").GetComponent<Image>();
            Text likeCountText = entryGO.transform.Find("LikeCountText").GetComponent<Text>();
            Text nameText = entryGO.transform.Find("NameText").GetComponent<Text>();

            rankText.text = $"{i + 1}��";
            profileImage.sprite = cardSprites.Find(sprite => sprite.name == sortedData[i].ImageName);
            likeCountText.text = $"{sortedData[i].LikeCount} Likes";
            nameText.text = sortedData[i].ImageName;

            RectTransform rectTransform = entryGO.GetComponent<RectTransform>();
            Vector2 targetPosition = rectTransform.anchoredPosition;
            Vector2 startPosition = targetPosition + new Vector2(Screen.width, 0);

            float duration = 0.5f;
            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float progress = Mathf.SmoothStep(0, 1, timer / duration);
                rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, progress);
                yield return null;
            }
            rectTransform.anchoredPosition = targetPosition;

            yield return new WaitForSeconds(0.2f);
        }
    }

    public System.Collections.Generic.IReadOnlyList<SwipeRecord> GetSwipeHistory()
    {
        return swipeHistory.AsReadOnly();
    }

}