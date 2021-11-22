//comment out to not use single camera rendering
#define SINGLE_CAMERA

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloudsManager : MonoBehaviour
{
    [Header("Player View Camera")]
    public Camera viewer;

    [Header("Compute Shader")]
    public ComputeShader cloudsCompute;

    [Header("Material For Clouds")]
    public Material CloudMaterial;

    [Header("Settings For the Generated Mesh")]
    public CloudChunkSettings chunkSettings;

    [Header("Graphics Settings for Clouds")]
    public CloudMaterialSettings cloudMaterialSettings;

    //the generatedMesh Used to create clouds
    Mesh cloudSourceMesh;

    //the current chunk our viewer is in
    Vector2 currentChunkCoord;

    //camera passed to instances. Is null if not using sungle rendering
    Camera renderCamera;

    List<CloudInstance> cloudInstances = new List<CloudInstance>();

    Matrix4x4 orthoMatrix;

    bool initialized;

    private void Start()
    {
        OnValidate();
    }

    private void OnValidate()
    {
        if (initialized)
        {
            DisposeOfChunks();
        }

        SetupViewer();

        SetupCloudGraphics();

        GenerateMesh();

        InitializeClouds();

        initialized = true;
    }

    void DisposeOfChunks()
    {
        for(int i = 0; i < cloudInstances.Count; i++)
        {
            cloudInstances[i].DisposeOfChunk();
        }

        cloudInstances.Clear();

        initialized = false;
    }

    void SetupCloudGraphics()
    {
        if(CloudMaterial == null)
        {
            CloudMaterial = new Material(Shader.Find("Unlit/Clouds"));
        }

        CloudMaterial.SetColor("_Color", cloudMaterialSettings.color);
        CloudMaterial.SetTexture("_NoiseTex", cloudMaterialSettings.cloudTexture);
        CloudMaterial.SetVector("_NoiseTexST1", cloudMaterialSettings.TexSettings1.CloudST);
        CloudMaterial.SetVector("_NoiseTexST2", cloudMaterialSettings.TexSettings2.CloudST);
        CloudMaterial.SetVector("_NoiseDir1", cloudMaterialSettings.TexSettings1.CloudDirection);
        CloudMaterial.SetVector("_NoiseDir2", cloudMaterialSettings.TexSettings2.CloudDirection);
        CloudMaterial.SetFloat("_NoiseSpeed1", cloudMaterialSettings.TexSettings1.CloudSpeed);
        CloudMaterial.SetFloat("_NoiseSpeed2", cloudMaterialSettings.TexSettings2.CloudSpeed);
        CloudMaterial.SetFloat("_CloudShadowAmount", cloudMaterialSettings.cloudShadowAmount);
        CloudMaterial.SetFloat("_CloudShadowValue", cloudMaterialSettings.cloudShadowValue);
        CloudMaterial.SetFloat("_CloudColorLB", cloudMaterialSettings.CloudBrightnessBoost);
        CloudMaterial.SetFloat("_AlphaThresh", cloudMaterialSettings.cloudAlphaCutoff);
        CloudMaterial.SetFloat("_SubSurfaceSize", cloudMaterialSettings.SunHighlightsSize);
        CloudMaterial.SetFloat("_WorldBendingAmount", cloudMaterialSettings.bending);
        CloudMaterial.SetFloat("_FadeMin", cloudMaterialSettings.CloudAlphaFadeDistance);

#if SINGLE_CAMERA
            renderCamera = viewer;
#endif
    }

    void SetupViewer()
    {
        if (viewer == null)
        {
            viewer = Camera.main;
        }

        float cameraHeight = Mathf.Cos(viewer.fieldOfView * Mathf.Deg2Rad) * viewer.farClipPlane;
        float cameraWidth = cameraHeight * viewer.aspect;

        orthoMatrix = Matrix4x4.Ortho(-cameraWidth, cameraWidth, -cameraHeight, cameraHeight, viewer.nearClipPlane, viewer.farClipPlane);
    }

    void GenerateMesh()
    {       
        Vector3 startOffset = new Vector3(-1f, 0f, -1f) * (chunkSettings.CloudChunkWorldSize / 2f);

        int numVertsPerLine = chunkSettings.VertsPerCloudChunkSide;

        Vector3[] verts = new Vector3[numVertsPerLine * numVertsPerLine];
        int[] tris = new int[verts.Length * 3 * 2];

        int triPos = 0;

        for (int x = 0; x < numVertsPerLine; x++)
        {
            for (int y = 0; y < numVertsPerLine; y++)
            {
                Vector2 percent = new Vector2(x, y) / (numVertsPerLine - 1f);
                verts[x * numVertsPerLine + y] = new Vector3(percent.x, 0f, percent.y) * chunkSettings.CloudChunkWorldSize + startOffset;

                if (x < numVertsPerLine - 1 && y < numVertsPerLine - 1)
                {
                    int a = x * numVertsPerLine + y;
                    int b = x * numVertsPerLine + y + 1;
                    int c = (x + 1) * numVertsPerLine + y;
                    int d = (x + 1) * numVertsPerLine + y + 1;

                    tris[triPos] = a;
                    tris[triPos + 1] = b;
                    tris[triPos + 2] = c;

                    tris[triPos + 3] = c;
                    tris[triPos + 4] = b;
                    tris[triPos + 5] = d;

                    triPos += 6;
                }
            }
        }

        cloudSourceMesh = new Mesh();

        cloudSourceMesh.vertices = verts;
        cloudSourceMesh.triangles = tris;

    }

    void InitializeClouds()
    {
        ResetCurrentChunkCoord();

        viewer.cullingMatrix = orthoMatrix * viewer.worldToCameraMatrix;

        for (int x = -chunkSettings.chunksPerSide; x <= chunkSettings.chunksPerSide; x++)
        {
            for (int y = -chunkSettings.chunksPerSide; y <= chunkSettings.chunksPerSide; y++)
            {
                Vector2 chunkCoord = currentChunkCoord + new Vector2(x, y);

                CreateCloudChunk(chunkCoord);
            }
        }
    }

    void CreateCloudChunk(Vector2 chunkCoord)
    {
        Vector3 position = new Vector3(chunkCoord.x, 0f, chunkCoord.y) * chunkSettings.CloudChunkWorldSize;

        ComputeShader computeShader = Instantiate(cloudsCompute);

        Material material = Instantiate(CloudMaterial);

        CloudInstance cloudInstance = new CloudInstance(cloudSourceMesh, chunkSettings, computeShader, material, renderCamera, position, chunkCoord);

        cloudInstances.Add(cloudInstance);
    }

    bool ResetCurrentChunkCoord()
    {
        Vector2 testCoord = new Vector2();
        testCoord.x = Mathf.RoundToInt(viewer.transform.position.x / chunkSettings.CloudChunkWorldSize);
        testCoord.y = Mathf.RoundToInt(viewer.transform.position.z / chunkSettings.CloudChunkWorldSize);

        if(testCoord != currentChunkCoord)
        {
            currentChunkCoord = testCoord;
            return true;
        }

        return false;
    }

    bool IsChunkVisible(Vector2 chunkCoord)
    {
        if(chunkCoord.x >= currentChunkCoord.x - chunkSettings.chunksPerSide && chunkCoord.x <= currentChunkCoord.x + chunkSettings.chunksPerSide &&
            chunkCoord.y >= currentChunkCoord.y - chunkSettings.chunksPerSide && chunkCoord.y <= currentChunkCoord.y + chunkSettings.chunksPerSide)
        {
            return true;
        }

        return false;
    }

    void UpdateChunks()
    {
        if(ResetCurrentChunkCoord())
        {
            viewer.cullingMatrix = orthoMatrix * viewer.worldToCameraMatrix;

            HashSet<Vector2> UpdatedChunks = new HashSet<Vector2>();

            for (int i = cloudInstances.Count - 1; i >= 0; i--)
            {
                CloudInstance cloud = cloudInstances[i];

                if (!IsChunkVisible(cloud.cloudCoordinate))
                {
                    cloud.DisposeOfChunk();
                    cloudInstances.RemoveAt(i);
                }
                else
                {
                    UpdatedChunks.Add(cloud.cloudCoordinate);
                }
            }

            for (int x = -chunkSettings.chunksPerSide; x <= chunkSettings.chunksPerSide; x++)
            {
                for (int y = -chunkSettings.chunksPerSide; y <= chunkSettings.chunksPerSide; y++)
                {
                    Vector2 chunkCoord = currentChunkCoord + new Vector2(x, y);

                    if (!UpdatedChunks.Contains(chunkCoord))
                    {
                        CreateCloudChunk(chunkCoord);                       
                    }                   
                }
            }
        }

        viewer.cullingMatrix = orthoMatrix * viewer.worldToCameraMatrix;

        for (int i = 0; i < cloudInstances.Count; i++)
        {
            cloudInstances[i].DrawChunk();
        }
    }

    void Update()
    {
        UpdateChunks();
    }

    private void OnPreRender()
    {
        viewer.ResetCullingMatrix();
    }

    private void OnDisable()
    {
        for (int i = 0; i < cloudInstances.Count; i++)
        {
            cloudInstances[i].DisposeOfChunk();
        }
    }
}