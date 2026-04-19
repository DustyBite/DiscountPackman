using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using static GeneticAlgorithm;

public class EvolutionManager : MonoBehaviour
{
    [Header("Population")]
    public GameObject enemyPrefab;
    public int populationSize = 20;
    public Transform[] spawnPoints;
    
    [Header("Evolution Settings")]
    public SelectionMethod selectionMethod = SelectionMethod.Tournament;
    public CrossoverMethod crossoverMethod = CrossoverMethod.Uniform;
    public int tournamentSize = 3;
    public float mutationRate = 0.2f;
    public float mutationStrength = 1f;
    public int eliteCount = 2;
    
    [Header("Generation Settings")]
    public float generationDuration = 60f; // seconds per generation
    public bool autoEvolve = true;
    public int currentGeneration = 0;
    
    [Header("Fitness Function Weights")]
    public float damageDealtWeight = 10f;
    public float survivalWeight = 5f;
    public float killWeight = 50f;
    public float damageTakenPenalty = -5f;
    public float deathPenalty = -100f;
    public float playerProximityWeight = 2f;
    public float accuracyWeight = 5f;
    
    [Header("Debug")]
    public bool showStats = true;
    public bool logEvolution = true;

    private List<Genome> currentPopulation = new List<Genome>();
    private List<GameObject> activeEnemies = new List<GameObject>();
    private float generationTimer = 0f;
    
    // Statistics
    private float bestFitness = 0f;
    private float averageFitness = 0f;
    private Genome bestGenome;

    void Start()
    {
        InitializePopulation();
        SpawnGeneration();
    }

    void Update()
    {
        if (!autoEvolve) return;

        generationTimer += Time.deltaTime;

        if (generationTimer >= generationDuration || AllEnemiesDead())
        {
            EvolveNextGeneration();
        }
    }

    // ==========================================
    // INITIALIZATION
    // ==========================================

    void InitializePopulation()
    {
        currentPopulation.Clear();
        
        for (int i = 0; i < populationSize; i++)
        {
            currentPopulation.Add(Genome.CreateRandom());
        }
        
        currentGeneration = 1;
        
        if (logEvolution)
        {
            Debug.Log($"[Evolution] Generation 1 initialized with {populationSize} random genomes");
        }
    }

