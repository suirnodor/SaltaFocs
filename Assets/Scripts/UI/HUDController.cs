using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class HUDController : MonoBehaviour
{
    // Texto que muestra el nivel actual
    public TextMeshProUGUI levelText;

    // ⭐ NUEVO: Texto que muestra la puntuación actual
    public TextMeshProUGUI scoreText;

    // Panel de pausa (lo activamos/desactivamos)
    public GameObject pausePanel;

    // Referencia al PlayerController
    private PlayerController playerController;

    // Estado interno de pausa
    private bool isPaused = false;

    private void Start()
    {
        // Buscar automáticamente al Player en la escena
        playerController = FindFirstObjectByType<PlayerController>();

        // ⭐ NUEVO: Inicializar puntuación al entrar en el nivel
        if (GameFlowManager.Instance != null && scoreText != null)
        {
            scoreText.text = "Puntuación: " + GameFlowManager.Instance.currentScore;
        }
    }

    // ------------------------------------------------------------
    // ACTUALIZAR TEXTO DEL NIVEL
    // ------------------------------------------------------------
    public void SetLevel(int worldIndex, int levelIndex)
    {
        if (levelText != null)
        {
            // Cálculo del número global de nivel
            int nivelGlobal = (worldIndex * 10) + (levelIndex + 1);

            levelText.text = "Nivel " + nivelGlobal;
        }
    }


    // ------------------------------------------------------------
    // ⭐ NUEVO: ACTUALIZAR TEXTO DE PUNTUACIÓN
    // ------------------------------------------------------------
    public void SetScore(int score)
    {
        if (scoreText != null)
            scoreText.text = "Puntuación: " + score;
    }

    // ------------------------------------------------------------
    // BOTÓN DE PAUSA DEL HUD
    // ------------------------------------------------------------
    public void OnPauseButton()
    {
        if (!isPaused)
        {
            isPaused = true;

            // Congela el tiempo
            Time.timeScale = 0f;

            // Desactivar controles del jugador
            if (playerController != null)
                playerController.DisableControls();

            // Pausar enemigos
            EnemyController[] enemies = FindObjectsOfType<EnemyController>();
            foreach (var e in enemies)
                e.isPaused = true;

            // Mostrar panel de pausa
            if (pausePanel != null)
                pausePanel.SetActive(true);
        }
        else
        {
            ResumeGame();
        }
    }

    // ------------------------------------------------------------
    // BOTÓN "CONTINUAR" DEL MENÚ DE PAUSA
    // ------------------------------------------------------------
    public void OnResumeButton()
    {
        ResumeGame();
    }

    // ------------------------------------------------------------
    // REANUDAR EL JUEGO
    // ------------------------------------------------------------
    private void ResumeGame()
    {
        isPaused = false;

        // Reactivar tiempo
        Time.timeScale = 1f;

        // Reactivar controles del jugador
        if (playerController != null)
            playerController.EnableControls();

        // Reactivar enemigos
        EnemyController[] enemies = FindObjectsOfType<EnemyController>();
        foreach (var e in enemies)
            e.isPaused = false;

        // Ocultar panel de pausa
        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    // Método mínimo para reproducir el sonido de click UI usando el AudioManager existente
    public void PlayClickSfx()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayUIClick();
    }


}
