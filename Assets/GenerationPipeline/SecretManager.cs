using System.IO;
using UnityEngine;

[System.Serializable]
public class Secrets
{
    public string huggingFaceApiKey;
}

public static class SecretManager
{
    private static Secrets secrets;

    public static Secrets GetSecrets()
    {
        if (secrets == null)
        {
            // Construct the path to the Secrets.json file
            string path = Path.Combine(Application.dataPath, "Secrets.json");

            // Check if the file exists
            if (File.Exists(path))
            {
                Debug.Log($"Path:{path} File found at ");
                // Read the file and parse it into the Secrets object
                string json = File.ReadAllText(path);
                secrets = JsonUtility.FromJson<Secrets>(json);
            }
            else
            {
                Debug.LogError($"Path:{path}: Secrets.json file not found! Ensure it exists and contains the necessary keys.");
            }
        }

        return secrets;
    }
}