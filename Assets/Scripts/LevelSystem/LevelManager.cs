using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Configuración individual de cada enemigo en un nivel
[System.Serializable]
public class EnemyConfig
{
    public Vector2Int startCoords;     // posición inicial del enemigo (si no usamos aleatorio)
    public float speed = 4f;           // velocidad del enemigo

    // Tipo de IA que usará este enemigo
    public EnemyController.EnemyAIType aiType = EnemyController.EnemyAIType.IA_BASICA;

    // Índice del prefab que queremos usar
    // 0 = básico, 1 = perseguidor, etc.
    public int prefabIndex = 0;

    // NUEVO: si es true, este enemigo aparecerá en una posición aleatoria válida
    // (por ejemplo, en la fila superior, en un tile que exista y no sea agujero)
    public bool randomTopSpawn = true;
}




public class LevelManager : MonoBehaviour
{
    // PREFAB DEL FONDO DEL MUNDO
    // Asignaremos aquí el fondo correcto desde el Inspector
    [Header("Fondos")]
    public GameObject backgroundPrefab;          // Fondo antiguo (si lo quieres mantener)
    public GameObject backgroundVisualPrefab;    // Fondo nuevo (imagen bonita)



    public PlayerController player;
    public TileController[,] tiles; // matriz lógica
    public int width = 3;
    public int height = 3;

    // Número de enemigos que queremos en el nivel
    public int enemyCount = 1;

    // Si es true → enemigo Zigzag
    // Si es false → enemigo Línea Recta
    //public bool useZigzag = false; ****esta linea sustituida por le de "public EnemyConfig[] enemies;"


    // Lista de posiciones iniciales de los enemigos en el tablero
    // Cada elemento es una coordenada (x, y) de tile
    //public Vector2Int[] enemyStartPositions;****esta linea también sustituida por le de "public EnemyConfig[] enemies;"

    // Lista de enemigos para este nivel
    public EnemyConfig[] enemies;


    // Lista de prefabs de enemigos
    // Element 0 = enemigo básico
    // Element 1 = enemigo perseguidor
    // Element 2 = enemigo zigzag (si lo añades en el futuro)
    public EnemyController[] enemyPrefabs;




    // ⬇️ Esta variable indica si el nivel ya ha sido completado.
    // Cuando el jugador pisa TODOS los tiles, la ponemos a true.
    // Los enemigos la consultan para saber si deben detenerse.
    public bool levelCompleted = false;

    // Paleta visual del mundo actual
    public WorldPalette palette;

    // ⭐ Tiempo entre la aparición de cada enemigo (configurable desde el Inspector)
    public float enemySpawnDelay = 2f;

    // ⭐ Tiempo entre apariciones sucesivas SOLO de enemigos básicos (IA_BASICA)
    [Header("Respawn enemigo básico")]
    public float basicRespawnDelay = 3f;   // Ajusta este valor en el Inspector


