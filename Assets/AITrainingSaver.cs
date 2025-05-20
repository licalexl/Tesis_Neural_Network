using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;


[System.Serializable]
public class SerializedNetwork
{
    // Estructura de capas de la red (n�mero de neuronas por capa)
    public int[] layers;

    // Versi�n serializable de los pesos (lista plana)
    public List<float> flattenedWeights;

    // Valor de fitness de esta red neuronal
    public float fitness;

    public NPCController.NPCType npcType;

    // Estado de bloqueo de las salidas 
    public bool[] outputLockStatus;



    // Campos  para identificar el tipo de red
    public NetworkType networkType = NetworkType.Navigation;

    // Campos para la red de combate
    public int[] combatLayers;
    public List<float> combatFlattenedWeights;
    public bool[] combatOutputLockStatus;

    // estadisticas de combate
    public int attacksPerformed;
    public int successfulBlocks;
    public int successfulDodges;
    public int successfulCounters;
}


[System.Serializable]
public class TrainingData
{
    // N�mero de generaci�n en la que se guard�
    public int generation;

    // Estad�sticas de fitness
    public float bestFitness;
    public float averageFitness;
    public float worstFitness;

    // Lista de las mejores redes neuronales
    public List<SerializedNetwork> networks;

    // Fecha y hora en que se guard�
    public string timestamp;
}



