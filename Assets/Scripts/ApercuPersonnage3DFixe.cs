using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sur le Canvas de la scène Collection. Affiche la RenderTexture du guerrier 3D
/// UNIQUEMENT dans la fiche Encyclopédie, sans modifier CollectionRefonteUI.cs.
/// Ne crée aucun mouvement et n'accepte aucune rotation de l'utilisateur.
/// </summary>
[DisallowMultipleComponent]
public sealed class ApercuPersonnage3DFixe : MonoBehaviour
{
    [SerializeField] private RenderTexture renduPersonnage;
    private RectTransform emplacementActuel;
    private int framesSansFiche;

    public void DefinirTexture(RenderTexture texture)
    {
        renduPersonnage = texture;
    }

    private void LateUpdate()
    {
        if (renduPersonnage == null) return;

        if (emplacementActuel != null && emplacementActuel.gameObject.activeInHierarchy)
            return;

        // La collection recrée son interface lors de chaque changement de page.
        // Nous recherchons la nouvelle fiche, sans modifier sa navigation.
        if (++framesSansFiche < 8) return;
        framesSansFiche = 0;

        Transform zone = transform.Find("CollectionRefonte_Generee/EmplacementModele3D");
        if (zone == null || !zone.gameObject.activeInHierarchy) return;

        var nouvelleZone = zone as RectTransform;
        if (nouvelleZone == null) return;

        Transform imageOriginale = nouvelleZone.Find("HeroIllustration2D");
        if (imageOriginale != null) imageOriginale.gameObject.SetActive(false);

        var objet = new GameObject("Apercu3D_Fixe", typeof(RectTransform), typeof(RawImage));
        objet.transform.SetParent(nouvelleZone, false);
        var rect = objet.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;

        var image = objet.GetComponent<RawImage>();
        image.texture = renduPersonnage;
        image.color = Color.white;
        image.raycastTarget = false;
        image.uvRect = new Rect(0f, 0f, 1f, 1f);

        emplacementActuel = nouvelleZone;
    }
}
