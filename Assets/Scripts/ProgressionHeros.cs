using System;
using UnityEngine;

// Etape 8.5 : calcul XP et moteur de promotion des etoiles.
// La monnaie reste provisoire ; l'interface de promotion n'est pas encore branchee.
// - Sauvegarde LOCALE provisoire PlayerPrefs ; sauvegarde de compte prevue plus tard.
// - Pas de changement des statistiques de combat ni de l'interface a cette etape.
// - Quatre promotions (1 a 5 etoiles) ; le cinquieme montant reste reserve.
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
    public int xpLineaire = 50;
    // Courbe : 100 + 50*(N-1) + round(63000*(N-1)*(N-2)/9506).
    // => 1->2 : 100 XP ; 2->3 : 150 XP ; 99->100 : 68000 XP.
    public int xpCourbureNumerateur = 63000;
    public int xpCourbureDenominateur = 9506;
    public float multiplicateurStatNiveau100 = 3f;
    // Couts envisages (or provisoire). 4 promotions seulement : la 5e valeur reste reservee.
    public int[] coutsPromotionProposes = { 1000, 10000, 20000, 50000, 100000 };
}

// Etats de retour de la promotion (utilisables plus tard dans l'interface).
public enum ResultatPromotionHeros
{
    Reussie,
    PersonnageNonPossede,
    EtatInvalide,
    NiveauInsuffisant,
    EtoilesMaximum,
    SoldeInsuffisant
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
            r.xpBase <= 0 || r.xpLineaire < 0 ||
            r.xpCourbureNumerateur < 0 || r.xpCourbureDenominateur <= 0 ||
            r.multiplicateurStatNiveau100 < 1f ||
            r.coutsPromotionProposes == null || r.coutsPromotionProposes.Length != 5) return false;
        for (int i = 0; i < r.coutsPromotionProposes.Length; i++)
            if (r.coutsPromotionProposes[i] <= 0) return false;
        for (int i = 1; i < 5; i++)
            if (r.plafondsNiveau[i] <= r.plafondsNiveau[i - 1]) return false;
        return true;
    }

    public static int PlafondNiveau(int etoiles)
    {
        return Regles.plafondsNiveau[Mathf.Clamp(etoiles, 1, 5) - 1];
    }

    // XP de la barre de progression du niveau actuel vers le suivant.
    // Niveau 1 -> 2 : 100 ; 2 -> 3 : 150 ; 99 -> 100 : 68000 exactement.
    public static int XpPourNiveauSuivant(int niveau)
    {
        if (niveau >= 100) return 0;
        int n = Mathf.Max(1, niveau) - 1;
        long numerateur = (long)Regles.xpCourbureNumerateur * n * Math.Max(0, n - 1);
        long denominateur = Regles.xpCourbureDenominateur;
        long termeCourbe = (numerateur + denominateur / 2) / denominateur;
        long resultat = (long)Regles.xpBase + (long)Regles.xpLineaire * n + termeCourbe;
        return (int)Math.Min(int.MaxValue, resultat);
    }

    // Pour le moment on ne DEPENSE aucune monnaie : les couts sont des propositions.
    // Rang 1 -> 2 : 1000 ; 2 -> 3 : 10000 ; 3 -> 4 : 20000 ; 4 -> 5 : 50000.
    // Le cinquieme montant (100000) reste reserve pour une decision ulterieure.
    public static int CoutPromotionPropose(int etoilesActuelles)
    {
        if (etoilesActuelles < 1 || etoilesActuelles >= 5) return 0;
        return Regles.coutsPromotionProposes[etoilesActuelles - 1];
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

    // Simulation pure (a part les parametres passes en reference) :
    // ne lit pas PlayerPrefs, ne modifie pas DonneesJoueur.Or, ne sauvegarde rien.
    // Pour les tests, utiliser un EtatProgressionHero et un solde de COPIE.
    public static ResultatPromotionHeros SimulerPromotion(EtatProgressionHero etat, ref int solde)
    {
        if (etat == null || etat.Etoiles < 1 || etat.Etoiles > 5 || etat.Niveau < 1)
            return ResultatPromotionHeros.EtatInvalide;

        if (etat.Etoiles >= 5)
            return ResultatPromotionHeros.EtoilesMaximum;

        // Pour promouvoir, le personnage doit etre au plafond de ses etoiles.
        int plafond = PlafondNiveau(etat.Etoiles);
        if (etat.Niveau < plafond)
            return ResultatPromotionHeros.NiveauInsuffisant;

        int cout = CoutPromotionPropose(etat.Etoiles);
        if (cout <= 0) return ResultatPromotionHeros.EtatInvalide;
        if (solde < cout) return ResultatPromotionHeros.SoldeInsuffisant;

        // Succes : on ne remet PAS le niveau a 1.
        solde -= cout;
        etat.Etoiles++;
        etat.Experience = 0;
        return ResultatPromotionHeros.Reussie;
    }

    // Point d'entree pour une future interface de promotion :
    // sauvegarde les nouvelles etoiles dans PlayerPrefs, et retourne le solde reduit
    // via 'ref'. L'appelant doit ensuite mettre a jour ET sauvegarder sa monnaie
    // (par exemple DonneesJoueur.Or) dans le futur systeme de portefeuille.
    // NE PAS appeler ce point d'entree tant que le portefeuille n'est pas raccorde.
    public static ResultatPromotionHeros TenterPromotion(int indexPersonnage, ref int solde)
    {
        if (indexPersonnage < 0 || !DonneesJoueur.Possede(indexPersonnage))
            return ResultatPromotionHeros.PersonnageNonPossede;

        EtatProgressionHero etat = ChargerInterne(indexPersonnage);
        int nouveauSolde = solde;
        ResultatPromotionHeros resultat = SimulerPromotion(etat, ref nouveauSolde);
        if (resultat != ResultatPromotionHeros.Reussie) return resultat;

        // Enregistrer la promotion seulement apres verification de toutes les conditions.
        EnregistrerInterne(etat);
        solde = nouveauSolde;
        return resultat;
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

        long xpTotale = (long)etat.Experience + quantiteXp;
        int necessaire = XpPourNiveauSuivant(etat.Niveau);
        if (necessaire > 0 && xpTotale >= necessaire)
        {
            // Choix du jeu : a CHAQUE niveau gagne, la jauge revient a 0.
            // Le surplus de cette attribution est perdu (aucun report).
            etat.Niveau++;
            etat.Experience = 0;
            return 1;
        }

        // Le heros n'a pas atteint son prochain niveau : conserver son XP courante.
        etat.Experience = (int)Math.Min(int.MaxValue, xpTotale);
        return 0;
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
