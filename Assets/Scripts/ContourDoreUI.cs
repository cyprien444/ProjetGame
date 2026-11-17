using UnityEngine;
using UnityEngine.UI;

// Contour decoratif uniquement : aucun fond, aucune image et aucun texte.
// Les contours sont calcules sur le RectTransform de leur emplacement.
// Ainsi, les quatre cotes restent visibles sur toute resolution.
[RequireComponent(typeof(RectTransform))]
public sealed class ContourDoreUI : MaskableGraphic
{
    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = rectTransform.rect;
        float plusPetit = Mathf.Min(r.width, r.height);
        if (plusPetit <= 8f) return;

        float marge = Mathf.Clamp(plusPetit * .022f, 2f, 6f);
        float epaisseur = Mathf.Clamp(plusPetit * .052f, 3.5f, 10f);
        float biseau = Mathf.Clamp(plusPetit * .12f, 6f, 19f);

        // Ombre metallique sous le trait dore.
        Anneau(vh, Octogone(r, marge, biseau),
            Octogone(r, marge + epaisseur + 1.2f, biseau),
            new Color32(64, 43, 22, 235), new Color32(37, 25, 14, 235));

        // Grand anneau dore avec une lumiere en haut et a gauche.
        Anneau(vh, Octogone(r, marge + 1.0f, biseau),
            Octogone(r, marge + epaisseur * .70f, biseau),
            new Color32(252, 207, 123, 255), new Color32(180, 118, 42, 255));

        // Fin liseré clair le long du bord exterieur.
        Anneau(vh, Octogone(r, marge + 1.8f, biseau),
            Octogone(r, marge + 2.9f, biseau),
            new Color32(255, 234, 164, 255), new Color32(240, 179, 80, 255));

        // Fin liseré brun qui souligne l'interieur du cadre.
        Anneau(vh, Octogone(r, marge + epaisseur * .75f, biseau),
            Octogone(r, marge + epaisseur + .3f, biseau),
            new Color32(137, 90, 37, 245), new Color32(105, 69, 27, 245));
    }

    private static Vector2[] Octogone(Rect r, float inset, float biseau)
    {
        float gauche = r.xMin + inset;
        float droite = r.xMax - inset;
        float bas = r.yMin + inset;
        float haut = r.yMax - inset;
        float coin = Mathf.Min(biseau, (droite - gauche) * .3f,
                                        (haut - bas) * .3f);
        return new[]
        {
            new Vector2(gauche + coin, bas),
            new Vector2(droite - coin, bas),
            new Vector2(droite, bas + coin),
            new Vector2(droite, haut - coin),
            new Vector2(droite - coin, haut),
            new Vector2(gauche + coin, haut),
            new Vector2(gauche, haut - coin),
            new Vector2(gauche, bas + coin)
        };
    }

    private static void Anneau(VertexHelper vh, Vector2[] dehors,
        Vector2[] dedans, Color32 couleurClaire, Color32 couleurSombre)
    {
        int debut = vh.currentVertCount;
        for (int i = 0; i < 8; i++)
        {
            // Haut/gauche plus clair ; bas/droite plus brun, aspect metal.
            Color32 teinte = (i >= 3 && i <= 6) ? couleurClaire : couleurSombre;
            vh.AddVert(new Vector3(dehors[i].x, dehors[i].y, 0), teinte, Vector2.zero);
            vh.AddVert(new Vector3(dedans[i].x, dedans[i].y, 0), teinte, Vector2.zero);
        }
        for (int i = 0; i < 8; i++)
        {
            int a = debut + 2 * i;
            int b = debut + 2 * ((i + 1) % 8);
            vh.AddTriangle(a, b, a + 1);
            vh.AddTriangle(b, b + 1, a + 1);
        }
    }
}
