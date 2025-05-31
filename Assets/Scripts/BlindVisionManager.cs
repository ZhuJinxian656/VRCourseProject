using UnityEngine;
using UnityEngine.Rendering;

public class BlindModeToggle : MonoBehaviour
{
    [Header("盲人视觉体积组件（后处理）")]
    public Volume blindVisionVolume;

    [Header("盲人遮挡 UI（可选）")]
    public GameObject blindOverlayUI;

    private bool isBlindMode = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            isBlindMode = !isBlindMode;

            // 切换 Volume 效果
            if (blindVisionVolume != null)
                blindVisionVolume.enabled = isBlindMode;

            // 切换 UI 遮罩
            if (blindOverlayUI != null)
                blindOverlayUI.SetActive(isBlindMode);

            Debug.Log("当前视觉模式: " + (isBlindMode ? "盲人视觉" : "正常视觉"));
        }
    }
}