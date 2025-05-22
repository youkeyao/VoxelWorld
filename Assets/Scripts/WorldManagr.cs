using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldManagr : MonoBehaviour
{
    public class Chunk
    {
        public GameObject root;
        public List<VoxelObject> objects = new List<VoxelObject>();

        public Chunk(WorldManagr manager, Vector3Int offset)
        {
            root = new GameObject("Chunk" + offset);
            GameObject go = new GameObject("Terrain");
            go.transform.SetParent(root.transform);
            TerrainObject comp = go.AddComponent<TerrainObject>();
            comp.size = manager.size;
            comp.offset = offset;
            comp.voxelCulling = manager.voxelCulling;
            comp.perlinNoise = manager.perlinNoise;
            comp.Init();
            objects.Add(comp);
        }
    }

    public Vector3Int size = new Vector3Int(256, 256, 256);
    public int radius = 1;
    public ComputeShader voxelCulling;
    public ComputeShader perlinNoise;

    Vector3Int m_lastChunkId = Vector3Int.zero;
    Dictionary<Vector3Int, Chunk> m_chunks = new Dictionary<Vector3Int, Chunk>();

    void Start()
    {
        Vector3 cameraPos = Camera.main.transform.position;
        int currentChunkX = Mathf.FloorToInt(cameraPos.x / size.x);
        int currentChunkY = Mathf.FloorToInt(cameraPos.y / size.y);
        int currentChunkZ = Mathf.FloorToInt(cameraPos.z / size.z);
        Vector3Int nowChunkId = new Vector3Int(currentChunkX, currentChunkY, currentChunkZ);
        SetChunkState(nowChunkId, true);
    }

    void Update()
    {
        // Vector3 cameraPos = Camera.main.transform.position;
        // int currentChunkX = Mathf.FloorToInt(cameraPos.x / size.x);
        // int currentChunkY = Mathf.FloorToInt(cameraPos.y / size.y);
        // int currentChunkZ = Mathf.FloorToInt(cameraPos.z / size.z);
        // Vector3Int nowChunkId = new Vector3Int(currentChunkX, currentChunkY, currentChunkZ);

        // if (m_lastChunkId != nowChunkId)
        // {
        //     SetChunkState(m_lastChunkId, false);
        //     SetChunkState(nowChunkId, true);
        //     m_lastChunkId = nowChunkId;
        // }
    }

    void SetChunkState(Vector3Int chunkId, bool state)
    {
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    Vector3Int chunkCoord = new Vector3Int(chunkId.x + x, chunkId.y + y, chunkId.z + z);
                    if (!m_chunks.ContainsKey(chunkCoord))
                    {
                        m_chunks.Add(chunkCoord, new Chunk(this, chunkCoord * size));
                    }
                    m_chunks[chunkCoord].root.SetActive(state);
                }
            }
        }
    }
}
