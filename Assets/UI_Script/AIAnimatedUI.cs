using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Script centralizado que integra el sistema de animaciones UI con la interfaz de entrenamiento de IA.
/// Solo requiere arrastrar referencias en el Inspector.
/// </summary>
public class AIAnimatedUI : MonoBehaviour
{
    [Header("Referencias a Sistemas")]
    [Tooltip("Referencia al algoritmo genético")]
    public NPCGeneticAlgorithm geneticAlgorithm;

    [Tooltip("Referencia al sistema de guardado")]
    public AITrainingSaver trainingSaver;

    [Tooltip("Referencia al sistema de carga")]
    public AITrainingLoader trainingLoader;

    [Header("Paneles Principales")]
    [Tooltip("Panel principal que contiene toda la UI")]
    public GameObject mainPanel;

    [Tooltip("Panel de estadísticas")]
    public GameObject statisticsPanel;

    [Tooltip("Panel de controles")]
    public GameObject controlsPanel;

    [Tooltip("Panel de configuración")]
    public GameObject settingsPanel;

    [Header("Animaciones")]
    [Tooltip("Prefijo para IDs de animación (para evitar conflictos)")]
    public string animationIDPrefix = "AI_";

    [Header("UI de Información")]
    public TextMeshProUGUI generationText;
    public TextMeshProUGUI bestFitnessText;
    public TextMeshProUGUI avgFitnessText;
    public TextMeshProUGUI worstFitnessText;
    public TextMeshProUGUI populationCountText;
    public TextMeshProUGUI timeScaleText;

    [Header("UI de Estadísticas de Salto")]
    public TextMeshProUGUI necessaryJumpsText;
    public TextMeshProUGUI unnecessaryJumpsText;
    public TextMeshProUGUI jumpEfficiencyText;
    public TextMeshProUGUI energyText;

    [Header("UI de Control")]
    public Button saveButton;
    public Button loadButton;
    public TMP_InputField saveNameInput;
    public Slider timeScaleSlider;
    public Button pauseResumeButton;
    public TextMeshProUGUI pauseResumeText;
    public Button restartButton;

    [Header("Control de Bloqueo de Comportamientos")]
    public GameObject lockControlsPanel;
    public Toggle forwardMovementLockToggle;
    public Toggle leftTurnLockToggle;
    public Toggle rightTurnLockToggle;
    public Toggle jumpLockToggle;

    [Header("Configuración de Reutilización")]
    public Toggle reuseNPCsToggle;
    public Toggle continueFromCurrentPositionToggle;
    public Slider immunityDurationSlider;
    public TextMeshProUGUI immunityDurationText;

    // Variables internas
    private bool isPaused = false;
    private float previousTimeScale = 1f;

    // Mapeo de comportamientos a índices de salida
    private Dictionary<string, int> behaviorToOutputIndex = new Dictionary<string, int>()
    {
        { "Forward", 0 },   // Movimiento hacia adelante
        { "LeftTurn", 1 },  // Giro izquierda
        { "RightTurn", 2 }, // Giro derecha
        { "Jump", 3 }       // Salto
    };

    private void Start()
    {
        // Búsqueda de componentes si no están asignados
        FindMissingComponents();

        // Configuración inicial de la UI
        SetupUI();

        // Configuración de animaciones
        SetupAnimations();

        // Animación inicial
        PlayOpenAnimation();
    }

    private void FindMissingComponents()
    {
        // Buscar componentes si no están asignados
        if (geneticAlgorithm == null)
            geneticAlgorithm = FindObjectOfType<NPCGeneticAlgorithm>();

        if (trainingSaver == null && GetComponent<AITrainingSaver>() != null)
            trainingSaver = GetComponent<AITrainingSaver>();

        if (trainingLoader == null && GetComponent<AITrainingLoader>() != null)
            trainingLoader = GetComponent<AITrainingLoader>();
    }

