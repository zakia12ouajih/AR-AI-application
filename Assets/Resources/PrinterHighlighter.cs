using UnityEngine;
using System.Collections;

public class PrinterHighlighter : MonoBehaviour
{
    [Header("Highlight Objects")]
    public GameObject powerButtonHighlight;
    public GameObject cancelButtonHighlight;
    public GameObject restartButtonHighlight;

    [Header("Colors")]
    public Color highlightColor = Color.green;
    public Color transparentColor = new Color(1f, 1f, 1f, 0.05f); // Fully transparent

    private Renderer powerRenderer;
    private Renderer cancelRenderer;
    private Renderer restartRenderer;

    [Header("Pulse Animation")]
    public float pulseScale = 2.0f;
    public float pulseSpeed = 2f;

    private Vector3 powerOriginalScale;
    private Vector3 cancelOriginalScale;
    private Vector3 restartOriginalScale;

    private Coroutine powerPulse;
    private Coroutine cancelPulse;
    private Coroutine restartPulse;

    void Start()
    {
        SetupRenderer(ref powerButtonHighlight, out powerRenderer);
        SetupRenderer(ref cancelButtonHighlight, out cancelRenderer);
        SetupRenderer(ref restartButtonHighlight, out restartRenderer);

        // 🔹 STORE ORIGINAL SCALES
        if (powerButtonHighlight != null)
            powerOriginalScale = powerButtonHighlight.transform.localScale;

        if (cancelButtonHighlight != null)
            cancelOriginalScale = cancelButtonHighlight.transform.localScale;

        if (restartButtonHighlight != null)
            restartOriginalScale = restartButtonHighlight.transform.localScale;

        SetAllTransparent();
    }
    IEnumerator Pulse(GameObject obj, Vector3 originalScale)
    {
        while (true)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * pulseSpeed;
                obj.transform.localScale =
                    Vector3.Lerp(originalScale, originalScale * pulseScale, t);
                yield return null;
            }

            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * pulseSpeed;
                obj.transform.localScale =
                    Vector3.Lerp(originalScale * pulseScale, originalScale, t);
                yield return null;
            }
        }
    }
    void StopAllPulses()
    {
        if (powerPulse != null)
        {
            StopCoroutine(powerPulse);
            powerPulse = null;
            powerButtonHighlight.transform.localScale = powerOriginalScale;
        }

        if (cancelPulse != null)
        {
            StopCoroutine(cancelPulse);
            cancelPulse = null;
            cancelButtonHighlight.transform.localScale = cancelOriginalScale;
        }

        if (restartPulse != null)
        {
            StopCoroutine(restartPulse);
            restartPulse = null;
            restartButtonHighlight.transform.localScale = restartOriginalScale;
        }
    }



    void SetupRenderer(ref GameObject obj, out Renderer rend)
    {
        rend = null;
        if (obj != null)
        {
            rend = obj.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material = new Material(rend.material); // unique instance
                rend.material.color = transparentColor;
            }
        }
    }

    // Highlight a specific button
    public void HighlightOnlyThisPart(string partName)
    {
        SetAllTransparent();

        switch (partName.ToLower())
        {
            case "powerbutton":
                powerRenderer.material.color = highlightColor;
                powerPulse = StartCoroutine(Pulse(powerButtonHighlight, powerOriginalScale));
                break;

            case "cancelbutton":
                cancelRenderer.material.color = highlightColor;
                cancelPulse = StartCoroutine(Pulse(cancelButtonHighlight, cancelOriginalScale));
                break;

            case "restartbutton":
                restartRenderer.material.color = highlightColor;
                restartPulse = StartCoroutine(Pulse(restartButtonHighlight, restartOriginalScale));
                break;

            default:
                Debug.LogWarning("Unknown part to highlight: " + partName);
                break;
        }
    }


    // Make all transparent
    public void SetAllTransparent()
    {
        if (powerRenderer != null) powerRenderer.material.color = transparentColor;
        if (cancelRenderer != null) cancelRenderer.material.color = transparentColor;
        if (restartRenderer != null) restartRenderer.material.color = transparentColor;

        StopAllPulses();
    }
}
