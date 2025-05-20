using UnityEngine;
using System.Collections.Generic;
using System;
using Random = UnityEngine.Random;


public class NPCController : MonoBehaviour
{
    // Enum para definir el tipo de NPC
    private List<bool> jumpNecessityHistory = new List<bool>();

    // Variables para estadísticas de salto
    [Header("Estadísticas de Salto")]
    [Tooltip("Número de saltos necesarios realizados")]
    public int necessaryJumps = 0;
    [Tooltip("Número de saltos innecesarios realizados")]
    public int unnecessaryJumps = 0;

    // Energía para limitar saltos excesivos
    [Header("Sistema de Energía")]
    [Tooltip("Energía actual del NPC")]
    public float energy = 100f;
    [Tooltip("Costo energético de un salto")]
    public float jumpEnergyCost = 5f;
    [Tooltip("Recuperación de energía por segundo")]
    public float energyRecoveryRate = 1f;



    [Header("Detección de Seguimiento de Paredes")]
    [Tooltip("Distancia máxima para considerar que está siguiendo una pared")]
    public float wallFollowingDistance = 2.0f;
    [Tooltip("Ángulo para la detección lateral de paredes")]
    public float wallDetectionAngle = 60f;
    [Tooltip("Tiempo continuo que debe seguir una pared para ser penalizado")]
    public float wallFollowingThreshold = 3.0f;
    [Tooltip("Penalización por seguir paredes")]
    public float wallFollowingPenalty = 2.0f;

    // Variables privadas para seguimiento
    private float wallFollowingTime = 0f;
    private bool isFollowingWall = false;
    private Vector3 lastWallPosition;

    [Header("Recompensa por Exploración Central")]
    [Tooltip("Bonus por explorar áreas centrales del mapa")]
    public float centralAreaBonus = 5f;
    [Tooltip("Radio considerado como área central")]
    public float centralAreaRadius = 15f;
    [Tooltip("Centro del mapa/nivel")]
    public Vector3 mapCenter = Vector3.zero;


    [Header("Sistema de Inmunidad")]
    [Tooltip("Indica si el NPC es inmune a colisiones")]
    public bool isImmune = false;

    [Tooltip("Material a usar durante la inmunidad")]
    public Material immunityMaterial;

    private Material originalMaterial;
    private float immunityTimeRemaining = 0f;
    private Renderer npcRenderer;



    public enum NPCType
    {
        Ally,
        Enemy
    }

    [Header("Tipo de NPC")]
    [Tooltip("Define si este NPC es aliado o enemigo")]
    public NPCType npcType = NPCType.Ally;

    [Tooltip("Color para visualizar el tipo de NPC")]
    public Color allyColor = Color.blue;
    public Color enemyColor = Color.red;

    [Header("Entradas/Salidas de la Red Neuronal")]
    [Tooltip("Valores de los 7 sensores, entrada para la red neuronal")]
    public float[] inputs = new float[8]; // 7 sensores + 1 entrada constante

    [Tooltip("Valores de salida de la red neuronal: avanzar, girar izquierda, girar derecha, saltar")]
    public float[] outputs = new float[4]; // Avanzar, girar izquierda, girar derecha, saltar

    [Header("IA y Fitness")]

    [Tooltip("Red neuronal para navegación")]
    public NeuralNetwork navigationBrain;

    [Tooltip("Red neuronal para combate")]
    public NeuralNetwork combatBrain;

    [Tooltip("Máquina de estados que controla el comportamiento")]
    private NPCStateMachine stateMachine;

    [Tooltip("Red neuronal que toma decisiones para este NPC")]
    public NeuralNetwork brain;

    [Tooltip("Puntuación de rendimiento para la selección genética")]
    public float fitness = 0;

    [Tooltip("Distancia total recorrida por el NPC")]
    public float totalDistance = 0;

    [Tooltip("Tiempo que ha sobrevivido el NPC")]
    public float timeAlive = 0;

    [Tooltip("Saltos exitosos realizados")]
    public int successfulJumps = 0;

    // Entradas adicionales para la red de combate
    [Tooltip("Valores de los sensores de combate, entrada para la red de combate")]
    public float[] combatInputs = new float[8]; // 7 sensores específicos de combate + 1 constante

    // Salidas de la red de combate
    [Tooltip("Valores de salida de la red de combate: atacar, bloquear, esquivar, contraatacar")]
    public float[] combatOutputs = new float[4];


    [Header("Configuración de Movimiento")]
    [Tooltip("Velocidad máxima de movimiento")]
    public float moveSpeed = 5f;

    [Tooltip("Velocidad mínima para no considerarse inactivo")]
    public float minSpeed = 0.5f;

    [Tooltip("Velocidad de rotación en grados por segundo")]
    public float rotationSpeed = 120f;

    [Header("Configuración de Salto")]
    [Tooltip("Fuerza de salto")]
    public float jumpForce = 5f;

    [Tooltip("Cooldown entre saltos")]
    public float jumpCooldown = 1f;

    [Tooltip("LayerMask para detectar el suelo")]
    public LayerMask groundLayer;

