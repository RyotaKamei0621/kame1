using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

// ★ 追加: スワイプの情報をまとめて記録するためのクラス
public class SwipeRecord
{
    public string ImageName;
    public bool Liked;
    public int AppearanceOrder; // 全体で何番目に表示されたか

    public SwipeRecord(string name, bool liked, int order)
    {
        ImageName = name;
        Liked = liked;
        AppearanceOrder = order;
    }
}

public class GameManager : MonoBehaviour
{
    [Header("カード設定")]
    [SerializeField] private List<Sprite> cardSprites;
    [SerializeField] private CardController cardController;

    [Header("メインUI要素")]
    [SerializeField] private Button likeButton;
    [SerializeField] private Button ummButton;

    [Header("周回後UI")]
    [SerializeField] private GameObject roundEndPanel;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button showResultButton;

    [Header("ランキングUI")]
    [SerializeField] private GameObject rankingPanel;
    [SerializeField] private Text rankingText;
    [SerializeField] private Button backButton;

    private Dictionary<string, int> likeCounts = new Dictionary<string, int>();
    private List<int> shuffledIndices = new List<int>();
    private int currentIndexInShuffle = 0;

    // ★ 追加: 全ての評価履歴を保存するリスト
    private List<SwipeRecord> swipeHistory = new List<SwipeRecord>();
    // ★ 追加: ラウンドをまたいでカウントし続ける、通算の表示順カウンター
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

        // ★ 変更点: 新しいカードを表示する前に、通算カウンターを1増やす
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

        // ★ 変更点: 新しいSwipeRecordを作成して履歴リストに追加
        var record = new SwipeRecord(imageName, isLike, overallAppearanceCount);
        swipeHistory.Add(record);

        // ★ 変更点: コンソールに表示する記録の内容をより詳細にする
        Debug.Log($"記録 #{record.AppearanceOrder}: 「{record.ImageName}」を「{(record.Liked ? "Like" : "Unlike")}」しました。");

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
            rankingStr += $"{rank}位: {pair.Key} ({pair.Value} Likes)\n";
            rank++;
            if (rank > 5)
            {
                break;
            }
        }

        if (string.IsNullOrEmpty(rankingStr))
        {
            rankingStr = "画像がありません。";
        }

        rankingText.text = rankingStr;
    }
    // 履歴を外部に公開（読み取り専用）
    public System.Collections.Generic.IReadOnlyList<SwipeRecord> GetSwipeHistory()
    {
        return swipeHistory.AsReadOnly();  // List→ReadOnlyCollection にして返す
    }

}