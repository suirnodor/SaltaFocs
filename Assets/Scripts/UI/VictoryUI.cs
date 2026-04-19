using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class VictoryUI : MonoBehaviour
{
    // ⭐ NUEVO: textos que mostraremos en la pantalla de victoria
    public TextMeshProUGUI scoreText;      // Puntuación final
    public TextMeshProUGUI bestScoreText;  // Mejor puntuación guardada
    public TextMeshProUGUI bonusText;      // Explicación del bonus

    private void Start()
    {
        // Asegurarnos de que existe el GameFlowManager
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

            // ⭐ Mostrar explicación del bonus
            if (bonusText != null)
                bonusText.text = "Bonus por completar el nivel: +10 puntos";
        }
    }

    // ------------------------------
    // BOTONES (los mismos que ya tenías)
    // ------------------------------

    public void OnBackToLevelSelect()
    {
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.LoadLevelSelect();
    }

    public void OnNextWorld()
    {
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.LoadNextWorld();
    }

    public void OnNextLevel()
    {
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.LoadNextLevel();
    }

    public void PlayClickSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUIClick();
        }
        else
        {
            Debug.LogError("AudioManager no encontrado en VictoryUI");
        }
    }

}
