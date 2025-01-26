using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class RequestManager : MonoBehaviour
{
    public string serverUrl;
    public string authorizationToken;
    
    [Header("spaces endpoint")]
    public string spaceName = "mit.reality.hack";
    public string region = "nyc3";
    public string accessKey = "access_key";
    public string secretKey = "secret_key";
    
    public UnityEvent<string> onNewImageStatus = new();
    public UnityEvent<string> onImageUploadComplete = new();
    
    [Header("Debug")]
    [SerializeField]
    private Texture2D _textureCache;
    
    // context button in menu "send test upload"
    [ContextMenu("Send Test Upload")]
    public void SendTestUpload()
    {
        if (!Application.isPlaying) return;
        
        var testTexture = new Texture2D(256, 256);
        var colors = new Color[256 * 256];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.red;
        }
        
        testTexture.SetPixels(colors);
        testTexture.Apply();
        
        UploadImageToTheCloud(testTexture);
    }
    
    public event Action<string> OnLocalSavingComplete;
    
    public void DownloadGlb(string downloadURL)
    {
        StartCoroutine(DownloadGLB(downloadURL));
    }

    private UnityWebRequest RequestBuilder(string requestUrl)
    {
        UnityWebRequest request = UnityWebRequest.Get(requestUrl);
        request.SetRequestHeader("Authorization", authorizationToken);
        
        return request;
    }

    private IEnumerator DownloadGLB(string downloadURL)
    {
        var request = RequestBuilder(downloadURL);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            byte[] glbData = request.downloadHandler.data;
            var sizeInMB = (glbData.Length / 1024f / 1024f);
            sizeInMB = (float) Math.Round(sizeInMB, 2);
            Debug.Log($"Downloaded GLB file size: {sizeInMB} MB");
            var randomId = Guid.NewGuid().ToString();
            randomId = randomId.Substring(0, randomId.IndexOf("-", StringComparison.Ordinal));
            var fileName = $"gen-{randomId}.glb";
            string filePath = Application.persistentDataPath + $"/Resources/Downloaded/";
            
            // write the downloaded glb file to the temporary cache path
            // File.WriteAllBytes(filePath, glbData);
            SaveDataStream(filePath, glbData, fileName);
        }
        else
        {
            Debug.LogError(request.error);
        }
    }

    private async void SaveDataStream(string filePath, byte[] glbData, string fileName)
    {
        string combinedPath = Path.Combine(filePath, fileName);
        Debug.Log($"Saving file to: {combinedPath}");

        try
        {
            await using (FileStream fileStream = new FileStream(combinedPath, FileMode.Create, FileAccess.Write,
                             FileShare.None, 4096, true))
            {
                await fileStream.WriteAsync(glbData, 0, glbData.Length);
            }

            // saved file size
            var fileSize = new FileInfo(combinedPath).Length;
            var sizeInMB = (fileSize / 1024f / 1024f);
            sizeInMB = (float)Math.Round(sizeInMB, 2);

            Debug.Log($"File saved at: {combinedPath}, {sizeInMB} MB");
            OnLocalSavingComplete?.Invoke(combinedPath);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

    public void UploadImageToTheCloud(Texture2D image)
    {
        // var bytes = image.EncodeToPNG();
        _textureCache = image;
        
        StartCoroutine(StartUploadingTexture());
    }

    IEnumerator StartUploadingTexture()
    {
        byte[] textureData = _textureCache.EncodeToPNG();
        
        var randomId = Guid.NewGuid().ToString();
        randomId = randomId.Substring(0, randomId.IndexOf("-", StringComparison.Ordinal));
        var fileName = $"{randomId}.png";
        
        string date = DateTime.UtcNow.ToString("r");
        string aclHeader = "x-amz-acl:public-read";
        string storageHeader = "x-amz-storage-class:STANDARD";
        string contentType = "image/png";
        string toSign = $"PUT\n\n{contentType}\n{date}\n{aclHeader}\n{storageHeader}\n/{spaceName}/{fileName}";
        
        // Compute signature
        var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(secretKey));
        string signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(toSign)));
        
        // Build request
        string urlRoot = "https://mit.nyc3.digitaloceanspaces.com";
        string finalUrl = $"{urlRoot}/{fileName}";
        var request = new UnityWebRequest(finalUrl, UnityWebRequest.kHttpVerbPUT);
        request.uploadHandler = new UploadHandlerRaw(textureData);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Host", $"mit.nyc3.digitaloceanspaces.com");
        request.SetRequestHeader("Date", date);
        request.SetRequestHeader("Content-Type", contentType);
        request.SetRequestHeader("x-amz-acl", "public-read");
        request.SetRequestHeader("x-amz-storage-class", "STANDARD");
        request.SetRequestHeader("Authorization", $"AWS {accessKey}:{signature}");
        request.certificateHandler = new BypassCertificate();

        onNewImageStatus?.Invoke("Uploading image...");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Upload successful!");
            onNewImageStatus?.Invoke("Upload successful!");
            onImageUploadComplete?.Invoke(finalUrl);
        }
        else
        {
            Debug.LogError("Upload failed: " + request.error);
            onNewImageStatus?.Invoke("Upload error!");
        }
    }
}

public class BypassCertificate : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        // Always accept
        return true;
    }
}
