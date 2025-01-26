using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using GLTFast;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Windows;

public class GlbLoader : MonoBehaviour
{
    public string glbURL;
    public RequestManager requestManager;
    public Transform spawnPosition;
    public float spawnScale = 0.15f;
    
    // add context menu button to load the glb file
    [ContextMenu("Load GLB")]
    void LoadGlb() {
        DownloadGLBviaWebRequest(glbURL);
    }

    private void DownloadGLBviaWebRequest(string downloadURL)
    {
        requestManager.DownloadGlb(glbURL);
    }

    private void OnEnable()
    {
        requestManager.OnLocalSavingComplete += OnLocalObjectSaveCompleteHandler;
    }

    private void OnDisable()
    {
        requestManager.OnLocalSavingComplete -= OnLocalObjectSaveCompleteHandler;
    }

    private void OnLocalObjectSaveCompleteHandler(string obj)
    {
        var generatedObject = new GameObject();
        generatedObject.name = "GeneratedObject";
        var gltfAsset = generatedObject.AddComponent<GltfAsset>();
        gltfAsset.Url = obj;
        gltfAsset.StreamingAsset = true;
    }
    
    public void LoadRemoteGLBToSceneWithURL(string remoteURL)
    {
        var generatedObject = new GameObject();
        generatedObject.transform.position = spawnPosition.position;
        generatedObject.name = "GeneratedObject";
        var gltfAsset = generatedObject.AddComponent<GltfAsset>();
        gltfAsset.Url = remoteURL;
        generatedObject.transform.localScale = new Vector3(spawnScale, spawnScale, spawnScale);
    }
}
