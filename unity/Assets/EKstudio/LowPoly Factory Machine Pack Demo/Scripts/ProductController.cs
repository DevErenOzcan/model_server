using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

[System.Serializable]
public class ServerResponse
{
    public string status;
    public bool is_defected;
    public string defect_type;
    public float defect_percentage;
    public float threshold;
    public string message;
}

public class ProductController : MonoBehaviour
{
    [Header("Movement Settings")]
    public Vector3 startPosition = new Vector3(0f, 1.2f, -9f);
    public Vector3 endPosition = new Vector3(0f, 1.2f, 9f);
    public float speed = 6f;

    [Header("Camera Capture Settings")]
    public float triggerRadius = 1.3f;
    public float cooldown = 2f;
    private float lastCaptureTime = -Mathf.Infinity;
    private bool hasCaptured = false;

    private bool isDefective = false;
    private bool movingToSide = false;
    private Vector3 sideTargetPosition;
    private bool shouldRestart = false;

    private void Start()
    {
        ResetToStart();
    }

    private void Update()
    {
        if (shouldRestart)
        {
            ResetToStart();
            shouldRestart = false;
            return;
        }

        if (!movingToSide)
        {
            Vector3 targetPos = new Vector3(startPosition.x, startPosition.y, endPosition.z);
            transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

            float distanceToOrigin = Vector3.Distance(new Vector3(0, transform.position.y, transform.position.z), Vector3.zero);
            if (distanceToOrigin <= triggerRadius && !hasCaptured && Time.time - lastCaptureTime > cooldown)
            {
                StartCoroutine(CaptureAndSendTexture());
                lastCaptureTime = Time.time;
                hasCaptured = true;
            }

            if (Mathf.Abs(transform.position.z - endPosition.z) < 0.1f && hasCaptured)
            {
                movingToSide = true;
                sideTargetPosition = new Vector3(isDefective ? -7.08f : 7.08f, transform.position.y, endPosition.z);
            }
        }
        else
        {
            Vector3 targetPos = new Vector3(sideTargetPosition.x, transform.position.y, endPosition.z);
            transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

            if (Mathf.Abs(transform.position.x - sideTargetPosition.x) < 0.1f)
            {
                movingToSide = false;
                shouldRestart = true;
            }
        }
    }

    void ResetToStart()
    {
        transform.position = startPosition;
        ApplyRandomRotation();
        ApplyRandomTexture();
        hasCaptured = false;
        isDefective = false;
        movingToSide = false;
    }

    void ApplyRandomRotation()
    {
        float randomYRot = Random.Range(-5f, 5f);
        transform.rotation = Quaternion.Euler(0f, randomYRot, 0f);
    }

    void ApplyRandomTexture()
    {
        Texture2D[] textures = Resources.LoadAll<Texture2D>("Textures");

        if (textures.Length == 0)
        {
            Debug.LogWarning("Hiçbir texture bulunamadı. Lütfen 'Resources/Textures' klasörünü kontrol edin.");
            return;
        }

        Texture2D randomTexture = textures[Random.Range(0, textures.Length)];

        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null && renderer.material != null)
        {
            renderer.material.mainTexture = randomTexture;
            Debug.Log("Rastgele texture uygulandı: " + randomTexture.name);
        }
        else
        {
            Debug.LogWarning("Renderer ya da materyal bulunamadı.");
        }
    }

    private IEnumerator CaptureAndSendTexture()
    {
        yield return new WaitForEndOfFrame();

        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null || renderer.material == null || renderer.material.mainTexture == null)
        {
            Debug.LogWarning("Renderer veya materyal veya texture bulunamadı.");
            yield break;
        }

        Texture2D texture = renderer.material.mainTexture as Texture2D;

        if (texture == null)
        {
            Debug.LogWarning("Ana texture bir Texture2D değil.");
            yield break;
        }

        // Texture'ı okunabilir hale getir (RenderTexture kullanarak)
        Texture2D readableTexture = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture tempRT = RenderTexture.GetTemporary(texture.width, texture.height, 0);

        Graphics.Blit(texture, tempRT);
        RenderTexture.active = tempRT;
        readableTexture.ReadPixels(new Rect(0, 0, tempRT.width, tempRT.height), 0, 0);
        readableTexture.Apply();

        RenderTexture.active = currentRT;
        RenderTexture.ReleaseTemporary(tempRT);

        byte[] imageData = readableTexture.EncodeToPNG();
        Destroy(readableTexture);

        yield return StartCoroutine(SendImageToServer(imageData));
    }

    private IEnumerator SendImageToServer(byte[] imageData)
    {
        string url = "http://127.0.0.1:5000/upload_from_unity";

        WWWForm form = new WWWForm();
        form.AddBinaryData("image", imageData, "texture.png", "image/png");

        UnityWebRequest www = UnityWebRequest.Post(url, form);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Texture başarıyla gönderildi.");
            string jsonResponse = www.downloadHandler.text;
            Debug.Log("Sunucu cevabı: " + jsonResponse);

            try
            {
                ServerResponse response = JsonUtility.FromJson<ServerResponse>(jsonResponse);
                if (response != null)
                {
                    isDefective = response.is_defected;
                    Debug.Log($"Durum: {(isDefective ? "Hatalı" : "Sağlam")}, Hata Türü: {response.defect_type}, Oran: %{(response.defect_percentage * 100f):F2}, Eşik: {response.threshold}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Sunucu yanıtı ayrıştırılamadı: " + e.Message);
                isDefective = false;
            }
        }
        else
        {
            Debug.LogError("Sunucuya gönderim hatası: " + www.error);
            isDefective = false;
        }
    }
}

