using System;
using System.Reflection;
using MonkChess;
using UnityEditor;
using UnityEngine;

public static class ChessDiagnostics
{
    [MenuItem("Tools/Monkchess/Run Chess Diagnostics")]
    public static void Run()
    {
        TestInitialPosition();
        TestFoolsMate();
        TestKingsideCastle();
        TestEnPassant();
        TestPromotion();
        TestStalemate();
        TestInsufficientMaterial();
        TestFiftyMoveDraw();
        TestThreefoldRepetition();
        Debug.Log("CHESS DIAGNOSTICS: ALL PASSED");
    }

    private static void TestInitialPosition()
    {
        ChessBoard board = new ChessBoard();
        Check(board.GenerateLegalMoves().Count == 20, "Initial legal move count");
        Check(board.GetPiece(new Vector2Int(4, 0)).Type == PieceType.King, "White king");
        Check(board.GetPiece(new Vector2Int(4, 7)).Type == PieceType.King, "Black king");
        Check(board.SideToMove == Team.White, "White starts");
    }

    private static void TestFoolsMate()
    {
        ChessBoard board = new ChessBoard();
        Play(board, 5, 1, 5, 2);
        Play(board, 4, 6, 4, 4);
        Play(board, 6, 1, 6, 3);
        Play(board, 3, 7, 7, 3);

        Check(board.Result == GameResult.BlackWin, "Fools mate winner");
        Check(board.Reason == EndReason.Checkmate, "Fools mate reason");
        Check(board.GenerateLegalMoves().Count == 0, "Fools mate no escapes");
    }

    private static void TestKingsideCastle()
    {
        ChessBoard board = new ChessBoard();
        Play(board, 4, 1, 4, 3);
        Play(board, 4, 6, 4, 4);
        Play(board, 6, 0, 5, 2);
        Play(board, 1, 7, 2, 5);
        Play(board, 5, 0, 2, 3);
        Play(board, 6, 7, 5, 5);

        ChessMove castle = board.GenerateLegalMoves(new Vector2Int(4, 0)).Find(delegate(ChessMove move)
        {
            return move.To == new Vector2Int(6, 0) && move.IsCastle;
        });

        Check(castle.IsCastle, "Castle is offered");
        Play(board, 4, 0, 6, 0);
        Check(board.GetPiece(new Vector2Int(6, 0)).Type == PieceType.King, "Castled king");
        Check(board.GetPiece(new Vector2Int(5, 0)).Type == PieceType.Rook, "Castled rook");
        Check(board.GetPiece(new Vector2Int(7, 0)).IsEmpty, "Old rook square empty");
        Check(!board.WhiteKingsideCastle, "White kingside right consumed");
    }

    private static void TestEnPassant()
    {
        ChessBoard board = new ChessBoard();
        Play(board, 4, 1, 4, 3);
        Play(board, 0, 6, 0, 5);
        Play(board, 4, 3, 4, 4);
        Play(board, 3, 6, 3, 4);

        ChessMove enPassant = board.GenerateLegalMoves(new Vector2Int(4, 4)).Find(delegate(ChessMove move)
        {
            return move.To == new Vector2Int(3, 5) && move.IsEnPassant;
        });

        Check(enPassant.IsEnPassant, "En passant is offered");
        Play(board, 4, 4, 3, 5);
        Check(board.GetPiece(new Vector2Int(3, 5)).Type == PieceType.Pawn, "En passant pawn landing");
        Check(board.GetPiece(new Vector2Int(3, 5)).Team == Team.White, "En passant pawn team");
        Check(board.GetPiece(new Vector2Int(3, 4)).IsEmpty, "Captured pawn removed");
    }

    private static void TestPromotion()
    {
        ChessBoard board = CreateEmptyBoard();
        SetPiece(board, 4, 0, PieceType.King, Team.White);
        SetPiece(board, 7, 7, PieceType.King, Team.Black);
        SetPiece(board, 0, 6, PieceType.Pawn, Team.White);
        SetSide(board, Team.White);

        Play(board, 0, 6, 0, 7, PieceType.Queen);
        Piece promoted = board.GetPiece(new Vector2Int(0, 7));
        Check(promoted.Type == PieceType.Queen && promoted.Team == Team.White, "Promotion to queen");
        Check(board.GetPiece(new Vector2Int(0, 6)).IsEmpty, "Promotion origin empty");
    }

