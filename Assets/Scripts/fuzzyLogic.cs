using UnityEngine;
using System.Collections.Generic;

public class FuzzyLogic
{
    // ==========================================
    // FUZZY SET DEFINITIONS
    // ==========================================
    
    // Membership function types
    public enum MembershipType
    {
        Triangular,
        Trapezoidal,
        Gaussian,
        Sigmoid
    }

    // Base fuzzy set
    public class FuzzySet
    {
        public string name;
        protected float min, max, peak1, peak2;
        protected MembershipType type;

        // Constructor for Triangular (3 points)
        public FuzzySet(string _name, float _min, float _peak, float _max)
        {
            name = _name;
            min = _min;
            max = _max;
            peak1 = _peak;
            peak2 = _peak;
            type = MembershipType.Triangular;
        }

        // Constructor for Trapezoidal (4 points) - MUST use CreateTrapezoidal
        public static FuzzySet CreateTrapezoidal(string _name, float _min, float _peak1, float _peak2, float _max)
        {
            FuzzySet set = new FuzzySet(_name, _min, (_peak1 + _peak2) / 2f, _max);
            set.peak1 = _peak1;
            set.peak2 = _peak2;
            set.type = MembershipType.Trapezoidal;
            return set;
        }

        // Constructor for Gaussian
        public static FuzzySet CreateGaussian(string _name, float _mean, float _sigma, float _min, float _max)
        {
            FuzzySet set = new FuzzySet(_name, _min, _mean, _max);
            set.type = MembershipType.Gaussian;
            return set;
        }

        // Calculate membership value (0 to 1)
        public virtual float GetMembership(float value)
        {
            switch (type)
            {
                case MembershipType.Triangular:
                    return TriangularMembership(value, min, peak1, max);
                case MembershipType.Trapezoidal:
                    return TrapezoidalMembership(value, min, peak1, peak2, max);
                case MembershipType.Gaussian:
                    return GaussianMembership(value, peak1, (max - min) / 4f);
                default:
                    return TriangularMembership(value, min, peak1, max);
            }
        }

        float TriangularMembership(float x, float a, float b, float c)
        {
            if (x <= a || x >= c) return 0f;
            if (x == b) return 1f;
            if (x < b) return (x - a) / (b - a);
            return (c - x) / (c - b);
        }

        float TrapezoidalMembership(float x, float a, float b, float c, float d)
        {
            if (x <= a || x >= d) return 0f;
            if (x >= b && x <= c) return 1f;
            if (x < b) return (x - a) / (b - a);
            return (d - x) / (d - c);
        }

        float GaussianMembership(float x, float mean, float sigma)
        {
            return Mathf.Exp(-Mathf.Pow(x - mean, 2) / (2 * sigma * sigma));
        }
    }

    // ==========================================
    // LINGUISTIC VARIABLES
    // ==========================================
    
    public class LinguisticVariable
    {
        public string name;
        public float minValue;
        public float maxValue;
        public Dictionary<string, FuzzySet> sets;

        public LinguisticVariable(string _name, float _min, float _max)
        {
            name = _name;
            minValue = _min;
            maxValue = _max;
            sets = new Dictionary<string, FuzzySet>();
        }

        public void AddSet(FuzzySet set)
        {
            sets[set.name] = set;
        }

        public float GetMembership(string setName, float value)
        {
            if (sets.ContainsKey(setName))
            {
                return sets[setName].GetMembership(value);
            }
            return 0f;
        }

        // Fuzzify a crisp value - returns all memberships
        public Dictionary<string, float> Fuzzify(float value)
        {
            Dictionary<string, float> memberships = new Dictionary<string, float>();
            foreach (var kvp in sets)
            {
                memberships[kvp.Key] = kvp.Value.GetMembership(value);
            }
            return memberships;
        }
    }

    // ==========================================
    // FUZZY RULES
    // ==========================================
    
    public class FuzzyRule
    {
        public string name;
        public List<Condition> conditions;
        public Conclusion conclusion;
        public float weight = 1f;

        public FuzzyRule(string _name)
        {
            name = _name;
            conditions = new List<Condition>();
        }

        public void AddCondition(string variable, string fuzzySet)
        {
            conditions.Add(new Condition(variable, fuzzySet));
        }

        public void SetConclusion(string variable, string fuzzySet)
        {
            conclusion = new Conclusion(variable, fuzzySet);
        }

