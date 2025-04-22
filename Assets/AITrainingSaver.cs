using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;


[System.Serializable]
public class SerializedNetwork
{
    // Estructura de capas de la red (número de neuronas por capa)
    public int[] layers;
    
    // Versión serializable de los pesos (lista plana)
    public List<float> flattenedWeights;
    
    // Valor de fitness de esta red neuronal
    public float fitness;

    public NPCController.NPCType npcType;

}



[System.Serializable]
public class TrainingData
{
    // Número de generación en la que se guardó
    public int generation;

    // Estadísticas de fitness
    public float bestFitness;
    public float averageFitness;
    public float worstFitness;

    // Lista de las mejores redes neuronales
    public List<SerializedNetwork> networks;

    // Fecha y hora en que se guardó
    public string timestamp;
}



public class AITrainingSaver : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Referencia al algoritmo genético")]
    public NPCGeneticAlgorithm geneticAlgorithm;

    [Header("Configuración de Guardado")]
    [Tooltip("Si es true, guarda en una carpeta del proyecto en lugar de persistentDataPath")]
    public bool saveInProjectFolder = true;

    [Tooltip("Carpeta donde se guardarán los archivos (si saveInProjectFolder es false)")]
    public string saveFolder = "TrainingData";

    [Tooltip("Nombre de la carpeta dentro de Assets para guardar archivos (si saveInProjectFolder es true)")]
    public string projectSaveFolder = "SavedTrainings";

    [Tooltip("Activar guardado automático")]
    public bool autoSave = true;

    [Tooltip("Guardar cada X generaciones")]
    public int autoSaveInterval = 5;

    // Ruta del último archivo guardado
    public string lastSavePath { get; private set; }

    
    void Start()
    {
        // Buscamos el algoritmo genético si no está asignado
        if (geneticAlgorithm == null)
        {
            geneticAlgorithm = FindObjectOfType<NPCGeneticAlgorithm>();
            if (geneticAlgorithm == null)
            {
                Debug.LogError("No se encontró el componente NPCGeneticAlgorithm");
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
        // Auto-guardar basado en el número de generación
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

        // Recorremos la estructura y añadimos cada peso a la lista
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
        // Verificamos que haya población para guardar
        if (geneticAlgorithm.population == null || geneticAlgorithm.population.Count == 0)
        {
            Debug.LogWarning("No hay población para guardar");
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
        // (máximo 10 o la cantidad total de la población si es menor)
        int networksToSave = Mathf.Min(10, geneticAlgorithm.population.Count);

        // Ordenamos los NPCs por fitness y tomamos los mejores
        var bestNPCs = geneticAlgorithm.population
            .OrderByDescending(npc => npc.fitness)
            .Take(networksToSave);

        // Serializamos cada red neuronal seleccionada
        foreach (var npc in bestNPCs)
        {
            // Verificamos que el cerebro exista
            if (npc.brain == null)
            {
                Debug.LogError("Un NPC no tiene cerebro (brain es null)");
                continue;
            }

            // Obtenemos los pesos y los convertimos a formato plano para mejor serialización
            float[][][] weights = npc.brain.GetWeights();
            List<float> flattenedWeights = FlattenWeights(weights);

            SerializedNetwork serializedNetwork = new SerializedNetwork
            {
                layers = npc.brain.GetLayers(),
                flattenedWeights = flattenedWeights,
                fitness = npc.fitness,
                npcType = npc.npcType // Añadir el tipo de NPC
            };

            data.networks.Add(serializedNetwork);

            Debug.Log($"Red guardada: Capas={string.Join(",", serializedNetwork.layers)}, " +
                $"Pesos planos={flattenedWeights.Count}, Fitness={npc.fitness}, " +
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
            // Usa la ruta persistente normal
            fullPath = Path.Combine(Application.persistentDataPath, saveFolder);
        }

        // Asegúrate de que la carpeta exista
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
        }

        // Creamos la ruta completa del archivo
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

  
    /// Método público para guardar con un nombre personalizado  desde UI  
    public void SaveTrainingWithCustomName(string customName)
    {
        SaveTraining(customName);
    }
}