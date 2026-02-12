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



    // Inicializa la posición lógica y física del enemigo
    public void Init(Vector2Int startCoords)
    {
        currentTileCoords = startCoords;

        // Coloca el enemigo encima del tile correspondiente
        transform.position =
            levelManager.GetTile(startCoords).transform.position
            + Vector3.up * 1f;
    }

    private void Update()
    {
        // Si no está moviéndose, intenta moverse
        if (!isMoving) 
        { 
            if (levelManager.useZigzag) 
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
        if (levelManager.HasTile(next))
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
            if (!levelManager.HasTile(nextAfterBounce))
            {
                direction.y *= -1; // rebote vertical
                nextAfterBounce = currentTileCoords + direction;
            }

            // Si ahora sí existe, nos movemos
            if (levelManager.HasTile(nextAfterBounce))
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

        if (levelManager.HasTile(next))
        {
            StartCoroutine(MoveToTile(next));
            return;
        }

        // Si falla, probamos el otro lado (rebote horizontal)
        direction.x *= -1;
        AudioManager.Instance.PlaySFX(AudioManager.Instance.enemyBounceClip);

        next = currentTileCoords + direction;

        if (levelManager.HasTile(next))
        {
            StartCoroutine(MoveToTile(next));
            return;
        }

        // Si aún así no hay tile, rebotamos verticalmente
        direction.y *= -1;
        next = currentTileCoords + direction;

        if (levelManager.HasTile(next))
        {
            StartCoroutine(MoveToTile(next));
        }
    }


    // Movimiento suave hacia el tile objetivo
    private IEnumerator MoveToTile(Vector2Int targetCoords)
    {
        isMoving = true;

        // Reproducir sonido de movimiento del enemigo
        AudioManager.Instance.PlaySFX(AudioManager.Instance.enemyMoveClip);


        // Posición inicial y final
        Vector3 startPos = transform.position;
        Vector3 endPos =
            levelManager.GetTile(targetCoords).transform.position
            + Vector3.up * 1f;

        float t = 0f;

        // Calculamos la duración del movimiento según la velocidad
        float moveDuration = 1f / speed;

        // Movimiento interpolado (suave)
        while (t < moveDuration)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, endPos, t / moveDuration);
            yield return null;
        }


        // Aseguramos la posición final exacta
        transform.position = endPos;

        // Instanciar partículas en la posición actual del enemigo
        // Solo si hemos asignado un prefab en el Inspector
        if (stepParticlesPrefab != null)
        {
            Instantiate(stepParticlesPrefab, transform.position, Quaternion.identity);
        }


        // Actualizamos las coordenadas lógicas
        currentTileCoords = targetCoords;

        isMoving = false;
    }

    //deteccion si el enemigo toca al playe

    private void OnTriggerEnter(Collider other)
    {
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

        Debug.Log("Llamamos al metodo FallAndDie");

        // Matamos al jugador
        player.StartCoroutine("FallAndDie");

        Debug.Log("Se ha llamamos a metodo FallAndDie debria morir el player");
    }






}
