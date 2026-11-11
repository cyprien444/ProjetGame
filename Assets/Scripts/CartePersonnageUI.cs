
using TMPro;
using UnityEngine;

public class CartePersonnageUI : MonoBehaviour
{
    [Header("Textes de la carte")]
    [SerializeField] private TMP_Text texteNom;
    [SerializeField] private TMP_Text texteRole;
    [SerializeField] private TMP_Text texteRarete;

    public void Configurer(string nom, string role, string rarete)
    {
        texteNom.text = nom;
        texteRole.text = role;
        texteRarete.text = rarete;
    }
}
