using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CloudChunkSettings
{
    //this is all baseMesh Generation settings
    public const int numberOfMeshSizes = 7;

    public static readonly int[] meshSizes = { 2, 4, 8, 16, 32, 64, 128 };

    [Header("Draw Distance"), Range(0f, 1000f)]
    public float maxCloudDistance = 1000f;

    [Header("Number of Chunks Per Side"), Range(1, 20)]
    public int chunksPerSide = 5;

    [Header("Cloud Mesh Resolution"), Range(0, numberOfMeshSizes - 1)]
    public int meshResolution = 0;

    [Header("Cloud Meshes Per Chunk"), Range(1, 50)]
    public int instancesPerChunk = 20;

    [Header("Cloud Volume"), Range(0.01f, 5.0f)]
    public float distanceBetweenInstances = 1.0f;

    [Header("Height of clouds"), Range(0.0f, 1000.0f)]
    public float cloudHeight = 250f;

    float _cloudChunkWorldSize = -1;

    public float CloudChunkWorldSize
    {
        get
        {
            if (_cloudChunkWorldSize < 0)
            {
                _cloudChunkWorldSize = Mathf.RoundToInt(maxCloudDistance / chunksPerSide);
            }

            return _cloudChunkWorldSize;
        }
    }

    int _vertsPerCloudChunkSide = -1;

    public int VertsPerCloudChunkSide
    {
        get
        {
            if (_vertsPerCloudChunkSide < 0)
            {
                _vertsPerCloudChunkSide = meshSizes[meshResolution];
            }

            return _vertsPerCloudChunkSide;
        }
    }
}
