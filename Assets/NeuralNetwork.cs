using UnityEngine;
using System;

// Implementa una red neuronal multicapa (perceptrón multicapa).
// Esta red neuronal toma decisiones para los NPCs basándose en sus sensores.

public class NeuralNetwork
{
    // La estructura de la red: cantidad de neuronas por capa
    private int[] layers;

    // Matriz para almacenar los valores de las neuronas
    // Primera dimensión: capa, Segunda dimensión: neurona en esa capa
    private float[][] neurons;

    // Matriz tridimensional para almacenar los pesos de las conexiones
    // Primera dimensión: capa de origen
    // Segunda dimensión: neurona de origen en esa capa
    // Tercera dimensión: neurona destino en la siguiente capa
    private float[][][] weights;

    /// Constructor que crea una nueva red neuronal con la estructura especificada.    
    /// Número de neuronas en cada capa (entrada, ocultas, salida)</param>
    public NeuralNetwork(params int[] layers)
    {
        this.layers = layers;
        InitializeNeurons();
        InitializeWeights();
    }

    //
    // Inicializa las matrices para almacenar los valores de las neuronas.
 
    private void InitializeNeurons()
    {
        // Creamos el array de arrays para las neuronas
        neurons = new float[layers.Length][];

        // Para cada capa, creamos un array del tamaño adecuado
        for (int i = 0; i < layers.Length; i++)
        {
            neurons[i] = new float[layers[i]];
        }
    }

