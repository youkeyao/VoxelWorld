Shader "VoxelWorld/VoxelShader" 
{
    Properties
    {
        _Sprite( "Sprite", 2D ) = "white" {}
        _Color( "Color", Color ) = ( 1, 1, 1, 1 )
        _Color1( "Color1", Color ) = ( 1, 1, 1, 1 )
        _Color2( "Color2", Color ) = ( 1, 1, 1, 1 )
        _Color3( "Color3", Color ) = ( 1, 1, 1, 1 )
        _Color4( "Color4", Color ) = ( 1, 1, 1, 1 )
        _Color5( "Color5", Color ) = ( 1, 1, 1, 1 )
        _Color6( "Color6", Color ) = ( 1, 1, 1, 1 )
        _Size( "Size", float ) = 1
    }


    SubShader
    {
        Tags{ "Queue" = "Geometry" "RenderType" = "Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma target 5.0

            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _Sprite;
            float4 _Color = float4( 1, 1, 1, 1 );
            float4 _Color1 = float4( 1, 1, 1, 1 );
            float4 _Color2 = float4( 1, 1, 1, 1 );
            float4 _Color3 = float4( 1, 1, 1, 1 );
            float4 _Color4 = float4( 1, 1, 1, 1 );
            float4 _Color5 = float4( 1, 1, 1, 1 );
            float4 _Color6 = float4( 1, 1, 1, 1 );

            int3 _size;
            int3 _offset;
            float4 _cameraPosition;

            matrix _worldMatrixTransform;

            StructuredBuffer<uint> _voxels;
            StructuredBuffer<uint> _voxelIndices;

            struct g2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2g
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
                int3 id : TEXCOORD1;
            };

            bool IsVoxelShow(int3 pos) {
                if (pos.x < 0 || pos.y < 0 || pos.z < 0 ||
                    pos.x >= _size.x || pos.y >= _size.y || pos.z >= _size.z) {
                    return false;
                }
                uint idx = pos.x + pos.y * _size.x + pos.z * _size.x * _size.y;
                return _voxels[idx] > 0;
            }

            v2g vert(uint id : SV_VertexID)
            {
                v2g o;

                id = _voxelIndices[id];
                int x = id % _size.x;
                int y = (id / _size.x) % _size.y;
                int z = id / (_size.x * _size.y);

                o.pos = float4(x + _offset.x, y + _offset.y, z + _offset.z, 1.0f);
                o.color = _Color;
                o.id = int3(x, y, z);

                return o;
            }

            [maxvertexcount(12)]
            void geom(point v2g p[1], inout TriangleStream<g2f> triStream)
            {
                float4 pos = p[0].pos;
                float4 shift;
                int3 id = p[0].id;
                int camSign;
                float halfS = 0.5;
                g2f pIn1, pIn2, pIn3, pIn4;

                pIn1.color = p[0].color;
                pIn1.uv = float2(0.0f, 0.0f);

                pIn2.color = p[0].color;
                pIn2.uv = float2(0.0f, 1.0f);

                pIn3.color = p[0].color;
                pIn3.uv = float2(1.0f, 0.0f);

                pIn4.color = p[0].color;
                pIn4.uv = float2(1.0f, 1.0f);

                // 右，左
                camSign = sign(_cameraPosition.x - pos.x);
                shift = float4(-camSign, 1, -camSign, 1);
                if (!IsVoxelShow(id + int3(camSign, 0, 0)))
                {
                    pIn1.pos = mul(UNITY_MATRIX_VP, pos + shift * float4(-halfS, -halfS, halfS, 0));
                    triStream.Append(pIn1);

                    pIn2.pos = mul(UNITY_MATRIX_VP, pos + shift * float4(-halfS, halfS, halfS, 0));
                    triStream.Append(pIn2);

                    pIn3.pos = mul(UNITY_MATRIX_VP, pos + shift * float4(-halfS, -halfS, -halfS, 0));
                    triStream.Append(pIn3);

                    pIn4.pos = mul(UNITY_MATRIX_VP, pos + shift * float4(-halfS, halfS, -halfS, 0));
                    triStream.Append(pIn4);

                    triStream.RestartStrip();
                }

                // 上，下
                camSign = sign(_cameraPosition.y - pos.y);
                shift = float4(1, -camSign, -camSign, 1);
                if (!IsVoxelShow(id + int3(0, camSign, 0)))
                {
                    pIn1.pos = mul(UNITY_MATRIX_VP, pos + shift * float4(-halfS, -halfS, halfS, 0));
                    triStream.Append(pIn1);

                    pIn2.pos = mul(UNITY_MATRIX_VP, pos + shift * float4(-halfS, -halfS, -halfS, 0));
                    triStream.Append(pIn2);

                    pIn3.pos = mul(UNITY_MATRIX_VP, pos + shift * float4(halfS, -halfS, halfS, 0));
                    triStream.Append(pIn3);

                    pIn4.pos = mul(UNITY_MATRIX_VP, pos + shift * float4(halfS, -halfS, -halfS, 0));
                    triStream.Append(pIn4);

                    triStream.RestartStrip();
                }

                // 前，后
                camSign = sign(_cameraPosition.z - pos.z);
                shift = float4(-camSign, 1, -camSign, 1);
                if (!IsVoxelShow(id + int3(0, 0, camSign)))
                {
                    pIn1.pos = mul(UNITY_MATRIX_VP, pos + shift * float4(-halfS, -halfS, -halfS, 0));
                    triStream.Append(pIn1);

                    pIn2.pos = mul(UNITY_MATRIX_VP, pos + shift * float4(-halfS, halfS, -halfS, 0));
                    triStream.Append(pIn2);

                    pIn3.pos = mul(UNITY_MATRIX_VP, pos + shift * float4(halfS, -halfS, -halfS, 0));
                    triStream.Append(pIn3);

                    pIn4.pos = mul(UNITY_MATRIX_VP, pos + shift * float4(halfS, halfS, -halfS, 0));
                    triStream.Append(pIn4);

                    triStream.RestartStrip();
                }
            }

            float4 frag(g2f i) : COLOR
            {
                return float4(i.uv.x, i.uv.y, 0, 1) * i.color;
            }

            ENDCG
        }
    }

    Fallback Off
}

