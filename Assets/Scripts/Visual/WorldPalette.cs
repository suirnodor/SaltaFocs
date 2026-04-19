using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Este ScriptableObject guarda los colores de un mundo.
[CreateAssetMenu(fileName = "WorldPalette", menuName = "SaltaFocs/World Palette")]
public class WorldPalette : ScriptableObject
{
    [Header("Tiles")]
    public Color tileBaseColor;      // Color del tile sin pisar
    public Color tileTargetColor;    // Color del tile pisado (objetivo)

    [Header("Enemigos")]
    public Color enemyColor;         // Color principal de los enemigos

    [Header("Iluminación")]
    public Color ambientColor;       // Luz ambiental del mundo

    [Header("UI (opcional futuro)")]
    public Color uiAccentColor;      // Color de acento para UI (botones, etc.)
}

