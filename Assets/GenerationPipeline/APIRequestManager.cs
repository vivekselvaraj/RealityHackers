using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class APIRequestManager : MonoBehaviour {
    [Header("API Configuration")] public string hostUrl = "https://tencent-hunyuan3d-2.hf.space";
    public string apiEndPoint = "/call/generation_all";
    
    [Header("Request Data")]
    public string textPrompt = "";
    public string imageUrl = "";
    public int inferenceStep = 20;
    public int guidanceScale = 3;
    public int seed = 0;
    public string octreeResolution = "256";
    public bool removeBackground = true;

    [Header("Response Data")]
    public int responseCutoffTime = 100;

    
    [Header("References")]
    public GlbLoader glbLoader;
    
    private Secrets _secrets;
    private string apiUrl => hostUrl + apiEndPoint;
    private void Start()
    {
        _secrets = SecretManager.GetSecrets();
        if (_secrets != null)
        {
            StartCoroutine(SendPostRequest());
        }
        else
        {
            Debug.LogError("Secrets not found. Ensure API keys are configured.");
        }
    }

    private IEnumerator SendPostRequest()
    {
        string jsonData = $@"
        {{
            ""data"": [
                ""{textPrompt}"",
                {{""path"": ""{imageUrl}""}},
                {inferenceStep},
                {guidanceScale},
                {seed},
                ""{octreeResolution}"",
                {removeBackground.ToString().ToLower()}
            ]
        }}";
        Debug.Log(jsonData);
        
        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {_secrets.huggingFaceApiKey}");
        Debug.Log(request.GetRequestHeader("Authorization"));
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Response: " + request.downloadHandler.text);

            string eventId = ExtractEventId(request.downloadHandler.text);
            if (!string.IsNullOrEmpty(eventId))
            {
                // Start the long polling process
                StartCoroutine(LongPollingRequest(eventId));
            }
            else
            {
                Debug.LogError("Failed to extract event_id from the response.");
            }
        }
        else
        {
            Debug.LogError("Request failed: " + request.error);
        }
    }

    private IEnumerator LongPollingRequest(string eventId)
    {
        string pollingUrl = $"{apiUrl}/{eventId}";
        float startTime = Time.time;

        using (UnityWebRequest request = UnityWebRequest.Get(pollingUrl))
        {
            request.SetRequestHeader("Authorization", $"Bearer {_secrets.huggingFaceApiKey}");

            // Start the request
            request.SendWebRequest();

            float delayBetweenChecks = 1f; // 1-second delay

            while (!request.isDone)
            {
                // Check if the request has exceeded the timeout
                if (Time.time - startTime > responseCutoffTime)
                {
                    Debug.LogError("Long connection timed out after " + responseCutoffTime + " seconds.");
                    request.Abort(); // Kill the connection
                    yield break;
                }

                // Log the elapsed time periodically
                Debug.Log("Still waiting... Time elapsed: " + (Time.time - startTime) + " seconds.");

                // Wait for the specified delay before the next check
                yield return new WaitForSeconds(delayBetweenChecks);
            }

            // Process the response if it completes successfully
            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                if (responseText.Contains("complete")) {
                    string glbUrl = $"{hostUrl}/static/{eventId}/textured_mesh.glb";
                    Debug.Log(glbUrl);
                    glbLoader.LoadRemoteGLBToSceneWithURL(glbUrl);
                    
                }
                else {
                    Debug.Log(responseText);
                }
            }
            else
            {
                Debug.LogError("Request failed: " + request.error);
            }
        }
    }

    private string ExtractEventId(string response)
    {
        try
        {
            var json = JsonUtility.FromJson<ResponseData>(response);
            return json.event_id;
        }
        catch
        {
            Debug.LogError("Failed to parse response JSON for event_id.");
            return null;
        }
    }

    [System.Serializable]
    private class ResponseData
    {
        public string event_id;
    }
}
