using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VFXColorSetter : MonoBehaviour
{
    // Este método lo llamará el TileController para aplicar el color del mundo
    public void SetColor(Color c)
    {
        // Obtenemos el ParticleSystem del objeto
        ParticleSystem ps = GetComponent<ParticleSystem>();

        if (ps != null)
        {
            // Cambiamos el color inicial de las partículas
            var main = ps.main;
            main.startColor = c;
        }
    }
}