    private static void TestStalemate()
    {
        ChessBoard board = CreateEmptyBoard();
        SetPiece(board, 0, 7, PieceType.King, Team.Black);
        SetPiece(board, 2, 5, PieceType.King, Team.White);
        SetPiece(board, 1, 5, PieceType.Queen, Team.White);
        SetSide(board, Team.Black);
        Evaluate(board);

        Check(!board.IsInCheck(Team.Black), "Stalemate is not check");
        Check(board.GenerateLegalMoves(Team.Black).Count == 0, "Stalemate has no moves");
        Check(board.Result == GameResult.Draw && board.Reason == EndReason.Stalemate, "Stalemate result");
    }

    private static void TestInsufficientMaterial()
    {
        ChessBoard board = CreateEmptyBoard();
        SetPiece(board, 4, 0, PieceType.King, Team.White);
        SetPiece(board, 4, 7, PieceType.King, Team.Black);
        SetSide(board, Team.White);
        Evaluate(board);

        Check(board.Result == GameResult.Draw && board.Reason == EndReason.InsufficientMaterial, "King vs king draw");
    }

    private static void TestFiftyMoveDraw()
    {
        ChessBoard board = CreateEmptyBoard();
        SetPiece(board, 4, 0, PieceType.King, Team.White);
        SetPiece(board, 4, 7, PieceType.King, Team.Black);
        SetPiece(board, 0, 0, PieceType.Rook, Team.White);
        SetSide(board, Team.White);
        SetHalfmoveClock(board, 99);

        Play(board, 0, 0, 0, 1);
        Check(board.Result == GameResult.Draw && board.Reason == EndReason.FiftyMove, "Fifty move draw");
    }

    private static void TestThreefoldRepetition()
    {
        ChessBoard board = new ChessBoard();
        for (int cycle = 0; cycle < 2; cycle++)
        {
            Play(board, 6, 0, 5, 2);
            Play(board, 6, 7, 5, 5);
            Play(board, 5, 2, 6, 0);
            Play(board, 5, 5, 6, 7);
        }

        Check(board.Result == GameResult.Draw, "Threefold draw result");
        Check(board.Reason == EndReason.ThreefoldRepetition, "Threefold reason");
    }

    private static void Play(ChessBoard board, int fromX, int fromY, int toX, int toY, PieceType promotion = PieceType.None)
    {
        ChessMove move = new ChessMove(new Vector2Int(fromX, fromY), new Vector2Int(toX, toY), promotion);
        MoveUndo undo = board.MakeMove(move);
        Check(undo != null, "Legal move " + move);
    }

    private static ChessBoard CreateEmptyBoard()
    {
        ChessBoard board = new ChessBoard();
        FieldInfo field = typeof(ChessBoard).GetField("squares", BindingFlags.NonPublic | BindingFlags.Instance);
        Piece[,] squares = (Piece[,])field.GetValue(board);
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                squares[x, y] = Piece.Empty;
            }
        }

        return board;
    }

    private static void SetPiece(ChessBoard board, int x, int y, PieceType type, Team team)
    {
        FieldInfo field = typeof(ChessBoard).GetField("squares", BindingFlags.NonPublic | BindingFlags.Instance);
        Piece[,] squares = (Piece[,])field.GetValue(board);
        squares[x, y] = new Piece(type, team);
    }

    private static void SetSide(ChessBoard board, Team team)
    {
        typeof(ChessBoard).GetProperty("SideToMove").SetValue(board, team);
    }

    private static void SetHalfmoveClock(ChessBoard board, int clock)
    {
        typeof(ChessBoard).GetProperty("HalfmoveClock").SetValue(board, clock);
    }

    private static void Evaluate(ChessBoard board)
    {
        MethodInfo method = typeof(ChessBoard).GetMethod("EvaluatePosition", BindingFlags.NonPublic | BindingFlags.Instance);
        method.Invoke(board, null);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception("CHESS DIAGNOSTIC FAILED: " + message);
        }
    }
}