    private void SetupUI()
    {
        // Configurar event listeners para botones
        if (saveButton != null)
            saveButton.onClick.AddListener(SaveTraining);

        if (loadButton != null)
            loadButton.onClick.AddListener(LoadTraining);

        if (pauseResumeButton != null)
            pauseResumeButton.onClick.AddListener(TogglePause);

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartTraining);

        // Configuración del slider de velocidad
        if (timeScaleSlider != null)
        {
            timeScaleSlider.value = Time.timeScale;
            timeScaleSlider.onValueChanged.AddListener(ChangeTimeScale);
        }

        // Configuración de toggles de bloqueo
        if (forwardMovementLockToggle != null)
            forwardMovementLockToggle.onValueChanged.AddListener(isOn => SetBehaviorLock("Forward", isOn));

        if (leftTurnLockToggle != null)
            leftTurnLockToggle.onValueChanged.AddListener(isOn => SetBehaviorLock("LeftTurn", isOn));

        if (rightTurnLockToggle != null)
            rightTurnLockToggle.onValueChanged.AddListener(isOn => SetBehaviorLock("RightTurn", isOn));

        if (jumpLockToggle != null)
            jumpLockToggle.onValueChanged.AddListener(isOn => SetBehaviorLock("Jump", isOn));

        // Configuración de toggles de reutilización
        if (reuseNPCsToggle != null && geneticAlgorithm != null)
        {
            reuseNPCsToggle.isOn = geneticAlgorithm.reuseNPCs;
            reuseNPCsToggle.onValueChanged.AddListener(OnReuseNPCsToggleChanged);
        }

        if (continueFromCurrentPositionToggle != null && geneticAlgorithm != null)
        {
            continueFromCurrentPositionToggle.isOn = geneticAlgorithm.continueFromCurrentPosition;
            continueFromCurrentPositionToggle.onValueChanged.AddListener(OnContinuePositionToggleChanged);
        }

