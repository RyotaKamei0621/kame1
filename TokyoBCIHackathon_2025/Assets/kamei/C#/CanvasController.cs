using UnityEngine;

public class CanvasController : MonoBehaviour
{
    [SerializeField] private GameObject canvas01;
    [SerializeField] private GameObject canvas02;

    void Start()
    {
        // 最初はCanvas01のみ表示
        canvas01.SetActive(true);
        canvas02.SetActive(false);
    }

    // 2秒後などに呼び出す想定
    public void SwitchToCanvas02()
    {
        canvas01.SetActive(false);
        canvas02.SetActive(true);
    }
}
