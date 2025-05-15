using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;
using TMPro;
using UnityEngine.UI;

[System.Serializable]
public class ColorScheme
{
    public Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
    public Color primaryColor = new Color(0.2f, 0.6f, 1f);
    public Color secondaryColor = new Color(0.2f, 0.8f, 0.2f);
    public Color accentColor = new Color(1f, 0.6f, 0.2f);
    public Color textColor = Color.white;
    public Color warningColor = new Color(1f, 0.6f, 0f);
    public Color errorColor = new Color(1f, 0.2f, 0.2f);
}

[System.Serializable]
public class ExportSettings
{
    public bool includeAllMetrics = true;
    public bool includeNetworkStructure = true;
    public bool includeRawData = false;
    public string exportFormat = "CSV";
    public string customExportPath = "";
}

public class TrainingVisualizationSettings : MonoBehaviour
{
    [Header("Configuración Visual")]
    [Tooltip("Esquema de colores para la interfaz")]
    public ColorScheme colorScheme = new ColorScheme();

    [Tooltip("Mostrar etiquetas de valores en gráficas")]
    public bool showDataLabels = true;

    [Tooltip("Número de puntos de datos a mostrar en gráficas (0 = todos)")]
    public int maxDataPoints = 0;

    [Tooltip("Tamaño de fuente para etiquetas")]
    public int labelFontSize = 12;

    [Tooltip("Grosor de línea para gráficas")]
    public float lineThickness = 2f;

    [Tooltip("Tamaño de los puntos de datos")]
    public float dataPointSize = 10f;

    [Header("Configuración de Carga")]
    [Tooltip("Carpeta de carga de archivos de entrenamiento")]
    public string loadFolder = "TrainingData";

    [Tooltip("Cargar desde carpeta del proyecto en lugar de persistentDataPath")]
    public bool loadFromProjectFolder = true;

    [Tooltip("Ordenar archivos por fecha")]
    public bool sortFilesByDate = true;

    [Tooltip("Cargar automáticamente archivos relacionados")]
    public bool autoLoadRelatedFiles = true;

    [Header("Configuración de Exportación")]
    [Tooltip("Configuración para exportar datos")]
    public ExportSettings exportSettings = new ExportSettings();

    [Header("UI Settings Panel")]
    [Tooltip("Panel de configuración")]
    public GameObject settingsPanel;

    [Tooltip("Toggle para mostrar/ocultar etiquetas")]
    public Toggle showLabelsToggle;

    [Tooltip("Slider para grosor de línea")]
    public Slider lineThicknessSlider;

    [Tooltip("Slider para tamaño de punto")]
    public Slider pointSizeSlider;

    [Tooltip("Dropdown para formato de exportación")]
    public TMP_Dropdown exportFormatDropdown;

    [Tooltip("Input para ruta de exportación")]
    public TMP_InputField exportPathInput;

    // Referencias a componentes del sistema
    private TrainingVisualizer trainingVisualizer;
    private NetworkAnalyzer networkAnalyzer;
    private TrainingDashboard dashboard;

    // Singleton para acceso fácil
    public static TrainingVisualizationSettings Instance { get; private set; }

    // Lista para almacenar preferencias de usuario
    [System.Serializable]
    private class SavedSettings
    {
        public ColorScheme colorScheme;
        public bool showDataLabels;
        public int maxDataPoints;
        public float lineThickness;
        public float dataPointSize;
        public string loadFolder;
        public bool loadFromProjectFolder;
        public ExportSettings exportSettings;
    }

    void Awake()
    {
        // Configuración del singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Encontrar referencias
        trainingVisualizer = FindObjectOfType<TrainingVisualizer>();
        networkAnalyzer = FindObjectOfType<NetworkAnalyzer>();
        dashboard = FindObjectOfType<TrainingDashboard>();

        // Cargar configuración guardada
        LoadSettings();

        // Inicializar panel de configuración
        InitializeSettingsPanel();
    }

    void Start()
    {
        // Ocultar panel de configuración al inicio
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        // Aplicar configuración a los componentes
        ApplySettings();
    }

    public void ToggleSettingsPanel()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(!settingsPanel.activeSelf);

