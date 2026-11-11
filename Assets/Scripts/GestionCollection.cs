
using UnityEngine;
using ProjetGame.Moteur;

public class GestionCollection : MonoBehaviour
{
    [Header("Affichage")]
    [SerializeField] private CartePersonnageUI prefabCarte;
    [SerializeField] private Transform contenuGrille;

    [Header("Personnages possedes (indices du JSON)")]
    [SerializeField] private int[] personnagesPossedes = { 0, 1, 2 };

    private void Start()
    {
        // Charger les donnees des personnages et des reliques
        ChargeurDonnees.Charger();

        if (!Donnees.Charge)
        {
            Debug.LogError("Impossible de charger les donnees du jeu.");
            return;
        }

        AfficherCollection();
    }

    public void AfficherCollection()
    {
        if (prefabCarte == null || contenuGrille == null)
        {
            Debug.LogError("Prefab Carte ou Contenu Grille non renseigne !");
            return;
        }

        if (!Donnees.Charge)
        {
            Debug.LogError("Les donnees ne sont pas encore chargees.");
            return;
        }

        // Supprimer les anciennes cartes
        foreach (Transform enfant in contenuGrille)
        {
            Destroy(enfant.gameObject);
        }

        // Creer une carte par personnage possede
        foreach (int index in personnagesPossedes)
        {
            if (index < 0 || index >= Donnees.Persos.Length)
            {
                Debug.LogWarning("Index de personnage invalide : " + index);
                continue;
            }

            Perso personnage = Donnees.Persos[index];

            CartePersonnageUI carte = Instantiate(
                prefabCarte,
                contenuGrille
            );

            carte.Configurer(
                personnage.Nom,
                personnage.Role,
                personnage.Rarete
            );
        }
    }
}
