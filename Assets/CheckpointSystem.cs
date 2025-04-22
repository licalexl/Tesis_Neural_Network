using UnityEngine;
using System.Collections.Generic;


public class CheckpointSystem : MonoBehaviour
{
    public static CheckpointSystem Instance { get; private set; }

    [Header("Configuración de Checkpoints")]
    public List<Transform> checkpoints = new List<Transform>();
    public float checkpointRadius = 3f;
    public float checkpointReward = 20f;
    public bool showGizmos = true;

    private Dictionary<NPCController, HashSet<int>> npcCheckpoints = new Dictionary<NPCController, HashSet<int>>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RegisterNPC(NPCController npc)
    {
        if (!npcCheckpoints.ContainsKey(npc))
        {
            npcCheckpoints[npc] = new HashSet<int>();
        }
    }

    public void UnregisterNPC(NPCController npc)
    {
        if (npcCheckpoints.ContainsKey(npc))
        {
            npcCheckpoints.Remove(npc);
        }
    }

    public float CheckCheckpoints(NPCController npc)
    {
        if (!npcCheckpoints.ContainsKey(npc))
        {
            RegisterNPC(npc);
        }

        float reward = 0f;
        Vector3 npcPosition = npc.transform.position;

        for (int i = 0; i < checkpoints.Count; i++)
        {
            if (checkpoints[i] == null) continue;

            float distance = Vector3.Distance(npcPosition, checkpoints[i].position);

            if (distance < checkpointRadius && !npcCheckpoints[npc].Contains(i))
            {
                // NPC alcanzó un nuevo checkpoint
                npcCheckpoints[npc].Add(i);
                reward += checkpointReward;

                // Bonus por checkpoints en orden
                if (i > 0 && npcCheckpoints[npc].Contains(i - 1))
                {
                    reward += checkpointReward * 0.5f;
                }

                Debug.Log($"NPC {npc.name} alcanzó checkpoint {i}! Reward: {reward}");
            }
        }

        return reward;
    }

    public void ResetNPCCheckpoints(NPCController npc)
    {
        if (npcCheckpoints.ContainsKey(npc))
        {
            npcCheckpoints[npc].Clear();
        }
    }

    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        for (int i = 0; i < checkpoints.Count; i++)
        {
            if (checkpoints[i] == null) continue;

            // Dibujar checkpoints
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(checkpoints[i].position, checkpointRadius);

            // Dibujar números
#if UNITY_EDITOR
            UnityEditor.Handles.Label(checkpoints[i].position, $"CP {i}");
#endif

            // Dibujar líneas entre checkpoints
            if (i < checkpoints.Count - 1 && checkpoints[i + 1] != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(checkpoints[i].position, checkpoints[i + 1].position);
            }
        }
    }
}