    [Header("Configuración de Sensores")]
    [Tooltip("Distancia máxima de detección de los sensores")]
    public float sensorLength = 10f;

    [Tooltip("Desplazamiento hacia adelante de los sensores desde el centro del NPC")]
    public float sensorForwardOffset = 1.0f;

    [Tooltip("Altura de los sensores desde la base del NPC")]
    public float sensorHeight = 1.0f;

    [Tooltip("Altura del sensor inferior para detectar obstáculos saltables")]
    public float lowerSensorHeight = 0.2f;

    [Header("Estado")]
    [Tooltip("Tiempo máximo que puede permanecer inactivo antes de 'morir'")]
    public float maxIdleTime = 5f;

    [Tooltip("Indica si el NPC ha 'muerto' (colisión o inactividad)")]
    public bool isDead = false;

    [Header("Anti-Loop System")]
    [Tooltip("Radio para detectar comportamiento circular")]
    public float loopDetectionRadius = 5f;

    [Tooltip("Tiempo mínimo entre checkpoints para evitar loops")]
    public float checkpointInterval = 3f;

    [Tooltip("Distancia mínima para considerar un nuevo checkpoint")]
    public float minCheckpointDistance = 5f;

    [Tooltip("Penalización por comportamiento circular")]
    public float loopPenalty = 10f;

    [Header("Exploration Rewards")]
    [Tooltip("Recompensa por explorar nuevas áreas")]
    public float explorationBonus = 2f;

    [Tooltip("Tamaño de la cuadrícula para dividir el mapa")]
    public float gridSize = 5f;


    // Variables privadas para el funcionamiento interno
    private float idleTime = 0f;
    private Vector3 lastPosition;
    private Vector3 startPosition;
    private Quaternion startRotation;
    public NPCGeneticAlgorithm geneticAlgorithm;

    // Componentes
    private Rigidbody rb;
    private Animator animator;

    // Variables para el salto
    private bool isGrounded = false;
    private float lastJumpTime = -1f;

    // Variables para anti-loop y exploración
    private List<Vector3> positionHistory = new List<Vector3>();
    private float lastCheckpointTime = 0f;
    private Vector3 lastCheckpointPosition;
    private HashSet<Vector2Int> visitedCells = new HashSet<Vector2Int>();
    private int uniqueAreasVisited = 0;
    private float distanceFromStart = 0f;
    public int consecutiveCircles = 0;
    private float lastAngle = 0f;
    private float totalRotation = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        npcRenderer = GetComponent<Renderer>();

