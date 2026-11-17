using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ProjetGame.Moteur;

// Collection personnelle + Encyclopedie pour Projet Game (Unity 6, UGUI et TextMeshPro).
// Ajouter sur le Canvas de la scene Collection. Aucune modification du moteur de combat.
// La collection personnelle lit DonneesJoueur ; l'encyclopedie lit les 15 entrees de persos.json.
public class CollectionRefonteUI : MonoBehaviour
{
    private enum Mode { Personnelle, Galerie, FicheEncyclopedie, InfoPersonnage }
    private enum SousInfo { Avis, Utilisations, Equipements }
    private enum Onglet { Competences, Infos, Equipements, Runes, Utilisations }

    private static readonly Color Or = new Color(0.96f, 0.76f, 0.39f);
    private static readonly Color TexteClair = new Color(0.98f, 0.96f, 0.91f);
    private static readonly Color TexteSecondaire = new Color(0.72f, 0.81f, 0.92f);
    private static readonly Color AccentBleu = new Color(0.52f, 0.79f, 1f);
    private static readonly Color Sombre = new Color(0.035f, 0.067f, 0.12f, 0.93f);
    private static readonly Color Card = new Color(0.075f, 0.12f, 0.21f, 0.95f);

    private Mode mode = Mode.Personnelle;
    private Onglet onglet = Onglet.Equipements;
    private int indexSelectionne = -1;
    private int indexCompetence = 0;
    // Fiche encyclopedie : un panneau d'explication n'apparait qu'au clic.
    private bool afficherDescriptionCompetence = false;
    private bool afficherDescriptionMeneur = false;
    private SousInfo sousInfo = SousInfo.Avis;
    private Mode retourDepuisInfos = Mode.Personnelle;
    private RectTransform racine;
    private ScrollRect listePortraits;
    private TMP_Text texteInfo;
    private float positionVerticale = 1f;
    private float positionHorizontaleMesHeros = 0f;
    private bool pret;

    private void Awake()
    {
        // Cet ecran remplace visuellement l'ancienne collection, sans supprimer ses objets.
        GestionCollection ancien = GetComponent<GestionCollection>();
        if (ancien != null) ancien.enabled = false;
        MasquerAncienEnfant("Scroll View");
        MasquerAncienEnfant("BoutonRetour");
        MasquerAncienEnfant("FichePersonnage");
    }

    private void MasquerAncienEnfant(string nom)
    {
        Transform enfant = transform.Find(nom);
        if (enfant != null) enfant.gameObject.SetActive(false);
    }

    private void Start()
    {
        if (GetComponent<Canvas>() == null)
        {
            Debug.LogError("CollectionRefonteUI doit etre sur le Canvas de Collection.");
            return;
        }
        try { ChargeurDonnees.Charger(); }
        catch (Exception e)
        {
            Debug.LogError("CollectionRefonteUI : echec du chargement JSON : " + e.Message);
            return;
        }
        if (!Donnees.Charge || Donnees.Persos == null || Donnees.Persos.Length == 0)
        {
            Debug.LogError("CollectionRefonteUI : donnees des personnages introuvables.");
            return;
        }

        var scaler = GetComponent<CanvasScaler>();
        if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = .5f;
        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        indexSelectionne = DonneesJoueur.PersonnagesPossedes.Count > 0
            ? DonneesJoueur.PersonnagesPossedes[0] : -1;
        pret = true;
        Construire();
    }

    private Sprite SpriteUI(string nom)
    {
        return Resources.Load<Sprite>("CollectionUI/" + nom);
    }

    private Sprite Portrait(int index)
    {
        return Resources.Load<Sprite>("CollectionUI/Portraits/" + index);
    }

