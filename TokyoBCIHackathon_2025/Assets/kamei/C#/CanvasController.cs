using UnityEngine;

public class CanvasController : MonoBehaviour
{
    [SerializeField] private GameObject canvas01;
    [SerializeField] private GameObject canvas02;
    [SerializeField] private GameObject LoadingPanel;

    void Start()
    {
        // 最初はCanvas01のみ表示
        canvas01.SetActive(true);
        LoadingPanel.SetActive(false);
        canvas02.SetActive(false);
    }

    public void LoadingPanelOn()
    {
        LoadingPanel.SetActive(true);
    }

    // 2秒後などに呼び出す想定
    public void SwitchToCanvas02()
    {
        canvas01.SetActive(false);
        LoadingPanel.SetActive(false);
        canvas02.SetActive(true);
    }
}
