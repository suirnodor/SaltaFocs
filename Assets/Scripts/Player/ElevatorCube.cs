using UnityEngine;
using System.Collections;

public class ElevatorCube : MonoBehaviour
{
    [Header("Velocidad de subida/bajada")]
    public float moveDuration = 1.5f;

    [Header("¿Está a la izquierda del tablero?")]
    public bool isLeftSide = false;

    [Header("¿Es un ascensor central?")]
    public bool isCenter = false;


    private Vector3 initialPos;          // posición inicial del ascensor
    private Vector3 topPos;              // posición final arriba
    private bool isMoving = false;       // evita doble movimiento
    private bool isAtTop = false;        // estado del ascensor
    private PlayerController player;     // referencia al jugador encima
    private LevelManager lm;             // referencia al LevelManager

    public void StartElevatorWithPlayer(PlayerController playerController)
    {
        // Si ya se está moviendo o ya está arriba, no hacemos nada
        if (isMoving || isAtTop)
            return;

        // Guardamos referencia al jugador
        player = playerController;

        // Marcamos que el jugador está sobre el ascensor
        player.isOnElevator = true;

        // Empezamos la corrutina de subida
        StartCoroutine(MoveUp());
    }


    private void Start()
    {
        lm = FindFirstObjectByType<LevelManager>();
        initialPos = transform.position;

        // Esperar a que el LevelManager termine de crear los tiles
        StartCoroutine(InitializeWhenReady());
    }

    private IEnumerator InitializeWhenReady()
    {
        // Esperar hasta que el LevelManager haya creado la matriz de tiles
        while (lm.tiles == null || lm.tiles.Length == 0)
            yield return null;

        // Ahora sí, calcular la posición superior
        topPos = CalculateTopPosition();
    }


    private Vector3 CalculateTopPosition()
    {
        // Determinar columna interna del tablero más cercana
        int targetColumn = isLeftSide ? 0 : lm.width - 1;

        // Buscar la fila más alta con tile válido
        for (int y = lm.height - 1; y >= 0; y--)
        {
            Vector2Int coords = new Vector2Int(targetColumn, y);

            if (lm.HasTile(coords))
            {
                TileController tile = lm.GetTile(coords);

                // El ascensor se coloca fuera del tablero pero a la altura del tile
                return new Vector3(
                    transform.position.x,          // fuera del tablero
                    tile.transform.position.y,     // altura del tile
                    tile.transform.position.z      // alineado en Z
                );
            }
        }

        // Si no encuentra nada (muy raro), no se mueve
        return transform.position;
    }

    //private void OnTriggerEnter(Collider other) { ... }
    //private void OnTriggerExit(Collider other) { ... }



    private IEnumerator MoveUp()
    {
        isMoving = true;

        Vector3 start = transform.position;
        Vector3 end = topPos;

        float t = 0f;

        // Desactivar controles mientras sube
        player.DisableControls();

        while (t < 1f)
        {
            t += Time.deltaTime / moveDuration;

            // Mover ascensor
            transform.position = Vector3.Lerp(start, end, t);

            // Mover jugador encima del ascensor
            player.transform.position = transform.position + Vector3.up * PlayerController.PLAYER_OFFSET_Y;

            yield return null;
        }

        isAtTop = true;
        isMoving = false;

        // ⭐ NUEVO: actualizar coordenadas lógicas del jugador al llegar arriba
        Vector2Int logicalCoords = lm.WorldToCoords(player.transform.position);
        player.currentTileCoords = logicalCoords;

        // Reactivar controles para que el jugador pueda saltar
        player.EnableControls();

    }




    // ⭐ Método público para iniciar la bajada del ascensor
    public void StartMoveDown()
    {
        if (!isMoving && isAtTop)
            StartCoroutine(MoveDown());
    }



    private IEnumerator MoveDown()
    {
        isMoving = true;

        Vector3 start = transform.position;
        Vector3 end = initialPos;

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / moveDuration;
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        isAtTop = false;
        isMoving = false;
    }
}
