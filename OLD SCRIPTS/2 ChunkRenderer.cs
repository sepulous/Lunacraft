using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.SceneManagement;

// TODO: Adjust render distance dynamically

/*

Water mesh data:
    0 = +z
    1 = -y
    2 = -x
    3 = +x
    4 = +y
    5 = -z

*/

public class ChunkRenderer : MonoBehaviour
{
    private ChunkGenerator chunkGenerator;
    private Player player;
    private GameObject camera;
    private int renderDistance; // Extent (not length) of patch around player
    private int maxChunkExtent;
    private List<List<Matrix4x4>>[,] chunkRenderingData;
    private Matrix4x4[][] renderingBatches;
    private bool[,] chunksReadyToRender;
    private bool[,] chunksBeingPrepared;
    private Array blocks;
    private List<Material> blockMaterials;
    private const int BATCH_LIMIT = 1023;
    private int numberOfBlocks;
    private int moon;
    private Mesh blockMesh;
    public Mesh waterMesh;
    public Material waterMaterial;
    private bool spawnedColliders = false;

    void Start()
    {
        renderDistance = 2;
        maxChunkExtent = (int)(GameData.MAX_RENDER_DISTANCE / GameData.CHUNK_SIZE);
        chunkGenerator = ChunkGenerator.Instance;
        player = GameObject.Find("PlayerCapsule").GetComponent<Player>();
        camera = GameObject.Find("MainCamera");
        moon = PlayerPrefs.GetInt("moon");
        //player = GameObject.Find("PlayerCapsule").GetComponent<Player>();
        blocks = Enum.GetValues(typeof(Block));
        numberOfBlocks = blocks.Length;
        blockMesh = GenerateBlockMesh();
        chunkRenderingData = new List<List<Matrix4x4>>[2*renderDistance + 1, 2*renderDistance + 1];
        for (int x = -renderDistance; x <= renderDistance; x++)
        {
            for (int z = -renderDistance; z <= renderDistance; z++)
            {
                chunkRenderingData[x + renderDistance, z + renderDistance] = new List<List<Matrix4x4>>();
                for (int i = 0; i < numberOfBlocks; i++)
                    chunkRenderingData[x + renderDistance, z + renderDistance].Add(new List<Matrix4x4>());
            }
        }

        chunksReadyToRender = new bool[2*renderDistance + 1, 2*renderDistance + 1];
        for (int x = -renderDistance; x <= renderDistance; x++)
        {
            for (int z = -renderDistance; z <= renderDistance; z++)
            {
                chunksReadyToRender[x + renderDistance, z + renderDistance] = false;
            }
        }

        chunksBeingPrepared = new bool[2*renderDistance + 1, 2*renderDistance + 1];
        for (int x = -renderDistance; x <= renderDistance; x++)
        {
            for (int z = -renderDistance; z <= renderDistance; z++)
            {
                chunksBeingPrepared[x + renderDistance, z + renderDistance] = false;
            }
        }

        renderingBatches = new Matrix4x4[(2*renderDistance + 1) * (2*renderDistance + 1)][];
        for (int i = 0; i < (2*renderDistance + 1) * (2*renderDistance + 1); i++)
            renderingBatches[i] = new Matrix4x4[BATCH_LIMIT];

        // Prepare materials
        blockMaterials = new List<Material>(numberOfBlocks);
        for (int i = 0; i < numberOfBlocks; i++)
        {
            String blockName = blocks.GetValue(i).ToString();
            String texturePath = String.Format("Textures/Blocks/{0}", blockName);
            Texture2D texture = Resources.Load<Texture2D>(texturePath);

            Material material;
            if (blockName == "water")
            {
                //material = new Material(Shader.Find("Shader Graphs/WaterShader"));
                material = waterMaterial;
            }
            else
            {
                material = new Material(Shader.Find("Shader Graphs/FlatShader"));
                material.SetTexture("_BaseTexture", texture);
            }
            material.enableInstancing = true;

            blockMaterials.Add(material);
        }

        // Prepare initial rendering data
        // for (int chunkX = -renderDistance; chunkX < renderDistance; chunkX++)
        // {
        //     for (int chunkZ = -renderDistance; chunkZ < renderDistance; chunkZ++)
        //     {
        //         // TODO: No file reading here. Let's load this work onto ChunkManager and have ChunkRenderer simply read what's in memory.
        //         //       Since ChunkManager can pre-generate the world and save it, ChunkRenderer can belong to the Game scene only
        //         //       FIRST: Let's make renderDistance correspond to blocks instead of chunks
        //         ChunkHelpers.GetChunkFromFile(chunksToRender[chunkX + renderDistance, chunkZ + renderDistance], moon, chunkX, chunkZ);
        //         for (int localBlockPosX = 0; localBlockPosX < GameData.CHUNK_SIZE; localBlockPosX++)
        //         {
        //             for (int localBlockPosY = 0; localBlockPosY < GameData.WORLD_HEIGHT_LIMIT; localBlockPosY++)
        //             {
        //                 for (int localBlockPosZ = 0; localBlockPosZ < GameData.CHUNK_SIZE; localBlockPosZ++)
        //                 {
        //                     Block block = chunksToRender[chunkX + renderDistance, chunkZ + renderDistance][localBlockPosX, localBlockPosY, localBlockPosZ];
        //                     if (block != Block.air)
        //                     {
        //                         Vector3 globalBlockPos = new Vector3(
        //                             (chunkX*GameData.CHUNK_SIZE + localBlockPosX), localBlockPosY, (chunkZ*GameData.CHUNK_SIZE + localBlockPosZ)
        //                         );

        //                         if (BlockIsTouchingAir(chunksToRender[chunkX + renderDistance, chunkZ + renderDistance], new Vector3(localBlockPosX, localBlockPosY, localBlockPosZ)))
        //                         {
        //                             float factor = block == Block.water ? 50 : 1;
        //                             Matrix4x4 matrix = Matrix4x4.TRS(globalBlockPos, Quaternion.identity, factor*Vector3.one);
        //                             chunkRenderingData[chunkX + renderDistance, chunkZ + renderDistance][(int)block].Add(matrix);
        //                         }
        //                     }
        //                 }
        //             }
        //         }
        //         chunksReadyToRender[chunkX + renderDistance, chunkZ + renderDistance] = true;
        //     }
        // }
    }

