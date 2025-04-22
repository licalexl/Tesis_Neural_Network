using UnityEngine;
using System.Collections.Generic;
using System;
using Random = UnityEngine.Random;

// Controla el comportamiento individual de cada NPC.
// Maneja los sensores, la red neuronal, el movimiento y el c�lculo de fitness.
// Incluye soporte para saltos, detecci�n de aliados/enemigos y prevenci�n de loops.

public class NPCController : MonoBehaviour
{
    // Enum para definir el tipo de NPC
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
    [Tooltip("Red neuronal que toma decisiones para este NPC")]
    public NeuralNetwork brain;

    [Tooltip("Puntuaci�n de rendimiento para la selecci�n gen�tica")]
    public float fitness = 0;

    [Tooltip("Distancia total recorrida por el NPC")]
    public float totalDistance = 0;

    [Tooltip("Tiempo que ha sobrevivido el NPC")]
    public float timeAlive = 0;

    [Tooltip("Saltos exitosos realizados")]
    public int successfulJumps = 0;

    [Header("Configuraci�n de Movimiento")]
    [Tooltip("Velocidad m�xima de movimiento")]
    public float moveSpeed = 5f;

    [Tooltip("Velocidad m�nima para no considerarse inactivo")]
    public float minSpeed = 0.5f;

    [Tooltip("Velocidad de rotaci�n en grados por segundo")]
    public float rotationSpeed = 120f;

    [Header("Configuraci�n de Salto")]
    [Tooltip("Fuerza de salto")]
    public float jumpForce = 5f;

    [Tooltip("Cooldown entre saltos")]
    public float jumpCooldown = 1f;

    [Tooltip("LayerMask para detectar el suelo")]
    public LayerMask groundLayer;

    [Header("Configuraci�n de Sensores")]
    [Tooltip("Distancia m�xima de detecci�n de los sensores")]
    public float sensorLength = 10f;

    [Tooltip("Desplazamiento hacia adelante de los sensores desde el centro del NPC")]
    public float sensorForwardOffset = 1.0f;

    [Tooltip("Altura de los sensores desde la base del NPC")]
    public float sensorHeight = 1.0f;

    [Tooltip("Altura del sensor inferior para detectar obst�culos saltables")]
    public float lowerSensorHeight = 0.2f;

    [Header("Estado")]
    [Tooltip("Tiempo m�ximo que puede permanecer inactivo antes de 'morir'")]
    public float maxIdleTime = 5f;

    [Tooltip("Indica si el NPC ha 'muerto' (colisi�n o inactividad)")]
    public bool isDead = false;

    [Header("Anti-Loop System")]
    [Tooltip("Radio para detectar comportamiento circular")]
    public float loopDetectionRadius = 5f;

    [Tooltip("Tiempo m�nimo entre checkpoints para evitar loops")]
    public float checkpointInterval = 3f;

    [Tooltip("Distancia m�nima para considerar un nuevo checkpoint")]
    public float minCheckpointDistance = 5f;

    [Tooltip("Penalizaci�n por comportamiento circular")]
    public float loopPenalty = 10f;

    [Header("Exploration Rewards")]
    [Tooltip("Recompensa por explorar nuevas �reas")]
    public float explorationBonus = 2f;

    [Tooltip("Tama�o de la cuadr�cula para dividir el mapa")]
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

    // Variables para anti-loop y exploraci�n
    private List<Vector3> positionHistory = new List<Vector3>();
    private float lastCheckpointTime = 0f;
    private Vector3 lastCheckpointPosition;
    private HashSet<Vector2Int> visitedCells = new HashSet<Vector2Int>();
    private int uniqueAreasVisited = 0;
    private float distanceFromStart = 0f;
    private int consecutiveCircles = 0;
    private float lastAngle = 0f;
    private float totalRotation = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

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

    void Start()
    {
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

        // Establecer color seg�n el tipo
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

        UpdateSensors();
        outputs = brain.FeedForward(inputs);
        timeAlive += Time.deltaTime;
    }

    void FixedUpdate()
    {
        if (isDead) return;

        // Verificar si est� en el suelo
        CheckGrounded();

        // Aplicamos las acciones determinadas por la red neuronal
        ApplyOutputs();

        // Nuevas verificaciones anti-loop
        CheckForLoopingBehavior();
        UpdateExplorationTracking();

        // Actualizamos la puntuaci�n de fitness
        UpdateFitness();

        // Verificamos si el NPC est� inactivo por demasiado tiempo
        CheckIdleStatus();
    }

