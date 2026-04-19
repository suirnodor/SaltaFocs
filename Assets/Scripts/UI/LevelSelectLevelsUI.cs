using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelSelectLevelsUI : MonoBehaviour
{
    [Header("Botones de nivel (10 por mundo, en orden)")]
    // Aquí arrastrarás los 10 botones: Nivel 1, Nivel 2, ..., Nivel 10
    public List<Button> levelButtons;

    [Header("Sprites Azul (nivel disponible)")]
    public Sprite blueNormal;
    public Sprite blueHover;
    public Sprite bluePressed;

    [Header("Sprites Dorado (nivel completado)")]
    public Sprite goldNormal;
    public Sprite goldHover;
    public Sprite goldPressed;

    [Header("Sprites Gris (nivel bloqueado)")]
    public Sprite greyNormal;
    public Sprite greyHover;
    public Sprite greyPressed;

    private void Start()
    {
        // Cuando se carga la escena de selección de niveles,
        // actualizamos todos los botones según el progreso guardado.
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.ReloadProgress();

        UpdateLevelButtons();

    }

    /// <summary>
    /// Actualiza el aspecto y el comportamiento de todos los botones de nivel.
    /// - Gris: bloqueado
    /// - Azul: disponible
    /// - Dorado: completado
    /// </summary>
    public void UpdateLevelButtons()
    {
        // Mundo actual (lo controla GameFlowManager)
        int worldIndex = GameFlowManager.Instance.currentWorldIndex;

        // Recorremos todos los botones de la lista
        for (int i = 0; i < levelButtons.Count; i++)
        {
            Button btn = levelButtons[i];
            Image img = btn.GetComponent<Image>();

            // ¿Este nivel está desbloqueado?
            bool unlocked = GameFlowManager.Instance.IsLevelUnlocked(worldIndex, i);

            // ¿Este nivel está completado?
            bool completed = IsLevelCompleted(worldIndex, i);

            if (!unlocked)
            {
                // 🔒 NIVEL BLOQUEADO
                btn.interactable = false;          // No se puede pulsar
                img.sprite = greyNormal;           // Sprite gris normal

                // Configuramos también hover y pressed en gris
                var swap = btn.spriteState;
                swap.highlightedSprite = greyHover;
                swap.pressedSprite = greyPressed;
                btn.spriteState = swap;
            }
            else
            {
                // 🔓 NIVEL DESBLOQUEADO (puede ser completado o el nivel activo)

                if (completed)
                {
                    // ⭐ NIVEL COMPLETADO → DORADO → NO JUGABLE
                    btn.interactable = true;   // No se puede pulsar
                    img.sprite = goldNormal;

                    var swap = btn.spriteState;
                    swap.highlightedSprite = goldNormal;
                    swap.pressedSprite = goldNormal;
                    btn.spriteState = swap;

                    // IMPORTANTE:
                    // NO añadimos listener, porque no queremos que se pueda volver a jugar.
                }
                else
                {
                    // 🔵 NIVEL ACTIVO → AZUL → SÍ JUGABLE
                    btn.interactable = true;    // Este sí se puede pulsar
                    img.sprite = blueNormal;

                    var swap = btn.spriteState;
                    swap.highlightedSprite = blueHover;
                    swap.pressedSprite = bluePressed;
                    btn.spriteState = swap;

                    // ⭐ Listener SOLO para el nivel activo (azul)
                    int levelIndex = i;

                    // Limpiamos listeners anteriores por si acaso
                    btn.onClick.RemoveAllListeners();

                    // Añadimos el listener que cargará el nivel correcto
                    btn.onClick.AddListener(() =>
                    {
                        // Opcional: reproducir sonido de click si quieres
                        if (AudioManager.Instance != null)
                            AudioManager.Instance.PlayUIClick();

                        // Cargar el nivel concreto de este mundo
                        GameFlowManager.Instance.LoadSpecificLevel(worldIndex, levelIndex);
                    });
                }
            }
        }
    }


            /// <summary>
            /// Consideramos un nivel "completado" si:
            /// - Su índice es menor que el nivel más alto desbloqueado en ese mundo.
            /// - O si el mundo es menor que el mundo más alto desbloqueado.
            /// </summary>
            private bool IsLevelCompleted(int worldIndex, int levelIndex)
    {
        int highestWorld = PlayerPrefs.GetInt("HighestWorldUnlocked", 0);
        int highestLevel = PlayerPrefs.GetInt("HighestLevelUnlocked", 0);

        // Si este mundo es menor que el mundo más alto desbloqueado,
        // todos sus niveles se consideran completados.
        if (worldIndex < highestWorld) return true;

        // Si este mundo es mayor que el mundo más alto desbloqueado,
        // ninguno de sus niveles está completado.
        if (worldIndex > highestWorld) return false;

        // Si estamos en el mismo mundo, se consideran completados
        // los niveles con índice menor que el más alto desbloqueado.
        return levelIndex < highestLevel;
    }

    /// <summary>
    /// Botón para volver al menú principal desde el selector de niveles.
    /// </summary>
    public void OnBackToMainMenu()
    {
        // Comprobamos que el GameFlowManager existe (siempre debería existir)
        if (GameFlowManager.Instance != null)
        {
            // Llamamos al método que ya existe en GameFlowManager
            // y que carga la escena MainMenu
            GameFlowManager.Instance.LoadMainMenu();
        }
        else
        {
            Debug.LogError("GameFlowManager no encontrado");
        }
    }

    /// <summary>
    /// Reproduce el sonido de click de la UI.
    /// Llamado desde los botones del selector de niveles.
    /// </summary>
    public void PlayClickSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUIClick();
        }
        else
        {
            Debug.LogError("AudioManager no encontrado");
        }
    }


}
