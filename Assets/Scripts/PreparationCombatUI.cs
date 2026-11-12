using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ProjetGame.Moteur;

// Ecran de preparation vertical : construit l'UI au lancement de la scene Equipe.
// Ajouter ce composant sur Canvas dans la scene Equipe.
// Les portraits provisoires sont chargees depuis Resources/PreparationUI/Portraits/0..14.
public class PreparationCombatUI : MonoBehaviour
{
    static readonly Color Or = new Color(0.97f, 0.77f, 0.35f);
    static readonly Color Blanc = new Color(0.97f, 0.93f, 0.84f);
    static readonly Color Bleu = new Color(0.57f, 0.78f, 1f);
    static readonly Color Rouge = new Color(1f, 0.65f, 0.55f);
    static readonly Color Sombre = new Color(0.05f, 0.08f, 0.14f, 0.93f);

    Canvas canvas;
    RectTransform racine;
    TMP_Text info;
    TMP_Text bonusMeneur;
    readonly Image[] imagesSlots = new Image[5];
    readonly Image[] portraitsSlots = new Image[5];
    readonly TMP_Text[] textesSlots = new TMP_Text[5];
    readonly TMP_Text[] etiquettesSlots = new TMP_Text[5];
    readonly Button[] boutonsMeneur = new Button[5];
    readonly Button[] boutonsRetirer = new Button[5];
    readonly PrepSourceGlisser[] sourcesSlots = new PrepSourceGlisser[5];
    readonly Dictionary<int, Image> galerieCartes = new Dictionary<int, Image>();
    readonly Dictionary<int, TMP_Text> galerieEtats = new Dictionary<int, TMP_Text>();
    int personnageSelectionne = -1;
    RectTransform fantome;

    private void Start()
    {
        ChargeurDonnees.Charger();
        if (!Donnees.Charge || Donnees.Persos == null)
        {
            Debug.LogError("PreparationCombatUI : impossible de lire les personnages JSON.");
            return;
        }
        canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("PreparationCombatUI : ajoute d'abord un Canvas dans la scene Equipe.");
            return;
        }
        if (canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (FindFirstObjectByType<EventSystem>() == null)
            Debug.LogWarning("PreparationCombatUI : aucun EventSystem dans la scene. Cree UI > Event System.");

        // Les cinq anciens boutons temporaires sont conserves, mais caches.
        for (int i = 1; i <= 5; i++)
        {
            Transform ancien = canvas.transform.Find("Emplacement" + i);
            if (ancien != null) ancien.gameObject.SetActive(false);
        }
        CreerEcran();
    }

    Sprite Visuel(string nom) { return Resources.Load<Sprite>("PreparationUI/" + nom); }
    Sprite Portrait(int index) { return Visuel("Portraits/" + index); }

