using MonkChess;
using UnityEngine;

public class player : MonoBehaviour
{
    public GameObject controller;
    public GameObject moveplate;

    public Sprite b_q, b_r, b_k, b_p, b_kt, b_b;
    public Sprite w_q, w_r, w_k, w_p, w_kt, w_b;

    private int xboard = -1;
    private int yboard = -1;
    private PieceType pieceType = PieceType.None;
    private Team team = Team.None;
    private bool selected;
    private bool hovered;
    private readonly Color normalColor = Color.white;
    private readonly Color hoverColor = new Color(1f, 0.96f, 0.88f, 1f);
    private readonly Color selectedColor = new Color(1f, 0.82f, 0.48f, 1f);

    public Team Team
    {
        get { return team; }
    }

    public PieceType PieceType
    {
        get { return pieceType; }
    }

    public void Initialize(PieceType newType, Team newTeam)
    {
        pieceType = newType;
        team = newTeam;
        gameObject.name = BuildName(newType, newTeam);
        ApplySprite();
        ApplyColor();
    }

    public void SetController(Game game)
    {
        controller = game.gameObject;
    }

    public void SetBoardPosition(int x, int y)
    {
        xboard = x;
        yboard = y;
    }

    public void SetCoords()
    {
        transform.position = BoardToWorld(xboard, yboard);
    }

    public static Vector3 BoardToWorld(int x, int y)
    {
        float worldX = x * 0.66f - 2.3f;
        float worldY = y * 0.66f - 2.35f;
        return new Vector3(worldX, worldY, 0f);
    }

    public int Getxboard()
    {
        return xboard;
    }

    public int Getyboard()
    {
        return yboard;
    }

    public void Setxboard(int x)
    {
        xboard = x;
    }

    public void Setyboard(int y)
    {
        yboard = y;
    }

    public void SetSelected(bool value)
    {
        selected = value;
        ApplyColor();
    }

    public void Promote(PieceType newType)
    {
        pieceType = newType;
        gameObject.name = BuildName(newType, team);
        ApplySprite();
        ApplyColor();
    }

    public Sprite GetSprite(PieceType type, Team pieceTeam)
    {
        if (pieceTeam == Team.White)
        {
            switch (type)
            {
                case PieceType.Pawn: return w_p;
                case PieceType.Knight: return w_kt;
                case PieceType.Bishop: return w_b;
                case PieceType.Rook: return w_r;
                case PieceType.Queen: return w_q;
                case PieceType.King: return w_k;
            }
        }
        else if (pieceTeam == Team.Black)
        {
            switch (type)
            {
                case PieceType.Pawn: return b_p;
                case PieceType.Knight: return b_kt;
                case PieceType.Bishop: return b_b;
                case PieceType.Rook: return b_r;
                case PieceType.Queen: return b_q;
                case PieceType.King: return b_k;
            }
        }

        return null;
    }

    private void ApplySprite()
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sprite = GetSprite(pieceType, team);
        }
    }

    private void ApplyColor()
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            return;
        }

        if (selected)
        {
            renderer.color = selectedColor;
        }
        else if (hovered)
        {
            renderer.color = hoverColor;
        }
        else
        {
            renderer.color = normalColor;
        }
    }

    private void OnMouseEnter()
    {
        hovered = true;
        ApplyColor();
    }

    private void OnMouseExit()
    {
        hovered = false;
        ApplyColor();
    }

    public static string BuildName(PieceType type, Team pieceTeam)
    {
        string prefix = pieceTeam == Team.White ? "w" : "b";
        string suffix;
        switch (type)
        {
            case PieceType.Pawn: suffix = "p"; break;
            case PieceType.Knight: suffix = "kt"; break;
            case PieceType.Bishop: suffix = "b"; break;
            case PieceType.Rook: suffix = "r"; break;
            case PieceType.Queen: suffix = "q"; break;
            case PieceType.King: suffix = "k"; break;
            default: suffix = "?"; break;
        }

        return prefix + "_" + suffix;
    }
}
