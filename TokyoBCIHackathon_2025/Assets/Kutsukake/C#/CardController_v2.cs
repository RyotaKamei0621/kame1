using UnityEngine;
using System.Collections;

public class CardController_v2 : MonoBehaviour
{
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private GameManager_k2 gameManager; // GameManagerへの参照

    void Awake()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    void Start()
    {
        // GameManagerのインスタンスを探して保持
        gameManager = FindObjectOfType<GameManager_k2>();
    }

    public void ResetCard()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        gameObject.SetActive(true);
    }

    public void StartSwipe(Vector2 direction)
    {
        StartCoroutine(SwipeCoroutine(direction, 0.5f));
    }

    private IEnumerator SwipeCoroutine(Vector2 direction, float duration)
    {
        float timer = 0f;
        Vector3 startPosition = transform.position;
        float targetX = Mathf.Sign(direction.x) * (Screen.width * 0.8f);
        Vector3 targetPosition = startPosition + new Vector3(targetX, 50f, 0);
        Quaternion targetRotation = Quaternion.Euler(0, 0, -Mathf.Sign(direction.x) * 20f);

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.SmoothStep(0, 1, timer / duration);
            transform.position = Vector3.Lerp(startPosition, targetPosition, progress);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, progress);
            yield return null;
        }

        gameObject.SetActive(false);

        // ★★★ アニメーション完了後、GameManagerに次のカードを表示するように直接伝える ★★★
        gameManager.ShowNextCard();
    }
}