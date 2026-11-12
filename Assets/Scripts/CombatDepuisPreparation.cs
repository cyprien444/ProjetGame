using UnityEngine;

// A AJOUTER sur l'objet Combat de la scene Combat (a cote de CombatScene).
// Awake s'execute avant Start de CombatScene, sans modifier le moteur existant.
public class CombatDepuisPreparation : MonoBehaviour
{
    private void Awake()
    {
        if (!PreparationCombatEtat.EstPret)
            return; // Lancer directement la scene Combat conserve son fonctionnement actuel.

        CombatScene combat = GetComponent<CombatScene>();
        if (combat == null)
            combat = FindFirstObjectByType<CombatScene>();

        if (combat == null)
        {
            Debug.LogError("CombatDepuisPreparation : aucun CombatScene dans la scene Combat.");
            return;
        }

        combat.equipeA = (int[])PreparationCombatEtat.EquipeAlliee.Clone();
        combat.equipeB = (int[])PreparationCombatEtat.EquipeEnnemie.Clone();
        combat.meneurA = PreparationCombatEtat.PositionMeneur;
        combat.meneurB = -1;

        // Evite de reappliquer l'ancienne equipe lors d'un futur lancement direct de Combat.
        PreparationCombatEtat.EstPret = false;
        Debug.Log("Combat : composition de l'equipe transmise depuis l'ecran de preparation.");
    }
}
