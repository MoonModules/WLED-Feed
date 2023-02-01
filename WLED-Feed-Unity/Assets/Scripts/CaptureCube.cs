using System;
using System.Collections.Generic;
using System.IO;
using DefaultNamespace;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class CaptureCube : MonoBehaviour
{
    private struct CubeFace
    {
        public Camera Camera;
        public RenderTexture RT;
    }

    [Header("Parameters")]
    public int Resolution = 8;
    public enum CaptureMethod { Simple, Async }
    public CaptureMethod Capture = CaptureMethod.Simple;
    public bool WriteDebugFile;
    [Header("Network")]
    public string SendIP = "192.168.121.109";
    public int SendPort = DdpConnection.DDP_DEFAULT_PORT;
    public int ReceivePort = DdpConnection.DDP_DEFAULT_PORT;
    [Header("Components")]
    public GameObject CameraPrefab;
    public GameObject CubePrefab;
    public Transform CubeParent;
    public List<RawImage> Displays;
    public Material LedMask;

    private List<CubeFace> m_cubeFaces;
    private RenderTexture m_captureRT;
    private NativeArray<byte> m_buffer;
    private int m_faceCount;
    private DdpConnection m_connection;

    #region Unity callbacks

    private void Start()
    {
        m_cubeFaces = new List<CubeFace>();
        m_cubeFaces.Add(InstantiateCubeFace(Quaternion.Euler(0, 0, 0)));
        m_cubeFaces.Add(InstantiateCubeFace(Quaternion.Euler(0, 90, 0)));
        m_cubeFaces.Add(InstantiateCubeFace(Quaternion.Euler(0, 180, 0)));
        m_cubeFaces.Add(InstantiateCubeFace(Quaternion.Euler(0, 270, 0)));
        m_cubeFaces.Add(InstantiateCubeFace(Quaternion.Euler(90, 0, 0)));
        m_faceCount = m_cubeFaces.Count;

        for (int i = 0; i < m_cubeFaces.Count; i++)
            Displays[i].texture = m_cubeFaces[i].RT;

        LedMask.mainTextureScale = Vector2.one * Resolution;

        m_connection = new DdpConnection();
        m_connection.StartConnection(SendIP, SendPort, ReceivePort);
    }

    private void Update()
    {
        byte[] bytes;
        switch (Capture)
        {
            case CaptureMethod.Simple:
                bytes = CaptureSimple();
                break;
            case CaptureMethod.Async:
                bytes = CaptureAsync();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        m_connection.realtimeBroadcast(0, SendIP, Resolution * Resolution, bytes, 1, false);
    }

    private void OnDestroy()
    {
        if (Capture == CaptureMethod.Async)
        {
            AsyncGPUReadback.WaitAllRequests();

            Destroy(m_captureRT);
            foreach (CubeFace cubeFace in m_cubeFaces)
                Destroy(cubeFace.RT);
            m_buffer.Dispose();
        }    
    }

    #endregion

    private CubeFace InstantiateCubeFace(Quaternion _rotation)
    {
        GameObject go = Instantiate(CameraPrefab, Vector3.zero, _rotation);
        CubeFace cubeFace = new CubeFace
        {
            Camera = go.GetComponent<Camera>(),
            RT = new RenderTexture(Resolution, Resolution, 0),
        };
        cubeFace.Camera.targetTexture = cubeFace.RT;
        cubeFace.RT.filterMode = FilterMode.Point;

        go = Instantiate(CubePrefab, CubeParent.position, _rotation, CubeParent);
        go.GetComponentInChildren<Renderer>().material.mainTexture = cubeFace.RT;

        return cubeFace;
    }

    #region Capture

    private byte[] CaptureSimple()
    {
        Texture2D capture = new Texture2D(Resolution * m_faceCount, Resolution, TextureFormat.RGB24, false);
        for (int i = 0; i < m_faceCount; i++)
        {
            RenderTexture.active = m_cubeFaces[i].RT;
            capture.ReadPixels(new Rect(0, 0, Resolution, Resolution), i * Resolution, 0);
        }
        if (WriteDebugFile)
            File.WriteAllBytes("test.png", capture.EncodeToPNG());
        
        return capture.GetRawTextureData();
    }

    private byte[] CaptureAsync()
    {
        // Read back RenderTexture to CPU (see https://github.com/keijiro/AsyncCaptureTest)
        m_captureRT = new RenderTexture(Resolution, Resolution * m_faceCount, 0);
        var (scale, offs) = (new Vector2(1, -1), new Vector2(0, 1));
        if (false)
            // Attempt to blit all faces into the same texture, doesn't work
            for (int i = 0; i < m_faceCount; i++)
            {
                Graphics.Blit(m_cubeFaces[i].RT, m_captureRT, scale, offs);
                offs.y += Resolution;
            }
        else
            Graphics.Blit(m_cubeFaces[0].RT, m_captureRT, scale, offs);

        m_buffer = new NativeArray<byte>(Resolution * Resolution * m_faceCount * 4, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        AsyncGPUReadback.RequestIntoNativeArray(ref m_buffer, m_captureRT, 0, OnCompleteReadback);

        return m_buffer.ToRawBytes();   // Untested
    }

    void OnCompleteReadback(AsyncGPUReadbackRequest request)
    {
        if (request.hasError)
        {
            Debug.Log("GPU readback error detected.");
            return;
        }

        using var encoded = ImageConversion.EncodeNativeArrayToPNG(m_buffer, m_captureRT.graphicsFormat, (uint)m_captureRT.width, (uint)m_captureRT.height);
        if (WriteDebugFile)
            File.WriteAllBytes("test.png", encoded.ToArray());
    }

    #endregion
}