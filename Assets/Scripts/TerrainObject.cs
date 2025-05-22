using UnityEngine;

class TerrainObject : VoxelObject
{
    public ComputeShader perlinNoise;

    protected int m_genTerrainKernel;

    new void Start()
    {
        base.Start();
    }

    public override void Init()
    {
        base.Init();

        m_genTerrainKernel = perlinNoise.FindKernel("GenTerrain");
        perlinNoise.SetBuffer(m_genTerrainKernel, "_size", m_sizeBuffer);
        perlinNoise.SetBuffer(m_genTerrainKernel, "_offset", m_offsetBuffer);
        perlinNoise.SetBuffer(m_genTerrainKernel, "_voxels", m_voxels);
        perlinNoise.Dispatch(m_genTerrainKernel, size.x / 8, size.y / 8, size.z / 8);

        // uint[] terrainVoxels = new uint[size.x * size.y * size.z];
        // int size = 3;

        // for (int i=0; i<voxels.Length; i++) voxels[i] = 0;
        // for (int i=0; i<heights.Length; i++) heights[i] = 255;

        // while (size * 3 <= 256) size *= 3;

        // putSponge( voxels, size, 0, 0, 0 );

        // MapVoxels.SetData( voxels );
        // MapHeights.SetData( heights );

        // for (int i = 0; i < size.x * size.y * size.z; i++)
        // {
        //     int x = i % size.x;
        //     int y = (i / size.x) % size.y;
        //     int z = i / (size.x * size.y);
        //     if (y < 10)
        //     {
        //         terrainVoxels[i] = 1;
        //     }
        // }
        // m_voxels.SetData(terrainVoxels);
    }
}