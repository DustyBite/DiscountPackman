using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using static GeneticAlgorithm;

public class EvolutionManager : MonoBehaviour
{
    [Header("Population")]
    public GameObject enemyPrefab;
    public int populationSizePerTeam = 10;
    public Transform[] redSpawnPoints;
    public Transform[] blueSpawnPoints;
    
    [Header("Evolution Settings")]
    public SelectionMethod selectionMethod = SelectionMethod.Tournament;
    public CrossoverMethod crossoverMethod = CrossoverMethod.Uniform;
    public int tournamentSize = 3;
    public float mutationRate = 0.2f;
    public float mutationStrength = 1f;
    public int eliteCount = 2;
    
    [Header("Battle Settings")]
    public float battleDuration = 90f;
    public bool autoEvolve = true;
    public int currentGeneration = 0;
    public bool evolveWinnerOnly = false; // Only evolve winning team
    
    [Header("Fitness Function Weights")]
    public float damageDealtWeight = 10f;
    public float survivalWeight = 5f;
    public float killWeight = 50f;
    public float damageTakenPenalty = -5f;
    public float deathPenalty = -100f;
    public float teamWinBonus = 200f;
    public float accuracyWeight = 5f;
    
    [Header("Debug")]
    public bool showStats = true;
    public bool logEvolution = true;

    private List<Genome> redPopulation = new List<Genome>();
    private List<Genome> bluePopulation = new List<Genome>();
    private List<GameObject> redEnemies = new List<GameObject>();
    private List<GameObject> blueEnemies = new List<GameObject>();
    private float battleTimer = 0f;
    
    // Statistics
    private float redBestFitness = 0f;
    private float blueBestFitness = 0f;
    private float redAvgFitness = 0f;
    private float blueAvgFitness = 0f;
    private int redWins = 0;
    private int blueWins = 0;

    void Start()
    {
        InitializePopulations();
        SpawnBattle();
    }

    void Update()
    {
        if (!autoEvolve) return;

        battleTimer += Time.deltaTime;

        if (battleTimer >= battleDuration || BattleOver())
        {
            EvolveNextGeneration();
        }
    }

    // ==========================================
    // INITIALIZATION
    // ==========================================

    void InitializePopulations()
    {
        redPopulation.Clear();
        bluePopulation.Clear();
        
        for (int i = 0; i < populationSizePerTeam; i++)
        {
            redPopulation.Add(Genome.CreateRandom());
            bluePopulation.Add(Genome.CreateRandom());
        }
        
        currentGeneration = 1;
        
        if (logEvolution)
        {
            Debug.Log($"[Evolution] Generation 1 initialized with {populationSizePerTeam} genomes per team");
        }
    }

    void SpawnBattle()
    {
        // Clear previous battle
        foreach (var enemy in redEnemies)
        {
            if (enemy != null) Destroy(enemy);
        }
        foreach (var enemy in blueEnemies)
        {
            if (enemy != null) Destroy(enemy);
        }
        redEnemies.Clear();
        blueEnemies.Clear();

        // Reset team stats
        if (TeamManager.Instance != null)
        {
            TeamManager.Instance.ResetStats();
        }

        // Spawn Red Team
        for (int i = 0; i < redPopulation.Count; i++)
        {
            Vector3 spawnPos = GetSpawnPosition(i, redSpawnPoints);
            GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            enemy.name = $"Red_{i}";
            
            EnemyAI ai = enemy.GetComponent<EnemyAI>();
            if (ai != null)
            {
                ai.SetTeam(Team.Red);
                ai.ApplyGenome(redPopulation[i]);
            }
            
            redEnemies.Add(enemy);
        }

        // Spawn Blue Team
        for (int i = 0; i < bluePopulation.Count; i++)
        {
            Vector3 spawnPos = GetSpawnPosition(i, blueSpawnPoints);
            GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            enemy.name = $"Blue_{i}";
            
            EnemyAI ai = enemy.GetComponent<EnemyAI>();
            if (ai != null)
            {
                ai.SetTeam(Team.Blue);
                ai.ApplyGenome(bluePopulation[i]);
            }
            
            blueEnemies.Add(enemy);
        }

        battleTimer = 0f;
        
        if (logEvolution)
        {
            Debug.Log($"[Evolution] Generation {currentGeneration} battle started!");
        }
    }

    Vector3 GetSpawnPosition(int index, Transform[] spawnPoints)
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int spawnIndex = index % spawnPoints.Length;
            Vector3 basePos = spawnPoints[spawnIndex].position;
            
