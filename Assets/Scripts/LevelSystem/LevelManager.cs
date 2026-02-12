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



    private void Start()
    {
        // Creamos la matriz con el tamaño del tablero
        tiles = new TileController[width, height];

        // Buscamos todos los tiles que están dentro del LevelManager
        TileController[] allTiles = GetComponentsInChildren<TileController>();

        foreach (TileController tile in allTiles)
        {
            // Convertimos la posición del tile en coordenadas lógicas
            int x = Mathf.FloorToInt(tile.transform.position.x + 0.5f);
            int y = Mathf.FloorToInt(tile.transform.position.z + 0.5f);


            // Guardamos el tile en la matriz
            tiles[x, y] = tile;
        }

        // Inicializamos al jugador en la posición (0,0)
        player.Init(new Vector2Int(0, 0));
        player.transform.position = new Vector3(0, 1, 0);

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
        foreach (TileController tile in tiles)
        {
            if (tile != null && tile.isChanged == false)
            {
                return false; // Aún hay tiles sin pisar
            }
        }

        return true; // Todos pisados → victoria
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
