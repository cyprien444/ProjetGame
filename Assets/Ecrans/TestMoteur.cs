using UnityEngine;
using ProjetGame.Moteur;

// Script de test : lance des combats avec le moteur et affiche le résultat dans la Console de Unity.
public class TestMoteur : MonoBehaviour
{
    void Start()
    {
        // Les équipes sont des numéros de persos (index dans Donnees.Persos, le numéro 1 du doc est le 0).
        int[] equipeA = { 12, 0, 9, 10, 11 };   // Valdis, Garrick, Ren, Aurélien, Naïa
        int[] equipeB = { 8, 4, 5, 13, 7 };     // Isaure, Hakon, Kenzo, Vesper, Anselm
        int meneurA = 3;                        // position du meneur : Aurélien
        int meneurB = 3;                        // Vesper
        int niveauReliques = 12;                // 0 = sans Reliques, 6 ou 12

        // Un combat avec la graine 1, journal complet : on affiche les 20 premières lignes.
        Resultat r = Simulateur.Combattre(equipeA, meneurA, equipeB, meneurB, 1, niveauReliques, true, true);
        for (int i = 0; i < Mathf.Min(20, r.Journal.Count); i++)
        {
            Debug.Log(r.Journal[i].EnTexte());
        }
        Debug.Log("Combat (graine 1) : victoire de " + r.Gagnant + " en " + r.Rounds + " rounds");

        // 100 combats avec les graines 1 à 100.
        int victoiresA = 0;
        int totalRounds = 0;
        for (uint graine = 1; graine <= 100; graine++)
        {
            Resultat x = Simulateur.Combattre(equipeA, meneurA, equipeB, meneurB, graine, niveauReliques);
            if (x.Gagnant == "A") victoiresA++;
            totalRounds += x.Rounds;
        }
        Debug.Log("100 combats : A " + victoiresA + " victoires, B " + (100 - victoiresA) + ". Rounds au total : " + totalRounds);
    }
}
