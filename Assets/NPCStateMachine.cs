using UnityEngine;

public class NPCStateMachine : MonoBehaviour
{
    public enum NPCState
    {
        Navigation,  // Usa la red de navegaci�n original
        Combat       // Usa la nueva red de combate
    }

    [Header("Estado")]
    [SerializeField] private NPCState currentState = NPCState.Navigation;

    [Header("Configuraci�n de Transici�n")]
    [Tooltip("Distancia a la que un NPC detecta enemigos y entra en modo combate")]
    public float combatDetectionRadius = 5f;

    [Tooltip("Capa para detectar enemigos potenciales")]
    public LayerMask enemyLayers;

    [Tooltip("Tiempo m�nimo en cada estado antes de poder cambiar")]
    public float minStateTime = 1.0f;

    // Referencia al controlador del NPC
    private NPCController npcController;

    // Tiempo transcurrido en el estado actual
    private float timeInCurrentState = 0f;

    // �ltimo enemigo detectado
    [HideInInspector]
    public Transform currentTarget;

    void Awake()
    {
        npcController = GetComponent<NPCController>();
    }

    void Update()
    {
        timeInCurrentState += Time.deltaTime;

        // Solo evaluar cambio de estado si ha pasado el tiempo m�nimo
        if (timeInCurrentState >= minStateTime)
        {
            EvaluateStateTransition();
        }
    }

    private void EvaluateStateTransition()
    {
        switch (currentState)
        {
            case NPCState.Navigation:
                // Verificar si hay enemigos cercanos para entrar en modo combate
                if (DetectEnemies())
                {
                    ChangeState(NPCState.Combat);
                }
                break;

            case NPCState.Combat:
                // Volver a navegaci�n si no hay enemigos cercanos
                if (!DetectEnemies() || currentTarget == null)
                {
                    ChangeState(NPCState.Navigation);
                }
                break;
        }
    }

    private bool DetectEnemies()
    {
        // Buscar enemigos en el radio de detecci�n
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, combatDetectionRadius, enemyLayers);

        if (hitColliders.Length > 0)
        {
            // Encontrar el enemigo m�s cercano
            float closestDistance = float.MaxValue;
            Transform closestEnemy = null;

            foreach (var hitCollider in hitColliders)
            {
                // Verificar si es un NPC y si es de tipo opuesto
                NPCController otherNPC = hitCollider.GetComponent<NPCController>();
                if (otherNPC != null && otherNPC.npcType != npcController.npcType)
                {
                    float distance = Vector3.Distance(transform.position, hitCollider.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestEnemy = hitCollider.transform;
                    }
                }
            }

            currentTarget = closestEnemy;
            return closestEnemy != null;
        }

        return false;
    }

    public void ChangeState(NPCState newState)
    {
        // Solo cambiar si es diferente al estado actual
        if (newState != currentState)
        {
            currentState = newState;
            timeInCurrentState = 0f;

            // Notificar al controlador del cambio de estado
            npcController.OnStateChanged(currentState);
        }
    }

    public NPCState GetCurrentState()
    {
        return currentState;
    }

    // Para visualizaci�n en el editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, combatDetectionRadius);
    }
}