using UnityEngine;
using System.Collections.Generic;
using System.Linq;


// Implementa un algoritmo gen�tico para evolucionar una poblaci�n de NPCs.
// Este script gestiona la creaci�n, evaluaci�n, selecci�n y evoluci�n de los NPCs.

public class NPCGeneticAlgorithm : MonoBehaviour
{
    // Variables configurables desde el Inspector de Unity
    [Header("Configuraci�n del Algoritmo Gen�tico")]
    [Tooltip("Cantidad de NPCs en cada generaci�n")]
    public int populationSize = 50;

    [Header("Configuraci�n de Poblaci�n")]
    [Tooltip("Porcentaje de NPCs enemigos en la poblaci�n")]
    [Range(0, 1)]
    public float enemyPercentage = 0.3f;

    [Tooltip("Probabilidad de mutaci�n para cada peso de la red neuronal (0-1)")]
    [Range(0, 1)]
    public float mutationRate = 0.01f;

    [Header("Referencias")]
    [Tooltip("Prefab del NPC que se instanciar�")]
    public GameObject npcPrefab;

    [Tooltip("Punto de partida donde aparecer�n los NPCs")]
    public Transform startPosition;

    // Variables internas
    [HideInInspector]
    public List<NPCController> population; // Lista que contiene todos los NPCs de la generaci�n actual

    [Header("Estado")]
    [Tooltip("Pausar la simulaci�n")]
    public bool isPaused = false;

    [Header("Estad�sticas")]
    [Tooltip("Generaci�n actual")]
    public int generation = 1;

    private float bestFitness = 0; // Mejor desempe�o en la generaci�n actual
    private float worstFitness = float.MaxValue; // Peor desempe�o en la generaci�n actual
    private float averageFitness = 0; // Desempe�o promedio en la generaci�n actual
    public float generationTimeLimit = 30f;
    private float generationTimer = 0f;

    /// <summary>
    /// Se llama al iniciar el script. Verifica las referencias y crea la poblaci�n inicial.
    /// </summary>
    void Start()
    {
        // Verificamos que el prefab del NPC est� asignado
        if (npcPrefab == null)
        {
            Debug.LogError("NPC prefab no asignado");
            return;
        }

        // Verificamos que la posici�n inicial est� asignada
        if (startPosition == null)
        {
            Debug.LogError("Posici�n inicial no asignada");
            return;
        }

        // Inicializamos la primera generaci�n de NPCs
        InitializePopulation();
    }

    /// <summary>
    /// Crea la poblaci�n inicial de NPCs.
    /// Cada NPC comienza con una red neuronal aleatoria.
    /// </summary>
    public void InitializePopulation()
    {
        population = new List<NPCController>();

        // Creamos cada NPC seg�n el tama�o de poblaci�n configurado
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
                Debug.LogError("NPCController no est� en los componentes del prefab");
            }
        }
    }

    /// <summary>
    /// Se ejecuta cada frame. Verifica si es momento de evaluar y evolucionar la poblaci�n.
    /// </summary>
    void Update()
    {
        // Si est� pausado o la poblaci�n no est� lista, no hacer nada
        if (isPaused || population == null || population.Count == 0)
        {
            return;
        }

        generationTimer += Time.deltaTime;
        // Eliminar null referencias si existen
        population = population.Where(p => p != null).ToList();

        // Verificar nuevamente despu�s de limpiar null referencias
        if (population.Count == 0)
        {
            Debug.LogWarning("La poblaci�n qued� vac�a despu�s de eliminar referencias nulas.");
            return;
        }

        // Verificar si todos los NPCs est�n muertos antes de proceder
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
    /// Eval�a el rendimiento de cada NPC en la generaci�n actual y calcula estad�sticas.
    /// </summary>
    void EvaluatePopulation()
    {
        bestFitness = float.MinValue;
        worstFitness = float.MaxValue;
        float totalFitness = 0;

        // Recorremos cada NPC para calcular estad�sticas
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

        // Mostramos las estad�sticas en la consola para seguimiento
        Debug.Log($"Generaci�n {generation}: Mejor Fitness = {bestFitness}, Peor Fitness = {worstFitness}, Promedio Fitness = {averageFitness}");
    }

    /// <summary>
    /// Selecciona los mejores NPCs para crear la siguiente generaci�n.
    /// Utiliza elitismo (conservar al mejor) y selecci�n por torneo.
    /// </summary>
    void Selection()
    {
        if (population == null || population.Count == 0)
        {
            Debug.LogError("La poblaci�n est� vac�a o nula.");
            return;
        }

        List<NPCController> newPopulation = new List<NPCController>();

        // Separar la poblaci�n por tipo
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

        // Mantener la proporci�n original
        int enemyCount = Mathf.RoundToInt(populationSize * enemyPercentage);
        int allyCount = populationSize - enemyCount;

        // Crear la nueva poblaci�n manteniendo los tipos
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

        // Limpiar la poblaci�n anterior
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
        // Verificar que la poblaci�n no est� vac�a
        if (population == null || population.Count == 0)
        {
            Debug.LogError("No se puede realizar la selecci�n por torneo: poblaci�n vac�a.");
            return null;
        }

        int tournamentSize = Mathf.Min(5, population.Count); // Evitar seleccionar m�s participantes que el tama�o de la poblaci�n
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
                // Aplica mutaci�n a cada red neuronal seg�n la tasa de mutaci�n configurada
                npc.brain.Mutate(mutationRate);
            }
            else
            {
                Debug.LogWarning("Se encontr� un NPC nulo o sin cerebro durante la mutaci�n");
            }
        }
    }

  
    // Reinicia todos los NPCs para la siguiente generaci�n.
   
    void ResetPopulation()
    {
        foreach (var npc in population)
        {
            if (npc != null)
            {
                // Reiniciamos cada NPC (posici�n, fitness, estado, etc.)
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

        Debug.Log("Avance forzado de generaci�n por bot�n o timeout");

        EvaluatePopulation();
        Selection();
        Mutation();
        ResetPopulation();
        generation++;

        generationTimer = 0f;
    }
}