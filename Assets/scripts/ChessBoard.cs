using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MonkChess
{
    public class MoveUndo
    {
        public ChessMove Move;
        public Piece MovingPiece;
        public Piece CapturedPiece;
        public Vector2Int CapturedSquare;
        public bool WasEnPassant;
        public bool WasCastle;
        public Vector2Int RookFrom;
        public Vector2Int RookTo;
        public Piece RookPiece;
        public Vector2Int? PreviousEnPassant;
        public bool PreviousWhiteKingside;
        public bool PreviousWhiteQueenside;
        public bool PreviousBlackKingside;
        public bool PreviousBlackQueenside;
        public int PreviousHalfmoveClock;
        public int PreviousFullmoveNumber;
        public Team PreviousSideToMove;
        public GameResult PreviousResult;
        public EndReason PreviousReason;
    }

    public class ChessBoard
    {
        public const int Size = 8;

        private readonly Piece[,] squares = new Piece[Size, Size];
        private readonly Stack<MoveUndo> undoStack = new Stack<MoveUndo>();
        private readonly List<string> positionKeys = new List<string>();

        public Team SideToMove { get; private set; }
        public int HalfmoveClock { get; private set; }
        public int FullmoveNumber { get; private set; }
        public bool WhiteKingsideCastle { get; private set; }
        public bool WhiteQueensideCastle { get; private set; }
        public bool BlackKingsideCastle { get; private set; }
        public bool BlackQueensideCastle { get; private set; }
        public Vector2Int? EnPassantTarget { get; private set; }
        public GameResult Result { get; private set; }
        public EndReason Reason { get; private set; }

        public bool IsGameOver
        {
            get { return Result != GameResult.None; }
        }

        public bool CanUndo
        {
            get { return undoStack.Count > 0; }
        }

        public ChessBoard()
        {
            Reset();
        }

        public void Reset()
        {
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    squares[x, y] = Piece.Empty;
                }
            }

            PieceType[] backRank =
            {
                PieceType.Rook,
                PieceType.Knight,
                PieceType.Bishop,
                PieceType.Queen,
                PieceType.King,
                PieceType.Bishop,
                PieceType.Knight,
                PieceType.Rook
            };

            for (int x = 0; x < Size; x++)
            {
                squares[x, 0] = new Piece(backRank[x], Team.White);
                squares[x, 1] = new Piece(PieceType.Pawn, Team.White);
                squares[x, 6] = new Piece(PieceType.Pawn, Team.Black);
                squares[x, 7] = new Piece(backRank[x], Team.Black);
            }

            SideToMove = Team.White;
            HalfmoveClock = 0;
            FullmoveNumber = 1;
            WhiteKingsideCastle = true;
            WhiteQueensideCastle = true;
            BlackKingsideCastle = true;
            BlackQueensideCastle = true;
            EnPassantTarget = null;
            Result = GameResult.None;
            Reason = EndReason.None;

            undoStack.Clear();
            positionKeys.Clear();
            positionKeys.Add(GetPositionKey());
        }

        public bool InBounds(Vector2Int square)
        {
            return square.x >= 0 && square.x < Size && square.y >= 0 && square.y < Size;
        }

        public Piece GetPiece(Vector2Int square)
        {
            if (!InBounds(square))
            {
                return Piece.Empty;
            }

            return squares[square.x, square.y];
        }

        public Piece GetPiece(int x, int y)
        {
            return GetPiece(new Vector2Int(x, y));
        }

        public List<ChessMove> GenerateLegalMoves()
        {
            return GenerateLegalMoves(SideToMove);
        }

        public List<ChessMove> GenerateLegalMoves(Team team)
        {
            List<ChessMove> legalMoves = new List<ChessMove>();
            if (team == Team.None)
            {
                return legalMoves;
            }

            List<ChessMove> pseudoMoves = GeneratePseudoLegalMoves(team);
            for (int i = 0; i < pseudoMoves.Count; i++)
            {
                ChessMove move = pseudoMoves[i];
                MoveUndo undo = ApplyMove(move);
                bool leavesKingSafe = !IsInCheck(team);
                UndoMove(undo);

                if (leavesKingSafe)
                {
                    legalMoves.Add(move);
                }
            }

            return legalMoves;
        }

        public List<ChessMove> GenerateLegalMoves(Vector2Int from)
        {
            List<ChessMove> moves = new List<ChessMove>();
            if (!InBounds(from))
            {
                return moves;
            }

            Piece piece = GetPiece(from);
            if (piece.IsEmpty || piece.Team != SideToMove)
            {
                return moves;
            }

            List<ChessMove> allMoves = GenerateLegalMoves(piece.Team);
            for (int i = 0; i < allMoves.Count; i++)
            {
                if (allMoves[i].From == from)
                {
                    moves.Add(allMoves[i]);
                }
            }

            return moves;
        }

        public bool IsLegalMove(ChessMove move)
        {
            ChessMove legalMove;
            return TryFindLegalMove(move, out legalMove);
        }

        public MoveUndo MakeMove(ChessMove move)
        {
            if (Result != GameResult.None)
            {
                return null;
            }

            ChessMove legalMove;
            if (!TryFindLegalMove(move, out legalMove))
            {
                return null;
            }

            MoveUndo undo = ApplyMove(legalMove);
            undoStack.Push(undo);
            positionKeys.Add(GetPositionKey());
            EvaluatePosition();
            return undo;
        }

        private bool TryFindLegalMove(ChessMove requested, out ChessMove legalMove)
        {
            legalMove = default(ChessMove);
            if (!InBounds(requested.From) || !InBounds(requested.To))
            {
                return false;
            }

            Piece piece = GetPiece(requested.From);
            if (piece.IsEmpty || piece.Team != SideToMove)
            {
                return false;
            }

            List<ChessMove> legalMoves = GenerateLegalMoves(requested.From);
            for (int i = 0; i < legalMoves.Count; i++)
            {
                ChessMove candidate = legalMoves[i];
                if (candidate.From != requested.From || candidate.To != requested.To)
                {
                    continue;
                }

                if (candidate.Promotion != PieceType.None)
                {
                    if (requested.Promotion != candidate.Promotion)
                    {
                        continue;
                    }
                }
                else if (requested.Promotion != PieceType.None)
                {
                    continue;
                }

                legalMove = candidate;
                return true;
            }

            return false;
        }

        public bool UndoLastMove(out MoveUndo undoneMove)
        {
            if (undoStack.Count == 0)
            {
                undoneMove = null;
                return false;
            }

            undoneMove = undoStack.Pop();
            UndoMove(undoneMove);

            if (positionKeys.Count > 1)
            {
                positionKeys.RemoveAt(positionKeys.Count - 1);
            }

            return true;
        }

        public bool IsInCheck(Team team)
        {
            Vector2Int? kingSquare = FindKing(team);
            if (!kingSquare.HasValue)
            {
                return false;
            }

            return IsSquareAttacked(kingSquare.Value, Opponent(team));
        }

        public bool IsSquareAttacked(Vector2Int square, Team byTeam)
        {
            if (byTeam == Team.None)
            {
                return false;
            }

            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    Piece piece = squares[x, y];
                    if (piece.IsEmpty || piece.Team != byTeam)
                    {
                        continue;
                    }

                    Vector2Int from = new Vector2Int(x, y);

                    switch (piece.Type)
                    {
                        case PieceType.Pawn:
                            int direction = piece.Team == Team.White ? 1 : -1;
                            if (square.y - from.y == direction && Mathf.Abs(square.x - from.x) == 1)
                            {
                                return true;
                            }
                            break;

                        case PieceType.Knight:
                            if (IsKnightStep(from, square))
                            {
                                return true;
                            }
                            break;

                        case PieceType.Bishop:
                            if (RayHits(from, square, DiagonalDirections))
                            {
                                return true;
                            }
                            break;

                        case PieceType.Rook:
                            if (RayHits(from, square, OrthogonalDirections))
                            {
                                return true;
                            }
                            break;

                        case PieceType.Queen:
                            if (RayHits(from, square, DiagonalDirections) ||
                                RayHits(from, square, OrthogonalDirections))
                            {
                                return true;
                            }
                            break;

                        case PieceType.King:
                            if (Mathf.Abs(square.x - from.x) <= 1 &&
                                Mathf.Abs(square.y - from.y) <= 1 &&
                                from != square)
                            {
                                return true;
                            }
                            break;
                    }
                }
            }

            return false;
        }

        private static readonly Vector2Int[] KnightOffsets =
        {
            new Vector2Int(1, 2), new Vector2Int(-1, 2),
            new Vector2Int(1, -2), new Vector2Int(-1, -2),
            new Vector2Int(2, 1), new Vector2Int(2, -1),
            new Vector2Int(-2, 1), new Vector2Int(-2, -1)
        };

        private static readonly Vector2Int[] DiagonalDirections =
        {
            new Vector2Int(1, 1), new Vector2Int(1, -1),
            new Vector2Int(-1, 1), new Vector2Int(-1, -1)
        };

        private static readonly Vector2Int[] OrthogonalDirections =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0),
            new Vector2Int(0, 1), new Vector2Int(0, -1)
        };

        private List<ChessMove> GeneratePseudoLegalMoves(Team team)
        {
            List<ChessMove> moves = new List<ChessMove>();

            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    Piece piece = squares[x, y];
                    if (piece.IsEmpty || piece.Team != team)
                    {
                        continue;
                    }

                    Vector2Int from = new Vector2Int(x, y);
                    switch (piece.Type)
                    {
                        case PieceType.Pawn:
                            AddPawnMoves(moves, from, piece);
                            break;
                        case PieceType.Knight:
                            AddKnightMoves(moves, from, piece);
                            break;
                        case PieceType.Bishop:
                            AddSlidingMoves(moves, from, piece, DiagonalDirections);
                            break;
                        case PieceType.Rook:
                            AddSlidingMoves(moves, from, piece, OrthogonalDirections);
                            break;
                        case PieceType.Queen:
                            AddSlidingMoves(moves, from, piece, DiagonalDirections);
                            AddSlidingMoves(moves, from, piece, OrthogonalDirections);
                            break;
                        case PieceType.King:
                            AddKingMoves(moves, from, piece);
                            AddCastlingMoves(moves, from, piece);
                            break;
                    }
                }
            }

            return moves;
        }

        private void AddPawnMoves(List<ChessMove> moves, Vector2Int from, Piece pawn)
        {
            int direction = pawn.Team == Team.White ? 1 : -1;
            int startRank = pawn.Team == Team.White ? 1 : 6;
            int promotionRank = pawn.Team == Team.White ? 7 : 0;

            Vector2Int oneStep = new Vector2Int(from.x, from.y + direction);
            if (InBounds(oneStep) && GetPiece(oneStep).IsEmpty)
            {
                AddPawnMove(moves, from, oneStep, promotionRank);

                Vector2Int twoSteps = new Vector2Int(from.x, from.y + direction * 2);
                if (from.y == startRank && InBounds(twoSteps) && GetPiece(twoSteps).IsEmpty)
                {
                    moves.Add(new ChessMove(from, twoSteps));
                }
            }

            int[] captureOffsets = { -1, 1 };
            for (int i = 0; i < captureOffsets.Length; i++)
            {
                Vector2Int target = new Vector2Int(from.x + captureOffsets[i], from.y + direction);
                if (!InBounds(target))
                {
                    continue;
                }

                Piece targetPiece = GetPiece(target);
                if (!targetPiece.IsEmpty && targetPiece.Team != pawn.Team && targetPiece.Type != PieceType.King)
                {
                    AddPawnMove(moves, from, target, promotionRank);
                }
                else if (targetPiece.IsEmpty &&
                         EnPassantTarget.HasValue &&
                         EnPassantTarget.Value == target)
                {
                    Piece adjacentPawn = GetPiece(new Vector2Int(target.x, from.y));
                    if (!adjacentPawn.IsEmpty &&
                        adjacentPawn.Team != pawn.Team &&
                        adjacentPawn.Type == PieceType.Pawn)
                    {
                        ChessMove enPassant = new ChessMove(from, target);
                        enPassant.IsEnPassant = true;
                        moves.Add(enPassant);
                    }
                }
            }
        }

        private static void AddPawnMove(List<ChessMove> moves, Vector2Int from, Vector2Int to, int promotionRank)
        {
            if (to.y == promotionRank)
            {
                moves.Add(new ChessMove(from, to, PieceType.Queen));
                moves.Add(new ChessMove(from, to, PieceType.Rook));
                moves.Add(new ChessMove(from, to, PieceType.Bishop));
                moves.Add(new ChessMove(from, to, PieceType.Knight));
            }
            else
            {
                moves.Add(new ChessMove(from, to));
            }
        }

        private void AddKnightMoves(List<ChessMove> moves, Vector2Int from, Piece knight)
        {
            for (int i = 0; i < KnightOffsets.Length; i++)
            {
                Vector2Int target = from + KnightOffsets[i];
                if (!InBounds(target))
                {
                    continue;
                }

                Piece targetPiece = GetPiece(target);
                if (targetPiece.IsEmpty || (targetPiece.Team != knight.Team && targetPiece.Type != PieceType.King))
                {
                    moves.Add(new ChessMove(from, target));
                }
            }
        }

        private void AddSlidingMoves(List<ChessMove> moves, Vector2Int from, Piece piece, Vector2Int[] directions)
        {
            for (int i = 0; i < directions.Length; i++)
            {
                Vector2Int target = from + directions[i];
                while (InBounds(target))
                {
                    Piece targetPiece = GetPiece(target);
                    if (targetPiece.IsEmpty)
                    {
                        moves.Add(new ChessMove(from, target));
                    }
                    else
                    {
                        if (targetPiece.Team != piece.Team && targetPiece.Type != PieceType.King)
                        {
                            moves.Add(new ChessMove(from, target));
                        }
                        break;
                    }

                    target += directions[i];
                }
            }
        }

        private void AddKingMoves(List<ChessMove> moves, Vector2Int from, Piece king)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0)
                    {
                        continue;
                    }

                    Vector2Int target = new Vector2Int(from.x + dx, from.y + dy);
                    if (!InBounds(target))
                    {
                        continue;
                    }

                    Piece targetPiece = GetPiece(target);
                    if (targetPiece.IsEmpty || (targetPiece.Team != king.Team && targetPiece.Type != PieceType.King))
                    {
                        moves.Add(new ChessMove(from, target));
                    }
                }
            }
        }

        private void AddCastlingMoves(List<ChessMove> moves, Vector2Int from, Piece king)
        {
            int homeRank = king.Team == Team.White ? 0 : 7;
            if (from != new Vector2Int(4, homeRank) || IsInCheck(king.Team))
            {
                return;
            }

            bool kingsideRight = king.Team == Team.White ? WhiteKingsideCastle : BlackKingsideCastle;
            bool queensideRight = king.Team == Team.White ? WhiteQueensideCastle : BlackQueensideCastle;
            Team opponent = Opponent(king.Team);
            Vector2Int kingsideRook = new Vector2Int(7, homeRank);
            Vector2Int queensideRook = new Vector2Int(0, homeRank);

            if (kingsideRight &&
                GetPiece(kingsideRook).Type == PieceType.Rook &&
                GetPiece(kingsideRook).Team == king.Team &&
                GetPiece(new Vector2Int(5, homeRank)).IsEmpty &&
                GetPiece(new Vector2Int(6, homeRank)).IsEmpty &&
                !IsSquareAttacked(new Vector2Int(5, homeRank), opponent) &&
                !IsSquareAttacked(new Vector2Int(6, homeRank), opponent))
            {
                ChessMove castle = new ChessMove(from, new Vector2Int(6, homeRank));
                castle.IsCastle = true;
                moves.Add(castle);
            }

            if (queensideRight &&
                GetPiece(queensideRook).Type == PieceType.Rook &&
                GetPiece(queensideRook).Team == king.Team &&
                GetPiece(new Vector2Int(1, homeRank)).IsEmpty &&
                GetPiece(new Vector2Int(2, homeRank)).IsEmpty &&
                GetPiece(new Vector2Int(3, homeRank)).IsEmpty &&
                !IsSquareAttacked(new Vector2Int(3, homeRank), opponent) &&
                !IsSquareAttacked(new Vector2Int(2, homeRank), opponent))
            {
                ChessMove castle = new ChessMove(from, new Vector2Int(2, homeRank));
                castle.IsCastle = true;
                moves.Add(castle);
            }
        }

        private MoveUndo ApplyMove(ChessMove move)
        {
            MoveUndo undo = new MoveUndo
            {
                Move = move,
                PreviousEnPassant = EnPassantTarget,
                PreviousWhiteKingside = WhiteKingsideCastle,
                PreviousWhiteQueenside = WhiteQueensideCastle,
                PreviousBlackKingside = BlackKingsideCastle,
                PreviousBlackQueenside = BlackQueensideCastle,
                PreviousHalfmoveClock = HalfmoveClock,
                PreviousFullmoveNumber = FullmoveNumber,
                PreviousSideToMove = SideToMove,
                PreviousResult = Result,
                PreviousReason = Reason
            };

            undo.MovingPiece = squares[move.From.x, move.From.y];
            undo.WasEnPassant = move.IsEnPassant;
            undo.CapturedSquare = move.IsEnPassant
                ? new Vector2Int(move.To.x, move.From.y)
                : move.To;
            undo.CapturedPiece = squares[undo.CapturedSquare.x, undo.CapturedSquare.y];

            squares[move.From.x, move.From.y] = Piece.Empty;

            if (move.IsEnPassant)
            {
                squares[undo.CapturedSquare.x, undo.CapturedSquare.y] = Piece.Empty;
            }

            if (move.IsCastle)
            {
                int homeRank = undo.MovingPiece.Team == Team.White ? 0 : 7;
                bool kingside = move.To.x > move.From.x;
                undo.WasCastle = true;
                undo.RookFrom = new Vector2Int(kingside ? 7 : 0, homeRank);
                undo.RookTo = new Vector2Int(kingside ? 5 : 3, homeRank);
                undo.RookPiece = squares[undo.RookFrom.x, undo.RookFrom.y];
                squares[undo.RookTo.x, undo.RookTo.y] = undo.RookPiece;
                squares[undo.RookFrom.x, undo.RookFrom.y] = Piece.Empty;
            }

            Piece placedPiece = undo.MovingPiece;
            if (undo.MovingPiece.Type == PieceType.Pawn && move.Promotion != PieceType.None)
            {
                placedPiece = new Piece(move.Promotion, undo.MovingPiece.Team);
            }

            squares[move.To.x, move.To.y] = placedPiece;

            UpdateCastlingRights(undo);

            EnPassantTarget = null;
            if (undo.MovingPiece.Type == PieceType.Pawn &&
                Mathf.Abs(move.To.y - move.From.y) == 2)
            {
                EnPassantTarget = new Vector2Int(move.From.x, (move.From.y + move.To.y) / 2);
            }

            bool pawnMove = undo.MovingPiece.Type == PieceType.Pawn;
            bool capture = !undo.CapturedPiece.IsEmpty;
            HalfmoveClock = pawnMove || capture ? 0 : HalfmoveClock + 1;
            if (SideToMove == Team.Black)
            {
                FullmoveNumber++;
            }

            SideToMove = Opponent(SideToMove);
            return undo;
        }

        private void UpdateCastlingRights(MoveUndo undo)
        {
            if (undo.MovingPiece.Type == PieceType.King)
            {
                if (undo.MovingPiece.Team == Team.White)
                {
                    WhiteKingsideCastle = false;
                    WhiteQueensideCastle = false;
                }
                else
                {
                    BlackKingsideCastle = false;
                    BlackQueensideCastle = false;
                }
            }

            if (undo.MovingPiece.Type == PieceType.Rook)
            {
                DisableRookCastleRight(undo.MovingPiece.Team, undo.Move.From);
            }

            if (!undo.CapturedPiece.IsEmpty && undo.CapturedPiece.Type == PieceType.Rook)
            {
                DisableRookCastleRight(undo.CapturedPiece.Team, undo.CapturedSquare);
            }
        }

        private void DisableRookCastleRight(Team team, Vector2Int square)
        {
            if (team == Team.White)
            {
                if (square == new Vector2Int(7, 0))
                {
                    WhiteKingsideCastle = false;
                }
                else if (square == new Vector2Int(0, 0))
                {
                    WhiteQueensideCastle = false;
                }
            }
            else if (team == Team.Black)
            {
                if (square == new Vector2Int(7, 7))
                {
                    BlackKingsideCastle = false;
                }
                else if (square == new Vector2Int(0, 7))
                {
                    BlackQueensideCastle = false;
                }
            }
        }

        private void UndoMove(MoveUndo undo)
        {
            squares[undo.Move.To.x, undo.Move.To.y] = Piece.Empty;

            if (undo.WasCastle)
            {
                squares[undo.RookTo.x, undo.RookTo.y] = Piece.Empty;
                squares[undo.RookFrom.x, undo.RookFrom.y] = undo.RookPiece;
            }

            squares[undo.Move.From.x, undo.Move.From.y] = undo.MovingPiece;
            squares[undo.CapturedSquare.x, undo.CapturedSquare.y] = undo.CapturedPiece;

            EnPassantTarget = undo.PreviousEnPassant;
            WhiteKingsideCastle = undo.PreviousWhiteKingside;
            WhiteQueensideCastle = undo.PreviousWhiteQueenside;
            BlackKingsideCastle = undo.PreviousBlackKingside;
            BlackQueensideCastle = undo.PreviousBlackQueenside;
            HalfmoveClock = undo.PreviousHalfmoveClock;
            FullmoveNumber = undo.PreviousFullmoveNumber;
            SideToMove = undo.PreviousSideToMove;
            Result = undo.PreviousResult;
            Reason = undo.PreviousReason;
        }

        private void EvaluatePosition()
        {
            List<ChessMove> legalMoves = GenerateLegalMoves(SideToMove);
            if (legalMoves.Count == 0)
            {
                if (IsInCheck(SideToMove))
                {
                    Result = SideToMove == Team.White ? GameResult.BlackWin : GameResult.WhiteWin;
                    Reason = EndReason.Checkmate;
                }
                else
                {
                    Result = GameResult.Draw;
                    Reason = EndReason.Stalemate;
                }

                return;
            }

            if (IsInsufficientMaterial())
            {
                Result = GameResult.Draw;
                Reason = EndReason.InsufficientMaterial;
                return;
            }

            if (HalfmoveClock >= 100)
            {
                Result = GameResult.Draw;
                Reason = EndReason.FiftyMove;
                return;
            }

            string currentKey = GetPositionKey();
            int repetitions = 0;
            for (int i = 0; i < positionKeys.Count; i++)
            {
                if (positionKeys[i] == currentKey)
                {
                    repetitions++;
                }
            }

            if (repetitions >= 3)
            {
                Result = GameResult.Draw;
                Reason = EndReason.ThreefoldRepetition;
                return;
            }

            Result = GameResult.None;
            Reason = EndReason.None;
        }

        private bool IsInsufficientMaterial()
        {
            int whiteMinors = 0;
            int blackMinors = 0;
            int whiteBishopColor = -1;
            int blackBishopColor = -1;

            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    Piece piece = squares[x, y];
                    if (piece.IsEmpty || piece.Type == PieceType.King)
                    {
                        continue;
                    }

                    if (piece.Type == PieceType.Pawn ||
                        piece.Type == PieceType.Rook ||
                        piece.Type == PieceType.Queen)
                    {
                        return false;
                    }

                    if (piece.Team == Team.White)
                    {
                        whiteMinors++;
                        if (piece.Type == PieceType.Bishop)
                        {
                            whiteBishopColor = (x + y) % 2;
                        }
                    }
                    else
                    {
                        blackMinors++;
                        if (piece.Type == PieceType.Bishop)
                        {
                            blackBishopColor = (x + y) % 2;
                        }
                    }
                }
            }

            if (whiteMinors == 0 && blackMinors == 0)
            {
                return true;
            }

            if (whiteMinors == 1 && blackMinors == 0)
            {
                return true;
            }

            if (whiteMinors == 0 && blackMinors == 1)
            {
                return true;
            }

            if (whiteMinors == 1 &&
                blackMinors == 1 &&
                whiteBishopColor >= 0 &&
                blackBishopColor >= 0 &&
                whiteBishopColor == blackBishopColor)
            {
                return true;
            }

            return false;
        }

        private Vector2Int? FindKing(Team team)
        {
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    Piece piece = squares[x, y];
                    if (!piece.IsEmpty && piece.Team == team && piece.Type == PieceType.King)
                    {
                        return new Vector2Int(x, y);
                    }
                }
            }

            return null;
        }

        private bool RayHits(Vector2Int from, Vector2Int target, Vector2Int[] directions)
        {
            for (int i = 0; i < directions.Length; i++)
            {
                Vector2Int step = from + directions[i];
                while (InBounds(step))
                {
                    if (step == target)
                    {
                        return true;
                    }

                    if (!GetPiece(step).IsEmpty)
                    {
                        break;
                    }

                    step += directions[i];
                }
            }

            return false;
        }

        private static bool IsKnightStep(Vector2Int from, Vector2Int to)
        {
            int dx = Mathf.Abs(from.x - to.x);
            int dy = Mathf.Abs(from.y - to.y);
            return (dx == 1 && dy == 2) || (dx == 2 && dy == 1);
        }

        private static Team Opponent(Team team)
        {
            if (team == Team.White)
            {
                return Team.Black;
            }

            if (team == Team.Black)
            {
                return Team.White;
            }

            return Team.None;
        }

        public string GetPositionKey()
        {
            StringBuilder builder = new StringBuilder(90);
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    builder.Append(squares[x, y].ToChar());
                }
            }

            builder.Append(SideToMove == Team.White ? " w " : " b ");
            builder.Append(WhiteKingsideCastle ? 'K' : '-');
            builder.Append(WhiteQueensideCastle ? 'Q' : '-');
            builder.Append(BlackKingsideCastle ? 'k' : '-');
            builder.Append(BlackQueensideCastle ? 'q' : '-');

            if (EnPassantTarget.HasValue)
            {
                builder.Append(' ');
                builder.Append(EnPassantTarget.Value.x);
                builder.Append(',');
                builder.Append(EnPassantTarget.Value.y);
            }
            else
            {
                builder.Append(" -");
            }

            return builder.ToString();
        }
    }
}
