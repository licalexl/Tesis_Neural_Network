using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.UI;
using TMPro;
using System;

public class TrainingVisualizer : MonoBehaviour
{
    [Header("Referencias UI")]
    [Tooltip("Panel principal que contendr� las gr�ficas")]
    public RectTransform graphContainer;

    [Tooltip("Prefab para los puntos de datos en la gr�fica")]
    public GameObject dataPointPrefab;

    [Tooltip("Prefab para las l�neas que conectan los puntos")]
    public GameObject lineRendererPrefab;

    [Tooltip("Texto para mostrar estad�sticas detalladas")]
    public TextMeshProUGUI statsText;

    [Tooltip("Dropdown para seleccionar qu� m�trica mostrar")]
    public TMP_Dropdown metricDropdown;

    [Tooltip("Dropdown para seleccionar archivos de entrenamiento")]
    public TMP_Dropdown filesDropdown;

    [Header("Configuraci�n de Carga")]
    [Tooltip("Si es true, carga desde una carpeta del proyecto en lugar de persistentDataPath")]
    public bool loadFromProjectFolder = true;

    [Tooltip("Carpeta donde se buscar�n los archivos guardados (si loadFromProjectFolder es false)")]
    public string loadFolder = "TrainingData";

    [Tooltip("Nombre de la carpeta dentro de Assets para cargar archivos (si loadFromProjectFolder es true)")]
    public string projectLoadFolder = "SavedTrainings";

    [Header("Configuraci�n de Gr�ficas")]
    [Tooltip("Alto m�ximo de la gr�fica")]
    public float graphHeight = 400f;

    [Tooltip("Ancho m�ximo de la gr�fica")]
    public float graphWidth = 800f;

    [Tooltip("Color para la l�nea de mejor fitness")]
    public Color bestFitnessColor = Color.green;

    [Tooltip("Color para la l�nea de fitness promedio")]
    public Color avgFitnessColor = Color.yellow;

    [Tooltip("Color para la l�nea de peor fitness")]
    public Color worstFitnessColor = Color.red;

    [Tooltip("Mostrar etiquetas de valor en los puntos de datos")]
    public bool showValueLabels = true;

    // Variables internas
    public List<TrainingData> loadedTrainingDataList = new List<TrainingData>();
    private List<string> trainingFiles = new List<string>();
    private Dictionary<string, List<GameObject>> graphElements = new Dictionary<string, List<GameObject>>();
    private string currentMetric = "bestFitness";
    private int selectedFileIndex = -1;

    // M�tricas adicionales calculadas
    private Dictionary<string, List<float>> derivedMetrics = new Dictionary<string, List<float>>();

    // Enumeraci�n para las m�tricas disponibles
    public enum GraphMetric
    {
        BestFitness,
        AverageFitness,
        WorstFitness,
        FitnessRange,      // Mejor - Peor
        NetworkComplexity,  // Basado en cantidad de pesos no-cero
        DiversityIndex,     // Medida de diversidad entre redes
        LearningRate,       // Tasa de mejora entre generaciones
        SuccessRate,        // % de NPCs con fitness superior a un umbral
        JumpEfficiency      // Calculado a partir de otras m�tricas si disponible
    }

    void Start()
    {
        InitializeUI();
        LoadTrainingFiles();
    }

    private void InitializeUI()
    {
        // Configurar el dropdown de m�tricas
        if (metricDropdown != null)
        {
            metricDropdown.ClearOptions();
            List<string> options = new List<string>
            {
                "Mejor Fitness",
                "Fitness Promedio",
                "Peor Fitness",
                "Rango de Fitness",
                "Complejidad de Red",
                "�ndice de Diversidad",
                "Tasa de Aprendizaje",
                "Tasa de �xito",
                "Todas las M�tricas"
            };
            metricDropdown.AddOptions(options);
            metricDropdown.onValueChanged.AddListener(OnMetricSelected);
        }

        // Configurar el dropdown de archivos
        if (filesDropdown != null)
        {
            filesDropdown.onValueChanged.AddListener(OnFileSelected);
        }
    }

