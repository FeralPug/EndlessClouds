using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CloudInstance
{
    //the mesh used to calculate vertices in the compute shader
    Mesh sourceMesh;

    //settings about how the compute shader should do that
    CloudChunkSettings settings;

    //the compute shader
    ComputeShader cloudsCompute;

    //the material used for the procedural draw
    Material cloudMaterial;

    //the camera rendering the clouds, this can be null which means all cameras are rendering the clouds
    Camera renderCamera;

    //would space position of the center of the clouds
    Vector3 chunkPosition;

    //the clouds coordinate in the spawning grid
    public Vector2 cloudCoordinate { get; private set; }

    //keeps track of if we have allocated compute buffers and what not
    bool initialized;

    //Compute Shader And Procedural Draw 
    //Kernal ID
    int cloudKernalID;

    //number of times to run computeShader
    int dispatchSize;

    //bounds for the generated mesh
    Bounds bounds;

    //buffers for the compute shader
    //the verts of the source Mesh
    ComputeBuffer SourceVerts;
    //the triangle indices of the source mesh
    ComputeBuffer SourceTriangles;
    //indirect arguments for the graphics shader
    ComputeBuffer IndirectArgsBuffer;
    //the result buffer of the computeShader
    ComputeBuffer DrawTriangles;

    //the reset of the argsBuffer if we need to re exectute
    int[] argsReset = { 0, 1, 0, 0 };

    //this is the data type of the source vertex buffer
    //this attribute just makes the memory be laid out sequentially which will improve performance but I dont think is necessary
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    struct SourceVertex
    {
        public Vector3 pos;
    }

    //buffer strides
    const int SOURCE_VERTS_STRIDE = sizeof(float) * 3;
    const int SOURCE_TRIS_STRIDE = sizeof(int);
    const int INDIRECT_ARGS_STRIDE = sizeof(int) * 4;
    const int DRAW_TRI_STRIDE = sizeof(float) * 3 * 3 + sizeof(float) * 3;

    //number of triangles in the source mesh
    int numSourceTriangles { get => sourceMesh.triangles.Length / 3; }

    //material ids
    int drawBufferGraphicsID;

    //constructor it generates its bounds here
    public CloudInstance(Mesh sourceMesh, CloudChunkSettings settings, ComputeShader cloudsCompute, Material cloudMaterial, Camera renderCamera, Vector3 chunkPosition, Vector2 cloudCoordinate)
    {
        this.sourceMesh = sourceMesh;
        this.settings = settings;
        this.cloudsCompute = cloudsCompute;
        this.cloudMaterial = cloudMaterial;
        this.renderCamera = renderCamera;
        this.chunkPosition = chunkPosition;
        this.cloudCoordinate = cloudCoordinate;
        GenerateBounds();
    }

    //this is called by the manager to draw the chunks
    public void DrawChunk()
    {
        //if we arent initialized we need to do so
        //this happens the first time they are draw
        if (!initialized)
        {
            InitializeComputeShader();
            ExecuteComputeShader();       
        }

        //draw
        Graphics.DrawProceduralIndirect(cloudMaterial, bounds, MeshTopology.Triangles, IndirectArgsBuffer, 0, renderCamera, null, ShadowCastingMode.Off, false, 0);
    }

    //initializes the compue and graphics shader, pretty much boilerplate code
    void InitializeComputeShader()
    {
        //if initialized we need to release used buffers before reassigning them as they are not managed by GC
        if (initialized)
        {
            DisposeOfComputeShaderResources();
        }

        //get the Kernal Index
        cloudKernalID = cloudsCompute.FindKernel("CSMain");

        //get the buffer id for the graphics shader
        drawBufferGraphicsID = Shader.PropertyToID("DrawTriangles");

        //assign the buffers
        SourceVerts = new ComputeBuffer(sourceMesh.vertexCount, SOURCE_VERTS_STRIDE, ComputeBufferType.Structured, ComputeBufferMode.Immutable);
        SourceTriangles = new ComputeBuffer(sourceMesh.triangles.Length, SOURCE_TRIS_STRIDE, ComputeBufferType.Structured, ComputeBufferMode.Immutable);
        IndirectArgsBuffer = new ComputeBuffer(1, INDIRECT_ARGS_STRIDE, ComputeBufferType.IndirectArguments);
        DrawTriangles = new ComputeBuffer(numSourceTriangles * settings.instancesPerChunk, DRAW_TRI_STRIDE, ComputeBufferType.Append);

        //set the buffer on the material
        cloudMaterial.SetBuffer(drawBufferGraphicsID, DrawTriangles);

        //set values on the compute shader

        //values for the source mesh
        cloudsCompute.SetInt("_NumSourceTriangles", numSourceTriangles);

        //values about mesh Generation
        cloudsCompute.SetVector("chunkCenter", new Vector4(chunkPosition.x, chunkPosition.y, chunkPosition.z, 0));
        cloudsCompute.SetFloat("startHeight", settings.cloudHeight);
        cloudsCompute.SetFloat("distanceBetweenPlanes", settings.distanceBetweenInstances);
        cloudsCompute.SetInt("numPlanesToGenerate", settings.instancesPerChunk);

        //set source buffers

        //generate data for sourceVertex and triangleBuffer buffer
        SourceVertex[] sourceVertices = new SourceVertex[sourceMesh.vertexCount];
        int[] sourceTris = sourceMesh.triangles;

        for (int i = 0; i < sourceVertices.Length; i++)
        {
            SourceVertex vertex = new SourceVertex
            {
                pos = sourceMesh.vertices[i]
            };
            sourceVertices[i] = vertex;
        }

        //set the data
        SourceVerts.SetData(sourceVertices);
        SourceTriangles.SetData(sourceTris);

        //pass to the GPU
        cloudsCompute.SetBuffer(cloudKernalID, "SourceVerts", SourceVerts);
        cloudsCompute.SetBuffer(cloudKernalID, "SourceTriangles", SourceTriangles);

        //set the draw buffer
        //set counter to 0 as it is an append buffer
        DrawTriangles.SetCounterValue(0);

        //pass to GPU
        cloudsCompute.SetBuffer(cloudKernalID, "DrawTriangles", DrawTriangles);

        //set the args buffer
        IndirectArgsBuffer.SetData(argsReset);

        //pass to GPU
        cloudsCompute.SetBuffer(cloudKernalID, "_IndirectArgsBuffer", IndirectArgsBuffer);

        //calculate the dispatchSize
        cloudsCompute.GetKernelThreadGroupSizes(cloudKernalID, out uint threadGroupSize, out _, out _);
        dispatchSize = Mathf.CeilToInt((float)(numSourceTriangles * settings.instancesPerChunk) / threadGroupSize);

        //set the state
        initialized = true;
    }

    //the call to actually run the compute shader
    void ExecuteComputeShader()
    {
        //dispatch the compute shader
        cloudsCompute.Dispatch(cloudKernalID, dispatchSize, 1, 1);
    }

    //public method so the manager can release resources in the class
    public void DisposeOfChunk()
    {
        DisposeOfComputeShaderResources();
    }

    //release all of the resources
    void DisposeOfComputeShaderResources()
    {
        if (initialized)
        {
            SourceVerts.Release();
            SourceTriangles.Release();
            IndirectArgsBuffer.Release();
            DrawTriangles.Release();
        }

        initialized = false;
    }

    //how to get the world space bounds for the chunk
    public void GenerateBounds()
    {
        float cloudWidth = (settings.instancesPerChunk - 1) * settings.distanceBetweenInstances;
        float centerHeight = settings.cloudHeight + cloudWidth / 2f;

        bounds = new Bounds(new Vector3(chunkPosition.x, centerHeight, chunkPosition.z), new Vector3(settings.CloudChunkWorldSize, cloudWidth, settings.CloudChunkWorldSize));
    }
}
