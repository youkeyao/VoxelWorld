using UnityEngine;

class TerrainObject : VoxelData
{
    public ComputeShader perlinGen;

    void Start()
    {
        base.Start();

        // perlinGen.SetBuffer(perlinGen.FindKernel( "PerlinMapGen" ), "MapVoxels", voxelData.voxels);

        uint[] terrainVoxels = new uint[size.x * size.y * size.z];
        // int size = 3;

        // for (int i=0; i<voxels.Length; i++) voxels[i] = 0;
        // for (int i=0; i<heights.Length; i++) heights[i] = 255;

        // while (size * 3 <= 256) size *= 3;

        // putSponge( voxels, size, 0, 0, 0 );

        // MapVoxels.SetData( voxels );
        // MapHeights.SetData( heights );

        for (int i = 0; i < size.x * size.y * size.z; i++)
        {
            int x = i % size.x;
            int y = (i / size.x) % size.y;
            int z = i / (size.x * size.y);
            if (y < 10)
            {
                terrainVoxels[i] = 1;
            }
        }
        m_voxels.SetData(terrainVoxels);
    }
}