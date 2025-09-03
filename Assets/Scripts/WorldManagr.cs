using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldManagr : MonoBehaviour
{
    public Vector3Int size = new Vector3Int(256, 256, 256);
    public int radius = 1;
    public ComputeShader voxelCulling;
    public ComputeShader perlinNoise;
    public ComputeShader fluidSimulation;

    int m_genTerrainKernel;
    int m_genFluidKernel;
    Vector3Int m_lastChunkId = Vector3Int.zero;
    Dictionary<Vector3Int, Chunk> m_chunks = new Dictionary<Vector3Int, Chunk>();

    void Start()
    {
        m_genTerrainKernel = perlinNoise.FindKernel("GenTerrain");
        m_genFluidKernel = fluidSimulation.FindKernel("GenFluid");

        Vector3 cameraPos = Camera.main.transform.position;
        int currentChunkX = Mathf.FloorToInt(cameraPos.x / size.x);
        int currentChunkY = Mathf.FloorToInt(cameraPos.y / size.y);
        int currentChunkZ = Mathf.FloorToInt(cameraPos.z / size.z);
        Vector3Int nowChunkId = new Vector3Int(currentChunkX, currentChunkY, currentChunkZ);
        SetChunkState(nowChunkId, true);
        m_lastChunkId = nowChunkId;
    }

    void OnDestroy()
    {
        foreach (var chunk in m_chunks)
        {
            chunk.Value.Clear();
            Destroy(chunk.Value.root);
        }
        m_chunks.Clear();
    }

    void Update()
    {
        Vector3 cameraPos = Camera.main.transform.position;
        int currentChunkX = Mathf.FloorToInt(cameraPos.x / size.x);
        int currentChunkY = Mathf.FloorToInt(cameraPos.y / size.y);
        int currentChunkZ = Mathf.FloorToInt(cameraPos.z / size.z);
        Vector3Int nowChunkId = new Vector3Int(currentChunkX, currentChunkY, currentChunkZ);

        if (m_lastChunkId != nowChunkId)
        {
            SetChunkState(m_lastChunkId, false);
            SetChunkState(nowChunkId, true);
            m_lastChunkId = nowChunkId;
        }

        OnSimulate();
    }

    void SetChunkState(Vector3Int chunkId, bool state)
    {
        for (int x = -radius; x <= radius; x++)
        {
            // for (int y = -radius; y <= radius; y++)
            // {
                for (int z = -radius; z <= radius; z++)
                {
                    Vector3Int chunkCoord = new Vector3Int(chunkId.x + x, 0, chunkId.z + z);
                    if (!m_chunks.ContainsKey(chunkCoord))
                    {
                        Chunk chunk = new Chunk(this, chunkCoord * size);
                        AddTerrain(chunk);
                        if (chunkCoord == Vector3Int.zero)
                            AddFluid(chunk);
                        m_chunks.Add(chunkCoord, chunk);
                    }
                    m_chunks[chunkCoord].root.SetActive(state);
                }
            // }
        }
    }

    void AddTerrain(Chunk chunk)
    {
        perlinNoise.SetVector("_size", new Vector4(size.x, size.y, size.z, 0));
        perlinNoise.SetVector("_offset", new Vector4(chunk.offset.x, chunk.offset.y, chunk.offset.z, 0));
        perlinNoise.SetBuffer(m_genTerrainKernel, "_voxels", chunk.voxels);
        perlinNoise.Dispatch(m_genTerrainKernel, size.x / 8, size.y / 8, size.z / 8);
    }

    void AddFluid(Chunk chunk)
    {
        fluidSimulation.SetVector("_size", new Vector4(size.x, size.y, size.z, 0));
        fluidSimulation.SetVector("_offset", new Vector4(chunk.offset.x, chunk.offset.y, chunk.offset.z, 0));
        fluidSimulation.SetBuffer(m_genFluidKernel, "_voxels", chunk.voxels);
        fluidSimulation.Dispatch(m_genFluidKernel, size.x / 8, size.y / 8, size.z / 8);
    }

    void OnSimulate()
    {
        for (int x = -radius; x <= radius; x++)
        {
            // for (int y = -radius; y <= radius; y++)
            // {
                for (int z = -radius; z <= radius; z++)
                {
                    Vector3Int chunkCoord = new Vector3Int(m_lastChunkId.x + x, 0, m_lastChunkId.z + z);
                    if (m_chunks[chunkCoord].root.activeSelf)
                        m_chunks[chunkCoord].OnSimulate();
                }
            // }
        }
    }

    void OnRenderObject()
    {
        for (int x = -radius; x <= radius; x++)
        {
            // for (int y = -radius; y <= radius; y++)
            // {
                for (int z = -radius; z <= radius; z++)
                {
                    Vector3Int chunkCoord = new Vector3Int(m_lastChunkId.x + x, 0, m_lastChunkId.z + z);
                    if (m_chunks[chunkCoord].root.activeSelf)
                        m_chunks[chunkCoord].OnRender();
                }
            // }
        }
    }
}
