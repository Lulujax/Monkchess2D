using System;
using UnityEngine;
using UnityEngine.UI;

public class InstructionsView : MonoBehaviour
{
    public static bool IsAnyOpen { get; private set; }

    private static readonly string[] Pages =
    {
        "<b><color=#F3C77A>OBJETIVO</color></b>\n" +
        "Da jaque mate al rey rival: amenázalo con una captura inevitable y sin escapatoria.\n\n" +
        "<b><color=#F3C77A>CÓMO MOVER</color></b>\n" +
        "1. Pulsa una de tus piezas. Solo se iluminan las casillas legales.\n" +
        "2. Las casillas doradas indican un movimiento; las rojizas, una captura.\n" +
        "3. Pulsa una casilla iluminada para mover la pieza.\n\n" +
        "<b><color=#F3C77A>TURNOS</color></b>\n" +
        "Las blancas mueven primero. El indicador superior muestra de quién es el turno. " +
        "Si tu rey está en jaque, el juego te avisa y solo permite movimientos que lo protejan.",

        "<b><color=#F3C77A>MOVIMIENTOS ESPECIALES</color></b>\n" +
        "Enroque: mueve el rey dos casillas hacia una torre si ambos están libres y el rey no cruza una casilla atacada.\n" +
        "Al paso: captura un peón rival que acaba de avanzar dos casillas.\n" +
        "Promoción: al llegar a la última fila, elige reina, torre, alfil o caballo.\n\n" +
        "<b><color=#F3C77A>TABLAS</color></b>\n" +
        "La partida termina en tablas por ahogado, material insuficiente, la regla de 50 movimientos o triple repetición.\n\n" +
        "<b><color=#F3C77A>CONTROLES</color></b>\n" +
        "MENÚ: pausa, deshacer, reiniciar, instrucciones y salir.\n" +
        "SONIDO: activa o silencia los efectos.\n" +
        "ESC: abre o cierra la pausa. CTRL+Z: deshace el último movimiento."
    };

    private Action closeAction;
    private Text pageText;
    private Text pageCounter;
    private Button previousButton;
    private Button nextButton;
    private int pageIndex;
    private bool closed;

    public static InstructionsView Show(Transform parent, Action onClose)
    {
        GameObject root = new GameObject("Instructions", typeof(RectTransform));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        UITheme.Stretch(rect, 0f, 0f, 0f, 0f);

        InstructionsView view = root.AddComponent<InstructionsView>();
        view.Build(onClose);
        return view;
    }

    private void Build(Action onClose)
    {
        closeAction = onClose;
        IsAnyOpen = true;

        UITheme.CreateFullScreenOverlay(transform, "Dim", UITheme.Dim, true);

        Image panel = UITheme.CreatePanel(transform, "Panel", UITheme.DeepBrown, true);
        UITheme.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980f, 660f));

        Image accent = UITheme.CreateImage(panel.transform, "Accent", UITheme.Gold, false);
        UITheme.Place(accent.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(120f, 3f));

        Text title = UITheme.CreateText(panel.transform, "Title", "CÓMO JUGAR", 40, UITheme.Cream, TextAnchor.MiddleCenter);
        UITheme.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -48f), new Vector2(700f, 52f));
        title.fontStyle = FontStyle.Bold;
        UITheme.AddShadow(title.gameObject, new Color(0f, 0f, 0f, 0.45f), new Vector2(2f, -2f));

        Text subtitle = UITheme.CreateText(panel.transform, "Subtitle", "AJEDREZ CLÁSICO · DOS JUGADORES", 14, UITheme.MutedCream, TextAnchor.MiddleCenter);
        UITheme.Place(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(600f, 24f));

        pageText = UITheme.CreateText(panel.transform, "PageText", string.Empty, 18, UITheme.Cream, TextAnchor.UpperLeft);
        RectTransform textRect = pageText.rectTransform;
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(48f, 92f);
        textRect.offsetMax = new Vector2(-48f, -128f);
        pageText.lineSpacing = 1.08f;

        pageCounter = UITheme.CreateText(panel.transform, "PageCounter", "1 / 2", 15, UITheme.MutedCream, TextAnchor.MiddleCenter);
        UITheme.Place(pageCounter.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 32f), new Vector2(100f, 24f));

        previousButton = UITheme.CreateButton(panel.transform, "Previous", "ANTERIOR", new Vector2(170f, 46f), UITheme.PanelBrown, UITheme.Cream, 16, PreviousPage);
        UITheme.Place(previousButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-300f, 20f), new Vector2(170f, 46f));

        Button closeButton = UITheme.CreateButton(panel.transform, "Close", "VOLVER", new Vector2(140f, 46f), UITheme.Gold, UITheme.Ink, 16, Close);
        UITheme.Place(closeButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(140f, 46f));

        nextButton = UITheme.CreateButton(panel.transform, "Next", "SIGUIENTE", new Vector2(170f, 46f), UITheme.PanelBrown, UITheme.Cream, 16, NextPage);
        UITheme.Place(nextButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(300f, 20f), new Vector2(170f, 46f));

        UpdatePage();
    }

    private void Update()
    {
        if (!closed && Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
    }

    private void PreviousPage()
    {
        SoundManager.Ensure().PlayClick();
        pageIndex = Mathf.Max(0, pageIndex - 1);
        UpdatePage();
    }

    private void NextPage()
    {
        SoundManager.Ensure().PlayClick();
        pageIndex = Mathf.Min(Pages.Length - 1, pageIndex + 1);
        UpdatePage();
    }

    private void UpdatePage()
    {
        pageText.text = Pages[pageIndex];
        pageCounter.text = (pageIndex + 1) + " / " + Pages.Length;
        previousButton.interactable = pageIndex > 0;
        nextButton.interactable = pageIndex < Pages.Length - 1;
    }

    private void Close()
    {
        if (closed)
        {
            return;
        }

        closed = true;
        IsAnyOpen = false;
        SoundManager.Ensure().PlayClick();

        if (closeAction != null)
        {
            closeAction();
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        IsAnyOpen = false;
    }
}
