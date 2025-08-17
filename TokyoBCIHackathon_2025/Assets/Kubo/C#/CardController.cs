using UnityEngine;
using System.Collections;

public class CardController : MonoBehaviour
{
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private GameManager gameManager; // GameManager�ւ̎Q��

    void Awake()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    void Start()
    {
        // GameManager�̃C���X�^���X��T���ĕێ�
        gameManager = FindObjectOfType<GameManager>();
    }

    public void ResetCard()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        gameObject.SetActive(true);
    }

    public void StartSwipe(Vector2 direction)
    {
        StartCoroutine(SwipeCoroutine(direction, 0.2f));
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

        // ������ �A�j���[�V����������AGameManager�Ɏ��̃J�[�h��\������悤�ɒ��ړ`���� ������
        gameManager.ShowNextCard();
    }
}