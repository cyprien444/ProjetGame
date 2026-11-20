#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class TestProgressionHeros
{
    [MenuItem("Projet Game/Progression/1 - Tester XP (sans modifier la sauvegarde)")]
    private static void Tester()
    {
        bool valide = true;
        valide &= Verifier("XP 1 vers 2", ProgressionHeros.XpPourNiveauSuivant(1), 100);
        valide &= Verifier("XP 2 vers 3", ProgressionHeros.XpPourNiveauSuivant(2), 150);
        valide &= Verifier("XP 10 vers 11", ProgressionHeros.XpPourNiveauSuivant(10), 1027);
        valide &= Verifier("XP 20 vers 21", ProgressionHeros.XpPourNiveauSuivant(20), 3317);
        valide &= Verifier("XP 99 vers 100", ProgressionHeros.XpPourNiveauSuivant(99), 68000);
        valide &= Verifier("Niveau 100 : aucun XP suivant", ProgressionHeros.XpPourNiveauSuivant(100), 0);
        valide &= Verifier("Plafond 1 etoile", ProgressionHeros.PlafondNiveau(1), 20);
        valide &= Verifier("Plafond 5 etoiles", ProgressionHeros.PlafondNiveau(5), 100);
        valide &= Verifier("Promotion 1 vers 2, cout propose", ProgressionHeros.CoutPromotionPropose(1), 1000);
        valide &= Verifier("Promotion 2 vers 3, cout propose", ProgressionHeros.CoutPromotionPropose(2), 10000);
        valide &= Verifier("Promotion 3 vers 4, cout propose", ProgressionHeros.CoutPromotionPropose(3), 20000);
        valide &= Verifier("Promotion 4 vers 5, cout propose", ProgressionHeros.CoutPromotionPropose(4), 50000);
        valide &= Verifier("Aucune promotion depuis 5 etoiles", ProgressionHeros.CoutPromotionPropose(5), 0);
        valide &= Verifier("Montant 100000 conserve en reserve", ProgressionHeros.Regles.coutsPromotionProposes[4], 100000);

        var heros = new EtatProgressionHero { IndexPersonnage = 0, Niveau = 1, Etoiles = 1, Experience = 0 };
        int niveaux = ProgressionHeros.SimulerGainExperience(heros, 100);
        valide &= Verifier("100 XP : niveau 2", heros.Niveau, 2);
        valide &= Verifier("100 XP : jauge remise a 0", heros.Experience, 0);
        valide &= Verifier("100 XP : un niveau gagne", niveaux, 1);

        niveaux = ProgressionHeros.SimulerGainExperience(heros, 80);
        valide &= Verifier("Niveau 2 : XP partielle", heros.Experience, 80);
        valide &= Verifier("Niveau 2 : pas encore de niveau", niveaux, 0);
        niveaux = ProgressionHeros.SimulerGainExperience(heros, 70);
        valide &= Verifier("150 XP cumulees : niveau 3", heros.Niveau, 3);
        valide &= Verifier("Niveau 3 : jauge remise a 0", heros.Experience, 0);
        valide &= Verifier("Niveau 3 : un niveau gagne", niveaux, 1);

        // Surplus perdu : la barre repart a 0, meme avec un gros gain d'XP.
        niveaux = ProgressionHeros.SimulerGainExperience(heros, 10000);
        valide &= Verifier("Gain enorme : un seul niveau", niveaux, 1);
        valide &= Verifier("Surplus de XP non conserve", heros.Experience, 0);

        var auPlafond = new EtatProgressionHero { IndexPersonnage = 0, Niveau = 20, Etoiles = 1, Experience = 0 };
        ProgressionHeros.SimulerGainExperience(auPlafond, 1000000);
        valide &= Verifier("Bloque au niveau 20", auPlafond.Niveau, 20);
        valide &= Verifier("Pas de reserve XP au plafond", auPlafond.Experience, 0);
        auPlafond.Etoiles = 2; // simulation seulement : pas de promotion gratuite dans le jeu.
        ProgressionHeros.SimulerGainExperience(auPlafond, 3317);
        valide &= Verifier("Avec 2 etoiles : passage niveau 21", auPlafond.Niveau, 21);
        valide &= Verifier("Nouvelle jauge vide", auPlafond.Experience, 0);

        var dernierNiveau = new EtatProgressionHero { IndexPersonnage = 0, Niveau = 99, Etoiles = 5, Experience = 0 };
        ProgressionHeros.SimulerGainExperience(dernierNiveau, 68000);
        valide &= Verifier("Passage niveau 100", dernierNiveau.Niveau, 100);
        valide &= Verifier("Niveau 100 : XP reste a 0", dernierNiveau.Experience, 0);

        valide &= VerifierApprox("Stats niveau 1 = x1", ProgressionHeros.CoefficientStats(1), 1f);
        valide &= VerifierApprox("Stats niveau 50", ProgressionHeros.CoefficientStats(50), 1f + 2f * 49f / 99f);
        valide &= VerifierApprox("Stats niveau 100 = x3", ProgressionHeros.CoefficientStats(100), 3f);

        if (valide)
            Debug.Log("[PROGRESSION ETAPE 8.4] TOUS LES TESTS SONT REUSSIS. XP 100/150/68000, remise a zero apres chaque niveau, stats x3 max. Aucune sauvegarde modifiee.");
        else
            Debug.LogError("[PROGRESSION ETAPE 8.4] ECHEC d'un test. Consultez la Console.");
    }

    private static bool Verifier(string nom, int obtenu, int attendu)
    {
        if (obtenu == attendu) return true;
        Debug.LogError("[PROGRESSION] " + nom + " : attendu " + attendu + ", obtenu " + obtenu);
        return false;
    }

    private static bool VerifierApprox(string nom, float obtenu, float attendu)
    {
        if (Mathf.Abs(obtenu - attendu) < 0.0001f) return true;
        Debug.LogError("[PROGRESSION] " + nom + " : attendu " + attendu + ", obtenu " + obtenu);
        return false;
    }
}
#endif