    void SpawnGeneration()
    {
        // Clear previous generation
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null) Destroy(enemy);
        }
        activeEnemies.Clear();

        // Spawn new generation
        for (int i = 0; i < currentPopulation.Count; i++)
        {
            Vector3 spawnPos = GetSpawnPosition(i);
            GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            
            // Apply genome
            EnemyAI ai = enemy.GetComponent<EnemyAI>();
            if (ai != null)
            {
                ai.ApplyGenome(currentPopulation[i]);
            }
            
            activeEnemies.Add(enemy);
        }

        generationTimer = 0f;
        
        if (logEvolution)
        {
            Debug.Log($"[Evolution] Generation {currentGeneration} spawned");
        }
    }

    Vector3 GetSpawnPosition(int index)
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int spawnIndex = index % spawnPoints.Length;
            return spawnPoints[spawnIndex].position;
        }
        
        // Random spawn in circle
        Vector2 randomCircle = Random.insideUnitCircle * 10f;
        return new Vector3(randomCircle.x, 0, randomCircle.y);
    }

    bool AllEnemiesDead()
    {
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null) return false;
        }
        return true;
    }

    // ==========================================
    // FITNESS EVALUATION
    // ==========================================

    void EvaluateFitness()
    {
        for (int i = 0; i < activeEnemies.Count; i++)
        {
            if (activeEnemies[i] == null) continue;

            EnemyAI ai = activeEnemies[i].GetComponent<EnemyAI>();
            if (ai == null) continue;

            FitnessMetrics metrics = ai.fitnessMetrics;
            
            // Calculate fitness using weighted components
            float fitness = 0f;

            // Positive contributions
            fitness += metrics.damageDealt * damageDealtWeight;
            fitness += metrics.survivalTime * survivalWeight;
            fitness += metrics.kills * killWeight;
            fitness += metrics.timeNearPlayer * playerProximityWeight;
            
            // Accuracy bonus
            int totalAttacks = metrics.successfulAttacks + metrics.missedAttacks;
            if (totalAttacks > 0)
            {
                float accuracy = (float)metrics.successfulAttacks / totalAttacks;
                fitness += accuracy * accuracyWeight * 10f;
            }
            
            // Health bonus (survived with health)
            fitness += (metrics.healthRemaining / 100f) * 20f;

            // Negative contributions
            fitness += metrics.damageTaken * damageTakenPenalty;
            fitness += metrics.deaths * deathPenalty;

            // Ensure minimum fitness
            fitness = Mathf.Max(0, fitness);

            currentPopulation[i].fitness = fitness;
        }

        // Calculate statistics
        if (currentPopulation.Count > 0)
        {
            averageFitness = currentPopulation.Average(g => g.fitness);
            bestFitness = currentPopulation.Max(g => g.fitness);
            bestGenome = currentPopulation.OrderByDescending(g => g.fitness).First().Clone();
        }

        if (logEvolution)
        {
            Debug.Log($"[Evolution] Gen {currentGeneration} - Best: {bestFitness:F1}, Avg: {averageFitness:F1}");
        }
    }

    // ==========================================
    // EVOLUTION
    // ==========================================

    public void EvolveNextGeneration()
    {
        EvaluateFitness();

        List<Genome> newPopulation = new List<Genome>();

        // Elitism - keep best genomes
        var sortedPopulation = currentPopulation.OrderByDescending(g => g.fitness).ToList();
        for (int i = 0; i < eliteCount && i < sortedPopulation.Count; i++)
        {
            newPopulation.Add(sortedPopulation[i].Clone());
        }

        // Fill rest with offspring
        while (newPopulation.Count < populationSize)
        {
            // Selection
            Genome parent1 = SelectParent(currentPopulation);
            Genome parent2 = SelectParent(currentPopulation);

            // Crossover
            Genome child = Crossover(parent1, parent2);

            // Mutation
            Mutate(child, mutationRate, mutationStrength);

            newPopulation.Add(child);
        }

        currentPopulation = newPopulation;
        currentGeneration++;

        SpawnGeneration();
    }

    Genome SelectParent(List<Genome> population)
    {
        switch (selectionMethod)
        {
            case SelectionMethod.Tournament:
                return TournamentSelection(population, tournamentSize);
            case SelectionMethod.Roulette:
                return RouletteSelection(population);
            case SelectionMethod.Rank:
                return RankSelection(population);
            case SelectionMethod.Elite:
                return population.OrderByDescending(g => g.fitness).First().Clone();
            default:
                return TournamentSelection(population, tournamentSize);
        }
    }

    Genome Crossover(Genome parent1, Genome parent2)
    {
        switch (crossoverMethod)
        {
            case CrossoverMethod.SinglePoint:
                return SinglePointCrossover(parent1, parent2);
            case CrossoverMethod.TwoPoint:
                // Simplified as single point for now
                return SinglePointCrossover(parent1, parent2);
            case CrossoverMethod.Uniform:
                return UniformCrossover(parent1, parent2);
            case CrossoverMethod.Arithmetic:
                return ArithmeticCrossover(parent1, parent2, Random.Range(0.3f, 0.7f));
            default:
                return UniformCrossover(parent1, parent2);
        }
    }

    // ==========================================
    // UI / DEBUG
    // ==========================================

    void OnGUI()
    {
        if (!showStats) return;

        GUIStyle style = new GUIStyle();
        style.fontSize = 16;
        style.normal.textColor = Color.white;
        style.fontStyle = FontStyle.Bold;

        GUI.Label(new Rect(10, 10, 400, 150),
            $"=== GENETIC ALGORITHM ===\n" +
            $"Generation: {currentGeneration}\n" +
            $"Time: {generationTimer:F1}s / {generationDuration}s\n" +
            $"Alive: {activeEnemies.Count(e => e != null)} / {populationSize}\n" +
            $"Best Fitness: {bestFitness:F1}\n" +
            $"Avg Fitness: {averageFitness:F1}",
            style);

        // Manual evolution button
        if (GUI.Button(new Rect(10, 170, 150, 30), "Evolve Now"))
        {
            EvolveNextGeneration();
        }
    }

    // Save/Load best genome
    public void SaveBestGenome()
    {
        if (bestGenome != null)
        {
            string json = JsonUtility.ToJson(bestGenome, true);
            System.IO.File.WriteAllText(Application.dataPath + "/BestGenome.json", json);
            Debug.Log("Best genome saved!");
        }
    }

    public void LoadBestGenome()
    {
        string path = Application.dataPath + "/BestGenome.json";
        if (System.IO.File.Exists(path))
        {
            string json = System.IO.File.ReadAllText(path);
            bestGenome = JsonUtility.FromJson<Genome>(json);
            Debug.Log("Best genome loaded!");
        }
    }
}