    private void LoadTrainingFiles()
    {
        trainingFiles.Clear();
        filesDropdown.ClearOptions();

        // Determina la ruta seg�n la configuraci�n
        string fullPath;
        if (loadFromProjectFolder)
        {
            fullPath = Path.Combine(Application.dataPath, projectLoadFolder);
        }
        else
        {
            fullPath = Path.Combine(Application.persistentDataPath, loadFolder);
        }

        // Verificamos que exista la carpeta
        if (!Directory.Exists(fullPath))
        {
            Debug.LogWarning($"La carpeta {fullPath} no existe. Cre�ndola.");
            Directory.CreateDirectory(fullPath);
            return;
        }

        // Obtenemos todos los archivos JSON de la carpeta
        string[] files = Directory.GetFiles(fullPath, "*.json");

        // Ordenamos los archivos por fecha de modificaci�n (m�s recientes primero)
        files = files.OrderByDescending(f => new FileInfo(f).LastWriteTime).ToArray();

        foreach (string file in files)
        {
            string fileName = Path.GetFileName(file);
            trainingFiles.Add(file);

            // Extraer informaci�n b�sica para mostrar en el dropdown
            string displayName = fileName;
            try
            {
                string json = File.ReadAllText(file);
                TrainingData data = JsonUtility.FromJson<TrainingData>(json);
                if (data != null)
                {
                    displayName = $"{fileName} - Gen {data.generation} ({data.timestamp})";
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Error al leer archivo {fileName}: {e.Message}");
            }

            filesDropdown.options.Add(new TMP_Dropdown.OptionData(displayName));
        }

        filesDropdown.RefreshShownValue();

        if (trainingFiles.Count > 0)
        {
            filesDropdown.value = 0;  // Seleccionar el primer archivo
            OnFileSelected(0);
        }
    }

    private void OnFileSelected(int index)
    {
        if (index < 0 || index >= trainingFiles.Count) return;

        selectedFileIndex = index;
        string filePath = trainingFiles[index];

        try
        {
            string json = File.ReadAllText(filePath);
            TrainingData data = JsonUtility.FromJson<TrainingData>(json);

            if (data != null)
            {
                // Limpiar visualizaciones anteriores
                ClearGraph();

                // Cargar y analizar el archivo de entrenamiento
                loadedTrainingDataList.Clear();
                loadedTrainingDataList.Add(data);

                // Si el archivo es parte de una serie (ej. Generation_X), intentar cargar archivos relacionados
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                if (fileName.StartsWith("Generation_"))
                {
                    LoadRelatedGenerations(filePath);
                }

                // Calcular m�tricas derivadas
                CalculateDerivedMetrics();

                // Actualizar la visualizaci�n
                UpdateGraph();
                UpdateStatsText();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error cargando archivo {filePath}: {e.Message}");
        }
    }

    private void LoadRelatedGenerations(string primaryFilePath)
    {
        // Extraer n�mero de generaci�n del archivo principal
        string fileName = Path.GetFileNameWithoutExtension(primaryFilePath);
        if (!fileName.StartsWith("Generation_")) return;

        string genNumberStr = fileName.Substring("Generation_".Length);
        if (!int.TryParse(genNumberStr, out int genNumber)) return;

        string directoryPath = Path.GetDirectoryName(primaryFilePath);

        // Buscar generaciones anteriores hasta gen 1
        for (int i = genNumber - 1; i >= 1; i--)
        {
            string previousGenFile = Path.Combine(directoryPath, $"Generation_{i}.json");
            if (File.Exists(previousGenFile))
            {
                try
                {
                    string json = File.ReadAllText(previousGenFile);
                    TrainingData prevData = JsonUtility.FromJson<TrainingData>(json);
                    if (prevData != null)
                    {
                        loadedTrainingDataList.Insert(0, prevData);
                    }
                }
                catch (Exception)
                {
                    // Ignorar archivos con errores
                }
            }
        }

        // Buscar generaciones posteriores
        int nextGen = genNumber + 1;
        bool continueSearching = true;

        while (continueSearching)
        {
            string nextGenFile = Path.Combine(directoryPath, $"Generation_{nextGen}.json");
            if (File.Exists(nextGenFile))
            {
                try
                {
                    string json = File.ReadAllText(nextGenFile);
                    TrainingData nextData = JsonUtility.FromJson<TrainingData>(json);
                    if (nextData != null)
                    {
                        loadedTrainingDataList.Add(nextData);
                    }
                }
                catch (Exception)
                {
                    // Ignorar archivos con errores
                }
                nextGen++;
            }
            else
            {
                continueSearching = false;
            }
        }

        // Ordenar por n�mero de generaci�n
        loadedTrainingDataList = loadedTrainingDataList.OrderBy(data => data.generation).ToList();
    }

    private void CalculateDerivedMetrics()
    {
        derivedMetrics.Clear();

        // Inicializar listas para las m�tricas
        derivedMetrics["fitnessRange"] = new List<float>();
        derivedMetrics["networkComplexity"] = new List<float>();
        derivedMetrics["diversityIndex"] = new List<float>();
        derivedMetrics["learningRate"] = new List<float>();
        derivedMetrics["successRate"] = new List<float>();
        derivedMetrics["jumpEfficiency"] = new List<float>();

        float previousBestFitness = 0;

        foreach (var data in loadedTrainingDataList)
        {
            // Rango de fitness
            derivedMetrics["fitnessRange"].Add(data.bestFitness - data.worstFitness);

            // Complejidad de red (promedio de pesos no-cero por red)
            float avgWeightCount = 0;
            if (data.networks != null && data.networks.Count > 0)
            {
                int totalNonZeroWeights = 0;
                int networkCount = 0;

                foreach (var network in data.networks)
                {
                    if (network.flattenedWeights != null)
                    {
                        networkCount++;
                        totalNonZeroWeights += network.flattenedWeights.Count(w => Mathf.Abs(w) > 0.01f);
                    }
                }

                if (networkCount > 0)
                {
                    avgWeightCount = (float)totalNonZeroWeights / networkCount;
                }
            }
            derivedMetrics["networkComplexity"].Add(avgWeightCount);

            // �ndice de diversidad (desviaci�n est�ndar de fitness)
            if (data.networks != null && data.networks.Count >= 2)
            {
                float sumSquaredDiff = 0;
                foreach (var network in data.networks)
                {
                    sumSquaredDiff += Mathf.Pow(network.fitness - data.averageFitness, 2);
                }
                float diversityIndex = Mathf.Sqrt(sumSquaredDiff / data.networks.Count);
                derivedMetrics["diversityIndex"].Add(diversityIndex);
            }
            else
            {
                derivedMetrics["diversityIndex"].Add(0);
            }

            // Tasa de aprendizaje (mejora respecto a generaci�n anterior)
            if (previousBestFitness > 0)
            {
                float improvement = data.bestFitness - previousBestFitness;
                derivedMetrics["learningRate"].Add(improvement);
            }
            else
            {
                derivedMetrics["learningRate"].Add(0);
            }
            previousBestFitness = data.bestFitness;

            // Tasa de �xito (% de NPCs sobre un umbral de fitness)
            if (data.networks != null && data.networks.Count > 0)
            {
                float threshold = data.averageFitness * 1.2f; // 20% mejor que el promedio
                int successfulNPCs = data.networks.Count(n => n.fitness >= threshold);
                float successRate = (float)successfulNPCs / data.networks.Count;
                derivedMetrics["successRate"].Add(successRate);
            }
            else
            {
                derivedMetrics["successRate"].Add(0);
            }

            // Eficiencia de salto (si est� disponible en los datos)
            // Esto depende de c�mo est�n estructurados tus datos
            // Por ahora usamos un valor aleatorio simulado
            derivedMetrics["jumpEfficiency"].Add(UnityEngine.Random.Range(0.3f, 0.9f));
        }
    }

    private void OnMetricSelected(int index)
    {
        switch (index)
        {
            case 0: currentMetric = "bestFitness"; break;
            case 1: currentMetric = "averageFitness"; break;
            case 2: currentMetric = "worstFitness"; break;
            case 3: currentMetric = "fitnessRange"; break;
            case 4: currentMetric = "networkComplexity"; break;
            case 5: currentMetric = "diversityIndex"; break;
            case 6: currentMetric = "learningRate"; break;
            case 7: currentMetric = "successRate"; break;
            case 8: currentMetric = "all"; break;
            default: currentMetric = "bestFitness"; break;
        }

        UpdateGraph();
    }

    public void UpdateGraph()
    {
        ClearGraph();

        if (loadedTrainingDataList.Count == 0) return;

        if (currentMetric == "all")
        {
            // Mostrar m�ltiples l�neas a la vez
            CreateGraphLine("bestFitness", bestFitnessColor);
            CreateGraphLine("averageFitness", avgFitnessColor);
            CreateGraphLine("worstFitness", worstFitnessColor);
        }
        else if (derivedMetrics.ContainsKey(currentMetric))
        {
            // Mostrar m�trica derivada
            Color color = currentMetric == "learningRate" ? Color.cyan :
                         (currentMetric == "diversityIndex" ? Color.magenta : Color.white);
            CreateDerivedMetricLine(currentMetric, color);
        }
        else
        {
            // Mostrar m�trica est�ndar
            Color color = currentMetric == "bestFitness" ? bestFitnessColor :
                         (currentMetric == "averageFitness" ? avgFitnessColor : worstFitnessColor);
            CreateGraphLine(currentMetric, color);
        }
    }

    private void CreateGraphLine(string metric, Color color)
    {
        if (loadedTrainingDataList.Count <= 1) return;

        // Encontrar valores m�nimos y m�ximos para escalar la gr�fica
        float minVal = float.MaxValue;
        float maxVal = float.MinValue;

        foreach (var data in loadedTrainingDataList)
        {
            float value = 0;
            switch (metric)
            {
                case "bestFitness": value = data.bestFitness; break;
                case "averageFitness": value = data.averageFitness; break;
                case "worstFitness": value = data.worstFitness; break;
            }

            if (value < minVal) minVal = value;
            if (value > maxVal) maxVal = value;
        }

        // A�adir margen
        float range = maxVal - minVal;
        minVal -= range * 0.1f;
        maxVal += range * 0.1f;

        // Crear objetos para los puntos de datos
        List<GameObject> linePoints = new List<GameObject>();
        List<Vector2> pointPositions = new List<Vector2>();

        for (int i = 0; i < loadedTrainingDataList.Count; i++)
        {
            var data = loadedTrainingDataList[i];
            float value = 0;

            switch (metric)
            {
                case "bestFitness": value = data.bestFitness; break;
                case "averageFitness": value = data.averageFitness; break;
                case "worstFitness": value = data.worstFitness; break;
            }

            // Normalizar valor para la altura de la gr�fica
            float normalizedY = Mathf.InverseLerp(minVal, maxVal, value);
            float xPos = (float)i / (loadedTrainingDataList.Count - 1) * graphWidth;
            float yPos = normalizedY * graphHeight;

            // Crear punto visual
            GameObject point = Instantiate(dataPointPrefab, graphContainer);
            point.transform.localPosition = new Vector3(xPos, yPos, 0);

            // Configurar color del punto
            Image pointImage = point.GetComponent<Image>();
            if (pointImage != null)
            {
                pointImage.color = color;
            }

            // A�adir etiqueta de valor si est� habilitado
            if (showValueLabels)
            {
                GameObject label = new GameObject($"Label_{i}");
                label.transform.SetParent(point.transform, false);
                TextMeshProUGUI labelText = label.AddComponent<TextMeshProUGUI>();
                labelText.text = value.ToString("F1");
                labelText.fontSize = 12;
                labelText.alignment = TextAlignmentOptions.Center;
                labelText.color = color;
                label.transform.localPosition = new Vector3(0, 15, 0);
                linePoints.Add(label);
            }

            linePoints.Add(point);
            pointPositions.Add(new Vector2(xPos, yPos));
        }

        // Crear l�neas entre puntos
        for (int i = 0; i < pointPositions.Count - 1; i++)
        {
            GameObject line = Instantiate(lineRendererPrefab, graphContainer);
            RectTransform lineRect = line.GetComponent<RectTransform>();

            // Calcular posici�n y rotaci�n de la l�nea
            Vector2 point1 = pointPositions[i];
            Vector2 point2 = pointPositions[i + 1];
            float distance = Vector2.Distance(point1, point2);
            Vector2 midPoint = (point1 + point2) / 2;

            lineRect.localPosition = new Vector3(midPoint.x, midPoint.y, 0);
            lineRect.sizeDelta = new Vector2(distance, 2); // ancho = distancia, alto = grosor de l�nea

            // Calcular �ngulo entre los puntos
            float angle = Mathf.Atan2(point2.y - point1.y, point2.x - point1.x) * Mathf.Rad2Deg;
            lineRect.localRotation = Quaternion.Euler(0, 0, angle);

            // Configurar color de la l�nea
            Image lineImage = line.GetComponent<Image>();
            if (lineImage != null)
            {
                lineImage.color = color;
            }

            linePoints.Add(line);
        }

        // Guardar referencias para limpieza posterior
        graphElements[metric] = linePoints;
    }

    private void CreateDerivedMetricLine(string metric, Color color)
    {
        if (!derivedMetrics.ContainsKey(metric) || derivedMetrics[metric].Count <= 1) return;

        List<float> values = derivedMetrics[metric];

        // Encontrar valores m�nimos y m�ximos para escalar la gr�fica
        float minVal = values.Min();
        float maxVal = values.Max();

        // A�adir margen
        float range = maxVal - minVal;
        minVal -= range * 0.1f;
        maxVal += range * 0.1f;

        // Ajuste para m�tricas espec�ficas
        if (metric == "successRate" || metric == "jumpEfficiency")
        {
            minVal = 0;
            maxVal = 1;
        }

        // Crear objetos para los puntos de datos
        List<GameObject> linePoints = new List<GameObject>();
        List<Vector2> pointPositions = new List<Vector2>();

        for (int i = 0; i < values.Count; i++)
        {
            float value = values[i];

            // Normalizar valor para la altura de la gr�fica
            float normalizedY = Mathf.InverseLerp(minVal, maxVal, value);
            float xPos = (float)i / (values.Count - 1) * graphWidth;
            float yPos = normalizedY * graphHeight;

            // Crear punto visual
            GameObject point = Instantiate(dataPointPrefab, graphContainer);
            point.transform.localPosition = new Vector3(xPos, yPos, 0);

            // Configurar color del punto
            Image pointImage = point.GetComponent<Image>();
            if (pointImage != null)
            {
                pointImage.color = color;
            }

            // A�adir etiqueta de valor si est� habilitado
            if (showValueLabels)
            {
                GameObject label = new GameObject($"Label_{i}");
                label.transform.SetParent(point.transform, false);
                TextMeshProUGUI labelText = label.AddComponent<TextMeshProUGUI>();

                // Formato especial para porcentajes
                if (metric == "successRate" || metric == "jumpEfficiency")
                {
                    labelText.text = (value * 100).ToString("F0") + "%";
                }
                else
                {
                    labelText.text = value.ToString("F1");
                }

                labelText.fontSize = 12;
                labelText.alignment = TextAlignmentOptions.Center;
                labelText.color = color;
                label.transform.localPosition = new Vector3(0, 15, 0);
                linePoints.Add(label);
            }

            linePoints.Add(point);
            pointPositions.Add(new Vector2(xPos, yPos));
        }

        // Crear l�neas entre puntos
        for (int i = 0; i < pointPositions.Count - 1; i++)
        {
            GameObject line = Instantiate(lineRendererPrefab, graphContainer);
            RectTransform lineRect = line.GetComponent<RectTransform>();

            // Calcular posici�n y rotaci�n de la l�nea
            Vector2 point1 = pointPositions[i];
            Vector2 point2 = pointPositions[i + 1];
            float distance = Vector2.Distance(point1, point2);
            Vector2 midPoint = (point1 + point2) / 2;

            lineRect.localPosition = new Vector3(midPoint.x, midPoint.y, 0);
            lineRect.sizeDelta = new Vector2(distance, 2); // ancho = distancia, alto = grosor de l�nea

            // Calcular �ngulo entre los puntos
            float angle = Mathf.Atan2(point2.y - point1.y, point2.x - point1.x) * Mathf.Rad2Deg;
            lineRect.localRotation = Quaternion.Euler(0, 0, angle);

            // Configurar color de la l�nea
            Image lineImage = line.GetComponent<Image>();
            if (lineImage != null)
            {
                lineImage.color = color;
            }

            linePoints.Add(line);
        }

        // Guardar referencias para limpieza posterior
        graphElements[metric] = linePoints;
    }

    private void UpdateStatsText()
    {
        if (loadedTrainingDataList.Count == 0 || statsText == null) return;

        // Mostrar estad�sticas del �ltimo archivo cargado
        TrainingData latestData = loadedTrainingDataList.Last();

        string statsInfo = $"<b>Estad�sticas de Entrenamiento</b>\n" +
                          $"Generaci�n: {latestData.generation}\n" +
                          $"Guardado: {latestData.timestamp}\n\n" +
                          $"<color=green>Mejor Fitness: {latestData.bestFitness:F2}</color>\n" +
                          $"<color=yellow>Fitness Promedio: {latestData.averageFitness:F2}</color>\n" +
                          $"<color=red>Peor Fitness: {latestData.worstFitness:F2}</color>\n\n";

        // A�adir informaci�n sobre las redes guardadas
        if (latestData.networks != null && latestData.networks.Count > 0)
        {
            statsInfo += $"Redes guardadas: {latestData.networks.Count}\n\n";

            // Analizar tipos de NPCs
            int allyCount = 0;
            int enemyCount = 0;

            foreach (var network in latestData.networks)
            {
                if (network.npcType == NPCController.NPCType.Ally)
                    allyCount++;
                else
                    enemyCount++;
            }

            statsInfo += $"Aliados: {allyCount}, Enemigos: {enemyCount}\n\n";

            // Informaci�n sobre la mejor red
            SerializedNetwork bestNetwork = latestData.networks.OrderByDescending(n => n.fitness).First();

            statsInfo += "<b>Mejor Red:</b>\n" +
                        $"Tipo: {bestNetwork.npcType}\n" +
                        $"Fitness: {bestNetwork.fitness:F2}\n";

            if (bestNetwork.layers != null)
            {
                statsInfo += $"Estructura: {string.Join("-", bestNetwork.layers)}\n";
            }

            if (bestNetwork.outputLockStatus != null)
            {
                statsInfo += "Bloqueos: ";
                string[] behaviors = { "Avanzar", "Girar Izq", "Girar Der", "Saltar" };

                for (int i = 0; i < bestNetwork.outputLockStatus.Length && i < behaviors.Length; i++)
                {
                    if (bestNetwork.outputLockStatus[i])
                    {
                        statsInfo += $"{behaviors[i]}, ";
                    }
                }
                statsInfo += "\n";
            }
        }

        // A�adir m�tricas derivadas si est�n disponibles
        if (derivedMetrics.Count > 0 && loadedTrainingDataList.Count > 1)
        {
            statsInfo += "\n<b>Tendencias:</b>\n";

            // Tasa de aprendizaje (promedio de mejora por generaci�n)
            if (derivedMetrics.ContainsKey("learningRate") && derivedMetrics["learningRate"].Count > 0)
            {
                float avgLearningRate = derivedMetrics["learningRate"].Average();
                statsInfo += $"Mejora promedio: {avgLearningRate:F2} por generaci�n\n";
            }

            // Diversidad de la poblaci�n
            if (derivedMetrics.ContainsKey("diversityIndex") && derivedMetrics["diversityIndex"].Count > 0)
            {
                float latestDiversity = derivedMetrics["diversityIndex"].Last();
                statsInfo += $"Diversidad actual: {latestDiversity:F2}\n";
            }
        }

        statsText.text = statsInfo;
    }

    private void ClearGraph()
    {
        foreach (var elements in graphElements.Values)
        {
            foreach (var element in elements)
            {
                if (element != null)
                {
                    Destroy(element);
                }
            }
        }

        graphElements.Clear();
    }

    // M�todo para actualizar la visualizaci�n manualmente (ej. desde un bot�n)
    public void RefreshVisualization()
    {
        LoadTrainingFiles();
    }

    // M�todo para exportar los datos como CSV
    public void ExportDataToCSV()
    {
        if (loadedTrainingDataList.Count == 0) return;

        string csvContent = "Generation,BestFitness,AverageFitness,WorstFitness,FitnessRange,NetworkComplexity,DiversityIndex,LearningRate,SuccessRate,JumpEfficiency\n";

        for (int i = 0; i < loadedTrainingDataList.Count; i++)
        {
            TrainingData data = loadedTrainingDataList[i];

            csvContent += $"{data.generation},{data.bestFitness},{data.averageFitness},{data.worstFitness}";

            // A�adir m�tricas derivadas si est�n disponibles
            if (derivedMetrics.Count > 0)
            {
                csvContent += $",{(i < derivedMetrics["fitnessRange"].Count ? derivedMetrics["fitnessRange"][i] : 0)}";
                csvContent += $",{(i < derivedMetrics["networkComplexity"].Count ? derivedMetrics["networkComplexity"][i] : 0)}";
                csvContent += $",{(i < derivedMetrics["diversityIndex"].Count ? derivedMetrics["diversityIndex"][i] : 0)}";
                csvContent += $",{(i < derivedMetrics["learningRate"].Count ? derivedMetrics["learningRate"][i] : 0)}";
                csvContent += $",{(i < derivedMetrics["successRate"].Count ? derivedMetrics["successRate"][i] : 0)}";
                csvContent += $",{(i < derivedMetrics["jumpEfficiency"].Count ? derivedMetrics["jumpEfficiency"][i] : 0)}";
            }

            csvContent += "\n";
        }

        // Guardar en la misma carpeta que los archivos de entrenamiento
        string savePath;
        if (loadFromProjectFolder)
        {
            savePath = Path.Combine(Application.dataPath, projectLoadFolder, "TrainingStats.csv");
        }
        else
        {
            savePath = Path.Combine(Application.persistentDataPath, loadFolder, "TrainingStats.csv");
        }

        File.WriteAllText(savePath, csvContent);
        Debug.Log($"Datos exportados a {savePath}");
    }
}