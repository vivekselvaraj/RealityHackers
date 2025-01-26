using UnityEngine;

public class RaycastPainter : MonoBehaviour
{
    
    // TODO: To be attached to an empty game object
    // TODO: convert the input get mouse button to controller trigger
    
    public Color sprayColor = Color.red; // The color to spray
    public float sprayRadius = 0.05f; // Spray radius in UV space (0 to 1)
    public int sprayDensity = 100; // Number of paint particles per spray

    void Update()
    {
        if (Input.GetMouseButton(0)) // Detect if the left mouse button is held down
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Check if the hit object has an OverlayTextureCreator component
                OverlayTextureCreator overlayCreator = hit.collider.GetComponent<OverlayTextureCreator>();

                if (overlayCreator != null)
                {
                    Texture2D overlayTexture = overlayCreator.OverlayTexture;

                    if (overlayTexture != null)
                    {
                        // Get UV coordinates from the raycast hit
                        Vector2 hitUV = NormalizeUV(hit.textureCoord);

                        // Apply spray paint at the UV point
                        ApplySpray(overlayTexture, hitUV);

                        // Apply the changes to the texture
                        overlayTexture.Apply();

                        Debug.Log($"Sprayed on {hit.collider.gameObject.name} at UV: {hitUV}");
                    }
                }
            }
        }
    }

    private void ApplySpray(Texture2D texture, Vector2 centerUV)
    {
        for (int i = 0; i < sprayDensity; i++)
        {
            // Generate a random point within the spray radius
            Vector2 randomOffset = Random.insideUnitCircle * sprayRadius;

            // Calculate the UV position
            Vector2 uv = centerUV + randomOffset;

            // Convert UV to pixel positions
            int pixelX = (int)(uv.x * texture.width);
            int pixelY = (int)(uv.y * texture.height);

            // Check bounds to prevent accessing pixels outside the texture
            if (pixelX >= 0 && pixelX < texture.width && pixelY >= 0 && pixelY < texture.height)
            {
                // Blend the spray color with the existing pixel color
                Color existingColor = texture.GetPixel(pixelX, pixelY);
                Color blendedColor = Color.Lerp(existingColor, sprayColor, 0.5f); // Adjust blend strength as needed
                texture.SetPixel(pixelX, pixelY, blendedColor);
            }
        }
    }

    private Vector2 NormalizeUV(Vector2 uv)
    {
        // Ensure UV coordinates are in the range [0, 1]
        return new Vector2(uv.x - Mathf.Floor(uv.x), uv.y - Mathf.Floor(uv.y));
    }
}
