using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class RequestManager : MonoBehaviour
{
    public string serverUrl;
    public string authorizationToken;
    
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
}