    // Inicializa los pesos de las conexiones con valores aleatorios.  
    private void InitializeWeights()
    {
        try
        {
            // Creamos el array tridimensional para los pesos
            // El número de capas de pesos es uno menos que el número de capas de neuronas
            weights = new float[layers.Length - 1][][];

            // Para cada capa de pesos (entre capas de neuronas)
            for (int i = 0; i < layers.Length - 1; i++)
            {
                // Creamos un array de arrays para las neuronas de origen
                weights[i] = new float[layers[i]][];

                // Para cada neurona en la capa actual (origen)
                for (int j = 0; j < layers[i]; j++)
                {
                    // Creamos un array para los pesos de las conexiones a la siguiente capa
                    weights[i][j] = new float[layers[i + 1]];

                    // Para cada neurona en la capa siguiente (destino)
                    for (int k = 0; k < layers[i + 1]; k++)
                    {
                        // Inicializamos con un peso aleatorio entre -1 y 1
                        weights[i][j][k] = UnityEngine.Random.Range(-1f, 1f);
                    }
                }
            }

            // Añadimos un sesgo a los pesos que conectan con la primera neurona de salida
            // La última capa de pesos es weights.Length - 1
            int lastWeightLayerIndex = weights.Length - 1;

            // Para cada neurona en la penúltima capa
            for (int j = 0; j < weights[lastWeightLayerIndex].Length; j++)
            {
                // Añadir sesgo a la conexión con la primera neurona de salida (neurona de avance)
                weights[lastWeightLayerIndex][j][0] += 0.5f;

                // Añadir sesgo negativo a la conexión con la neurona de salto (índice 3)
                // Esto hará que los NPCs sean menos propensos a saltar por defecto
                if (layers[layers.Length - 1] > 3) // Asegurar que hay al menos 4 neuronas de salida
                {
                    weights[lastWeightLayerIndex][j][3] -= 0.3f;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al inicializar pesos: {e.Message}");
            Debug.LogException(e);
        }
    }


    public float[] FeedForward(float[] inputs)
    {
        // Colocamos los valores de entrada en la primera capa de neuronas
        for (int i = 0; i < inputs.Length; i++)
        {
            neurons[0][i] = inputs[i];
        }

        // Procesamos capa por capa, desde la primera capa oculta hasta la de salida
        for (int i = 1; i < layers.Length; i++)
        {
            // Para cada neurona en la capa actual
            for (int j = 0; j < layers[i]; j++)
            {
                float sum = 0;

                // Sumamos todas las entradas ponderadas desde la capa anterior
                for (int k = 0; k < layers[i - 1]; k++)
                {
                    // Valor de la neurona anterior * peso de la conexión
                    sum += neurons[i - 1][k] * weights[i - 1][k][j];
                }

                // Aplicamos la función de activación (tanh) y guardamos el resultado
                neurons[i][j] = (float)Math.Tanh(sum);
            }
        }

        // Devolvemos la última capa (valores de salida)
        return neurons[neurons.Length - 1];
    }

  
    public void Mutate(float mutationRate)
    {
        // Recorremos todos los pesos de la red
        for (int i = 0; i < weights.Length; i++)
        {
            for (int j = 0; j < weights[i].Length; j++)
            {
                for (int k = 0; k < weights[i][j].Length; k++)
                {
                    // Con una probabilidad igual a mutationRate, modificamos el peso
                    if (UnityEngine.Random.value < mutationRate)
                    {
                        // Añadimos un valor aleatorio pequeño al peso
                        weights[i][j][k] += UnityEngine.Random.Range(-0.1f, 0.1f);
                    }
                }
            }
        }
    }

    
    public NeuralNetwork Copy()
    {
        // Creamos una nueva red con la misma estructura
        NeuralNetwork copy = new NeuralNetwork(layers);

        // Copiamos todos los pesos
        for (int i = 0; i < weights.Length; i++)
        {
            for (int j = 0; j < weights[i].Length; j++)
            {
                for (int k = 0; k < weights[i][j].Length; k++)
                {
                    copy.weights[i][j][k] = weights[i][j][k];
                }
            }
        }

        return copy;
    }

    
    public void Crossover(NeuralNetwork other)
    {
        // Recorremos todos los pesos
        for (int i = 0; i < weights.Length; i++)
        {
            for (int j = 0; j < weights[i].Length; j++)
            {
                for (int k = 0; k < weights[i][j].Length; k++)
                {
                    // Con 50% de probabilidad, tomamos el peso de la otra red
                    if (UnityEngine.Random.value < 0.5f)
                    {
                        weights[i][j][k] = other.weights[i][j][k];
                    }
                    // Si no, mantenemos nuestro peso actual
                }
            }
        }
    }

    /// Obtiene la estructura de capas de la red.
    // Usado para serialización.
    public int[] GetLayers()
    {
        return layers;
    }

   
    // Obtiene todos los pesos de la red.
    // Usado para serialización.  
   
    public float[][][] GetWeights()
    {
        return weights;
    }

    // Establece todos los pesos de la red.
    // Usado al cargar una red previamente guardada.
    public void SetWeights(float[][][] newWeights)
    {
        // Verificar que newWeights no sea nulo
        if (newWeights == null)
        {
            Debug.LogError("Error al establecer pesos: El array de pesos es nulo");
            return;
        }

        // Verificar que la estructura coincida con nuestra red
        if (newWeights.Length != weights.Length)
        {
            Debug.LogError($"Error al establecer pesos: Dimensiones incompatibles. Se esperaba {weights.Length} capas, pero se recibieron {newWeights.Length}");
            return;
        }

        try
        {
            // Copiamos los pesos desde la matriz proporcionada
            for (int i = 0; i < weights.Length; i++)
            {
                if (newWeights[i] == null)
                {
                    Debug.LogError($"Error: La capa de pesos {i} es nula");
                    continue;
                }

                if (newWeights[i].Length != weights[i].Length)
                {
                    Debug.LogError($"Error: Dimensiones incorrectas en capa {i}. Se esperaba {weights[i].Length}, se recibió {newWeights[i].Length}");
                    continue;
                }

                for (int j = 0; j < weights[i].Length; j++)
                {
                    if (newWeights[i][j] == null)
                    {
                        Debug.LogError($"Error: Pesos nulos en capa {i}, neurona {j}");
                        continue;
                    }

                    if (newWeights[i][j].Length != weights[i][j].Length)
                    {
                        Debug.LogError($"Error: Dimensiones incorrectas en capa {i}, neurona {j}. Se esperaba {weights[i][j].Length}, se recibió {newWeights[i][j].Length}");
                        continue;
                    }

                    for (int k = 0; k < weights[i][j].Length; k++)
                    {
                        weights[i][j][k] = newWeights[i][j][k];
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al establecer pesos: {e.Message}");
            Debug.LogException(e);
        }
    }
}