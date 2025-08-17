using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;

using System;

public class GameManager_k2 : MonoBehaviour
{
    [Header("カード設定")]
    [SerializeField] private List<Sprite> cardSprites;
    [SerializeField] private CardController_v2 cardController;

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

    // 画像ごとのLikeカウント
    private Dictionary<string, int> likeCounts = new Dictionary<string, int>();

    // シャッフル順と現在位置
    private List<int> shuffledIndices = new List<int>();
    private int currentIndexInShuffle = 0;

    // ───────── ランキングの記録（追加）─────────
    [System.Serializable]
    public struct RankEntry
    {
        public int rank;     // 1位, 2位, …
        public string name;  // 画像名（Sprite.name）
        public int likes;    // Like数
    }

    // 直近で表示したランキング順を保持
    private List<RankEntry> lastRanking = new List<RankEntry>();

    // 外部参照用（読み取り専用）
    public IReadOnlyList<RankEntry> LastRanking => lastRanking;
    // ───────────────────────────────────────────

    void Start()
    {
        likeButton.onClick.AddListener(OnLikeButtonClicked);
        ummButton.onClick.AddListener(OnUmmButtonClicked);
        restartButton.onClick.AddListener(OnRestartButtonClicked);
        showResultButton.onClick.AddListener(OnShowResultButtonClicked);
        backButton.onClick.AddListener(OnBackButtonClicked);

        // すべての画像名を辞書に初期登録（0で初期化）
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
        // ※ ここで次カードを出すかどうかは既存の挙動に合わせて変更しません。
        //   （アニメ完了イベントやコルーチンで ShowNextCard() を呼ぶのが理想）
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
        // 表示順の記録を毎回リセット
        lastRanking.Clear();

        if (likeCounts.Count == 0)
        {
            rankingText.text = "画像がありません。";
            return;
        }

        // 同率時も安定するよう、名前で二次キー
        var sortedLikes = likeCounts
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key)
            .Take(5)
            .ToList();

        int rank = 1;
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (var pair in sortedLikes)
        {
            // 表示順を記録
            lastRanking.Add(new RankEntry
            {
                rank = rank,
                name = pair.Key,
                likes = pair.Value
            });

            sb.AppendLine($"{rank}位: {pair.Key} ({pair.Value} Likes)");
            rank++;
        }

        rankingText.text = sb.ToString();
    }
}
