using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public PlayerController player;
    public TileController[,] tiles; // matriz lógica
    public int width = 3;
    public int height = 3;

    // Número de enemigos que queremos en el nivel
    public int enemyCount = 1;

    // Si es true → enemigo Zigzag
    // Si es false → enemigo Línea Recta
    public bool useZigzag = false;

    // Prefab del enemigo
    public EnemyController enemyPrefab;

    // Lista de posiciones iniciales de los enemigos en el tablero
    // Cada elemento es una coordenada (x, y) de tile
    public Vector2Int[] enemyStartPositions;

    // ⬇️ Esta variable indica si el nivel ya ha sido completado.
    // Cuando el jugador pisa TODOS los tiles, la ponemos a true.
    // Los enemigos la consultan para saber si deben detenerse.
    public bool levelCompleted = false;





    private void Start()
    {
        // Creamos la matriz con el tamaño del tablero
        tiles = new TileController[width, height];

        // Buscamos todos los tiles que están dentro del LevelManager
        TileController[] allTiles = GetComponentsInChildren<TileController>();

        foreach (TileController tile in allTiles)
        {
            // Convertimos la posición del tile en coordenadas lógicas (X = columna, Z = fila)
            int x = Mathf.FloorToInt(tile.transform.position.x + 0.5f);
            int y = Mathf.FloorToInt(tile.transform.position.z + 0.5f);

            // Guardamos el tile en la matriz lógica
            tiles[x, y] = tile;

            // Guardamos también la altura lógica del tile (por si queremos usarla más adelante)
            // Ejemplo: si el tile está en Y = 0 → heightLevel = 0, si está en Y = 1 → heightLevel = 1, etc.
            tile.heightLevel = Mathf.RoundToInt(tile.transform.position.y);
        }


        // Inicializamos al jugador en la posición (0,0)
        Vector2Int startCoords = new Vector2Int(0, 0);
        player.Init(startCoords);
        player.SetLevelManager(this);


        TileController startTile = GetTile(startCoords);

        //coge offset del playerController que es una variable publica y que desde inspector puedo cambiar y se cambia en todas partes
        float playerOffsetY = PlayerController.PLAYER_OFFSET_Y;


        player.transform.position = startTile.transform.position + Vector3.up * playerOffsetY;


        // ⭐ CREAR ENEMIGOS AUTOMÁTICAMENTE ⭐
        if (enemyStartPositions != null && enemyStartPositions.Length > 0)
        {
            // Recorremos cada posición definida en el Inspector
            for (int i = 0; i < enemyStartPositions.Length; i++)
            {
                Vector2Int enemyCoords = enemyStartPositions[i];

                // Seguridad: solo creamos el enemigo si hay tile en esa posición
                if (HasTile(enemyCoords))
                {
                    SpawnEnemy(enemyCoords, useZigzag);
                }
                else
                {
                    Debug.LogWarning("Intento de crear enemigo en coordenadas sin tile: " + enemyCoords);
                }
            }
        }
        else
        {
            Debug.LogWarning("No hay posiciones de enemigos definidas en enemyStartPositions.");
        }

    }
    public bool CheckVictory()
    {
        // ⬇️ Recorremos todos los tiles del tablero
        foreach (TileController tile in tiles)
        {
            // Si encontramos un tile que NO ha sido pisado → aún no hay victoria
            if (tile != null && tile.isChanged == false)
            {
                return false;
            }
        }

        // ⬇️ Si llegamos aquí significa que TODOS los tiles están pisados.
        // Marcamos que el nivel está completado para que los enemigos se detengan.
        levelCompleted = true;

        return true; // ← Victoria
    }




    public bool HasTile(Vector2Int coords)
    {
        if (coords.x < 0 || coords.x >= width) return false;
        if (coords.y < 0 || coords.y >= height) return false;
        return tiles[coords.x, coords.y] != null;
    }

    public TileController GetTile(Vector2Int coords)
    {
        return tiles[coords.x, coords.y];
    }


    //Este metodo crea un enemigo ,le asigna el LevelManager, le asigna coordenadas iniciales, decide si es Zigzag o Línea Recta

    // Crea un enemigo en una posición concreta del tablero
    public void SpawnEnemy(Vector2Int startCoords, bool zigzag)
    {
        // 1. Crear una instancia del enemigo
        EnemyController enemy = Instantiate(enemyPrefab);

        // 2. Asignar el LevelManager al enemigo
        enemy.levelManager = this;

        // 3. Inicializar al enemigo en el tile indicado
        enemy.Init(startCoords);

        // 4. Elegir el tipo de movimiento
        if (zigzag)
        {
            // Activar Zigzag (la dirección inicial sigue siendo diagonal)
            enemy.direction = new Vector2Int(1, -1);
            enemy.toggle = false; // empezar alternancia
        }
        else
        {
            // Activar Línea Recta
            enemy.direction = new Vector2Int(1, -1);
        }
    }


}
