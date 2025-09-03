using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk
{
    const int JACOBI_ITERATIONS = 40;

    public GameObject root;
    public Vector3Int size = new Vector3Int(1, 1, 1);
    public Vector3Int offset = new Vector3Int(0, 0, 0);

    public ComputeBuffer voxels;
    public ComputeBuffer densities;
    public ComputeBuffer velocities;
    public ComputeBuffer velocitiesNew;
    public ComputeBuffer divergence;
    public ComputeBuffer pressure;
    public ComputeBuffer pressureNew;

    ComputeBuffer m_voxelIndices;
    ComputeBuffer m_countBuffer;

    ComputeShader m_voxelCulling;
    ComputeShader m_fluidSimulation;

    Material m_material;
    int m_genIndicesKernel;
    int m_advectionKernel;
    int m_applyForceKernel;
    int m_divergenceKernel;
    int m_pressureSolveKernel;
    int m_velocitiyUpdateKernel;

    public Chunk(WorldManagr manager, Vector3Int offset)
    {
        root = new GameObject("Chunk" + offset);
        root.transform.position = new Vector3(offset.x * size.x, offset.y * size.y, offset.z * size.z);
        this.size = manager.size;
        this.offset = offset;
        m_voxelCulling = manager.voxelCulling;
        m_fluidSimulation = manager.fluidSimulation;

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
        int totalGrids = size.x * size.y * size.z;
        voxels = new ComputeBuffer(totalGrids, sizeof(uint));
        densities = new ComputeBuffer(totalGrids, sizeof(float));
        velocities = new ComputeBuffer(totalGrids, sizeof(float) * 3);
        velocitiesNew = new ComputeBuffer(totalGrids, sizeof(float) * 3);
        divergence = new ComputeBuffer(totalGrids, sizeof(float));
        pressure = new ComputeBuffer(totalGrids, sizeof(float));
        pressureNew = new ComputeBuffer(totalGrids, sizeof(float));

        // Vector3[] zerosV = new Vector3[totalGrids];
        // float[] zerosF = new float[totalGrids];
        // for (int i = 0; i < totalGrids; i++) { zerosV[i] = Vector3.zero; zerosF[i] = 0f; }
        // velocities.SetData(zerosV);
        // velocitiesNew.SetData(zerosV);
        // divergence.SetData(zerosF);
        // pressure.SetData(zerosF);
        // pressureNew.SetData(zerosF);

        m_voxelIndices = new ComputeBuffer(totalGrids, sizeof(uint), ComputeBufferType.Append);
        m_countBuffer = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.Raw);

        m_material = new Material(Shader.Find("VoxelWorld/VoxelShader"));
        m_material.SetVector("_size", new Vector4(size.x, size.y, size.z, 0));
        m_material.SetVector("_offset", new Vector4(offset.x, offset.y, offset.z, 0));
        m_material.SetBuffer("_voxels", voxels);
        m_material.SetBuffer("_voxelIndices", m_voxelIndices);
        m_material.SetBuffer("_velocities", velocities);

        m_genIndicesKernel = m_voxelCulling.FindKernel("GenIndices");
        m_advectionKernel = m_fluidSimulation.FindKernel("Advection");
        m_applyForceKernel = m_fluidSimulation.FindKernel("ApplyForce");
        m_divergenceKernel = m_fluidSimulation.FindKernel("Divergence");
        m_pressureSolveKernel = m_fluidSimulation.FindKernel("PressureSolve");
        m_velocitiyUpdateKernel= m_fluidSimulation.FindKernel("VelocityUpdate");
    }

    public void OnSimulate()
    {
        m_fluidSimulation.SetFloat("_dt", Time.deltaTime);
        m_fluidSimulation.SetVector("_size", new Vector4(size.x, size.y, size.z, 0));
        m_fluidSimulation.SetVector("_offset", new Vector4(offset.x, offset.y, offset.z, 0));

        m_fluidSimulation.SetBuffer(m_advectionKernel, "_voxels", voxels);
        m_fluidSimulation.SetBuffer(m_advectionKernel, "_densities", densities);
        m_fluidSimulation.SetBuffer(m_advectionKernel, "_velocities", velocities);
        m_fluidSimulation.SetBuffer(m_advectionKernel, "_velocitiesNew", velocitiesNew);
        m_fluidSimulation.Dispatch(m_advectionKernel, size.x / 8, size.y / 8, size.z / 8);
        Swap(ref velocities, ref velocitiesNew);

        m_fluidSimulation.SetBuffer(m_applyForceKernel, "_voxels", voxels);
        m_fluidSimulation.SetBuffer(m_applyForceKernel, "_densities", densities);
        m_fluidSimulation.SetBuffer(m_applyForceKernel, "_velocities", velocities);
        m_fluidSimulation.Dispatch(m_applyForceKernel, size.x / 8, size.y / 8, size.z / 8);

        m_fluidSimulation.SetBuffer(m_divergenceKernel, "_voxels", voxels);
        m_fluidSimulation.SetBuffer(m_divergenceKernel, "_velocities", velocities);
        m_fluidSimulation.SetBuffer(m_divergenceKernel, "_divergence", divergence);
        m_fluidSimulation.Dispatch(m_divergenceKernel, size.x / 8, size.y / 8, size.z / 8);

        for (int i = 0; i < JACOBI_ITERATIONS; i++)
        {
            m_fluidSimulation.SetBuffer(m_pressureSolveKernel, "_divergence", divergence);
            m_fluidSimulation.SetBuffer(m_pressureSolveKernel, "_pressure", pressure);
            m_fluidSimulation.SetBuffer(m_pressureSolveKernel, "_pressureNew", pressureNew);
            m_fluidSimulation.Dispatch(m_pressureSolveKernel, size.x / 8, size.y / 8, size.z / 8);
            Swap(ref pressure, ref pressureNew);
        }

        m_fluidSimulation.SetBuffer(m_velocitiyUpdateKernel, "_velocities", velocities);
        m_fluidSimulation.SetBuffer(m_velocitiyUpdateKernel, "_pressure", pressure);
        m_fluidSimulation.Dispatch(m_velocitiyUpdateKernel, size.x / 8, size.y / 8, size.z / 8);
    }

    public void OnRender()
    {
        m_voxelIndices.SetCounterValue(0);
        m_voxelCulling.SetVector("_size", new Vector4(size.x, size.y, size.z, 0));
        m_voxelCulling.SetBuffer(m_genIndicesKernel, "_voxels", voxels);
        m_voxelCulling.SetBuffer(m_genIndicesKernel, "_voxelIndices", m_voxelIndices);
        m_voxelCulling.Dispatch(m_genIndicesKernel, size.x / 8, size.y / 8, size.z / 8);
        ComputeBuffer.CopyCount(m_voxelIndices, m_countBuffer, 0);
        uint[] countData = new uint[1];
        m_countBuffer.GetData(countData);
        int visibleCount = (int)countData[0];

        m_material.SetPass(0);
        m_material.SetVector("_cameraPosition", Camera.main.transform.position);

        Graphics.DrawProceduralNow(MeshTopology.Points, visibleCount);
    }

    void Swap(ref ComputeBuffer a, ref ComputeBuffer b)
    {
        var tmp = a; a = b; b = tmp;
    }
}