    void UpdateSensors()
    {
        if (inputs.Length < 8)
        {
            inputs = new float[8];
            Debug.LogWarning("Array inputs recreado con tama�o 8");
        }

        // Posici�n desde donde parten los rayos de los sensores normales
        Vector3 sensorStartPos = transform.position + transform.forward * sensorForwardOffset + Vector3.up * sensorHeight;

        // Posici�n para el sensor bajo (detecta obst�culos saltables)
        Vector3 lowerSensorStartPos = transform.position + transform.forward * sensorForwardOffset + Vector3.up * lowerSensorHeight;

        // Actualizamos los 5 sensores originales con detecci�n de aliados/enemigos
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
                        // Es un enemigo, valor positivo m�s alto
                        inputs[i] = distanceValue * 1.5f;
                        Debug.DrawRay(sensorStartPos, sensorDirection * hit.distance, Color.red);
                    }
                }
                else
                {
                    // Es un obst�culo normal
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

        // Sensor 6: Frontal bajo para detectar obst�culos saltables
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

        // Sensor 7: Frontal alto para detectar obst�culos que no se pueden saltar
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

    void ApplyOutputs()
    {
        if (outputs.Length < 4)
        {
            // Si el array no es del tama�o correcto, redimensionarlo
            Array.Resize(ref outputs, 4);
            Debug.LogWarning("Array outputs redimensionado a tama�o 4");
        }
        // A�ade ruido aleatorio al principio del entrenamiento
        float explorationFactor = Mathf.Max(0, 50 - geneticAlgorithm.generation) / 50f;
        float randomNoise = Random.Range(0f, 0.5f) * explorationFactor;

        float forwardSpeed = Mathf.Clamp(outputs[0] + 0.3f + randomNoise, 0, 1) * moveSpeed;
        float turnAmount = (outputs[1] - outputs[2]) * rotationSpeed;

        // Aseguramos una velocidad m�nima
        if (forwardSpeed > 0)
        {
            forwardSpeed = Mathf.Max(forwardSpeed, minSpeed);
        }

        // Aplicamos el movimiento al Rigidbody
        rb.velocity = new Vector3(0, rb.velocity.y, 0) + transform.forward * forwardSpeed;

        // Aplicamos la rotaci�n al transform
        transform.Rotate(Vector3.up * turnAmount * Time.fixedDeltaTime);

        // Manejo del salto
        if (outputs.Length >= 4 && outputs[3] > 0.5f && CanJump())
        {
            Jump();
        }

        // Actualizamos las animaciones si hay un componente Animator
        if (animator != null)
        {
            animator.SetFloat("Speed", forwardSpeed / moveSpeed);
            animator.SetFloat("Turn", turnAmount / rotationSpeed);
            animator.SetBool("IsGrounded", isGrounded);
            animator.SetTrigger("Jump"); // Si existe un trigger de salto en el animator
        }

        // Calculamos la distancia recorrida desde el �ltimo frame
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
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        lastJumpTime = Time.time;
        isGrounded = false;
        successfulJumps++;
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
        // Registrar la posici�n cada cierto intervalo
        if (Time.time - lastCheckpointTime > checkpointInterval)
        {
            Vector3 currentPos = transform.position;
            float distanceFromCheckpoint = Vector3.Distance(currentPos, lastCheckpointPosition);

            // Si no se ha movido lo suficiente desde el �ltimo checkpoint
            if (distanceFromCheckpoint < minCheckpointDistance)
            {
                consecutiveCircles++;
                fitness -= loopPenalty * consecutiveCircles; // Penalizaci�n progresiva
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

        // Detectar rotaci�n excesiva (indicador de c�rculos)
        float currentAngle = transform.eulerAngles.y;
        float angleDelta = Mathf.DeltaAngle(lastAngle, currentAngle);
        totalRotation += Mathf.Abs(angleDelta);
        lastAngle = currentAngle;

        // Penalizar si rota demasiado en relaci�n a la distancia recorrida
        if (totalDistance > 0 && totalRotation / totalDistance > 10f)
        {
            fitness -= loopPenalty * 0.1f;
        }
    }

    void UpdateExplorationTracking()
    {
        // Convertir la posici�n actual a coordenadas de la cuadr�cula
        Vector2Int gridCell = new Vector2Int(
            Mathf.FloorToInt(transform.position.x / gridSize),
            Mathf.FloorToInt(transform.position.z / gridSize)
        );

        // Si es una nueva celda, premiar la exploraci�n
        if (!visitedCells.Contains(gridCell))
        {
            visitedCells.Add(gridCell);
            uniqueAreasVisited++;
            fitness += explorationBonus;
        }

        // Actualizar distancia desde el punto de inicio
        distanceFromStart = Vector3.Distance(transform.position, startPosition);
    }

    void UpdateFitness()
    {
        // Nueva f�rmula de fitness
        float baseReward = 0f;

        // 1. Recompensa por distancia real recorrida
        baseReward += totalDistance * 0.5f;

        // 2. Recompensa por exploraci�n (�reas �nicas visitadas)
        baseReward += uniqueAreasVisited * explorationBonus;

        // 3. Recompensa por distancia desde el punto de inicio
        baseReward += distanceFromStart * 0.3f;

        // 4. Recompensa por saltos exitosos
        baseReward += successfulJumps * 5f;

        // 5. Penalizaci�n por tiempo (para evitar que el tiempo sea lo �nico que importa)
        float timePenalty = Mathf.Min(timeAlive * 0.1f, 10f); // Limitar la penalizaci�n

        // 6. Penalizaci�n por comportamiento repetitivo
        float repetitivePenalty = consecutiveCircles * loopPenalty;

        // 7. Recompensa por checkpoints
        if (CheckpointSystem.Instance != null)
        {
            float checkpointBonus = CheckpointSystem.Instance.CheckCheckpoints(this);
            baseReward += checkpointBonus;
        }

        // Calcular fitness final
        fitness = baseReward - timePenalty - repetitivePenalty;

        // Asegurar que el fitness no sea negativo
        fitness = Mathf.Max(0, fitness);
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

    void OnCollisionEnter(Collision collision)
    {
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

            // Resetear variables anti-loop y exploraci�n
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
            Debug.LogError("No se encontr� el Rigidbody");
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

    // M�todo para visualizar el comportamiento (�til para debugging)
    void OnDrawGizmos()
    {
        if (Application.isPlaying && positionHistory.Count > 1)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < positionHistory.Count - 1; i++)
            {
                Gizmos.DrawLine(positionHistory[i], positionHistory[i + 1]);
            }

            // Dibujar �rea de detecci�n de loops
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(lastCheckpointPosition, loopDetectionRadius);
        }
    }
}