using UnityEngine;

public enum Team
{
    Red,
    Blue,
    Neutral
}

public class TeamManager : MonoBehaviour
{
    public static TeamManager Instance;

    [Header("Team Colors")]
    public Color redTeamColor = Color.red;
    public Color blueTeamColor = Color.blue;
    public Color neutralColor = Color.gray;

    [Header("Team Stats")]
    public int redTeamAlive = 0;
    public int blueTeamAlive = 0;
    public int redTeamKills = 0;
    public int blueTeamKills = 0;
    public float redTeamDamageDealt = 0;
    public float blueTeamDamageDealt = 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ResetStats()
    {
        redTeamAlive = 0;
        blueTeamAlive = 0;
        redTeamKills = 0;
        blueTeamKills = 0;
        redTeamDamageDealt = 0;
        blueTeamDamageDealt = 0;
    }

    public void RegisterDeath(Team team)
    {
        if (team == Team.Red)
        {
            redTeamAlive--;
            blueTeamKills++;
        }
        else if (team == Team.Blue)
        {
            blueTeamAlive--;
            redTeamKills++;
        }
    }

    public void RegisterDamage(Team team, float damage)
    {
        if (team == Team.Red)
        {
            redTeamDamageDealt += damage;
        }
        else if (team == Team.Blue)
        {
            blueTeamDamageDealt += damage;
        }
    }

    public Color GetTeamColor(Team team)
    {
        switch (team)
        {
            case Team.Red: return redTeamColor;
            case Team.Blue: return blueTeamColor;
            default: return neutralColor;
        }
    }

    public Team GetWinningTeam()
    {
        if (blueTeamAlive == 0) return Team.Red;
        if (redTeamAlive == 0) return Team.Blue;
        return Team.Neutral;
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 18;
        style.fontStyle = FontStyle.Bold;

        // Red Team Stats
        style.normal.textColor = redTeamColor;
        GUI.Label(new Rect(10, Screen.height - 120, 300, 100),
            $"RED TEAM\n" +
            $"Alive: {redTeamAlive}\n" +
            $"Kills: {redTeamKills}\n" +
            $"Damage: {redTeamDamageDealt:F0}",
            style);

        // Blue Team Stats
        style.normal.textColor = blueTeamColor;
        GUI.Label(new Rect(Screen.width - 310, Screen.height - 120, 300, 100),
            $"BLUE TEAM\n" +
            $"Alive: {blueTeamAlive}\n" +
            $"Kills: {blueTeamKills}\n" +
            $"Damage: {blueTeamDamageDealt:F0}",
            style);

        // Winner announcement
        Team winner = GetWinningTeam();
        if (winner != Team.Neutral)
        {
            style.normal.textColor = GetTeamColor(winner);
            style.fontSize = 48;
            GUI.Label(new Rect(Screen.width / 2 - 200, Screen.height / 2 - 50, 400, 100),
                $"{winner.ToString().ToUpper()} TEAM WINS!",
                style);
        }
    }
}