using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GeneticAlgorithm
{
    // ==========================================
    // GENOME (AI DNA)
    // ==========================================
    
    [System.Serializable]
    public class Genome
    {
        public int id;
        public float fitness = 0f;
        
        // AI Personality Genes
        public float scary;
        public float bravery;
        public float aggression;
        public float caution;
        
        // Behavior Genes
        public float chaseRange;
        public float attackRange;
        public float fleeHealthThreshold;
        public float patrolRadius;
        
        // Movement Genes
        public float moveSpeed;
        public float rotationSpeed;
        
        // Influence Genes
        public float influenceWeight;
        public float goalWeight;
        public float heatAttractionWeight;
        
        // Fuzzy Logic Genes
        public float aggressionBias;      // Shifts aggression threshold
        public float cautionBias;         // Shifts caution threshold
        public float fleeBias;            // Shifts flee threshold
        
        // Combat Genes
        public float attackCooldownModifier;  // How fast they attack
        public float damageMultiplier;        // How much damage they deal

        public Genome()
        {
            id = Random.Range(0, 999999);
        }

        // Create random genome
        public static Genome CreateRandom()
        {
            Genome genome = new Genome();
            
            genome.scary = Random.Range(1f, 10f);
            genome.bravery = Random.Range(1f, 10f);
            genome.aggression = Random.Range(0f, 100f);
            genome.caution = Random.Range(0f, 100f);
            
            genome.chaseRange = Random.Range(5f, 15f);
            genome.attackRange = Random.Range(1.5f, 4f);
            genome.fleeHealthThreshold = Random.Range(10f, 50f);
            genome.patrolRadius = Random.Range(10f, 20f);
            
            genome.moveSpeed = Random.Range(2f, 5f);
            genome.rotationSpeed = Random.Range(5f, 15f);
            
            genome.influenceWeight = Random.Range(0.2f, 3f);
            genome.goalWeight = Random.Range(2f, 8f);
            genome.heatAttractionWeight = Random.Range(1f, 5f);
            
            genome.aggressionBias = Random.Range(-20f, 20f);
            genome.cautionBias = Random.Range(-20f, 20f);
            genome.fleeBias = Random.Range(-20f, 20f);
            
            genome.attackCooldownModifier = Random.Range(0.5f, 2f);
            genome.damageMultiplier = Random.Range(0.5f, 1.5f);
            
            return genome;
        }

        // Deep copy
        public Genome Clone()
        {
            Genome clone = new Genome();
            clone.id = Random.Range(0, 999999);
            
            clone.scary = scary;
            clone.bravery = bravery;
            clone.aggression = aggression;
            clone.caution = caution;
            
            clone.chaseRange = chaseRange;
            clone.attackRange = attackRange;
            clone.fleeHealthThreshold = fleeHealthThreshold;
            clone.patrolRadius = patrolRadius;
            
            clone.moveSpeed = moveSpeed;
            clone.rotationSpeed = rotationSpeed;
            
            clone.influenceWeight = influenceWeight;
            clone.goalWeight = goalWeight;
            clone.heatAttractionWeight = heatAttractionWeight;
            
            clone.aggressionBias = aggressionBias;
            clone.cautionBias = cautionBias;
            clone.fleeBias = fleeBias;
            
            clone.attackCooldownModifier = attackCooldownModifier;
            clone.damageMultiplier = damageMultiplier;
            
            return clone;
        }
    }

    // ==========================================
    // FITNESS TRACKING
    // ==========================================
    
    [System.Serializable]
    public class FitnessMetrics
    {
        public float damageDealt = 0f;
        public float damageTaken = 0f;
        public float survivalTime = 0f;
        public int kills = 0;
        public int deaths = 0;
        public float distanceTraveled = 0f;
        public float timeNearPlayer = 0f;
        public int successfulAttacks = 0;
        public int missedAttacks = 0;
        public float healthRemaining = 100f;
        
        public void Reset()
        {
            damageDealt = 0f;
            damageTaken = 0f;
            survivalTime = 0f;
            kills = 0;
            deaths = 0;
            distanceTraveled = 0f;
            timeNearPlayer = 0f;
            successfulAttacks = 0;
            missedAttacks = 0;
            healthRemaining = 100f;
        }
    }

    // ==========================================
    // GENETIC OPERATORS
    // ==========================================
    
    public enum SelectionMethod
    {
        Tournament,
        Roulette,
        Rank,
        Elite
    }

    public enum CrossoverMethod
    {
        SinglePoint,
        TwoPoint,
        Uniform,
        Arithmetic
    }

    // Tournament Selection
    public static Genome TournamentSelection(List<Genome> population, int tournamentSize)
    {
        List<Genome> tournament = new List<Genome>();
        
        for (int i = 0; i < tournamentSize; i++)
        {
            int randomIndex = Random.Range(0, population.Count);
            tournament.Add(population[randomIndex]);
        }
        
        tournament = tournament.OrderByDescending(g => g.fitness).ToList();
        return tournament[0].Clone();
    }

    // Roulette Wheel Selection
    public static Genome RouletteSelection(List<Genome> population)
    {
        float totalFitness = population.Sum(g => Mathf.Max(0, g.fitness));
        
        if (totalFitness <= 0)
        {
            return population[Random.Range(0, population.Count)].Clone();
        }
        
        float randomValue = Random.Range(0f, totalFitness);
        float currentSum = 0f;
        
        foreach (var genome in population)
        {
            currentSum += Mathf.Max(0, genome.fitness);
            if (currentSum >= randomValue)
            {
                return genome.Clone();
            }
        }
        
        return population[population.Count - 1].Clone();
    }

    // Rank Selection
    public static Genome RankSelection(List<Genome> population)
    {
        var sorted = population.OrderBy(g => g.fitness).ToList();
        int totalRank = (population.Count * (population.Count + 1)) / 2;
        
        float randomValue = Random.Range(0f, totalRank);
        float currentSum = 0f;
        
        for (int i = 0; i < sorted.Count; i++)
        {
            currentSum += i + 1;
            if (currentSum >= randomValue)
            {
                return sorted[i].Clone();
            }
        }
        
        return sorted[sorted.Count - 1].Clone();
    }

    // Single Point Crossover
    public static Genome SinglePointCrossover(Genome parent1, Genome parent2)
    {
        Genome child = new Genome();
        bool useParent1 = Random.value > 0.5f;
        
        child.scary = useParent1 ? parent1.scary : parent2.scary;
        child.bravery = useParent1 ? parent1.bravery : parent2.bravery;
        child.aggression = useParent1 ? parent1.aggression : parent2.aggression;
        child.caution = useParent1 ? parent1.caution : parent2.caution;
        
        useParent1 = Random.value > 0.5f; // Switch at crossover point
        
        child.chaseRange = useParent1 ? parent1.chaseRange : parent2.chaseRange;
        child.attackRange = useParent1 ? parent1.attackRange : parent2.attackRange;
        child.fleeHealthThreshold = useParent1 ? parent1.fleeHealthThreshold : parent2.fleeHealthThreshold;
        child.patrolRadius = useParent1 ? parent1.patrolRadius : parent2.patrolRadius;
        
        child.moveSpeed = useParent1 ? parent1.moveSpeed : parent2.moveSpeed;
        child.rotationSpeed = useParent1 ? parent1.rotationSpeed : parent2.rotationSpeed;
        
        child.influenceWeight = useParent1 ? parent1.influenceWeight : parent2.influenceWeight;
        child.goalWeight = useParent1 ? parent1.goalWeight : parent2.goalWeight;
        child.heatAttractionWeight = useParent1 ? parent1.heatAttractionWeight : parent2.heatAttractionWeight;
        
        child.aggressionBias = useParent1 ? parent1.aggressionBias : parent2.aggressionBias;
        child.cautionBias = useParent1 ? parent1.cautionBias : parent2.cautionBias;
        child.fleeBias = useParent1 ? parent1.fleeBias : parent2.fleeBias;
        
        child.attackCooldownModifier = useParent1 ? parent1.attackCooldownModifier : parent2.attackCooldownModifier;
        child.damageMultiplier = useParent1 ? parent1.damageMultiplier : parent2.damageMultiplier;
        
        return child;
    }

    // Uniform Crossover
    public static Genome UniformCrossover(Genome parent1, Genome parent2)
    {
        Genome child = new Genome();
        
        child.scary = Random.value > 0.5f ? parent1.scary : parent2.scary;
        child.bravery = Random.value > 0.5f ? parent1.bravery : parent2.bravery;
        child.aggression = Random.value > 0.5f ? parent1.aggression : parent2.aggression;
        child.caution = Random.value > 0.5f ? parent1.caution : parent2.caution;
        
        child.chaseRange = Random.value > 0.5f ? parent1.chaseRange : parent2.chaseRange;
        child.attackRange = Random.value > 0.5f ? parent1.attackRange : parent2.attackRange;
        child.fleeHealthThreshold = Random.value > 0.5f ? parent1.fleeHealthThreshold : parent2.fleeHealthThreshold;
        child.patrolRadius = Random.value > 0.5f ? parent1.patrolRadius : parent2.patrolRadius;
        
        child.moveSpeed = Random.value > 0.5f ? parent1.moveSpeed : parent2.moveSpeed;
        child.rotationSpeed = Random.value > 0.5f ? parent1.rotationSpeed : parent2.rotationSpeed;
        
        child.influenceWeight = Random.value > 0.5f ? parent1.influenceWeight : parent2.influenceWeight;
        child.goalWeight = Random.value > 0.5f ? parent1.goalWeight : parent2.goalWeight;
        child.heatAttractionWeight = Random.value > 0.5f ? parent1.heatAttractionWeight : parent2.heatAttractionWeight;
        
        child.aggressionBias = Random.value > 0.5f ? parent1.aggressionBias : parent2.aggressionBias;
        child.cautionBias = Random.value > 0.5f ? parent1.cautionBias : parent2.cautionBias;
        child.fleeBias = Random.value > 0.5f ? parent1.fleeBias : parent2.fleeBias;
        
        child.attackCooldownModifier = Random.value > 0.5f ? parent1.attackCooldownModifier : parent2.attackCooldownModifier;
        child.damageMultiplier = Random.value > 0.5f ? parent1.damageMultiplier : parent2.damageMultiplier;
        
        return child;
    }

    // Arithmetic Crossover (blend genes)
    public static Genome ArithmeticCrossover(Genome parent1, Genome parent2, float alpha = 0.5f)
    {
        Genome child = new Genome();
        
        child.scary = Mathf.Lerp(parent1.scary, parent2.scary, alpha);
        child.bravery = Mathf.Lerp(parent1.bravery, parent2.bravery, alpha);
        child.aggression = Mathf.Lerp(parent1.aggression, parent2.aggression, alpha);
        child.caution = Mathf.Lerp(parent1.caution, parent2.caution, alpha);
        
        child.chaseRange = Mathf.Lerp(parent1.chaseRange, parent2.chaseRange, alpha);
        child.attackRange = Mathf.Lerp(parent1.attackRange, parent2.attackRange, alpha);
        child.fleeHealthThreshold = Mathf.Lerp(parent1.fleeHealthThreshold, parent2.fleeHealthThreshold, alpha);
        child.patrolRadius = Mathf.Lerp(parent1.patrolRadius, parent2.patrolRadius, alpha);
        
        child.moveSpeed = Mathf.Lerp(parent1.moveSpeed, parent2.moveSpeed, alpha);
        child.rotationSpeed = Mathf.Lerp(parent1.rotationSpeed, parent2.rotationSpeed, alpha);
        
        child.influenceWeight = Mathf.Lerp(parent1.influenceWeight, parent2.influenceWeight, alpha);
        child.goalWeight = Mathf.Lerp(parent1.goalWeight, parent2.goalWeight, alpha);
        child.heatAttractionWeight = Mathf.Lerp(parent1.heatAttractionWeight, parent2.heatAttractionWeight, alpha);
        
        child.aggressionBias = Mathf.Lerp(parent1.aggressionBias, parent2.aggressionBias, alpha);
        child.cautionBias = Mathf.Lerp(parent1.cautionBias, parent2.cautionBias, alpha);
        child.fleeBias = Mathf.Lerp(parent1.fleeBias, parent2.fleeBias, alpha);
        
        child.attackCooldownModifier = Mathf.Lerp(parent1.attackCooldownModifier, parent2.attackCooldownModifier, alpha);
        child.damageMultiplier = Mathf.Lerp(parent1.damageMultiplier, parent2.damageMultiplier, alpha);
        
        return child;
    }

    // Mutation
    public static void Mutate(Genome genome, float mutationRate, float mutationStrength)
    {
        if (Random.value < mutationRate) genome.scary = Mathf.Clamp(genome.scary + Random.Range(-mutationStrength, mutationStrength), 1f, 10f);
        if (Random.value < mutationRate) genome.bravery = Mathf.Clamp(genome.bravery + Random.Range(-mutationStrength, mutationStrength), 1f, 10f);
        if (Random.value < mutationRate) genome.aggression = Mathf.Clamp(genome.aggression + Random.Range(-mutationStrength * 10, mutationStrength * 10), 0f, 100f);
        if (Random.value < mutationRate) genome.caution = Mathf.Clamp(genome.caution + Random.Range(-mutationStrength * 10, mutationStrength * 10), 0f, 100f);
        
        if (Random.value < mutationRate) genome.chaseRange = Mathf.Clamp(genome.chaseRange + Random.Range(-mutationStrength * 2, mutationStrength * 2), 5f, 15f);
        if (Random.value < mutationRate) genome.attackRange = Mathf.Clamp(genome.attackRange + Random.Range(-mutationStrength * 0.5f, mutationStrength * 0.5f), 1.5f, 4f);
        if (Random.value < mutationRate) genome.fleeHealthThreshold = Mathf.Clamp(genome.fleeHealthThreshold + Random.Range(-mutationStrength * 5, mutationStrength * 5), 10f, 50f);
        if (Random.value < mutationRate) genome.patrolRadius = Mathf.Clamp(genome.patrolRadius + Random.Range(-mutationStrength * 2, mutationStrength * 2), 10f, 20f);
        
        if (Random.value < mutationRate) genome.moveSpeed = Mathf.Clamp(genome.moveSpeed + Random.Range(-mutationStrength * 0.5f, mutationStrength * 0.5f), 2f, 5f);
        if (Random.value < mutationRate) genome.rotationSpeed = Mathf.Clamp(genome.rotationSpeed + Random.Range(-mutationStrength * 2, mutationStrength * 2), 5f, 15f);
        
        if (Random.value < mutationRate) genome.influenceWeight = Mathf.Clamp(genome.influenceWeight + Random.Range(-mutationStrength * 0.5f, mutationStrength * 0.5f), 0.2f, 3f);
        if (Random.value < mutationRate) genome.goalWeight = Mathf.Clamp(genome.goalWeight + Random.Range(-mutationStrength, mutationStrength), 2f, 8f);
        if (Random.value < mutationRate) genome.heatAttractionWeight = Mathf.Clamp(genome.heatAttractionWeight + Random.Range(-mutationStrength, mutationStrength), 1f, 5f);
        
        if (Random.value < mutationRate) genome.aggressionBias = Mathf.Clamp(genome.aggressionBias + Random.Range(-mutationStrength * 5, mutationStrength * 5), -20f, 20f);
        if (Random.value < mutationRate) genome.cautionBias = Mathf.Clamp(genome.cautionBias + Random.Range(-mutationStrength * 5, mutationStrength * 5), -20f, 20f);
        if (Random.value < mutationRate) genome.fleeBias = Mathf.Clamp(genome.fleeBias + Random.Range(-mutationStrength * 5, mutationStrength * 5), -20f, 20f);
        
        if (Random.value < mutationRate) genome.attackCooldownModifier = Mathf.Clamp(genome.attackCooldownModifier + Random.Range(-mutationStrength * 0.3f, mutationStrength * 0.3f), 0.5f, 2f);
        if (Random.value < mutationRate) genome.damageMultiplier = Mathf.Clamp(genome.damageMultiplier + Random.Range(-mutationStrength * 0.2f, mutationStrength * 0.2f), 0.5f, 1.5f);
    }
}