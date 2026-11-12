// Passage temporaire des equipes entre la scene Equipe et la scene Combat.
// Compatible avec les indices des personnages dans persos.json.
public static class PreparationCombatEtat
{
    public static bool EstPret = false;
    public static int[] EquipeAlliee = { -1, -1, -1, -1, -1 };
    public static int[] EquipeEnnemie = { 8, 4, 5, 13, 7 }; // equipe de demo du moteur actuel
    public static int PositionMeneur = -1;
    public static string Mode = "Combat d'essai";
}
