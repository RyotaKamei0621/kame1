using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("�J�[�h�ݒ�")]
    [SerializeField] private List<Sprite> cardSprites; // �]������摜�̃��X�g
    [SerializeField] private CardController cardController; // �V�[����̃J�[�h�ւ̎Q��

    [Header("UI�v�f")]
    [SerializeField] private Button likeButton;
    [SerializeField] private Button ummButton;
    [SerializeField] private GameObject endPanel; // �I�����ɕ\������p�l��

    [Header("�^�C�}�[�ݒ�")]
    [Tooltip("�J�[�h�̕\�����ԁi�b�j")]
    [SerializeField] private float displayTime = 5.0f;
    private float timer;
    private bool isTimerActive = false;

    private int currentCardIndex = 0;

    void Start()
    {
        // �{�^���������ꂽ��A���ꂼ��̃��\�b�h���Ăяo���悤�ɐݒ�
        likeButton.onClick.AddListener(OnLikeButtonClicked);
        ummButton.onClick.AddListener(OnUmmButtonClicked);

        // �I���p�l���͔�\����
        if (endPanel != null)
        {
            endPanel.SetActive(false);
        }

        // �ŏ��̃J�[�h��\��
        ShowNextCard();
    }

    void Update()
    {
        // �^�C�}�[���L���ȏꍇ�̂ݎ��Ԃ��J�E���g�_�E��
        if (isTimerActive)
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                // ���Ԑ؂�̏ꍇ�͎����I��UMM
                isTimerActive = false; // �^�C�}�[���~�߂�
                OnUmmButtonClicked();
            }
        }
    }

    public void ShowNextCard()
    {
        if (currentCardIndex < cardSprites.Count)
        {
            // �J�[�h�����Z�b�g���ĐV�����摜��ݒ�
            cardController.ResetCard();
            cardController.GetComponent<Image>().sprite = cardSprites[currentCardIndex];

            // �{�^����������悤�ɂ���
            SetButtonsInteractable(true);

            // �^�C�}�[�����Z�b�g���ċN��
            timer = displayTime;
            isTimerActive = true;
        }
        else
        {
            // �S�ẴJ�[�h���I�����
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
        if (!isTimerActive) return; // ���ɏ������n�܂��Ă���ꍇ�͉������Ȃ�

        // �^�C�}�[���~�߂�
        isTimerActive = false;
        // �A�ł�h�����߂Ƀ{�^���𖳌���
        SetButtonsInteractable(false);

        // �J�[�h�̃X���C�v���J�n
        cardController.StartSwipe(swipeDirection);

        // ���̃J�[�h��
        currentCardIndex++;
    }

    private void SetButtonsInteractable(bool interactable)
    {
        likeButton.interactable = interactable;
        ummButton.interactable = interactable;
    }

    private void EndSession()
    {
        // �J�[�h�ƃ{�^�����\��
        cardController.gameObject.SetActive(false);
        likeButton.gameObject.SetActive(false);
        ummButton.gameObject.SetActive(false);

        // �I���p�l����\��
        if (endPanel != null)
        {
            endPanel.SetActive(true);
        }
        Debug.Log("�S�ẴJ�[�h�̕]�����I���܂����B");
    }
}