    RectTransform Zone(string nom, Transform parent, Vector2 a, Vector2 b)
    {
        GameObject go = new GameObject(nom, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform r = (RectTransform)go.transform;
        r.anchorMin = a; r.anchorMax = b;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
        return r;
    }

    RectTransform Zone(string nom, Transform parent, float x1, float y1, float x2, float y2)
    { return Zone(nom, parent, new Vector2(x1, y1), new Vector2(x2, y2)); }

    Image Fond(RectTransform rect, Color couleur, string sprite = null, bool raycast = false)
    {
        Image i = rect.gameObject.AddComponent<Image>();
        i.sprite = sprite == null ? null : Visuel(sprite);
        i.color = couleur;
        i.raycastTarget = raycast;
        return i;
    }

    TMP_Text Texte(string nom, Transform parent, string contenu, int taille, Color couleur,
                   TextAlignmentOptions align, float x1 = 0f, float y1 = 0f, float x2 = 1f, float y2 = 1f)
    {
        RectTransform r = Zone(nom, parent, x1, y1, x2, y2);
        TextMeshProUGUI t = r.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = contenu; t.fontSize = taille; t.color = couleur; t.alignment = align;
        t.raycastTarget = false; t.enableWordWrapping = false;
        t.overflowMode = TextOverflowModes.Ellipsis;
        return t;
    }

    Button Bouton(string nom, Transform parent, string etiquette, int taille, string sprite,
                  Color teinte, Action action, float x1, float y1, float x2, float y2)
    {
        RectTransform r = Zone(nom, parent, x1, y1, x2, y2);
        Image image = Fond(r, teinte, sprite, true);
        Button b = r.gameObject.AddComponent<Button>();
        b.targetGraphic = image;
        b.transition = Selectable.Transition.ColorTint;
        ColorBlock couleurs = b.colors;
        couleurs.normalColor = Color.white;
        couleurs.highlightedColor = new Color(1f, 0.94f, 0.78f);
        couleurs.pressedColor = new Color(0.8f, 0.75f, 0.60f);
        b.colors = couleurs;
        b.onClick.AddListener(() => action());
        Texte("Libelle", r, etiquette, taille, Blanc, TextAlignmentOptions.Center, 0.06f, 0.06f, 0.94f, 0.94f);
        return b;
    }

    void CreerEcran()
    {
        galerieCartes.Clear(); galerieEtats.Clear();
        racine = Zone("PreparationCombat_Generee", canvas.transform, 0f, 0f, 1f, 1f);
        Fond(racine, Color.white, "fond_foret");

        RectTransform haut = Zone("Entete", racine, 0.025f, 0.875f, 0.975f, 0.977f);
        Fond(haut, Color.white, "panneau_or");
        Texte("NomEcran", haut, "PRÉPARATION DU COMBAT", 46, Blanc, TextAlignmentOptions.Center, .17f, .45f, .94f, .92f);
        Texte("Mode", haut, PreparationCombatEtat.Mode.ToUpperInvariant(), 28, Or, TextAlignmentOptions.Center, .17f, .10f, .94f, .45f);
        Bouton("Retour", haut, "<", 62, "bouton_nuit", Color.white,
            () => SceneManager.LoadScene("Menu"), .025f, .15f, .15f, .85f);

        RectTransform leaderBandeau = Zone("BandeauMeneur", racine, 0.035f, .839f, .965f, .878f);
        Fond(leaderBandeau, Sombre);
        bonusMeneur = Texte("AptitudeLeader", leaderBandeau, "Meneur : aucun", 24, Or,
            TextAlignmentOptions.Center, .015f, .05f, .985f, .95f);

        Bandeau("ÉQUIPE ADVERSE", .809f, .840f, Rouge);
        int[] ennemis = PreparationCombatEtat.EquipeEnnemie;
        for (int i = 0; i < 5; i++)
        {
            int index = (ennemis != null && i < ennemis.Length) ? ennemis[i] : -1;
            float gauche = .048f + i * .186f;
            CreerCarteEnnemie(i, index, gauche, .695f, gauche + .162f, .805f);
        }

        RectTransform vs = Zone("VS", racine, 0.32f, .635f, 0.68f, .695f);
        Fond(vs, new Color(.13f, .09f, .065f, .78f));
        Texte("TitreVS", vs, "VS", 92, Or, TextAlignmentOptions.Center);

        Bandeau("MON ÉQUIPE  ·  2 DEVANT / 3 DERRIÈRE", .606f, .642f, Bleu);
        // Positions 1-2 = avant ; positions 3-5 = arriere.
        CreerSlot(0, .295f, .491f, .475f, .608f);
        CreerSlot(1, .525f, .491f, .705f, .608f);
        CreerSlot(2, .167f, .360f, .347f, .485f);
        CreerSlot(3, .410f, .360f, .590f, .485f);
        CreerSlot(4, .653f, .360f, .833f, .485f);

        RectTransform inventaire = Zone("Inventaire", racine, .023f, .125f, .977f, .351f);
        Fond(inventaire, new Color(.045f,.065f,.10f,.94f), "panneau_or");
        Texte("TitreInventaire", inventaire, "MES HÉROS", 37, Or, TextAlignmentOptions.MidlineLeft,
            .035f, .78f, .50f, .98f);
        Texte("AideInventaire", inventaire, "Toucher puis placer  /  Glisser-déposer", 20, Blanc,
            TextAlignmentOptions.MidlineRight, .43f, .78f, .975f, .98f);
        ConstruireGalerie(inventaire);

        info = Texte("Message", racine, "Choisis un héros dans la liste, puis touche une place libre.",
            22, Blanc, TextAlignmentOptions.Center, .06f, .108f, .94f, .133f);
        Bouton("Annuler", racine, "ANNULER", 37, "bouton_nuit", Color.white,
            () => SceneManager.LoadScene("Menu"), .07f, .025f, .41f, .105f);
        Bouton("Commencer", racine, "COMMENCER", 40, "bouton_or", Color.white,
            CommencerCombat, .45f, .025f, .93f, .105f);

        // Visible seulement dans le prototype : permet d'atteindre les 5 places avec 3 heros initiaux.
        Bouton("AjouterHerosTest", inventaire, "+ 2 HÉROS TEST", 20, "bouton_nuit", Color.white,
            AjouterHerosTest, .67f, .012f, .977f, .16f);
        Actualiser();
    }

    void Bandeau(string libelle, float bas, float haut, Color couleur)
    {
        RectTransform bandeau = Zone("Bandeau_" + libelle, racine, .075f, bas, .925f, haut);
        Fond(bandeau, new Color(.04f,.07f,.12f,.86f));
        Texte("Titre", bandeau, libelle, 30, couleur, TextAlignmentOptions.Center);
    }

    void CreerCarteEnnemie(int position, int index, float x1, float y1, float x2, float y2)
    {
        RectTransform card = Zone("Ennemi_" + (position+1), racine, x1,y1,x2,y2);
        Fond(card, Color.white, "cadre_ennemi");
        if (index < 0 || index >= Donnees.Persos.Length) return;
        Image portrait = Fond(Zone("Portrait",card,.09f,.26f,.91f,.93f),Color.white);
        portrait.sprite = Portrait(index);
        Texte("Nom",card,Donnees.Persos[index].Nom,21,Blanc,TextAlignmentOptions.Center,.02f,.05f,.98f,.28f);
    }

    void CreerSlot(int position, float x1, float y1, float x2, float y2)
    {
        RectTransform card = Zone("Position_" + (position+1), racine, x1, y1, x2, y2);
        imagesSlots[position] = Fond(card, Color.white, "cadre_ami", true);
        PrepCibleDepot cible = card.gameObject.AddComponent<PrepCibleDepot>();
        cible.Initialiser(this, position);
        PrepSourceGlisser source = card.gameObject.AddComponent<PrepSourceGlisser>();
        source.Initialiser(this, -1, position);
        sourcesSlots[position] = source;
        portraitsSlots[position] = Fond(Zone("Portrait",card,.09f,.26f,.91f,.94f),Color.white);
        textesSlots[position] = Texte("Nom",card,"VIDE",22,Blanc,TextAlignmentOptions.Center,.02f,.06f,.98f,.30f);
        etiquettesSlots[position] = Texte("Numero",card,(position+1).ToString(),27,Or,
            TextAlignmentOptions.MidlineLeft,.07f,.80f,.30f,.99f);
        boutonsMeneur[position] = Bouton("Meneur",card,"★",30,"bouton_nuit",Color.white,
            () => ChoisirMeneur(position), .62f,.76f,.96f,.97f);
        boutonsRetirer[position] = Bouton("Retirer",card,"×",29,"bouton_nuit",Color.white,
            () => Retirer(position), .68f,.01f,.98f,.25f);
    }

    void ConstruireGalerie(RectTransform inventaire)
    {
        RectTransform zone = Zone("ListeDeroulante", inventaire, .025f, .17f, .975f, .78f);
        ScrollRect scroll = zone.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = true; scroll.vertical = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 65f;
        RectTransform vue = Zone("Viewport", zone, 0f, 0f, 1f, 1f);
        Fond(vue, new Color(.05f,.07f,.11f,.48f),null,true);
        vue.gameObject.AddComponent<RectMask2D>();
        RectTransform contenu = Zone("Content", vue, 0f, 0f, 0f, 1f);
        contenu.pivot = new Vector2(0f, .5f);
        // Deux lignes comme dans la maquette. Scroll horizontal quand la collection grandit.
        GridLayoutGroup grille = contenu.gameObject.AddComponent<GridLayoutGroup>();
        grille.cellSize = new Vector2(136f, 112f);
        grille.spacing = new Vector2(11f, 9f);
        grille.padding = new RectOffset(9,9,7,7);
        grille.startAxis = GridLayoutGroup.Axis.Vertical;
        grille.constraint = GridLayoutGroup.Constraint.FixedRowCount;
        grille.constraintCount = 2;
        int colonnes = Mathf.Max(1, Mathf.CeilToInt(DonneesJoueur.PersonnagesPossedes.Count/2f));
        contenu.sizeDelta = new Vector2(18f+colonnes*147f, 0f);
        scroll.viewport = vue; scroll.content = contenu;

        foreach (int index in DonneesJoueur.PersonnagesPossedes)
        {
            if (index < 0 || index >= Donnees.Persos.Length) continue;
            RectTransform card = Zone("Hero_" + index, contenu, 0f,0f,0f,0f);
            Image cadre = Fond(card, Color.white, "cadre_ami", true);
            galerieCartes[index] = cadre;
            Image portrait = Fond(Zone("Portrait",card,.07f,.26f,.93f,.93f), Color.white);
            portrait.sprite = Portrait(index);
            Texte("Nom",card,Donnees.Persos[index].Nom,22,Blanc,TextAlignmentOptions.Center,.02f,.02f,.98f,.29f);
            galerieEtats[index] = Texte("Utilise",card,"",15,Or,TextAlignmentOptions.Center,.02f,.78f,.98f,.96f);
            PrepSourceGlisser source = card.gameObject.AddComponent<PrepSourceGlisser>();
            source.Initialiser(this,index,-1);
        }
    }

    public void CliquerPersonnage(int index)
    {
        if (!DonneesJoueur.PersonnagesPossedes.Contains(index)) return;
        personnageSelectionne = index;
        AfficherMessage(Donnees.Persos[index].Nom + " sélectionné : touche une place ou glisse son portrait.");
        Actualiser();
    }

    public void CliquerEmplacement(int position)
    {
        if (personnageSelectionne >= 0)
            Placer(personnageSelectionne, position);
        else if (DonneesJoueur.Equipe[position] >= 0)
            AfficherMessage("Glisse pour déplacer, ★ pour le meneur, × pour retirer.");
        else
            AfficherMessage("Sélectionne d'abord un héros dans la liste en bas.");
    }

    public void Depot(int index, int origine, int destination)
    {
        if (destination < 0 || destination >= 5) return;
        if (origine >= 0 && origine < 5)
        {
            // Déplacer entre deux positions : échange si la destination est occupee.
            if (origine == destination) return;
            int un = DonneesJoueur.Equipe[origine];
            int deux = DonneesJoueur.Equipe[destination];
            DonneesJoueur.Equipe[destination] = un;
            DonneesJoueur.Equipe[origine] = deux;
            AfficherMessage("Positions échangées.");
            Actualiser();
            return;
        }
        Placer(index, destination);
    }

    void Placer(int index, int position)
    {
        if (position < 0 || position >= 5) return;
        if (!DonneesJoueur.PersonnagesPossedes.Contains(index)) return;
        if (index < 0 || index >= Donnees.Persos.Length) return;
        int placePrecedente = Array.IndexOf(DonneesJoueur.Equipe, index);
        if (placePrecedente == position) { personnageSelectionne = -1; Actualiser(); return; }
        int ancien = DonneesJoueur.Equipe[position];
        DonneesJoueur.Equipe[position] = index;
        if (placePrecedente >= 0) DonneesJoueur.Equipe[placePrecedente] = ancien;
        if (DonneesJoueur.Meneur >= 0 && Array.IndexOf(DonneesJoueur.Equipe, DonneesJoueur.Meneur) < 0)
            DonneesJoueur.Meneur = -1;
        personnageSelectionne = -1;
        AfficherMessage(Donnees.Persos[index].Nom + " placé en position " + (position+1) + ".");
        Actualiser();
    }

    void Retirer(int position)
    {
        int ancien = DonneesJoueur.Equipe[position];
        DonneesJoueur.Equipe[position] = -1;
        if (ancien >= 0 && ancien == DonneesJoueur.Meneur) DonneesJoueur.Meneur = -1;
        AfficherMessage("Emplacement " + (position+1) + " libéré.");
        Actualiser();
    }

    void ChoisirMeneur(int position)
    {
        int index = DonneesJoueur.Equipe[position];
        if (index < 0) return;
        DonneesJoueur.Meneur = index;
        AfficherMessage("Meneur : " + Donnees.Persos[index].Nom + ".");
        Actualiser();
    }

    void Actualiser()
    {
        for (int i = 0; i < 5; i++)
        {
            int index = DonneesJoueur.Equipe[i];
            bool valide = index >= 0 && index < Donnees.Persos.Length;
            portraitsSlots[i].enabled = valide;
            sourcesSlots[i].Personnage = valide ? index : -1;
            if (valide)
            {
                portraitsSlots[i].sprite = Portrait(index);
                textesSlots[i].text = Donnees.Persos[index].Nom;
                imagesSlots[i].color = Color.white;
            }
            else
            {
                textesSlots[i].text = "+   PLACE " + (i+1);
                imagesSlots[i].color = new Color(.77f,.86f,1f,.85f);
            }
            etiquettesSlots[i].text = DonneesJoueur.Meneur == index && valide ? "★" : (i+1).ToString();
            boutonsMeneur[i].gameObject.SetActive(valide);
            boutonsRetirer[i].gameObject.SetActive(valide);
        }
        foreach (KeyValuePair<int,Image> entree in galerieCartes)
        {
            int index = entree.Key;
            bool pris = Array.IndexOf(DonneesJoueur.Equipe,index) >= 0;
            entree.Value.color = index == personnageSelectionne ? new Color(1f,.88f,.56f) :
                pris ? new Color(.69f,.85f,.78f) : Color.white;
            galerieEtats[index].text = pris ? "✓ ÉQUIPE" : "";
        }
        if (bonusMeneur != null)
        {
            int index = DonneesJoueur.Meneur;
            if (index >= 0 && index < Donnees.Persos.Length && Array.IndexOf(DonneesJoueur.Equipe,index)>=0)
            {
                Passif passif = Donnees.Persos[index].Meneur;
                bonusMeneur.text = passif == null ?
                    "Meneur : " + Donnees.Persos[index].Nom + "  ·  Pas de bonus passif" :
                    "Meneur : " + Donnees.Persos[index].Nom + "  ·  " + passif.Texte;
            }
            else bonusMeneur.text = "Meneur : aucun  ·  Touche ★ sur un héros placé";
        }
    }

    void AfficherMessage(string texte)
    {
        if (info != null) info.text = texte;
    }

    void AjouterHerosTest()
    {
        // Seulement pour tester le moteur a 5 avant d'avoir l'invocation.
        if (!DonneesJoueur.PersonnagesPossedes.Contains(3)) DonneesJoueur.PersonnagesPossedes.Add(3);
        if (!DonneesJoueur.PersonnagesPossedes.Contains(4)) DonneesJoueur.PersonnagesPossedes.Add(4);
        if (racine != null) Destroy(racine.gameObject);
        CreerEcran();
        AfficherMessage("Maëlle et Hakon ajoutés provisoirement pour tester une équipe complète.");
    }

    void CommencerCombat()
    {
        var dejaVus = new HashSet<int>();
        for (int i = 0; i < 5; i++)
        {
            int index = DonneesJoueur.Equipe[i];
            if (index < 0 || !DonneesJoueur.PersonnagesPossedes.Contains(index) || !dejaVus.Add(index))
            {
                AfficherMessage("Il faut 5 héros différents dans l'équipe pour lancer le moteur actuel.");
                return;
            }
        }
        PreparationCombatEtat.EquipeAlliee = (int[])DonneesJoueur.Equipe.Clone();
        PreparationCombatEtat.PositionMeneur = Array.IndexOf(DonneesJoueur.Equipe,DonneesJoueur.Meneur);
        PreparationCombatEtat.EstPret = true;
        if (!Application.CanStreamedLevelBeLoaded("Combat"))
        {
            AfficherMessage("Ajoute la scène Combat dans File > Build Profiles > Scene List.");
            PreparationCombatEtat.EstPret = false;
            return;
        }
        SceneManager.LoadScene("Combat");
    }

    // Fantome de glisser-deposer : volontairement non interactif pour ne pas bloquer les drops.
    public void DebutGlisser(int index, PointerEventData evt)
    {
        if (index < 0 || index >= Donnees.Persos.Length) return;
        if (fantome != null) Destroy(fantome.gameObject);
        fantome = Zone("FantomeGlisser",racine,new Vector2(.5f,.5f),new Vector2(.5f,.5f));
        fantome.sizeDelta = new Vector2(170f,210f);
        Fond(fantome,new Color(1f,1f,1f,.86f),"cadre_ami");
        Image img = Fond(Zone("Portrait",fantome,.12f,.27f,.88f,.92f),Color.white);
        img.sprite = Portrait(index);
        Texte("Nom",fantome,Donnees.Persos[index].Nom,24,Blanc,TextAlignmentOptions.Center,.02f,.03f,.98f,.28f);
        fantome.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
        AvancerGlisser(evt);
    }

    public void AvancerGlisser(PointerEventData evt)
    {
        if (fantome == null) return;
        Vector2 local;
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(racine,evt.position,cam,out local))
            fantome.anchoredPosition = local;
    }

