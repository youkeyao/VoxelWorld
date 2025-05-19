using UnityEngine;

class VoxelData : MonoBehaviour
{
    public enum VoxelType { SOLID, LIQUID }
    public static Vector3Int size = new Vector3Int(256, 256, 256);

    public VoxelType type = VoxelType.SOLID;
    public Shader voxelShader;

    public ComputeShader voxelCulling;

    protected ComputeBuffer m_voxels;
    protected ComputeBuffer m_voxelIndices;
    protected ComputeBuffer m_countBuffer;

    Material m_material;
    int m_genIndicesKernel;

    public void Start()
    {
        m_voxels = new ComputeBuffer(size.x * size.y * size.z, sizeof(uint));
        m_voxelIndices = new ComputeBuffer(size.x * size.y * size.z, sizeof(uint), ComputeBufferType.Append);
        m_countBuffer = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.Raw);

        m_material = new Material(voxelShader);
        m_material.SetVector("_size", new Vector4(size.x, size.y, size.z, 0));
        m_material.SetBuffer("_voxels", m_voxels);
        m_material.SetBuffer("_voxelIndices", m_voxelIndices);

        m_genIndicesKernel = voxelCulling.FindKernel("GenIndices");
        voxelCulling.SetBuffer(m_genIndicesKernel, "_voxels", m_voxels);
        voxelCulling.SetBuffer(m_genIndicesKernel, "_voxelIndices", m_voxelIndices);
        voxelCulling.SetVector("_size", new Vector4(size.x, size.y, size.z, 0));
    }

    void OnRenderObject()
    {
        m_voxelIndices.SetCounterValue(0);
        voxelCulling.Dispatch(m_genIndicesKernel, size.x / 8, size.y / 8, size.z / 8);
        ComputeBuffer.CopyCount(m_voxelIndices, m_countBuffer, 0);
        uint[] countData = new uint[1];
        m_countBuffer.GetData(countData);
        int visibleCount = (int)countData[0];
        Debug.Log(visibleCount);

        m_material.SetPass(0);
        m_material.SetVector("_cameraPosition", Camera.main.transform.position);
        // m_material.SetVector( "_chunkPosition", transform.position );

        // m_material.SetTexture( "_Sprite", sprite );
        // m_material.SetVector("_size", size);
        // m_material.SetMatrix("_worldMatrixTransform", transform.localToWorldMatrix);

        Graphics.DrawProceduralNow(MeshTopology.Points, visibleCount);
    }
}