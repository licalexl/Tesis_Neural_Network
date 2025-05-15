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

    [Header("Configuraci�n de Reutilizaci�n")]
    [Tooltip("Si es true, reutiliza NPCs en lugar de destruirlos/crearlos")]
    public bool reuseNPCs = true;

    [Tooltip("Duraci�n de la inmunidad temporal al inicio de cada generaci�n (segundos)")]
    public float immunityDuration = 2f;

    [Tooltip("Si es true, los NPCs contin�an desde su posici�n actual; si es false, vuelven al spawn")]
    public bool continueFromCurrentPosition = false;




    private List<NPCController> unusedNPCs = new List<NPCController>();
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

        // Separar la poblaci�n por tipo
        var allies = population.Where(npc => npc.npcType == NPCController.NPCType.Ally).ToList();
        var enemies = population.Where(npc => npc.npcType == NPCController.NPCType.Enemy).ToList();

        // Ordenar NPCs por fitness
        allies = allies.OrderByDescending(c => c.fitness).ToList();
        enemies = enemies.OrderByDescending(c => c.fitness).ToList();

        // Crear nueva poblaci�n reutilizando los NPCs existentes
        int enemyCount = Mathf.RoundToInt(populationSize * enemyPercentage);
        int allyCount = populationSize - enemyCount;

        List<NPCController> newPopulation = new List<NPCController>();
        unusedNPCs.Clear();
        unusedNPCs.AddRange(population);

        // M�TODO MODIFICADO PARA REUTILIZACI�N
        if (reuseNPCs)
        {
            // Primero, a�adimos los mejores de cada tipo (elitismo)
            if (enemies.Count > 0)
            {
                NPCController bestEnemy = enemies[0];
                newPopulation.Add(bestEnemy);
                unusedNPCs.Remove(bestEnemy);
            }

            if (allies.Count > 0)
            {
                NPCController bestAlly = allies[0];
                newPopulation.Add(bestAlly);
                unusedNPCs.Remove(bestAlly);
            }

            // Completar la poblaci�n reutilizando NPCs existentes
            while (newPopulation.Count < populationSize)
            {
                NPCController.NPCType targetType = (newPopulation.Count < enemyCount) ?
                    NPCController.NPCType.Enemy : NPCController.NPCType.Ally;

                var parentPool = (targetType == NPCController.NPCType.Enemy) ? enemies : allies;

                if (parentPool.Count >= 2)
                {
                    NPCController parent1 = TournamentSelection(parentPool);
                    NPCController parent2 = TournamentSelection(parentPool);

                    // Obtener un NPC para reutilizar
                    NPCController childNPC = GetNPCToReuse(targetType);

                    // Configurar el NPC reutilizado
                    if (childNPC != null)
                    {
                        // Copiar cerebro del padre 1 y aplicar crossover con padre 2
                        childNPC.brain = parent1.brain.Copy();
                        childNPC.brain.Crossover(parent2.brain);

                        // Asegurar tipo correcto
                        childNPC.npcType = targetType;
                        childNPC.SetNPCColor();

                        // Aplicar inmunidad temporal
                        childNPC.ApplyTemporaryImmunity(immunityDuration);

                        // Decidir posici�n inicial
                        if (!continueFromCurrentPosition)
                        {
                            childNPC.transform.position = startPosition.position;
                            childNPC.transform.rotation = startPosition.rotation;
                        }

                        newPopulation.Add(childNPC);
                    }
                }
            }
        }

        population = newPopulation;
    }

    private NPCController GetNPCToReuse(NPCController.NPCType targetType)
    {
        // Primero intentamos encontrar un NPC del mismo tipo que no est� en uso
        var matchingTypeNPCs = unusedNPCs.Where(npc => npc.npcType == targetType).ToList();
        if (matchingTypeNPCs.Count > 0)
        {
            NPCController npc = matchingTypeNPCs[0];
            unusedNPCs.Remove(npc);
            return npc;
        }

        // Si no hay del mismo tipo, usamos cualquier tipo
        if (unusedNPCs.Count > 0)
        {
            NPCController npc = unusedNPCs[0];
            unusedNPCs.Remove(npc);
            return npc;
        }

        // Si no hay NPCs disponibles para reutilizar, creamos uno nuevo
        GameObject npcGO = Instantiate(npcPrefab, startPosition.position, startPosition.rotation);
        NPCController newNPC = npcGO.GetComponent<NPCController>();

        if (newNPC == null)
        {
            Debug.LogError("Error al crear nuevo NPC: No se encontr� el componente NPCController");
        }

        return newNPC;
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
                // Reiniciamos el NPC
                if (!continueFromCurrentPosition)
                {
                    // Reiniciar a la posici�n inicial
                    npc.Reset();
                }
                else
                {
                    // Solo reiniciamos el fitness y otros valores, mantenemos la posici�n
                    npc.ResetWithoutPosition();
                }

                // Aplicamos inmunidad
                npc.ApplyTemporaryImmunity(immunityDuration);
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