    public void FinGlisser()
    {
        if (fantome != null) Destroy(fantome.gameObject);
        fantome = null;
    }
}

// Ces petits composants sont utilises uniquement pour les evenements des cartes.
public class PrepSourceGlisser : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    PreparationCombatUI controleur;
    ScrollRect defilement;
    bool defileHorizontal;
    public int Personnage;
    public int Origine; // -1 = collection, 0..4 = emplacement de l'equipe

    public void Initialiser(PreparationCombatUI ui, int personnage, int origine)
    {
        controleur = ui; Personnage = personnage; Origine = origine;
    }
    public void OnPointerClick(PointerEventData evt)
    {
        if (controleur == null) return;
        if (Origine < 0) controleur.CliquerPersonnage(Personnage);
        // Les clics sur les places sont traites par PrepCibleDepot.
    }
    public void OnBeginDrag(PointerEventData evt)
    {
        // Dans la liste : un geste horizontal defile ; un geste vertical prend le portrait.
        defilement = Origine == -1 ? GetComponentInParent<ScrollRect>() : null;
        defileHorizontal = defilement != null && Mathf.Abs(evt.delta.x) > Mathf.Abs(evt.delta.y);
        if (defileHorizontal) defilement.OnBeginDrag(evt);
        else if (controleur != null && Personnage >= 0) controleur.DebutGlisser(Personnage,evt);
    }
    public void OnDrag(PointerEventData evt)
    {
        if (defileHorizontal && defilement != null) defilement.OnDrag(evt);
        else if (controleur != null && Personnage >= 0) controleur.AvancerGlisser(evt);
    }
    public void OnEndDrag(PointerEventData evt)
    {
        if (defileHorizontal && defilement != null) defilement.OnEndDrag(evt);
        else if (controleur != null) controleur.FinGlisser();
        defileHorizontal = false;
    }
}

public class PrepCibleDepot : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    PreparationCombatUI controleur;
    int position;
    public void Initialiser(PreparationCombatUI ui, int p) { controleur = ui; position = p; }
    public void OnPointerClick(PointerEventData evt)
    {
        if (controleur != null) controleur.CliquerEmplacement(position);
    }
    public void OnDrop(PointerEventData evt)
    {
        PrepSourceGlisser source = evt.pointerDrag == null ? null : evt.pointerDrag.GetComponent<PrepSourceGlisser>();
        if (source != null && controleur != null && source.Personnage >= 0)
            controleur.Depot(source.Personnage,source.Origine,position);
    }
}
