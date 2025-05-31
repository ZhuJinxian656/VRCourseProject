using UnityEngine;
using UnityEngine.Rendering;

public class BlindModeToggle : MonoBehaviour
{
    [Header("ä���Ӿ�������������")]
    public Volume blindVisionVolume;

    [Header("ä���ڵ� UI����ѡ��")]
    public GameObject blindOverlayUI;

    private bool isBlindMode = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            isBlindMode = !isBlindMode;

            // �л� Volume Ч��
            if (blindVisionVolume != null)
                blindVisionVolume.enabled = isBlindMode;

            // �л� UI ����
            if (blindOverlayUI != null)
                blindOverlayUI.SetActive(isBlindMode);

            Debug.Log("��ǰ�Ӿ�ģʽ: " + (isBlindMode ? "ä���Ӿ�" : "�����Ӿ�"));
        }
    }
}