public class AITrainingSaver : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Referencia al algoritmo gen�tico")]
    public NPCGeneticAlgorithm geneticAlgorithm;

    [Header("Configuraci�n de Guardado")]
    [Tooltip("Si es true, guarda en una carpeta del proyecto en lugar de persistentDataPath")]
    public bool saveInProjectFolder = true;

    [Tooltip("Carpeta donde se guardar�n los archivos (si saveInProjectFolder es false)")]
    public string saveFolder = "TrainingData";

    [Tooltip("Nombre de la carpeta dentro de Assets para guardar archivos (si saveInProjectFolder es true)")]
    public string projectSaveFolder = "SavedTrainings";

    [Tooltip("Activar guardado autom�tico")]
    public bool autoSave = true;

    [Tooltip("Guardar cada X generaciones")]
    public int autoSaveInterval = 5;

    // Ruta del �ltimo archivo guardado
    public string lastSavePath { get; private set; }

    
    void Start()
    {
        // Buscamos el algoritmo gen�tico si no est� asignado
        if (geneticAlgorithm == null)
        {
            geneticAlgorithm = FindObjectOfType<NPCGeneticAlgorithm>();
            if (geneticAlgorithm == null)
            {
                Debug.LogError("No se encontr� el componente NPCGeneticAlgorithm");
                enabled = false; // Desactivamos el componente
                return;
            }
        }

        // Creamos el directorio de guardado si no existe
        string fullPath;
        if (saveInProjectFolder)
        {
            fullPath = Path.Combine(Application.dataPath, projectSaveFolder);
            Debug.Log("Guardando en carpeta del proyecto: " + fullPath);
        }
        else
        {
            fullPath = Path.Combine(Application.persistentDataPath, saveFolder);
            Debug.Log("Guardando en carpeta persistente: " + fullPath);
        }

        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
        }
    }


    void Update()
    {
        // Auto-guardar basado en el n�mero de generaci�n
        if (autoSave &&
            geneticAlgorithm.generation % autoSaveInterval == 0 &&
            geneticAlgorithm.generation != 0 &&
            GetComponent<AITrainingLoader>()?.isLoading == false)
        {
            SaveTraining($"Generation_{geneticAlgorithm.generation}");
        }
    }


    private List<float> FlattenWeights(float[][][] weights)
    {
        List<float> flatWeights = new List<float>();

        if (weights == null)
        {
            Debug.LogError("Error: Intento de aplanar pesos nulos");
            return flatWeights;
        }

        // Recorremos la estructura y a�adimos cada peso a la lista
        for (int i = 0; i < weights.Length; i++)
        {
            if (weights[i] == null) continue;

            for (int j = 0; j < weights[i].Length; j++)
            {
                if (weights[i][j] == null) continue;

                for (int k = 0; k < weights[i][j].Length; k++)
                {
                    flatWeights.Add(weights[i][j][k]);
                }
            }
        }

        return flatWeights;
    }

   
    // Guarda el estado actual del entrenamiento en un archivo JSON.        
    public void SaveTraining(string saveName = "")
    {

        // Verificamos que haya poblaci�n para guardar
        if (geneticAlgorithm.population == null || geneticAlgorithm.population.Count == 0)
        {
            Debug.LogWarning("No hay poblaci�n para guardar");
            return;
        }

        // Creamos la estructura de datos para guardar
        TrainingData data = new TrainingData
        {
            generation = geneticAlgorithm.generation,
            bestFitness = geneticAlgorithm.population.Max(npc => npc.fitness),
            averageFitness = geneticAlgorithm.population.Average(npc => npc.fitness),
            worstFitness = geneticAlgorithm.population.Min(npc => npc.fitness),
            networks = new List<SerializedNetwork>(),
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        // Solo guardamos las mejores redes para ahorrar espacio
        // (m�ximo 10 o la cantidad total de la poblaci�n si es menor)
        int networksToSave = Mathf.Min(10, geneticAlgorithm.population.Count);

        // Ordenamos los NPCs por fitness y tomamos los mejores
        var bestNPCs = geneticAlgorithm.population
            .OrderByDescending(npc => npc.fitness)
            .Take(networksToSave);

        // Serializamos cada red neuronal seleccionada
        foreach (var npc in bestNPCs)
        {
            // Verificamos que el cerebro exista
            if (npc.navigationBrain == null || npc.combatBrain == null)
            {
                Debug.LogError("Un NPC no tiene cerebro completo (navigationBrain o combatBrain es null)");
                continue;
            }
            // Verificamos que el cerebro exista
            if (npc.brain == null)
            {
                Debug.LogError("Un NPC no tiene cerebro ");
                continue;
            }


            // Se buscan los pesos y los convertimos a formato plano osea texto para mejor lectura en el archivo de guardado 
         
            // pesos de la red de navegaci�n
            float[][][] navWeights = npc.navigationBrain.GetWeights();
            List<float> navFlattenedWeights = FlattenWeights(navWeights);

            // pesos de la red de combate
            float[][][] combatWeights = npc.combatBrain.GetWeights();
            List<float> combatFlattenedWeights = FlattenWeights(combatWeights);

            SerializedNetwork serializedNetwork = new SerializedNetwork
            {
                // Datos de la red de navegaci�n
                layers = npc.navigationBrain.GetLayers(),
                flattenedWeights = navFlattenedWeights,
                outputLockStatus = npc.navigationBrain.GetOutputLockStatus(),
                networkType = NetworkType.Navigation,

                // Datos de la red de combate
                combatLayers = npc.combatBrain.GetLayers(),
                combatFlattenedWeights = combatFlattenedWeights,
                combatOutputLockStatus = npc.combatBrain.GetOutputLockStatus(),

                // Datos comunes
                fitness = npc.fitness,
                npcType = npc.npcType,

                // Estad�sticas de combate (si las has implementado)
                attacksPerformed = 0, // Reemplazar con estad�sticas reales
                successfulBlocks = 0,
                successfulDodges = 0,
                successfulCounters = 0
            };

            data.networks.Add(serializedNetwork);

            Debug.Log($"Red guardada: Capas={string.Join(",", serializedNetwork.layers)}, " +
                $"Pesos planos={navFlattenedWeights.Count}, Fitness={npc.fitness}, " +
                $"Tipo={npc.npcType}");
        }

        if (string.IsNullOrEmpty(saveName))
        {
            saveName = $"Training_{DateTime.Now:yyyyMMdd_HHmmss}";
        }

        string fullPath;
        if (saveInProjectFolder)
        {
            // Guarda en una carpeta en el proyecto
            fullPath = Path.Combine(Application.dataPath, projectSaveFolder);
        }
        else
        {
            // ruta por defecto
            fullPath = Path.Combine(Application.persistentDataPath, saveFolder);
        }

        // asegurar que la carpeta exista
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
        }

        // ruta  del archivo
        string fileName = $"{saveName}.json";
        string filePath = Path.Combine(fullPath, fileName);

        // Convertimos los datos a formato JSON con formato legible (pretty print)
        string json = JsonUtility.ToJson(data, true);

        // Guardamos el archivo
        File.WriteAllText(filePath, json);

        // Guardamos la ruta para referencia
        lastSavePath = filePath;
        Debug.Log($"Entrenamiento guardado en: {filePath}");

        // Si guardamos en el proyecto, notificar a Unity que actualice el AssetDatabase
#if UNITY_EDITOR
        if (saveInProjectFolder)
        {
            UnityEditor.AssetDatabase.Refresh();
        }
#endif
    }

  
    /// M�todo p�blico para guardar con un nombre personalizado  desde UI  
    public void SaveTrainingWithCustomName(string customName)
    {
        SaveTraining(customName);
    }
}