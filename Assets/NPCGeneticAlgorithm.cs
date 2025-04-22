using UnityEngine;
using System.Collections.Generic;
using System.Linq;


// Implementa un algoritmo genético para evolucionar una población de NPCs.
// Este script gestiona la creación, evaluación, selección y evolución de los NPCs.

public class NPCGeneticAlgorithm : MonoBehaviour
{
    // Variables configurables desde el Inspector de Unity
    [Header("Configuración del Algoritmo Genético")]
    [Tooltip("Cantidad de NPCs en cada generación")]
    public int populationSize = 50;

    [Header("Configuración de Población")]
    [Tooltip("Porcentaje de NPCs enemigos en la población")]
    [Range(0, 1)]
    public float enemyPercentage = 0.3f;

    [Tooltip("Probabilidad de mutación para cada peso de la red neuronal (0-1)")]
    [Range(0, 1)]
    public float mutationRate = 0.01f;

    [Header("Referencias")]
    [Tooltip("Prefab del NPC que se instanciará")]
    public GameObject npcPrefab;

    [Tooltip("Punto de partida donde aparecerán los NPCs")]
    public Transform startPosition;

    // Variables internas
    [HideInInspector]
    public List<NPCController> population; // Lista que contiene todos los NPCs de la generación actual

    [Header("Estado")]
    [Tooltip("Pausar la simulación")]
    public bool isPaused = false;

    [Header("Estadísticas")]
    [Tooltip("Generación actual")]
    public int generation = 1;

    private float bestFitness = 0; // Mejor desempeño en la generación actual
    private float worstFitness = float.MaxValue; // Peor desempeño en la generación actual
    private float averageFitness = 0; // Desempeño promedio en la generación actual
    public float generationTimeLimit = 30f;
    private float generationTimer = 0f;

    /// <summary>
    /// Se llama al iniciar el script. Verifica las referencias y crea la población inicial.
    /// </summary>
    void Start()
    {
        // Verificamos que el prefab del NPC esté asignado
        if (npcPrefab == null)
        {
            Debug.LogError("NPC prefab no asignado");
            return;
        }

        // Verificamos que la posición inicial esté asignada
        if (startPosition == null)
        {
            Debug.LogError("Posición inicial no asignada");
            return;
        }

        // Inicializamos la primera generación de NPCs
        InitializePopulation();
    }

    /// <summary>
    /// Crea la población inicial de NPCs.
    /// Cada NPC comienza con una red neuronal aleatoria.
    /// </summary>
    public void InitializePopulation()
    {
        population = new List<NPCController>();

        // Creamos cada NPC según el tamaño de población configurado
        int enemyCount = Mathf.RoundToInt(populationSize * enemyPercentage);


        for (int i = 0; i < populationSize; i++)
        {
            GameObject npcGO = Instantiate(npcPrefab, startPosition.position, startPosition.rotation);
            NPCController npc = npcGO.GetComponent<NPCController>();

            if (npc != null)
            {
                // Asignar tipo de NPC
                if (i < enemyCount)
                {
                    npc.npcType = NPCController.NPCType.Enemy;
                }
                else
                {
                    npc.npcType = NPCController.NPCType.Ally;
                }

                // Inicializar el NPC
                npc.SetNPCColor();
                population.Add(npc);
            }
            else
            {
                Debug.LogError("NPCController no está en los componentes del prefab");
            }
        }
    }

    /// <summary>
    /// Se ejecuta cada frame. Verifica si es momento de evaluar y evolucionar la población.
    /// </summary>
    void Update()
    {
        // Si está pausado o la población no está lista, no hacer nada
        if (isPaused || population == null || population.Count == 0)
        {
            return;
        }

        generationTimer += Time.deltaTime;
        // Eliminar null referencias si existen
        population = population.Where(p => p != null).ToList();

        // Verificar nuevamente después de limpiar null referencias
        if (population.Count == 0)
        {
            Debug.LogWarning("La población quedó vacía después de eliminar referencias nulas.");
            return;
        }

        // Verificar si todos los NPCs están muertos antes de proceder
        if (generationTimer >= generationTimeLimit)
        {
            ForceNextGeneration();
        }

        if (population.All(c => c.isDead))
        {
            EvaluatePopulation();
            Selection();
            Mutation();
            ResetPopulation();
            generation++;
        }
    }

    /// <summary>
    /// Evalúa el rendimiento de cada NPC en la generación actual y calcula estadísticas.
    /// </summary>
    void EvaluatePopulation()
    {
        bestFitness = float.MinValue;
        worstFitness = float.MaxValue;
        float totalFitness = 0;

        // Recorremos cada NPC para calcular estadísticas
        foreach (var npc in population)
        {
            // Actualizamos el mejor fitness si encontramos uno mejor
            if (npc.fitness > bestFitness) bestFitness = npc.fitness;

            // Actualizamos el peor fitness si encontramos uno peor
            if (npc.fitness < worstFitness) worstFitness = npc.fitness;

            // Sumamos todos los fitness para calcular el promedio
            totalFitness += npc.fitness;
        }

        // Calculamos el fitness promedio
        averageFitness = totalFitness / population.Count;

        // Mostramos las estadísticas en la consola para seguimiento
        Debug.Log($"Generación {generation}: Mejor Fitness = {bestFitness}, Peor Fitness = {worstFitness}, Promedio Fitness = {averageFitness}");
    }

