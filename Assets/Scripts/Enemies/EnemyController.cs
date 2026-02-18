using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    // Coordenadas del tile donde está el enemigo
    public Vector2Int currentTileCoords;

    // Duración del movimiento entre tiles (NO la usaremos más)
    //public float moveDuration = 0.25f; - Eliminada asi no me sale en el inspector
    // Velocidad del enemigo (tiles por segundo)
    public float speed = 4f;


    // Para evitar que el enemigo se mueva mientras ya está moviéndose
    public bool isMoving = false;

    // Referencia al LevelManager para saber dónde están los tiles
    public LevelManager levelManager;

    // Dirección inicial del enemigo (1, -1) = derecha + abajo
    public Vector2Int direction = new Vector2Int(1, -1);

    // Para alternar izquierda/derecha en el movimiento Zigzag
    public bool toggle = false;

    // Prefab de partículas que se instanciará al moverse
    public GameObject stepParticlesPrefab;

    // Offset vertical del enemigo para que quede encima del tile
    public const float enemyOffsetY = 1.5f;

    // Esta referencia la asigna el LevelManager cuando crea al enemigo
    private LevelManager lm;

    // Esta es la referencia elegante que usaremos internamente
    public LevelManager levelmanager;

    // Altura del salto del enemigo (ajustable)
    public float jumpHeight = 0.4f;

    // Intensidad del squash & stretch del enemigo
    public float squashAmount = 0.2f;

    // Escala original del enemigo (para restaurarla al final del salto)
    private Vector3 originalScale;


    // Indica si el enemigo ya ha matado al jugador
    private bool hasKilledPlayer = false;




    private void Start()
    {
        // Guardamos la escala original del enemigo
        originalScale = transform.localScale;
    }


    // Inicializa la posición lógica y física del enemigo
    public void Init(Vector2Int startCoords)
    {
        // Aquí SÍ tenemos levelManager asignado por LevelManager
        lm = levelManager;

        currentTileCoords = startCoords; 
        // Obtenemos el tile donde debe aparecer el enemigo
        TileController startTile = lm.GetTile(startCoords);
        // Lo colocamos encima del tile usando el offset
        transform.position = startTile.transform.position + Vector3.up * enemyOffsetY; }


        private void Update()
    {
        // ⬇️ Si el nivel está completado, el enemigo NO debe moverse más.
        // Esto evita que siga caminando después de que el jugador gane.
        if (lm.levelCompleted)
            return;

        // ⬇️ Movimiento normal del enemigo. si no esta moviendose debe moverse
        // Si ya mató al jugador → NO se mueve nunca más
        if (hasKilledPlayer)
            return;

        // Si el nivel está completado → tampoco se mueve
        if (lm.levelCompleted)
            return;

        // Movimiento normal
        if (!isMoving)
        {
            if (lm.useZigzag)
                TryMoveZigzag();
            else
                TryMove();
        }

    }


    // Movimiento Línea Recta
    public void TryMove()
    {
        // Calculamos la siguiente casilla
        Vector2Int next = currentTileCoords + direction;

        // Si existe tile en esa dirección, nos movemos
        if (lm.HasTile(next))
        {
            StartCoroutine(MoveToTile(next));
        }
        else
        {
            // Rebote horizontal
            direction.x *= -1;

            AudioManager.Instance.PlaySFX(AudioManager.Instance.enemyBounceClip);

            // Intentamos movernos en la nueva dirección
            Vector2Int nextAfterBounce = currentTileCoords + direction;

            // Si tampoco existe ese tile, invertimos también la dirección vertical
            if (!lm.HasTile(nextAfterBounce))
            {
                direction.y *= -1; // rebote vertical
                nextAfterBounce = currentTileCoords + direction;
            }

            // Si ahora sí existe, nos movemos
            if (lm.HasTile(nextAfterBounce))
                StartCoroutine(MoveToTile(nextAfterBounce));
        }

    }


    // Movimiento Zigzag
    // Movimiento Zigzag: izquierda, derecha, izquierda, derecha...

    public void TryMoveZigzag()
    {
        // Alterna entre -1 y +1
        int horizontal = toggle ? 1 : -1;
        toggle = !toggle;

        // Dirección zigzag: izquierda/derecha + abajo
        direction = new Vector2Int(horizontal, -1);

        // Primer intento
        Vector2Int next = currentTileCoords + direction;

        if (lm.HasTile(next))
        {
            StartCoroutine(MoveToTile(next));
            return;
        }

        // Si falla, probamos el otro lado (rebote horizontal)
        direction.x *= -1;
        AudioManager.Instance.PlaySFX(AudioManager.Instance.enemyBounceClip);

        next = currentTileCoords + direction;

        if (lm.HasTile(next))
        {
            StartCoroutine(MoveToTile(next));
            return;
        }

        // Si aún así no hay tile, rebotamos verticalmente
        direction.y *= -1;
        next = currentTileCoords + direction;

        if (lm.HasTile(next))
        {
            StartCoroutine(MoveToTile(next));
        }
    }




    // Movimiento suave hacia el tile objetivo
    private IEnumerator MoveToTile(Vector2Int targetCoords)
    {
        isMoving = true;

        // Sonido de movimiento del enemigo
        AudioManager.Instance.PlaySFX(AudioManager.Instance.enemyMoveClip);

        // Posición inicial y final
        Vector3 startPos = transform.position;
        TileController targetTile = lm.GetTile(targetCoords);
        Vector3 endPos = targetTile.transform.position + Vector3.up * enemyOffsetY;

        float t = 0f;

        // Duración del movimiento según la velocidad (igual que antes)
        float moveDuration = 1f / speed;

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

            // --- 3) Squash & Stretch del enemigo ---
            // En el centro del salto se estira, al inicio y final se aplasta
            float stretch = 1 + squashAmount * (1 - Mathf.Abs(lerp * 2 - 1));

            // Escalamos solo en Y, manteniendo la escala original en X y Z
            transform.localScale = new Vector3(
                originalScale.x,
                originalScale.y * stretch,
                originalScale.z
            );

            yield return null;
        }

        // Aseguramos la posición final exacta
        transform.position = endPos;

        // Restauramos la escala original
        transform.localScale = originalScale;

        // Instanciar partículas en la posición actual del enemigo
        if (stepParticlesPrefab != null)
        {
            Vector3 particlePos = transform.position + new Vector3(0, 0.3f, 0);
            Instantiate(stepParticlesPrefab, particlePos, Quaternion.identity);
        }

        // Actualizamos las coordenadas lógicas
        currentTileCoords = targetCoords;

        isMoving = false;
    }





    //deteccion si el enemigo toca al playe

    private void OnTriggerEnter(Collider other)
    {
        // ⬇️ Si el nivel está completado, ignoramos TODAS las colisiones. 
        // Esto evita que el enemigo mate al jugador después de ganar.
        if (lm.levelCompleted) 
            return;

        // Si ya matamos al jugador una vez, ignoramos más colisiones
        if (hasKilledPlayer)
            return;

        Debug.Log("colision detectada y dentro de Ontrigger");

        // Solo reaccionamos si lo que hemos tocado es el jugador
        if (!other.CompareTag("Player"))
        {
            Debug.Log("se decide que el objeto tocado no es player y seguimos");
            return; // ← AHORA SÍ funciona bien
        }

        Debug.Log("se decide que el objeto tocado ES PLAYER y seguimos");

        // Sonido de golpe al jugador
        AudioManager.Instance.PlaySFX(AudioManager.Instance.enemyHitClip);

        // Intentamos obtener el PlayerController
        //PlayerController player = other.GetComponent<PlayerController>();
        // ← CAMBIO IMPORTANTE AQUÍ...busca en el hijo...dentro de Modelo
        PlayerController player = other.GetComponentInParent<PlayerController>();


        if (player == null) 
        { 
            Debug.Log("NO se ha encontrado PlayerController en el objeto tocado"); 
            return; 
        }

        Debug.Log("Llamamos al metodo DeathByEnemy");

        // Paramos el movimiento del enemigo para que no siga saltando
        isMoving = true; // lo dejamos "ocupado" para que Update no llame más a TryMove

        // Matamos al jugador con la animación de muerte por enemigo
        player.StartCoroutine(player.DeathByEnemy());

        Debug.Log("Se ha llamado a DeathByEnemy, el player debería morir SIN caer");

        hasKilledPlayer = true;
        isMoving = true; // lo dejamos "ocupado"


    }






}
