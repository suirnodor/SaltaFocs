using System.Collections;                  // Necesario para usar corrutinas (IEnumerator)
using System.Collections.Generic;          // (No lo usamos aún, pero no molesta)
using UnityEngine;                         // Necesario para MonoBehaviour, Vector3, etc.
using UnityEngine.InputSystem;             // Necesario para el NUEVO Input System
using UnityEngine.SceneManagement;         // Necesario para reiniciar la escena


public class PlayerController : MonoBehaviour
{
    // Guarda la posición lógica del jugador en el tablero (no la posición 3D).
    // Ejemplo: (0,0) significa primer tile, (1,2) significa columna 1, fila 2.
    public Vector2Int currentTileCoords;

    // Tiempo que tarda el jugador en moverse de un tile a otro.
    public float moveDuration = 0.2f;

    // Indica si el jugador está moviéndose ahora mismo.
    // Esto evita que se mueva dos veces a la vez.
    private bool isMoving = false;

    // Referencia al asset de Input Actions generado automáticamente
    // (PlayerInputActions.inputactions → Generate C# Class).
    private PlayerInputActions controls;

    // Nueva variable para evitar que FallAndDie se ejecute muchas veces
    private bool isDead = false;

    // Offset vertical del jugador para que quede encima del tile 
    // Es constante → siempre tendrá este valor en todo el juego
    public const float PLAYER_OFFSET_Y = 1.7f;


    // Aquí guardaremos el LevelManager UNA sola vez
    // Así no tenemos que buscarlo cada vez
    private LevelManager lm;


    // Altura del salto (ajustable)
    public float jumpHeight = 0.5f;

    // Intensidad del squash & stretch
    public float squashAmount = 0.1f;

    // Referencias a los sprites del jugador (los hijos)
    public Transform spriteFront;
    public Transform spriteBack;

    //escala original de los sprites del jugador (los hijos)
    private Vector3 spriteFrontOriginalScale;
    private Vector3 spriteBackOriginalScale;



    // Esta función se llama desde el LevelManager para decirle al jugador
    // en qué tile empieza.
    public void Init(Vector2Int startCoords)
    {
        currentTileCoords = startCoords;
    }


    // Awake se ejecuta cuando el objeto aparece en la escena.
    private void Awake()
    {
        // Creamos el sistema de controles
        controls = new PlayerInputActions();

        // Guardamos el LevelManager en la variable lm 
        // Esto se ejecuta solo una vez al iniciar el jugador
        lm = FindObjectOfType<LevelManager>();

        //guardamos las escales de lo sprites del player hijos en las variables creadas
        spriteFrontOriginalScale = spriteFront.localScale;
        spriteBackOriginalScale = spriteBack.localScale;

    }


    // OnEnable se ejecuta cuando el objeto se activa.
    private void OnEnable()
    {
        // Activamos el sistema de controles.
        controls.Enable();

        // Conectamos cada acción del Input System con un movimiento.
        // Estas acciones vienen de tu PlayerInputActions (mapa "Gameplay").

        // Diagonales:
        controls.Gameplay.MoveUpLeft.performed += ctx =>
        {
            TryMove(new Vector2Int(-1, 1));   // arriba-izquierda
        };

        controls.Gameplay.MoveUpRight.performed += ctx =>
        {
            TryMove(new Vector2Int(1, 1));    // arriba-derecha
        };

        controls.Gameplay.MoveDownLeft.performed += ctx =>
        {
            TryMove(new Vector2Int(-1, -1));  // abajo-izquierda
        };

        controls.Gameplay.MoveDownRight.performed += ctx =>
        {
            TryMove(new Vector2Int(1, -1));   // abajo-derecha
        };

        // Ortogonales (WASD):
        controls.Gameplay.MoveUp.performed += ctx =>
        {
            TryMove(Vector2Int.up);           // arriba
        };

        controls.Gameplay.MoveDown.performed += ctx =>
        {
            TryMove(Vector2Int.down);         // abajo
        };

        controls.Gameplay.MoveLeft.performed += ctx =>
        {
            TryMove(Vector2Int.left);         // izquierda
        };

        controls.Gameplay.MoveRight.performed += ctx =>
        {
            TryMove(Vector2Int.right);        // derecha
        };
    }


    // OnDisable se ejecuta cuando el objeto se desactiva.
    private void OnDisable()
    {
        // Desactivamos el sistema de controles.
        controls.Disable();
    }