    /// <summary>
    /// Selecciona los mejores NPCs para crear la siguiente generación.
    /// Utiliza elitismo (conservar al mejor) y selección por torneo.
    /// </summary>
    void Selection()
    {
        if (population == null || population.Count == 0)
        {
            Debug.LogError("La población está vacía o nula.");
            return;
        }

        List<NPCController> newPopulation = new List<NPCController>();

        // Separar la población por tipo
        var allies = population.Where(npc => npc.npcType == NPCController.NPCType.Ally).ToList();
        var enemies = population.Where(npc => npc.npcType == NPCController.NPCType.Enemy).ToList();

        // Seleccionar los mejores de cada tipo
        if (allies.Any())
        {
            NPCController bestAlly = allies.OrderByDescending(c => c.fitness).First();
            newPopulation.Add(bestAlly);
        }

        if (enemies.Any())
        {
            NPCController bestEnemy = enemies.OrderByDescending(c => c.fitness).First();
            newPopulation.Add(bestEnemy);
        }

        // Mantener la proporción original
        int enemyCount = Mathf.RoundToInt(populationSize * enemyPercentage);
        int allyCount = populationSize - enemyCount;

        // Crear la nueva población manteniendo los tipos
        while (newPopulation.Count < populationSize)
        {
            NPCController.NPCType targetType = (newPopulation.Count < enemyCount) ?
                NPCController.NPCType.Enemy : NPCController.NPCType.Ally;

            var parentPool = (targetType == NPCController.NPCType.Enemy) ? enemies : allies;

            if (parentPool.Count >= 2)
            {
                NPCController parent1 = TournamentSelection(parentPool);
                NPCController parent2 = TournamentSelection(parentPool);

                GameObject childGO = Instantiate(npcPrefab, startPosition.position, startPosition.rotation);
                NPCController child = childGO.GetComponent<NPCController>();

                if (child != null)
                {
                    child.brain = parent1.brain.Copy();
                    child.brain.Crossover(parent2.brain);
                    child.npcType = targetType;
                    child.SetNPCColor();
                    newPopulation.Add(child);
                }
            }
        }

        // Limpiar la población anterior
        foreach (var npc in population)
        {
            if (!newPopulation.Contains(npc))
            {
                Destroy(npc.gameObject);
            }
        }

        population = newPopulation;
    }

    NPCController TournamentSelection(List<NPCController> pool)
    {
        if (pool == null || pool.Count == 0) return null;

        int tournamentSize = Mathf.Min(5, pool.Count);
        NPCController best = null;
        float bestFitness = float.MinValue;

        for (int i = 0; i < tournamentSize; i++)
        {
            NPCController contender = pool[Random.Range(0, pool.Count)];
            if (contender != null && contender.fitness > bestFitness)
            {
                best = contender;
                bestFitness = contender.fitness;
            }
        }

        return best;
    }
   
    NPCController TournamentSelection()
    {
        // Verificar que la población no esté vacía
        if (population == null || population.Count == 0)
        {
            Debug.LogError("No se puede realizar la selección por torneo: población vacía.");
            return null;
        }

        int tournamentSize = Mathf.Min(5, population.Count); // Evitar seleccionar más participantes que el tamaño de la población
        NPCController best = null;
        float bestFitness = float.MinValue;

        for (int i = 0; i < tournamentSize; i++)
        {
            NPCController tournamentContender = population[Random.Range(0, population.Count)];
            if (tournamentContender != null && tournamentContender.fitness > bestFitness)
            {
                best = tournamentContender;
                bestFitness = tournamentContender.fitness;
            }
        }

        if (best == null)
        {
            Debug.LogError("No se pudo seleccionar un ganador en el torneo. Usando el primer NPC disponible.");
            best = population.FirstOrDefault(p => p != null);
        }

        return best;
    }

    // Aplica mutaciones aleatorias a la red neuronal de cada NPC.
    // Esto introduce variabilidad y ayuda a explorar nuevas soluciones.
   
    void Mutation()
    {
        foreach (var npc in population)
        {
            if (npc != null && npc.brain != null)
            {
                // Aplica mutación a cada red neuronal según la tasa de mutación configurada
                npc.brain.Mutate(mutationRate);
            }
            else
            {
                Debug.LogWarning("Se encontró un NPC nulo o sin cerebro durante la mutación");
            }
        }
    }

  
    // Reinicia todos los NPCs para la siguiente generación.
   
    void ResetPopulation()
    {
        foreach (var npc in population)
        {
            if (npc != null)
            {
                // Reiniciamos cada NPC (posición, fitness, estado, etc.)
                npc.Reset();
            }
            else
            {
                Debug.LogError("Error al resetear el NPC: referencia nula");
            }
        }
    }

    public void ForceNextGeneration()
    {
        if (population == null || population.Count == 0) return;

        Debug.Log("Avance forzado de generación por botón o timeout");

        EvaluatePopulation();
        Selection();
        Mutation();
        ResetPopulation();
        generation++;

        generationTimer = 0f;
    }
}