    void Update()
    {
        (int playerChunkCoordX, int playerChunkCoordZ) = player.GetChunkCoords();

        if (!spawnedColliders)
        {
            for (int chunkX = -renderDistance; chunkX <= renderDistance; chunkX++)
            {
                for (int chunkZ = -renderDistance; chunkZ <= renderDistance; chunkZ++)
                {
                    for (int localBlockPosX = 0; localBlockPosX < GameData.CHUNK_SIZE; localBlockPosX++)
                    {
                        for (int localBlockPosY = 0; localBlockPosY < GameData.WORLD_HEIGHT_LIMIT; localBlockPosY++)
                        {
                            for (int localBlockPosZ = 0; localBlockPosZ < GameData.CHUNK_SIZE; localBlockPosZ++)
                            {
                                Block block = chunkGenerator.GetLoadedChunks()[maxChunkExtent + chunkX, maxChunkExtent + chunkZ][localBlockPosX, localBlockPosY, localBlockPosZ];
                                if (block == Block.topsoil || block == Block.sand)
                                {
                                    GameObject colliderObj = new GameObject();
                                    colliderObj.layer = LayerMask.NameToLayer("BlockCollider");
                                    colliderObj.transform.position = new Vector3(
                                        (chunkX*GameData.CHUNK_SIZE + localBlockPosX), localBlockPosY, (chunkZ*GameData.CHUNK_SIZE + localBlockPosZ)
                                    );
                                    colliderObj.AddComponent<BoxCollider>();
                                }
                            }
                        }
                    }
                }
            }
            spawnedColliders = true;
        }

        if (ChunkGenerator.Instance.rendererInfo.shouldUpdate)
        {
            Direction updateDirection = ChunkGenerator.Instance.rendererInfo.updateDirection;
            if (updateDirection == Direction.POSITIVE_X)
            {
                for (int z = 0; z < 2*renderDistance + 1; z++)
                {
                    for (int x = 0; x < 2*renderDistance; x++)
                    {
                        chunksReadyToRender[x, z] = chunksReadyToRender[x+1, z];
                        chunksBeingPrepared[x, z] = chunksBeingPrepared[x+1, z];
                        chunkRenderingData[x, z] = chunkRenderingData[x+1, z];
                    }

                    chunksReadyToRender[2*renderDistance, z] = false;
                    chunksBeingPrepared[2*renderDistance, z] = false;
                }
            }
            else if (updateDirection == Direction.NEGATIVE_X)
            {
                for (int z = 0; z < 2*renderDistance + 1; z++)
                {
                    for (int x = 2*renderDistance; x > 0; x--)
                    {
                        chunksReadyToRender[x, z] = chunksReadyToRender[x-1, z];
                        chunksBeingPrepared[x, z] = chunksBeingPrepared[x-1, z];
                        chunkRenderingData[x, z] = chunkRenderingData[x-1, z];
                    }

                    chunksReadyToRender[0, z] = false;
                    chunksBeingPrepared[0, z] = false;
                }
            }
            else if (updateDirection == Direction.POSITIVE_Z)
            {
                for (int x = 0; x < 2*renderDistance + 1; x++)
                {
                    for (int z = 0; z < 2*renderDistance; z++)
                    {
                        chunksReadyToRender[x, z] = chunksReadyToRender[x, z+1];
                        chunksBeingPrepared[x, z] = chunksBeingPrepared[x, z+1];
                        chunkRenderingData[x, z] = chunkRenderingData[x, z+1];
                    }
                    chunksReadyToRender[x, 2*renderDistance] = false;
                    chunksBeingPrepared[x, 2*renderDistance] = false;
                }
            }
            else // NEGATIVE_Z
            {
                for (int x = 0; x < 2*renderDistance + 1; x++)
                {
                    for (int z = 2*renderDistance; z > 0; z--)
                    {
                        chunksReadyToRender[x, z] = chunksReadyToRender[x, z-1];
                        chunksBeingPrepared[x, z] = chunksBeingPrepared[x, z-1];
                        chunkRenderingData[x, z] = chunkRenderingData[x, z-1];
                    }
                    chunksReadyToRender[x, 0] = false;
                    chunksBeingPrepared[x, 0] = false;
                }
            }

            // for (int x = -renderDistance; x <= renderDistance; x++)
            // {
            //     for (int z = -renderDistance; z <= renderDistance; z++)
            //     {
            //         chunksReadyToRender[x + renderDistance, z + renderDistance] = false;
            //         chunksBeingPrepared[x + renderDistance, z + renderDistance] = false;
            //     }
            // }

            ChunkGenerator.Instance.rendererInfo.shouldUpdate = false;
        }

        int renderingIndex = 0;
        for (int chunkX = -renderDistance; chunkX <= renderDistance; chunkX++)
        {
            for (int chunkZ = -renderDistance; chunkZ <= renderDistance; chunkZ++)
            {
                if (!chunksReadyToRender[chunkX + renderDistance, chunkZ + renderDistance] && !chunksBeingPrepared[chunkX + renderDistance, chunkZ + renderDistance])
                {
                    StartCoroutine(PrepareChunkForRendering(chunkX, chunkZ, playerChunkCoordX, playerChunkCoordZ));
                }
                else
                {
                    StartCoroutine(RenderChunk(renderingIndex, chunkRenderingData[chunkX + renderDistance, chunkZ + renderDistance]));
                }
                renderingIndex++;
            }
        }
    }

