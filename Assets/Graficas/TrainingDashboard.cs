using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;

public class TrainingDashboard : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Referencia al componente TrainingVisualizer")]
    public TrainingVisualizer trainingVisualizer;

    [Tooltip("Referencia al componente NetworkAnalyzer")]
    public NetworkAnalyzer networkAnalyzer;

    [Header("Componentes UI")]
    [Tooltip("Pestañas para cambiar entre vistas")]
    public GameObject[] tabButtons;

    [Tooltip("Paneles correspondientes a cada pestaña")]
    public GameObject[] tabPanels;

    [Tooltip("Panel de visualización de generaciones")]
    public GameObject generationsPanel;

    [Tooltip("Panel de visualización de redes")]
    public GameObject networksPanel;

    [Tooltip("Panel de comparación")]
    public GameObject comparisonPanel;

    [Tooltip("Panel de información")]
    public GameObject infoPanel;

    [Tooltip("Texto que muestra el archivo actualmente cargado")]
    public TextMeshProUGUI currentFileText;

    [Tooltip("Botón para exportar los datos a CSV")]
    public Button exportButton;

    [Tooltip("Botón para refrescar la visualización")]
    public Button refreshButton;

    [Header("Comparación de Archivos")]
    [Tooltip("Dropdown para seleccionar archivos a comparar")]
    public TMP_Dropdown compareFileDropdown;

    [Tooltip("Botón para añadir archivo a la comparación")]
    public Button addCompareButton;

    [Tooltip("Lista de archivos seleccionados para comparar")]
    public TextMeshProUGUI selectedFilesText;

    [Tooltip("Contenedor para la gráfica de comparación")]
    public RectTransform comparisonGraphContainer;

    [Tooltip("Dropdown para seleccionar métrica de comparación")]
    public TMP_Dropdown compareMetricDropdown;

    [Header("Estadísticas de NPCs")]
    [Tooltip("Texto para mostrar estadísticas de tipos de NPCs")]
    public TextMeshProUGUI npcTypeStatsText;

    [Header("Filtros y Ordenación")]
    [Tooltip("Dropdown para ordenar la lista de redes")]
    public TMP_Dropdown sortNetworksDropdown;

    [Tooltip("Toggle para mostrar solo aliados")]
    public Toggle showOnlyAlliesToggle;

    [Tooltip("Toggle para mostrar solo enemigos")]
    public Toggle showOnlyEnemiesToggle;

    // Variables internas
    private int currentTabIndex = 0;
    private List<string> filesToCompare = new List<string>();
    private List<TrainingData> loadedComparisonData = new List<TrainingData>();
    private Dictionary<string, List<GameObject>> comparisonGraphElements = new Dictionary<string, List<GameObject>>();
    private List<SerializedNetwork> filteredNetworks = new List<SerializedNetwork>();

    void Start()
    {
        InitializeDashboard();
    }

    private void InitializeDashboard()
    {
        // Verificar dependencias
        if (trainingVisualizer == null)
        {
            trainingVisualizer = FindObjectOfType<TrainingVisualizer>();
        }

        if (networkAnalyzer == null)
        {
            networkAnalyzer = FindObjectOfType<NetworkAnalyzer>();
        }

        // Inicializar pestañas
        SetActiveTab(0);

        for (int i = 0; i < tabButtons.Length; i++)
        {
            int tabIndex = i; // Capturar el índice para el lambda
            tabButtons[i].GetComponent<Button>().onClick.AddListener(() => SetActiveTab(tabIndex));
        }

        // Configurar otros botones
        if (refreshButton != null)
        {
            refreshButton.onClick.AddListener(RefreshDashboard);
        }

        if (exportButton != null)
        {
            exportButton.onClick.AddListener(ExportData);
        }

        if (addCompareButton != null)
        {
            addCompareButton.onClick.AddListener(AddFileToComparison);
        }

        // Configurar dropdowns
        if (compareMetricDropdown != null)
        {
            compareMetricDropdown.onValueChanged.AddListener(_ => UpdateComparisonGraph());
        }

        if (sortNetworksDropdown != null)
        {
            sortNetworksDropdown.onValueChanged.AddListener(_ => SortAndFilterNetworks());
        }

        // Configurar toggles
        if (showOnlyAlliesToggle != null)
        {
            showOnlyAlliesToggle.onValueChanged.AddListener(_ => SortAndFilterNetworks());
        }

        if (showOnlyEnemiesToggle != null)
        {
            showOnlyEnemiesToggle.onValueChanged.AddListener(_ => SortAndFilterNetworks());
        }
    }

    public void RefreshDashboard()
    {
        // Actualizar visualizador de entrenamiento
        if (trainingVisualizer != null)
        {
            trainingVisualizer.RefreshVisualization();
        }

        // Actualizar información de archivo actual
        UpdateCurrentFileInfo();

        // Actualizar panel de comparación
        RefreshComparisonFiles();
    }

    private void SetActiveTab(int tabIndex)
    {
        if (tabIndex < 0 || tabIndex >= tabPanels.Length) return;

        currentTabIndex = tabIndex;

        // Desactivar todos los paneles
        foreach (var panel in tabPanels)
        {
            panel.SetActive(false);
        }

        // Activar el panel seleccionado
        tabPanels[tabIndex].SetActive(true);

        // Cambiar estilo de los botones
        for (int i = 0; i < tabButtons.Length; i++)
        {
            Image btnImage = tabButtons[i].GetComponent<Image>();
            if (btnImage != null)
            {
                btnImage.color = i == tabIndex ? new Color(0.8f, 0.8f, 1f) : new Color(0.6f, 0.6f, 0.6f);
            }

            TextMeshProUGUI btnText = tabButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.fontStyle = i == tabIndex ? FontStyles.Bold : FontStyles.Normal;
            }
        }

        // Actualizar la pestaña seleccionada
        UpdateCurrentTabContent();
    }

    private void UpdateCurrentTabContent()
    {
        switch (currentTabIndex)
        {
            case 0: // Generaciones
                // Contenido ya actualizado por TrainingVisualizer
                break;

            case 1: // Redes
                UpdateNetworksPanel();
                break;

            case 2: // Comparación
                RefreshComparisonFiles();
                break;

            case 3: // Información
                UpdateInfoPanel();
                break;
        }
    }

    private void UpdateCurrentFileInfo()
    {
        if (currentFileText == null) return;

        if (trainingVisualizer != null && trainingVisualizer.loadedTrainingDataList.Count > 0)
        {
            var latestData = trainingVisualizer.loadedTrainingDataList.Last();
            currentFileText.text = $"Generación {latestData.generation} - {latestData.timestamp}";
        }
        else
        {
            currentFileText.text = "Ningún archivo cargado";
        }
    }

    private void UpdateNetworksPanel()
    {
        if (trainingVisualizer == null ||
            trainingVisualizer.loadedTrainingDataList.Count == 0 ||
            networkAnalyzer == null) return;

        // Obtener el archivo más reciente
        TrainingData latestData = trainingVisualizer.loadedTrainingDataList.Last();

        if (latestData.networks == null || latestData.networks.Count == 0)
        {
            if (npcTypeStatsText != null)
            {
                npcTypeStatsText.text = "No hay redes disponibles para analizar.";
            }
            return;
        }

        // Filtrar y ordenar redes
        SortAndFilterNetworks();

        // Analizar tipos de NPCs
        if (npcTypeStatsText != null)
        {
            int allyCount = latestData.networks.Count(n => n.npcType == NPCController.NPCType.Ally);
            int enemyCount = latestData.networks.Count(n => n.npcType == NPCController.NPCType.Enemy);

            float allyAvgFitness = latestData.networks
                .Where(n => n.npcType == NPCController.NPCType.Ally)
                .Average(n => n.fitness);

            float enemyAvgFitness = latestData.networks
                .Where(n => n.npcType == NPCController.NPCType.Enemy)
                .Average(n => n.fitness);

            npcTypeStatsText.text = $"<b>Distribución de NPCs:</b>\n" +
                                  $"Aliados: {allyCount} (Fitness promedio: {allyAvgFitness:F2})\n" +
                                  $"Enemigos: {enemyCount} (Fitness promedio: {enemyAvgFitness:F2})\n\n" +
                                  $"Diferencia promedio: {Mathf.Abs(allyAvgFitness - enemyAvgFitness):F2}";
        }
    }

    private void SortAndFilterNetworks()
    {
        if (trainingVisualizer == null ||
            trainingVisualizer.loadedTrainingDataList.Count == 0) return;

        TrainingData latestData = trainingVisualizer.loadedTrainingDataList.Last();

        if (latestData.networks == null)
        {
            filteredNetworks.Clear();
            return;
        }

        // Filtrar por tipo
        bool showAllies = !showOnlyEnemiesToggle.isOn;
        bool showEnemies = !showOnlyAlliesToggle.isOn;

        var filtered = latestData.networks.Where(n =>
            (n.npcType == NPCController.NPCType.Ally && showAllies) ||
            (n.npcType == NPCController.NPCType.Enemy && showEnemies)
        ).ToList();

        // Ordenar según criterio seleccionado
        switch (sortNetworksDropdown.value)
        {
            case 0: // Mejor fitness primero
                filtered = filtered.OrderByDescending(n => n.fitness).ToList();
                break;

            case 1: // Peor fitness primero
                filtered = filtered.OrderBy(n => n.fitness).ToList();
                break;

            case 2: // Aliados primero
                filtered = filtered.OrderByDescending(n => n.npcType == NPCController.NPCType.Ally)
                                 .ThenByDescending(n => n.fitness)
                                 .ToList();
                break;

            case 3: // Enemigos primero
                filtered = filtered.OrderByDescending(n => n.npcType == NPCController.NPCType.Enemy)
                                 .ThenByDescending(n => n.fitness)
                                 .ToList();
                break;
        }

        filteredNetworks = filtered;

        // Actualizar visualizador de redes
        if (networkAnalyzer != null)
        {
            networkAnalyzer.SetNetworks(filteredNetworks);
        }
    }

    private void RefreshComparisonFiles()
    {
        if (compareFileDropdown == null) return;

        // Obtener lista de archivos
        string fullPath;
        if (trainingVisualizer.loadFromProjectFolder)
        {
            fullPath = Path.Combine(Application.dataPath, trainingVisualizer.projectLoadFolder);
        }
        else
        {
            fullPath = Path.Combine(Application.persistentDataPath, trainingVisualizer.loadFolder);
        }

        if (!Directory.Exists(fullPath)) return;

        string[] files = Directory.GetFiles(fullPath, "*.json");
        files = files.OrderByDescending(f => new FileInfo(f).LastWriteTime).ToArray();

        // Actualizar dropdown
        compareFileDropdown.ClearOptions();
        List<string> options = new List<string>();

        foreach (string file in files)
        {
            string fileName = Path.GetFileName(file);

            // Extraer información básica
            string displayName = fileName;
            try
            {
                string json = File.ReadAllText(file);
                TrainingData data = JsonUtility.FromJson<TrainingData>(json);
                if (data != null)
                {
                    displayName = $"{fileName} - Gen {data.generation}";
                }
            }
            catch (Exception) { }

            options.Add(displayName);
        }

        compareFileDropdown.AddOptions(options);

        // Actualizar lista de archivos seleccionados
        UpdateSelectedFilesText();
    }

    private void AddFileToComparison()
    {
        if (compareFileDropdown == null) return;

        int index = compareFileDropdown.value;

        // Determinar ruta
        string fullPath;
        if (trainingVisualizer.loadFromProjectFolder)
        {
            fullPath = Path.Combine(Application.dataPath, trainingVisualizer.projectLoadFolder);
        }
        else
        {
            fullPath = Path.Combine(Application.persistentDataPath, trainingVisualizer.loadFolder);
        }

        string[] files = Directory.GetFiles(fullPath, "*.json");
        files = files.OrderByDescending(f => new FileInfo(f).LastWriteTime).ToArray();

        if (index < 0 || index >= files.Length) return;

        string selectedFile = files[index];

        // Evitar duplicados
        if (!filesToCompare.Contains(selectedFile))
        {
            filesToCompare.Add(selectedFile);

            // Cargar el archivo
            try
            {
                string json = File.ReadAllText(selectedFile);
                TrainingData data = JsonUtility.FromJson<TrainingData>(json);
                if (data != null)
                {
                    loadedComparisonData.Add(data);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error al cargar archivo para comparación: {e.Message}");
            }

            UpdateSelectedFilesText();
            UpdateComparisonGraph();
        }
    }

    private void UpdateSelectedFilesText()
    {
        if (selectedFilesText == null) return;

        if (filesToCompare.Count == 0)
        {
            selectedFilesText.text = "Ningún archivo seleccionado para comparar.";
            return;
        }

        string text = "<b>Archivos seleccionados:</b>\n";

        for (int i = 0; i < filesToCompare.Count; i++)
        {
            string file = Path.GetFileName(filesToCompare[i]);

            // Buscar en datos cargados
            if (i < loadedComparisonData.Count && loadedComparisonData[i] != null)
            {
                text += $"{i + 1}. {file} - Gen {loadedComparisonData[i].generation}\n";
            }
            else
            {
                text += $"{i + 1}. {file}\n";
            }
        }

        selectedFilesText.text = text;
    }

    private void UpdateComparisonGraph()
    {
        ClearComparisonGraph();

        if (loadedComparisonData.Count < 2) return;

        int metricIndex = compareMetricDropdown.value;
        string metricName;

        switch (metricIndex)
        {
            case 0: metricName = "bestFitness"; break;
            case 1: metricName = "averageFitness"; break;
            case 2: metricName = "worstFitness"; break;
            default: metricName = "bestFitness"; break;
        }

        // Crear barras para cada archivo
        float barWidth = 50f;
        float spacing = 20f;
        float totalWidth = (barWidth + spacing) * loadedComparisonData.Count;
        float startX = -totalWidth / 2 + barWidth / 2;

        // Encontrar valor máximo para escalar
        float maxValue = 0;

        foreach (var data in loadedComparisonData)
        {
            float value = 0;
            switch (metricName)
            {
                case "bestFitness": value = data.bestFitness; break;
                case "averageFitness": value = data.averageFitness; break;
                case "worstFitness": value = data.worstFitness; break;
            }

            if (value > maxValue) maxValue = value;
        }

        // Altura máxima de barra
        float maxBarHeight = 300f;

        List<GameObject> elements = new List<GameObject>();
        Color[] barColors = {
            new Color(0.2f, 0.6f, 1f), // Azul
            new Color(0.2f, 0.8f, 0.2f), // Verde
            new Color(1f, 0.6f, 0.2f), // Naranja
            new Color(0.8f, 0.2f, 0.8f), // Púrpura
            new Color(1f, 0.8f, 0.2f)  // Amarillo
        };

        // Crear barras
        for (int i = 0; i < loadedComparisonData.Count; i++)
        {
            var data = loadedComparisonData[i];

            float value = 0;
            switch (metricName)
            {
                case "bestFitness": value = data.bestFitness; break;
                case "averageFitness": value = data.averageFitness; break;
                case "worstFitness": value = data.worstFitness; break;
            }

            // Calcular altura normalizada
            float normalizedHeight = value / maxValue;
            float barHeight = normalizedHeight * maxBarHeight;

            // Crear barra
            GameObject barObj = new GameObject($"Bar_{i}");
            barObj.transform.SetParent(comparisonGraphContainer, false);

            RectTransform barRect = barObj.AddComponent<RectTransform>();
            barRect.sizeDelta = new Vector2(barWidth, barHeight);
            barRect.anchoredPosition = new Vector2(startX + (barWidth + spacing) * i, barHeight / 2);

            Image barImage = barObj.AddComponent<Image>();
            barImage.color = barColors[i % barColors.Length];

            elements.Add(barObj);

            // Etiqueta de valor
            GameObject labelObj = new GameObject($"Label_{i}");
            labelObj.transform.SetParent(barObj.transform, false);

            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(barWidth, 30);
            labelRect.anchoredPosition = new Vector2(0, barHeight / 2 + 15);

            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = value.ToString("F1");
            labelText.fontSize = 14;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = Color.white;

            elements.Add(labelObj);

            // Etiqueta de generación
            GameObject genLabelObj = new GameObject($"GenLabel_{i}");
            genLabelObj.transform.SetParent(barObj.transform, false);

            RectTransform genLabelRect = genLabelObj.AddComponent<RectTransform>();
            genLabelRect.sizeDelta = new Vector2(barWidth + 20, 30);
            genLabelRect.anchoredPosition = new Vector2(0, -15);

            TextMeshProUGUI genLabelText = genLabelObj.AddComponent<TextMeshProUGUI>();
            genLabelText.text = $"Gen {data.generation}";
            genLabelText.fontSize = 12;
            genLabelText.alignment = TextAlignmentOptions.Center;
            genLabelText.color = Color.white;

            elements.Add(genLabelObj);
        }

        comparisonGraphElements["bars"] = elements;
    }

    private void ClearComparisonGraph()
    {
        foreach (var elements in comparisonGraphElements.Values)
        {
            foreach (var element in elements)
            {
                if (element != null)
                {
                    Destroy(element);
                }
            }
        }

        comparisonGraphElements.Clear();
    }

    private void UpdateInfoPanel()
    {
        // Aquí puedes mostrar información del sistema, documentación, etc.
    }

    public void ExportData()
    {
        if (trainingVisualizer != null)
        {
            trainingVisualizer.ExportDataToCSV();
        }
    }

    public void ClearComparisonSelection()
    {
        filesToCompare.Clear();
        loadedComparisonData.Clear();
        UpdateSelectedFilesText();
        ClearComparisonGraph();
    }
}