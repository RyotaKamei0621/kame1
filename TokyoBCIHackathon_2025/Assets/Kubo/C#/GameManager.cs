using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// 評価結果を格納するためのシンプルなクラス
public class SwipeResult
{
    public string ImageName;
    public bool Liked; // true: Like, false: UMM

    public SwipeResult(string name, bool liked)
    {
        ImageName = name;
        Liked = liked;
    }
}

public class GameManager : MonoBehaviour
{
    [Header("カード設定")]
    [SerializeField] private List<Sprite> cardSprites;
    [SerializeField] private CardController cardController; // 参照を1つに戻す

    [Header("UI要素")]
    [SerializeField] private Button likeButton;
    [SerializeField] private Button ummButton;
    [SerializeField] private GameObject endPanel;

    [Header("タイマー設定")]
    [SerializeField] private float displayTime = 5.0f;
    private float timer;
    private bool isTimerActive = false;

    private int currentCardIndex = 0;

    // 評価結果を保存するためのリスト
    private List<SwipeResult> swipeResults = new List<SwipeResult>();

    void Start()
    {
        likeButton.onClick.AddListener(OnLikeButtonClicked);
        ummButton.onClick.AddListener(OnUmmButtonClicked);

        if (endPanel != null)
        {
            endPanel.SetActive(false);
        }

        // 最初のカードを表示
        ShowNextCard();
    }

    void Update()
    {
        if (isTimerActive)
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                OnUmmButtonClicked();
            }
        }
    }

    // 次のカードを表示するメソッド（シンプル版）
    public void ShowNextCard()
    {
        if (currentCardIndex < cardSprites.Count)
        {
            cardController.ResetCard();
            cardController.GetComponent<Image>().sprite = cardSprites[currentCardIndex];

            SetButtonsInteractable(true);
            timer = displayTime;
            isTimerActive = true;
        }
        else
        {
            EndSession();
        }
    }

    private void OnLikeButtonClicked()
    {
        HandleSwipe(Vector2.right, true);
    }

    private void OnUmmButtonClicked()
    {
        HandleSwipe(Vector2.left, false);
    }

    // スワイプ処理と記録をまとめたメソッド
    private void HandleSwipe(Vector2 direction, bool isLike)
    {
        if (!isTimerActive) return;

        isTimerActive = false;
        SetButtonsInteractable(false);

        // ★★★ 評価結果をここで記録します ★★★
        string imageName = cardSprites[currentCardIndex].name;
        swipeResults.Add(new SwipeResult(imageName, isLike));
        Debug.Log($"記録: {imageName} -> {(isLike ? "Like" : "UMM")}");

        // カードのスワイプを開始
        cardController.StartSwipe(direction);

        // 次のカードへインデックスを進める
        currentCardIndex++;
    }

    private void SetButtonsInteractable(bool interactable)
    {
        likeButton.interactable = interactable;
        ummButton.interactable = interactable;
    }

    private void EndSession()
    {
        cardController.gameObject.SetActive(false);
        likeButton.gameObject.SetActive(false);
        ummButton.gameObject.SetActive(false);

        if (endPanel != null)
        {
            endPanel.SetActive(true);
        }

        Debug.Log("全てのカードの評価が終わりました。");
        PrintResults();
    }

    // 最終結果をコンソールに出力する
    private void PrintResults()
    {
        Debug.Log("--- 最終評価結果 ---");
        foreach (var result in swipeResults)
        {
            Debug.Log($"{result.ImageName}: {(result.Liked ? "Like" : "UnLike")}");
        }
    }
}