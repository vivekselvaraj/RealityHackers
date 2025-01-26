using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RaycastPainter : MonoBehaviour
{
    public Color[] sprayColors = { Color.red, Color.blue, Color.green }; // Set of colors to rotate
    public float sprayRadius = 0.05f; // Spray radius in UV space (0 to 1)
    public int sprayDensity = 100; // Number of paint particles per spray

    public OVRInput.Controller controller = OVRInput.Controller.RTouch; // Default to the right-hand controller

    private LineRenderer lineRenderer; // LineRenderer to visualize the ray
    private Color defaultColor; // Lighter version of sprayColor for always-on ray
    private int currentColorIndex = 0; // Index of the current color in sprayColors

    void Start()
    {
        // Initialize the LineRenderer
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2; // A line requires two points
        lineRenderer.startWidth = 0.01f; // Adjust the line width as needed
        lineRenderer.endWidth = 0.01f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default")); // Simple material

        // Set the initial spray color and default color
        UpdateSprayColor();
    }

    void Update()
    {
        HandleThumbstickInput();

        // Get the controller's position and rotation
        Vector3 controllerPosition = OVRInput.GetLocalControllerPosition(controller);
        Quaternion controllerRotation = OVRInput.GetLocalControllerRotation(controller);

        // Create a ray from the controller's position in its forward direction
        Ray ray = new Ray(controllerPosition, controllerRotation * Vector3.forward);

        // Set the start of the line to the controller's position
        lineRenderer.SetPosition(0, controllerPosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Set the end of the line to the hit position
            lineRenderer.SetPosition(1, hit.point);

            // Check if the trigger button is pressed
            if (OVRInput.Get(OVRInput.RawButton.RIndexTrigger))
            {
                // Use the full spray color when the trigger is pressed
                lineRenderer.startColor = sprayColors[currentColorIndex];
                lineRenderer.endColor = sprayColors[currentColorIndex];

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
            else
            {
                // Revert to the lighter default color when the trigger is not pressed
                lineRenderer.startColor = defaultColor;
                lineRenderer.endColor = defaultColor;
            }
        }
        else
        {
            // Set the end of the line to a point far along the ray direction
            lineRenderer.SetPosition(1, ray.origin + ray.direction * 10f);

            // Ensure the color reflects the trigger state
            if (!OVRInput.Get(OVRInput.RawButton.RIndexTrigger))
            {
                lineRenderer.startColor = defaultColor;
                lineRenderer.endColor = defaultColor;
            }
        }
    }

    private void HandleThumbstickInput()
    {
        // Check for thumbstick left press
        if (OVRInput.GetDown(OVRInput.RawButton.RThumbstickLeft))
        {
            currentColorIndex = (currentColorIndex - 1 + sprayColors.Length) % sprayColors.Length;
            UpdateSprayColor();
        }

        // Check for thumbstick right press
        if (OVRInput.GetDown(OVRInput.RawButton.RThumbstickRight))
        {
            currentColorIndex = (currentColorIndex + 1) % sprayColors.Length;
            UpdateSprayColor();
        }
    }

    private void UpdateSprayColor()
    {
        // Update the spray color and the lighter default color
        defaultColor = new Color(sprayColors[currentColorIndex].r, sprayColors[currentColorIndex].g, sprayColors[currentColorIndex].b, 0.3f);
    }

    private void ApplySpray(Texture2D texture, Vector2 centerUV)
    {
        for (int i = 0; i < sprayDensity; i++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * sprayRadius;
            Vector2 uv = centerUV + randomOffset;
            int pixelX = (int)(uv.x * texture.width);
            int pixelY = (int)(uv.y * texture.height);

            if (pixelX >= 0 && pixelX < texture.width && pixelY >= 0 && pixelY < texture.height)
            {
                Color existingColor = texture.GetPixel(pixelX, pixelY);
                Color blendedColor = Color.Lerp(existingColor, sprayColors[currentColorIndex], 0.5f);
                texture.SetPixel(pixelX, pixelY, blendedColor);
            }
        }
    }

    private Vector2 NormalizeUV(Vector2 uv)
    {
        return new Vector2(uv.x - Mathf.Floor(uv.x), uv.y - Mathf.Floor(uv.y));
    }
}
