using UnityEngine;

public class VoxelObject : MonoBehaviour
{
    public enum VoxelType { SOLID, LIQUID }

    public VoxelType type = VoxelType.SOLID;
    public ComputeShader voxelCulling;

    public Vector3Int size = new Vector3Int(1, 1, 1);
    public Vector3Int offset = new Vector3Int(0, 0, 0);

    protected ComputeBuffer m_voxels;
    protected ComputeBuffer m_voxelIndices;
    protected ComputeBuffer m_sizeBuffer;
    protected ComputeBuffer m_offsetBuffer;
    protected ComputeBuffer m_countBuffer;

    Material m_material;
    int m_genIndicesKernel;

    public void Start()
    {
        Init();
    }

    void OnDestroy()
    {
        m_voxels.Release();
        m_voxelIndices.Release();
        m_sizeBuffer.Release();
        m_offsetBuffer.Release();
        m_countBuffer.Release();
    }

    public virtual void Init()
    {
        m_voxels = new ComputeBuffer(size.x * size.y * size.z, sizeof(uint));
        m_voxelIndices = new ComputeBuffer(size.x * size.y * size.z, sizeof(uint), ComputeBufferType.Append);
        m_sizeBuffer = new ComputeBuffer(3, sizeof(uint));
        m_offsetBuffer = new ComputeBuffer(3, sizeof(uint));
        m_countBuffer = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.Raw);

        m_sizeBuffer.SetData(new int[] { size.x, size.y, size.z });
        m_offsetBuffer.SetData(new int[] { offset.x, offset.y, offset.z });

        m_material = new Material(Shader.Find("VoxelWorld/VoxelShader"));
        m_material.SetVector("_size", new Vector4(size.x, size.y, size.z, 0));
        m_material.SetVector("_offset", new Vector4(offset.x, offset.y, offset.z, 0));
        m_material.SetBuffer("_voxels", m_voxels);
        m_material.SetBuffer("_voxelIndices", m_voxelIndices);

        m_genIndicesKernel = voxelCulling.FindKernel("GenIndices");
    }

    void OnRenderObject()
    {
        m_voxelIndices.SetCounterValue(0);
        voxelCulling.SetBuffer(m_genIndicesKernel, "_size", m_sizeBuffer);
        voxelCulling.SetBuffer(m_genIndicesKernel, "_voxels", m_voxels);
        voxelCulling.SetBuffer(m_genIndicesKernel, "_voxelIndices", m_voxelIndices);
        voxelCulling.Dispatch(m_genIndicesKernel, size.x / 8, size.y / 8, size.z / 8);
        ComputeBuffer.CopyCount(m_voxelIndices, m_countBuffer, 0);
        uint[] countData = new uint[1];
        m_countBuffer.GetData(countData);
        int visibleCount = (int)countData[0];

        m_material.SetPass(0);
        m_material.SetVector("_cameraPosition", Camera.main.transform.position);
        // m_material.SetVector( "_chunkPosition", transform.position );

        // m_material.SetTexture( "_Sprite", sprite );
        // m_material.SetVector("_size", size);
        // m_material.SetMatrix("_worldMatrixTransform", transform.localToWorldMatrix);

        Graphics.DrawProceduralNow(MeshTopology.Points, visibleCount);
    }
}