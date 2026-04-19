using UnityEngine;
using System.Collections.Generic;
using static FuzzyLogic;

public class EnemyFuzzyLogic : MonoBehaviour
{
    private FuzzyInferenceSystem fis;
    
    [Header("Fuzzy System")]
    public bool useFuzzyLogic = true;
    public bool showFuzzyDebug = false;

    [Header("Fuzzy Biases (from Genome)")]
    [Tooltip("Shifts aggression output (-20 to +20)")]
    public float aggressionBias = 0f;
    
    [Tooltip("Shifts caution output (-20 to +20)")]
    public float cautionBias = 0f;
    
    [Tooltip("Shifts flee desire output (-20 to +20)")]
    public float fleeBias = 0f;

    // Cached fuzzy outputs
    [HideInInspector] public float fuzzyThreatLevel = 0f;
    [HideInInspector] public float fuzzyAggressionLevel = 0f;
    [HideInInspector] public float fuzzyCautionLevel = 0f;
    [HideInInspector] public float fuzzyDesireToFlee = 0f;

    void Awake()
    {
        InitializeFuzzySystem();
    }

    void InitializeFuzzySystem()
    {
        fis = new FuzzyInferenceSystem();

        // ==========================================
        // INPUT VARIABLES
        // ==========================================

        // Health (0-100)
        LinguisticVariable health = new LinguisticVariable("Health", 0f, 100f);
        health.AddSet(FuzzySet.CreateTrapezoidal("Critical", 0f, 0f, 20f, 30f));
        health.AddSet(new FuzzySet("Low", 20f, 35f, 50f));
        health.AddSet(new FuzzySet("Medium", 40f, 60f, 80f));
        health.AddSet(FuzzySet.CreateTrapezoidal("High", 70f, 85f, 100f, 100f));
        fis.AddInputVariable(health);

        // Distance to Target (0-20 units)
        LinguisticVariable distance = new LinguisticVariable("Distance", 0f, 20f);
        distance.AddSet(FuzzySet.CreateTrapezoidal("VeryClose", 0f, 0f, 2f, 3f));
        distance.AddSet(new FuzzySet("Close", 2f, 4f, 6f));
        distance.AddSet(new FuzzySet("Medium", 5f, 8f, 11f));
        distance.AddSet(FuzzySet.CreateTrapezoidal("Far", 10f, 15f, 20f, 20f));
        fis.AddInputVariable(distance);

        // Threat Level (enemy scary value, 0-10)
        LinguisticVariable threat = new LinguisticVariable("Threat", 0f, 10f);
        threat.AddSet(FuzzySet.CreateTrapezoidal("Low", 0f, 0f, 3f, 4f));
        threat.AddSet(new FuzzySet("Medium", 3f, 5f, 7f));
        threat.AddSet(FuzzySet.CreateTrapezoidal("High", 6f, 8f, 10f, 10f));
        fis.AddInputVariable(threat);

        // Bravery (0-10)
        LinguisticVariable bravery = new LinguisticVariable("Bravery", 0f, 10f);
        bravery.AddSet(FuzzySet.CreateTrapezoidal("Coward", 0f, 0f, 3f, 4f));
        bravery.AddSet(new FuzzySet("Cautious", 3f, 5f, 7f));
        bravery.AddSet(FuzzySet.CreateTrapezoidal("Brave", 6f, 8f, 10f, 10f));
        fis.AddInputVariable(bravery);

        // Nearby Allies (0-5)
        LinguisticVariable allies = new LinguisticVariable("Allies", 0f, 5f);
        allies.AddSet(FuzzySet.CreateTrapezoidal("None", 0f, 0f, 0f, 1f));
        allies.AddSet(new FuzzySet("Few", 0f, 1f, 2f));
        allies.AddSet(FuzzySet.CreateTrapezoidal("Many", 2f, 3f, 5f, 5f));
        fis.AddInputVariable(allies);

        // Influence/Danger in Area (0-10)
        LinguisticVariable influence = new LinguisticVariable("Influence", 0f, 10f);
        influence.AddSet(FuzzySet.CreateTrapezoidal("Safe", 0f, 0f, 2f, 3f));
        influence.AddSet(new FuzzySet("Risky", 2f, 5f, 8f));
        influence.AddSet(FuzzySet.CreateTrapezoidal("Dangerous", 7f, 9f, 10f, 10f));
        fis.AddInputVariable(influence);

        // ==========================================
        // OUTPUT VARIABLES
        // ==========================================

        // Aggression Level (0-100)
        LinguisticVariable aggression = new LinguisticVariable("Aggression", 0f, 100f);
        aggression.AddSet(FuzzySet.CreateTrapezoidal("Passive", 0f, 0f, 20f, 30f));
        aggression.AddSet(new FuzzySet("Moderate", 25f, 50f, 75f));
        aggression.AddSet(FuzzySet.CreateTrapezoidal("Aggressive", 70f, 90f, 100f, 100f));
        fis.AddOutputVariable(aggression);

        // Caution Level (0-100)
        LinguisticVariable caution = new LinguisticVariable("Caution", 0f, 100f);
        caution.AddSet(FuzzySet.CreateTrapezoidal("Reckless", 0f, 0f, 20f, 30f));
        caution.AddSet(new FuzzySet("Careful", 25f, 50f, 75f));
        caution.AddSet(FuzzySet.CreateTrapezoidal("Paranoid", 70f, 90f, 100f, 100f));
        fis.AddOutputVariable(caution);

        // Desire to Flee (0-100)
        LinguisticVariable flee = new LinguisticVariable("Flee", 0f, 100f);
        flee.AddSet(FuzzySet.CreateTrapezoidal("Stay", 0f, 0f, 20f, 30f));
        flee.AddSet(new FuzzySet("Consider", 25f, 50f, 75f));
        flee.AddSet(FuzzySet.CreateTrapezoidal("Retreat", 70f, 90f, 100f, 100f));
        fis.AddOutputVariable(flee);

        // ==========================================
        // FUZZY RULES
        // ==========================================

        // AGGRESSION RULES
        FuzzyRule rule1 = new FuzzyRule("HighHealthCloseWeakTarget");
        rule1.AddCondition("Health", "High");
        rule1.AddCondition("Distance", "Close");
        rule1.AddCondition("Threat", "Low");
        rule1.SetConclusion("Aggression", "Aggressive");
        fis.AddRule(rule1);

        FuzzyRule rule2 = new FuzzyRule("BraveWithAllies");
        rule2.AddCondition("Bravery", "Brave");
        rule2.AddCondition("Allies", "Many");
        rule2.SetConclusion("Aggression", "Aggressive");
        fis.AddRule(rule2);

        FuzzyRule rule3 = new FuzzyRule("LowHealthPassive");
        rule3.AddCondition("Health", "Low");
        rule3.SetConclusion("Aggression", "Passive");
        fis.AddRule(rule3);

        FuzzyRule rule4 = new FuzzyRule("HighThreatPassive");
        rule4.AddCondition("Threat", "High");
        rule4.AddCondition("Health", "Medium");
        rule4.SetConclusion("Aggression", "Passive");
        fis.AddRule(rule4);

        // CAUTION RULES
        FuzzyRule rule5 = new FuzzyRule("DangerousAreaCautious");
        rule5.AddCondition("Influence", "Dangerous");
        rule5.SetConclusion("Caution", "Paranoid");
        fis.AddRule(rule5);

        FuzzyRule rule6 = new FuzzyRule("LowHealthCautious");
        rule6.AddCondition("Health", "Low");
        rule6.SetConclusion("Caution", "Paranoid");
        fis.AddRule(rule6);

        FuzzyRule rule7 = new FuzzyRule("BraveReckless");
        rule7.AddCondition("Bravery", "Brave");
        rule7.AddCondition("Health", "High");
        rule7.SetConclusion("Caution", "Reckless");
        fis.AddRule(rule7);

        FuzzyRule rule8 = new FuzzyRule("CowardCautious");
        rule8.AddCondition("Bravery", "Coward");
        rule8.SetConclusion("Caution", "Careful");
        fis.AddRule(rule8);

        // FLEE RULES
        FuzzyRule rule9 = new FuzzyRule("CriticalHealthRetreat");
        rule9.AddCondition("Health", "Critical");
        rule9.SetConclusion("Flee", "Retreat");
        fis.AddRule(rule9);

        FuzzyRule rule10 = new FuzzyRule("HighThreatCloseRetreat");
        rule10.AddCondition("Threat", "High");
        rule10.AddCondition("Distance", "VeryClose");
        rule10.AddCondition("Health", "Low");
        rule10.SetConclusion("Flee", "Retreat");
        fis.AddRule(rule10);

        FuzzyRule rule11 = new FuzzyRule("CowardWithHighThreat");
        rule11.AddCondition("Bravery", "Coward");
        rule11.AddCondition("Threat", "High");
        rule11.SetConclusion("Flee", "Retreat");
        fis.AddRule(rule11);

        FuzzyRule rule12 = new FuzzyRule("BraveStay");
        rule12.AddCondition("Bravery", "Brave");
        rule12.AddCondition("Health", "High");
        rule12.SetConclusion("Flee", "Stay");
        fis.AddRule(rule12);

        FuzzyRule rule13 = new FuzzyRule("NoAlliesDangerConsiderFlee");
        rule13.AddCondition("Allies", "None");
        rule13.AddCondition("Influence", "Dangerous");
        rule13.AddCondition("Threat", "Medium");
        rule13.SetConclusion("Flee", "Consider");
        fis.AddRule(rule13);

        FuzzyRule rule14 = new FuzzyRule("ManyAlliesStay");
        rule14.AddCondition("Allies", "Many");
        rule14.AddCondition("Health", "Medium");
        rule14.SetConclusion("Flee", "Stay");
        fis.AddRule(rule14);

        // COMBINED RULES
        FuzzyRule rule15 = new FuzzyRule("HighThreatFarModerateAggression");
        rule15.AddCondition("Threat", "High");
        rule15.AddCondition("Distance", "Far");
        rule15.AddCondition("Bravery", "Brave");
        rule15.SetConclusion("Aggression", "Moderate");
        fis.AddRule(rule15);

        FuzzyRule rule16 = new FuzzyRule("RiskyAreaCareful");
        rule16.AddCondition("Influence", "Risky");
        rule16.SetConclusion("Caution", "Careful");
        fis.AddRule(rule16);
    }

