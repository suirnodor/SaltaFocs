using UnityEngine;

public class DebugMenuButtons : MonoBehaviour
{
    // ⭐ Simular que solo Mundo 1 está desbloqueado (Nivel 1)
    public void GoToWorld1()
    {
        PlayerPrefs.SetInt("HighestWorldUnlocked", 0); // Mundo 1
        PlayerPrefs.SetInt("HighestLevelUnlocked", 0); // Nivel 1
        PlayerPrefs.Save();

        Debug.Log("DEBUG: Mundo 1 desbloqueado, Nivel 1 activo");
        // GameFlowManager.Instance.LoadSpecificLevel(0, 0);
    }

    // ⭐ Simular que Mundo 1 está completado y Mundo 2 desbloqueado
    public void GoToWorld2()
    {
        // Mundo 1 completado (índice 0)
        PlayerPrefs.SetInt("HighestWorldUnlocked", 1); // Mundo 2 desbloqueado
        PlayerPrefs.SetInt("HighestLevelUnlocked", 0); // Nivel 10 del Mundo 1 completado
        PlayerPrefs.Save();

        Debug.Log("DEBUG: Mundo 1 completado, Mundo 2 desbloqueado");
        //GameFlowManager.Instance.LoadSpecificLevel(1, 0);
    }

    // ⭐ Simular que Mundo 1 y 2 están completados y Mundo 3 desbloqueado
    public void GoToWorld3()
    {
        // Mundo 1 y 2 completados
        PlayerPrefs.SetInt("HighestWorldUnlocked", 2); // Mundo 3 desbloqueado
        PlayerPrefs.SetInt("HighestLevelUnlocked", 0); // Nivel 10 del Mundo 2 completado
        PlayerPrefs.Save();

        Debug.Log("DEBUG: Mundo 1 y 2 completados, Mundo 3 desbloqueado");
        // GameFlowManager.Instance.LoadSpecificLevel(2, 0);
    }

    // ⭐ Simular que Mundo 1, 2 y 3 están completados y Mundo 4 desbloqueado
    public void GoToWorld4()
    {
        // Mundo 1, 2 y 3 completados
        PlayerPrefs.SetInt("HighestWorldUnlocked", 3); // Mundo 4 desbloqueado
        PlayerPrefs.SetInt("HighestLevelUnlocked", 0); // Nivel 10 del Mundo 3 completado
        PlayerPrefs.Save();

        Debug.Log("DEBUG: Mundo 1, 2 y 3 completados, Mundo 4 desbloqueado");
        //GameFlowManager.Instance.LoadSpecificLevel(3, 0);
    }
}
