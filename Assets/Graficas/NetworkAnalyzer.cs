using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

public class NetworkAnalyzer : MonoBehaviour
{

    [Header("Referencias UI")]
    [Tooltip("Panel contenedor para la visualización de la red neuronal")]
    public RectTransform networkContainer;

    [Tooltip("Prefab para las neuronas")]
    public GameObject neuronPrefab;

    [Tooltip("Prefab para las conexiones entre neuronas")]
    public GameObject connectionPrefab;

    [Tooltip("Dropdown para seleccionar redes de la generación actual")]
    public TMP_Dropdown networkDropdown;

    [Tooltip("Texto para mostrar detalles de la red seleccionada")]
    public TextMeshProUGUI networkDetailsText;

    [Header("Configuración de Visualización")]
    [Tooltip("Distancia horizontal entre capas")]
    public float layerDistance = 150f;

    [Tooltip("Distancia vertical entre neuronas de la misma capa")]
    public float neuronDistance = 60f;

    [Tooltip("Escala para representar la importancia de las conexiones")]
    public float weightScale = 3f;

    [Tooltip("Color para pesos positivos")]
    public Color positiveWeightColor = Color.green;

    [Tooltip("Color para pesos negativos")]
    public Color negativeWeightColor = Color.red;

    [Tooltip("Color para neuronas bloqueadas")]
    public Color lockedNeuronColor = Color.yellow;

    [Header("Opciones de Análisis")]
    [Tooltip("Umbral para considerar un peso como significativo")]
    public float significantWeightThreshold = 0.2f;

    [Tooltip("Mostrar solo conexiones significativas")]
    public bool showOnlySignificantConnections = false;

    [Tooltip("Mostrar etiquetas en las neuronas")]
    public bool showNeuronLabels = true;

    // Referencias internas
    private TrainingVisualizer visualizer;
    private List<SerializedNetwork> currentNetworks = new List<SerializedNetwork>();
    private SerializedNetwork selectedNetwork;
    private Dictionary<string, List<GameObject>> networkElements = new Dictionary<string, List<GameObject>>();

    // Nombres para las neuronas de entrada y salida
    private readonly string[] inputNeuronNames = {
        "Sensor Izq", "Sensor Izq-Centro", "Sensor Centro",
        "Sensor Der-Centro", "Sensor Der", "Sensor Bajo",
        "Sensor Alto", "Constante"
    };

    private readonly string[] outputNeuronNames = {
        "Avanzar", "Girar Izq", "Girar Der", "Saltar"
    };

    void Start()
    {
        visualizer = FindObjectOfType<TrainingVisualizer>();

        if (networkDropdown != null)
        {
            networkDropdown.onValueChanged.AddListener(OnNetworkSelected);
        }
    }

    public void SetNetworks(List<SerializedNetwork> networks)
    {
        currentNetworks = networks;
        PopulateNetworkDropdown();

        if (currentNetworks.Count > 0)
        {
            networkDropdown.value = 0;
            OnNetworkSelected(0);
        }
        else
        {
            ClearNetworkVisualization();
            if (networkDetailsText != null)
            {
                networkDetailsText.text = "No hay redes disponibles para analizar.";
            }
        }
    }

    private void PopulateNetworkDropdown()
    {
        if (networkDropdown == null) return;

        networkDropdown.ClearOptions();
        List<string> options = new List<string>();

        for (int i = 0; i < currentNetworks.Count; i++)
        {
            SerializedNetwork network = currentNetworks[i];
            string networkType = network.npcType == NPCController.NPCType.Ally ? "Aliado" : "Enemigo";
            options.Add($"Red #{i + 1} ({networkType}) - Fitness: {network.fitness:F2}");
        }

        networkDropdown.AddOptions(options);
        networkDropdown.RefreshShownValue();
    }

    private void OnNetworkSelected(int index)
    {
        if (index < 0 || index >= currentNetworks.Count) return;

        selectedNetwork = currentNetworks[index];
        VisualizeNetwork(selectedNetwork);
        UpdateNetworkDetails();
    }

