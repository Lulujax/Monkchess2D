using System;
using UnityEngine;

namespace MonkChess
{
    public enum PieceType
    {
        None,
        Pawn,
        Knight,
        Bishop,
        Rook,
        Queen,
        King
    }

    public enum Team
    {
        None,
        White,
        Black
    }

    public enum GameResult
    {
        None,
        WhiteWin,
        BlackWin,
        Draw
    }

    public enum EndReason
    {
        None,
        Checkmate,
        Stalemate,
        InsufficientMaterial,
        FiftyMove,
        ThreefoldRepetition
    }

    [Serializable]
    public struct Piece
    {
        public PieceType Type;
        public Team Team;

        public bool IsEmpty
        {
            get { return Type == PieceType.None; }
        }

        public Piece(PieceType type, Team team)
        {
            Type = type;
            Team = team;
        }

        public static Piece Empty
        {
            get { return new Piece(PieceType.None, Team.None); }
        }

        public char ToChar()
        {
            if (IsEmpty)
            {
                return '.';
            }

            char c;
            switch (Type)
            {
                case PieceType.Pawn: c = 'P'; break;
                case PieceType.Knight: c = 'N'; break;
                case PieceType.Bishop: c = 'B'; break;
                case PieceType.Rook: c = 'R'; break;
                case PieceType.Queen: c = 'Q'; break;
                case PieceType.King: c = 'K'; break;
                default: c = '?'; break;
            }

            return Team == Team.White ? c : char.ToLowerInvariant(c);
        }
    }

    [Serializable]
    public struct ChessMove
    {
        public Vector2Int From;
        public Vector2Int To;
        public PieceType Promotion;
        public bool IsEnPassant;
        public bool IsCastle;

        public ChessMove(Vector2Int from, Vector2Int to, PieceType promotion = PieceType.None)
        {
            From = from;
            To = to;
            Promotion = promotion;
            IsEnPassant = false;
            IsCastle = false;
        }

        public bool SameAs(ChessMove other)
        {
            return From == other.From && To == other.To && Promotion == other.Promotion;
        }

        public override string ToString()
        {
            return From + " -> " + To + (Promotion == PieceType.None ? string.Empty : " (" + Promotion + ")");
        }
    }
}
