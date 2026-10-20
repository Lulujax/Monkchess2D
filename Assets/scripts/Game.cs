using System;
using System.Collections;
using System.Collections.Generic;
using MonkChess;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Game : MonoBehaviour
{
    public GameObject chesspiece;

    private ChessBoard board;
    private player pieceTemplate;
    private readonly player[,] pieceViews = new player[8, 8];
    private readonly List<GameObject> movePlates = new List<GameObject>();
    private readonly List<GameObject> lastMoveMarkers = new List<GameObject>();

    private Vector2Int? selectedSquare;
    private player selectedView;
    private List<ChessMove> selectedMoves;
    private GameObject selectionMarker;

    private GameObject uiRoot;
    private Text turnText;
    private Text statusText;
    private Text moveText;
    private Button soundButton;
    private Button pauseUndoButton;

    private GameObject gameOverPanel;
    private Text gameOverTitle;
    private Text gameOverReason;
    private GameObject pausePanel;
    private GameObject promotionPanel;
    private Image[] promotionImages;
    private GameObject confirmPanel;
    private Text confirmMessage;
    private Action confirmAction;

    private bool paused;
    private bool promotionOpen;
    private bool gameOver;
    private bool confirmOpen;
    private ChessMove pendingPromotionMove;
    private Team promotionTeam;
    private Coroutine gameOverRoutine;

    private static Sprite squareSprite;
    private static readonly PieceType[] PromotionTypes =
    {
        PieceType.Queen,
        PieceType.Rook,
        PieceType.Bishop,
        PieceType.Knight
    };

    private static readonly string[] PromotionNames =
    {
        "REINA",
        "TORRE",
        "ALFIL",
        "CABALLO"
    };

    private void Start()
    {
        board = new ChessBoard();
        pieceTemplate = chesspiece != null ? chesspiece.GetComponent<player>() : null;

        HideLegacyUi();
        SoundManager.Ensure();
        UITheme.EnsureEventSystem();
        BuildInterface();
        SpawnAllPieces();
        UpdateHud();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (InstructionsView.IsAnyOpen)
            {
                return;
            }

            if (confirmOpen)
            {
                HideConfirm();
                return;
            }

            if (promotionOpen || gameOver)
            {
                return;
            }

            if (paused)
            {
                ResumeGame();
            }
            else
            {
                OpenPause();
            }
        }

        bool control = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        if (control && Input.GetKeyDown(KeyCode.Z) && !IsBlocked())
        {
            UndoMove();
        }

        if (Input.GetMouseButtonUp(0))
        {
            HandleBoardPointer();
        }
    }

    private void HandleBoardPointer()
    {
        if (IsBlocked() || PointerOverInteractiveUi())
        {
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        Vector3 pointer = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Collider2D hit = Physics2D.OverlapPoint(new Vector2(pointer.x, pointer.y));
        if (hit == null)
        {
            return;
        }

        player clickedPiece = hit.GetComponent<player>();
        if (clickedPiece != null)
        {
            OnPieceClicked(clickedPiece);
            return;
        }

        moveplate clickedPlate = hit.GetComponent<moveplate>();
        if (clickedPlate != null)
        {
            OnMoveTargetClicked(clickedPlate);
        }
    }

    private static bool PointerOverInteractiveUi()
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        for (int i = 0; i < results.Count; i++)
        {
            if (results[i].gameObject.GetComponentInParent<Selectable>() != null)
            {
                return true;
            }
        }

        return false;
    }

    public void OnPieceClicked(player clicked)
    {
        if (clicked == null || IsBlocked())
        {
            return;
        }

        Vector2Int square = new Vector2Int(clicked.Getxboard(), clicked.Getyboard());

        if (selectedSquare.HasValue)
        {
            ChessMove? targetMove = FindSelectedMoveTo(square);
            if (targetMove.HasValue)
            {
                HandleTargetMove(targetMove.Value);
                return;
            }
        }

        Piece piece = board.GetPiece(square);
        if (piece.IsEmpty || piece.Team != board.SideToMove)
        {
            ClearSelection();
            UpdateHud();
            return;
        }

        if (selectedSquare.HasValue && selectedSquare.Value == square)
        {
            ClearSelection();
            UpdateHud();
            return;
        }

        SelectSquare(square);
    }

    public void OnMoveTargetClicked(moveplate plate)
    {
        if (plate == null || IsBlocked())
        {
            return;
        }

        HandleTargetMove(plate.Move);
    }

    private void HandleTargetMove(ChessMove move)
    {
        if (move.Promotion != PieceType.None)
        {
            BeginPromotion(move);
        }
        else
        {
            CommitMove(move);
        }
    }

    private void SelectSquare(Vector2Int square)
    {
        ClearSelection();
        selectedSquare = square;
        selectedView = pieceViews[square.x, square.y];
        selectedMoves = board.GenerateLegalMoves(square);

        if (selectedView != null)
        {
            selectedView.SetSelected(true);
        }

        selectionMarker = CreateSquareMarker("Selection", square, new Color(1f, 0.76f, 0.34f, 0.24f), -6);

        List<Vector2Int> shownTargets = new List<Vector2Int>();
        for (int i = 0; i < selectedMoves.Count; i++)
        {
            ChessMove move = selectedMoves[i];
            if (shownTargets.Contains(move.To))
            {
                continue;
            }

            shownTargets.Add(move.To);
            CreateMovePlate(move);
        }

        SoundManager.Ensure().PlayClick();
        UpdateHud();
    }

    private ChessMove? FindSelectedMoveTo(Vector2Int square)
    {
        if (selectedMoves == null)
        {
            return null;
        }

        ChessMove fallback = default(ChessMove);
        bool found = false;

        for (int i = 0; i < selectedMoves.Count; i++)
        {
            ChessMove move = selectedMoves[i];
            if (move.To != square)
            {
                continue;
            }

            if (!found || move.Promotion == PieceType.Queen)
            {
                fallback = move;
                found = true;
            }

            if (move.Promotion == PieceType.Queen)
            {
                break;
            }
        }

        if (!found)
        {
            return null;
        }

        return fallback;
    }

    private void CreateMovePlate(ChessMove move)
    {
        GameObject prefab = selectedView != null ? selectedView.moveplate : null;
        if (prefab == null && pieceTemplate != null)
        {
            prefab = pieceTemplate.moveplate;
        }

        GameObject plate;
        if (prefab != null)
        {
            plate = Instantiate(prefab, SquarePosition(move.To), Quaternion.identity);
        }
        else
        {
            plate = new GameObject("MovePlate", typeof(SpriteRenderer), typeof(BoxCollider2D));
            plate.transform.position = SquarePosition(move.To);
            plate.transform.localScale = new Vector3(0.58f, 0.58f, 1f);
            SpriteRenderer renderer = plate.GetComponent<SpriteRenderer>();
            renderer.sprite = GetSquareSprite();
            renderer.sortingOrder = -4;
        }

        moveplate script = plate.GetComponent<moveplate>();
        if (script == null)
        {
            script = plate.AddComponent<moveplate>();
        }

        bool isCapture = !board.GetPiece(move.To).IsEmpty || move.IsEnPassant;
        script.Setup(this, move, isCapture);
        movePlates.Add(plate);
    }

    private void ClearSelection()
    {
        DestroyMovePlates();

        if (selectionMarker != null)
        {
            Destroy(selectionMarker);
            selectionMarker = null;
        }

        if (selectedView != null)
        {
            selectedView.SetSelected(false);
        }

        selectedView = null;
        selectedSquare = null;
        selectedMoves = null;
    }

    private void DestroyMovePlates()
    {
        for (int i = 0; i < movePlates.Count; i++)
        {
            if (movePlates[i] != null)
            {
                Destroy(movePlates[i]);
            }
        }

        movePlates.Clear();
    }

    private void BeginPromotion(ChessMove move)
    {
        pendingPromotionMove = move;
        promotionTeam = board.SideToMove;
        ClearSelection();
        promotionOpen = true;
        UpdatePromotionImages();
        promotionPanel.SetActive(true);
        SoundManager.Ensure().PlayClick();
    }

    private void UpdatePromotionImages()
    {
        if (promotionImages == null)
        {
            return;
        }

        for (int i = 0; i < promotionImages.Length; i++)
        {
            if (promotionImages[i] == null)
            {
                continue;
            }

            promotionImages[i].sprite = GetPieceSprite(PromotionTypes[i], promotionTeam);
        }
    }

    private void ChoosePromotion(PieceType type)
    {
        if (!promotionOpen)
        {
            return;
        }

        ChessMove move = pendingPromotionMove;
        move.Promotion = type;
        promotionOpen = false;
        promotionPanel.SetActive(false);
        CommitMove(move);
    }

    private void CommitMove(ChessMove move)
    {
        if (board == null || board.IsGameOver)
        {
            return;
        }

        MoveUndo undo = board.MakeMove(move);
        if (undo == null)
        {
            ClearSelection();
            UpdateHud();
            return;
        }

        ClearSelection();
        SpawnAllPieces();
        ShowLastMove(undo);
        PlayMoveSound(undo);

        if (board.IsGameOver)
        {
            gameOver = true;
            UpdateHud();
            if (gameOverRoutine != null)
            {
                StopCoroutine(gameOverRoutine);
            }

            gameOverRoutine = StartCoroutine(ShowGameOverSoon());
        }
        else
        {
            UpdateHud();
            StartCoroutine(AnimateArrival(move.To));
        }
    }

    private void UndoMove()
    {
        if (board == null || !board.CanUndo)
        {
            return;
        }

        MoveUndo undoneMove;
        if (!board.UndoLastMove(out undoneMove))
        {
            return;
        }

        if (gameOverRoutine != null)
        {
            StopCoroutine(gameOverRoutine);
            gameOverRoutine = null;
        }

        gameOver = false;
        ClearSelection();
        ClearLastMoveMarkers();
        SpawnAllPieces();
        UpdateHud();
        SoundManager.Ensure().PlayClick();
    }

    private void RestartGame()
    {
        if (gameOverRoutine != null)
        {
            StopCoroutine(gameOverRoutine);
            gameOverRoutine = null;
        }

        board.Reset();
        gameOver = false;
        paused = false;
        promotionOpen = false;
        confirmOpen = false;
        pendingPromotionMove = default(ChessMove);
        ClearSelection();
        ClearLastMoveMarkers();
        HideAllPanels();
        SpawnAllPieces();
        UpdateHud();
        SoundManager.Ensure().PlayClick();
    }

    private void SpawnAllPieces()
    {
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                if (pieceViews[x, y] != null)
                {
                    pieceViews[x, y].gameObject.SetActive(false);
                    Destroy(pieceViews[x, y].gameObject);
                    pieceViews[x, y] = null;
                }
            }
        }

        if (chesspiece == null)
        {
            Debug.LogError("Chess piece prefab is not assigned on the Game controller.");
            return;
        }

        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                Piece piece = board.GetPiece(new Vector2Int(x, y));
                if (!piece.IsEmpty)
                {
                    CreatePiece(piece, x, y);
                }
            }
        }
    }

    private void CreatePiece(Piece piece, int x, int y)
    {
        GameObject pieceObject = Instantiate(chesspiece, SquarePosition(new Vector2Int(x, y)), Quaternion.identity);
        player view = pieceObject.GetComponent<player>();
        view.Initialize(piece.Type, piece.Team);
        view.SetController(this);
        view.SetBoardPosition(x, y);
        view.SetCoords();
        pieceViews[x, y] = view;
    }

    private void ShowLastMove(MoveUndo undo)
    {
        ClearLastMoveMarkers();
        lastMoveMarkers.Add(CreateSquareMarker("LastFrom", undo.Move.From, new Color(1f, 0.72f, 0.28f, 0.14f), -8));
        lastMoveMarkers.Add(CreateSquareMarker("LastTo", undo.Move.To, new Color(1f, 0.84f, 0.46f, 0.2f), -7));
    }

    private void ClearLastMoveMarkers()
    {
        for (int i = 0; i < lastMoveMarkers.Count; i++)
        {
            if (lastMoveMarkers[i] != null)
            {
                Destroy(lastMoveMarkers[i]);
            }
        }

        lastMoveMarkers.Clear();
    }

    private void PlayMoveSound(MoveUndo undo)
    {
        SoundManager sound = SoundManager.Instance != null ? SoundManager.Instance : SoundManager.Ensure();
        if (board.IsGameOver)
        {
            sound.PlayEnd();
        }
        else if (board.IsInCheck(board.SideToMove))
        {
            sound.PlayCheck();
        }
        else if (!undo.CapturedPiece.IsEmpty)
        {
            sound.PlayCapture();
        }
        else
        {
            sound.PlayMove();
        }
    }

    private IEnumerator AnimateArrival(Vector2Int square)
    {
        player view = pieceViews[square.x, square.y];
        if (view == null)
        {
            yield break;
        }

        Transform target = view.transform;
        Vector3 finalScale = target.localScale;
        Vector3 startScale = finalScale * 0.84f;
        float elapsed = 0f;

        while (elapsed < 0.12f)
        {
            if (view == null)
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / 0.12f);
            target.localScale = Vector3.Lerp(startScale, finalScale, t);
            yield return null;
        }

        if (view != null)
        {
            target.localScale = finalScale;
        }
    }

    private IEnumerator ShowGameOverSoon()
    {
        yield return new WaitForSeconds(0.55f);
        gameOverRoutine = null;

        if (board == null || !board.IsGameOver)
        {
            yield break;
        }

        gameOverTitle.text = GetResultText();
        gameOverReason.text = GetReasonText();
        gameOverPanel.SetActive(true);
    }

    private void UpdateHud()
    {
        if (board == null)
        {
            return;
        }

        Team side = board.SideToMove;
        turnText.text = side == Team.White ? "TURNO DE LAS BLANCAS" : "TURNO DE LAS NEGRAS";
        turnText.color = side == Team.White ? UITheme.LightGold : new Color(0.82f, 0.86f, 0.95f, 1f);

        if (board.IsGameOver)
        {
            SetStatus(GetResultText(), UITheme.LightGold);
        }
        else if (board.IsInCheck(side))
        {
            SetStatus(side == Team.White ? "JAQUE AL REY BLANCO" : "JAQUE AL REY NEGRO", new Color(1f, 0.56f, 0.42f, 1f));
        }
        else if (selectedMoves != null)
        {
            string suffix = selectedMoves.Count == 1 ? " MOVIMIENTO LEGAL" : " MOVIMIENTOS LEGALES";
            SetStatus(selectedMoves.Count + suffix, UITheme.MutedCream);
        }
        else
        {
            SetStatus("SELECCIONA UNA PIEZA PARA MOVER", UITheme.MutedCream);
        }

        moveText.text = "MOV. " + board.FullmoveNumber + " · " + (side == Team.White ? "BLANCAS" : "NEGRAS");

        if (pauseUndoButton != null)
        {
            pauseUndoButton.interactable = board.CanUndo;
        }

        if (soundButton != null)
        {
            bool muted = SoundManager.Instance != null && SoundManager.Instance.Muted;
            UITheme.SetButtonLabel(soundButton, muted ? "MUDO" : "SONIDO");
        }
    }

    private void SetStatus(string message, Color color)
    {
        statusText.text = message;
        statusText.color = color;
    }

    private string GetResultText()
    {
        if (board.Result == GameResult.WhiteWin)
        {
            return "¡GANAN LAS BLANCAS!";
        }

        if (board.Result == GameResult.BlackWin)
        {
            return "¡GANAN LAS NEGRAS!";
        }

        return "TABLAS";
    }

    private string GetReasonText()
    {
        switch (board.Reason)
        {
            case EndReason.Checkmate: return "Jaque mate";
            case EndReason.Stalemate: return "Rey ahogado";
            case EndReason.InsufficientMaterial: return "Material insuficiente";
            case EndReason.FiftyMove: return "Regla de 50 movimientos";
            case EndReason.ThreefoldRepetition: return "Triple repetición";
            default: return "Partida terminada";
        }
    }

    private void OpenPause()
    {
        if (board == null || paused || gameOver || promotionOpen || confirmOpen)
        {
            return;
        }

        paused = true;
        pausePanel.SetActive(true);
        pauseUndoButton.interactable = board.CanUndo;
        SoundManager.Ensure().PlayClick();
    }

    private void ResumeGame()
    {
        if (!paused)
        {
            return;
        }

        paused = false;
        pausePanel.SetActive(false);
        SoundManager.Ensure().PlayClick();
    }

    private void OpenInstructionsFromPause()
    {
        if (!paused)
        {
            return;
        }

        InstructionsView.Show(uiRoot.transform, null);
    }

    private void ToggleSound()
    {
        SoundManager sound = SoundManager.Ensure();
        bool muted = sound.ToggleMute();
        UITheme.SetButtonLabel(soundButton, muted ? "MUDO" : "SONIDO");
        if (!muted)
        {
            sound.PlayClick();
        }
    }

    private void ShowConfirm(string message, Action action)
    {
        confirmOpen = true;
        confirmAction = action;
        confirmMessage.text = message;
        confirmPanel.SetActive(true);
        SoundManager.Ensure().PlayClick();
    }

    private void ConfirmAction()
    {
        if (!confirmOpen)
        {
            return;
        }

        Action action = confirmAction;
        confirmOpen = false;
        confirmAction = null;
        confirmPanel.SetActive(false);
        SoundManager.Ensure().PlayClick();

        if (action != null)
        {
            action();
        }
    }

    private void HideConfirm()
    {
        confirmOpen = false;
        confirmAction = null;
        confirmPanel.SetActive(false);
        SoundManager.Ensure().PlayClick();
    }

    private void ConfirmRestart()
    {
        ShowConfirm("¿Reiniciar la partida actual?", RestartGame);
    }

    private void ConfirmMainMenu()
    {
        ShowConfirm("¿Volver al menú principal? Se perderá la partida.", delegate
        {
            SceneManager.LoadScene("start");
        });
    }

    private void ConfirmExit()
    {
        ShowConfirm("¿Cerrar Monkchess?", delegate
        {
            Application.Quit();
        });
    }

    private void HideAllPanels()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (promotionPanel != null) promotionPanel.SetActive(false);
        if (confirmPanel != null) confirmPanel.SetActive(false);
    }

    private bool IsBlocked()
    {
        return board == null || paused || promotionOpen || gameOver || confirmOpen || InstructionsView.IsAnyOpen;
    }

    private static Vector3 SquarePosition(Vector2Int square)
    {
        return new Vector3(square.x * 0.66f - 2.3f, square.y * 0.66f - 2.35f, 0f);
    }

    private static GameObject CreateSquareMarker(string name, Vector2Int square, Color color, int sortingOrder)
    {
        GameObject marker = new GameObject(name);
        marker.transform.position = SquarePosition(square);
        marker.transform.localScale = new Vector3(0.62f, 0.62f, 1f);
        SpriteRenderer renderer = marker.AddComponent<SpriteRenderer>();
        renderer.sprite = GetSquareSprite();
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return marker;
    }

    private static Sprite GetSquareSprite()
    {
        if (squareSprite == null)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            texture.filterMode = FilterMode.Bilinear;
            texture.hideFlags = HideFlags.HideAndDontSave;
            squareSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            squareSprite.hideFlags = HideFlags.HideAndDontSave;
        }

        return squareSprite;
    }

    private Sprite GetPieceSprite(PieceType type, Team team)
    {
        if (pieceTemplate != null)
        {
            return pieceTemplate.GetSprite(type, team);
        }

        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                if (pieceViews[x, y] != null)
                {
                    return pieceViews[x, y].GetSprite(type, team);
                }
            }
        }

        return null;
    }

    private static void HideLegacyUi()
    {
        string[] names = { "gameover", "close" };
        for (int i = 0; i < names.Length; i++)
        {
            GameObject objectToHide = GameObject.Find(names[i]);
            if (objectToHide != null)
            {
                objectToHide.SetActive(false);
            }
        }

        string[] tags = { "turn1", "turn2", "win", "whitewon", "blackwon", "restart", "gameover" };
        for (int i = 0; i < tags.Length; i++)
        {
            try
            {
                GameObject objectToHide = GameObject.FindGameObjectWithTag(tags[i]);
                if (objectToHide != null)
                {
                    objectToHide.SetActive(false);
                }
            }
            catch (UnityException)
            {
            }
        }
    }

    public void Quitgame()
    {
        Application.Quit();
    }

    private void BuildInterface()
    {
        GameObject canvasObject = UITheme.CreateCanvas("MonkchessGameUI", 40);
        uiRoot = canvasObject;
        Transform root = canvasObject.transform;

        Image bar = UITheme.CreatePanel(root, "HudBar", new Color32(36, 22, 14, 238), false);
        RectTransform barRect = bar.rectTransform;
        barRect.anchorMin = new Vector2(0f, 1f);
        barRect.anchorMax = new Vector2(1f, 1f);
        barRect.pivot = new Vector2(0.5f, 1f);
        barRect.anchoredPosition = new Vector2(0f, -18f);
        barRect.sizeDelta = new Vector2(-36f, 74f);

        Image barLine = UITheme.CreateImage(bar.transform, "GoldLine", UITheme.Gold, false);
        barLine.rectTransform.anchorMin = new Vector2(0f, 0f);
        barLine.rectTransform.anchorMax = new Vector2(1f, 0f);
        barLine.rectTransform.pivot = new Vector2(0.5f, 0f);
        barLine.rectTransform.anchoredPosition = new Vector2(0f, 0f);
        barLine.rectTransform.sizeDelta = new Vector2(-32f, 2f);

        Text title = UITheme.CreateText(bar.transform, "Title", "MONKCHESS", 25, UITheme.Cream, TextAnchor.MiddleLeft);
        UITheme.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -8f), new Vector2(360f, 32f));
        title.fontStyle = FontStyle.Bold;
        UITheme.AddShadow(title.gameObject, new Color(0f, 0f, 0f, 0.4f), new Vector2(1f, -1f));

        Text subtitle = UITheme.CreateText(bar.transform, "Subtitle", "PARTIDA LOCAL · 2 JUGADORES", 11, UITheme.MutedCream, TextAnchor.MiddleLeft);
        UITheme.Place(subtitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(25f, -42f), new Vector2(400f, 18f));

        Image turnPill = UITheme.CreatePanel(bar.transform, "TurnPill", new Color(1f, 1f, 1f, 0.08f), false);
        UITheme.Place(turnPill.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(330f, 42f));

        turnText = UITheme.CreateText(turnPill.transform, "TurnText", "TURNO DE LAS BLANCAS", 17, UITheme.LightGold, TextAnchor.MiddleCenter);
        UITheme.Stretch(turnText.rectTransform, 8f, 8f, 0f, 0f);
        turnText.fontStyle = FontStyle.Bold;

        Button menuButton = UITheme.CreateButton(bar.transform, "MenuButton", "MENÚ", new Vector2(104f, 40f), UITheme.PanelBrown, UITheme.Cream, 15, OpenPause);
        UITheme.Place(menuButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -17f), new Vector2(104f, 40f));

        soundButton = UITheme.CreateButton(bar.transform, "SoundButton", "SONIDO", new Vector2(104f, 40f), UITheme.PanelBrown, UITheme.Cream, 15, ToggleSound);
        UITheme.Place(soundButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-132f, -17f), new Vector2(104f, 40f));

        statusText = UITheme.CreateText(root, "Status", "SELECCIONA UNA PIEZA PARA MOVER", 17, UITheme.MutedCream, TextAnchor.MiddleCenter);
        UITheme.Place(statusText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 38f), new Vector2(900f, 30f));
        UITheme.AddShadow(statusText.gameObject, new Color(0f, 0f, 0f, 0.6f), new Vector2(1f, -1f));

        moveText = UITheme.CreateText(root, "MoveCounter", "MOV. 1 · BLANCAS", 13, UITheme.MutedCream, TextAnchor.MiddleRight);
        UITheme.Place(moveText.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-26f, 40f), new Vector2(260f, 24f));

        Text hint = UITheme.CreateText(root, "PauseHint", "ESC · PAUSA", 12, new Color(0.84f, 0.73f, 0.56f, 0.85f), TextAnchor.MiddleLeft);
        UITheme.Place(hint.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(26f, 40f), new Vector2(220f, 24f));

        BuildGameOverPanel(root);
        BuildPausePanel(root);
        BuildPromotionPanel(root);
        BuildConfirmPanel(root);
    }

    private void BuildGameOverPanel(Transform root)
    {
        gameOverPanel = new GameObject("GameOver", typeof(RectTransform));
        RectTransform rootRect = gameOverPanel.GetComponent<RectTransform>();
        rootRect.SetParent(root, false);
        UITheme.Stretch(rootRect, 0f, 0f, 0f, 0f);

        UITheme.CreateFullScreenOverlay(gameOverPanel.transform, "Dim", new Color(0.07f, 0.04f, 0.02f, 0.84f), true);

        Image card = UITheme.CreatePanel(gameOverPanel.transform, "Card", UITheme.DeepBrown, true);
        UITheme.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 460f));

        Image accent = UITheme.CreateImage(card.transform, "Accent", UITheme.Gold, false);
        UITheme.Place(accent.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(130f, 3f));

        gameOverTitle = UITheme.CreateText(card.transform, "Title", "¡GANAN LAS BLANCAS!", 36, UITheme.Cream, TextAnchor.MiddleCenter);
        UITheme.Place(gameOverTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(560f, 58f));
        gameOverTitle.fontStyle = FontStyle.Bold;
        UITheme.AddShadow(gameOverTitle.gameObject, new Color(0f, 0f, 0f, 0.45f), new Vector2(2f, -2f));

        gameOverReason = UITheme.CreateText(card.transform, "Reason", "Jaque mate", 19, UITheme.MutedCream, TextAnchor.MiddleCenter);
        UITheme.Place(gameOverReason.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 52f), new Vector2(520f, 30f));

        Text message = UITheme.CreateText(card.transform, "Message", "La partida ha terminado.", 15, UITheme.MutedCream, TextAnchor.MiddleCenter);
        UITheme.Place(message.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 18f), new Vector2(520f, 24f));

        Button restart = UITheme.CreateButton(card.transform, "Restart", "JUGAR OTRA VEZ", new Vector2(250f, 52f), UITheme.Gold, UITheme.Ink, 16, RestartGame);
        UITheme.Place(restart.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-138f, 44f), new Vector2(250f, 52f));

        Button menu = UITheme.CreateButton(card.transform, "MainMenu", "MENÚ PRINCIPAL", new Vector2(250f, 52f), UITheme.PanelBrown, UITheme.Cream, 16, ConfirmMainMenu);
        UITheme.Place(menu.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(138f, 44f), new Vector2(250f, 52f));

        gameOverPanel.SetActive(false);
    }

    private void BuildPausePanel(Transform root)
    {
        pausePanel = new GameObject("Pause", typeof(RectTransform));
        RectTransform rootRect = pausePanel.GetComponent<RectTransform>();
        rootRect.SetParent(root, false);
        UITheme.Stretch(rootRect, 0f, 0f, 0f, 0f);

        UITheme.CreateFullScreenOverlay(pausePanel.transform, "Dim", UITheme.Dim, true);

        Image card = UITheme.CreatePanel(pausePanel.transform, "Card", UITheme.DeepBrown, true);
        UITheme.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(540f, 620f));

        Image accent = UITheme.CreateImage(card.transform, "Accent", UITheme.Gold, false);
        UITheme.Place(accent.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(110f, 3f));

        Text title = UITheme.CreateText(card.transform, "Title", "PAUSA", 38, UITheme.Cream, TextAnchor.MiddleCenter);
        UITheme.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(400f, 48f));
        title.fontStyle = FontStyle.Bold;

        Text subtitle = UITheme.CreateText(card.transform, "Subtitle", "La partida está en pausa", 15, UITheme.MutedCream, TextAnchor.MiddleCenter);
        UITheme.Place(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(400f, 24f));

        Button resume = CreatePauseButton(card.transform, "Resume", "CONTINUAR", 0, UITheme.Gold, UITheme.Ink, ResumeGame);
        UITheme.SetButtonLabel(resume, "CONTINUAR");

        pauseUndoButton = CreatePauseButton(card.transform, "Undo", "DESHACER", 1, UITheme.PanelBrown, UITheme.Cream, UndoMove);
        CreatePauseButton(card.transform, "Restart", "REINICIAR", 2, UITheme.PanelBrown, UITheme.Cream, ConfirmRestart);
        CreatePauseButton(card.transform, "Instructions", "INSTRUCCIONES", 3, UITheme.PanelBrown, UITheme.Cream, OpenInstructionsFromPause);
        CreatePauseButton(card.transform, "MainMenu", "MENÚ PRINCIPAL", 4, UITheme.PanelBrown, UITheme.Cream, ConfirmMainMenu);
        CreatePauseButton(card.transform, "Exit", "SALIR", 5, UITheme.Danger, UITheme.Cream, ConfirmExit);

        pausePanel.SetActive(false);
    }

    private Button CreatePauseButton(Transform parent, string name, string label, int index, Color background, Color textColor, Action action)
    {
        Button button = UITheme.CreateButton(parent, name, label, new Vector2(360f, 52f), background, textColor, 16, delegate
        {
            action();
        });

        UITheme.Place(button.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -130f - index * 62f), new Vector2(360f, 52f));
        return button;
    }

    private void BuildPromotionPanel(Transform root)
    {
        promotionPanel = new GameObject("Promotion", typeof(RectTransform));
        RectTransform rootRect = promotionPanel.GetComponent<RectTransform>();
        rootRect.SetParent(root, false);
        UITheme.Stretch(rootRect, 0f, 0f, 0f, 0f);

        UITheme.CreateFullScreenOverlay(promotionPanel.transform, "Dim", UITheme.Dim, true);

        Image card = UITheme.CreatePanel(promotionPanel.transform, "Card", UITheme.DeepBrown, true);
        UITheme.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 360f));

        Image accent = UITheme.CreateImage(card.transform, "Accent", UITheme.Gold, false);
        UITheme.Place(accent.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(110f, 3f));

        Text title = UITheme.CreateText(card.transform, "Title", "PROMOCIÓN", 34, UITheme.Cream, TextAnchor.MiddleCenter);
        UITheme.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -38f), new Vector2(500f, 44f));
        title.fontStyle = FontStyle.Bold;

        Text subtitle = UITheme.CreateText(card.transform, "Subtitle", "Elige la pieza para tu peón", 15, UITheme.MutedCream, TextAnchor.MiddleCenter);
        UITheme.Place(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -82f), new Vector2(500f, 24f));

        promotionImages = new Image[PromotionTypes.Length];
        for (int i = 0; i < PromotionTypes.Length; i++)
        {
            PieceType type = PromotionTypes[i];
            int index = i;
            Button button = UITheme.CreateButton(card.transform, "Promotion" + PromotionNames[i], string.Empty, new Vector2(150f, 190f), UITheme.Cream, UITheme.Ink, 15, delegate
            {
                ChoosePromotion(PromotionTypes[index]);
            });

            UITheme.Place(button.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-255f + i * 170f, -20f), new Vector2(150f, 190f));

            Text label = button.GetComponentInChildren<Text>();
            label.text = PromotionNames[i];
            label.fontSize = 15;
            label.alignment = TextAnchor.LowerCenter;
            label.rectTransform.anchorMin = new Vector2(0f, 0f);
            label.rectTransform.anchorMax = new Vector2(1f, 0f);
            label.rectTransform.pivot = new Vector2(0.5f, 0f);
            label.rectTransform.anchoredPosition = new Vector2(0f, 8f);
            label.rectTransform.sizeDelta = new Vector2(-10f, 28f);

            Image pieceImage = UITheme.CreateImage(button.transform, "Piece", Color.white, false);
            UITheme.Place(pieceImage.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(94f, 128f));
            pieceImage.preserveAspect = true;
            UITheme.AddShadow(pieceImage.gameObject, new Color(0f, 0f, 0f, 0.45f), new Vector2(2f, -2f));
            promotionImages[i] = pieceImage;
        }

        promotionPanel.SetActive(false);
    }

    private void BuildConfirmPanel(Transform root)
    {
        confirmPanel = new GameObject("Confirm", typeof(RectTransform));
        RectTransform rootRect = confirmPanel.GetComponent<RectTransform>();
        rootRect.SetParent(root, false);
        UITheme.Stretch(rootRect, 0f, 0f, 0f, 0f);

        UITheme.CreateFullScreenOverlay(confirmPanel.transform, "Dim", new Color(0.07f, 0.04f, 0.02f, 0.64f), true);

        Image card = UITheme.CreatePanel(confirmPanel.transform, "Card", UITheme.DeepBrown, true);
        UITheme.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(580f, 300f));

        Text title = UITheme.CreateText(card.transform, "Title", "CONFIRMAR", 28, UITheme.Cream, TextAnchor.MiddleCenter);
        UITheme.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(480f, 36f));
        title.fontStyle = FontStyle.Bold;

        confirmMessage = UITheme.CreateText(card.transform, "Message", string.Empty, 17, UITheme.MutedCream, TextAnchor.MiddleCenter);
        UITheme.Place(confirmMessage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 34f), new Vector2(500f, 76f));

        Button cancel = UITheme.CreateButton(card.transform, "Cancel", "CANCELAR", new Vector2(210f, 50f), UITheme.PanelBrown, UITheme.Cream, 16, HideConfirm);
        UITheme.Place(cancel.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-118f, 34f), new Vector2(210f, 50f));

        Button confirm = UITheme.CreateButton(card.transform, "Confirm", "CONFIRMAR", new Vector2(210f, 50f), UITheme.Gold, UITheme.Ink, 16, ConfirmAction);
        UITheme.Place(confirm.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(118f, 34f), new Vector2(210f, 50f));

        confirmPanel.SetActive(false);
    }
}
