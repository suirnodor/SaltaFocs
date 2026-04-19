using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{

    public enum EnemyAIType
    {
        IA_BASICA,
        IA_ZIGZAG,
        IA_PERSEGUIDORA,
        IA_EXPLORADORA,
        IA_IMPREDECIBLE,
        IA_SUBE_JUGADOR,
        IA_CAMBIO_VELOCIDAD,
        IA_ESTADOS,
        IA_OMNI
    }

    public Vector2Int currentTileCoords;
    public float speed = 4f;
    public bool isMoving = false;
    public LevelManager levelManager;
    public Vector2Int direction = new Vector2Int(1, -1);
    public bool toggle = false;
    public GameObject stepParticlesPrefab;
    public GameObject spawnParticlesPrefab;   // NUEVO: partículas de aparición


    public const float enemyOffsetY = 1.0f;

    private LevelManager lm;
    //public LevelManager levelmanager; esta linea se tienen que eliminar no se usa y esta doblada con otra

    public float jumpHeight = 0.4f;

    private Vector3 originalScale;

    private bool hasKilledPlayer = false;

    public bool isPaused = false;

    // NUEVO: referencia al Animator
    private Animator animator;

    // NUEVO: referencia al SpriteRenderer para poder hacer flipX
    private SpriteRenderer spriteRenderer;

    // Lista de TODOS los SpriteRenderer del enemigo
    private SpriteRenderer[] allSpriteRenderers;


    // NUEVO: recordar el último tile para evitar volver atrás
    private Vector2Int lastTileCoords;

    // NUEVO: lista de últimos tiles visitados para la IA exploradora
    private List<Vector2Int> recentTiles = new List<Vector2Int>();

    // Cuántos tiles recientes recordamos (por ejemplo, los últimos 20)
    private int recentTilesMemorySize = 20;


    //Añadir el tipo de IA
    public EnemyAIType aiType = EnemyAIType.IA_BASICA;

    //offset partoculas aparición para ajustarlas en altura
    [Header("particulas Aparición")]
    public float spawnOffsetY = 1.0f;   // Ajustable desde el Inspector

    //offset particulas petardo enemigo básico para ajustarlas en altura
    [Header("Partículas petardo enemigo básico")]
    public float stepParticlesOffsetY = 0.5f;   // Ajustable desde el inspector


    // Si está activado, la animación de salto se acelera según la velocidad (speed)
    // Lo usaremos SOLO en algunos prefabs (por ejemplo, el perseguidor)
    [Header("Opciones de animación")]
    public bool scaleAnimationWithSpeed = false;


    [Header("Caída al vacío")]
    public float fallSpeed = 4f;     // Velocidad de caída
    public float fallDuration = 1f;  // Duración de la caída


    private void Start()
    {
        // Guardamos la escala original
        originalScale = transform.localScale;

        // Animator del mismo GameObject
        animator = GetComponent<Animator>();

        // Obtener TODOS los SpriteRenderer del enemigo (incluidos hijos)
        allSpriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        // Obtener el principal (el primero)
        if (allSpriteRenderers.Length > 0)
            spriteRenderer = allSpriteRenderers[0];
    }


    public void Init(Vector2Int startCoords)
    {
        // 1) Buscar automáticamente el LevelManager en la escena
        if (levelManager == null)
        {
            levelManager = FindObjectOfType<LevelManager>();
            if (levelManager == null)
            {
                Debug.LogError("[EnemyController] NO se encontró LevelManager en la escena");
                return;
            }
        }

        // 2) Guardar referencia interna
        lm = levelManager;

        // 3) Guardar coordenadas iniciales
        currentTileCoords = startCoords;

        // 4) Colocar al enemigo en su tile inicial
        TileController startTile = lm.GetTile(startCoords);
        transform.position = startTile.transform.position + Vector3.up * enemyOffsetY;

        // Asegurar que tenemos TODOS los SpriteRenderer antes del fade
        allSpriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);


        // 5) Lanzar animación de aparición
        StartCoroutine(AppearRoutine());
    }




    private void Update()
    {
        if (isPaused) return;
        if (lm == null) return;
        if (lm.levelCompleted) return;

        if (hasKilledPlayer) return;
        if (isMoving) return;

        switch (aiType)
        {
            case EnemyAIType.IA_BASICA:
                // ANTES:
                // TryMove();   // rebotes diagonales

                // AHORA: el básico baja recto y se cae
                AIMoveFallStraight();
                break;

            case EnemyAIType.IA_ZIGZAG:
                TryMoveZigzag();
                break;

            case EnemyAIType.IA_PERSEGUIDORA:
                AIMoveChasePlayer();
                break;

            case EnemyAIType.IA_EXPLORADORA:
                AIMoveExplorer();
                break;

            case EnemyAIType.IA_IMPREDECIBLE:
                AIMoveRandom();
                break;

            case EnemyAIType.IA_SUBE_JUGADOR:
                AIMoveClimbToPlayer();
                break;

            case EnemyAIType.IA_CAMBIO_VELOCIDAD:
                AIMoveSpeedVariation();
                break;

            case EnemyAIType.IA_ESTADOS:
                AIMoveStateMachine();
                break;

            case EnemyAIType.IA_OMNI:
                AIMoveOmni();
                break;

        }
    }


    public void TryMove()
    {
        // Calculamos el siguiente tile en la dirección actual
        Vector2Int next = currentTileCoords + direction;

        // Si hay tile → nos movemos normal
        if (lm.HasTile(next))
        {
            StartCoroutine(MoveToTile(next));
        }
        else
        {
            // No hay tile → "rebotamos"

            // En lugar de simplemente invertir X (direction.x *= -1),
            // usamos RandomSign() para que a veces cambie y a veces no,
            // evitando patrones demasiado repetitivos.
            direction.x *= RandomSign();

            // Sonido de rebote del enemigo
            AudioManager.Instance.PlaySFX(AudioManager.Instance.enemyBounceClip);

            Vector2Int nextAfterBounce = currentTileCoords + direction;

            // Si sigue sin haber tile después del rebote en X...
            if (!lm.HasTile(nextAfterBounce))
            {
                // También variamos Y de forma aleatoria
                direction.y *= RandomSign();
                nextAfterBounce = currentTileCoords + direction;
            }

            // Si ahora sí hay tile → nos movemos
            if (lm.HasTile(nextAfterBounce))
            {
                StartCoroutine(MoveToTile(nextAfterBounce));
            }
            else
            {
                // Caso extremo: está muy encerrado.
                // Como último recurso, elegimos una dirección completamente nueva al azar.
                direction = new Vector2Int(RandomSign(), RandomSign());
            }
        }
    }


    // IA BÁSICA NUEVA:
    // El enemigo solo baja recto hacia abajo.
    // Si ya no hay tile debajo (borde o agujero), "se cae" y se destruye.
    private void AIMoveFallStraight()
    {
        // Dirección siempre hacia abajo en el tablero (misma X, Y - 1)
        Vector2Int downDir = new Vector2Int(0, -1);

        // Calculamos el siguiente tile hacia abajo
        Vector2Int next = currentTileCoords + downDir;

        // lm.HasTile(next) ya tiene en cuenta:
        // - límites del tablero
        // - si hay TileController (no agujero)
        if (lm.HasTile(next))
        {
            // Hay tile debajo → nos movemos a ese tile
            StartCoroutine(MoveToTile(next));
        }
        else
        {
            // No hay tile debajo → es borde o agujero → se cae

            Debug.Log("[EnemyController] Enemigo básico se cae en " + currentTileCoords);

            // Igual que el player: salto hacia fuera y luego caída
            StartCoroutine(EnemyJumpOutAndFall(next));

        }

    }


    public void TryMoveZigzag()
    {
        // Alternamos izquierda/derecha con el toggle
        int horizontal = toggle ? 1 : -1;
        toggle = !toggle;

        // Pequeña probabilidad de invertir el zigzag (rompe patrones)
        if (Random.value < 0.1f)
            horizontal *= -1;

        // Dirección principal del zigzag
        direction = new Vector2Int(horizontal, -1);

        // Lista de TODAS las direcciones diagonales posibles
        Vector2Int[] allDirs = new Vector2Int[]
        {
        new Vector2Int(1,1),
        new Vector2Int(-1,1),
        new Vector2Int(1,-1),
        new Vector2Int(-1,-1)
        };

        // 1) Intentar dirección principal
        Vector2Int next = currentTileCoords + direction;

        if (lm.HasTile(next) && next != lastTileCoords)
        {
            StartCoroutine(MoveToTile(next));
            return;
        }

        // 2) Intentar TODAS las diagonales excepto la que nos devuelve atrás
        foreach (var dir in allDirs)
        {
            Vector2Int test = currentTileCoords + dir;

            if (lm.HasTile(test) && test != lastTileCoords)
            {
                direction = dir;
                StartCoroutine(MoveToTile(test));
                return;
            }
        }

        // 3) Si sigue sin haber salida → elegir dirección aleatoria
        direction = new Vector2Int(RandomSign(), RandomSign());
        next = currentTileCoords + direction;

        if (lm.HasTile(next) && next != lastTileCoords)
        {
            StartCoroutine(MoveToTile(next));
            return;
        }

        // 4) Intentar encontrar una dirección válida REAL
        Vector2Int escapeDir;
        if (TryFindValidDirection(out escapeDir))
        {
            direction = escapeDir;
            StartCoroutine(MoveToTile(currentTileCoords + escapeDir));
            return;
        }

        // 5) Si no hay ninguna dirección válida (muy raro), quedarse quieto 1 frame
        // y volver a intentarlo en el siguiente Update()

    }

    // IA PERSEGUIDORA SIMPLE
    // Prioridades:
    // 1) Moverse directamente hacia el jugador (diagonal si es posible)
    // 2) Si la diagonal no existe, probar horizontal o vertical
    // 3) Si no hay camino, usar movimiento básico (rebote)

    private void AIMoveChasePlayer()
    {
        // Posición del jugador
        Vector2Int playerTile = levelManager.player.currentTileCoords;

        // Diferencia entre enemigo y jugador
        int dx = playerTile.x - currentTileCoords.x;
        int dy = playerTile.y - currentTileCoords.y;

        // Convertimos dx y dy en -1, 0 o 1
        int dirX = dx == 0 ? 0 : (dx > 0 ? 1 : -1);
        int dirY = dy == 0 ? 0 : (dy > 0 ? 1 : -1);

        // Dirección diagonal hacia el jugador
        Vector2Int chaseDir = new Vector2Int(dirX, dirY);

        // Tile objetivo
        Vector2Int next = currentTileCoords + chaseDir;

        // Si existe tile → mover
        if (lm.HasTile(next))
        {
            direction = chaseDir;
            StartCoroutine(MoveToTile(next));
            return;
        }

        // Si la diagonal no existe, probar alternativas simples
        Vector2Int[] alternatives = new Vector2Int[]
        {
        new Vector2Int(dirX, 0),
        new Vector2Int(0, dirY)
        };

        foreach (var alt in alternatives)
        {
            Vector2Int test = currentTileCoords + alt;
            if (lm.HasTile(test))
            {
                direction = alt;
                StartCoroutine(MoveToTile(test));
                return;
            }
        }

        // Si no hay nada, fallback básico
        TryMove();
    }








    // IA EXPLORADORA AVANZADA
    // Prioridades:
    // 0) Si estamos muy arriba, intentar bajar primero
    // 1) Ir a un tile válido que NO esté en recentTiles
    // 2) Si no hay, ir a un tile válido que no sea lastTileCoords
    // 3) Si no hay, fallback TryMove()
    private void AIMoveExplorer()
    {
        // Direcciones diagonales ordenadas para priorizar bajar
        Vector2Int[] allDirs = new Vector2Int[]
        {
        new Vector2Int(1,-1),   // abajo-derecha
        new Vector2Int(-1,-1),  // abajo-izquierda
        new Vector2Int(1,1),    // arriba-derecha
        new Vector2Int(-1,1)    // arriba-izquierda
        };

        // ⭐ 0) Si estamos muy arriba en el tablero, intentamos bajar primero
        // Ajusta el valor 6 según la altura de tu tablero (Height = 8 → 6 es perfecto)
        bool shouldForceDown = currentTileCoords.y >= 7;

        if (shouldForceDown)
        {
            // Solo direcciones que bajan
            Vector2Int[] downDirs = new Vector2Int[]
            {
            new Vector2Int(1,-1),
            new Vector2Int(-1,-1)
            };

            foreach (var dir in downDirs)
            {
                Vector2Int test = currentTileCoords + dir;

                // Si hay tile y NO está en recientes → BAJA YA
                if (lm.HasTile(test) && !recentTiles.Contains(test))
                {
                    direction = dir;
                    StartCoroutine(MoveToTile(test));
                    return;
                }
            }
        }

        // ⭐ 1) Buscar un tile válido que NO esté en recentTiles
        foreach (var dir in allDirs)
        {
            Vector2Int test = currentTileCoords + dir;

            if (lm.HasTile(test) && !recentTiles.Contains(test))
            {
                direction = dir;
                StartCoroutine(MoveToTile(test));
                return;
            }
        }

        // ⭐ 2) Buscar un tile válido que no sea el último tile
        foreach (var dir in allDirs)
        {
            Vector2Int test = currentTileCoords + dir;

            if (lm.HasTile(test) && test != lastTileCoords)
            {
                direction = dir;
                StartCoroutine(MoveToTile(test));
                return;
            }
        }

        // ⭐ 3) Fallback → movimiento básico
        TryMove();
    }


    // IA IMPREDECIBLE
    private void AIMoveRandom()
    {
        // Movimiento completamente aleatorio por ahora
        Vector2Int randomDir = new Vector2Int(RandomSign(), RandomSign());
        Vector2Int next = currentTileCoords + randomDir;

        if (lm.HasTile(next))
        {
            direction = randomDir;
            StartCoroutine(MoveToTile(next));
        }
        else
        {
            // Si la dirección aleatoria no sirve, usamos la básica
            TryMove();
        }
    }




    // IA SUBE JUGADOR AVANZADA
    // Prioridades:
    // 1) Subir hacia el jugador (si está arriba)
    // 2) Elegir la diagonal que más reduce la distancia vertical
    // 3) Evitar recentTiles (memoria)
    // 4) Evitar lastTileCoords (no volver atrás)
    // 5) Fallback seguro
    private void AIMoveClimbToPlayer()
    {
        // Obtener posición del jugador en tiles
        Vector2Int playerTile = levelManager.player.currentTileCoords;

        // Diferencia entre enemigo y jugador
        int dx = playerTile.x - currentTileCoords.x;
        int dy = playerTile.y - currentTileCoords.y;

        // Dirección horizontal hacia el jugador
        int dirX = dx == 0 ? 0 : (dx > 0 ? 1 : -1);

        // Dirección vertical hacia el jugador (subir o bajar)
        int dirY = dy == 0 ? 0 : (dy > 0 ? 1 : -1);

        // ⭐ 1) Intentar la diagonal que más acerca verticalmente al jugador
        Vector2Int primaryDir = new Vector2Int(dirX, dirY);
        Vector2Int testPrimary = currentTileCoords + primaryDir;

        if (lm.HasTile(testPrimary) &&
            testPrimary != lastTileCoords &&
            !recentTiles.Contains(testPrimary))
        {
            direction = primaryDir;
            StartCoroutine(MoveToTile(testPrimary));
            return;
        }

        // ⭐ 2) Intentar diagonales alternativas (solo cambiar X o Y)
        Vector2Int[] alternatives = new Vector2Int[]
        {
        new Vector2Int(dirX, -dirY),   // misma X, Y invertida
        new Vector2Int(-dirX, dirY),   // misma Y, X invertida
        new Vector2Int(dirX, 0),       // solo horizontal
        new Vector2Int(0, dirY)        // solo vertical
        };

        foreach (var alt in alternatives)
        {
            Vector2Int test = currentTileCoords + alt;

            if (lm.HasTile(test) &&
                test != lastTileCoords &&
                !recentTiles.Contains(test))
            {
                direction = alt;
                StartCoroutine(MoveToTile(test));
                return;
            }
        }

        // ⭐ 3) Intentar cualquier diagonal válida que no sea volver atrás
        Vector2Int[] allDirs = new Vector2Int[]
        {
        new Vector2Int(1,1),
        new Vector2Int(-1,1),
        new Vector2Int(1,-1),
        new Vector2Int(-1,-1)
        };

        foreach (var dir in allDirs)
        {
            Vector2Int test = currentTileCoords + dir;

            if (lm.HasTile(test) && test != lastTileCoords)
            {
                direction = dir;
                StartCoroutine(MoveToTile(test));
                return;
            }
        }

        // ⭐ 4) Fallback → movimiento básico
        TryMove();
    }





    // IA QUE CAMBIA DE VELOCIDAD
    private void AIMoveSpeedVariation()
    {
        // Cambiamos la velocidad de forma ligera y luego usamos la básica
        speed = Random.Range(2f, 6f);
        TryMove();
    }





    // IA CON ESTADOS (Patrulla → Persecución → Huida)
    private void AIMoveStateMachine()
    {
        // Obtener posición del jugador
        Vector2Int playerTile = levelManager.player.currentTileCoords;

        // Distancia Manhattan
        float dist = Mathf.Abs(playerTile.x - currentTileCoords.x) +
                     Mathf.Abs(playerTile.y - currentTileCoords.y);

        // ⭐ CAMBIO DE ESTADO
        if (dist > 6)
        {
            StatePatrol(playerTile);
        }
        else if (dist > 3)
        {
            StateChase(playerTile);
        }
        else
        {
            StateFlee(playerTile);
        }
    }

    //Estado patrulla de la IA CON ESTADOS (Patrulla → Persecución → Huida) - Velocidad 1.0f
    private void StatePatrol(Vector2Int playerTile)
    {
        // Velocidad del estado patrulla
        speed = 1f;

        // Direcciones diagonales
        Vector2Int[] dirs = new Vector2Int[]
        {
        new Vector2Int(1,-1),
        new Vector2Int(-1,-1),
        new Vector2Int(1,1),
        new Vector2Int(-1,1)
        };

        // Intentar evitar recentTiles
        foreach (var dir in dirs)
        {
            Vector2Int test = currentTileCoords + dir;

            if (lm.HasTile(test) &&
                !recentTiles.Contains(test) &&
                test != lastTileCoords)
            {
                direction = dir;
                StartCoroutine(MoveToTile(test));
                return;
            }
        }

        // Fallback
        TryMove();
    }




    //Estado persecución de la IA CON ESTADOS (Patrulla → Persecución → Huida) - Velocidad 2.0f

    private void StateChase(Vector2Int playerTile)
    {
        // Velocidad del estado persecución
        speed = 2f;

        int dx = playerTile.x - currentTileCoords.x;
        int dy = playerTile.y - currentTileCoords.y;

        int dirX = dx == 0 ? 0 : (dx > 0 ? 1 : -1);
        int dirY = dy == 0 ? 0 : (dy > 0 ? 1 : -1);

        Vector2Int[] dirs = new Vector2Int[]
        {
        new Vector2Int(dirX, dirY),
        new Vector2Int(dirX, -dirY),
        new Vector2Int(-dirX, dirY),
        new Vector2Int(dirX, 0),
        new Vector2Int(0, dirY)
        };

        foreach (var dir in dirs)
        {
            Vector2Int test = currentTileCoords + dir;

            if (lm.HasTile(test) &&
                !recentTiles.Contains(test) &&
                test != lastTileCoords)
            {
                direction = dir;
                StartCoroutine(MoveToTile(test));
                return;
            }
        }

        TryMove();
    }



    //Estado huida de la IA CON ESTADOS (Patrulla → Persecución → Huida) - Velocidad 1.6f

    private void StateFlee(Vector2Int playerTile)
    {
        // Velocidad del estado huida
        speed = 1.6f;

        int dx = currentTileCoords.x - playerTile.x; // invertido
        int dy = currentTileCoords.y - playerTile.y; // invertido

        int dirX = dx == 0 ? 0 : (dx > 0 ? 1 : -1);
        int dirY = dy == 0 ? 0 : (dy > 0 ? 1 : -1);

        Vector2Int[] dirs = new Vector2Int[]
        {
        new Vector2Int(dirX, dirY),
        new Vector2Int(dirX, -dirY),
        new Vector2Int(-dirX, dirY),
        new Vector2Int(dirX, 0),
        new Vector2Int(0, dirY)
        };

        foreach (var dir in dirs)
        {
            Vector2Int test = currentTileCoords + dir;

            if (lm.HasTile(test) &&
                !recentTiles.Contains(test) &&
                test != lastTileCoords)
            {
                direction = dir;
                StartCoroutine(MoveToTile(test));
                return;
            }
        }

        TryMove();
    }








    // IA OMNI (sube, baja, izquierda, derecha y diagonales)
    // Prioridades:
    // 1) Bajar si es posible
    // 2) Moverse recto (izquierda/derecha)
    // 3) Moverse en diagonal
    // 4) Evitar recentTiles
    // 5) Evitar lastTileCoords
    // 6) Fallback aleatorio seguro
    private void AIMoveOmni()
    {
        // 8 direcciones posibles
        Vector2Int[] allDirs = new Vector2Int[]
        {
        new Vector2Int(0,-1),   // abajo
        new Vector2Int(1,0),    // derecha
        new Vector2Int(-1,0),   // izquierda
        new Vector2Int(0,1),    // arriba
        new Vector2Int(1,-1),   // diagonal abajo-derecha
        new Vector2Int(-1,-1),  // diagonal abajo-izquierda
        new Vector2Int(1,1),    // diagonal arriba-derecha
        new Vector2Int(-1,1)    // diagonal arriba-izquierda
        };

        // ⭐ 1) Prioridad absoluta: bajar si es posible y no repetido
        Vector2Int down = new Vector2Int(0, -1);
        Vector2Int testDown = currentTileCoords + down;

        if (lm.HasTile(testDown) && !recentTiles.Contains(testDown))
        {
            direction = down;
            StartCoroutine(MoveToTile(testDown));
            return;
        }

        // ⭐ 2) Prioridad: moverse recto izquierda/derecha si no repetido
        Vector2Int[] straightDirs = new Vector2Int[]
        {
        new Vector2Int(1,0),
        new Vector2Int(-1,0)
        };

        foreach (var dir in straightDirs)
        {
            Vector2Int test = currentTileCoords + dir;

            if (lm.HasTile(test) && !recentTiles.Contains(test))
            {
                direction = dir;
                StartCoroutine(MoveToTile(test));
                return;
            }
        }

        // ⭐ 3) Prioridad: diagonales no repetidas
        Vector2Int[] diagDirs = new Vector2Int[]
        {
        new Vector2Int(1,-1),
        new Vector2Int(-1,-1),
        new Vector2Int(1,1),
        new Vector2Int(-1,1)
        };

        foreach (var dir in diagDirs)
        {
            Vector2Int test = currentTileCoords + dir;

            if (lm.HasTile(test) && !recentTiles.Contains(test))
            {
                direction = dir;
                StartCoroutine(MoveToTile(test));
                return;
            }
        }

        // ⭐ 4) Si no hay nada mejor, evitar volver atrás
        foreach (var dir in allDirs)
        {
            Vector2Int test = currentTileCoords + dir;

            if (lm.HasTile(test) && test != lastTileCoords)
            {
                direction = dir;
                StartCoroutine(MoveToTile(test));
                return;
            }
        }

        // ⭐ 5) Fallback: elegir una dirección válida aleatoria
        List<Vector2Int> valid = new List<Vector2Int>();

        foreach (var dir in allDirs)
        {
            Vector2Int test = currentTileCoords + dir;
            if (lm.HasTile(test))
                valid.Add(dir);
        }

        if (valid.Count > 0)
        {
            direction = valid[Random.Range(0, valid.Count)];
            StartCoroutine(MoveToTile(currentTileCoords + direction));
            return;
        }

        // ⭐ 6) Si no hay nada (muy raro), quedarse quieto
    }






    private IEnumerator MoveToTile(Vector2Int targetCoords)
    {
        isMoving = true;

        // 🔹 1) Calcular la dirección del movimiento ANTES de empezar a moverse
        // targetCoords = tile al que vamos
        // currentTileCoords = tile en el que estamos ahora
        Vector2Int moveDir = targetCoords - currentTileCoords;

        // 🔹 2) Girar el sprite según la dirección horizontal
        // Si se va a mover hacia la derecha (x > 0) → mirar a la derecha
        // Si se va a mover hacia la izquierda (x < 0) → mirar a la izquierda
        if (moveDir.x > 0)
        {
            spriteRenderer.flipX = false; // mirando a la derecha
        }
        else if (moveDir.x < 0)
        {
            spriteRenderer.flipX = true;  // mirando a la izquierda
        }
        // Si moveDir.x == 0, no tocamos el flip (por si se mueve solo vertical/diagonal)

        // 🔹 3) Activar animación de salto
        animator.SetTrigger("Jump");

        // Sonido de movimiento
        AudioManager.Instance.PlaySFX(AudioManager.Instance.enemyMoveClip);

        // ⭐ SOLO el enemigo básico activa las partículas de salto
        if (aiType == EnemyAIType.IA_BASICA)
        {
            Transform particles = transform.Find("EnemyStepParticles");
            if (particles != null)
            {
                var ps = particles.GetComponent<ParticleSystem>();
                if (ps != null)
                    ps.Play();
            }
        }



        // 🔹 4) Preparar posiciones de inicio y fin
        Vector3 startPos = transform.position;
        TileController targetTile = lm.GetTile(targetCoords);
        Vector3 endPos = targetTile.transform.position + Vector3.up * enemyOffsetY;

        float t = 0f;
        float moveDuration = 1f / speed;

        // Si queremos que este enemigo sincronice la animación con la velocidad,
        // aceleramos el Animator en función de speed
        if (scaleAnimationWithSpeed && animator != null)
        {
            animator.speed = speed;
        }


        // 🔹 5) Bucle de movimiento con arco
        while (t < moveDuration)
        {
            t += Time.deltaTime;
            float lerp = t / moveDuration;

            Vector3 horizontalPos = Vector3.Lerp(startPos, endPos, lerp);

            float arc = 4 * jumpHeight * lerp * (1 - lerp);
            horizontalPos.y += arc;

            transform.position = horizontalPos;

            yield return null;
        }

        // Aseguramos que termina exactamente en la posición final
        transform.position = endPos;

        // NUEVO: lanzar animación de aterrizaje
        animator.SetTrigger("Land");

        // Guardamos el tile anterior
        lastTileCoords = currentTileCoords;

        // Actualizamos las coordenadas lógicas del enemigo
        currentTileCoords = targetCoords;

        // ⭐ NUEVO: actualizar la lista de tiles recientes ⭐
        recentTiles.Add(currentTileCoords);

        // Si la lista es demasiado larga, eliminamos el más antiguo
        if (recentTiles.Count > recentTilesMemorySize)
        {
            recentTiles.RemoveAt(0);
        }


        // Restaurar la velocidad normal de la animación
        if (scaleAnimationWithSpeed && animator != null)
        {
            animator.speed = 1f;
        }


        isMoving = false;


    }


    // ------------------------------------------------------------
    // SALTO HACIA EL VACÍO DEL ENEMIGO (igual que el Player)
    // ------------------------------------------------------------
    private IEnumerator EnemyJumpOutAndFall(Vector2Int targetCoords)
    {
        isMoving = true;

        // Si quieres, puedes usar la misma animación de salto
        if (animator != null)
            animator.SetTrigger("Jump");

        // Posición actual del enemigo
        Vector3 startPos = transform.position;

        // Posición "ficticia" hacia donde salta (como si hubiera un tile)
        // OJO: aquí NO usamos lm.GetTile porque NO hay tile.
        // Usamos las coords lógicas como en el Player.
        Vector3 fakeEndPos = new Vector3(
            targetCoords.x,
            startPos.y,
            targetCoords.y
        );

        float t = 0f;
        float duration = 1f / speed;   // mismo tiempo que un salto normal del enemigo

        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = t / duration;

            // Movimiento horizontal hacia fuera
            Vector3 pos = Vector3.Lerp(startPos, fakeEndPos, lerp);

            // Mismo arco que MoveToTile
            float arc = 4 * jumpHeight * lerp * (1 - lerp);
            pos.y += arc;

            transform.position = pos;

            yield return null;
        }

        // Al terminar el salto → empieza la caída recta hacia abajo
        StartCoroutine(EnemyFallAndDie());
    }



    // ------------------------------------------------------------
    // CAÍDA RECTa HACIA ABAJO Y DESAPARECE
    // ------------------------------------------------------------
    private IEnumerator EnemyFallAndDie()
    {
        // Ya no queremos que la IA siga pensando
        isMoving = true;
        enabled = false;   // Desactiva Update()

        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + Vector3.down * 20f;   // cae bastante hacia abajo

        float t = 0f;
        float duration = fallDuration;   // configurable desde el Inspector

        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = t / duration;

            transform.position = Vector3.Lerp(startPos, endPos, lerp);

            yield return null;
        }

        Destroy(gameObject);
    }









    private void OnTriggerEnter(Collider other)
    {
        if (lm == null)
        {
            Debug.LogWarning("[EnemyController] OnTriggerEnter: lm es NULL");
            return;
        }

        if (lm.levelCompleted) return;
        if (hasKilledPlayer) return;

        if (!other.CompareTag("Player"))
            return;

        PlayerController player = other.GetComponentInParent<PlayerController>();

        if (player == null)
        {
            Debug.LogWarning("[EnemyController] OnTriggerEnter: NO encuentro PlayerController en " + other.name);
            return;
        }

        AudioManager.Instance.PlaySFX(AudioManager.Instance.enemyHitClip);

        isMoving = true;
        player.StartCoroutine(player.DeathByEnemy());

        hasKilledPlayer = true;
    }







    // NUEVO: devuelve -1 o +1 al azar
    // Lo usaremos para variar la dirección del enemigo y evitar patrones repetitivos
    private int RandomSign()
    {
        // Random.value devuelve un número entre 0 y 1
        // Si es menor que 0.5 → -1, si no → +1
        return Random.value < 0.5f ? -1 : 1;
    }


    // NUEVO: devuelve una dirección diagonal válida si existe
    private bool TryFindValidDirection(out Vector2Int validDir)
    {
        Vector2Int[] allDirs = new Vector2Int[]
        {
        new Vector2Int(1,1),
        new Vector2Int(-1,1),
        new Vector2Int(1,-1),
        new Vector2Int(-1,-1)
        };

        foreach (var dir in allDirs)
        {
            Vector2Int test = currentTileCoords + dir;

            if (lm.HasTile(test))
            {
                validDir = dir;
                return true;
            }
        }

        validDir = Vector2Int.zero;
        return false;
    }


    // CORUTINA DE APARICIÓN CON FADE-IN + PARTÍCULAS
    private IEnumerator AppearRoutine()
    {
        isMoving = true; // mientras aparece, no se mueve

        // Congelar animación en Idle
        if (animator != null)
        {
            // No forzamos un nombre concreto (Enemy_Idle),
            // así cada prefab usa su propio estado inicial (Idle básico, Idle chaser, etc.)
            //animator.Play("Enemy_Idle", 0, 0f); // ir al inicio del Idle

            animator.speed = 0f;               // congelar animación
        }

        // 1) Hacer TODOS los SpriteRenderer invisibles
        foreach (var sr in allSpriteRenderers)
        {
            if (sr != null)
            {
                Color col = sr.color;
                col.a = 0f;
                sr.color = col;
            }
        }

        // 2) Instanciar partículas de aparición
        if (spawnParticlesPrefab != null)
        {
            GameObject particles = Instantiate(
            spawnParticlesPrefab,
            transform.position + Vector3.up * spawnOffsetY,
            Quaternion.identity
            );

            Destroy(particles, 2f);
        }

        // 3) Fade-in de TODOS los SpriteRenderer
        float duration = 1.0f;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = t / duration;

            foreach (var sr in allSpriteRenderers)
            {
                if (sr != null)
                {
                    Color col = sr.color;
                    col.a = Mathf.Lerp(0f, 1f, lerp);
                    sr.color = col;
                }
            }

            yield return null;
        }

        // Asegurar alpha = 1
        foreach (var sr in allSpriteRenderers)
        {
            if (sr != null)
            {
                Color col = sr.color;
                col.a = 1f;
                sr.color = col;
            }
        }

        // Reactivar animación
        if (animator != null)
            animator.speed = 1f;

        isMoving = false; // ahora ya puede moverse
    }





}