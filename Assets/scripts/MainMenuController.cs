using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    private Transform uiRoot;

    private void Start()
    {
        HideLegacyMenu();
        SoundManager.Ensure();
        UITheme.EnsureEventSystem();
        BuildMenu();
    }

    private static void HideLegacyMenu()
    {
        HideObject("maintext");
        HideObject("w");
        HideObject("maintext (1)");

        RectTransform leftKing = FindRect("RawImage");
        if (leftKing != null)
        {
            leftKing.anchoredPosition = new Vector2(-365f, 30f);
            leftKing.localScale = new Vector3(1.3f, 1.3f, 1f);
        }

        RectTransform rightKing = FindRect("RawImage (1)");
        if (rightKing != null)
        {
            rightKing.anchoredPosition = new Vector2(365f, 30f);
            rightKing.localScale = new Vector3(1.3f, 1.3f, 1f);
        }
    }

    private static void HideObject(string name)
    {
        GameObject target = GameObject.Find(name);
        if (target != null)
        {
            target.SetActive(false);
        }
    }

    private static RectTransform FindRect(string name)
    {
        GameObject target = GameObject.Find(name);
        return target != null ? target.GetComponent<RectTransform>() : null;
    }

    private void BuildMenu()
    {
        GameObject canvasObject = UITheme.CreateCanvas("MonkchessMenuUI", 30);
        uiRoot = canvasObject.transform;

        UITheme.CreateFullScreenOverlay(uiRoot, "Shade", new Color(0.06f, 0.03f, 0.02f, 0.28f), false);

        Image frame = UITheme.CreatePanel(uiRoot, "Frame", new Color(0.85f, 0.62f, 0.32f, 0.32f), false);
        UITheme.Place(frame.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(672f, 712f));

        Image card = UITheme.CreatePanel(uiRoot, "Card", new Color(0.19f, 0.11f, 0.07f, 0.91f), false);
        UITheme.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640f, 680f));

        Image accent = UITheme.CreateImage(card.transform, "Accent", UITheme.Gold, false);
        UITheme.Place(accent.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(130f, 3f));

        Text kicker = UITheme.CreateText(card.transform, "Kicker", "AJEDREZ CLÁSICO · DOS JUGADORES", 14, UITheme.Gold, TextAnchor.MiddleCenter);
        UITheme.Place(kicker.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(540f, 24f));

        Text title = UITheme.CreateText(card.transform, "Title", "MONKCHESS", 72, UITheme.Cream, TextAnchor.MiddleCenter);
        UITheme.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(580f, 88f));
        title.fontStyle = FontStyle.Bold;
        UITheme.AddShadow(title.gameObject, new Color(0f, 0f, 0f, 0.55f), new Vector2(3f, -3f));

        Text subtitle = UITheme.CreateText(card.transform, "Subtitle", "Un duelo de ingenio, turno a turno.", 18, UITheme.MutedCream, TextAnchor.MiddleCenter);
        UITheme.Place(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -180f), new Vector2(520f, 28f));

        Image divider = UITheme.CreateImage(card.transform, "Divider", new Color(0.85f, 0.62f, 0.32f, 0.4f), false);
        UITheme.Place(divider.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -218f), new Vector2(220f, 1f));

        Button play = UITheme.CreateButton(card.transform, "Play", "JUGAR", new Vector2(380f, 64f), UITheme.Gold, UITheme.Ink, 22, OnPlay);
        UITheme.Place(play.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -258f), new Vector2(380f, 64f));

        Button instructions = UITheme.CreateButton(card.transform, "Instructions", "INSTRUCCIONES", new Vector2(380f, 56f), UITheme.PanelBrown, UITheme.Cream, 18, OnInstructions);
        UITheme.Place(instructions.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -340f), new Vector2(380f, 56f));

        Button quit = UITheme.CreateButton(card.transform, "Quit", "SALIR", new Vector2(250f, 44f), new Color(0.3f, 0.16f, 0.1f, 0.8f), UITheme.MutedCream, 16, OnQuit);
        UITheme.Place(quit.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -414f), new Vector2(250f, 44f));

        Text footer = UITheme.CreateText(card.transform, "Footer", "LAS BLANCAS MUEVEN PRIMERO", 12, new Color(0.85f, 0.73f, 0.56f, 0.9f), TextAnchor.MiddleCenter);
        UITheme.Place(footer.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(500f, 22f));

        CanvasGroup group = card.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        StartCoroutine(RevealCard(group, card.rectTransform));
    }

    private IEnumerator RevealCard(CanvasGroup group, RectTransform card)
    {
        Vector3 targetScale = card.localScale;
        card.localScale = targetScale * 0.95f;
        float elapsed = 0f;

        while (elapsed < 0.38f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / 0.38f);
            float eased = 1f - (1f - t) * (1f - t);
            group.alpha = eased;
            card.localScale = Vector3.Lerp(targetScale * 0.95f, targetScale, eased);
            yield return null;
        }

        group.alpha = 1f;
        card.localScale = targetScale;
    }

    private void OnPlay()
    {
        SoundManager.Ensure().PlayClick();
        SceneManager.LoadScene("Game");
    }

    private void OnInstructions()
    {
        SoundManager.Ensure().PlayClick();
        InstructionsView.Show(uiRoot, null);
    }

    private void OnQuit()
    {
        SoundManager.Ensure().PlayClick();
        Application.Quit();
    }
}
