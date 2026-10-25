using UnityEngine;
using ProjetGame.Moteur;

// Lit persos.json et reliques.json dans le dossier Assets/Resources/Donnees
// et les donne au moteur. À appeler une fois au démarrage de chaque scène qui lance un combat.
public static class ChargeurDonnees
{
    public static void Charger()
    {
        if (Donnees.Charge) return;

        TextAsset persos = Resources.Load<TextAsset>("Donnees/persos");
        TextAsset reliques = Resources.Load<TextAsset>("Donnees/reliques");
        if (persos == null || reliques == null)
        {
            Debug.LogError("Fichiers introuvables : il faut Assets/Resources/Donnees/persos.json et reliques.json " +
                           "(le dossier doit s'appeler exactement Resources).");
            return;
        }
        Donnees.Charger(persos.text, reliques.text);
    }
}