    public void EvaluateFuzzyLogic(
        float health,
        float distanceToTarget,
        float threatLevel,
        float braveryLevel,
        int nearbyAllies,
        float influenceAtPosition)
    {
        if (!useFuzzyLogic) return;

        // Prepare crisp inputs
        Dictionary<string, float> inputs = new Dictionary<string, float>
        {
            { "Health", health },
            { "Distance", distanceToTarget },
            { "Threat", threatLevel },
            { "Bravery", braveryLevel },
            { "Allies", nearbyAllies },
            { "Influence", influenceAtPosition }
        };

        // Run fuzzy inference
        Dictionary<string, float> outputs = fis.Infer(inputs);

        // Store outputs WITH BIASES APPLIED
        if (outputs.ContainsKey("Aggression"))
        {
            fuzzyAggressionLevel = Mathf.Clamp(outputs["Aggression"] + aggressionBias, 0f, 100f);
        }

        if (outputs.ContainsKey("Caution"))
        {
            fuzzyCautionLevel = Mathf.Clamp(outputs["Caution"] + cautionBias, 0f, 100f);
        }

        if (outputs.ContainsKey("Flee"))
        {
            fuzzyDesireToFlee = Mathf.Clamp(outputs["Flee"] + fleeBias, 0f, 100f);
        }

        // Calculate overall threat perception
        fuzzyThreatLevel = (threatLevel * 10f + influenceAtPosition * 10f - (nearbyAllies * 20f)) / 100f;
        fuzzyThreatLevel = Mathf.Clamp01(fuzzyThreatLevel);

        if (showFuzzyDebug)
        {
            Debug.Log($"[Fuzzy] Aggression: {fuzzyAggressionLevel:F1}, Caution: {fuzzyCautionLevel:F1}, Flee: {fuzzyDesireToFlee:F1}");
        }
    }