    /*

    CURRENT RENDERING BUG
    ===========================================

    When the player moves to a new chunk in some direction, it's only the row in front of the player that doesn't carry forward.


    */

    private IEnumerator PrepareChunkForRendering(int chunkX, int chunkZ, int playerChunkCoordX, int playerChunkCoordZ)
    {
        chunksBeingPrepared[chunkX + renderDistance, chunkZ + renderDistance] = true;

        for (int i = 0; i < numberOfBlocks; i++)
            chunkRenderingData[chunkX + renderDistance, chunkZ + renderDistance][i].Clear();

        for (int localBlockPosX = 0; localBlockPosX < GameData.CHUNK_SIZE; localBlockPosX++)
        {
            for (int localBlockPosY = 0; localBlockPosY < GameData.WORLD_HEIGHT_LIMIT; localBlockPosY++)
            {
                for (int localBlockPosZ = 0; localBlockPosZ < GameData.CHUNK_SIZE; localBlockPosZ++)
                {
                    Vector3 globalBlockPos = new Vector3(
                        ((playerChunkCoordX + chunkX)*GameData.CHUNK_SIZE + localBlockPosX), localBlockPosY, ((playerChunkCoordZ + chunkZ)*GameData.CHUNK_SIZE + localBlockPosZ)
                    );

                    Block block = chunkGenerator.GetLoadedChunks()[maxChunkExtent + chunkX, maxChunkExtent + chunkZ][localBlockPosX, localBlockPosY, localBlockPosZ];
                    if (block != Block.air && BlockIsTouchingAir(chunkGenerator.GetLoadedChunks()[maxChunkExtent + chunkX, maxChunkExtent + chunkZ], new Vector3(localBlockPosX, localBlockPosY, localBlockPosZ)))
                    {
                        float factor = block == Block.water ? 50 : 1;
                        Matrix4x4 matrix = Matrix4x4.TRS(globalBlockPos, Quaternion.identity, factor*Vector3.one);
                        chunkRenderingData[chunkX + renderDistance, chunkZ + renderDistance][(int)block].Add(matrix);
                    }
                }
            }
            yield return null;
        }
        chunksBeingPrepared[chunkX + renderDistance, chunkZ + renderDistance] = false;
        chunksReadyToRender[chunkX + renderDistance, chunkZ + renderDistance] = true;
    }

