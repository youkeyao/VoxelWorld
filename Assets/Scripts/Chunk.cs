using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk
{
    public GameObject root;
    public Vector3Int size = new Vector3Int(1, 1, 1);
    public Vector3Int offset = new Vector3Int(0, 0, 0);

    public ComputeBuffer voxels;

    protected ComputeBuffer m_voxelIndices;
    protected ComputeBuffer m_countBuffer;

    ComputeShader voxelCulling;

    Material m_material;
    int m_genIndicesKernel;


    public Chunk(WorldManagr manager, Vector3Int offset)
    {
        root = new GameObject("Chunk" + offset);
        root.transform.position = new Vector3(offset.x * size.x, offset.y * size.y, offset.z * size.z);
        this.size = manager.size;
        this.offset = offset;
        voxelCulling = manager.voxelCulling;

        Init();
    }

    ~Chunk()
    {
        Clear();
    }

    public void Clear()
    {
        voxels.Release();
        m_voxelIndices.Release();
        m_countBuffer.Release();
    }

    public void Init()
    {
        voxels = new ComputeBuffer(size.x * size.y * size.z, sizeof(uint));
        m_voxelIndices = new ComputeBuffer(size.x * size.y * size.z, sizeof(uint), ComputeBufferType.Append);
        m_countBuffer = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.Raw);

        m_material = new Material(Shader.Find("VoxelWorld/VoxelShader"));
        m_material.SetVector("_size", new Vector4(size.x, size.y, size.z, 0));
        m_material.SetVector("_offset", new Vector4(offset.x, offset.y, offset.z, 0));
        m_material.SetBuffer("_voxels", voxels);
        m_material.SetBuffer("_voxelIndices", m_voxelIndices);

        m_genIndicesKernel = voxelCulling.FindKernel("GenIndices");
    }

    public void OnRender()
    {
        m_voxelIndices.SetCounterValue(0);
        voxelCulling.SetVector("_size", new Vector4(size.x, size.y, size.z, 0));
        voxelCulling.SetBuffer(m_genIndicesKernel, "_voxels", voxels);
        voxelCulling.SetBuffer(m_genIndicesKernel, "_voxelIndices", m_voxelIndices);
        voxelCulling.Dispatch(m_genIndicesKernel, size.x / 8, size.y / 8, size.z / 8);
        ComputeBuffer.CopyCount(m_voxelIndices, m_countBuffer, 0);
        uint[] countData = new uint[1];
        m_countBuffer.GetData(countData);
        int visibleCount = (int)countData[0];

        m_material.SetPass(0);
        m_material.SetVector("_cameraPosition", Camera.main.transform.position);

        Graphics.DrawProceduralNow(MeshTopology.Points, visibleCount);
    }
}
