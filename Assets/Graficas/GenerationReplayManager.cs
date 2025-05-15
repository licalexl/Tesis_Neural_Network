using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System;

public class GenerationReplayManager : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Referencia al sistema de carga de entrenamiento")]
    public AITrainingLoader trainingLoader;

    [Tooltip("Referencia al algoritmo genético")]
    public NPCGeneticAlgorithm geneticAlgorithm;

    [Header("Controles de Reproducción")]
    [Tooltip("Dropdown para seleccionar generación a reproducir")]
    public TMP_Dropdown generationDropdown;

    [Tooltip("Botón para iniciar la reproducción")]
    public Button playButton;

    [Tooltip("Botón para pausar la reproducción")]
    public Button pauseButton;

    [Tooltip("Botón para detener y reiniciar la reproducción")]
    public Button stopButton;

    [Tooltip("Slider para controlar la velocidad de reproducción")]
    public Slider speedSlider;

    [Tooltip("Toggle para activar el modo de seguimiento de cámara")]
    public Toggle followCameraToggle;

    [Header("Información de Reproducción")]
    [Tooltip("Texto que muestra el tiempo de reproducción")]
    public TextMeshProUGUI timeInfoText;

    [Tooltip("Texto que muestra estadísticas de la reproducción actual")]
    public TextMeshProUGUI replayStatsText;

    [Tooltip("Indicador de progreso de reproducción")]
    public Slider progressSlider;

    [Header("Visualización de NPCs")]
    [Tooltip("Toggle para resaltar el mejor NPC")]
    public Toggle highlightBestToggle;

    [Tooltip("Toggle para mostrar solo los N mejores NPCs")]
    public Toggle showTopNPCsToggle;

    [Tooltip("Slider para seleccionar cuántos mejores NPCs mostrar")]
    public Slider topNPCsSlider;

    [Tooltip("Material para resaltar el mejor NPC")]
    public Material highlightMaterial;

    [Header("Configuración")]
    [Tooltip("Duración máxima de la reproducción en segundos")]
    public float maxReplayDuration = 60f;

    [Tooltip("Objeto que contiene la cámara de seguimiento")]
    public GameObject followCamera;

    [Tooltip("Distancia de la cámara al NPC en modo seguimiento")]
    public float cameraFollowDistance = 5f;

    [Tooltip("Altura de la cámara en modo seguimiento")]
    public float cameraFollowHeight = 3f;

    // Variables internas
    private bool isReplaying = false;
    private float replayTime = 0f;
    private TrainingData currentReplayData;
    private NPCController bestNPC;
    private NPCController[] topNPCs;
    private int topNPCCount = 5;
    private Dictionary<NPCController, float> originalSpeeds = new Dictionary<NPCController, float>();
    private Camera mainCamera;
    private Camera followCam;
    private Transform originalCameraTransform;
    private List<Material> originalMaterials = new List<Material>();
    private List<NPCController> hiddenNPCs = new List<NPCController>();

    void Start()
    {
        InitializeReplayManager();
    }

    private void InitializeReplayManager()
    {
        // Buscar componentes necesarios si no están asignados
        if (trainingLoader == null)
        {
            trainingLoader = FindObjectOfType<AITrainingLoader>();
        }

        if (geneticAlgorithm == null)
        {
            geneticAlgorithm = FindObjectOfType<NPCGeneticAlgorithm>();
        }

        // Configurar listeners para botones
        if (playButton != null)
        {
            playButton.onClick.AddListener(StartReplay);
        }

        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(PauseReplay);
            pauseButton.interactable = false;
        }

        if (stopButton != null)
        {
            stopButton.onClick.AddListener(StopReplay);
            stopButton.interactable = false;
        }

        // Configurar listener para slider de velocidad
        if (speedSlider != null)
        {
            speedSlider.onValueChanged.AddListener(SetReplaySpeed);
        }

        // Configurar listener para toggle de cámara
        if (followCameraToggle != null)
        {
            followCameraToggle.onValueChanged.AddListener(ToggleFollowCamera);
        }

        // Configurar listener para toggle de mejor NPC
        if (highlightBestToggle != null)
        {
            highlightBestToggle.onValueChanged.AddListener(ToggleHighlightBest);
        }

        // Configurar listener para selección de mejores NPCs
        if (showTopNPCsToggle != null)
        {
            showTopNPCsToggle.onValueChanged.AddListener(ToggleShowTopNPCs);
        }

        if (topNPCsSlider != null)
        {
            topNPCsSlider.onValueChanged.AddListener(SetTopNPCCount);
            topNPCCount = Mathf.RoundToInt(topNPCsSlider.value);
        }

        // Inicializar cámaras
        mainCamera = Camera.main;

        if (followCamera != null)
        {
            followCam = followCamera.GetComponent<Camera>();
            followCamera.SetActive(false);
        }

        // Guardar transformación original de la cámara principal
        if (mainCamera != null)
        {
            originalCameraTransform = new GameObject("OriginalCameraTransform").transform;
            originalCameraTransform.position = mainCamera.transform.position;
            originalCameraTransform.rotation = mainCamera.transform.rotation;
        }

        // Actualizar el dropdown con las generaciones disponibles
        RefreshGenerationDropdown();
    }

    public void RefreshGenerationDropdown()
    {
        if (generationDropdown == null) return;

        generationDropdown.ClearOptions();

        // Obtener la ruta de la carpeta de guardado
        string fullPath;
        if (trainingLoader.loadFromProjectFolder)
        {
            fullPath = System.IO.Path.Combine(Application.dataPath, trainingLoader.projectSaveFolder);
        }
        else
        {
            fullPath = System.IO.Path.Combine(Application.persistentDataPath, trainingLoader.saveFolder);
        }

        if (!System.IO.Directory.Exists(fullPath)) return;

        // Obtener archivos de guardado
        var files = System.IO.Directory.GetFiles(fullPath, "*.json");
        files = files.OrderByDescending(f => new System.IO.FileInfo(f).LastWriteTime).ToArray();

        List<string> options = new List<string>();

        foreach (var file in files)
        {
            string fileName = System.IO.Path.GetFileName(file);

            // Intentar extraer información básica
            try
            {
                string json = System.IO.File.ReadAllText(file);
                TrainingData data = JsonUtility.FromJson<TrainingData>(json);
                if (data != null)
                {
                    options.Add($"{fileName} - Gen {data.generation} ({data.timestamp})");
                }
                else
                {
                    options.Add(fileName);
                }
            }
            catch (Exception)
            {
                options.Add(fileName);
            }
        }

        generationDropdown.AddOptions(options);
        generationDropdown.RefreshShownValue();
    }

    private void StartReplay()
    {
        if (isReplaying)
        {
            // Si ya estamos reproduciendo, simplemente reanudamos
            Time.timeScale = speedSlider.value;
            pauseButton.interactable = true;
            playButton.interactable = false;
            return;
        }

        // Cargar el entrenamiento seleccionado
        int selectedIndex = generationDropdown.value;
        if (selectedIndex < 0 || trainingLoader == null) return;

        // Pausar el algoritmo genético
        if (geneticAlgorithm != null)
        {
            geneticAlgorithm.isPaused = true;
        }

        // Obtener el nombre del archivo seleccionado
        string fileName = generationDropdown.options[selectedIndex].text;
        fileName = fileName.Split(' ')[0]; // Obtener solo el nombre del archivo

        // Cargar el entrenamiento
        trainingLoader.LoadTraining(fileName);

        // Establecer estado de reproducción
        isReplaying = true;
        replayTime = 0f;

        // Ajustar controles
        playButton.interactable = false;
        pauseButton.interactable = true;
        stopButton.interactable = true;
        generationDropdown.interactable = false;

        // Asegurar que la velocidad de reproducción sea correcta
        Time.timeScale = speedSlider.value;

        // Inicializar seguimiento de NPCs
        InitializeNPCTracking();

        // Mostrar u ocultar NPCs según configuración
        UpdateNPCVisibility();
    }

    private void InitializeNPCTracking()
    {
        if (geneticAlgorithm == null || geneticAlgorithm.population == null) return;

        // Guardar velocidades originales de los NPCs
        originalSpeeds.Clear();

        foreach (var npc in geneticAlgorithm.population)
        {
            if (npc != null)
            {
                originalSpeeds[npc] = npc.moveSpeed;
            }
        }

        // Identificar al mejor NPC
        if (geneticAlgorithm.population.Count > 0)
        {
            bestNPC = geneticAlgorithm.population.OrderByDescending(npc => npc.fitness).FirstOrDefault();

            // Guardar materiales originales
            originalMaterials.Clear();
            if (bestNPC != null)
            {
                Renderer renderer = bestNPC.GetComponent<Renderer>();
                if (renderer != null)
                {
                    originalMaterials.Add(renderer.material);
                }
            }

            // Identificar los N mejores NPCs
            topNPCs = geneticAlgorithm.population
                .OrderByDescending(npc => npc.fitness)
                .Take(topNPCCount)
                .ToArray();
        }

        // Aplicar highlight si está activado
        if (highlightBestToggle.isOn && bestNPC != null)
        {
            HighlightNPC(bestNPC);
        }

        // Mostrar solo los mejores si está activado
        if (showTopNPCsToggle.isOn)
        {
            ShowOnlyTopNPCs();
        }
    }

    private void HighlightNPC(NPCController npc)
    {
        if (npc == null || highlightMaterial == null) return;

        Renderer renderer = npc.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = highlightMaterial;
        }
    }

    private void RestoreNPCMaterials()
    {
        if (bestNPC == null) return;

        Renderer renderer = bestNPC.GetComponent<Renderer>();
        if (renderer != null && originalMaterials.Count > 0)
        {
            renderer.material = originalMaterials[0];
        }
    }

    private void ShowOnlyTopNPCs()
    {
        if (geneticAlgorithm == null || geneticAlgorithm.population == null || topNPCs == null) return;

        // Ocultar todos los NPCs excepto los mejores
        hiddenNPCs.Clear();

        foreach (var npc in geneticAlgorithm.population)
        {
            if (npc != null && !topNPCs.Contains(npc))
            {
                npc.gameObject.SetActive(false);
                hiddenNPCs.Add(npc);
            }
        }
    }

    private void ShowAllNPCs()
    {
        foreach (var npc in hiddenNPCs)
        {
            if (npc != null)
            {
                npc.gameObject.SetActive(true);
            }
        }

        hiddenNPCs.Clear();
    }

    private void UpdateNPCVisibility()
    {
        if (showTopNPCsToggle.isOn)
        {
            ShowOnlyTopNPCs();
        }
        else
        {
            ShowAllNPCs();
        }

        if (highlightBestToggle.isOn && bestNPC != null)
        {
            HighlightNPC(bestNPC);
        }
        else
        {
            RestoreNPCMaterials();
        }
    }

    private void PauseReplay()
    {
        Time.timeScale = 0f;
        pauseButton.interactable = false;
        playButton.interactable = true;
    }

    private void StopReplay()
    {
        // Restaurar velocidades originales
        foreach (var pair in originalSpeeds)
        {
            if (pair.Key != null)
            {
                pair.Key.moveSpeed = pair.Value;
            }
        }

        // Restaurar materiales
        RestoreNPCMaterials();

        // Mostrar todos los NPCs
        ShowAllNPCs();

        // Restaurar cámara
        if (followCameraToggle.isOn)
        {
            ToggleFollowCamera(false);
        }

        // Restaurar time scale
        Time.timeScale = 1f;

        // Actualizar estado
        isReplaying = false;
        replayTime = 0f;

        // Actualizar controles
        playButton.interactable = true;
        pauseButton.interactable = false;
        stopButton.interactable = false;
        generationDropdown.interactable = true;

        if (progressSlider != null)
        {
            progressSlider.value = 0f;
        }

        // Reanudar algoritmo genético
        if (geneticAlgorithm != null)
        {
            geneticAlgorithm.isPaused = false;
        }
    }

    private void SetReplaySpeed(float speed)
    {
        if (isReplaying && !pauseButton.interactable)
        {
            // Si estamos en pausa, no cambiar el timeScale
            return;
        }

        Time.timeScale = speed;
    }

    private void ToggleFollowCamera(bool follow)
    {
        if (followCamera == null || mainCamera == null) return;

        if (follow)
        {
            // Activar cámara de seguimiento
            followCamera.SetActive(true);
            mainCamera.gameObject.SetActive(false);
        }
        else
        {
            // Restaurar cámara principal
            followCamera.SetActive(false);
            mainCamera.gameObject.SetActive(true);

            // Restaurar posición original
            if (originalCameraTransform != null)
            {
                mainCamera.transform.position = originalCameraTransform.position;
                mainCamera.transform.rotation = originalCameraTransform.rotation;
            }
        }
    }

    private void ToggleHighlightBest(bool highlight)
    {
        if (highlight && bestNPC != null)
        {
            HighlightNPC(bestNPC);
        }
        else
        {
            RestoreNPCMaterials();
        }
    }

    private void ToggleShowTopNPCs(bool show)
    {
        UpdateNPCVisibility();
    }

    private void SetTopNPCCount(float count)
    {
        topNPCCount = Mathf.RoundToInt(count);

        // Actualizar el array de mejores NPCs
        if (geneticAlgorithm != null && geneticAlgorithm.population != null && geneticAlgorithm.population.Count > 0)
        {
            topNPCs = geneticAlgorithm.population
                .OrderByDescending(npc => npc.fitness)
                .Take(topNPCCount)
                .ToArray();

            // Actualizar visibilidad
            if (showTopNPCsToggle.isOn)
            {
                ShowAllNPCs(); // Primero mostrar todos para luego ocultar los correctos
                ShowOnlyTopNPCs();
            }
        }
    }

    void Update()
    {
        if (!isReplaying) return;

        // Actualizar tiempo de reproducción
        replayTime += Time.unscaledDeltaTime;

        // Actualizar texto de tiempo
        if (timeInfoText != null)
        {
            int minutes = Mathf.FloorToInt(replayTime / 60);
            int seconds = Mathf.FloorToInt(replayTime % 60);
            timeInfoText.text = string.Format("Tiempo: {0:00}:{1:00}", minutes, seconds);
        }

        // Actualizar slider de progreso
        if (progressSlider != null)
        {
            progressSlider.value = Mathf.Clamp01(replayTime / maxReplayDuration);
        }

        // Actualizar estadísticas
        UpdateReplayStats();

        // Actualizar cámara de seguimiento
        UpdateFollowCamera();

        // Verificar si hemos llegado al final
        if (replayTime >= maxReplayDuration)
        {
            StopReplay();
        }
    }

    private void UpdateReplayStats()
    {
        if (replayStatsText == null || geneticAlgorithm == null) return;

        // Calcular estadísticas en tiempo real
        int aliveCount = 0;
        float bestCurrentFitness = 0f;
        NPCController currentBestNPC = null;

        foreach (var npc in geneticAlgorithm.population)
        {
            if (npc == null) continue;

            if (!npc.isDead)
            {
                aliveCount++;
            }

            if (npc.fitness > bestCurrentFitness)
            {
                bestCurrentFitness = npc.fitness;
                currentBestNPC = npc;
            }
        }

        // Si ha cambiado el mejor NPC y tenemos seguimiento activado
        if (currentBestNPC != null && currentBestNPC != bestNPC)
        {
            if (highlightBestToggle.isOn)
            {
                RestoreNPCMaterials();
                bestNPC = currentBestNPC;
                HighlightNPC(bestNPC);

                // Actualizar los mejores NPCs
                topNPCs = geneticAlgorithm.population
                    .OrderByDescending(npc => npc.fitness)
                    .Take(topNPCCount)
                    .ToArray();

                if (showTopNPCsToggle.isOn)
                {
                    ShowAllNPCs();
                    ShowOnlyTopNPCs();
                }
            }
        }

        // Actualizar texto de estadísticas
        string statsText = $"NPCs vivos: {aliveCount}/{geneticAlgorithm.population.Count}\n" +
                         $"Mejor fitness actual: {bestCurrentFitness:F2}\n";

        if (bestNPC != null)
        {
            statsText += $"Distancia recorrida: {bestNPC.totalDistance:F2}\n";
            statsText += $"Saltos realizados: {bestNPC.successfulJumps}\n";
            statsText += $"Saltos necesarios: {bestNPC.necessaryJumps}\n";
            statsText += $"Saltos innecesarios: {bestNPC.unnecessaryJumps}\n";

            float jumpEfficiency = bestNPC.necessaryJumps + bestNPC.unnecessaryJumps > 0 ?
                (float)bestNPC.necessaryJumps / (bestNPC.necessaryJumps + bestNPC.unnecessaryJumps) * 100f : 0f;

            statsText += $"Eficiencia de salto: {jumpEfficiency:F1}%\n";
            statsText += $"Energía: {bestNPC.energy:F1}%";
        }

        replayStatsText.text = statsText;
    }

    private void UpdateFollowCamera()
    {
        if (!followCameraToggle.isOn || followCam == null || bestNPC == null) return;

        // Calcular posición de la cámara
        Vector3 targetPosition = bestNPC.transform.position;

        // Posición detrás del NPC
        Vector3 offset = -bestNPC.transform.forward * cameraFollowDistance;
        offset.y = cameraFollowHeight;

        // Mover la cámara
        followCam.transform.position = targetPosition + offset;

        // Orientar la cámara hacia el NPC
        followCam.transform.LookAt(targetPosition + Vector3.up);
    }
}