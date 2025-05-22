using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class CameraCapture : MonoBehaviour
{
    public Transform target;
    public float triggerRadius = 1.3f;
    public float cooldown = 2f;
    private float lastCaptureTime = -Mathf.Infinity;
    private bool hasCaptured = false;

    void Update()
    {
        if (target == null) return;

        float distanceToOrigin = Vector3.Distance(target.position, Vector3.zero);

        if (distanceToOrigin <= triggerRadius && !hasCaptured && Time.time - lastCaptureTime > cooldown)
        {
            StartCoroutine(CaptureAndSendScreenshot());
            lastCaptureTime = Time.time;
            hasCaptured = true;
        }

        if (distanceToOrigin > triggerRadius)
        {
            hasCaptured = false;
        }
    }

    private IEnumerator CaptureAndSendScreenshot()
    {
        yield return new WaitForEndOfFrame();

        // Ekranı belleğe al
        Texture2D screenImage = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        screenImage.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        screenImage.Apply();

        // PNG formatına dönüştür
        byte[] imageData = screenImage.EncodeToPNG();

        // Sunucuya gönder
        StartCoroutine(SendImageToServer(imageData));

        // Belleği temizle
        Destroy(screenImage);
    }

    private IEnumerator SendImageToServer(byte[] imageData)
    {
        // Flask sunucu adresi (kendine göre düzenle)
        string url = "http://127.0.0.1:5000/upload_from_unity";

        WWWForm form = new WWWForm();
        form.AddBinaryData("image", imageData, "screenshot.png", "image/png");

        UnityWebRequest www = UnityWebRequest.Post(url, form);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Görüntü başarıyla gönderildi.");
        }
        else
        {
            Debug.LogError("Gönderme hatası: " + www.error);
        }
    }
}
