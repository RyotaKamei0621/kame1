using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// �]�����ʂ��i�[���邽�߂̃V���v���ȃN���X
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
    [Header("�J�[�h�ݒ�")]
    [SerializeField] private List<Sprite> cardSprites;
    [SerializeField] private CardController cardController; // �Q�Ƃ�1�ɖ߂�

    [Header("UI�v�f")]
    [SerializeField] private Button likeButton;
    [SerializeField] private Button ummButton;
    [SerializeField] private GameObject endPanel;

    [Header("�^�C�}�[�ݒ�")]
    [SerializeField] private float displayTime = 5.0f;
    private float timer;
    private bool isTimerActive = false;

    private int currentCardIndex = 0;

    // �]�����ʂ�ۑ����邽�߂̃��X�g
    private List<SwipeResult> swipeResults = new List<SwipeResult>();

    void Start()
    {
        likeButton.onClick.AddListener(OnLikeButtonClicked);
        ummButton.onClick.AddListener(OnUmmButtonClicked);

        if (endPanel != null)
        {
            endPanel.SetActive(false);
        }

        // �ŏ��̃J�[�h��\��
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

    // ���̃J�[�h��\�����郁�\�b�h�i�V���v���Łj
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

    // �X���C�v�����ƋL�^���܂Ƃ߂����\�b�h
    private void HandleSwipe(Vector2 direction, bool isLike)
    {
        if (!isTimerActive) return;

        isTimerActive = false;
        SetButtonsInteractable(false);

        // ������ �]�����ʂ������ŋL�^���܂� ������
        string imageName = cardSprites[currentCardIndex].name;
        swipeResults.Add(new SwipeResult(imageName, isLike));
        Debug.Log($"�L�^: {imageName} -> {(isLike ? "Like" : "UMM")}");

        // �J�[�h�̃X���C�v���J�n
        cardController.StartSwipe(direction);

        // ���̃J�[�h�փC���f�b�N�X��i�߂�
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

        Debug.Log("�S�ẴJ�[�h�̕]�����I���܂����B");
        PrintResults();
    }

    // �ŏI���ʂ��R���\�[���ɏo�͂���
    private void PrintResults()
    {
        Debug.Log("--- �ŏI�]������ ---");
        foreach (var result in swipeResults)
        {
            Debug.Log($"{result.ImageName}: {(result.Liked ? "Like" : "UnLike")}");
        }
    }
}