    private IEnumerator RenderChunk(int renderingIndex, List<List<Matrix4x4>> chunkRenderingData)
    {
        Matrix4x4 currentMatrix;
        int currentRenderingBatchCount = 0;
        (int initialPlayerChunkCoordX, int initialPlayerChunkCoordZ) = player.GetChunkCoords();
        for (int i = 0; i < numberOfBlocks; i++)
        {
            int blockMatrixListCount = chunkRenderingData[i].Count;
            for (int j = 0; j < blockMatrixListCount; j++)
            {
                if (j < chunkRenderingData[i].Count)
                {
                    (int newPlayerChunkCoordX, int newPlayerChunkCoordZ) = player.GetChunkCoords();
                    currentMatrix = chunkRenderingData[i][j];
                    currentMatrix[0,3] += (newPlayerChunkCoordX - initialPlayerChunkCoordX) * GameData.CHUNK_SIZE;
                    currentMatrix[2,3] += (newPlayerChunkCoordZ - initialPlayerChunkCoordZ) * GameData.CHUNK_SIZE;
                    renderingBatches[renderingIndex][currentRenderingBatchCount++] = currentMatrix;
                }

                if (currentRenderingBatchCount == BATCH_LIMIT || j == blockMatrixListCount - 1)
                {
                    if (i == (int)Block.water)
                    {
                        // for (int k = 0; k < 6; k++)
                        // {
                            Graphics.DrawMeshInstanced(waterMesh, 4, blockMaterials[i], renderingBatches[renderingIndex], currentRenderingBatchCount);
                        //}
                    }
                    else
                    {
                        Graphics.DrawMeshInstanced(blockMesh, 0, blockMaterials[i], renderingBatches[renderingIndex], currentRenderingBatchCount);
                    }
                    currentRenderingBatchCount = 0;

                    yield return null; // Use custom condition instead?
                }
            }
        }
    }

    private bool BlockIsTouchingAir(Block[,,] chunk, Vector3 localBlockPos)
    {
        int localBlockPosX = (int)localBlockPos.x;
        int localBlockPosY = (int)localBlockPos.y;
        int localBlockPosZ = (int)localBlockPos.z;
        bool xTouchingAir = (localBlockPosX > 0 && chunk[localBlockPosX - 1, localBlockPosY, localBlockPosZ] == Block.air) || (localBlockPosX < GameData.CHUNK_SIZE - 1 && chunk[localBlockPosX + 1, localBlockPosY, localBlockPosZ] == Block.air);
        bool yTouchingAir = (localBlockPosY > 0 && chunk[localBlockPosX, localBlockPosY - 1, localBlockPosZ] == Block.air) || (localBlockPosY < GameData.WORLD_HEIGHT_LIMIT - 1 && chunk[localBlockPosX, localBlockPosY + 1, localBlockPosZ] == Block.air);
        bool zTouchingAir = (localBlockPosZ > 0 && chunk[localBlockPosX, localBlockPosY, localBlockPosZ - 1] == Block.air) || (localBlockPosZ < GameData.CHUNK_SIZE - 1 && chunk[localBlockPosX, localBlockPosY, localBlockPosZ + 1] == Block.air);
        return xTouchingAir || yTouchingAir || zTouchingAir;
    }