    private void Start()
    {
        // ⭐ Cambiar a la música del mundo actual
        AudioManager.Instance.PlayMusic(AudioManager.Instance.world1Music);


        // ⭐ INSTANCIAR EL FONDO DEL MUNDO ⭐ 
        // Si hemos asignado un prefab de fondo en el Inspector, lo instanciamos aquí.
        // ⭐ Instanciar fondo antiguo (si existe)
        if (backgroundPrefab != null)
        {
            Instantiate(backgroundPrefab);
        }

        // ⭐ Instanciar fondo nuevo visual (si existe)
        if (backgroundVisualPrefab != null)
        {
            Instantiate(backgroundVisualPrefab);
        }


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

        // Aplicar colores del mundo a los tiles y luz ambiental
        ApplyPalette();



        // Inicializamos al jugador en la posición (0,0)
        Vector2Int startCoords = new Vector2Int(0, 0);
        player.Init(startCoords);
        player.SetLevelManager(this);


        TileController startTile = GetTile(startCoords);

        //coge offset del playerController que es una variable publica y que desde inspector puedo cambiar y se cambia en todas partes
        float playerOffsetY = PlayerController.PLAYER_OFFSET_Y;


        player.transform.position = startTile.transform.position + Vector3.up * playerOffsetY;
        // ⭐ Marcar el tile inicial como pisado ⭐
        startTile.OnStepped();   // ← Esto enciende el tile igual que si el jugador lo pisara



        // ⭐ CREAR ENEMIGOS CON RETRASO ⭐
        // 1) Crear todos los enemigos UNA sola vez, con delay entre ellos
        StartCoroutine(SpawnInitialEnemies());

        // 2) Bucle independiente: ir creando SOLO enemigos básicos cada cierto tiempo
        StartCoroutine(SpawnBasicRespawnLoop());




        // ⭐ MOSTRAR EL NÚMERO DE NIVEL EN EL HUD ⭐
        if (GameFlowManager.Instance != null)
        {
            int worldIndex = GameFlowManager.Instance.currentWorldIndex;
            int levelIndex = GameFlowManager.Instance.currentLevelIndex;

            HUDController hud = FindObjectOfType<HUDController>();
            if (hud != null)
                hud.SetLevel(worldIndex, levelIndex);
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

    // Crea un enemigo según la configuración recibida (config)
    public void SpawnEnemy(EnemyConfig config)
    {
        // 1) Instanciar el prefab correcto según el índice
        // enemyPrefabs[0] = Enemy_Basic
        // enemyPrefabs[1] = Enemy_Chaser
        EnemyController enemy = Instantiate(enemyPrefabs[config.prefabIndex]);

        // 2) Asignar LevelManager al enemigo
        enemy.levelManager = this;

        // 3) DECIDIR LA POSICIÓN INICIAL (spawnCoords)
        Vector2Int spawnCoords = config.startCoords;

        // Si este enemigo está marcado como "randomTopSpawn",
        // elegimos una posición aleatoria en la fila superior.
        if (config.randomTopSpawn)
        {
            // Fila superior del tablero (y = height - 1)
            int topY = height - 1;

            // Lista de columnas válidas donde SÍ hay tile (no agujero)
            List<int> validColumns = new List<int>();

            for (int x = 0; x < width; x++)
            {
                Vector2Int test = new Vector2Int(x, topY);

                // HasTile(test) devuelve true solo si:
                // - está dentro de los límites
                // - tiles[x,y] != null (es decir, hay TileController, no agujero)
                if (HasTile(test))
                {
                    validColumns.Add(x);
                }
            }

            if (validColumns.Count > 0)
            {
                // Elegimos una columna aleatoria entre las válidas
                int randomIndex = Random.Range(0, validColumns.Count);
                int randomX = validColumns[randomIndex];

                spawnCoords = new Vector2Int(randomX, topY);
            }
            else
            {
                Debug.LogWarning("[LevelManager] No hay tiles válidos en la fila superior para spawn aleatorio.");
            }
        }

        // 4) Inicializar al enemigo en las coordenadas decididas
        enemy.Init(spawnCoords);

        // 5) Asignar velocidad
        enemy.speed = config.speed;

        // 6) Asignar tipo de IA
        enemy.aiType = config.aiType;

        // 7) Aplicar color de la paleta al SpriteRenderer principal
        SpriteRenderer rend = enemy.GetComponentInChildren<SpriteRenderer>();
        if (rend != null && palette != null)
        {
            rend.color = palette.enemyColor;
        }
    }

    //******************  Eliminado    **************
    // Llamado por EnemyController cuando un enemigo "muere" (se cae, etc.)
    // public void EnemyDied(EnemyController enemy)
    //{
    //   Debug.Log("[LevelManager] EnemyDied llamado por " + enemy.name);

    // Aquí podríamos decidir qué hacer:
    // - Contar cuántos enemigos quedan
    // - Lanzar un respawn
    // De momento, lanzamos un respawn genérico:
    //   StartCoroutine(RespawnEnemyRoutine());
    //}

    // Corrutina que espera un tiempo y luego crea un nuevo enemigo
    //private IEnumerator RespawnEnemyRoutine()
    //{
    // Esperamos el tiempo configurado en enemySpawnDelay
    //  yield return new WaitForSeconds(enemySpawnDelay);

    //enemySpawnDelay *= 0.95f; // cada oleada un 5% más rápido


    // Por simplicidad, vamos a respawnear el PRIMER enemigo de la lista (enemies[0])
    // En el futuro puedes hacer algo más avanzado (aleatorio, por tipo, etc.)
    //if (enemies != null && enemies.Length > 0)
    //{
    //    EnemyConfig config = enemies[0];

    // Aseguramos que use spawn aleatorio
    //  config.randomTopSpawn = true;

    // Creamos un nuevo enemigo
    //SpawnEnemy(config);
    //}
    //   else
    //   {
    //       Debug.LogWarning("[LevelManager] No hay EnemyConfig definidos para respawn.");
    //   }
    //}
    //*************************************************************





    // ⭐ Corrutina que crea TODOS los enemigos UNA sola vez, con delay entre ellos ⭐
    private IEnumerator SpawnInitialEnemies()
    {
        // Si no hay enemigos definidos, no hacemos nada
        if (enemies == null || enemies.Length == 0)
            yield break;

        // Recorremos todos los enemigos definidos en el Inspector
        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyConfig config = enemies[i];

            // Si usamos spawn aleatorio, ignoramos startCoords
            if (config.randomTopSpawn)
            {
                // Aparece en una posición aleatoria válida (fila superior, sin agujero)
                SpawnEnemy(config);
            }
            else
            {
                // Solo si NO es aleatorio, comprobamos startCoords
                if (HasTile(config.startCoords))
                    SpawnEnemy(config);
                else
                    Debug.LogWarning("Coordenadas inválidas para enemigo: " + config.startCoords);
            }

            // ⭐ Esperamos el tiempo configurado antes de crear el siguiente enemigo ⭐
            yield return new WaitForSeconds(enemySpawnDelay);
        }
    }



    // ⭐ Corrutina que crea SOLO enemigos básicos cada cierto tiempo ⭐
    private IEnumerator SpawnBasicRespawnLoop()
    {
        // Si no hay enemigos definidos, no hacemos nada
        if (enemies == null || enemies.Length == 0)
            yield break;

        // Bucle principal: mientras el nivel NO esté completado
        while (!levelCompleted)
        {
            // Esperamos el tiempo configurado para el respawn del básico
            yield return new WaitForSeconds(basicRespawnDelay);

            // Recorremos la lista de enemigos definida en el Inspector
            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyConfig config = enemies[i];

                // Solo queremos respawnear los que sean IA_BASICA
                if (config.aiType == EnemyController.EnemyAIType.IA_BASICA)
                {
                    // Podemos forzar que el respawn sea aleatorio arriba
                    // o respetar lo que tengas en el Inspector.
                    // Si quieres que SIEMPRE sea aleatorio arriba, descomenta esta línea:
                    // config.randomTopSpawn = true;

                    config.randomTopSpawn = true;   // ← Forzamos spawn aleatorio SIEMPRE
                    SpawnEnemy(config);

                }
            }
        }
    }






    // Aplica la paleta del mundo a tiles, iluminación, etc.
    private void ApplyPalette()
    {
        if (palette == null)
        {
            Debug.LogWarning("No hay paleta asignada en el LevelManager.");
            return;
        }

        // 1. Luz ambiental del mundo
        RenderSettings.ambientLight = palette.ambientColor;

        // 2. Aplicar colores a todos los tiles
        foreach (TileController tile in tiles)
        {
            if (tile != null)
            {
                tile.ApplyPaletteColors(palette.tileBaseColor, palette.tileTargetColor);
            }
        }
    }

    // Convierte una posición del mundo (X,Z) a coordenadas lógicas del tablero
    public Vector2Int WorldToCoords(Vector3 worldPos)
    {
        int x = Mathf.FloorToInt(worldPos.x + 0.5f);
        int y = Mathf.FloorToInt(worldPos.z + 0.5f);

        return new Vector2Int(x, y);
    }
}