        // Evaluate rule strength using AND (minimum)
        public float Evaluate(Dictionary<string, Dictionary<string, float>> fuzzifiedInputs)
        {
            float minMembership = 1f;

            foreach (var condition in conditions)
            {
                if (fuzzifiedInputs.ContainsKey(condition.variable) &&
                    fuzzifiedInputs[condition.variable].ContainsKey(condition.fuzzySet))
                {
                    float membership = fuzzifiedInputs[condition.variable][condition.fuzzySet];
                    minMembership = Mathf.Min(minMembership, membership);
                }
                else
                {
                    return 0f; // Condition not met
                }
            }

            return minMembership * weight;
        }
    }

    public class Condition
    {
        public string variable;
        public string fuzzySet;

        public Condition(string _var, string _set)
        {
            variable = _var;
            fuzzySet = _set;
        }
    }

    public class Conclusion
    {
        public string variable;
        public string fuzzySet;

        public Conclusion(string _var, string _set)
        {
            variable = _var;
            fuzzySet = _set;
        }
    }

    // ==========================================
    // FUZZY INFERENCE ENGINE
    // ==========================================
    
    public class FuzzyInferenceSystem
    {
        public Dictionary<string, LinguisticVariable> inputVariables;
        public Dictionary<string, LinguisticVariable> outputVariables;
        public List<FuzzyRule> rules;

        public FuzzyInferenceSystem()
        {
            inputVariables = new Dictionary<string, LinguisticVariable>();
            outputVariables = new Dictionary<string, LinguisticVariable>();
            rules = new List<FuzzyRule>();
        }

        public void AddInputVariable(LinguisticVariable variable)
        {
            inputVariables[variable.name] = variable;
        }

        public void AddOutputVariable(LinguisticVariable variable)
        {
            outputVariables[variable.name] = variable;
        }

        public void AddRule(FuzzyRule rule)
        {
            rules.Add(rule);
        }

        // Main inference method
        public Dictionary<string, float> Infer(Dictionary<string, float> crispInputs)
        {
            // Step 1: Fuzzification
            Dictionary<string, Dictionary<string, float>> fuzzifiedInputs = new Dictionary<string, Dictionary<string, float>>();
            
            foreach (var input in crispInputs)
            {
                if (inputVariables.ContainsKey(input.Key))
                {
                    fuzzifiedInputs[input.Key] = inputVariables[input.Key].Fuzzify(input.Value);
                }
            }

            // Step 2: Rule Evaluation
            Dictionary<string, Dictionary<string, float>> outputAggregation = new Dictionary<string, Dictionary<string, float>>();

            foreach (var rule in rules)
            {
                float ruleStrength = rule.Evaluate(fuzzifiedInputs);
                
                if (ruleStrength > 0f && rule.conclusion != null)
                {
                    string outVar = rule.conclusion.variable;
                    string outSet = rule.conclusion.fuzzySet;

                    if (!outputAggregation.ContainsKey(outVar))
                    {
                        outputAggregation[outVar] = new Dictionary<string, float>();
                    }

                    // Use maximum for aggregation
                    if (!outputAggregation[outVar].ContainsKey(outSet))
                    {
                        outputAggregation[outVar][outSet] = ruleStrength;
                    }
                    else
                    {
                        outputAggregation[outVar][outSet] = Mathf.Max(
                            outputAggregation[outVar][outSet],
                            ruleStrength
                        );
                    }
                }
            }

            // Step 3: Defuzzification (Center of Gravity)
            Dictionary<string, float> crispOutputs = new Dictionary<string, float>();

            foreach (var outVar in outputAggregation)
            {
                if (outputVariables.ContainsKey(outVar.Key))
                {
                    crispOutputs[outVar.Key] = Defuzzify(
                        outputVariables[outVar.Key],
                        outVar.Value
                    );
                }
            }

            return crispOutputs;
        }

        // Center of Gravity defuzzification
        float Defuzzify(LinguisticVariable variable, Dictionary<string, float> activations)
        {
            float numerator = 0f;
            float denominator = 0f;
            int samples = 100;

            float step = (variable.maxValue - variable.minValue) / samples;

            for (int i = 0; i < samples; i++)
            {
                float x = variable.minValue + i * step;
                float maxMembership = 0f;

                foreach (var activation in activations)
                {
                    if (variable.sets.ContainsKey(activation.Key))
                    {
                        float setMembership = variable.sets[activation.Key].GetMembership(x);
                        float clippedMembership = Mathf.Min(setMembership, activation.Value);
                        maxMembership = Mathf.Max(maxMembership, clippedMembership);
                    }
                }

                numerator += x * maxMembership;
                denominator += maxMembership;
            }

            if (denominator == 0f) return variable.minValue;
            return numerator / denominator;
        }
    }
}