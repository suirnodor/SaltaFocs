using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class HUDController : MonoBehaviour
{
    // Referencia al texto que muestra el nivel
    public TextMeshProUGUI levelText;

    // Llamado por el LevelManager para actualizar el texto
    public void SetLevel(int level)
    {
        levelText.text = "Nivel " + level;
    }

    // Llamado cuando pulsamos el botón de pausa
    public void OnPauseButton()
    {
        Debug.Log("Botón de pausa pulsado");
        // Más adelante abriremos un menú de pausa
    }
}
