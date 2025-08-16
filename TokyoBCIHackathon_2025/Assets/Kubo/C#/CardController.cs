using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class CardController : MonoBehaviour
{
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private GameManager gameManager;

    void Awake()
    {
        // 初期位置と回転を保存
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    void Start()
    {
        // GameManagerのインスタンスを探してキャッシュ
        gameManager = FindObjectOfType<GameManager>();
    }

    // カードを初期状態に戻す
    public void ResetCard()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        gameObject.SetActive(true);
    }

    // スワイプアニメーションを開始する
    public void StartSwipe(Vector2 direction)
    {
        StartCoroutine(SwipeCoroutine(direction, 0.5f));
    }

    private IEnumerator SwipeCoroutine(Vector2 direction, float duration)
    {
        float timer = 0f;
        Vector3 startPosition = transform.position;
        // 画面の幅を基準に、画面外へ移動するターゲット位置を設定
        float targetX = Mathf.Sign(direction.x) * (Screen.width * 0.8f);
        Vector3 targetPosition = startPosition + new Vector3(targetX, 50f, 0); // 少し上に持ち上げながら移動

        // スワイプ方向に少し傾ける
        Quaternion targetRotation = Quaternion.Euler(0, 0, -Mathf.Sign(direction.x) * 20f);

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.SmoothStep(0, 1, timer / duration); // 滑らかな動きにする

            transform.position = Vector3.Lerp(startPosition, targetPosition, progress);
            transform.rotation = Quaternion.Lerp(initialRotation, targetRotation, progress);

            yield return null;
        }

        // アニメーション完了後
        gameObject.SetActive(false);

        // GameManagerに次のカードを表示するように伝える
        gameManager.ShowNextCard();
    }
}