    // Update YA NO SE USA para leer teclas.
    // El nuevo Input System llama a las acciones automáticamente.
    private void Update()
    {
        // Si el jugador está moviéndose, no aceptamos nuevas teclas.
        if (isMoving) return;
    }


    // Intenta mover al jugador en una dirección.
    private void TryMove(Vector2Int direction)
    {
        // Calculamos las coordenadas del tile al que queremos ir.
        Vector2Int targetCoords = currentTileCoords + direction;

        // Preguntamos al LevelManager si existe un tile en esa posición.
        if (FindObjectOfType<LevelManager>().HasTile(targetCoords))
        {
            // Como el movimiento es válido, reproducimos el sonido de salto.
            // Llamamos al AudioManager y le pedimos que reproduzca el clip de salto.
            AudioManager.Instance.PlaySFX(AudioManager.Instance.jumpClip);

            // Ahora sí, movemos al jugador hacia el tile destino.
            MoveTo(targetCoords);
        }
        else
        {
            // No hay tile → salto hacia fuera y luego caída
            StartCoroutine(JumpOutAndFall(targetCoords));
        }

    }



    // Actualiza las coordenadas lógicas y empieza la animación de movimiento.
    private void MoveTo(Vector2Int targetCoords)
    {
        // Actualizamos la posición lógica del jugador en el tablero.
        currentTileCoords = targetCoords;

        // Empezamos el movimiento suave hacia ese tile.
        StartCoroutine(MoveRoutine(targetCoords));
    }




    // Corrutina que mueve al jugador suavemente durante moveDuration segundos.
    private IEnumerator MoveRoutine(Vector2Int targetCoords)
    {
        isMoving = true; // Bloqueamos el movimiento.

        // Guardamos la posición actual del jugador
        Vector3 startPos = transform.position;

        // Obtenemos el tile al que queremos movernos 
        TileController targetTile = lm.GetTile(targetCoords);

        // Calculamos la posición final sumando el offset vertical
        Vector3 endPos = targetTile.transform.position + Vector3.up * PLAYER_OFFSET_Y;

        float t = 0;

        // --- NUEVO: parámetros del salto ---
        // (Puedes ajustar estos valores desde el Inspector)
        // jumpHeight y squashAmount ya los tienes declarados arriba
        // así que no hace falta declararlos aquí otra vez.

        // Movimiento suave usando Lerp.
        while (t < moveDuration)
        {
            t += Time.deltaTime;
            float lerp = t / moveDuration;

            // --- 1) Movimiento horizontal (X y Z) ---
            Vector3 horizontalPos = Vector3.Lerp(startPos, endPos, lerp);

            // --- 2) Arco vertical (Y) ---
            float arc = 4 * jumpHeight * lerp * (1 - lerp);
            horizontalPos.y += arc;

            // Aplicamos la posición final del frame
            transform.position = horizontalPos;

            // --- 3) Squash & Stretch (simulación de animación) ---
            float stretch = 1 + squashAmount * (1 - Mathf.Abs(lerp * 2 - 1));

            // Aplicamos la escala SOLO a los sprites visibles
            spriteFront.localScale = new Vector3(
                spriteFrontOriginalScale.x,
                spriteFrontOriginalScale.y * stretch,
                spriteFrontOriginalScale.z
            );

            spriteBack.localScale = new Vector3(
                spriteBackOriginalScale.x,
                spriteBackOriginalScale.y * stretch,
                spriteBackOriginalScale.z
            );


            yield return null;
        }

        // Aseguramos que termina exactamente en la posición final.
        transform.position = endPos;

        // Restauramos escala normal en los sprites
        spriteFront.localScale = spriteFrontOriginalScale;
        spriteBack.localScale = spriteBackOriginalScale;


        // Avisamos al tile que ha sido pisado.
        TileController tile = lm.GetTile(targetCoords);
        tile.OnStepped(); // ← ESTA LÍNEA ES OBLIGATORIA

        // Comprobamos si ya hemos ganado (todos los tiles pisados).
        if (lm.CheckVictory())
        {
            StartCoroutine(RestartAfterWin());
        }

        isMoving = false; // Permitimos nuevos movimientos.
    }



