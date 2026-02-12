using UnityEngine;

public class TileController : MonoBehaviour
{
    // Indica si este tile ya ha sido pisado
    public bool isChanged = false;

    // Color inicial del tile
    public Color baseColor = Color.white;

    // Color cuando el jugador lo pisa
    public Color targetColor = Color.yellow;

    // Referencia al Renderer para cambiar el color del material
    private Renderer rend;



    private void Awake()
    {
        // Guardamos el renderer del tile
        rend = GetComponent<Renderer>();

        // Al iniciar, el tile tiene su color base
        rend.material.color = baseColor;
    }

    // Esta función se llama cuando el jugador pisa el tile
    public void OnStepped()
    {
        // Si ya estaba cambiado, no hacemos nada
        if (!isChanged)
        {
            isChanged = true; // Marcamos como pisado

            // Cambiamos el color del material
            rend.material.color = targetColor;

            // Reproducimos el sonido al pisar el tile
            // Llamamos al AudioManager y le pedimos que reproduzca el sonido tileClip
            AudioManager.Instance.PlaySFX(AudioManager.Instance.tileClip);

            // Más adelante avisaremos al LevelManager para comprobar victoria
        }
    }

}
