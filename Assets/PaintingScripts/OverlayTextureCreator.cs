using UnityEngine;

public class OverlayTextureCreator : MonoBehaviour
{
    // TODO: To be attached to the object with the mesh filter
    // This dynamically adds a mesh collider to the object if there is none
    
    public Shader overlayShader; // Assign a shader for the overlay material (e.g., Unlit/Transparent)
    public int textureResolution = 512;
    private Material overlayMaterial; // The runtime-created overlay material
    private Texture2D overlayTexture; // The runtime-created texture

    public Texture2D OverlayTexture => overlayTexture; // Expose the texture to other scripts
    public Material OverlayMaterial => overlayMaterial; // Expose the material to other scripts

    void Start()
    {
        EnsureMeshCollider();
        InitializeOverlay();
    }
    private void EnsureMeshCollider()
    {
        if (GetComponent<Collider>() == null) // Check if the object already has any Collider
        {
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                // Add a MeshCollider and assign the mesh
                MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = meshFilter.sharedMesh;

                Debug.Log("MeshCollider was added to the GameObject.");
            }
            else
            {
                Debug.LogError("No MeshFilter or Mesh found. Cannot add MeshCollider.");
            }
        }
        else
        {
            Debug.Log("Collider already exists on the GameObject.");
        }
    }

    private void InitializeOverlay()
    {
        Renderer renderer = GetComponent<Renderer>();

        if (renderer == null)
        {
            Debug.LogError("No Renderer found on the GameObject. Overlay initialization failed.");
            return;
        }

        // Create a new overlay texture
        overlayTexture = new Texture2D(textureResolution, textureResolution, TextureFormat.RGBA32, false);
        ClearOverlayTexture(Color.clear); // Start with a transparent texture

        // Create a new overlay material
        if (overlayShader == null)
        {
            overlayShader = Shader.Find("Unlit/Transparent"); // Default shader for the overlay
        }

        overlayMaterial = new Material(overlayShader)
        {
            mainTexture = overlayTexture
        };

        // Add the overlay material to the Renderer
        var materials = renderer.materials;
        System.Array.Resize(ref materials, materials.Length + 1); // Add space for the new material
        materials[materials.Length - 1] = overlayMaterial;
        renderer.materials = materials;

        Debug.Log("Overlay material initialized and applied.");
    }

    private void ClearOverlayTexture(Color clearColor)
    {
        for (int x = 0; x < overlayTexture.width; x++)
        {
            for (int y = 0; y < overlayTexture.height; y++)
            {
                overlayTexture.SetPixel(x, y, clearColor);
            }
        }
        overlayTexture.Apply();
    }
}