    // Corrutina que hace caer al jugador cuando no hay tile debajo.
    // IMPORTANTE:
    // - El jugador NO cae hasta el infinito.
    // - Solo baja 5 unidades hacia abajo (Vector3.down * 5f).
    // - Esto crea una animación de caída controlada.
    // - Después reiniciamos el nivel.
    // Nueva variable para evitar que FallAndDie se ejecute muchas veces

    private IEnumerator FallAndDie()
    {
        // Si ya estamos muertos, no volvemos a ejecutar la animación
        if (isDead)
        {
            Debug.Log("FallAndDie() NO se ejecuta porque el jugador ya está muerto");
            yield break; // ← Salimos del método
        }

        // Marcamos que el jugador ya ha muerto (evita múltiples ejecuciones)
        isDead = true;

        Debug.Log("FallAndDie() HA SIDO LLAMADO y estamos en PlayerController"); // ← MENSAJE DE PRUEBA

        // Reproducir sonido de muerte
        AudioManager.Instance.PlaySFX(AudioManager.Instance.deathClip);

        isMoving = true; // Bloqueamos el movimiento.

        Vector3 startPos = transform.position;

        // Caída de 5 unidades hacia abajo (no infinita).
        Vector3 endPos = startPos + Vector3.down * 5f;

        float t = 0f;
        float duration = 0.5f; // Duración de la caída.

        // Animación de caída
        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = t / duration;
            transform.position = Vector3.Lerp(startPos, endPos, lerp);
            yield return null;
        }

        Debug.Log("Jugador muerto por caída");

        // Esperamos un momento y reiniciamos la escena.
        yield return new WaitForSeconds(1f);

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }


    // ------------------------------------------------------------
    // NUEVA CORRUTINA: muerte por enemigo (sin atravesar el cubo)
    // ------------------------------------------------------------
    public IEnumerator DeathByEnemy()
    {
        // Si ya estamos muertos, no volvemos a ejecutar la animación
        if (isDead)
        {
            Debug.Log("DeathByEnemy() NO se ejecuta porque el jugador ya está muerto");
            yield break; // ← Salimos del método
        }

        // Marcamos que el jugador ya ha muerto (evita múltiples ejecuciones)
        isDead = true;
        isMoving = true; // Bloqueamos el movimiento del jugador

        // ⬇️ BLOQUEAMOS EL INPUT SYSTEM
        controls.Disable();

        Debug.Log("Jugador muerto por ENEMIGO");

        // Reproducir sonido de muerte (puedes usar el mismo que la caída)
        AudioManager.Instance.PlaySFX(AudioManager.Instance.deathClip);

        // ⬇️ Aquí más adelante lanzaremos la animación HIT del Animator
        // Ejemplo futuro:
        // animator.SetTrigger("Hit");

        // Esperamos un poco para que se vea la animación de muerte
        yield return new WaitForSeconds(0.8f);

        // Reiniciamos la escena
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }




    // Corrutina para reiniciar la escena después de ganar.
    private IEnumerator RestartAfterWin()
    {
        // Esperamos un segundo para que el jugador vea que ha ganado.
        yield return new WaitForSeconds(1f);

        // Reiniciamos la escena actual.
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void SetLevelManager(LevelManager manager)
    {
        lm = manager;
    }


    // ------------------------------------------------------------
    // NUEVA CORRUTINA: salto hacia un tile inexistente y luego caída
    // ------------------------------------------------------------
    private IEnumerator JumpOutAndFall(Vector2Int targetCoords)
    {
        isMoving = true; // Bloqueamos movimiento

        // Posición actual del jugador
        Vector3 startPos = transform.position;

        // Posición "ficticia" donde estaría el tile (aunque no exista)
        Vector3 fakeEndPos = new Vector3(
            targetCoords.x,
            startPos.y,          // misma altura inicial
            targetCoords.y
        );

        float t = 0f;
        float duration = moveDuration; // mismo tiempo que un salto normal

        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = t / duration;

            // Movimiento horizontal hacia fuera
            Vector3 pos = Vector3.Lerp(startPos, fakeEndPos, lerp);

            // Arco vertical (igual que un salto normal)
            float arc = 4 * jumpHeight * lerp * (1 - lerp);
            pos.y += arc;

            transform.position = pos;

            yield return null;
        }

        // Cuando termina el salto hacia fuera → empieza la caída real
        StartCoroutine(FallAndDie());
    }


}
