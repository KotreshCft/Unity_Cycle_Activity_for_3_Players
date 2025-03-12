using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ImageAnimator : MonoBehaviour
{
    public Image targetImage; // The image component you want to animate
    public float animationDuration = 2.0f; // Duration of each animation in seconds

    private CanvasGroup canvasGroup;

    private void Start()
    {
        canvasGroup = targetImage.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = targetImage.gameObject.AddComponent<CanvasGroup>();
        }

        StartCoroutine(AnimationLoop());
    }

    private IEnumerator AnimationLoop()
    {
        while (targetImage.enabled)
        {
            yield return FadeIn();
            //yield return SlideIn();
            yield return ZoomIn();
            yield return Wait(1.0f); // Optional wait
            //yield return SlideOut();
            yield return ZoomOut();
            yield return FadeOut();
            yield return Wait(1.0f); // Optional wait
        }
    }

    private IEnumerator FadeIn()
    {
        float elapsedTime = 0f;
        while (elapsedTime < animationDuration)
        {
            float alpha = Mathf.Clamp01(elapsedTime / animationDuration);
            canvasGroup.alpha = alpha;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOut()
    {
        float elapsedTime = 0f;
        while (elapsedTime < animationDuration)
        {
            float alpha = Mathf.Clamp01(1 - (elapsedTime / animationDuration));
            canvasGroup.alpha = alpha;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = 0f;
    }

    private IEnumerator SlideIn()
    {
        Vector3 startPosition = targetImage.rectTransform.anchoredPosition;
        Vector3 endPosition = Vector3.zero; // Adjust this based on where you want to slide in from
        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            float t = Mathf.Clamp01(elapsedTime / animationDuration);
            targetImage.rectTransform.anchoredPosition = Vector3.Lerp(startPosition, endPosition, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        targetImage.rectTransform.anchoredPosition = endPosition;
    }

    private IEnumerator SlideOut()
    {
        Vector3 startPosition = targetImage.rectTransform.anchoredPosition;
        Vector3 endPosition = new Vector3(0, -500, 0); // Adjust this based on where you want to slide out to
        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            float t = Mathf.Clamp01(elapsedTime / animationDuration);
            targetImage.rectTransform.anchoredPosition = Vector3.Lerp(startPosition, endPosition, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        targetImage.rectTransform.anchoredPosition = endPosition;
    }

    private IEnumerator ZoomIn()
    {
        Vector3 startScale = targetImage.rectTransform.localScale;
        Vector3 endScale = new Vector3(1.2f, 1.2f, 1.2f); // Adjust the scale factor as needed
        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            float t = Mathf.Clamp01(elapsedTime / animationDuration);
            targetImage.rectTransform.localScale = Vector3.Lerp(startScale, endScale, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        targetImage.rectTransform.localScale = endScale;
    }

    private IEnumerator ZoomOut()
    {
        Vector3 startScale = targetImage.rectTransform.localScale;
        Vector3 endScale = new Vector3(1f, 1f, 1f); // Return to original scale
        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            float t = Mathf.Clamp01(elapsedTime / animationDuration);
            targetImage.rectTransform.localScale = Vector3.Lerp(startScale, endScale, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        targetImage.rectTransform.localScale = endScale;
    }

    private IEnumerator Wait(float duration)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }
}
