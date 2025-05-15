using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using TMPro;

//
// Gestiona la carga de entrenamientos previamente guardados.
// Permite recuperar redes neuronales y continuar el entrenamiento desde un punto anterior.
// Actualizado para soportar NPCs con saltos y sistema de aliados/enemigos.

public class AITrainingLoader : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Referencia al algoritmo gen�tico")]
    public NPCGeneticAlgorithm geneticAlgorithm;

    [Header("Configuraci�n")]
    [Tooltip("Si es true, carga desde una carpeta del proyecto en lugar de persistentDataPath")]
    public bool loadFromProjectFolder = true;

    [Tooltip("Carpeta donde se buscar�n los archivos guardados (si loadFromProjectFolder es false)")]
    public string saveFolder = "TrainingData";

    [Tooltip("Nombre de la carpeta dentro de Assets para cargar archivos (si loadFromProjectFolder es true)")]
    public string projectSaveFolder = "SavedTrainings";

    [Header("UI (Opcional)")]
    [Tooltip("Dropdown para seleccionar archivos guardados")]
    public TMP_Dropdown saveFilesDropdown;

    [Tooltip("Bot�n para cargar el archivo seleccionado")]
    public Button loadButton;

    // Bandera para indicar que estamos en proceso de carga
    // Evita que el sistema de guardado autom�tico interfiera
    [HideInInspector]
    public bool isLoading = false;

    // Lista con los nombres de archivos guardados
    private List<string> saveFiles = new List<string>();

   
    void Start()
    {
        // Buscamos el algoritmo gen�tico si no est� asignado
        if (geneticAlgorithm == null)
        {
            geneticAlgorithm = FindObjectOfType<NPCGeneticAlgorithm>();
            if (geneticAlgorithm == null)
            {
                Debug.LogError("No se encontr� el componente NPCGeneticAlgorithm");
                enabled = false;
                return;
            }
        }

        // Inicializamos la UI si existe
        if (saveFilesDropdown != null)
        {
            // Llenamos el dropdown con los archivos existentes
            RefreshSaveFilesList();

            // Configuramos el listener para eventos de cambio
            saveFilesDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        }

        // Configuramos el bot�n de carga
        if (loadButton != null)
        {
            loadButton.onClick.AddListener(LoadSelectedTraining);
        }

        // Mostrar la ruta en la consola para saber d�nde est�n los archivos
        string fullPath;
        if (loadFromProjectFolder)
        {
            fullPath = Path.Combine(Application.dataPath, projectSaveFolder);
            Debug.Log("Cargando desde carpeta del proyecto: " + fullPath);
        }
        else
        {
            fullPath = Path.Combine(Application.persistentDataPath, saveFolder);
            Debug.Log("Cargando desde carpeta persistente: " + fullPath);
        }
    }

    //
    /// Reconstruye la estructura  de pesos a partir de la lista plana
    
    private float[][][] RebuildWeights(List<float> flatWeights, int[] layers)
    {
        // Verificamos que los datos sean v�lidos
        if (flatWeights == null || flatWeights.Count == 0)
        {
            Debug.LogError("Error: La lista de pesos planos es nula o vac�a");
            return null;
        }

        if (layers == null || layers.Length < 2)
        {
            Debug.LogError("Error: La estructura de capas es inv�lida");
            return null;
        }

        float[][][] weights = new float[layers.Length - 1][][];

        int weightIndex = 0;

        try
        {
            // Recreamos la estructura original
            for (int i = 0; i < layers.Length - 1; i++)
            {
                weights[i] = new float[layers[i]][];

                for (int j = 0; j < layers[i]; j++)
                {
                    weights[i][j] = new float[layers[i + 1]];

                    for (int k = 0; k < layers[i + 1]; k++)
                    {
                        // Verificamos que no nos quedemos sin �ndices
                        if (weightIndex < flatWeights.Count)
                        {
                            weights[i][j][k] = flatWeights[weightIndex];
                            weightIndex++;
                        }
                        else
                        {
                            // Si faltan pesos, usamos valores aleatorios
                            weights[i][j][k] = UnityEngine.Random.Range(-1f, 1f);
                            Debug.LogWarning("Faltaron pesos al reconstruir. Usando valores aleatorios.");
                        }
                    }
                }
            }

            Debug.Log($"Pesos reconstruidos correctamente. Utilizados: {weightIndex}/{flatWeights.Count}");
            return weights;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al reconstruir pesos: {e.Message}");
            return null;
        }
    }

 
    public void RefreshSaveFilesList()
    {
        if (saveFilesDropdown == null) return;

        // Limpiamos listas existentes
        saveFiles.Clear();
        saveFilesDropdown.ClearOptions();

        // Determina la ruta seg�n la configuraci�n
        string fullPath;
        if (loadFromProjectFolder)
        {
            fullPath = Path.Combine(Application.dataPath, projectSaveFolder);
        }
        else
        {
            fullPath = Path.Combine(Application.persistentDataPath, saveFolder);
        }

        // Verificamos que exista la carpeta de guardado
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
            return;
        }

        // Obtenemos todos los archivos JSON de la carpeta
        string[] files = Directory.GetFiles(fullPath, "*.json");
        foreach (string file in files)
        {
            saveFiles.Add(Path.GetFileName(file));
        }

        // A�adimos las opciones al dropdown
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        foreach (string file in saveFiles)
        {
            options.Add(new TMP_Dropdown.OptionData(file));
        }
        saveFilesDropdown.AddOptions(options);

        Debug.Log($"Se encontraron {saveFiles.Count} archivos de guardado");
    }

    
    private void OnDropdownValueChanged(int index)
    {
        // Aqu� se pueden a�adir acciones al cambiar la selecci�n
        if (index >= 0 && index < saveFiles.Count)
        {
            Debug.Log($"Archivo seleccionado: {saveFiles[index]}");
        }
    }

  
    public void LoadSelectedTraining()
    {
        // Verificamos que haya un archivo seleccionado v�lido
        if (saveFilesDropdown == null ||
            saveFilesDropdown.value < 0 ||
            saveFilesDropdown.value >= saveFiles.Count)
        {
            Debug.LogWarning("No hay archivo de guardado seleccionado");
            return;
        }

        // Obtenemos el nombre del archivo seleccionado
        string fileName = saveFiles[saveFilesDropdown.value];

        // Cargamos el archivo
        LoadTraining(fileName);
    }


    public void LoadTraining(string fileName)
    {
        // Determina la ruta seg�n la configuraci�n
        string fullPath;
        if (loadFromProjectFolder)
        {
            fullPath = Path.Combine(Application.dataPath, projectSaveFolder, fileName);
        }
        else
        {
            fullPath = Path.Combine(Application.persistentDataPath, saveFolder, fileName);
        }

        // Verificamos que el archivo exista
        if (!File.Exists(fullPath))
        {
            Debug.LogError($"Archivo no encontrado: {fullPath}");
            return;
        }

        // Establece isPaused en true para evitar que Update() procese la poblaci�n mientras cargas
        if (geneticAlgorithm != null)
        {
            geneticAlgorithm.isPaused = true;
        }

        try
        {
            // Activamos la bandera de carga
            isLoading = true;

            // Leemos y deserializamos el archivo JSON
            string json = File.ReadAllText(fullPath);
            Debug.Log($"Contenido JSON le�do: {json.Substring(0, Math.Min(json.Length, 100))}..."); // Mostrar parte del JSON

            TrainingData data = JsonUtility.FromJson<TrainingData>(json);

            // Verificar que la deserializaci�n fue exitosa
            if (data == null)
            {
                Debug.LogError("Error al deserializar los datos: El resultado es nulo");
                return;
            }

            // Verificar que la lista de redes exista
            if (data.networks == null)
            {
                Debug.LogError("La lista de redes es nula en los datos cargados");
                return;
            }

            Debug.Log($"Se encontraron {data.networks.Count} redes en el archivo");

            Debug.Log($"Cargando entrenamiento - Generaci�n: {data.generation}, " +
                     $"Mejor Fitness: {data.bestFitness}, " +
                     $"Guardado: {data.timestamp}");

            // Destruimos la poblaci�n actual
            if (geneticAlgorithm.population != null)
            {
                foreach (var npc in geneticAlgorithm.population)
                {
                    if (npc != null)
                    {
                        Destroy(npc.gameObject);
                    }
                }
                geneticAlgorithm.population.Clear();
            }

            // Actualizamos el n�mero de generaci�n
            geneticAlgorithm.generation = data.generation;

            // Importante: Inicializamos la nueva poblaci�n antes de a�adir NPCs
            geneticAlgorithm.population = new List<NPCController>();

            // Verificar que hay redes guardadas para cargar
            if (data.networks.Count == 0)
            {
                Debug.LogError("El archivo no contiene redes neuronales para cargar.");
                return;
            }

            // Creamos los NPCs con las redes neuronales cargadas
            for (int i = 0; i < geneticAlgorithm.populationSize; i++)
            {
                // Instanciamos un nuevo NPC
                GameObject npcGO = Instantiate(geneticAlgorithm.npcPrefab,
                                             geneticAlgorithm.startPosition.position,
                                             geneticAlgorithm.startPosition.rotation);

                NPCController npc = npcGO.GetComponent<NPCController>();

                if (npc != null)
                {
                    // Determinamos qu� red neuronal usar para este NPC
                    if (i < data.networks.Count)
                    {
                        // Si tenemos suficientes redes guardadas, usamos una directamente
                        SerializedNetwork savedNetwork = data.networks[i];

                        // Verificar que la informaci�n de la red sea v�lida
                        if (savedNetwork == null)
                        {
                            Debug.LogError($"Red guardada #{i} es nula");
                            continue;
                        }

                        if (savedNetwork.layers == null)
                        {
                            Debug.LogError($"Capas de la red #{i} son nulas");
                            continue;
                        }

                        // Establecer el tipo de NPC
                        npc.npcType = savedNetwork.npcType;
                        npc.SetNPCColor();

                        // Inicializar la red neuronal con las dimensiones correctas
                        try
                        {
                            npc.brain = new NeuralNetwork(savedNetwork.layers);
                            Debug.Log($"Red #{i} creada con �xito. Estructura: {string.Join(",", savedNetwork.layers)}");

                            // Verificamos si tenemos los pesos en formato plano
                            if (savedNetwork.flattenedWeights != null && savedNetwork.flattenedWeights.Count > 0)
                            {
                                // Reconstruimos la estructura 3D
                                float[][][] rebuiltWeights = RebuildWeights(savedNetwork.flattenedWeights, savedNetwork.layers);
                                if (rebuiltWeights != null)
                                {
                                    npc.brain.SetWeights(rebuiltWeights);
                                    Debug.Log($"Pesos reconstruidos correctamente para la red #{i}");

                                    // Aplicar estado de bloqueo si existe
                                    if (savedNetwork.outputLockStatus != null)
                                    {
                                        npc.brain.SetOutputLockStatus(savedNetwork.outputLockStatus);
                                        Debug.Log($"Estado de bloqueo aplicado correctamente para la red #{i}");
                                    }
                                }
                                else
                                {
                                    Debug.LogError($"Error al reconstruir pesos para la red #{i}");
                                }
                            }
                            else
                            {
                                Debug.LogWarning($"No se encontraron pesos planos para la red #{i}. Usando pesos aleatorios.");
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"Error al crear/configurar red #{i}: {e.Message}");
                            Debug.LogException(e);

                            // Crear una red por defecto en caso de error
                            npc.brain = new NeuralNetwork(8, 8, 6, 4); // Estructura actualizada
                        }
                    }
                    else
                    {
                        // Si necesitamos m�s NPCs que los guardados, hacemos copias
                        // y las mutamos ligeramente para a�adir diversidad
                        try
                        {
                            int sourceIndex = i % data.networks.Count;
                            SerializedNetwork sourceNetwork = data.networks[sourceIndex];

                            if (sourceNetwork == null || sourceNetwork.layers == null)
                            {
                                Debug.LogError($"Red fuente #{sourceIndex} es inv�lida");
                                npc.brain = new NeuralNetwork(8, 8, 6, 4); // Estructura actualizada
                            }
                            else
                            {
                                npc.npcType = sourceNetwork.npcType;
                                npc.SetNPCColor();
                                npc.brain = new NeuralNetwork(sourceNetwork.layers);

                                if (sourceNetwork.flattenedWeights != null && sourceNetwork.flattenedWeights.Count > 0)
                                {
                                    float[][][] rebuiltWeights = RebuildWeights(sourceNetwork.flattenedWeights, sourceNetwork.layers);
                                    if (rebuiltWeights != null)
                                    {
                                        npc.brain.SetWeights(rebuiltWeights);
                                        // Aplicamos una mutaci�n m�s alta para diversidad
                                        npc.brain.Mutate(geneticAlgorithm.mutationRate * 2);

                                        // Aplicar estado de bloqueo si existe
                                        if (sourceNetwork.outputLockStatus != null)
                                        {
                                            npc.brain.SetOutputLockStatus(sourceNetwork.outputLockStatus);
                                        }
                                    }
                                    else
                                    {
                                        Debug.LogWarning($"Pesos nulos en red fuente #{sourceIndex}, no se aplic� mutaci�n");
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"Error al copiar/mutar red para NPC #{i}: {e.Message}");
                            // Fallback: crear NPC con configuraci�n por defecto
                            npc.brain = new NeuralNetwork(8, 8, 6, 4); // Estructura actualizada
                                                                       // Decidir tipo basado en el porcentaje configurado
                            npc.npcType = (i < geneticAlgorithm.populationSize * geneticAlgorithm.enemyPercentage) ?
                                NPCController.NPCType.Enemy : NPCController.NPCType.Ally;
                            npc.SetNPCColor();
                        }
                    }

                    // A�adimos el NPC a la poblaci�n
                    geneticAlgorithm.population.Add(npc);

                    // Log para verificar la creaci�n
                    Debug.Log($"NPC {i} creado con �xito");
                }
                else
                {
                    Debug.LogError("NPCController no est� en los componentes del prefab");
                }
            }

            // Verifica el tama�o de la poblaci�n despu�s de la creaci�n
            Debug.Log($"Poblaci�n despu�s de cargar: {geneticAlgorithm.population.Count}");

            // Solo reanuda si la poblaci�n se cre� correctamente
            if (geneticAlgorithm.population.Count > 0)
            {
                Debug.Log("Entrenamiento cargado exitosamente. Reanudando simulaci�n.");
                // Reanuda el algoritmo
                geneticAlgorithm.isPaused = false;
            }
            else
            {
                Debug.LogError("La poblaci�n est� vac�a despu�s de la carga. No se reanudar� el algoritmo.");
            }

            // Sincronizar la UI con el estado de bloqueo cargado
            if (FindObjectOfType<AITrainingUI>() != null)
            {
                FindObjectOfType<AITrainingUI>().SyncUIWithLockStatus();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al cargar entrenamiento: {e.Message}");
            Debug.LogException(e);
        }
        finally
        {
            // Desactivamos la bandera de carga
            isLoading = false;
        }
        if (FindObjectOfType<AITrainingUI>() != null)
        {
            FindObjectOfType<AITrainingUI>().SyncUIWithLockStatus();
        }
    }
}