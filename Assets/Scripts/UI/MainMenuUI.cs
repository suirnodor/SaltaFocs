using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    public void OnPlay()
    {
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.LoadWorldSelect();   // 👈 AHORA SÍ
        }
        else
        {
            Debug.LogError("GameFlowManager.Instance es NULL");
        }
    }

    public void OnQuit()
    {
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.QuitGame();
        }
        else
        {
            Debug.LogError("GameFlowManager.Instance es NULL");
        }
    }

    public void PlayClickSound()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayUIClick();
        else
            Debug.LogError("AudioManager no encontrado");
    }

}
