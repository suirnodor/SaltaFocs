using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    // ⭐ NUEVO: textos para mostrar puntuaciones
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI bestScoreText;

    private void Start()
    {
        if (GameFlowManager.Instance != null)
        {
            int score = GameFlowManager.Instance.currentScore;
            int best = GameFlowManager.Instance.bestScore;

            // Mostrar puntuación final
            if (scoreText != null)
                scoreText.text = "Puntuación: " + score;

            // Mostrar mejor puntuación
            if (bestScoreText != null)
                bestScoreText.text = "Mejor puntuación: " + best;
        }
    }

    // Botón REINTENTAR
    public void OnRetry()
    {
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.ReloadCurrentLevel();
    }

    // Botón MENÚ PRINCIPAL
    public void OnMainMenu()
    {
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.LoadMainMenu();
    }

    public void PlayClickSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUIClick();
        }
        else
        {
            Debug.LogError("AudioManager no encontrado en GameOverUI");
        }
    }

}
