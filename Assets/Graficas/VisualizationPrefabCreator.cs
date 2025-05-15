using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class VisualizationPrefabCreator : MonoBehaviour
{
    [Header("Prefabs Requeridos")]
    [SerializeField] private GameObject dataPointPrefab;
    [SerializeField] private GameObject lineRendererPrefab;
    [SerializeField] private GameObject neuronPrefab;

    [Header("Creación de Prefabs")]
    [SerializeField] private Button createPrefabsButton;

    [Header("Referencias")]
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private GameObject previewPanel;
    [SerializeField] private TextMeshProUGUI statusText;

    // Lista para rastrear objetos creados
    private List<GameObject> createdObjects = new List<GameObject>();

    void Start()
    {
        if (createPrefabsButton != null)
        {
            createPrefabsButton.onClick.AddListener(CreateRequiredPrefabs);
        }
    }

    public void CreateRequiredPrefabs()
    {
        // Limpiar objetos previos
        ClearCreatedObjects();

        // Crear prefabs básicos si no existen
        if (dataPointPrefab == null)
        {
            dataPointPrefab = CreateDataPointPrefab();
        }

        if (lineRendererPrefab == null)
        {
            lineRendererPrefab = CreateLineRendererPrefab();
        }

        if (neuronPrefab == null)
        {
            neuronPrefab = CreateNeuronPrefab();
        }

        // Mostrar prefabs en la escena
        ShowPrefabsPreview();

        if (statusText != null)
        {
            statusText.text = "Prefabs creados con éxito. Para guardarlos como prefabs:\n" +
                            "1. Arrastre cada objeto desde la jerarquía a la carpeta de prefabs\n" +
                            "2. Asigne estos prefabs a los campos correspondientes en los componentes del sistema";
        }
    }

    private GameObject CreateDataPointPrefab()
    {
        GameObject dataPoint = new GameObject("DataPointPrefab");
        dataPoint.transform.SetParent(transform);

        // Añadir componentes necesarios
        RectTransform rect = dataPoint.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(10, 10); // Tamaño del punto

        Image image = dataPoint.AddComponent<Image>();
        image.color = Color.white;
        image.sprite = CreateCircleSprite(32, Color.white);

        // Añadir a la lista de creados
        createdObjects.Add(dataPoint);

        return dataPoint;
    }

    private GameObject CreateLineRendererPrefab()
    {
        GameObject lineRenderer = new GameObject("LineRendererPrefab");
        lineRenderer.transform.SetParent(transform);

        // Añadir componentes necesarios
        RectTransform rect = lineRenderer.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(100, 2); // Largo x ancho

        Image image = lineRenderer.AddComponent<Image>();
        image.color = Color.white;

        // Añadir a la lista de creados
        createdObjects.Add(lineRenderer);

        return lineRenderer;
    }

    private GameObject CreateNeuronPrefab()
    {
        GameObject neuron = new GameObject("NeuronPrefab");
        neuron.transform.SetParent(transform);

        // Añadir componentes necesarios
        RectTransform rect = neuron.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(20, 20); // Tamaño del neuron

        Image image = neuron.AddComponent<Image>();
        image.color = Color.white;
        image.sprite = CreateCircleSprite(32, Color.white);

        // Añadir a la lista de creados
        createdObjects.Add(neuron);

        return neuron;
    }

    private void ShowPrefabsPreview()
    {
        if (previewPanel == null || canvasRect == null) return;

        // Asegurarse de que el panel de preview es visible
        previewPanel.SetActive(true);

        // Colocar el data point
        if (dataPointPrefab != null)
        {
            GameObject dataPointPreview = Instantiate(dataPointPrefab, previewPanel.transform);
            RectTransform rect = dataPointPreview.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchoredPosition = new Vector2(-100, 0);
                rect.sizeDelta = new Vector2(20, 20); // Punto más grande para mejor visibilidad
            }
            Image img = dataPointPreview.GetComponent<Image>();
            if (img != null)
            {
                img.color = Color.green;
            }
            createdObjects.Add(dataPointPreview);
        }

        // Colocar la línea
        if (lineRendererPrefab != null)
        {
            GameObject linePreview = Instantiate(lineRendererPrefab, previewPanel.transform);
            RectTransform rect = linePreview.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchoredPosition = new Vector2(0, 0);
                rect.sizeDelta = new Vector2(150, 4); // Línea más visible
            }
            Image img = linePreview.GetComponent<Image>();
            if (img != null)
            {
                img.color = Color.blue;
            }
            createdObjects.Add(linePreview);
        }

        // Colocar el neuron
        if (neuronPrefab != null)
        {
            GameObject neuronPreview = Instantiate(neuronPrefab, previewPanel.transform);
            RectTransform rect = neuronPreview.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchoredPosition = new Vector2(100, 0);
                rect.sizeDelta = new Vector2(30, 30); // Neurona más visible
            }
            Image img = neuronPreview.GetComponent<Image>();
            if (img != null)
            {
                img.color = Color.red;
            }
            createdObjects.Add(neuronPreview);
        }
    }

    private void ClearCreatedObjects()
    {
        foreach (var obj in createdObjects)
        {
            if (obj != null)
            {
                DestroyImmediate(obj);
            }
        }
        createdObjects.Clear();
    }

    private Sprite CreateCircleSprite(int resolution, Color color)
    {
        // Crear textura circular
        Texture2D texture = new Texture2D(resolution, resolution);

        // Centro y radio
        Vector2 center = new Vector2(resolution / 2, resolution / 2);
        float radius = resolution / 2;

        // Recorrer cada pixel y configurar el color
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance <= radius)
                {
                    texture.SetPixel(x, y, color);
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }

        // Aplicar cambios y crear sprite
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, resolution, resolution), Vector2.one * 0.5f);
    }

    void OnDestroy()
    {
        // Limpiar al destruir
        ClearCreatedObjects();
    }
}