    private Mesh GenerateBlockMesh()
    {
        GameObject tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Mesh mesh = tempCube.GetComponent<MeshFilter>().mesh;

        Vector3[] vertices = {
            new Vector3(0.5f, 0.5f, -0.5f),
            new Vector3(0.5f, -0.5f, -0.5f),
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3(-0.5f, 0.5f, -0.5f),

            new Vector3(-0.5f, 0.5f, 0.5f),
            new Vector3(-0.5f, -0.5f, 0.5f),
            new Vector3(0.5f, -0.5f, 0.5f),
            new Vector3(0.5f, 0.5f, 0.5f),

            new Vector3(-0.5f, 0.5f, -0.5f),
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3(-0.5f, -0.5f, 0.5f),
            new Vector3(-0.5f, 0.5f, 0.5f),

            new Vector3(0.5f, 0.5f, -0.5f),
            new Vector3(-0.5f, 0.5f, -0.5f),
            new Vector3(-0.5f, 0.5f, 0.5f),
            new Vector3(0.5f, 0.5f, 0.5f),

            new Vector3(0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, 0.5f, -0.5f),
            new Vector3(0.5f, 0.5f, 0.5f),
            new Vector3(0.5f, -0.5f, 0.5f),

            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, -0.5f, 0.5f),
            new Vector3(-0.5f, -0.5f, 0.5f),
        };

        int[] triangles = {
            0, 1, 2,
            0, 2, 3,

            4, 5, 6,
            4, 6, 7,

            8, 9, 10,
            8, 10, 11,

            12, 13, 14,
            12, 14, 15,

            16, 17, 18,
            16, 18, 19,

            20, 21, 22,
            20, 22, 23
        };

        Vector2[] uvs = {
            new Vector2(0.75f, 0.33f),
            new Vector2(0.50f, 0.33f),
            new Vector2(0.50f, 0.67f),
            new Vector2(0.75f, 0.67f),
            new Vector2(0.00f, 0.67f),
            new Vector2(0.25f, 0.67f),
            new Vector2(0.25f, 0.33f),
            new Vector2(0.00f, 0.33f),
            new Vector2(0.25f, 0.00f),
            new Vector2(0.25f, 0.33f),
            new Vector2(0.50f, 0.33f),
            new Vector2(0.50f, 0.00f),
            new Vector2(1.00f, 0.33f),
            new Vector2(0.75f, 0.33f),
            new Vector2(0.75f, 0.67f),
            new Vector2(1.00f, 0.67f),
            new Vector2(0.25f, 0.67f),
            new Vector2(0.25f, 1.00f),
            new Vector2(0.50f, 1.00f),
            new Vector2(0.50f, 0.67f),
            new Vector2(0.50f, 0.33f),
            new Vector2(0.25f, 0.33f),
            new Vector2(0.25f, 0.67f),
            new Vector2(0.50f, 0.67f),
        };

        Vector3[] normals = CalculateNormals(vertices, triangles);

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.normals = normals;
        mesh.uv = uvs;

        Destroy(tempCube);

        return mesh;
    }

    private Vector3[] CalculateNormals(Vector3[] vertices, int[] triangles)
    {
        Vector3[] normals = new Vector3[vertices.Length];

        for (int i = 0; i < triangles.Length; i += 3)
        {
            // Get indices of the triangle's vertices
            int index0 = triangles[i];
            int index1 = triangles[i + 1];
            int index2 = triangles[i + 2];

            // Get the triangle's vertices
            Vector3 v0 = vertices[index0];
            Vector3 v1 = vertices[index1];
            Vector3 v2 = vertices[index2];

            // Calculate the face normal
            Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0).normalized;

            // Assign the same normal to all vertices of the triangle
            normals[index0] = normal;
            normals[index1] = normal;
            normals[index2] = normal;
        }

        return normals;
    }
}