    public bool ShouldAttack()
    {
        return fuzzyAggressionLevel > 60f;
    }

    public bool ShouldFlee()
    {
        return fuzzyDesireToFlee > 60f;
    }

    public bool ShouldBeCareful()
    {
        return fuzzyCautionLevel > 60f;
    }

    public float GetFuzzySpeedModifier()
    {
        // Higher caution = slower movement
        // Higher aggression = faster movement
        float cautionFactor = Mathf.Lerp(1f, 0.6f, fuzzyCautionLevel / 100f);
        float aggressionFactor = Mathf.Lerp(1f, 1.4f, fuzzyAggressionLevel / 100f);
        return cautionFactor * aggressionFactor;
    }

    public float GetFuzzyInfluenceWeight()
    {
        // Higher caution = more influenced by danger zones
        return Mathf.Lerp(0.5f, 3f, fuzzyCautionLevel / 100f);
    }

    void OnGUI()
    {
        if (!showFuzzyDebug) return;

        GUIStyle style = new GUIStyle();
        style.fontSize = 12;
        style.normal.textColor = Color.white;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 3f);
        
        if (screenPos.z > 0)
        {
            GUI.Label(new Rect(screenPos.x, Screen.height - screenPos.y, 200, 100),
                $"Aggression: {fuzzyAggressionLevel:F0}\n" +
                $"Caution: {fuzzyCautionLevel:F0}\n" +
                $"Flee Desire: {fuzzyDesireToFlee:F0}",
                style);
        }
    }
}