        if (immunityDurationSlider != null && geneticAlgorithm != null)
        {
            immunityDurationSlider.value = geneticAlgorithm.immunityDuration;
            immunityDurationSlider.onValueChanged.AddListener(OnImmunityDurationChanged);
            UpdateImmunityDurationText();
        }
    }

    private void SetupAnimations()
    {
        // Configuración de animaciones para el panel principal
        if (mainPanel != null)
        {
            // Fade in para el panel principal
            UIFadeAnimation mainFade = mainPanel.AddComponent<UIFadeAnimation>();
            mainFade.animationID = animationIDPrefix + "MainPanelFade";
            mainFade.duration = 0.5f;
            mainFade.fromAlpha = 0f;
            mainFade.toAlpha = 1f;
            mainFade.affectChildren = true;

            // Animación de escala para el panel principal
            UIScaleAnimation mainScale = mainPanel.AddComponent<UIScaleAnimation>();
            mainScale.animationID = animationIDPrefix + "MainPanelScale";
            mainScale.duration = 0.4f;
            mainScale.fromScale = new Vector3(0.9f, 0.9f, 1f);
            mainScale.toScale = Vector3.one;
            mainScale.useBounce = true;
            mainScale.bounceIntensity = 0.1f;

            // Grupo de animación para la entrada del panel principal
            UIAnimationGroup mainGroup = mainPanel.AddComponent<UIAnimationGroup>();
            mainGroup.groupID = animationIDPrefix + "MainPanelOpen";
            mainGroup.animations.Add(mainFade);
            mainGroup.animations.Add(mainScale);
        }

        // Configuración de animaciones para los subpaneles
        SetupPanelAnimation(statisticsPanel, "Stats", 0.1f);
        SetupPanelAnimation(controlsPanel, "Controls", 0.2f);
        SetupPanelAnimation(settingsPanel, "Settings", 0.3f);

        // Configuración de animación para botones
        SetupButtonAnimations();
    }

    private void SetupPanelAnimation(GameObject panel, string panelName, float delay)
    {
        if (panel == null) return;

        // Animación de movimiento deslizante
        UIMoveAnimation moveAnim = panel.AddComponent<UIMoveAnimation>();
        moveAnim.animationID = animationIDPrefix + panelName + "PanelSlide";
        moveAnim.duration = 0.4f;
        moveAnim.delay = delay;
        moveAnim.fromPosition = new Vector3(50f, 0f, 0f);
        moveAnim.toPosition = Vector3.zero;
        moveAnim.useLocalPosition = true;

        // Animación de fade
        UIFadeAnimation fadeAnim = panel.AddComponent<UIFadeAnimation>();
        fadeAnim.animationID = animationIDPrefix + panelName + "PanelFade";
        fadeAnim.duration = 0.4f;
        fadeAnim.delay = delay;
        fadeAnim.fromAlpha = 0f;
        fadeAnim.toAlpha = 1f;

        // Grupo de animación
        UIAnimationGroup panelGroup = panel.AddComponent<UIAnimationGroup>();
        panelGroup.groupID = animationIDPrefix + panelName + "PanelOpen";
        panelGroup.animations.Add(moveAnim);
        panelGroup.animations.Add(fadeAnim);
    }

    private void SetupButtonAnimations()
    {
        // Para cada botón importante, añadir animación de botón
        AddButtonAnimation(saveButton);
        AddButtonAnimation(loadButton);
        AddButtonAnimation(pauseResumeButton);
        AddButtonAnimation(restartButton);
    }

    private void AddButtonAnimation(Button button)
    {
        if (button == null) return;

        UIButtonAnimation buttonAnim = button.gameObject.AddComponent<UIButtonAnimation>();
        buttonAnim.normalScale = Vector3.one;
        buttonAnim.hoverScale = new Vector3(1.05f, 1.05f, 1.05f);
        buttonAnim.clickScale = new Vector3(0.95f, 0.95f, 0.95f);
        buttonAnim.transitionSpeed = 10f;
    }

    private void PlayOpenAnimation()
    {
        // Reproducir la animación principal de apertura
        UIAnimationManager.Instance.PlayAnimation(animationIDPrefix + "MainPanelOpen");

        // Reproducir animaciones de subpaneles en secuencia
        UIAnimationManager.Instance.PlaySequence(
            animationIDPrefix + "StatsPanelOpen",
            animationIDPrefix + "ControlsPanelOpen",
            animationIDPrefix + "SettingsPanelOpen"
        );
    }

    private void Update()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (geneticAlgorithm == null) return;

        // Actualiza textos de información
        if (generationText != null)
            generationText.text = $"Generación: {geneticAlgorithm.generation}";

        if (timeScaleText != null)
            timeScaleText.text = $"Velocidad: x{Time.timeScale:F1}";

        if (pauseResumeText != null)
            pauseResumeText.text = isPaused ? "Reanudar" : "Pausar";

        // Si no hay población, salimos
        if (geneticAlgorithm.population == null || geneticAlgorithm.population.Count == 0)
            return;

        // Cálculo de estadísticas
        float bestFitness = float.MinValue;
        float worstFitness = float.MaxValue;
        float totalFitness = 0;
        int aliveCount = 0;

        // Variables para estadísticas de salto
        int totalNecessaryJumps = 0;
        int totalUnnecessaryJumps = 0;
        float totalEnergy = 0;

        // Recopilación de datos de NPCs
        foreach (var npc in geneticAlgorithm.population)
        {
            if (npc == null) continue;

            // Actualiza fitness
            if (npc.fitness > bestFitness)
                bestFitness = npc.fitness;

            if (npc.fitness < worstFitness)
                worstFitness = npc.fitness;

            totalFitness += npc.fitness;

            // Cuenta NPCs vivos
            if (!npc.isDead)
                aliveCount++;

            // Estadísticas de salto
            totalNecessaryJumps += npc.necessaryJumps;
            totalUnnecessaryJumps += npc.unnecessaryJumps;
            totalEnergy += npc.energy;
        }

        // Cálculo de promedios
        float avgFitness = totalFitness / geneticAlgorithm.population.Count;
        float avgNecessaryJumps = (float)totalNecessaryJumps / geneticAlgorithm.population.Count;
        float avgUnnecessaryJumps = (float)totalUnnecessaryJumps / geneticAlgorithm.population.Count;
        float avgEnergy = totalEnergy / geneticAlgorithm.population.Count;

        // Cálculo de eficiencia de salto
        float totalJumps = totalNecessaryJumps + totalUnnecessaryJumps;
        float jumpEfficiency = (totalJumps > 0) ? (float)totalNecessaryJumps / totalJumps * 100f : 0f;

        // Actualización de textos de estadísticas
        if (bestFitnessText != null)
            bestFitnessText.text = $"Mejor fitness: {bestFitness:F2}";

        if (avgFitnessText != null)
            avgFitnessText.text = $"Fitness promedio: {avgFitness:F2}";

        if (worstFitnessText != null)
            worstFitnessText.text = $"Peor fitness: {worstFitness:F2}";

        if (populationCountText != null)
            populationCountText.text = $"Población: {geneticAlgorithm.population.Count} (Vivos: {aliveCount})";

        // Actualización de textos de salto
        if (necessaryJumpsText != null)
            necessaryJumpsText.text = $"Saltos necesarios: {totalNecessaryJumps} (Prom: {avgNecessaryJumps:F1})";

        if (unnecessaryJumpsText != null)
            unnecessaryJumpsText.text = $"Saltos innecesarios: {totalUnnecessaryJumps} (Prom: {avgUnnecessaryJumps:F1})";

        if (jumpEfficiencyText != null)
            jumpEfficiencyText.text = $"Eficiencia de salto: {jumpEfficiency:F1}%";

        if (energyText != null)
            energyText.text = $"Energía promedio: {avgEnergy:F1}%";
    }

    // Callbacks para controles de UI

    public void SaveTraining()
    {
        if (trainingSaver == null)
        {
            Debug.LogWarning("AITrainingSaver no está asignado");
            return;
        }

        // Reproducir animación del botón
        PlayButtonClickAnimation(saveButton);

        // Determinar nombre del archivo
        string saveName = "Training";
        if (saveNameInput != null && !string.IsNullOrEmpty(saveNameInput.text))
        {
            saveName = saveNameInput.text;
        }
        else
        {
            saveName = $"Generation_{geneticAlgorithm.generation}";
        }

        // Guardar entrenamiento
        trainingSaver.SaveTraining(saveName);
    }

    public void LoadTraining()
    {
        if (trainingLoader == null)
        {
            Debug.LogWarning("AITrainingLoader no está asignado");
            return;
        }

        // Reproducir animación del botón
        PlayButtonClickAnimation(loadButton);

        // Cargar entrenamiento seleccionado
        trainingLoader.LoadSelectedTraining();

        // Sincronizar UI con el estado de bloqueo cargado
        SyncUIWithLockStatus();
    }

    public void TogglePause()
    {
        // Reproducir animación del botón
        PlayButtonClickAnimation(pauseResumeButton);

        if (isPaused)
        {
            // Reanudar simulación
            Time.timeScale = previousTimeScale;
            isPaused = false;
        }
        else
        {
            // Pausar simulación
            PauseBeforeLoad();
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            isPaused = true;
        }

        // Actualizar texto del botón
        if (pauseResumeText != null)
            pauseResumeText.text = isPaused ? "Reanudar" : "Pausar";
    }

    public void ChangeTimeScale(float newTimeScale)
    {
        // Solo cambiar velocidad si no está pausado
        if (!isPaused)
        {
            Time.timeScale = newTimeScale;
            previousTimeScale = newTimeScale;
        }
    }

    public void RestartTraining()
    {
        if (geneticAlgorithm == null) return;

        // Reproducir animación del botón
        PlayButtonClickAnimation(restartButton);

        // Animar salida y reiniciar
        UIAnimationManager.Instance.PlayAnimation(animationIDPrefix + "MainPanelFade");

        // Esperar a que termine la animación para reiniciar
        Invoke("DoRestartTraining", 0.5f);
    }

    private void DoRestartTraining()
    {
        // Destruir población actual
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

        // Reiniciar contador de generación
        geneticAlgorithm.generation = 1;

        // Inicializar nueva población
        geneticAlgorithm.InitializePopulation();

        // Animar entrada
        PlayOpenAnimation();
    }

    public void SetBehaviorLock(string behaviorName, bool locked)
    {
        if (!behaviorToOutputIndex.ContainsKey(behaviorName) ||
            geneticAlgorithm == null ||
            geneticAlgorithm.population == null)
            return;

        int outputIndex = behaviorToOutputIndex[behaviorName];

        // Aplicar a todos los NPCs
        foreach (var npc in geneticAlgorithm.population)
        {
            if (npc != null && npc.brain != null)
            {
                npc.brain.SetOutputLockStatus(outputIndex, locked);
            }
        }

        Debug.Log($"Comportamiento '{behaviorName}' {(locked ? "bloqueado" : "desbloqueado")} para entrenamiento");
        UpdateLockStatusVisual();
    }

    public void UpdateLockStatusVisual()
    {
        // Actualizar visualización de NPCs basado en bloqueos
        if (geneticAlgorithm != null && geneticAlgorithm.population != null)
        {
            foreach (var npc in geneticAlgorithm.population)
            {
                if (npc != null)
                {
                    npc.UpdateVisualBasedOnLocks();
                }
            }
        }
    }

    public void SyncUIWithLockStatus()
    {
        if (geneticAlgorithm == null ||
            geneticAlgorithm.population == null ||
            geneticAlgorithm.population.Count == 0)
            return;

        // Usar el primer NPC como referencia
        var firstNPC = geneticAlgorithm.population[0];
        if (firstNPC == null || firstNPC.brain == null)
            return;

        bool[] lockStatus = firstNPC.brain.GetOutputLockStatus();

        // Evitar eventos en cascada
        if (forwardMovementLockToggle != null && lockStatus.Length > 0)
        {
            forwardMovementLockToggle.SetIsOnWithoutNotify(lockStatus[0]);
        }

        if (leftTurnLockToggle != null && lockStatus.Length > 1)
        {
            leftTurnLockToggle.SetIsOnWithoutNotify(lockStatus[1]);
        }

        if (rightTurnLockToggle != null && lockStatus.Length > 2)
        {
            rightTurnLockToggle.SetIsOnWithoutNotify(lockStatus[2]);
        }

        if (jumpLockToggle != null && lockStatus.Length > 3)
        {
            jumpLockToggle.SetIsOnWithoutNotify(lockStatus[3]);
        }
    }

    void PauseBeforeLoad()
    {
        if (geneticAlgorithm != null)
        {
            geneticAlgorithm.isPaused = true;

            // Refrescar lista de archivos guardados
            if (trainingLoader != null)
            {
                trainingLoader.RefreshSaveFilesList();
            }
        }
    }

    void OnReuseNPCsToggleChanged(bool value)
    {
        if (geneticAlgorithm != null)
        {
            geneticAlgorithm.reuseNPCs = value;
        }
    }

    void OnContinuePositionToggleChanged(bool value)
    {
        if (geneticAlgorithm != null)
        {
            geneticAlgorithm.continueFromCurrentPosition = value;
        }
    }

    void OnImmunityDurationChanged(float value)
    {
        if (geneticAlgorithm != null)
        {
            geneticAlgorithm.immunityDuration = value;
            UpdateImmunityDurationText();
        }
    }

    void UpdateImmunityDurationText()
    {
        if (immunityDurationText != null && geneticAlgorithm != null)
        {
            immunityDurationText.text = $"Inmunidad: {geneticAlgorithm.immunityDuration:F1}s";
        }
    }

    void PlayButtonClickAnimation(Button button)
    {
        if (button == null) return;

        // Añadir efecto de punch al botón
        GameObject buttonGO = button.gameObject;
        UIPunchAnimation punchAnim = buttonGO.GetComponent<UIPunchAnimation>();

        if (punchAnim == null)
        {
            punchAnim = buttonGO.AddComponent<UIPunchAnimation>();
            punchAnim.duration = 0.3f;
            punchAnim.punchDirection = new Vector3(0, -5, 0);
            punchAnim.oscillations = 2;
            punchAnim.elasticity = 0.3f;
            punchAnim.affectPosition = true;
        }

        punchAnim.Play();
    }

    // Métodos para cambiar paneles con animaciones

    public void ShowPanel(GameObject panel)
    {
        if (panel == null) return;

        // Nombre del panel para animación
        string panelName = "";

        if (panel == statisticsPanel) panelName = "Stats";
        else if (panel == controlsPanel) panelName = "Controls";
        else if (panel == settingsPanel) panelName = "Settings";
        else return;

        // Mostrar panel con animación
        panel.SetActive(true);
        UIAnimationManager.Instance.PlayAnimation(animationIDPrefix + panelName + "PanelOpen");
    }

    public void HidePanel(GameObject panel)
    {
        if (panel == null) return;

        // Nombre del panel para animación
        string panelName = "";

        if (panel == statisticsPanel) panelName = "Stats";
        else if (panel == controlsPanel) panelName = "Controls";
        else if (panel == settingsPanel) panelName = "Settings";
        else return;

        // Ocultar panel con animación
        UIFadeAnimation fadeAnimation = panel.GetComponent<UIFadeAnimation>();
        if (fadeAnimation != null)
        {
            // Guardar referencia para usarla en las funciones de invocación
            GameObject panelRef = panel;
            UIFadeAnimation fadeAnimRef = fadeAnimation;

            // Invertir animación para salida
            float tempFrom = fadeAnimation.fromAlpha;
            float tempTo = fadeAnimation.toAlpha;
            fadeAnimation.fromAlpha = tempTo;
            fadeAnimation.toAlpha = tempFrom;

            fadeAnimation.Play();

            // Ocultar después de la animación
            StartCoroutine(HidePanelDelayed(panelRef, fadeAnimRef.duration, tempFrom, tempTo));
        }
    }

    private IEnumerator HidePanelDelayed(GameObject panel, float delay, float originalFrom, float originalTo)
    {
        // Esperar a que termine la animación
        yield return new WaitForSeconds(delay);

        // Ocultar el panel
        panel.SetActive(false);

        // Restaurar valores originales
        UIFadeAnimation fadeAnim = panel.GetComponent<UIFadeAnimation>();
        if (fadeAnim != null)
        {
            fadeAnim.fromAlpha = originalFrom;
            fadeAnim.toAlpha = originalTo;
        }
    }


    // Método para animar toda la UI (entrada/salida)

    public void AnimateUI(bool show)
    {
        if (show)
        {
            // Mostrar UI con animación
            mainPanel.SetActive(true);
            PlayOpenAnimation();
        }
        else
        {
            // Ocultar UI con animación
            UIFadeAnimation fadeAnimation = mainPanel.GetComponent<UIFadeAnimation>();
            if (fadeAnimation != null)
            {
                // Guardar referencia
                GameObject panelRef = mainPanel;
                UIFadeAnimation fadeAnimRef = fadeAnimation;

                // Invertir animación para salida
                float tempFrom = fadeAnimation.fromAlpha;
                float tempTo = fadeAnimation.toAlpha;
                fadeAnimation.fromAlpha = tempTo;
                fadeAnimation.toAlpha = tempFrom;

                fadeAnimation.Play();

                // Ocultar y restaurar usando corrutina
                StartCoroutine(HideMainPanelDelayed(panelRef, fadeAnimRef.duration, tempFrom, tempTo));
            }
        }
    }

    private IEnumerator HideMainPanelDelayed(GameObject panel, float delay, float originalFrom, float originalTo)
    {
        // Esperar a que termine la animación
        yield return new WaitForSeconds(delay);

        // Ocultar el panel
        panel.SetActive(false);

        // Restaurar valores originales
        UIFadeAnimation fadeAnim = panel.GetComponent<UIFadeAnimation>();
        if (fadeAnim != null)
        {
            fadeAnim.fromAlpha = originalFrom;
            fadeAnim.toAlpha = originalTo;
        }
    }
}