    private void VisualizeNetwork(SerializedNetwork network)
    {
        ClearNetworkVisualization();

        if (network == null || network.layers == null || network.flattenedWeights == null)
        {
            Debug.LogWarning("Red neuronal inválida o incompleta.");
            return;
        }

        List<GameObject> elements = new List<GameObject>();
        Dictionary<string, GameObject> neurons = new Dictionary<string, GameObject>();

        // Reconstruir estructura 3D de pesos desde la lista plana
        float[][][] weights = RebuildWeights(network.flattenedWeights, network.layers);

        if (weights == null)
        {
            Debug.LogError("Error al reconstruir pesos de la red.");
            return;
        }

        // Crear neuronas para cada capa
        for (int layer = 0; layer < network.layers.Length; layer++)
        {
            int neuronsInLayer = network.layers[layer];
            float layerWidth = layerDistance * layer;

            // Calcular offset vertical para centrar la capa
            float layerHeight = (neuronsInLayer - 1) * neuronDistance;
            float startY = -layerHeight / 2;

            for (int neuronIdx = 0; neuronIdx < neuronsInLayer; neuronIdx++)
            {
                float xPos = layerWidth;
                float yPos = startY + (neuronIdx * neuronDistance);

                GameObject neuronObj = Instantiate(neuronPrefab, networkContainer);
                neuronObj.transform.localPosition = new Vector3(xPos, yPos, 0);

                // Guardar referencia a la neurona
                string neuronId = $"{layer}_{neuronIdx}";
                neurons[neuronId] = neuronObj;

                // Configurar apariencia de la neurona
                Image neuronImage = neuronObj.GetComponent<Image>();
                if (neuronImage != null)
                {
                    // Colorear neuronas de salida bloqueadas
                    if (layer == network.layers.Length - 1 && network.outputLockStatus != null &&
                        neuronIdx < network.outputLockStatus.Length && network.outputLockStatus[neuronIdx])
                    {
                        neuronImage.color = lockedNeuronColor;
                    }
                }

                // Añadir etiqueta si está habilitado
                if (showNeuronLabels)
                {
                    GameObject labelObj = new GameObject($"Label_{neuronId}");
                    labelObj.transform.SetParent(neuronObj.transform, false);
                    TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();

                    // Asignar nombre según tipo de neurona
                    string neuronName = "";
                    if (layer == 0 && neuronIdx < inputNeuronNames.Length)
                    {
                        neuronName = inputNeuronNames[neuronIdx];
                    }
                    else if (layer == network.layers.Length - 1 && neuronIdx < outputNeuronNames.Length)
                    {
                        neuronName = outputNeuronNames[neuronIdx];
                    }
                    else
                    {
                        neuronName = $"N{layer}_{neuronIdx}";
                    }

                    labelText.text = neuronName;
                    labelText.fontSize = 10;
                    labelText.alignment = TextAlignmentOptions.Center;

                    // Posicionar etiqueta
                    labelObj.transform.localPosition = layer == 0 ?
                                                     new Vector3(-40, 0, 0) :
                                                     (layer == network.layers.Length - 1 ?
                                                     new Vector3(40, 0, 0) :
                                                     new Vector3(0, -20, 0));

                    if (layer == 0)
                    {
                        // Etiquetas a la izquierda para capa de entrada
                        labelText.alignment = TextAlignmentOptions.Right;
                    }
                    else if (layer == network.layers.Length - 1)
                    {
                        // Etiquetas a la derecha para capa de salida
                        labelText.alignment = TextAlignmentOptions.Left;
                    }

                    elements.Add(labelObj);
                }

                elements.Add(neuronObj);
            }
        }

        // Crear conexiones entre neuronas
        for (int layer = 0; layer < weights.Length; layer++)
        {
            for (int neuronFrom = 0; neuronFrom < weights[layer].Length; neuronFrom++)
            {
                for (int neuronTo = 0; neuronTo < weights[layer][neuronFrom].Length; neuronTo++)
                {
                    float weight = weights[layer][neuronFrom][neuronTo];

                    // Omitir conexiones no significativas si está habilitada la opción
                    if (showOnlySignificantConnections && Mathf.Abs(weight) < significantWeightThreshold)
                    {
                        continue;
                    }

                    // Obtener las neuronas conectadas
                    GameObject fromNeuron = neurons[$"{layer}_{neuronFrom}"];
                    GameObject toNeuron = neurons[$"{layer + 1}_{neuronTo}"];

                    if (fromNeuron == null || toNeuron == null)
                    {
                        continue;
                    }

                    // Crear línea de conexión
                    GameObject connection = Instantiate(connectionPrefab, networkContainer);
                    RectTransform connRect = connection.GetComponent<RectTransform>();

                    // Calcular posición y grosor de la línea
                    Vector2 fromPos = fromNeuron.transform.localPosition;
                    Vector2 toPos = toNeuron.transform.localPosition;
                    float distance = Vector2.Distance(fromPos, toPos);
                    Vector2 midPoint = (fromPos + toPos) / 2;

                    // Configurar línea
                    connRect.localPosition = new Vector3(midPoint.x, midPoint.y, 0);

                    // Grosor proporcional al peso
                    float thickness = Mathf.Abs(weight) * weightScale;
                    thickness = Mathf.Clamp(thickness, 1f, 8f); // Limitar grosor

                    connRect.sizeDelta = new Vector2(distance, thickness);

                    // Calcular ángulo
                    float angle = Mathf.Atan2(toPos.y - fromPos.y, toPos.x - fromPos.x) * Mathf.Rad2Deg;
                    connRect.localRotation = Quaternion.Euler(0, 0, angle);

                    // Configurar color según signo del peso
                    Image connImage = connection.GetComponent<Image>();
                    if (connImage != null)
                    {
                        connImage.color = weight >= 0 ? positiveWeightColor : negativeWeightColor;

                        // Ajustar transparencia según la magnitud
                        Color color = connImage.color;
                        float alpha = Mathf.Clamp01(Mathf.Abs(weight) / 2f);
                        alpha = Mathf.Max(0.2f, alpha); // Al menos 20% de opacidad
                        connImage.color = new Color(color.r, color.g, color.b, alpha);
                    }

                    elements.Add(connection);
                }
            }
        }

        networkElements["current"] = elements;
    }