            // Add random offset to avoid exact overlap
            Vector2 randomOffset = Random.insideUnitCircle * 2f;
            return basePos + new Vector3(randomOffset.x, 0, randomOffset.y);
        }
        
        // Fallback random spawn
        Vector2 randomCircle = Random.insideUnitCircle * 10f;
        return new Vector3(randomCircle.x, 0, randomCircle.y);
    }

    bool BattleOver()
    {
        if (TeamManager.Instance == null) return false;
        return TeamManager.Instance.GetWinningTeam() != Team.Neutral;
    }

    // ==========================================
    // FITNESS EVALUATION
    // ==========================================

    void EvaluateFitness()
    {
        Team winner = Team.Neutral;
        if (TeamManager.Instance != null)
        {
            winner = TeamManager.Instance.GetWinningTeam();
            
            if (winner == Team.Red) redWins++;
            else if (winner == Team.Blue) blueWins++;
        }

        // Evaluate Red Team
        for (int i = 0; i < redEnemies.Count; i++)
        {
            if (i >= redPopulation.Count) continue;
            
            float fitness = 0f;
            
            if (redEnemies[i] != null)
            {
                EnemyAI ai = redEnemies[i].GetComponent<EnemyAI>();
                if (ai != null)
                {
                    fitness = CalculateFitness(ai.fitnessMetrics, winner == Team.Red);
                }
            }
            else
            {
                // Dead - still calculate fitness from last known metrics
                fitness = deathPenalty;
            }

            redPopulation[i].fitness = fitness;
        }

        // Evaluate Blue Team
        for (int i = 0; i < blueEnemies.Count; i++)
        {
            if (i >= bluePopulation.Count) continue;
            
            float fitness = 0f;
            
            if (blueEnemies[i] != null)
            {
                EnemyAI ai = blueEnemies[i].GetComponent<EnemyAI>();
                if (ai != null)
                {
                    fitness = CalculateFitness(ai.fitnessMetrics, winner == Team.Blue);
                }
            }
            else
            {
                fitness = deathPenalty;
            }

            bluePopulation[i].fitness = fitness;
        }

        // Calculate statistics
        redAvgFitness = redPopulation.Average(g => g.fitness);
        blueAvgFitness = bluePopulation.Average(g => g.fitness);
        redBestFitness = redPopulation.Max(g => g.fitness);
        blueBestFitness = bluePopulation.Max(g => g.fitness);

        if (logEvolution)
        {
            Debug.Log($"[Evolution] Gen {currentGeneration} Results:");
            Debug.Log($"  Winner: {winner}");
            Debug.Log($"  Red - Best: {redBestFitness:F1}, Avg: {redAvgFitness:F1}");
            Debug.Log($"  Blue - Best: {blueBestFitness:F1}, Avg: {blueAvgFitness:F1}");
        }
    }

    float CalculateFitness(FitnessMetrics metrics, bool wonBattle)
    {
        float fitness = 0f;

        // Positive contributions
        fitness += metrics.damageDealt * damageDealtWeight;
        fitness += metrics.survivalTime * survivalWeight;
        fitness += metrics.kills * killWeight;
        
        // Accuracy bonus
        int totalAttacks = metrics.successfulAttacks + metrics.missedAttacks;
        if (totalAttacks > 0)
        {
            float accuracy = (float)metrics.successfulAttacks / totalAttacks;
            fitness += accuracy * accuracyWeight * 10f;
        }
        
        // Health bonus
        fitness += (metrics.healthRemaining / 100f) * 20f;

        // Negative contributions
        fitness += metrics.damageTaken * damageTakenPenalty;
        fitness += metrics.deaths * deathPenalty;

        // Team win bonus
        if (wonBattle)
        {
            fitness += teamWinBonus;
        }

        return Mathf.Max(0, fitness);
    }

    // ==========================================
    // EVOLUTION
    // ==========================================

    public void EvolveNextGeneration()
    {
        EvaluateFitness();

        Team winner = Team.Neutral;
        if (TeamManager.Instance != null)
        {
            winner = TeamManager.Instance.GetWinningTeam();
        }

        // Evolve Red Team (unless evolveWinnerOnly and they lost)
        if (!evolveWinnerOnly || winner == Team.Red || winner == Team.Neutral)
        {
            redPopulation = EvolvePopulation(redPopulation);
        }

        // Evolve Blue Team (unless evolveWinnerOnly and they lost)
        if (!evolveWinnerOnly || winner == Team.Blue || winner == Team.Neutral)
        {
            bluePopulation = EvolvePopulation(bluePopulation);
        }

        currentGeneration++;
        SpawnBattle();
    }

    List<Genome> EvolvePopulation(List<Genome> population)
    {
        List<Genome> newPopulation = new List<Genome>();

        // Elitism
        var sortedPopulation = population.OrderByDescending(g => g.fitness).ToList();
        for (int i = 0; i < eliteCount && i < sortedPopulation.Count; i++)
        {
            newPopulation.Add(sortedPopulation[i].Clone());
        }

        // Fill with offspring
        while (newPopulation.Count < population.Count)
        {
            Genome parent1 = SelectParent(population);
            Genome parent2 = SelectParent(population);
            Genome child = Crossover(parent1, parent2);
            Mutate(child, mutationRate, mutationStrength);
            newPopulation.Add(child);
        }

        return newPopulation;
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
            case CrossoverMethod.Uniform:
                return UniformCrossover(parent1, parent2);
            case CrossoverMethod.Arithmetic:
                return ArithmeticCrossover(parent1, parent2, Random.Range(0.3f, 0.7f));
            default:
                return UniformCrossover(parent1, parent2);
        }
    }

    // ==========================================
    // UI
    // ==========================================

    void OnGUI()
    {
        if (!showStats) return;

        GUIStyle style = new GUIStyle();
        style.fontSize = 16;
        style.normal.textColor = Color.white;
        style.fontStyle = FontStyle.Bold;

        GUI.Label(new Rect(Screen.width / 2 - 150, 10, 300, 200),
            $"=== BATTLE SIMULATOR ===\n" +
            $"Generation: {currentGeneration}\n" +
            $"Time: {battleTimer:F1}s / {battleDuration}s\n\n" +
            $"Red Wins: {redWins}\n" +
            $"Blue Wins: {blueWins}\n\n" +
            $"Red Best: {redBestFitness:F0}\n" +
            $"Blue Best: {blueBestFitness:F0}",
            style);

        if (GUI.Button(new Rect(Screen.width / 2 - 75, 220, 150, 30), "Next Battle"))
        {
            EvolveNextGeneration();
        }
    }
}