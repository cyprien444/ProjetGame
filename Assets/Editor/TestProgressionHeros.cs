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
        valide &= Verifier("XP 10 vers 11", ProgressionHeros.XpPourNiveauSuivant(10), 442);
        valide &= Verifier("XP 20 vers 21", ProgressionHeros.XpPourNiveauSuivant(20), 1202);
        valide &= Verifier("XP 99 vers 100", ProgressionHeros.XpPourNiveauSuivant(99), 21268);
        valide &= Verifier("Plafond 1 etoile", ProgressionHeros.PlafondNiveau(1), 20);
        valide &= Verifier("Plafond 5 etoiles", ProgressionHeros.PlafondNiveau(5), 100);

        var heros = new EtatProgressionHero { IndexPersonnage = 0, Niveau = 1, Etoiles = 1, Experience = 0 };
        int niveaux = ProgressionHeros.SimulerGainExperience(heros, 100);
        valide &= Verifier("Gain de 100 XP : niveau", heros.Niveau, 2);
        valide &= Verifier("Gain de 100 XP : XP restante", heros.Experience, 0);
        valide &= Verifier("Gain de 100 XP : niveaux gagnes", niveaux, 1);

        ProgressionHeros.SimulerGainExperience(heros, 2000000);
        valide &= Verifier("Blocage au niveau 20", heros.Niveau, 20);
        valide &= Verifier("XP bloquee au niveau 20", heros.Experience, 0);
        ProgressionHeros.SimulerGainExperience(heros, 5000);
        valide &= Verifier("Pas de reserve d'XP au plafond", heros.Experience, 0);

        // Promotion FUTURE : on modifie seulement l'etat temporaire du test.
        // Aucune methode de promotion gratuite n'est exposee dans le jeu.
        heros.Etoiles = 2;
        ProgressionHeros.SimulerGainExperience(heros, 1202);
        valide &= Verifier("Avec 2 etoiles : niveau 21 possible", heros.Niveau, 21);
        valide &= Mathf.Abs(ProgressionHeros.CoefficientStats(1) - 1f) < 0.0001f;
        valide &= Mathf.Abs(ProgressionHeros.CoefficientStats(100) - 3f) < 0.0001f;

        if (valide)
            Debug.Log("[PROGRESSION ETAPE 8.3] TOUS LES TESTS SONT REUSSIS. Aucun heros et aucune sauvegarde n'ont ete modifies.");
        else
            Debug.LogError("[PROGRESSION ETAPE 8.3] ECHEC d'un test. Consultez les lignes precedentes de la Console.");
    }

    private static bool Verifier(string nom, int obtenu, int attendu)
    {
        if (obtenu == attendu) return true;
        Debug.LogError("[PROGRESSION] " + nom + " : attendu " + attendu + ", obtenu " + obtenu);
        return false;
    }
}
#endif
