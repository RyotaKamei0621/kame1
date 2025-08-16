using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("カード設定")]
    [SerializeField] private List<Sprite> cardSprites; // 評価する画像のリスト
    [SerializeField] private CardController cardController; // シーン上のカードへの参照

    [Header("UI要素")]
    [SerializeField] private Button likeButton;
    [SerializeField] private Button ummButton;
    [SerializeField] private GameObject endPanel; // 終了時に表示するパネル

    [Header("タイマー設定")]
    [Tooltip("カードの表示時間（秒）")]
    [SerializeField] private float displayTime = 5.0f;
    private float timer;
    private bool isTimerActive = false;

    private int currentCardIndex = 0;

    void Start()
    {
        // ボタンが押されたら、それぞれのメソッドを呼び出すように設定
        likeButton.onClick.AddListener(OnLikeButtonClicked);
        ummButton.onClick.AddListener(OnUmmButtonClicked);

        // 終了パネルは非表示に
        if (endPanel != null)
        {
            endPanel.SetActive(false);
        }

        // 最初のカードを表示
        ShowNextCard();
    }

    void Update()
    {
        // タイマーが有効な場合のみ時間をカウントダウン
        if (isTimerActive)
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                // 時間切れの場合は自動的にUMM
                isTimerActive = false; // タイマーを止める
                OnUmmButtonClicked();
            }
        }
    }

    public void ShowNextCard()
    {
        if (currentCardIndex < cardSprites.Count)
        {
            // カードをリセットして新しい画像を設定
            cardController.ResetCard();
            cardController.GetComponent<Image>().sprite = cardSprites[currentCardIndex];

            // ボタンを押せるようにする
            SetButtonsInteractable(true);

            // タイマーをリセットして起動
            timer = displayTime;
            isTimerActive = true;
        }
        else
        {
            // 全てのカードが終わった
            EndSession();
        }
    }

    private void OnLikeButtonClicked()
    {
        HandleButtonAction(Vector2.right);
    }

    private void OnUmmButtonClicked()
    {
        HandleButtonAction(Vector2.left);
    }

    private void HandleButtonAction(Vector2 swipeDirection)
    {
        if (!isTimerActive) return; // 既に処理が始まっている場合は何もしない

        // タイマーを止める
        isTimerActive = false;
        // 連打を防ぐためにボタンを無効化
        SetButtonsInteractable(false);

        // カードのスワイプを開始
        cardController.StartSwipe(swipeDirection);

        // 次のカードへ
        currentCardIndex++;
    }

    private void SetButtonsInteractable(bool interactable)
    {
        likeButton.interactable = interactable;
        ummButton.interactable = interactable;
    }

    private void EndSession()
    {
        // カードとボタンを非表示
        cardController.gameObject.SetActive(false);
        likeButton.gameObject.SetActive(false);
        ummButton.gameObject.SetActive(false);

        // 終了パネルを表示
        if (endPanel != null)
        {
            endPanel.SetActive(true);
        }
        Debug.Log("全てのカードの評価が終わりました。");
    }
}