    private float[][][] RebuildWeights(List<float> flatWeights, int[] layers)
    {
        if (flatWeights == null || flatWeights.Count == 0 || layers == null || layers.Length < 2)
        {
            return null;
        }

        float[][][] weights = new float[layers.Length - 1][][];
        int weightIndex = 0;

        try
        {
            for (int i = 0; i < layers.Length - 1; i++)
            {
                weights[i] = new float[layers[i]][];

                for (int j = 0; j < layers[i]; j++)
                {
                    weights[i][j] = new float[layers[i + 1]];

                    for (int k = 0; k < layers[i + 1]; k++)
                    {
                        if (weightIndex < flatWeights.Count)
                        {
                            weights[i][j][k] = flatWeights[weightIndex];
                            weightIndex++;
                        }
                        else
                        {
                            weights[i][j][k] = 0;
                        }
                    }
                }
            }

            return weights;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al reconstruir pesos: {e.Message}");
            return null;
        }
    }

    private void UpdateNetworkDetails()
    {
        if (networkDetailsText == null || selectedNetwork == null) return;

        string details = "<b>Detalles de la Red Neuronal</b>\n\n";

        // Información básica
        details += $"Tipo: {selectedNetwork.npcType}\n";
        details += $"Fitness: {selectedNetwork.fitness:F2}\n\n";

        // Estructura
        if (selectedNetwork.layers != null)
        {
            details += $"Estructura: {string.Join("-", selectedNetwork.layers)}\n";
        }

        // Análisis de pesos
        if (selectedNetwork.flattenedWeights != null && selectedNetwork.flattenedWeights.Count > 0)
        {
            float avgWeight = 0;
            float maxWeight = float.MinValue;
            float minWeight = float.MaxValue;
            int nonZeroWeights = 0;

            foreach (float weight in selectedNetwork.flattenedWeights)
            {
                avgWeight += weight;
                maxWeight = Mathf.Max(maxWeight, weight);
                minWeight = Mathf.Min(minWeight, weight);

                if (Mathf.Abs(weight) > 0.01f)
                {
                    nonZeroWeights++;
                }
            }

            avgWeight /= selectedNetwork.flattenedWeights.Count;
            float percentageActive = (float)nonZeroWeights / selectedNetwork.flattenedWeights.Count * 100f;

            details += $"Total de pesos: {selectedNetwork.flattenedWeights.Count}\n";
            details += $"Pesos activos: {nonZeroWeights} ({percentageActive:F1}%)\n";
            details += $"Peso promedio: {avgWeight:F3}\n";
            details += $"Peso máximo: {maxWeight:F3}\n";
            details += $"Peso mínimo: {minWeight:F3}\n\n";
        }

        // Información de bloqueos
        if (selectedNetwork.outputLockStatus != null)
        {
            details += "<b>Estado de bloqueo:</b>\n";

            for (int i = 0; i < selectedNetwork.outputLockStatus.Length && i < outputNeuronNames.Length; i++)
            {
                details += $"{outputNeuronNames[i]}: {(selectedNetwork.outputLockStatus[i] ? "BLOQUEADO" : "Desbloqueado")}\n";
            }
        }

        // Análisis de comportamiento
        details += "\n<b>Análisis de comportamiento:</b>\n";

        // Proporción de pesos para cada acción
        if (selectedNetwork.flattenedWeights != null && selectedNetwork.layers != null)
        {
            float[][][] weights = RebuildWeights(selectedNetwork.flattenedWeights, selectedNetwork.layers);
            if (weights != null && weights.Length > 0)
            {
                int lastLayerIndex = weights.Length - 1;

                // Suma de pesos absolutos para cada neurona de salida
                float[] outputWeightSums = new float[selectedNetwork.layers[selectedNetwork.layers.Length - 1]];

                for (int j = 0; j < weights[lastLayerIndex].Length; j++)
                {
                    for (int k = 0; k < weights[lastLayerIndex][j].Length; k++)
                    {
                        outputWeightSums[k] += Mathf.Abs(weights[lastLayerIndex][j][k]);
                    }
                }

                // Obtener total para calcular porcentajes
                float totalWeightSum = 0;
                foreach (float sum in outputWeightSums)
                {
                    totalWeightSum += sum;
                }

                if (totalWeightSum > 0)
                {
                    for (int i = 0; i < outputWeightSums.Length && i < outputNeuronNames.Length; i++)
                    {
                        float percentage = outputWeightSums[i] / totalWeightSum * 100f;
                        details += $"{outputNeuronNames[i]}: {percentage:F1}% de influencia\n";
                    }
                }
            }
        }

        networkDetailsText.text = details;
    }

    private void ClearNetworkVisualization()
    {
        if (networkElements.ContainsKey("current"))
        {
            foreach (var element in networkElements["current"])
            {
                if (element != null)
                {
                    Destroy(element);
                }
            }

            networkElements.Remove("current");
        }
    }

    // Configuraciones de visualización
    public void ToggleSignificantConnectionsOnly(bool value)
    {
        showOnlySignificantConnections = value;
        if (selectedNetwork != null)
        {
            VisualizeNetwork(selectedNetwork);
        }
    }

    public void SetSignificantWeightThreshold(float value)
    {
        significantWeightThreshold = value;
        if (selectedNetwork != null && showOnlySignificantConnections)
        {
            VisualizeNetwork(selectedNetwork);
        }
    }

    public void ToggleNeuronLabels(bool value)
    {
        showNeuronLabels = value;
        if (selectedNetwork != null)
        {
            VisualizeNetwork(selectedNetwork);
        }
    }
}