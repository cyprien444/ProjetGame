
using System.Collections.Generic;

public static class DonneesJoueur
{
    // Personnages possedes par le joueur
    // Les numeros correspondent aux indices dans persos.json
    public static readonly List<int> PersonnagesPossedes =
        new List<int> { 0, 1, 2 };

    // Equipe de 5 personnages
    // -1 signifie que l'emplacement est vide
    public static readonly int[] Equipe =
        { -1, -1, -1, -1, -1 };

    // Index du meneur dans persos.json
    // -1 signifie aucun meneur selectionne
    public static int Meneur = -1;

    // Ressources de depart (provisoires)
    public static int Or = 0;
    public static int Gemmes = 0;
    public static int Energie = 50;

    // Verifie si le joueur possede un personnage
    public static bool Possede(int indexPersonnage)
    {
        return PersonnagesPossedes.Contains(indexPersonnage);
    }

    // Ajoute un personnage s'il n'est pas deja possede
    public static void AjouterPersonnage(int indexPersonnage)
    {
        if (indexPersonnage < 0)
            return;

        if (!Possede(indexPersonnage))
        {
            PersonnagesPossedes.Add(indexPersonnage);
        }
    }
}