            // Actualizar UI con los valores actuales
            if (settingsPanel.activeSelf)
            {
                UpdateSettingsPanelUI();
            }
        }
    }

    private void InitializeSettingsPanel()
    {
        if (settingsPanel == null) return;

        // Configurar toggles
        if (showLabelsToggle != null)
        {
            showLabelsToggle.isOn = showDataLabels;
            showLabelsToggle.onValueChanged.AddListener(value => {
                showDataLabels = value;
                ApplySettings();
            });
        }

        // Configurar sliders
        if (lineThicknessSlider != null)
        {
            lineThicknessSlider.value = lineThickness;
            lineThicknessSlider.onValueChanged.AddListener(value => {
                lineThickness = value;
                ApplySettings();
            });
        }

        if (pointSizeSlider != null)
        {
            pointSizeSlider.value = dataPointSize;
            pointSizeSlider.onValueChanged.AddListener(value => {
                dataPointSize = value;
                ApplySettings();
            });
        }

        // Configurar dropdown de formato
        if (exportFormatDropdown != null)
        {
            exportFormatDropdown.ClearOptions();
            exportFormatDropdown.AddOptions(new List<string> { "CSV", "JSON", "TXT" });

            int formatIndex = 0;
            switch (exportSettings.exportFormat)
            {
                case "CSV": formatIndex = 0; break;
                case "JSON": formatIndex = 1; break;
                case "TXT": formatIndex = 2; break;
            }

            exportFormatDropdown.value = formatIndex;
            exportFormatDropdown.onValueChanged.AddListener(value => {
                switch (value)
                {
                    case 0: exportSettings.exportFormat = "CSV"; break;
                    case 1: exportSettings.exportFormat = "JSON"; break;
                    case 2: exportSettings.exportFormat = "TXT"; break;
                }
            });
        }

        // Configurar input para ruta
        if (exportPathInput != null)
        {
            exportPathInput.text = exportSettings.customExportPath;
            exportPathInput.onEndEdit.AddListener(value => {
                exportSettings.customExportPath = value;
            });
        }
    }

    private void UpdateSettingsPanelUI()
    {
        if (showLabelsToggle != null)
        {
            showLabelsToggle.isOn = showDataLabels;
        }

        if (lineThicknessSlider != null)
        {
            lineThicknessSlider.value = lineThickness;
        }

        if (pointSizeSlider != null)
        {
            pointSizeSlider.value = dataPointSize;
        }

        if (exportPathInput != null)
        {
            exportPathInput.text = exportSettings.customExportPath;
        }
    }

    private void ApplySettings()
    {
        // Aplicar configuración al visualizador de entrenamiento
        if (trainingVisualizer != null)
        {
            trainingVisualizer.showValueLabels = showDataLabels;

            // Actualizar rutas de carga
            trainingVisualizer.loadFolder = loadFolder;
            trainingVisualizer.loadFromProjectFolder = loadFromProjectFolder;

            // Forzar actualización visual
            trainingVisualizer.UpdateGraph();
        }

        // Aplicar configuración al analizador de redes
        if (networkAnalyzer != null)
        {
            networkAnalyzer.showNeuronLabels = showDataLabels;

            // Si la red está visualizada, actualizar
            if (networkAnalyzer.gameObject.activeInHierarchy)
            {
                // Usar reflexión para llamar al método privado
                System.Reflection.MethodInfo methodInfo =
                    typeof(NetworkAnalyzer).GetMethod("UpdateNetworkDetails",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);

                if (methodInfo != null)
                {
                    methodInfo.Invoke(networkAnalyzer, null);
                }
            }
        }

        // Guardar configuración
        SaveSettings();
    }

    public void SaveSettings()
    {
        SavedSettings settings = new SavedSettings
        {
            colorScheme = colorScheme,
            showDataLabels = showDataLabels,
            maxDataPoints = maxDataPoints,
            lineThickness = lineThickness,
            dataPointSize = dataPointSize,
            loadFolder = loadFolder,
            loadFromProjectFolder = loadFromProjectFolder,
            exportSettings = exportSettings
        };

        string json = JsonUtility.ToJson(settings, true);
        string path = Path.Combine(Application.persistentDataPath, "VisualizationSettings.json");

        try
        {
            File.WriteAllText(path, json);
            Debug.Log("Configuración guardada en: " + path);
        }
        catch (Exception e)
        {
            Debug.LogError("Error al guardar configuración: " + e.Message);
        }
    }

    public void LoadSettings()
    {
        string path = Path.Combine(Application.persistentDataPath, "VisualizationSettings.json");

        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                SavedSettings settings = JsonUtility.FromJson<SavedSettings>(json);

                if (settings != null)
                {
                    colorScheme = settings.colorScheme;
                    showDataLabels = settings.showDataLabels;
                    maxDataPoints = settings.maxDataPoints;
                    lineThickness = settings.lineThickness;
                    dataPointSize = settings.dataPointSize;
                    loadFolder = settings.loadFolder;
                    loadFromProjectFolder = settings.loadFromProjectFolder;
                    exportSettings = settings.exportSettings;

                    Debug.Log("Configuración cargada desde: " + path);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error al cargar configuración: " + e.Message);
            }
        }
        else
        {
            Debug.Log("No se encontró archivo de configuración. Usando valores por defecto.");
        }
    }

    public void ResetToDefaults()
    {
        // Restablecer valores predeterminados
        colorScheme = new ColorScheme();
        showDataLabels = true;
        maxDataPoints = 0;
        labelFontSize = 12;
        lineThickness = 2f;
        dataPointSize = 10f;
        loadFolder = "TrainingData";
        loadFromProjectFolder = true;
        exportSettings = new ExportSettings();

        // Actualizar UI
        UpdateSettingsPanelUI();

        // Aplicar cambios
        ApplySettings();
    }

    public string GetExportPath()
    {
        if (!string.IsNullOrEmpty(exportSettings.customExportPath))
        {
            return exportSettings.customExportPath;
        }

        // Ruta predeterminada
        return Path.Combine(
            loadFromProjectFolder ?
            Path.Combine(Application.dataPath, loadFolder) :
            Path.Combine(Application.persistentDataPath, loadFolder)
        );
    }

    // Método para aplicar el esquema de colores a un elemento UI
    public void ApplyColorScheme(GameObject uiElement, string elementType)
    {
        if (uiElement == null) return;

        // Obtener componentes de imagen y texto
        Image image = uiElement.GetComponent<Image>();
        TextMeshProUGUI text = uiElement.GetComponent<TextMeshProUGUI>();
        Button button = uiElement.GetComponent<Button>();

        // Aplicar colores según el tipo de elemento
        switch (elementType.ToLower())
        {
            case "background":
                if (image != null) image.color = colorScheme.backgroundColor;
                break;

            case "primary":
                if (image != null) image.color = colorScheme.primaryColor;
                if (text != null) text.color = colorScheme.textColor;
                break;

            case "secondary":
                if (image != null) image.color = colorScheme.secondaryColor;
                if (text != null) text.color = colorScheme.textColor;
                break;

            case "accent":
                if (image != null) image.color = colorScheme.accentColor;
                if (text != null) text.color = colorScheme.textColor;
                break;

            case "button":
                if (button != null && button.colors != null)
                {
                    ColorBlock colors = button.colors;
                    colors.normalColor = colorScheme.primaryColor;
                    colors.highlightedColor = Color.Lerp(colorScheme.primaryColor, Color.white, 0.2f);
                    colors.pressedColor = Color.Lerp(colorScheme.primaryColor, Color.black, 0.2f);
                    colors.selectedColor = colorScheme.accentColor;
                    button.colors = colors;
                }
                break;

            case "text":
                if (text != null) text.color = colorScheme.textColor;
                break;

            case "warning":
                if (image != null) image.color = colorScheme.warningColor;
                if (text != null) text.color = colorScheme.warningColor;
                break;

            case "error":
                if (image != null) image.color = colorScheme.errorColor;
                if (text != null) text.color = colorScheme.errorColor;
                break;
        }
    }

    // Aplicar el esquema de colores a toda la interfaz
    public void ApplyColorSchemeToUI()
    {
        if (dashboard == null) return;

        // Obtener panels principales
        if (dashboard.generationsPanel != null)
            ApplyColorScheme(dashboard.generationsPanel, "background");

        if (dashboard.networksPanel != null)
            ApplyColorScheme(dashboard.networksPanel, "background");

        if (dashboard.comparisonPanel != null)
            ApplyColorScheme(dashboard.comparisonPanel, "background");

        if (dashboard.infoPanel != null)
            ApplyColorScheme(dashboard.infoPanel, "background");

        // Obtener botones de pestañas
        if (dashboard.tabButtons != null)
        {
            foreach (var tabButton in dashboard.tabButtons)
            {
                ApplyColorScheme(tabButton, "button");
            }
        }

        // Actualizar textos principales
        if (dashboard.currentFileText != null)
            ApplyColorScheme(dashboard.currentFileText.gameObject, "text");

        if (dashboard.npcTypeStatsText != null)
            ApplyColorScheme(dashboard.npcTypeStatsText.gameObject, "text");

        if (dashboard.selectedFilesText != null)
            ApplyColorScheme(dashboard.selectedFilesText.gameObject, "text");
    }
}