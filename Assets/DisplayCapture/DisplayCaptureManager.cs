using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

namespace Anaglyph.DisplayCapture
{
	[DefaultExecutionOrder(-1000)]
	public class DisplayCaptureManager : MonoBehaviour
	{
		public static DisplayCaptureManager Instance { get; private set; }

		public bool startScreenCaptureOnStart = true;
		public bool flipTextureOnGPU = false;

		[SerializeField] private Vector2Int textureSize = new(1024, 1024);
		public Vector2Int Size => textureSize;

		private Texture2D screenTexture;
		public Texture2D ScreenCaptureTexture => screenTexture;
		
		private RenderTexture flipTexture;

		public Matrix4x4 ProjectionMatrix { get; private set; }

		public UnityEvent<Texture2D> onTextureInitialized = new();
		public UnityEvent onStarted = new();
		public UnityEvent onPermissionDenied = new();
		public UnityEvent onStopped = new();
		public UnityEvent onNewFrame = new();

		private unsafe sbyte* imageData;
		private int bufferSize;
		
		[Header("Debug")]
		public bool screenCaptureStarted = false;
		public bool isOneFrameCaptureRequested = false;
		private Texture2D _oneFrameTexture;
		public Texture2D OneFrameTexture => _oneFrameTexture;

		private class AndroidInterface
		{
			private AndroidJavaClass androidClass;
			private AndroidJavaObject androidInstance;

			public AndroidInterface(GameObject messageReceiver, int textureWidth, int textureHeight)
			{
				androidClass = new AndroidJavaClass("com.trev3d.DisplayCapture.DisplayCaptureManager");
				androidInstance = androidClass.CallStatic<AndroidJavaObject>("getInstance");
				androidInstance.Call("setup", messageReceiver.name, textureWidth, textureHeight);
			}

			public void RequestCapture() => androidInstance.Call("requestCapture");
			public void StopCapture() => androidInstance.Call("stopCapture");

			public unsafe sbyte* GetByteBuffer()
			{
				AndroidJavaObject byteBuffer = androidInstance.Call<AndroidJavaObject>("getByteBuffer");
				return AndroidJNI.GetDirectBufferAddress(byteBuffer.GetRawObject());
			}
		}

		private AndroidInterface androidInterface;

		private void Awake()
		{
			Instance = this;

			androidInterface = new AndroidInterface(gameObject, Size.x, Size.y);

			screenTexture = new Texture2D(Size.x, Size.y, TextureFormat.RGBA32, 1, false);
			_oneFrameTexture = new Texture2D(Size.x, Size.y, TextureFormat.RGBA32, 1, false);
		}

		private void Start()
		{
			flipTexture = new RenderTexture(Size.x, Size.y, 1, RenderTextureFormat.ARGB32, 1);
			flipTexture.Create();

			// onTextureInitialized.Invoke(screenTexture);
			onTextureInitialized.Invoke(_oneFrameTexture);

			if (startScreenCaptureOnStart)
			{
				StartScreenCapture();
			}
			bufferSize = Size.x * Size.y * 4; // RGBA_8888 format: 4 bytes per pixel
		}

		public void StartScreenCapture()
		{
			androidInterface.RequestCapture();
		}
		
		public void RequestOneFrameCapture()
		{
			isOneFrameCaptureRequested = true;

			if (!screenCaptureStarted)
			{
				androidInterface.RequestCapture();
			}
		}

		public void StopScreenCapture()
		{
			androidInterface.StopCapture();
		}

		private void ResetOneFrameOrder()
		{
			isOneFrameCaptureRequested = false;
		}

		// Messages sent from Android

#pragma warning disable IDE0051 // Remove unused private members
		private unsafe void OnCaptureStarted()
		{
			onStarted.Invoke();
			imageData = androidInterface.GetByteBuffer();
			screenCaptureStarted = true;
		}

		private void OnPermissionDenied()
		{
			ResetOneFrameOrder();
			onPermissionDenied.Invoke();
		}

		private unsafe void OnNewFrameAvailable()
		{
			if (imageData == default) return;
			
			Texture2D textureToUse = isOneFrameCaptureRequested ? _oneFrameTexture : screenTexture;
			
			textureToUse.LoadRawTextureData((IntPtr)imageData, bufferSize);
			textureToUse.Apply();
			
			// converted to PNG
			var bytes = textureToUse.EncodeToPNG();

			if (flipTextureOnGPU)
			{
				Graphics.Blit(textureToUse, flipTexture, new Vector2(1, -1), Vector2.zero);
				Graphics.CopyTexture(flipTexture, textureToUse);
			}

			ResetOneFrameOrder();

			// onNewFrame.Invoke();
		}

		private void OnCaptureStopped()
		{
			ResetOneFrameOrder();
			onStopped.Invoke();
			screenCaptureStarted = false;
		}
#pragma warning restore IDE0051 // Remove unused private members
	}
}