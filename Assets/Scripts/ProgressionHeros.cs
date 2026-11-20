using System;
using UnityEngine;

// Etape 8.3 : progression individuelle des heros (XP, niveau, etoiles).
// - Sauvegarde LOCALE provisoire PlayerPrefs ; sauvegarde de compte prevue plus tard.
// - Pas de changement des statistiques de combat ni de l'interface a cette etape.
// - Pas de promotion d'etoiles avant la definition de son cout.
// - Utilise DonneesJoueur.Possede(index) sans modifier DonneesJoueur.cs.

[Serializable]
public class EtatProgressionHero
{
    public int IndexPersonnage;
    public int Niveau = 1;
    public int Etoiles = 1;
    public int Experience = 0;
}

[Serializable]
public class ReglesProgressionHeros
{
    public int[] plafondsNiveau = { 20, 40, 60, 80, 100 };
    public int xpBase = 100;
    public int xpLineaire = 20;
    public int xpQuadratique = 2;
    public float multiplicateurStatNiveau100 = 3f;
}

public static class ProgressionHeros
{
    private const string ClePrefixe = "ProjetGame.ProgressionHeros.v1.";
    private static ReglesProgressionHeros regles;

    public static ReglesProgressionHeros Regles
    {
        get
        {
            if (regles == null)
            {
                regles = new ReglesProgressionHeros();
                TextAsset fichier = Resources.Load<TextAsset>("Donnees/progression");
                if (fichier != null)
                {
                    try
                    {
                        ReglesProgressionHeros chargees = JsonUtility.FromJson<ReglesProgressionHeros>(fichier.text);
                        if (ReglesValides(chargees)) regles = chargees;
                        else Debug.LogWarning("Progression : progression.json invalide, utilisation des valeurs par defaut.");
                    }
                    catch (Exception exception)
                    {
                        Debug.LogWarning("Progression : lecture de progression.json impossible : " + exception.Message);
                    }
                }
            }
            return regles;
        }
    }

    private static bool ReglesValides(ReglesProgressionHeros r)
    {
        if (r == null || r.plafondsNiveau == null || r.plafondsNiveau.Length != 5 ||
            r.plafondsNiveau[0] < 1 || r.plafondsNiveau[4] != 100 ||
            r.xpBase <= 0 || r.xpLineaire < 0 || r.xpQuadratique < 0 ||
            r.multiplicateurStatNiveau100 < 1f) return false;
        for (int i = 1; i < 5; i++)
            if (r.plafondsNiveau[i] <= r.plafondsNiveau[i - 1]) return false;
        return true;
    }

    public static int PlafondNiveau(int etoiles)
    {
        return Regles.plafondsNiveau[Mathf.Clamp(etoiles, 1, 5) - 1];
    }

    // XP requise pour passer du niveau "niveau" a "niveau + 1".
    // Au niveau 100, aucune progression supplementaire n'existe.
    public static int XpPourNiveauSuivant(int niveau)
    {
        if (niveau >= 100) return 0;
        int n = Mathf.Max(1, niveau) - 1;
        long resultat = (long)Regles.xpBase + (long)Regles.xpLineaire * n +
                        (long)Regles.xpQuadratique * n * n;
        return (int)Math.Min(int.MaxValue, resultat);
    }

    // Sera applique plus tard aux PV, ATQ et DEF par le moteur de combat.
    // Au niveau 1 : x1 ; au niveau 100 : x3 (avec les regles validees).
    public static float CoefficientStats(int niveau)
    {
        float avancee = (Mathf.Clamp(niveau, 1, 100) - 1f) / 99f;
        return Mathf.Lerp(1f, Regles.multiplicateurStatNiveau100, avancee);
    }

    // Lecture du niveau d'un personnage possede. Retourne null s'il n'est pas possede.
    // L'objet retourne est une copie : il ne suffit pas de modifier ses champs pour sauvegarder.
    public static EtatProgressionHero Lire(int indexPersonnage)
    {
        if (indexPersonnage < 0 || !DonneesJoueur.Possede(indexPersonnage)) return null;
        return Copier(ChargerInterne(indexPersonnage));
    }

