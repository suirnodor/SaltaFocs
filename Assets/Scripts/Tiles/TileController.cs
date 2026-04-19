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

    // nivel de altura del tile (opcional)
    public int heightLevel = 0;

    // Prefab del VFX al pisar el tile
    public GameObject stepVFX;


    // Devuelve una versión más brillante de un color
    private Color Brighter(Color c, float amount = 0.4f)
    {
        return new Color(
            Mathf.Clamp01(c.r + amount),
            Mathf.Clamp01(c.g + amount),
            Mathf.Clamp01(c.b + amount),
            1f
        );
    }



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

            // Instanciar el VFX de activación del tile adaptado a la altura real
            if (stepVFX != null)
            {
                GameObject vfxInstance = Instantiate(
                    stepVFX,
                    new Vector3(
                        transform.position.x,
                        transform.position.y + 1.2f, //altura excta del VFX sobre el tile
                        transform.position.z
                    ),
                    Quaternion.identity
                );

                // Cambiar el color del VFX según el color objetivo del tile, pero más brillante
                VFXColorSetter setter = vfxInstance.GetComponent<VFXColorSetter>();
                if (setter != null)
                {
                    // Usamos una versión más brillante del color del tile
                    Color vfxColor = Brighter(targetColor, 0.4f);
                    setter.SetColor(vfxColor);
                }


            }



            // Reproducimos el sonido al pisar el tile
            // Llamamos al AudioManager y le pedimos que reproduzca el sonido tileClip
            AudioManager.Instance.PlaySFX(AudioManager.Instance.tileClip);


            // NUEVO: sumar 1 punto por cubo cambiado
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.AddScore(1);
            }

            // Más adelante avisaremos al LevelManager para comprobar victoria
        }
    }


    // Esta función la llama el LevelManager para aplicar los colores de la paleta del mundo
    public void ApplyPaletteColors(Color baseCol, Color targetCol)
    {
        // Guardamos los colores nuevos
        baseColor = baseCol;
        targetColor = targetCol;

        // Si el tile no ha sido pisado, debe verse con el color base
        if (!isChanged)
        {
            if (rend == null)
                rend = GetComponent<Renderer>();

            rend.material.color = baseColor;
        }
    }


}
