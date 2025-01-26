using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InnerTransformProvider : MonoBehaviour
{
    public Transform innerTransform;
    private Texture2D _snapshotImage;
    public RawImage snapshotImage;
    
    public void SetupPreviewImage(Texture2D image)
    {
        _snapshotImage = image;
        snapshotImage.texture = _snapshotImage;
    }
}
