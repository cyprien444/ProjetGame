#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Test de regles en memoire : aucune lecture/ecriture des sauvegardes du joueur.
public static class TestPromotionsHeros
{
    [MenuItem("Projet Game/Progression/2 - Tester promotions (sans modifier la sauvegarde)")]
    private static void TesterPromotions()
    {
        bool toutValide = true;
        toutValide &= Comparer("Plafond 1 etoile", ProgressionHeros.PlafondNiveau(1), 20);
        toutValide &= Comparer("Plafond 2 etoiles", ProgressionHeros.PlafondNiveau(2), 40);
        toutValide &= Comparer("Plafond 3 etoiles", ProgressionHeros.PlafondNiveau(3), 60);
        toutValide &= Comparer("Plafond 4 etoiles", ProgressionHeros.PlafondNiveau(4), 80);
        toutValide &= Comparer("Plafond 5 etoiles", ProgressionHeros.PlafondNiveau(5), 100);

        int[] couts = { 1000, 10000, 20000, 50000 };
        for (int etoiles = 1; etoiles <= 4; etoiles++)
            toutValide &= Comparer("Cout promotion " + etoiles + "->" + (etoiles + 1),
                ProgressionHeros.CoutPromotionPropose(etoiles), couts[etoiles - 1]);
        toutValide &= Comparer("100 000 reserve", ProgressionHeros.Regles.coutsPromotionProposes[4], 100000);

        // Ne peut pas promouvoir avant le plafond.
        var tropTot = Etat(19, 1, 25);
        int solde = 10000;
        toutValide &= Resultat("Promotion avant niveau 20 refusee",
            ProgressionHeros.SimulerPromotion(tropTot, ref solde), ResultatPromotionHeros.NiveauInsuffisant);
        toutValide &= Comparer("Etoile inchangee (niveau insuffisant)", tropTot.Etoiles, 1);
        toutValide &= Comparer("Solde intact (niveau insuffisant)", solde, 10000);

        // Solde trop faible : aucune mutation.
        var pauvre = Etat(20, 1, 0);
        solde = 999;
        toutValide &= Resultat("Promotion refusee si 999 or",
            ProgressionHeros.SimulerPromotion(pauvre, ref solde), ResultatPromotionHeros.SoldeInsuffisant);
        toutValide &= Comparer("Etoiles intactes (or insuffisant)", pauvre.Etoiles, 1);
        toutValide &= Comparer("Solde intact (or insuffisant)", solde, 999);

        // Chaque rang : verifier cout, etoile, niveau preserve et XP vide.
        for (int etoiles = 1; etoiles <= 4; etoiles++)
        {
            int ancienNiveau = ProgressionHeros.PlafondNiveau(etoiles);
            var h = Etat(ancienNiveau, etoiles, 0);
            solde = couts[etoiles - 1] + 123;
            toutValide &= Resultat("Promotion reussie " + etoiles + "->" + (etoiles + 1),
                ProgressionHeros.SimulerPromotion(h, ref solde), ResultatPromotionHeros.Reussie);
            toutValide &= Comparer("Nouveau nombre d'etoiles", h.Etoiles, etoiles + 1);
            toutValide &= Comparer("Niveau conserve", h.Niveau, ancienNiveau);
            toutValide &= Comparer("XP remise a zero", h.Experience, 0);
            toutValide &= Comparer("Montant exact debite", solde, 123);
            toutValide &= Comparer("Nouveau plafond", ProgressionHeros.PlafondNiveau(h.Etoiles),
                ProgressionHeros.Regles.plafondsNiveau[etoiles]);
        }

        // Rang 5 : promotion impossible, meme au niveau 100 et avec beaucoup d'or.
        var maximal = Etat(100, 5, 0);
        solde = 500000;
        toutValide &= Resultat("5 etoiles = aucune promotion",
            ProgressionHeros.SimulerPromotion(maximal, ref solde), ResultatPromotionHeros.EtoilesMaximum);
        toutValide &= Comparer("Solde intact au maximum", solde, 500000);

        // Les regles de progression/statistiques restent identiques.
        toutValide &= Comparer("XP 1->2", ProgressionHeros.XpPourNiveauSuivant(1), 100);
        toutValide &= Comparer("XP 2->3", ProgressionHeros.XpPourNiveauSuivant(2), 150);
        toutValide &= Comparer("XP 99->100", ProgressionHeros.XpPourNiveauSuivant(99), 68000);
        toutValide &= Proche("Coefficient niveau 100 x3", ProgressionHeros.CoefficientStats(100), 3f);

        if (toutValide)
            Debug.Log("[PROGRESSION ETAPE 8.5] TOUS LES TESTS SONT REUSSIS. Promotions en memoire uniquement : aucune sauvegarde ni monnaie reelle modifiee.");
        else
            Debug.LogError("[PROGRESSION ETAPE 8.5] AU MOINS UN TEST A ECHOUE. Voir les erreurs au-dessus.");
    }

    private static EtatProgressionHero Etat(int niveau, int etoiles, int xp)
    {
        return new EtatProgressionHero { IndexPersonnage = 0, Niveau = niveau, Etoiles = etoiles, Experience = xp };
    }

    private static bool Comparer(string description, int obtenu, int attendu)
    {
        if (obtenu == attendu) return true;
        Debug.LogError("[PROMOTION] " + description + " : attendu " + attendu + ", obtenu " + obtenu);
        return false;
    }

    private static bool Resultat(string description, ResultatPromotionHeros obtenu, ResultatPromotionHeros attendu)
    {
        if (obtenu == attendu) return true;
        Debug.LogError("[PROMOTION] " + description + " : attendu " + attendu + ", obtenu " + obtenu);
        return false;
    }

    private static bool Proche(string description, float obtenu, float attendu)
    {
        if (Mathf.Abs(obtenu - attendu) < 0.0001f) return true;
        Debug.LogError("[PROMOTION] " + description + " : attendu " + attendu + ", obtenu " + obtenu);
        return false;
    }
}
#endif
