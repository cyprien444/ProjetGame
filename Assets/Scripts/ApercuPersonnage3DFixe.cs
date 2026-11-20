using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Affiche la camera du personnage 3D fixe dans l'Encyclopedie.
/// Reconnecte le rendu dans la meme frame lorsque CollectionRefonteUI
/// reconstruit la fiche (ouverture/fermeture des competences, Lead, etc.).
/// Aucune rotation ou interaction 3D n'est ajoutee.
/// </summary>
[DisallowMultipleComponent]
public sealed class ApercuPersonnage3DFixe : MonoBehaviour
{
    [SerializeField] private RenderTexture renduPersonnage;
    private RectTransform emplacementActuel;

    public void DefinirTexture(RenderTexture texture)
    {
        renduPersonnage = texture;
        emplacementActuel = null;
    }

    private void LateUpdate()
    {
        if (renduPersonnage == null)
            return;

        // IMPORTANT : Destroy() est differe a la fin de la frame.
        // Pendant un clic, l'ancien et le nouveau "CollectionRefonte_Generee"
        // peuvent donc coexister. Le dernier enfant est le plus recent.
        RectTransform nouvelleZone = TrouverDernierEmplacement3D();
        if (nouvelleZone == null)
        {
            emplacementActuel = null;
            return;
        }

        // Ne pas recreer le RawImage a chaque frame.
        if (nouvelleZone == emplacementActuel)
            return;

        // Masquer l'illustration 2D AVANT le rendu de cette meme frame.
        Transform illustration2D = nouvelleZone.Find("HeroIllustration2D");
        if (illustration2D != null)
            illustration2D.gameObject.SetActive(false);

        Transform existant = nouvelleZone.Find("Apercu3D_Fixe");
        RawImage image3D;
        if (existant != null)
        {
            image3D = existant.GetComponent<RawImage>();
            if (image3D == null)
                image3D = existant.gameObject.AddComponent<RawImage>();
        }
        else
        {
            var objet = new GameObject("Apercu3D_Fixe", typeof(RectTransform), typeof(RawImage));
            objet.transform.SetParent(nouvelleZone, false);
            image3D = objet.GetComponent<RawImage>();
        }

        var rect = image3D.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;

        image3D.texture = renduPersonnage;
        image3D.color = Color.white;
        image3D.raycastTarget = false;
        image3D.uvRect = new Rect(0f, 0f, 1f, 1f);

        emplacementActuel = nouvelleZone;
    }

    private RectTransform TrouverDernierEmplacement3D()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform enfant = transform.GetChild(i);
            if (enfant.name != "CollectionRefonte_Generee")
                continue;

            Transform zone = enfant.Find("EmplacementModele3D");
            if (zone != null && zone.gameObject.activeInHierarchy)
                return zone as RectTransform;
        }
        return null;
    }
}
