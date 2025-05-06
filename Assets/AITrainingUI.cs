using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class AITrainingUI : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Referencia al algoritmo genético")]
    public NPCGeneticAlgorithm geneticAlgorithm;

    [Tooltip("Referencia al sistema de guardado")]
    public AITrainingSaver trainingSaver;

    [Tooltip("Referencia al sistema de carga")]
    public AITrainingLoader trainingLoader;

    [Header("UI de Información")]
    [Tooltip("Texto que muestra la generación actual")]
    public TextMeshProUGUI generationText;

    [Tooltip("Texto que muestra el mejor fitness")]
    public TextMeshProUGUI bestFitnessText;

    [Tooltip("Texto que muestra el fitness promedio")]
    public TextMeshProUGUI avgFitnessText;

    [Tooltip("Texto que muestra el peor fitness")]
    public TextMeshProUGUI worstFitnessText;

    [Tooltip("Texto que muestra información de la población")]
    public TextMeshProUGUI populationCountText;

    [Tooltip("Texto que muestra la velocidad de simulación")]
    public TextMeshProUGUI timeScaleText;

    [Header("UI de Control")]
    [Tooltip("Botón para guardar entrenamiento")]
    public Button saveButton;

    [Tooltip("Botón para cargar entrenamiento")]
    public Button loadButton;

    [Tooltip("Campo de texto para el nombre de guardado")]
    public TMP_InputField saveNameInput;

    [Tooltip("Slider para ajustar la velocidad de simulación")]
    public Slider timeScaleSlider;

    [Tooltip("Botón para pausar/reanudar la simulación")]
    public Button pauseResumeButton;

    [Tooltip("Texto del botón de pausa/reanudación")]
    public TextMeshProUGUI pauseResumeText;

    [Tooltip("Botón para reiniciar el entrenamiento")]
    public Button restartButton;

    [Header("UI de Estadísticas de Salto")]
    [Tooltip("Texto que muestra los saltos necesarios realizados")]
    public TextMeshProUGUI necessaryJumpsText;

    [Tooltip("Texto que muestra los saltos innecesarios realizados")]
    public TextMeshProUGUI unnecessaryJumpsText;

    [Tooltip("Texto que muestra la ratio de efectividad de saltos")]
    public TextMeshProUGUI jumpEfficiencyText;

    [Tooltip("Texto que muestra la energía promedio de los NPCs")]
    public TextMeshProUGUI energyText;

    // Variables de estado
    private bool isPaused = false;
    private float previousTimeScale = 1f;


    void Start()
    {
        // Buscamos componentes si no están asignados
        if (geneticAlgorithm == null)
            geneticAlgorithm = FindObjectOfType<NPCGeneticAlgorithm>();

        if (trainingSaver == null && GetComponent<AITrainingSaver>() != null)
            trainingSaver = GetComponent<AITrainingSaver>();

        if (trainingLoader == null && GetComponent<AITrainingLoader>() != null)
            trainingLoader = GetComponent<AITrainingLoader>();

        // Configuramos listeners para botones
        if (saveButton != null)
            saveButton.onClick.AddListener(SaveTraining);

        if (loadButton != null)
            loadButton.onClick.AddListener(LoadTraining);

        if (pauseResumeButton != null)
            pauseResumeButton.onClick.AddListener(TogglePause);

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartTraining);

        // Configuramos el slider de velocidad
        if (timeScaleSlider != null)
        {
            timeScaleSlider.value = Time.timeScale; // Valor inicial igual a la velocidad actual
            timeScaleSlider.onValueChanged.AddListener(ChangeTimeScale);
        }
    }

    /// <summary>
    /// Se ejecuta cada frame. Actualiza la información mostrada en la UI.
    /// </summary>
    void Update()
    {
        UpdateUI();
    }

    /// <summary>
    /// Actualiza todos los elementos de la interfaz con la información actual.
    /// </summary>
    void UpdateUI()
    {
        if (geneticAlgorithm == null) return;

        // Actualizamos el texto de generación
        if (generationText != null)
            generationText.text = $"Generación: {geneticAlgorithm.generation}";

        // Calculamos estadísticas en tiempo real si hay población
        if (geneticAlgorithm.population != null && geneticAlgorithm.population.Count > 0)
        {
            float bestFitness = float.MinValue;
            float worstFitness = float.MaxValue;
            float totalFitness = 0;
            int aliveCount = 0; // Contador de NPCs vivos

            // Variables para estadísticas de salto
            int totalNecessaryJumps = 0;
            int totalUnnecessaryJumps = 0;
            float totalEnergy = 0;

            // Recorremos todos los NPCs para calcular estadísticas
            foreach (var npc in geneticAlgorithm.population)
            {
                if (npc == null) continue;

                // Actualizamos el mejor fitness
                if (npc.fitness > bestFitness)
                    bestFitness = npc.fitness;

                // Actualizamos el peor fitness
                if (npc.fitness < worstFitness)
                    worstFitness = npc.fitness;

                // Sumamos para calcular el promedio
                totalFitness += npc.fitness;

                // Contamos los NPCs vivos
                if (!npc.isDead)
                    aliveCount++;

                // Recopilamos estadísticas de salto
                totalNecessaryJumps += npc.necessaryJumps;
                totalUnnecessaryJumps += npc.unnecessaryJumps;
                totalEnergy += npc.energy;
            }

            // Calculamos el fitness promedio
            float avgFitness = totalFitness / geneticAlgorithm.population.Count;

            // Calculamos promedios para estadísticas de salto
            float avgNecessaryJumps = (float)totalNecessaryJumps / geneticAlgorithm.population.Count;
            float avgUnnecessaryJumps = (float)totalUnnecessaryJumps / geneticAlgorithm.population.Count;
            float avgEnergy = totalEnergy / geneticAlgorithm.population.Count;

            // Calculamos la eficiencia de salto (ratio necesarios / total)
            float totalJumps = totalNecessaryJumps + totalUnnecessaryJumps;
            float jumpEfficiency = (totalJumps > 0) ? (float)totalNecessaryJumps / totalJumps * 100f : 0f;

            // Actualizamos los textos con la información calculada
            if (bestFitnessText != null)
                bestFitnessText.text = $"Mejor fitness: {bestFitness:F2}";

            if (avgFitnessText != null)
                avgFitnessText.text = $"Fitness promedio: {avgFitness:F2}";

            if (worstFitnessText != null)
                worstFitnessText.text = $"Peor fitness: {worstFitness:F2}";

            if (populationCountText != null)
                populationCountText.text = $"Población: {geneticAlgorithm.population.Count} (Vivos: {aliveCount})";

            // Actualizamos los nuevos textos de estadísticas de salto
            if (necessaryJumpsText != null)
                necessaryJumpsText.text = $"Saltos necesarios: {totalNecessaryJumps} (Prom: {avgNecessaryJumps:F1})";

            if (unnecessaryJumpsText != null)
                unnecessaryJumpsText.text = $"Saltos innecesarios: {totalUnnecessaryJumps} (Prom: {avgUnnecessaryJumps:F1})";

            if (jumpEfficiencyText != null)
                jumpEfficiencyText.text = $"Eficiencia de salto: {jumpEfficiency:F1}%";

            if (energyText != null)
                energyText.text = $"Energía promedio: {avgEnergy:F1}%";
        }

        // Actualizamos información de la velocidad de simulación
        if (timeScaleText != null)
            timeScaleText.text = $"Velocidad: x{Time.timeScale:F1}";

        // Actualizamos el texto del botón de pausa
        if (pauseResumeText != null)
            pauseResumeText.text = isPaused ? "Reanudar" : "Pausar";
    }
    // Guarda el estado actual del entrenamiento.
    public void SaveTraining()
    {
        if (trainingSaver == null)
        {
            Debug.LogWarning("AITrainingSaver no está asignado");
            return;
        }

        // Determinamos el nombre para el archivo
        string saveName = "Training";
        if (saveNameInput != null && !string.IsNullOrEmpty(saveNameInput.text))
        {
            // Usamos el nombre ingresado por el usuario
            saveName = saveNameInput.text;
        }
        else
        {
            // Generamos un nombre basado en la generación
            saveName = $"Generation_{geneticAlgorithm.generation}";
        }

        // Guardamos el entrenamiento
        trainingSaver.SaveTraining(saveName);
    }

   
    // Carga un entrenamiento guardado.
    public void LoadTraining()
    {
        if (trainingLoader == null)
        {
            Debug.LogWarning("AITrainingLoader no está asignado");
            return;
        }

        // Cargamos el entrenamiento seleccionado en el dropdown
        trainingLoader.LoadSelectedTraining();
    }

   
    // Alterna entre pausar y reanudar la simulación.   
    public void TogglePause()
    {
        if (isPaused)
        {
            // Reanudar la simulación
            Time.timeScale = previousTimeScale;
            isPaused = false;
        }
        else
        {
            // Pausar la simulación
            PauseBeforeLoad();
            previousTimeScale = Time.timeScale; // Guardamos la velocidad actual
            Time.timeScale = 0f; // Establecemos la velocidad a 0 (pausa)
            isPaused = true;
        }

        // Actualizamos el texto del botón
        if (pauseResumeText != null)
            pauseResumeText.text = isPaused ? "Reanudar" : "Pausar";
    }

    // Cambia la velocidad de la simulación.
    public void ChangeTimeScale(float newTimeScale)
    {
        // Solo cambiamos la velocidad si no está pausado
        if (!isPaused)
        {
            Time.timeScale = newTimeScale;
            previousTimeScale = newTimeScale; // Guardamos la velocidad para cuando se despausa
        }
    }

    // Reinicia el entrenamiento desde cero.
    public void RestartTraining()
    {
        if (geneticAlgorithm == null) return;

        // Destruimos la población actual
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

        // Reiniciamos el contador de generación
        geneticAlgorithm.generation = 1;

        // Inicializamos una nueva población
        geneticAlgorithm.InitializePopulation();
    }

    public void PauseBeforeLoad()
    {
        if (geneticAlgorithm != null)
        {
            geneticAlgorithm.isPaused = true;

            // Refresca la lista de archivos guardados
            if (trainingLoader != null)
            {
                trainingLoader.RefreshSaveFilesList();
            }
        }
    }
}