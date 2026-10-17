using System.Collections;
using System.IO;
using UnityEngine;

// Copy to Assets/Validation in the disposable consumer, alongside the Editor validator.
public sealed class SampleVisualCapture : MonoBehaviour
{
    public bool Complete { get; private set; }
    public float WhiteRatio { get; private set; }
    public float BlackRatio { get; private set; }
    public float ColorRatio { get; private set; }
    public float PinkRatio { get; private set; }

    public void Capture(string path) => StartCoroutine(CaptureFrame(path));

    private IEnumerator CaptureFrame(string path)
    {
        yield return new WaitForEndOfFrame();
        var image = ScreenCapture.CaptureScreenshotAsTexture();
        var pixels = image.GetPixels();
        int white = 0, black = 0, colorful = 0, pink = 0;
        foreach (var color in pixels)
        {
            float min = Mathf.Min(color.r, color.g, color.b);
            float max = Mathf.Max(color.r, color.g, color.b);
            if (min > .95f) ++white;
            if (max < .03f) ++black;
            if (max - min > .05f) ++colorful;
            if (color.r > .5f && color.b > .5f && color.r > color.g * 2 && color.b > color.g * 2) ++pink;
        }
        WhiteRatio = (float)white / pixels.Length;
        BlackRatio = (float)black / pixels.Length;
        ColorRatio = (float)colorful / pixels.Length;
        PinkRatio = (float)pink / pixels.Length;
        File.WriteAllBytes(path, image.EncodeToPNG());
        Destroy(image);
        Complete = true;
    }
}
