using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GeneticAlgorithm : MonoBehaviour
{
    public int populationSize = 50;
    public float mutationRate = 0.01f;
    public GameObject carPrefab;
    public Transform startPosition;

    public List<CarController> population;
    public int generation = 1;
    private float bestFitness = 0;
    private float worstFitness = float.MaxValue;
    private float averageFitness = 0;

    void Start()
    {
        if (carPrefab == null)
        {
            Debug.LogError("carro no asignado error");
            return;
        }

        if (startPosition == null)
        {
            Debug.LogError("error de asignacion en el start!");
            return;
        }

        InitializePopulation();
    }

    void InitializePopulation()
    {
        population = new List<CarController>();
        for (int i = 0; i < populationSize; i++)
        {
            GameObject carGO = Instantiate(carPrefab, startPosition.position, startPosition.rotation);
            CarController car = carGO.GetComponent<CarController>();
            if (car != null)
            {
                population.Add(car);
            }
            else
            {
                Debug.LogError("CarController no esta en los componentes del carrito error");
            }
        }
    }

    void Update()
    {
        if (population != null && population.All(c => c != null && c.isDead))
        {
            EvaluatePopulation();
            Selection();
            Mutation();
            ResetPopulation();
            generation++;
        }
    }

    //rendimiento de cada carro en la generación actual.
    void EvaluatePopulation()
    {
        bestFitness = float.MinValue;
        worstFitness = float.MaxValue;
        float totalFitness = 0;

        foreach (var car in population)
        {
            if (car.fitness > bestFitness) bestFitness = car.fitness;
            if (car.fitness < worstFitness) worstFitness = car.fitness;
            totalFitness += car.fitness;
        }

        averageFitness = totalFitness / populationSize;

        Debug.Log($"Generation {generation}: mejor Fitness = {bestFitness}, peor Fitness = {worstFitness}, promedio Fitness = {averageFitness}");
    }

    void Selection()
    {
        List<CarController> newPopulation = new List<CarController>();

       
        CarController best = population.OrderByDescending(c => c.fitness).First();
        newPopulation.Add(best);

        // Selección por torneo
        while (newPopulation.Count < populationSize)
        {
            CarController parent1 = TournamentSelection();
            CarController parent2 = TournamentSelection();

            CarController child = Instantiate(carPrefab, startPosition.position, startPosition.rotation).GetComponent<CarController>();
            child.brain = parent1.brain.Copy();
            child.brain.Crossover(parent2.brain);

            newPopulation.Add(child);
        }

        // Eliminar la población anterior
        foreach (var car in population)
        {
            if (!newPopulation.Contains(car))
            {
                Destroy(car.gameObject);
            }
        }

        population = newPopulation;
    }

    CarController TournamentSelection()
    {
        int tournamentSize = 5;
        CarController best = null;
        float bestFitness = float.MinValue;

        for (int i = 0; i < tournamentSize; i++)
        {
            CarController torneoResult = population[Random.Range(0, populationSize)];
            if (torneoResult.fitness > bestFitness)
            {
                best = torneoResult;
                bestFitness = torneoResult.fitness;
            }
        }

        return best;
    }

    void Mutation()
    {
        foreach (var car in population)
        {
            car.brain.Mutate(mutationRate);
        }
    }

    void ResetPopulation()
    {
        foreach (var car in population)
        {
            if (car != null)
            {
                car.Reset();
            }
            else
            {
                Debug.LogError("error al instanciar el reset");
            }
        }
    }
}