    public static bool EstAuPlafond(int indexPersonnage)
    {
        EtatProgressionHero etat = Lire(indexPersonnage);
        return etat != null && etat.Niveau >= PlafondNiveau(etat.Etoiles);
    }

    public static bool PeutEtrePromu(int indexPersonnage)
    {
        EtatProgressionHero etat = Lire(indexPersonnage);
        return etat != null && etat.Etoiles < 5 &&
               etat.Niveau >= PlafondNiveau(etat.Etoiles);
    }

    // Ajoute de l'XP et retourne le nombre de niveaux gagnes.
    // IMPORTANT : au plafond de l'etoile courante, l'XP excedentaire est PERDUE,
    // meme si elle aurait suffi a monter plusieurs niveaux.
    public static int AjouterExperience(int indexPersonnage, int quantiteXp)
    {
        if (indexPersonnage < 0 || !DonneesJoueur.Possede(indexPersonnage) || quantiteXp <= 0)
            return 0;

        EtatProgressionHero etat = ChargerInterne(indexPersonnage);
        int niveauxGagnes = SimulerGainExperience(etat, quantiteXp);
        EnregistrerInterne(etat);
        return niveauxGagnes;
    }

    // Simulation en memoire : utile pour les tests, sans modifier la sauvegarde.
    public static int SimulerGainExperience(EtatProgressionHero etat, int quantiteXp)
    {
        if (etat == null || quantiteXp <= 0) return 0;
        Normaliser(etat);

        int plafond = PlafondNiveau(etat.Etoiles);
        if (etat.Niveau >= plafond)
        {
            etat.Experience = 0;
            return 0;
        }

        long xpRestante = (long)etat.Experience + quantiteXp;
        int niveauxGagnes = 0;
        while (etat.Niveau < plafond)
        {
            int necessaire = XpPourNiveauSuivant(etat.Niveau);
            if (necessaire <= 0 || xpRestante < necessaire) break;
            xpRestante -= necessaire;
            etat.Niveau++;
            niveauxGagnes++;
        }

        // Aucun stockage d'XP au plafond, conformement a la regle validee.
        etat.Experience = etat.Niveau >= plafond ? 0 : (int)Math.Min(int.MaxValue, xpRestante);
        return niveauxGagnes;
    }

    private static EtatProgressionHero ChargerInterne(int index)
    {
        EtatProgressionHero etat = null;
        string cle = ClePrefixe + index;
        if (PlayerPrefs.HasKey(cle))
        {
            try { etat = JsonUtility.FromJson<EtatProgressionHero>(PlayerPrefs.GetString(cle)); }
            catch (Exception e)
            {
                Debug.LogWarning("Progression : sauvegarde illisible pour le perso " + index + " : " + e.Message);
            }
        }

        if (etat == null)
            etat = new EtatProgressionHero { IndexPersonnage = index, Niveau = 1, Etoiles = 1, Experience = 0 };

        etat.IndexPersonnage = index;
        Normaliser(etat);
        return etat;
    }

    private static void Normaliser(EtatProgressionHero etat)
    {
        etat.Etoiles = Mathf.Clamp(etat.Etoiles, 1, 5);
        etat.Niveau = Mathf.Clamp(etat.Niveau, 1, PlafondNiveau(etat.Etoiles));
        etat.Experience = Mathf.Max(0, etat.Experience);
        if (etat.Niveau >= PlafondNiveau(etat.Etoiles)) etat.Experience = 0;
        else etat.Experience = Mathf.Min(etat.Experience, XpPourNiveauSuivant(etat.Niveau) - 1);
    }

    private static void EnregistrerInterne(EtatProgressionHero etat)
    {
        PlayerPrefs.SetString(ClePrefixe + etat.IndexPersonnage, JsonUtility.ToJson(etat));
        PlayerPrefs.Save();
    }

    private static EtatProgressionHero Copier(EtatProgressionHero original)
    {
        return new EtatProgressionHero
        {
            IndexPersonnage = original.IndexPersonnage,
            Niveau = original.Niveau,
            Etoiles = original.Etoiles,
            Experience = original.Experience
        };
    }
}
