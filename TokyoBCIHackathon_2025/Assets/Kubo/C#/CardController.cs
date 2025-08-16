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
        // �����ʒu�Ɖ�]��ۑ�
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    void Start()
    {
        // GameManager�̃C���X�^���X��T���ăL���b�V��
        gameManager = FindObjectOfType<GameManager>();
    }

    // �J�[�h��������Ԃɖ߂�
    public void ResetCard()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        gameObject.SetActive(true);
    }

    // �X���C�v�A�j���[�V�������J�n����
    public void StartSwipe(Vector2 direction)
    {
        StartCoroutine(SwipeCoroutine(direction, 0.5f));
    }

    private IEnumerator SwipeCoroutine(Vector2 direction, float duration)
    {
        float timer = 0f;
        Vector3 startPosition = transform.position;
        // ��ʂ̕�����ɁA��ʊO�ֈړ�����^�[�Q�b�g�ʒu��ݒ�
        float targetX = Mathf.Sign(direction.x) * (Screen.width * 0.8f);
        Vector3 targetPosition = startPosition + new Vector3(targetX, 50f, 0); // ������Ɏ����グ�Ȃ���ړ�

        // �X���C�v�����ɏ����X����
        Quaternion targetRotation = Quaternion.Euler(0, 0, -Mathf.Sign(direction.x) * 20f);

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.SmoothStep(0, 1, timer / duration); // ���炩�ȓ����ɂ���

            transform.position = Vector3.Lerp(startPosition, targetPosition, progress);
            transform.rotation = Quaternion.Lerp(initialRotation, targetRotation, progress);

            yield return null;
        }

        // �A�j���[�V����������
        gameObject.SetActive(false);

        // GameManager�Ɏ��̃J�[�h��\������悤�ɓ`����
        gameManager.ShowNextCard();
    }
}