        if (npcRenderer != null)
        {
            originalMaterial = npcRenderer.material;
        }

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        startPosition = transform.position;
        startRotation = transform.rotation;
        lastPosition = transform.position;
        lastCheckpointPosition = transform.position;
    }

    public void ApplyTemporaryImmunity(float duration)
    {
        isImmune = true;
        immunityTimeRemaining = duration;

        // Cambiar apariencia para indicar inmunidad
        if (npcRenderer != null && immunityMaterial != null)
        {
            npcRenderer.material = immunityMaterial;
        }
        else
        {
            // Si no hay material específico, usar un efecto de transparencia
            if (npcRenderer != null)
            {
                Color tempColor = npcRenderer.material.color;
                tempColor.a = 0.5f; // Semi-transparente
                npcRenderer.material.color = tempColor;
            }
        }
    }
    void Start()
    {
        // Inicialización existente
        if (navigationBrain == null)
        {
            navigationBrain = new NeuralNetwork(NetworkType.Navigation, 8, 8, 6, 4);
        }

        // Inicializar la red de combate
        if (combatBrain == null)
        {
            combatBrain = new NeuralNetwork(NetworkType.Combat, 8, 10, 6, 4);
        }

        // Obtener la máquina de estados
        stateMachine = GetComponent<NPCStateMachine>();
        if (stateMachine == null)
        {
            stateMachine = gameObject.AddComponent<NPCStateMachine>();
        }

        if (geneticAlgorithm == null)
        {
            geneticAlgorithm = FindObjectOfType<NPCGeneticAlgorithm>();
        }
        if (brain == null)
        {
            // Estructura actualizada de la red neuronal:
            // - 8 neuronas de entrada (7 sensores + 1 constante)
            // - 8 neuronas en primera capa oculta
            // - 6 neuronas en segunda capa oculta
            // - 4 neuronas de salida (acciones)
            brain = new NeuralNetwork(8, 8, 6, 4);
        }

        // Establecer color según el tipo
        SetNPCColor();

        // Registrar con el sistema de checkpoints
        if (CheckpointSystem.Instance != null)
        {
            CheckpointSystem.Instance.RegisterNPC(this);
        }
    }

    void Update()
    {
        if (isDead) return;

        // Actualizar inmunidad
        if (isImmune)
        {
            immunityTimeRemaining -= Time.deltaTime;

            if (immunityTimeRemaining <= 0)
            {
                // Terminar inmunidad
                isImmune = false;

                // Restaurar apariencia
                if (npcRenderer != null)
                {
                    if (originalMaterial != null)
                    {
                        npcRenderer.material = originalMaterial;
                    }

                    // Asegurar color correcto
                    SetNPCColor();
                }
            }
        }

        UpdateSensors();
        outputs = brain.FeedForward(inputs);

        if (stateMachine.GetCurrentState() == NPCStateMachine.NPCState.Navigation)
        {
            UpdateSensors();
            outputs = navigationBrain.FeedForward(inputs);
        }
        else // Estado de combate
        {
            UpdateCombatSensors();
            combatOutputs = combatBrain.FeedForward(combatInputs);
        }

        timeAlive += Time.deltaTime;
    }

    void FixedUpdate()
    {
        if (isDead) return;

        // Verificar si está en el suelo
        CheckGrounded();

        // Recuperar energía con el tiempo
        if (energy < 100f)
        {
            energy += energyRecoveryRate * Time.fixedDeltaTime;
            energy = Mathf.Min(energy, 100f); // Limitar a 100
        }

        // Aplicamos las acciones determinadas por la red neuronal
        ApplyOutputs();

        // Nuevas verificaciones anti-loop
        CheckForLoopingBehavior();
        UpdateExplorationTracking();

        // Verificar seguimiento de paredes
        CheckWallFollowing();

        // Actualizamos la puntuación de fitness
        UpdateFitness();

        // Verificamos si el NPC está inactivo por demasiado tiempo
        CheckIdleStatus();
    }

    void CheckWallFollowing()
    {
        // Configurar los rayos laterales para detectar paredes
        Vector3 leftRayStart = transform.position + Vector3.up * sensorHeight;
        Vector3 rightRayStart = leftRayStart;
        Vector3 leftRayDir = Quaternion.Euler(0, -wallDetectionAngle, 0) * transform.forward;
        Vector3 rightRayDir = Quaternion.Euler(0, wallDetectionAngle, 0) * transform.forward;

        RaycastHit leftHit, rightHit;
        bool leftWallDetected = Physics.Raycast(leftRayStart, leftRayDir, out leftHit, wallFollowingDistance);
        bool rightWallDetected = Physics.Raycast(rightRayStart, rightRayDir, out rightHit, wallFollowingDistance);

        // Visualización para depuración
        Debug.DrawRay(leftRayStart, leftRayDir * wallFollowingDistance, leftWallDetected ? Color.red : Color.green);
        Debug.DrawRay(rightRayStart, rightRayDir * wallFollowingDistance, rightWallDetected ? Color.red : Color.green);

        // Determinar si está siguiendo una pared
        bool currentlyFollowingWall = leftWallDetected || rightWallDetected;

        // Determinar si está siguiendo la misma pared
        bool samePath = false;
        if (currentlyFollowingWall)
        {
            Vector3 currentWallPos = leftWallDetected ? leftHit.point : rightHit.point;
            if (isFollowingWall)
            {
                // Si la dirección de movimiento es aproximadamente paralela a la pared
                Vector3 movementDir = transform.forward;
                Vector3 wallDir = Vector3.Cross(leftWallDetected ? leftHit.normal : rightHit.normal, Vector3.up).normalized;
                float angleWithWall = Vector3.Angle(movementDir, wallDir);

                // Considera que sigue la pared si se mueve aproximadamente paralelo a ella
                samePath = (angleWithWall < 45f || angleWithWall > 135f);
            }
            lastWallPosition = currentWallPos;
        }

        // Actualizar tiempo de seguimiento de pared
        if (currentlyFollowingWall && samePath)
        {
            if (!isFollowingWall)
            {
                // Iniciando nuevo seguimiento de pared
                isFollowingWall = true;
            }
            wallFollowingTime += Time.deltaTime;
        }
        else
        {
            // Reiniciar si no está siguiendo pared o cambió de pared
            isFollowingWall = currentlyFollowingWall;
            wallFollowingTime = 0f;
        }

        // Aplicar penalización si supera el umbral
        if (wallFollowingTime > wallFollowingThreshold)
        {
            // Penalización progresiva para desincentivar el comportamiento
            float penalty = wallFollowingPenalty * (wallFollowingTime - wallFollowingThreshold);
            fitness -= penalty * Time.deltaTime;

            // Para depuración
            if (Time.frameCount % 60 == 0) // Cada ~1 segundo
            {
                Debug.Log($"NPC {name} penalizado por seguir pared durante {wallFollowingTime:F1}s. Penalización: {penalty:F1}");
            }
        }
    }

    void UpdateCombatSensors()
    {
        if (combatInputs.Length < 8)
        {
            combatInputs = new float[8];
        }

        // Posición de los sensores (similar a los sensores de navegación)
        Vector3 sensorStartPos = transform.position + transform.forward * sensorForwardOffset + Vector3.up * sensorHeight;

        // 1. Sensor de distancia al enemigo
        if (stateMachine.currentTarget != null)
        {
            float distanceToEnemy = Vector3.Distance(transform.position, stateMachine.currentTarget.position);
            combatInputs[0] = 1.0f - Mathf.Clamp01(distanceToEnemy / stateMachine.combatDetectionRadius);
        }
        else
        {
            combatInputs[0] = 0f;
        }

        // 2-4. Sensores de dirección al enemigo (adelante, izquierda, derecha)
        if (stateMachine.currentTarget != null)
        {
            Vector3 directionToEnemy = (stateMachine.currentTarget.position - transform.position).normalized;

            // Sensor frontal
            float forwardDot = Vector3.Dot(transform.forward, directionToEnemy);
            combatInputs[1] = Mathf.Max(0, forwardDot);

            // Sensor izquierdo
            Vector3 leftDir = Quaternion.Euler(0, -90, 0) * transform.forward;
            float leftDot = Vector3.Dot(leftDir, directionToEnemy);
            combatInputs[2] = Mathf.Max(0, leftDot);

            // Sensor derecho
            Vector3 rightDir = Quaternion.Euler(0, 90, 0) * transform.forward;
            float rightDot = Vector3.Dot(rightDir, directionToEnemy);
            combatInputs[3] = Mathf.Max(0, rightDot);
        }
        else
        {
            combatInputs[1] = 0f;
            combatInputs[2] = 0f;
            combatInputs[3] = 0f;
        }

        // 5. Sensor de "enemigo atacando" 
        if (stateMachine.currentTarget != null)
        {
            RaycastHit hit;
            Vector3 enemyDirection = (stateMachine.currentTarget.position - transform.position).normalized;
            if (Physics.Raycast(stateMachine.currentTarget.position, enemyDirection, out hit, 2f))
            {
                if (hit.transform == transform)
                {
                    combatInputs[4] = 1.0f; // El enemigo está orientado hacia nosotros y cerca
                }
                else
                {
                    combatInputs[4] = 0.5f; // El enemigo está orientado hacia nosotros pero hay obstáculos
                }
            }
            else
            {
                combatInputs[4] = 0f; // El enemigo no está orientado hacia nosotros
            }
        }
        else
        {
            combatInputs[4] = 0f;
        }

        // 6. Sensor de salud/energía
        combatInputs[5] = energy / 100f;

        // 7. Sensor de "aliados cercanos"
        Collider[] nearbyAllies = Physics.OverlapSphere(transform.position, stateMachine.combatDetectionRadius);
        int allyCount = 0;

        foreach (var nearbyCollider in nearbyAllies)
        {
            NPCController otherNPC = nearbyCollider.GetComponent<NPCController>();
            if (otherNPC != null && otherNPC != this && otherNPC.npcType == npcType)
            {
                allyCount++;
            }
        }

        combatInputs[6] = Mathf.Min(1.0f, allyCount / 5.0f); // Normalizado a max 5 aliados

        // 8. Entrada constante (bias)
        combatInputs[7] = 1.0f;
    }


    public void OnStateChanged(NPCStateMachine.NPCState newState)
    {
        // lógica de cambiar de estado
        switch (newState)
        {
            case NPCStateMachine.NPCState.Navigation:
                // Configurar para navegación
                break;

            case NPCStateMachine.NPCState.Combat:
                // Configurar para combate 
                break;
        }
    }
    void UpdateSensors()
    {
        if (inputs.Length < 8)
        {
            inputs = new float[8];
            Debug.LogWarning("Array inputs recreado con tamaño 8");
        }

        // Posición desde donde parten los rayos de los sensores normales
        Vector3 sensorStartPos = transform.position + transform.forward * sensorForwardOffset + Vector3.up * sensorHeight;

        // Posición para el sensor bajo (detecta obstáculos saltables)
        Vector3 lowerSensorStartPos = transform.position + transform.forward * sensorForwardOffset + Vector3.up * lowerSensorHeight;

        // Actualizamos los 5 sensores originales con detección de aliados/enemigos
        for (int i = 0; i < 5; i++)
        {
            RaycastHit hit;
            Vector3 sensorDirection = Quaternion.Euler(0, -90 + 45 * i, 0) * transform.forward;

            if (Physics.Raycast(sensorStartPos, sensorDirection, out hit, sensorLength))
            {
                // Verificar si el objeto detectado es otro NPC
                NPCController otherNPC = hit.collider.GetComponent<NPCController>();
                if (otherNPC != null)
                {
                    float distanceValue = 1 - (hit.distance / sensorLength);

                    if (otherNPC.npcType == this.npcType)
                    {
                        // Es un aliado, usar un valor diferente
                        inputs[i] = -distanceValue * 0.5f;
                        Debug.DrawRay(sensorStartPos, sensorDirection * hit.distance, Color.green);
                    }
                    else
                    {
                        // Es un enemigo, valor positivo más alto
                        inputs[i] = distanceValue * 1.5f;
                        Debug.DrawRay(sensorStartPos, sensorDirection * hit.distance, Color.red);
                    }
                }
                else
                {
                    // Es un obstáculo normal
                    inputs[i] = 1 - (hit.distance / sensorLength);
                    Debug.DrawRay(sensorStartPos, sensorDirection * hit.distance, Color.yellow);
                }
            }
            else
            {
                inputs[i] = 0f;
                Debug.DrawRay(sensorStartPos, sensorDirection * sensorLength, Color.white);
            }
        }

        // Sensor 6: Frontal bajo para detectar obstáculos saltables
        RaycastHit lowerHit;
        if (Physics.Raycast(lowerSensorStartPos, transform.forward, out lowerHit, sensorLength))
        {
            inputs[5] = 1 - (lowerHit.distance / sensorLength);
            Debug.DrawRay(lowerSensorStartPos, transform.forward * lowerHit.distance, Color.yellow);
        }
        else
        {
            inputs[5] = 0f;
            Debug.DrawRay(lowerSensorStartPos, transform.forward * sensorLength, Color.blue);
        }

        // Sensor 7: Frontal alto para detectar obstáculos que no se pueden saltar
        RaycastHit upperHit;
        Vector3 upperSensorStartPos = transform.position + transform.forward * sensorForwardOffset + Vector3.up * (sensorHeight * 1.5f);
        if (Physics.Raycast(upperSensorStartPos, transform.forward, out upperHit, sensorLength))
        {
            inputs[6] = 1 - (upperHit.distance / sensorLength);
            Debug.DrawRay(upperSensorStartPos, transform.forward * upperHit.distance, Color.magenta);
        }
        else
        {
            inputs[6] = 0f;
            Debug.DrawRay(upperSensorStartPos, transform.forward * sensorLength, Color.cyan);
        }

        // Entrada constante (bias)
        inputs[7] = 1f;
    }

    void ApplyCombatOutputs()
    {
        if (combatOutputs.Length < 4)
        {
            Array.Resize(ref combatOutputs, 4);
        }

        // Las salidas de la red de combate son:
        // [0] = Atacar
        // [1] = Bloquear
        // [2] = Esquivar
        // [3] = Contraatacar

        // Determinar la acción con mayor valor
        int maxOutputIndex = 0;
        float maxOutputValue = combatOutputs[0];

        for (int i = 1; i < combatOutputs.Length; i++)
        {
            if (combatOutputs[i] > maxOutputValue)
            {
                maxOutputValue = combatOutputs[i];
                maxOutputIndex = i;
            }
        }

        // Si el valor máximo es mayor que un umbral, ejecutar esa acción
        if (maxOutputValue > 0.5f)
        {
            switch (maxOutputIndex)
            {
                case 0: // Atacar
                    PerformAttack();
                    break;
                case 1: // Bloquear
                    PerformBlock();
                    break;
                case 2: // Esquivar
                    PerformDodge();
                    break;
                case 3: // Contraatacar
                    PerformCounterAttack();
                    break;
            }
        }
        else
        {
            // Si ninguna acción supera el umbral, moverse hacia el enemigo
            if (stateMachine.currentTarget != null)
            {
                MoveTowardsEnemy();
            }
        }
    }


    private void PerformAttack()
    {
        if (stateMachine.currentTarget == null) return;

        // Orientarse hacia el enemigo
        Vector3 directionToEnemy = (stateMachine.currentTarget.position - transform.position).normalized;
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            Quaternion.LookRotation(new Vector3(directionToEnemy.x, 0, directionToEnemy.z)),
            rotationSpeed * Time.deltaTime
        );

        // Si estamos lo suficientemente cerca y orientados hacia el enemigo
        float distanceToEnemy = Vector3.Distance(transform.position, stateMachine.currentTarget.position);
        float dotProduct = Vector3.Dot(transform.forward, directionToEnemy);

        if (distanceToEnemy < 2.0f && dotProduct > 0.8f)
        {
            // Realizar ataque (lanzar raycast para simular ataque)
            RaycastHit hit;
            if (Physics.Raycast(transform.position + Vector3.up, transform.forward, out hit, 2.0f))
            {
                NPCController enemyNPC = hit.collider.GetComponent<NPCController>();
                if (enemyNPC != null && enemyNPC.npcType != npcType)
                {
                    // Ataque exitoso, añadir recompensa al fitness
                    fitness += 5.0f;

                    // Reducir energía del enemigo (en un sistema real)
                    // enemyNPC.TakeDamage(10);
                }
            }

            // Aplicar un pequeño retroceso tras el ataque
            rb.AddForce(-transform.forward * 3.0f, ForceMode.Impulse);
        }
        else
        {
            // Moverse hacia el enemigo si está lejos
            MoveTowardsEnemy();
        }
    }

    private void PerformBlock()
    {
        // Estar quieto y orientado hacia el enemigo para bloquear
        if (stateMachine.currentTarget != null)
        {
            Vector3 directionToEnemy = (stateMachine.currentTarget.position - transform.position).normalized;
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.LookRotation(new Vector3(directionToEnemy.x, 0, directionToEnemy.z)),
                rotationSpeed * 2 * Time.deltaTime
            );

            // Velocidad reducida mientras bloquea
            rb.velocity = Vector3.zero;

            // Si un enemigo está atacando (sensor 4 alto), recompensar por bloquear
            if (combatInputs[4] > 0.7f)
            {
                // Bloqueo exitoso
                fitness += 3.0f;
            }
        }
    }

    private void PerformDodge()
    {
        if (stateMachine.currentTarget == null) return;

        Vector3 directionToEnemy = (stateMachine.currentTarget.position - transform.position).normalized;
        Vector3 rightDirection = Vector3.Cross(Vector3.up, directionToEnemy).normalized;

        // Determinar dirección de esquiva (derecha o izquierda del enemigo)
        Vector3 dodgeDirection = Vector3.Dot(rightDirection, transform.right) > 0 ? rightDirection : -rightDirection;

        // Aplicar fuerza para esquivar
        rb.AddForce(dodgeDirection * moveSpeed * 1.5f, ForceMode.VelocityChange);

        // Si un enemigo está atacando (sensor 4 alto), recompensar por esquivar
        if (combatInputs[4] > 0.7f)
        {
            // Esquiva exitosa
            fitness += 4.0f;
        }
    }

    private void PerformCounterAttack()
    {
        // Contraatacar es una combinación de esquivar y atacar
        // Primero esquivar
        PerformDodge();

        // Luego atacar si el enemigo está atacando
        if (combatInputs[4] > 0.7f)
        {
            PerformAttack();
            // Bonificación adicional por contraatacar
            fitness += 2.0f;
        }
    }

    private void MoveTowardsEnemy()
    {
        if (stateMachine.currentTarget == null) return;

        Vector3 directionToEnemy = (stateMachine.currentTarget.position - transform.position).normalized;

        // Orientarse hacia el enemigo
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            Quaternion.LookRotation(new Vector3(directionToEnemy.x, 0, directionToEnemy.z)),
            rotationSpeed * Time.deltaTime
        );

        // Moverse hacia el enemigo
        rb.velocity = new Vector3(0, rb.velocity.y, 0) + transform.forward * moveSpeed * 0.8f;
    }
    void ApplyOutputs()
    {
        if (outputs.Length < 4)
        {
            // Si el array no es del tamaño correcto, redimensionarlo
            Array.Resize(ref outputs, 4);
            Debug.LogWarning("Array outputs redimensionado a tamaño 4");
        }
        // Añade ruido aleatorio al principio del entrenamiento
        float explorationFactor = Mathf.Max(0, 50 - geneticAlgorithm.generation) / 50f;
        float randomNoise = Random.Range(0f, 0.5f) * explorationFactor;

        float forwardSpeed = Mathf.Clamp(outputs[0] + 0.3f + randomNoise, 0, 1) * moveSpeed;
        float turnAmount = (outputs[1] - outputs[2]) * rotationSpeed;

        // Aseguramos una velocidad mínima
        if (forwardSpeed > 0)
        {
            forwardSpeed = Mathf.Max(forwardSpeed, minSpeed);
        }

        // Aplicamos el movimiento al Rigidbody
        rb.velocity = new Vector3(0, rb.velocity.y, 0) + transform.forward * forwardSpeed;

        // Aplicamos la rotación al transform
        transform.Rotate(Vector3.up * turnAmount * Time.fixedDeltaTime);

        // Manejo del salto
        if (outputs.Length >= 4 && outputs[3] > 0.5f && CanJump())
        {
            Jump();
        }

        if (animator != null)
        {
           // animator.SetFloat("Speed", forwardSpeed / moveSpeed);
           // animator.SetFloat("Turn", turnAmount / rotationSpeed);
          //  animator.SetBool("IsGrounded", isGrounded);
          //  animator.SetTrigger("Jump"); // Si existe un trigger de salto en el animator
        }

        // Calculamos la distancia recorrida desde el último frame
        float distanceMoved = Vector3.Distance(transform.position, lastPosition);
        totalDistance += distanceMoved;
        lastPosition = transform.position;
    }

    bool CanJump()
    {
        return isGrounded && (Time.time - lastJumpTime > jumpCooldown);
    }

    void Jump()
    {
        // Verificar si tenemos suficiente energía
        if (energy < jumpEnergyCost)
        {
            return; // No saltar si no hay suficiente energía
        }

        // Determinar si este salto es necesario
        bool jumpIsNecessary = DetermineIfJumpNecessary();
        jumpNecessityHistory.Add(jumpIsNecessary);

        // Actualizar contadores
        if (jumpIsNecessary)
        {
            necessaryJumps++;
        }
        else
        {
            unnecessaryJumps++;
        }

        // Aplicar costo energético del salto
        energy -= jumpEnergyCost;

        // Realizar el salto físico
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        lastJumpTime = Time.time;
        isGrounded = false;
        successfulJumps++;

        // Actualizar animaciones si es necesario
        if (animator != null)
        {
           // animator.SetTrigger("Jump");
        }
    }

    bool DetermineIfJumpNecessary()
    {
        // Salto necesario si hay un obstáculo bajo pero no uno alto
        bool lowObstacleDetected = inputs[5] > 0.3f; // Obstáculo bajo/medio detectado
        bool highObstacleDetected = inputs[6] > 0.4f; // Obstáculo alto detectado

        // Un salto es necesario cuando:
        // 1. Hay un obstáculo bajo o medio que se puede saltar
        // 2. No hay un obstáculo alto que nos impediría saltar
        // 3. Estamos en el suelo para poder saltar
        return lowObstacleDetected && !highObstacleDetected && isGrounded;
    }

    void CheckGrounded()
    {
        float rayDistance = 0.1f;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.05f;

        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, rayDistance, groundLayer))
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }

        Debug.DrawRay(rayOrigin, Vector3.down * rayDistance, isGrounded ? Color.green : Color.red);
    }

    void CheckForLoopingBehavior()
    {
        // Registrar la posición cada cierto intervalo
        if (Time.time - lastCheckpointTime > checkpointInterval)
        {
            Vector3 currentPos = transform.position;
            float distanceFromCheckpoint = Vector3.Distance(currentPos, lastCheckpointPosition);

            // Si no se ha movido lo suficiente desde el último checkpoint
            if (distanceFromCheckpoint < minCheckpointDistance)
            {
                consecutiveCircles++;
                fitness -= loopPenalty * consecutiveCircles; // Penalización progresiva

                // NUEVA FUNCIONALIDAD: Terminar NPC después de 3 bucles consecutivos
                if (consecutiveCircles >= 3)
                {
                    isDead = true;
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    Debug.Log($"NPC {name} terminado por comportamiento de bucle persistente");
                }
            }
            else
            {
                consecutiveCircles = 0; // Resetear si se mueve significativamente
            }

            lastCheckpointPosition = currentPos;
            lastCheckpointTime = Time.time;
            positionHistory.Add(currentPos);

            // Mantener el historial limitado
            if (positionHistory.Count > 10)
            {
                positionHistory.RemoveAt(0);
            }
        }

        // Detectar rotación excesiva (indicador de círculos)
        float currentAngle = transform.eulerAngles.y;
        float angleDelta = Mathf.DeltaAngle(lastAngle, currentAngle);
        totalRotation += Mathf.Abs(angleDelta);
        lastAngle = currentAngle;

        // Penalizar si rota demasiado en relación a la distancia recorrida
        if (totalDistance > 0 && totalRotation / totalDistance > 10f)
        {
            fitness -= loopPenalty * 0.1f;
        }
    }

    void UpdateExplorationTracking()
    {
        // Convertir la posición actual a coordenadas de la cuadrícula
        Vector2Int gridCell = new Vector2Int(
            Mathf.FloorToInt(transform.position.x / gridSize),
            Mathf.FloorToInt(transform.position.z / gridSize)
        );

        // Si es una nueva celda, premiar la exploración
        if (!visitedCells.Contains(gridCell))
        {
            visitedCells.Add(gridCell);
            uniqueAreasVisited++;
            fitness += explorationBonus;
        }

        // Actualizar distancia desde el punto de inicio
        distanceFromStart = Vector3.Distance(transform.position, startPosition);

        // Recompensa por explorar área central
        float distanceToCenter = Vector3.Distance(
            new Vector3(transform.position.x, 0, transform.position.z),
            new Vector3(mapCenter.x, 0, mapCenter.z));

        // Si el NPC está explorando la zona central
        if (distanceToCenter < centralAreaRadius)
        {
            // La recompensa es mayor cuanto más cerca esté del centro
            float centerBonus = centralAreaBonus * (1 - (distanceToCenter / centralAreaRadius));
            fitness += centerBonus * Time.deltaTime * 0.1f; // Escalar para no hacer la bonificación demasiado grande
        }
    }

    public void UpdateVisualBasedOnLocks()
    {
        if (brain == null) return;

        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            // Color base según tipo
            Color baseColor = (npcType == NPCType.Ally) ? allyColor : enemyColor;

            // Verificar si tiene comportamientos bloqueados
            bool[] lockStatus = brain.GetOutputLockStatus();
            int lockedCount = 0;

            foreach (bool locked in lockStatus)
            {
                if (locked) lockedCount++;
            }

            // indicador visual basado en cuántos comportamientos están bloqueados
            if (lockedCount > 0)
            {
                // tinte dorado para indicar bloqueo y su intensidad
                float lockIntensity = (float)lockedCount / lockStatus.Length * 0.5f;
                baseColor = Color.Lerp(baseColor, Color.yellow, lockIntensity);

                // mejora posible futura para mi codigo donde puedo añadir algún efecto de partículas o un objeto hijo como indicador visual
            }

            renderer.material.color = baseColor;
        }
    }

    void UpdateFitness()
    {
        // Nueva fórmula de fitness
        float baseReward = 0f;

        // 1. Recompensa por distancia real recorrida
        baseReward += totalDistance * 0.5f;

        // 2. Recompensa por exploración (áreas únicas visitadas)
        baseReward += uniqueAreasVisited * explorationBonus;

        // 3. Recompensa por distancia desde el punto de inicio
        baseReward += distanceFromStart * 0.3f;

        // 4. Sistema mejorado de recompensa por saltos
        float jumpReward = necessaryJumps * 10f; // Mayor recompensa por saltos necesarios
        float jumpPenalty = unnecessaryJumps * 3f; // Penalización por saltos innecesarios
        baseReward += jumpReward - jumpPenalty;

        // 5. Penalización por tiempo (para evitar que el tiempo sea lo único que importa)
        float timePenalty = Mathf.Min(timeAlive * 0.1f, 10f); // Limitar la penalización

        // 6. Penalización por comportamiento repetitivo
        float repetitivePenalty = consecutiveCircles * loopPenalty;

        // 7. Recompensa por checkpoints
        if (CheckpointSystem.Instance != null)
        {
            float checkpointBonus = CheckpointSystem.Instance.CheckCheckpoints(this);
            baseReward += checkpointBonus;
        }

        // Calcular fitness final
        fitness = baseReward - timePenalty - repetitivePenalty;

        fitness = Mathf.Max(-100f, fitness);
    }

    void CheckIdleStatus()
    {
        float currentSpeed = rb.velocity.magnitude;

        if (currentSpeed < minSpeed)
        {
            idleTime += Time.fixedDeltaTime;

            if (idleTime > maxIdleTime)
            {
                isDead = true;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
        else
        {
            idleTime = 0f;
        }
    }

    public void ResetWithoutPosition()
    {
        if (rb != null)
        {
            // No cambiamos la posición ni rotación
            Vector3 currentPosition = transform.position;
            Quaternion currentRotation = transform.rotation;

            // Guardamos la posición actual como nueva posición de inicio
            lastPosition = currentPosition;

            isDead = false;
            fitness = 0;
            totalDistance = 0;
            timeAlive = 0;
            idleTime = 0f;
            successfulJumps = 0;
            lastJumpTime = -1f;

            // Resetear variables adicionales
            jumpNecessityHistory.Clear();
            necessaryJumps = 0;
            unnecessaryJumps = 0;
            energy = 100f;

            // Resetear variables de seguimiento de paredes y loops
            wallFollowingTime = 0f;
            isFollowingWall = false;
            consecutiveCircles = 0;
            totalRotation = 0f;
            lastAngle = transform.rotation.eulerAngles.y;

            // Mantener historial de posiciones pero actualizar el último checkpoint
            lastCheckpointPosition = currentPosition;
            lastCheckpointTime = 0f;

            // Conservamos el historial de celdas visitadas (para continuar la exploración)
            uniqueAreasVisited = visitedCells.Count;

            // Actualizar distancia desde punto de inicio original
            distanceFromStart = Vector3.Distance(currentPosition, startPosition);

            // Detener movimiento
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // Resetear checkpoints (opcional, depende de tu diseño)
            if (CheckpointSystem.Instance != null)
            {
                CheckpointSystem.Instance.ResetNPCCheckpoints(this);
            }
        }
        else
        {
            Debug.LogError("No se encontró el Rigidbody");
        }
    }
    void OnCollisionEnter(Collision collision)
    {
        // Si está inmune, ignorar colisiones con obstáculos
        if (isImmune) return;

        if (collision.gameObject.CompareTag("Obstacle"))
        {
            isDead = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void Reset()
    {
        if (rb != null)
        {
            transform.position = startPosition;
            transform.rotation = startRotation;
            lastPosition = startPosition;

            isDead = false;
            fitness = 0;
            totalDistance = 0;
            timeAlive = 0;
            idleTime = 0f;
            successfulJumps = 0;
            lastJumpTime = -1f;

            // Reiniciar variables de salto
            jumpNecessityHistory.Clear();
            necessaryJumps = 0;
            unnecessaryJumps = 0;
            energy = 100f; // Restaurar energía

            // Reiniciar variables de seguimiento de paredes
            wallFollowingTime = 0f;
            isFollowingWall = false;

            // Resetear variables anti-loop y exploración
            positionHistory.Clear();
            lastCheckpointTime = 0f;
            lastCheckpointPosition = startPosition;
            visitedCells.Clear();
            uniqueAreasVisited = 0;
            distanceFromStart = 0f;
            consecutiveCircles = 0;
            totalRotation = 0f;
            lastAngle = transform.rotation.eulerAngles.y;

            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // Resetear checkpoints
            if (CheckpointSystem.Instance != null)
            {
                CheckpointSystem.Instance.ResetNPCCheckpoints(this);
            }
        }
        else
        {
            Debug.LogError("No se encontró el Rigidbody");
        }
    }

    public void SetNPCColor()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = (npcType == NPCType.Ally) ? allyColor : enemyColor;
        }
    }

    void OnDestroy()
    {
        // Desregistrar del sistema de checkpoints
        if (CheckpointSystem.Instance != null)
        {
            CheckpointSystem.Instance.UnregisterNPC(this);
        }
    }

    // Método para visualizar el comportamiento (útil para debugging)
    void OnDrawGizmos()
    {
        if (Application.isPlaying && positionHistory.Count > 1)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < positionHistory.Count - 1; i++)
            {
                Gizmos.DrawLine(positionHistory[i], positionHistory[i + 1]);
            }

            // Dibujar área de detección de loops
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(lastCheckpointPosition, loopDetectionRadius);
        }
    }
}