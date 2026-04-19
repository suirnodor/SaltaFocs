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

    // Indica si el jugador está actualmente sobre un ascensor
    public bool isOnElevator = false;


    // Offset vertical del jugador para que quede encima del tile 
    // Es constante → siempre tendrá este valor en todo el juego
    public const float PLAYER_OFFSET_Y = 1.1f;


    // Aquí guardaremos el LevelManager UNA sola vez
    // Así no tenemos que buscarlo cada vez
    private LevelManager lm;


    // Altura del salto (ajustable)
    public float jumpHeight = 0.5f;

    // Intensidad del squash & stretch
    public float squashAmount = 0.1f;

    // Referencias a los sprites del jugador (los hijos)
    //public Transform spriteFront;
    //public Transform spriteBack;

    //escala original de los sprites del jugador (los hijos)
    //private Vector3 spriteFrontOriginalScale;
    //private Vector3 spriteBackOriginalScale;


    // Referencia al Animator del Player (para controlar las animaciones)
    private Animator animator;


    // Tiempo quieto antes de activar Idle especial
    public float idleSpecialDelay = 3f;

    // Temporizador interno
    private float idleTimer = 0f;

    // ------------------------------------------------------------
    // Variables para detectar gestos táctiles (swipe)
    // ------------------------------------------------------------
    private Vector2 touchStartPos;
    private Vector2 touchEndPos;
    private bool isTouching = false;


    // Volumen del SFX de salto (0.0 - 1.0). Ajustable desde el Inspector del prefab Player.
    [Header("Audio")]
    public float jumpSfxVolume = 0.6f;

    private ElevatorCube elevator; // referencia al ascensor actual




    // Esta función se llama desde el LevelManager para decirle al jugador
    // en qué tile empieza.
    public void Init(Vector2Int startCoords)
    {
        // Guardamos las coordenadas iniciales del jugador en el tablero
        currentTileCoords = startCoords;

    }


    // Awake se ejecuta cuando el objeto aparece en la escena.
    private void Awake()
    {
        // Creamos el sistema de controles
        controls = new PlayerInputActions();

        // Guardamos el LevelManager en la variable lm 
        // Esto se ejecuta solo una vez al iniciar el jugador
        //lm = FindObjectOfType<LevelManager>(); ********* me daba error por eso lo sustotuimos por el codigo de debajo
        lm = FindFirstObjectByType<LevelManager>();


        // Referencia al Animator (está en un hijo del Player)
        animator = GetComponentInChildren<Animator>();


        //guardamos las escales de lo sprites del player hijos en las variables creadas
        //spriteFrontOriginalScale = spriteFront.localScale;
        //spriteBackOriginalScale = spriteBack.localScale;

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


    // ------------------------------------------------------------
    // NUEVO: Detectar dirección del swipe y mover al jugador
    // ------------------------------------------------------------
    private void HandleSwipeDirection()
    {
        Vector2 swipe = touchEndPos - touchStartPos;

        // Si el swipe es muy pequeño, ignoramos
        if (swipe.magnitude < 50f)
            return;

        // Normalizamos para obtener dirección
        swipe.Normalize();

        int dx = 0;
        int dy = 0;

        // Tolerancia para decidir dirección
        float tolerance = 0.4f;

        // Horizontal
        if (swipe.x > tolerance) dx = 1;
        else if (swipe.x < -tolerance) dx = -1;

        // Vertical
        if (swipe.y > tolerance) dy = 1;
        else if (swipe.y < -tolerance) dy = -1;

        // Si no hay dirección clara, no mover
        if (dx == 0 && dy == 0)
            return;

        // Ejecutar movimiento
        TryMove(new Vector2Int(dx, dy));
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
        // Si el jugador está moviéndose, reiniciamos el temporizador
        // y nos aseguramos de que NO está en idle especial
        if (isMoving)
        {
            idleTimer = 0f;
            animator.SetBool("IsIdleSpecial", false);
            return;
        }

        // Si NO se mueve, sumamos tiempo
        idleTimer += Time.deltaTime;

        // Si supera el tiempo configurado → activar idle especial
        if (idleTimer >= idleSpecialDelay)
        {
            animator.SetBool("IsIdleSpecial", true);
        }
    }




    // Intenta mover al jugador en una dirección.
    private void TryMove(Vector2Int direction)
    {
        Debug.Log($"TryMove called dir={direction} isMoving={isMoving} isDead={isDead} time={Time.time}");

        // Evitar procesar input si ya estamos moviéndonos o si estamos muertos/caídos
        //if (isMoving || isDead)
        //    return;
        if (isDead)
            return;

        if (isOnElevator)
        {
            // ⭐ Caso 1: ascensor central → permitir izquierda y derecha
            if (elevator.isCenter)
            {
                if (direction != Vector2Int.left && direction != Vector2Int.right)
                    return;

                isOnElevator = false;

                if (elevator != null)
                    elevator.StartMoveDown();

                // permitir movimiento normal
            }
            else
            {
                // ⭐ Caso 2: ascensor lateral → solo salida hacia el lado correcto
                Vector2Int exitDirection = elevator.isLeftSide ? Vector2Int.right : Vector2Int.left;

                if (direction != exitDirection)
                    return;

                isOnElevator = false;

                if (elevator != null)
                    elevator.StartMoveDown();
            }
        }




        // En cuanto el jugador intenta moverse, salimos del idle especial
        animator.SetBool("IsIdleSpecial", false);
        idleTimer = 0f;


        // Calculamos las coordenadas del tile al que queremos ir.
        Vector2Int targetCoords = currentTileCoords + direction;

        // Preguntamos al LevelManager si existe un tile en esa posición.
        //if (FindObjectOfType<LevelManager>().HasTile(targetCoords)) *** me daba error por eso lo cambiamos por el codigo de abajo
        if (lm.HasTile(targetCoords))
        {
            // Como el movimiento es válido, reproducimos el sonido de salto.
            // Llamamos al AudioManager y le pedimos que reproduzca el clip de salto.
            // Reproducir salto a lo indicado en la variable publica jumpSfxVolume
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(AudioManager.Instance.jumpClip, jumpSfxVolume);



            // Lanzamos animación de salto completo
            animator.SetTrigger("Jump");


            // Ahora sí, movemos al jugador hacia el tile destino.
            MoveTo(targetCoords);
        }
        else
        {
            // No hay tile → antes de caer, comprobamos si hay un ascensor en esa posición lógica

            // 1) Convertimos las coords lógicas a posición de mundo (igual que en JumpOutAndFall)
            Vector3 worldTargetPos = new Vector3(
                targetCoords.x,
                transform.position.y,   // misma altura actual
                targetCoords.y
            );

            // 2) Buscamos colliders alrededor de ese punto
            float checkRadius = 0.2f; // pequeño radio de búsqueda
            Collider[] hits = Physics.OverlapSphere(worldTargetPos, checkRadius);

            foreach (Collider hit in hits)
            {
                ElevatorCube elevator = hit.GetComponent<ElevatorCube>();
                if (elevator != null)
                {
                    // Hemos encontrado un ascensor en la dirección del salto

                    // Marcamos que estamos sobre un ascensor
                    isOnElevator = true;

                    // Hacemos un salto REAL hacia el ascensor
                    StartCoroutine(JumpToElevator(elevator));
                    return;

                }
            }

            // 3) Si no hay ni tile ni ascensor → si estamos en ascensor, no caer
            if (isOnElevator)
                return;

            // Si no estamos en ascensor → caída normal
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
        Debug.Log($"MoveRoutine START target={targetCoords} time={Time.time}");

        isMoving = true; // Bloqueamos el movimiento.

        // Lanzamos animación de salto
        //animator.SetTrigger("Jump");


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
            //float stretch = 1 + squashAmount * (1 - Mathf.Abs(lerp * 2 - 1));

            // Aplicamos la escala SOLO a los sprites visibles
            //spriteFront.localScale = new Vector3(
            //    spriteFrontOriginalScale.x,
            //    spriteFrontOriginalScale.y * stretch,
            //    spriteFrontOriginalScale.z
            //);

            //spriteBack.localScale = new Vector3(
            //    spriteBackOriginalScale.x,
            //    spriteBackOriginalScale.y * stretch,
            //    spriteBackOriginalScale.z
            //);


            yield return null;
        }

        // Aseguramos que termina exactamente en la posición final.
        transform.position = endPos;

        // Restauramos escala normal en los sprites
        //spriteFront.localScale = spriteFrontOriginalScale;
        //spriteBack.localScale = spriteBackOriginalScale;


        // Avisamos al tile que ha sido pisado.
        TileController tile = lm.GetTile(targetCoords);
        tile.OnStepped(); // ← ESTA LÍNEA ES OBLIGATORIA

        // Comprobamos si ya hemos ganado (todos los tiles pisados).
        if (lm.CheckVictory())
        {
            // Bloqueamos el movimiento y el input para que el jugador no pueda seguir moviéndose
            isMoving = true;
            controls.Disable();

            // Lanzamos animación de victoria
            animator.SetTrigger("Win");

            // Esperamos un poco para que la animación se vea completa
            yield return new WaitForSeconds(1.5f);   // Ajusta el tiempo si quieres

            // Ahora sí, cambiamos a la escena Victory
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.OnLevelCompleted();
            }
            else
            {
                Debug.LogError("GameFlowManager.Instance es NULL. Asegúrate de que hay un GameFlowManager en la escena inicial.");
            }

            yield break;
        }



        //else
        //{
        // Animación de aterrizaje
        //animator.SetTrigger("JumpLand");
        //}


        isMoving = false; // Permitimos nuevos movimientos.

        Debug.Log($"MoveRoutine END target={targetCoords} time={Time.time}");

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

        

        // Desactivar controles para que no lleguen más inputs mientras caemos
        controls.Disable();


        Debug.Log("FallAndDie() HA SIDO LLAMADO y estamos en PlayerController"); // ← MENSAJE DE PRUEBA

        // Reproducir sonido de muerte
        AudioManager.Instance.PlaySFX(AudioManager.Instance.deathClip);

        isMoving = true; // Bloqueamos el movimiento.


        Vector3 startPos = transform.position;

        // Caída de 20 unidades hacia abajo (no infinita).
        Vector3 endPos = startPos + Vector3.down * 20f;

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

        // Esperamos un momento y vamos a GameOver
        yield return new WaitForSeconds(1f);

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.OnPlayerDied();
        }
        else
        {
            Debug.LogError("GameFlowManager.Instance es NULL");
        }

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
        animator.SetTrigger("Hit");


        // Esperamos un poco para que se vea la animación de muerte
        yield return new WaitForSeconds(0.8f);

        // Ir a GameOver
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.OnPlayerDied();
        }
        else
        {
            Debug.LogError("GameFlowManager.Instance es NULL");
        }

    }



    //*********ya no se utiliza la subrutitna RestarAfterWin y la ponemos en comentarios
    // Corrutina para reiniciar la escena después de ganar.
    //private IEnumerator RestartAfterWin()
    //{
    // Esperamos un segundo para que el jugador vea que ha ganado.
    //  yield return new WaitForSeconds(1f);

    // Reiniciamos la escena actual.
    //SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    //}
    //********************hasta aqui subrutina  RestarAfterWin



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

        // Lanzamos animación de caída
        animator.SetTrigger("Fall");


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


    private IEnumerator JumpToElevator(ElevatorCube elevator)
    {
        isMoving = true;

        animator.SetTrigger("Jump");

        Vector3 startPos = transform.position;
        Vector3 endPos = elevator.transform.position + Vector3.up * PLAYER_OFFSET_Y;

        float t = 0f;
        float duration = moveDuration;

        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = t / duration;

            Vector3 pos = Vector3.Lerp(startPos, endPos, lerp);
            float arc = 4 * jumpHeight * lerp * (1 - lerp);
            pos.y += arc;

            transform.position = pos;

            yield return null;
        }

        // Aseguramos posición final exacta
        transform.position = endPos;

        isMoving = false;


        this.elevator = elevator; // guardamos referencia al ascensor actual

        // ⬇️ NUEVO: decirle al ascensor que empiece a subir con este jugador
        elevator.StartElevatorWithPlayer(this);
    }



    // ------------------------------------------------------------
    // MÉTODOS PARA ACTIVAR / DESACTIVAR CONTROLES DEL JUGADOR
    // ------------------------------------------------------------

    // Llamado cuando el juego entra en pausa
    public void DisableControls()
    {
        // Desactiva el Input System → el jugador NO recibe teclas
        controls.Disable();
    }

    // Llamado cuando el juego sale de pausa
    public void EnableControls()
    {
        // Reactiva el Input System → el jugador vuelve a recibir teclas
        controls.Enable();
    }

    // ------------------------------------------------------------
    // MÉTODO PARA PlayerInput: acción Tap (pantalla táctil)
    // ------------------------------------------------------------
    public void OnTap(InputAction.CallbackContext ctx)
    {
        // Mensaje de depuración para saber si OnTap se está llamando
        Debug.Log($"OnTap llamado. Phase = {ctx.phase}");

        // Usamos el NUEVO Input System para leer el dedo
        var touchscreen = Touchscreen.current;

        // Si no hay pantalla táctil disponible, salimos
        if (touchscreen == null)
        {
            Debug.Log("No hay Touchscreen.current");
            return;
        }

        // Cuando empieza el toque
        if (ctx.started)
        {
            // Leemos la posición del dedo en el momento de empezar
            touchStartPos = touchscreen.primaryTouch.position.ReadValue();
            isTouching = true;
            Debug.Log($"TOQUE INICIADO en {touchStartPos}");
        }

        // Cuando termina el toque
        if (ctx.canceled)
        {
            // Leemos la posición del dedo al terminar
            touchEndPos = touchscreen.primaryTouch.position.ReadValue();
            isTouching = false;
            Debug.Log($"TOQUE TERMINADO en {touchEndPos}");

            HandleSwipeDirection();
        }
    }




}