    private RectTransform Zone(string nom, Transform parent, float x1, float y1, float x2, float y2)
    {
        var obj = new GameObject(nom, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        var rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(x1, y1);
        rt.anchorMax = new Vector2(x2, y2);
        rt.pivot = new Vector2(.5f, .5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    private Image ImageUI(RectTransform rt, Color couleur, string sprite = null, bool raycast = false)
    {
        var i = rt.gameObject.AddComponent<Image>();
        Sprite s = sprite == null ? null : SpriteUI(sprite);
        if (s != null)
        {
            i.sprite = s;
            if (s.border.sqrMagnitude > .5f) i.type = Image.Type.Sliced;
        }
        i.color = couleur;
        i.raycastTarget = raycast;
        return i;
    }

    private TMP_Text Texte(string nom, Transform parent, string contenu, int taille,
        Color couleur, float x1, float y1, float x2, float y2,
        TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        var rt = Zone(nom, parent, x1, y1, x2, y2);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = contenu ?? "";
        t.fontSize = taille;
        t.color = couleur;
        t.alignment = align;
        t.enableWordWrapping = true;
        t.overflowMode = TextOverflowModes.Ellipsis;
        t.raycastTarget = false;
        return t;
    }

    private Button Bouton(string nom, Transform parent, string libelle,
        Action action, float x1, float y1, float x2, float y2,
        bool actif = false, int taille = 28, bool disponible = true)
    {
        var rt = Zone(nom, parent, x1, y1, x2, y2);
        string texture = actif ? "bouton_dore" : "bouton_sombre";
        var fond = ImageUI(rt, actif ? Color.white : new Color(.95f,.98f,1f), texture, true);
        var bouton = rt.gameObject.AddComponent<Button>();
        bouton.targetGraphic = fond;
        bouton.interactable = disponible;
        var cols = bouton.colors;
        cols.normalColor = Color.white;
        cols.highlightedColor = new Color(1f, .93f, .76f);
        cols.pressedColor = new Color(.75f, .83f, .94f);
        cols.disabledColor = new Color(.57f, .6f, .68f, .83f);
        bouton.colors = cols;
        if (action != null) bouton.onClick.AddListener(() => action());
        Texte("Libelle", rt, libelle, taille, actif ? new Color(.15f,.12f,.09f) : TexteClair,
            .03f, .04f, .97f, .96f);
        return bouton;
    }

    private RectTransform Panneau(string nom, Transform parent,
        float x1, float y1, float x2, float y2)
    {
        var z = Zone(nom, parent, x1, y1, x2, y2);
        ImageUI(z, Color.white, "panneau_codex");
        return z;
    }

    private List<int> ListePossedee()
    {
        var l = new List<int>();
        foreach (int i in DonneesJoueur.PersonnagesPossedes)
            if (i >= 0 && i < Donnees.Persos.Length && !l.Contains(i)) l.Add(i);
        return l;
    }

    private void RetenirDefilement()
    {
        if (listePortraits == null) return;
        // Seule la bande « Mes héros » est défilante.
        if (mode == Mode.Personnelle)
            positionHorizontaleMesHeros = listePortraits.horizontalNormalizedPosition;
    }

    private void Selection(int index)
    {
        if (index < 0 || index >= Donnees.Persos.Length) return;
        RetenirDefilement();
        indexSelectionne = index;
        indexCompetence = 0;
        afficherDescriptionCompetence = false;
        afficherDescriptionMeneur = false;
        // Conserver l'onglet actif lorsque l'on change de personnage.
        // Depuis la galerie de l'encyclopedie, ouvrir la nouvelle fiche sur INFOS.
        if (mode == Mode.Galerie)
        {
            mode = Mode.FicheEncyclopedie;
            onglet = Onglet.Infos;
        }
        Construire();
    }

    private void BasculerPersonnelle()
    {
        RetenirDefilement();
        mode = Mode.Personnelle;
        onglet = Onglet.Equipements;
        var possedes = ListePossedee();
        if (!possedes.Contains(indexSelectionne))
            indexSelectionne = possedes.Count > 0 ? possedes[0] : -1;
        Construire();
    }

    private void BasculerEncyclopedie()
    {
        mode = Mode.Galerie;
        onglet = Onglet.Infos;
        Construire();
    }

    private void OuvrirInfoPersonnage()
    {
        retourDepuisInfos = mode;
        mode = Mode.InfoPersonnage;
        sousInfo = SousInfo.Avis;
        Construire();
    }

    // Ces deux raccourcis sont reserves a la fiche de l'Encyclopedie.
    // Ils reutilisent la page d'infos deja presente dans la V3.4.
    private void OuvrirInfoDepuisEncyclopedie(SousInfo section)
    {
        retourDepuisInfos = Mode.FicheEncyclopedie;
        sousInfo = section;
        mode = Mode.InfoPersonnage;
        Construire();
    }

    private void SelectionnerCompetenceEncyclopedie(int numero)
    {
        bool etaitOuverte = afficherDescriptionCompetence && indexCompetence == numero;
        indexCompetence = numero;
        afficherDescriptionCompetence = !etaitOuverte;
        afficherDescriptionMeneur = false;
        Construire();
    }

    private void SelectionnerMeneurEncyclopedie()
    {
        afficherDescriptionMeneur = !afficherDescriptionMeneur;
        afficherDescriptionCompetence = false;
        Construire();
    }

    // Images decoratives : des decoupes de la maquette choisie par l'utilisateur.
    // Tous les textes propres aux heros et leurs statistiques restent dynamiques.
    private RectTransform IllustrationFiche(string nom, Transform parent, string ressource,
        float x1, float y1, float x2, float y2)
    {
        var zone = Zone(nom, parent, x1, y1, x2, y2);
        var image = ImageUI(zone, Color.white);

        // V3.5.6 : cadres 100 % Unity UI avec des Image standard.
        // Les anciens PNG et ContourDoreUI ne sont plus utilises.
        // Le dessin utilise des bandes UI standard (4 cotes a chaque couche),
        // sans mesh personnalise, sans decoupage d'images et sans losange.
        bool estCadre = ressource == "panneau_stats" ||
                        ressource == "icone_eveil" ||
                        ressource == "icone_lead" ||
                        ressource == "icone_vide" ||
                        ressource == "icone_skill_1" ||
                        ressource == "icone_skill_2" ||
                        ressource == "icone_skill_3";
        if (estCadre)
        {
            // Graphic invisible conservé pour les boutons et leur zone de clic.
            image.sprite = null;
            image.color = Color.clear;
            DessinerCadreAvecImagesUnity(zone);
        }
        else
        {
            // Aucun changement : fond, bannieres, Communaute et Utilisation
            // conservent leurs images, texte, fond bleu et boutons d'origine.
            image.sprite = SpriteUI("Encyclopedie/" + ressource);
            if (image.sprite == null)
            {
                image.sprite = SpriteUI("panneau_codex");
                Debug.LogWarning("Ressource de la fiche manquante : " + ressource +
                    ". Lance Projet Game > Collections > Configurer images fiche V3.5.");
            }
        }
        return zone;
    }

    // V3.5.7 : mêmes emplacements qu'en V3.5.6, mais coins biseautés.
    // Bordures réalisées avec des Image UGUI transparentes au centre,
    // sans PNG, sans shader et sans modifier les boutons ni leurs positions.
    private void DessinerCadreAvecImagesUnity(RectTransform parent)
    {
        AjouterCoucheCadreBiseaute(parent, "Ombre", 2f, 10f, 22f,
            new Color(0.22f, 0.13f, 0.055f, 0.97f));
        AjouterCoucheCadreBiseaute(parent, "Or", 3.5f, 6f, 20f,
            new Color(0.83f, 0.57f, 0.22f, 1f));
        AjouterCoucheCadreBiseaute(parent, "Reflet", 4.5f, 1.8f, 19f,
            new Color(1f, 0.88f, 0.58f, 1f));
        AjouterCoucheCadreBiseaute(parent, "LisereInterieur", 10f, 1.5f, 24f,
            new Color(0.58f, 0.36f, 0.13f, 0.90f));
    }

    // Les tailles sont en pixels du Canvas (pas en coordonnées 0..1).
    // Les 4 côtés sont ancrés, les 4 diagonales sont ancrées aux coins :
    // l'ensemble s'adapte sans lire parent.rect, même au premier rendu.
    private void AjouterCoucheCadreBiseaute(RectTransform parent, string nom,
        float marge, float epaisseur, float chanfrein, Color teinte)
    {
        AjouterBandeCadre(parent, nom + "_Haut",
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(.5f, .5f), new Vector2(-2f * chanfrein, epaisseur),
            new Vector2(0f, -marge), teinte);
        AjouterBandeCadre(parent, nom + "_Bas",
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(.5f, .5f), new Vector2(-2f * chanfrein, epaisseur),
            new Vector2(0f, marge), teinte);
        AjouterBandeCadre(parent, nom + "_Gauche",
            new Vector2(0f, 0f), new Vector2(0f, 1f),
            new Vector2(.5f, .5f), new Vector2(epaisseur, -2f * chanfrein),
            new Vector2(marge, 0f), teinte);
        AjouterBandeCadre(parent, nom + "_Droite",
            new Vector2(1f, 0f), new Vector2(1f, 1f),
            new Vector2(.5f, .5f), new Vector2(epaisseur, -2f * chanfrein),
            new Vector2(-marge, 0f), teinte);

        float centreCoin = (marge + chanfrein) * .5f;
        float longueur = (chanfrein - marge) * Mathf.Sqrt(2f);
        AjouterDiagonaleCadre(parent, nom + "_CoinHG",
            new Vector2(0f, 1f), new Vector2(centreCoin, -centreCoin),
            longueur, epaisseur, 45f, teinte);
        AjouterDiagonaleCadre(parent, nom + "_CoinHD",
            new Vector2(1f, 1f), new Vector2(-centreCoin, -centreCoin),
            longueur, epaisseur, -45f, teinte);
        AjouterDiagonaleCadre(parent, nom + "_CoinBG",
            new Vector2(0f, 0f), new Vector2(centreCoin, centreCoin),
            longueur, epaisseur, -45f, teinte);
        AjouterDiagonaleCadre(parent, nom + "_CoinBD",
            new Vector2(1f, 0f), new Vector2(-centreCoin, centreCoin),
            longueur, epaisseur, 45f, teinte);
    }

    private void AjouterDiagonaleCadre(RectTransform parent, string nom,
        Vector2 coinAncre, Vector2 decalage, float longueur,
        float epaisseur, float angle, Color teinte)
    {
        var objet = new GameObject(nom, typeof(RectTransform));
        objet.transform.SetParent(parent, false);
        var rt = objet.GetComponent<RectTransform>();
        rt.anchorMin = coinAncre;
        rt.anchorMax = coinAncre;
        rt.pivot = new Vector2(.5f, .5f);
        rt.sizeDelta = new Vector2(longueur, epaisseur);
        rt.anchoredPosition = decalage;
        rt.localRotation = Quaternion.Euler(0f, 0f, angle);
        var image = objet.AddComponent<Image>();
        image.color = teinte;
        image.raycastTarget = false;
    }

    private void AjouterBandeCadre(RectTransform parent, string nom,
        Vector2 ancreMin, Vector2 ancreMax, Vector2 pivot,
        Vector2 taille, Vector2 position, Color teinte)
    {
        var objet = new GameObject(nom, typeof(RectTransform));
        objet.transform.SetParent(parent, false);
        var rt = objet.GetComponent<RectTransform>();
        rt.anchorMin = ancreMin;
        rt.anchorMax = ancreMax;
        rt.pivot = pivot;
        rt.sizeDelta = taille;
        rt.anchoredPosition = position;
        var trait = objet.AddComponent<Image>();
        trait.color = teinte;
        trait.raycastTarget = false;
    }

    private void BoutonFicheImage(string nom, Transform parent, string ressource,
        Action action, float x1, float y1, float x2, float y2, bool disponible = true)
    {
        var zone = IllustrationFiche(nom, parent, ressource, x1, y1, x2, y2);
        var image = zone.GetComponent<Image>();
        image.raycastTarget = true;
        var bouton = zone.gameObject.AddComponent<Button>();
        bouton.targetGraphic = image;
        bouton.interactable = disponible;
        if (action != null) bouton.onClick.AddListener(() => action());
    }

    private void RetourFichePersonnelle()
    {
        mode = retourDepuisInfos;
        Construire();
    }

    private void ChangerSousInfo(SousInfo nouveau)
    {
        sousInfo = nouveau;
        Construire();
    }

    private void ChangerOnglet(Onglet nouveau)
    {
        RetenirDefilement();
        onglet = nouveau;
        Construire();
    }

    private void RetourGalerie()
    {
        mode = Mode.Galerie;
        Construire();
    }

    private void Construire()
    {
        if (!pret) return;
        if (racine != null) Destroy(racine.gameObject);
        racine = Zone("CollectionRefonte_Generee", transform, 0f, 0f, 1f, 1f);
        ImageUI(racine, Color.white, "fond_codex");
        if (mode == Mode.Personnelle) ConstruirePersonnelle();
        else if (mode == Mode.Galerie) ConstruireGalerie();
        else if (mode == Mode.InfoPersonnage) ConstruireInfoPersonnage();
        else ConstruireFicheEncyclopedie();
    }

    private void Entete(string titre, string sousTitre, Action retour = null)
    {
        var b = Panneau("Entete", racine, .014f, .921f, .986f, .993f);
        Bouton("Retour", b, "‹", retour ?? (() => SceneManager.LoadScene("Menu")),
            .012f, .12f, .135f, .91f, false, 59);
        Texte("Titre", b, titre, 47, Or, .16f, .40f, .83f, .98f);
        Texte("SousTitre", b, sousTitre, 24, TexteSecondaire, .16f, .05f, .83f, .44f);
        Bouton("Menu", b, "⌂", () => SceneManager.LoadScene("Menu"),
            .855f, .12f, .988f, .91f, false, 45);
    }

    private void DessinerPortrait(RectTransform parent, int index, bool selectionne,
        bool etiquette = false)
    {
        var bord = ImageUI(parent, selectionne ? Or : Color.white, "cadre_portrait", true);
        var btn = parent.gameObject.AddComponent<Button>();
        btn.targetGraphic = bord;
        btn.onClick.AddListener(() => Selection(index));
        var imgRect = Zone("Portrait", parent, .065f, etiquette ? .24f : .065f,
            .935f, .935f);
        var i = ImageUI(imgRect, Color.white);
        i.sprite = Portrait(index);
        i.preserveAspect = true;
        // Les personnages non possedes conservent leur image d'origine en plus sombre.
        i.color = DonneesJoueur.Possede(index)
            ? Color.white : new Color(.65f, .65f, .65f, 1f);
        if (i.sprite == null)
            Texte("Initiale", imgRect, Donnees.Persos[index].Nom.Substring(0,1),
                50, Or, 0f, 0f, 1f, 1f);
        if (etiquette)
            Texte("Nom", parent, Donnees.Persos[index].Nom, 21, TexteClair,
                0f, .015f, 1f, .23f);
    }

    // Barre permanente de portraits possedes, en bas. Bouton Encyclopedie a droite.
    private void BandeMesHerosEnBas(Transform parent)
    {
        var indices = ListePossedee();
        var panneau = Panneau("BandeMesHeros", parent, .025f, .050f, .975f, .253f);
        Texte("TitreBande", panneau, "MES HÉROS · " + indices.Count + "/" + Donnees.Persos.Length,
            23, Or, .025f, .865f, .785f, .995f, TextAlignmentOptions.MidlineLeft);
        var scroll = Zone("ScrollPossedes", panneau, .012f, .075f, .808f, .868f);
        var sr = scroll.gameObject.AddComponent<ScrollRect>();
        sr.horizontal = true;
        sr.vertical = false;
        sr.scrollSensitivity = 55f;
        sr.movementType = ScrollRect.MovementType.Clamped;
        var vp = Zone("Viewport", scroll, 0f, 0f, 1f, 1f);
        ImageUI(vp, new Color(.01f, .02f, .04f, .05f), null, true);
        vp.gameObject.AddComponent<RectMask2D>();
        var content = Zone("Content", vp, 0f, 0f, 0f, 1f);
        content.pivot = new Vector2(0f, .5f);
        content.sizeDelta = new Vector2(Mathf.Max(1, indices.Count) * 174f + 24f, 0f);
        var layout = content.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.spacing = 12f;
        layout.padding = new RectOffset(10, 10, 0, 0);
        foreach (int index in indices)
        {
            var cadre = Zone("PortraitPossede_" + index, content, 0f, 0f, 0f, 0f);
            cadre.sizeDelta = new Vector2(162f, 168f);
            DessinerPortrait(cadre, index, index == indexSelectionne);
        }
        sr.content = content;
        sr.viewport = vp;
        listePortraits = sr;
        Canvas.ForceUpdateCanvases();
        sr.horizontalNormalizedPosition = positionHorizontaleMesHeros;
        Bouton("BoutonEncyclopedieBas", panneau, "ENCYCLO-\nPÉDIE",
            BasculerEncyclopedie, .825f, .11f, .988f, .84f, true, 24);
    }

    // Ecran personnel inspire de la reference: art au centre, competences a droite,
    // statistiques en bas a gauche, liste de heros en bas.
    private void ConstruirePersonnelle()
    {
        var possedes = ListePossedee();
        if (!possedes.Contains(indexSelectionne))
            indexSelectionne = possedes.Count > 0 ? possedes[0] : -1;

        string titre = indexSelectionne < 0 ? "COLLECTION" : Donnees.Persos[indexSelectionne].Nom.ToUpperInvariant();
        string sousTitre = indexSelectionne < 0 ? "Aucun héros possédé" :
            Donnees.Persos[indexSelectionne].Role + " · " + Donnees.Persos[indexSelectionne].Rarete;
        Entete(titre, sousTitre);
        BandeMesHerosEnBas(racine);

        if (indexSelectionne < 0)
        {
            var vide = Panneau("SansHeros", racine, .035f, .32f, .969f, .87f);
            Texte("AucunHero", vide, "Aucun personnage possédé pour le moment.\n\nOuvre l'Encyclopédie pour voir tous les héros.",
                36, TexteClair, .05f, .18f, .95f, .82f);
            return;
        }

        Perso p = Donnees.Persos[indexSelectionne];
        var scene = Panneau("ScenePersonnage", racine, .025f, .267f, .746f, .910f);
        var zoneArt = Zone("HeroCentral", scene, .035f, .195f, .972f, .965f);
        var art = ImageUI(zoneArt, Color.white);
        art.sprite = SpriteUI("HeroArt/" + indexSelectionne);
        if (art.sprite == null) art.sprite = Portrait(indexSelectionne);
        art.preserveAspect = true;

        Bouton("Informations", scene, "ⓘ INFOS", OuvrirInfoPersonnage,
            .027f, .875f, .365f, .978f, true, 26);

        var stats = Panneau("StatsBase", scene, .030f, .047f, .57f, .405f);
        Texte("TitreStats", stats, "STATISTIQUES DE BASE", 25, Or,
            .035f, .83f, .96f, .98f, TextAlignmentOptions.MidlineLeft);
        string[] lignes = {
            "PV       " + Stat(p, 0).ToString("N0"),
            "ATQ      " + Stat(p, 1).ToString("N0"),
            "DEF      " + Stat(p, 2).ToString("N0"),
            "SPD      " + p.Spd,
            "CRIT     " + p.Crit + " %"
        };
        for (int k = 0; k < lignes.Length; k++)
        {
            float yy = .765f - k * .142f;
            Texte("Stat" + k, stats, lignes[k], 26, TexteClair,
                .06f, yy - .10f, .95f, yy + .02f, TextAlignmentOptions.MidlineLeft);
        }
        int n = p.Competences == null ? 0 : p.Competences.Length;
        if (n > 0)
        {
            indexCompetence = Mathf.Clamp(indexCompetence, 0, n - 1);
            var selected = p.Competences[indexCompetence];
            var apercu = Panneau("DescriptionCompetence", scene, .59f, .047f, .972f, .405f);
            Texte("TitreCompetence", apercu, selected.Nom, 25, Or, .045f, .70f, .95f, .97f);
            Texte("EffetCompetence", apercu, Description(selected), 23, TexteClair,
                .055f, .13f, .945f, .74f);
        }

        var competences = Panneau("EchelleCompetences", racine, .752f, .267f, .975f, .910f);
        Texte("CompetencesTitre", competences, "COMPÉTENCES", 25, Or,
            .02f, .908f, .98f, .989f);
        for (int j = 0; j < 3; j++)
        {
            int pos = j;
            bool existe = p.Competences != null && j < p.Competences.Length;
            float y = .69f - j * .215f;
            Bouton("Competence" + j, competences, (j + 1).ToString(),
                existe ? (Action)(() => { indexCompetence = pos; Construire(); }) : null,
                .20f, y, .80f, y + .158f, existe && indexCompetence == j, 45, existe);
            Texte("CompetenceNom" + j, competences,
                existe ? p.Competences[j].Nom : "À DÉBLOQUER", 20,
                existe ? TexteClair : TexteSecondaire,
                .015f, y - .067f, .985f, y + .005f);
        }
        Bouton("Eveil", competences, "ÉVEIL", null,
            .115f, .025f, .885f, .139f, false, 24, false);
        Texte("EveilNote", competences, "À VENIR", 18, TexteSecondaire,
            .025f, .148f, .975f, .187f);
    }

    // La page d'information est preparee. Les avis et tendances demanderont plus tard
    // un compte joueur, un etat d'eveil et des donnees communautaires reelles.
    private void ConstruireInfoPersonnage()
    {
        if (indexSelectionne < 0 || indexSelectionne >= Donnees.Persos.Length)
        {
            RetourFichePersonnelle();
            return;
        }
        Perso p = Donnees.Persos[indexSelectionne];
        Entete("INFOS DU HÉROS", p.Nom, RetourFichePersonnelle);
        var identite = Panneau("IdentificationHeros", racine, .035f, .690f, .965f, .841f);
        var cadre = Zone("MiniPortrait", identite, .035f, .085f, .235f, .915f);
        var img = ImageUI(cadre, Color.white);
        img.sprite = Portrait(indexSelectionne);
        img.preserveAspect = true;
        Texte("NomHero", identite, p.Nom, 44, Or, .275f, .56f, .955f, .90f,
            TextAlignmentOptions.MidlineLeft);
        Texte("RoleHero", identite, p.Role + " · " + p.Rarete, 27, TexteClair,
            .275f, .17f, .955f, .54f, TextAlignmentOptions.MidlineLeft);

        var tabs = Zone("OngletsInfo", racine, .035f, .616f, .965f, .678f);
        Bouton("Avis", tabs, "COMMUNAUTÉ", () => ChangerSousInfo(SousInfo.Avis),
            .00f, .03f, .328f, .98f, sousInfo == SousInfo.Avis, 23);
        Bouton("Utilisations", tabs, "UTILISATIONS", () => ChangerSousInfo(SousInfo.Utilisations),
            .336f, .03f, .662f, .98f, sousInfo == SousInfo.Utilisations, 23);
        Bouton("ReliquesPopulaires", tabs, "ÉQUIPEMENTS", () => ChangerSousInfo(SousInfo.Equipements),
            .670f, .03f, 1f, .98f, sousInfo == SousInfo.Equipements, 23);
        var contenu = Panneau("ContenuInfos", racine, .035f, .15f, .965f, .604f);
        if (sousInfo == SousInfo.Avis)
        {
            Texte("Titre", contenu, "AVIS DE LA COMMUNAUTÉ", 35, Or,
                .05f, .82f, .95f, .965f);
            Texte("Explication", contenu,
                "Plus tard, un compte joueur pourra donner un seul avis sur ce personnage, après son éveil.\n\n" +
                "Aucun avis communautaire n'est disponible dans ce prototype.",
                28, TexteClair, .065f, .34f, .935f, .78f);
            Bouton("DonnerAvis", contenu, "DONNER MON AVIS · À VENIR", null,
                .10f, .08f, .90f, .20f, false, 25, false);
        }
        else if (sousInfo == SousInfo.Utilisations)
        {
            Texte("Titre", contenu, "OÙ CE HÉROS EST-IL UTILISÉ ?", 33, Or,
                .05f, .83f, .95f, .97f);
            Texte("Explication", contenu,
                "À terme : répartition des utilisations par mode de jeu, basée sur les équipes réelles de la communauté.\n\n" +
                "Les données ne sont pas encore collectées. Aucun pourcentage n'est inventé.",
                27, TexteClair, .065f, .18f, .935f, .79f);
        }
        else
        {
            Texte("Titre", contenu, "ÉQUIPEMENTS LES PLUS UTILISÉS", 33, Or,
                .05f, .83f, .95f, .97f);
            Texte("Explication", contenu,
                "À terme : sets de Reliques et statistiques d'équipement les plus utilisés sur ce héros.\n\n" +
                "Aucune donnée communautaire disponible pour le moment.",
                28, TexteClair, .065f, .18f, .935f, .79f);
        }
    }

    private void Tab(RectTransform parent, string nom, Onglet cible, float x1, float x2)
    {
        Bouton("Tab_" + nom, parent, nom, () => ChangerOnglet(cible),
            x1, .05f, x2, .97f, onglet == cible, 22);
    }

    private void VueEquipements(RectTransform zone, Perso perso)
    {
        Texte("TitreEquipements", zone, "MES 6 EMPLACEMENTS DE RELIQUES", 28, Or,
            .025f, .914f, .585f, .99f, TextAlignmentOptions.MidlineLeft);
        var emplacement = Zone("PanneauSixCases", zone, .023f, .205f, .598f, .895f);
        ImageUI(emplacement, new Color(.05f,.09f,.16f,.62f));
        Texte("Centre", emplacement, "✧\nRELIQUES", 30, Or,
            .355f,.38f,.645f,.65f);
        string[] noms = { "Arme", "Bottes", "Armure", "Gants", "Casque", "Amulette" };
        string[] icons = { "arme", "bottes", "armure", "gants", "casque", "amulette" };
        for (int k = 0; k < 6; k++)
        {
            int j=k;
            int col=k%2, row=k/2;
            float x1=col==0?.03f:.68f;
            float y2=.98f-row*.315f;
            var slot = Zone("Relique_" + noms[k], emplacement,x1,y2-.285f,x1+.285f,y2);
            var frame=ImageUI(slot,Color.white,"emplacement_relique",true);
            var button=slot.gameObject.AddComponent<Button>();
            button.targetGraphic=frame;
            button.onClick.AddListener(() => Message("Relique " + noms[j] + " : disponible à l'étape 10."));
            var ic=Zone("Icon",slot,.20f,.24f,.80f,.89f);
            var img=ImageUI(ic,Color.white);
            img.sprite=SpriteUI("Icons/"+icons[k]);
            img.preserveAspect=true;
            Texte("Nom",slot,noms[k].ToUpperInvariant(),18,TexteClair,.01f,.01f,.99f,.23f);
        }
        var stats = Panneau("StatsEquipements",zone,.618f,.205f,.984f,.897f);
        Texte("TitreStats",stats,"STATS · NIV. 1",30,Or,.04f,.89f,.96f,.99f);
        int pv=Stat(perso,0), atk=Stat(perso,1), def=Stat(perso,2);
        string[] lignes={ "PV      "+pv.ToString("N0"),"ATQ     "+atk.ToString("N0"),
            "DEF     "+def.ToString("N0"),"SPD     "+perso.Spd,
            "Crit     "+perso.Crit+" %","Dég crit   50 %","ACC     0 %","RES     0 %"};
        for(int k=0;k<lignes.Length;k++)
        {
            float yy=.83f-k*.083f;
            Texte("Stat_"+k,stats,lignes[k],25, k%2==0?TexteClair:TexteSecondaire,
                .065f, yy-.07f, .95f, yy+.01f, TextAlignmentOptions.MidlineLeft);
        }
        Texte("Ensemble",zone,"BONUS DE SET : aucun (non équipé)",23,TexteSecondaire,
            .03f,.127f,.97f,.197f,TextAlignmentOptions.Center);
        Bouton("AutoEquip",zone,"AUTO-ÉQUIPER",null,.045f,.021f,.48f,.109f,false,24,false);
        Bouton("Retirer",zone,"RETIRER TOUT",null,.515f,.021f,.955f,.109f,false,24,false);
    }

    private static int Stat(Perso p, int colonne)
    {
        if (p.StatsFixes != null && p.StatsFixes.Length > colonne)
            return p.StatsFixes[colonne];
        if (Donnees.Base == null || Donnees.MultRarete == null ||
            !Donnees.Base.ContainsKey(p.Role) || !Donnees.MultRarete.ContainsKey(p.Rarete)) return 0;
        return Mathf.FloorToInt((float)(Donnees.Base[p.Role][colonne]
            * Donnees.MultRarete[p.Rarete] / 100.0));
    }

    private void VueInfos(RectTransform zone, Perso p)
    {
        Texte("Titre",zone,"STATISTIQUES · NIVEAU 1",36,Or,.04f,.87f,.96f,.975f);
        string[] labels={"PV","ATQ","DEF","SPD","TAUX CRIT","DÉG. CRIT","PRÉCISION","RÉSISTANCE"};
        string[] valeurs={Stat(p,0).ToString("N0"), Stat(p,1).ToString("N0"),
            Stat(p,2).ToString("N0"),p.Spd.ToString(),p.Crit+" %","50 %","0 %","0 %"};
        for(int k=0;k<labels.Length;k++)
        {
            int col=k/4, row=k%4;
            float xx=col==0?.04f:.52f;
            float yy=.74f-row*.14f;
            Texte("Label"+k,zone,labels[k],28,TexteSecondaire,xx,yy,xx+.23f,yy+.08f,
                TextAlignmentOptions.MidlineLeft);
            Texte("Valeur"+k,zone,valeurs[k],30,TexteClair,xx+.23f,yy,xx+.42f,yy+.08f,
                TextAlignmentOptions.MidlineRight);
        }
        Texte("Passif",zone,"PASSIF DE MENEUR\n"+
            (p.Meneur == null?"Aucun":p.Meneur.Texte),27,Or,
            .05f,.06f,.95f,.27f,TextAlignmentOptions.Center);
    }

    private void VueCompetences(RectTransform zone, Perso p)
    {
        Texte("Titre",zone,"COMPÉTENCES DE "+p.Nom.ToUpperInvariant(),33,Or,
            .04f,.87f,.96f,.98f);
        int count=p.Competences==null?0:p.Competences.Length;
        if(count==0)
        {
            Texte("Vide",zone,"Aucune compétence renseignée.",30,TexteClair,.05f,.2f,.95f,.8f);
            return;
        }
        for(int i=0;i<count;i++)
        {
            Competence c=p.Competences[i];
            float h=.74f/count, top=.85f-i*h;
            var slot=Panneau("Competence"+i,zone,.035f,top-h+.017f,.965f,top);
            Texte("Nom",slot,(i+1)+". "+c.Nom,29,Or,.045f,.54f,.95f,.95f,
                TextAlignmentOptions.MidlineLeft);
            Texte("Description",slot,Description(c),25,TexteClair,.045f,.045f,.95f,.56f,
                TextAlignmentOptions.MidlineLeft);
        }
        Texte("Note",zone,"Compétences disponibles dès le niveau 1.",23,TexteSecondaire,
            .03f,.015f,.97f,.095f);
    }

    private static string Description(Competence c)
    {
        string cible=c.Type=="A"?"tous les ennemis":
            c.Type=="H1"?"un allié":c.Type=="HA"?"tous les alliés":"un ennemi";
        string effet=c.EstSoin?"Soin":"Dégâts";
        string puissance=c.Coups>1?c.Coups+" × "+c.Puissance+" %":c.Puissance+" %";
        return effet+" : "+puissance+" · "+cible+" · RC "+c.Recharge;
    }

    private void VueRunes(RectTransform zone)
    {
        Texte("Titre",zone,"RUNES",39,Or,.07f,.79f,.93f,.97f);
        Texte("Explication",zone,
            "Cet onglet n'a pas encore de fonction distincte dans le prototype.\n\n"+
            "Les six emplacements de Reliques sont maintenant dans ÉQUIPEMENTS.\n\n"+
            "Aucune Rune supplémentaire n'est définie pour le moment.",
            30,TexteClair,.07f,.23f,.93f,.77f,TextAlignmentOptions.Center);
        Bouton("RetourEquipements",zone,"VOIR MES ÉQUIPEMENTS",()=>ChangerOnglet(Onglet.Equipements),
            .21f,.065f,.79f,.16f,true,27);
    }

    private void Message(string message)
    {
        if (texteInfo != null) texteInfo.text = message;
        Debug.Log("CollectionRefonteUI : " + message);
    }

    // Encyclopedie : page galerie complete, sans barre de defilement.
    // Les 15 portraits sont visibles directement. Clic => fiche dediee.
    private void ConstruireGalerie()
    {
        Entete("ENCYCLOPÉDIE", "Toutes les légendes · " + Donnees.Persos.Length + " personnages",
            BasculerPersonnelle);
        var panneau = Panneau("GaleriePortraits", racine, .032f, .065f, .968f, .910f);
        Texte("Instruction", panneau, "CHOISIS UN PERSONNAGE POUR OUVRIR SA FICHE", 29, Or,
            .045f, .916f, .955f, .982f);

        // Grille fixe de 4 colonnes, sans ScrollRect ni bande de portraits.
        var grille = Zone("GrillePortraits", panneau, .03f, .055f, .97f, .906f);
        var layout = grille.gameObject.AddComponent<GridLayoutGroup>();
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 4;
        layout.cellSize = new Vector2(200f, 224f);
        layout.spacing = new Vector2(20f, 26f);
        layout.padding = new RectOffset(16, 16, 18, 18);
        layout.childAlignment = TextAnchor.UpperCenter;
        for (int k = 0; k < Donnees.Persos.Length; k++)
        {
            var frame = Zone("Hero" + k, grille, 0f, 0f, 0f, 0f);
            DessinerPortrait(frame, k, k == indexSelectionne);
        }
        Texte("Bas", racine,
            "Portraits assombris : héros non possédés · Tous les personnages sont consultables.",
            24, TexteSecondaire, .05f, .014f, .95f, .062f);
    }

    // Fermeture de la fiche : fond bleu nuit propre, bordure doree biseautee,
    // deux diagonales UGUI pour la croix. Aucun caractère spécial / sprite requis.
    private void ConstruireCroixRetourEncyclopedie(RectTransform banniere)
    {
        var zone = Zone("RetourGalerie", banniere,
            .885f, .218f, .976f, .782f);
        var fond = ImageUI(zone, new Color(.046f, .072f, .119f, 1f), null, true);
        var bouton = zone.gameObject.AddComponent<Button>();
        bouton.targetGraphic = fond;
        bouton.onClick.AddListener(RetourGalerie);
        var teintes = bouton.colors;
        teintes.normalColor = Color.white;
        teintes.highlightedColor = new Color(1f, .98f, .89f, 1f);
        teintes.pressedColor = new Color(.78f, .83f, .94f, 1f);
        bouton.colors = teintes;

        // Réutilise exactement le même dessin de contour que les autres cadres.
        DessinerCadreAvecImagesUnity(zone);

        // Ombre discrète puis deux branches dorées, dessinées directement en UI.
        var centre = new Vector2(.5f, .5f);
        var ombre = new Color(.20f, .105f, .025f, .95f);
        var lumiere = new Color(1f, .88f, .56f, 1f);
        AjouterDiagonaleCadre(zone, "CroixOmbre1", centre,
            new Vector2(0f, -2f), 43f, 11f, 45f, ombre);
        AjouterDiagonaleCadre(zone, "CroixOmbre2", centre,
            new Vector2(0f, -2f), 43f, 11f, -45f, ombre);
        AjouterDiagonaleCadre(zone, "CroixOr1", centre,
            Vector2.zero, 43f, 7.5f, 45f, lumiere);
        AjouterDiagonaleCadre(zone, "CroixOr2", centre,
            Vector2.zero, 43f, 7.5f, -45f, lumiere);
    }

    // Fiche immersive V3.5, inspiree exactement de la maquette bleue/or.
    // Reservee a l'encyclopedie. La collection personnelle reste 100% V3.2.
    // Pour le futur rendu 3D, remplacer le composant Image "HeroIllustration2D"
    // dans "EmplacementModele3D" par une RawImage alimentee par une RenderTexture.
    private void ConstruireFicheEncyclopedie()
    {
        if (indexSelectionne < 0 || indexSelectionne >= Donnees.Persos.Length)
            indexSelectionne = 0;

        Perso p = Donnees.Persos[indexSelectionne];

        // 1 - Fond bleu nuit, angles dores et banniere EXACTE de la reference.
        IllustrationFiche("FondBleuEtOr", racine, "fond_fiche", 0f, 0f, 1f, 1f);
        IllustrationFiche("EnteteEncyclopedie", racine, "banniere_titre",
            0f, .910f, 1f, 1f);
        var panneauIdentite = IllustrationFiche("BannierePersonnage", racine,
            "banniere_hero", .019f, .816f, .977f, .917f);

        // Informations variables : jamais du texte incruste dans la photo.
        var titre = Texte("NomPersonnage", panneauIdentite, p.Nom, 48, Or,
            .085f, .45f, .855f, .88f);
        titre.enableAutoSizing = true;
        titre.fontSizeMin = 26;
        titre.fontSizeMax = 48;
        Texte("RoleEtRarete", panneauIdentite, p.Role + "  ·  " + p.Rarete, 30,
            TexteClair, .13f, .12f, .82f, .46f);

        // Bouton de fermeture entierement construit en UGUI.
        // La banniere PNG livree ne contient plus l'ancienne croix incrustee.
        ConstruireCroixRetourEncyclopedie(panneauIdentite);

        // 2 - Deux boutons a gauche. Ils ouvrent les vraies pages deja existantes.
        BoutonFicheImage("Communauté", racine, "bouton_communaute",
            () => OuvrirInfoDepuisEncyclopedie(SousInfo.Avis),
            .017f, .739f, .241f, .807f);
        BoutonFicheImage("Utilisation", racine, "bouton_utilisation",
            () => OuvrirInfoDepuisEncyclopedie(SousInfo.Utilisations),
            .017f, .670f, .241f, .737f);

        // 3 - Illustration de ce personnage, en conservant son sprite d'origine.
        // Ce noeud accueillera plus tard le modele 3D (camera -> RenderTexture).
        var emplacement3D = Zone("EmplacementModele3D", racine,
            .178f, .236f, .794f, .821f);
        var imageHero = Zone("HeroIllustration2D", emplacement3D,
            0f, 0f, 1f, 1f);
        var art = ImageUI(imageHero, Color.white);
        Sprite illustration = SpriteUI("HeroArt/" + indexSelectionne);
        art.sprite = illustration != null ? illustration : Portrait(indexSelectionne);
        art.preserveAspect = true;
        art.raycastTarget = false;
        if (art.sprite == null)
            Texte("IllustrationManquante", emplacement3D, p.Nom, 38, TexteClair,
                .1f, .3f, .9f, .7f);

        // 4 - 5 emplacements a droite : eveil, 3 competences, meneur.
        // Toutes les images de competence sont PROVISOIRES (pas d'icones par
        // competence dans les donnees actuelles). Les effets affiches sont reels.
        BoutonFicheImage("EveilVerrouille", racine, "icone_eveil", null,
            .771f, .691f, .966f, .796f, false);

        string[] visuels = { "icone_skill_1", "icone_skill_2", "icone_skill_3" };
        float[] bas = { .574f, .471f, .367f };
        // Trois cases strictement de meme largeur ET de meme hauteur.
        const float hauteurCompetence = .096f;
        for (int j = 0; j < 3; j++)
        {
            int numero = j;
            bool existe = p.Competences != null && j < p.Competences.Length;
            BoutonFicheImage("Competence_" + (j + 1), racine,
                existe ? visuels[j] : "icone_vide",
                existe ? (Action)(() => SelectionnerCompetenceEncyclopedie(numero)) : null,
                .786f, bas[j], .954f, bas[j] + hauteurCompetence, existe);
        }

        bool meneurExiste = p.Meneur != null;
        BoutonFicheImage("Lead", racine,
            meneurExiste ? "icone_lead" : "icone_vide",
            meneurExiste ? (Action)SelectionnerMeneurEncyclopedie : null,
            .786f, .263f, .954f, .359f, meneurExiste);

        // 5 - Veritable panneau dore de la maquette : les chiffres originaux
        // ont ete effaces de l'asset, puis remplis avec les stats JSON du heros.
        var statistiques = IllustrationFiche("PanneauStats", racine, "panneau_stats",
            .022f, .052f, .486f, .229f);
        string[] valeurs =
        {
            Stat(p, 0).ToString("N0"),
            Stat(p, 1).ToString("N0"),
            Stat(p, 2).ToString("N0"),
            p.Spd.ToString(),
            p.Crit + " %"
        };
        // Titre et lignes entierement dynamiques (plus de valeurs sans legendes).
        var titreStatistiques = Texte("TitreStatistiques", statistiques,
            "STATISTIQUES", 28, Or,
            .09f, .837f, .91f, .970f, TextAlignmentOptions.Center);
        titreStatistiques.fontStyle = FontStyles.Bold;
        var traitTitre = Zone("SeparateurTitre", statistiques,
            .100f, .817f, .900f, .823f);
        ImageUI(traitTitre, new Color(.93f, .73f, .37f, .45f));

        string[] etiquettes = { "PV", "ATQ", "DEF", "SPD", "CRIT" };
        float[] centre = { .735f, .582f, .430f, .278f, .126f };
        for (int i = 0; i < valeurs.Length; i++)
        {
            // V3.5.9 : lignes assez hautes pour la police TMP.
            // Sans cela, TextMeshPro masque le texte (mode Ellipsis) et
            // affiche des avertissements sur la police LiberationSans SDF.
            var label = Texte("LibelleStat_" + i, statistiques,
                etiquettes[i], 30, Or,
                .125f, centre[i] - .072f, .52f, centre[i] + .072f,
                TextAlignmentOptions.MidlineLeft);
            label.fontStyle = FontStyles.Bold;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Overflow;
            label.margin = Vector4.zero;

            var valeur = Texte("Valeur" + i, statistiques,
                valeurs[i], 31, TexteClair,
                .54f, centre[i] - .072f, .935f, centre[i] + .072f,
                TextAlignmentOptions.MidlineRight);
            valeur.enableWordWrapping = false;
            valeur.overflowMode = TextOverflowModes.Overflow;
            valeur.margin = Vector4.zero;

            if (i < valeurs.Length - 1)
            {
                var separateur = Zone("SeparateurStat_" + i, statistiques,
                    .115f, centre[i] - .078f, .885f, centre[i] - .074f);
                ImageUI(separateur, new Color(.88f, .68f, .35f, .29f));
            }
        }

        // 6 - Pop-up de details : absente par defaut comme sur la reference.
        if (afficherDescriptionCompetence && p.Competences != null &&
            indexCompetence >= 0 && indexCompetence < p.Competences.Length)
        {
            Competence c = p.Competences[indexCompetence];
            var popup = Panneau("DetailsCompetence", racine,
                .275f, .292f, .765f, .500f);
            Texte("NomCompetence", popup, c.Nom, 29, Or,
                .05f, .64f, .95f, .91f);
            Texte("DescriptionCompetence", popup, Description(c), 24, TexteClair,
                .07f, .20f, .93f, .66f);
            Bouton("FermerDetails", popup, "FERMER",
                () => { afficherDescriptionCompetence = false; Construire(); },
                .30f, .035f, .70f, .19f, false, 20);
        }
        else if (afficherDescriptionMeneur && p.Meneur != null)
        {
            var popup = Panneau("DetailsMeneur", racine,
                .275f, .292f, .765f, .500f);
            Texte("TitreMeneur", popup, "TALENT DE MENEUR", 29, Or,
                .05f, .64f, .95f, .91f);
            Texte("EffetMeneur", popup, p.Meneur.Texte, 23, TexteClair,
                .07f, .19f, .93f, .68f);
            Bouton("FermerDetails", popup, "FERMER",
                () => { afficherDescriptionMeneur = false; Construire(); },
                .30f, .035f, .70f, .19f, false, 20);
        }
    }






}
