using MonkChess;
using UnityEngine;

public class moveplate : MonoBehaviour
{
    public GameObject controller;
    public bool attack;

    private Game game;
    private SpriteRenderer spriteRenderer;
    private Color baseColor;
    private bool hovered;

    public ChessMove Move { get; private set; }

    public void Setup(Game owner, ChessMove move, bool isAttack)
    {
        game = owner;
        Move = move;
        attack = isAttack;
        controller = owner != null ? owner.gameObject : null;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = -4;
            baseColor = attack
                ? new Color(0.76f, 0.25f, 0.18f, 0.66f)
                : new Color(1f, 0.86f, 0.56f, 0.38f);
            spriteRenderer.color = baseColor;
        }

        gameObject.name = "MovePlate";
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

    private void ApplyColor()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        if (hovered)
        {
            Color highlight = attack
                ? new Color(0.92f, 0.35f, 0.26f, 0.82f)
                : new Color(1f, 0.94f, 0.72f, 0.6f);
            spriteRenderer.color = highlight;
        }
        else
        {
            spriteRenderer.color = baseColor;
        }
    }
}
