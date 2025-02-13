using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class ChunkRenderer : MonoBehaviour
{
    private Camera camera;
    private Player player;
    private int world;

    private BinaryFormatter formatter;

    public Mesh waterMesh;
    public Material waterMaterial;

    private Array blocks;
    private int numberOfBlocks;
    private Mesh blockMesh;
    private List<Material> blockMaterials;
    private List<List<Matrix4x4>> blockMatrixLists;
    private int blockMatrixListCount;
    private List<Matrix4x4> currentBatch;
    private const int batchLimit = 1023;
    private int currentBatchCount = 0;

    private List<Item[,,]> chunksReadyToRender;
    private Item[,,] currentChunkToRender;
    private Item[,,] currentlyLoadingChunk;
    private bool currentChunkReadyToRender = false;
    private bool instanceDataLoaded = false;
    private Vector3 cameraForward;
    private Vector3 cameraPosition;
    private Vector3 blockPosition;
    private Vector3 displacement;
    private Matrix4x4 currentMatrix;

    void Start()
    {
        player = GameObject.Find("PlayerCapsule").GetComponent<Player>();
        world = PlayerPrefs.GetInt("world", 0);
        camera = GameObject.Find("MainCamera").GetComponent<Camera>();
        formatter = new BinaryFormatter();
        blocks = Enum.GetValues(typeof(Item));
        numberOfBlocks = blocks.Length;
        blockMesh = GenerateBlockMesh();
        blockMaterials = new List<Material>();
        chunksReadyToRender = new List<Item[,,]>();

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

        blockMatrixLists = new List<List<Matrix4x4>>();
        for (int i = 0; i < numberOfBlocks; i++)
            blockMatrixLists.Add(new List<Matrix4x4>());

        currentBatch = new List<Matrix4x4>(batchLimit);

        // WorldGenerator prepares initial chunks in Awake(), so we can go through them here in Start()
        (int x, int z) playerChunkCoords = player.GetChunkCoords();
        for (int globalChunkX = playerChunkCoords.x - GameData.RENDER_DISTANCE; globalChunkX <= playerChunkCoords.x + GameData.RENDER_DISTANCE; globalChunkX++)
        {
            for (int globalChunkZ = playerChunkCoords.z - GameData.RENDER_DISTANCE; globalChunkZ <= playerChunkCoords.z + GameData.RENDER_DISTANCE; globalChunkZ++)
            {
                int transformedChunkX = globalChunkX - playerChunkCoords.x + GameData.RENDER_DISTANCE; // Move origin to bottom left corner of render patch
                int transformedChunkZ = globalChunkZ - playerChunkCoords.z + GameData.RENDER_DISTANCE; //
                int chunkIndex = transformedChunkX*(2*GameData.RENDER_DISTANCE + 1) + transformedChunkZ; // NOTE: This is calculated based on the order of these loops, so be careful switching them
                String chunkFile = String.Format("{0}/worlds/world{1}/chunks/{2}_{3}.dat", Application.persistentDataPath, world, globalChunkX, globalChunkZ);
                if (true || File.Exists(chunkFile))
                {
                    using (FileStream file = File.Open(chunkFile, FileMode.Open))
                    {
                        //currentlyLoadingChunk = (Item[,,])formatter.Deserialize(file);
                        // chunksReadyToRender[chunkX,chunkZ] = new Item[GameData.CHUNK_SIZE, GameData.WORLD_HEIGHT_LIMIT, GameData.CHUNK_SIZE];
                        // for (int x = 0; x < GameData.CHUNK_SIZE; x++)
                        // {
                        //     for (int y = 0; y < GameData.WORLD_HEIGHT_LIMIT; y++)
                        //     {
                        //         for (int z = 0; z < GameData.CHUNK_SIZE; z++)
                        //         {
                        //             chunksReadyToRender[chunkX - playerChunkCoords.x + GameData.RENDER_DISTANCE, chunkZ - playerChunkCoords.z + GameData.RENDER_DISTANCE][x,y,z] = currentlyLoadingChunk[x,y,z];
                        //         }
                        //     }
                        // }
                        chunksReadyToRender.Add((Item[,,])formatter.Deserialize(file));
                    }
                }

                // Prepare chunk for rendering
                // if (!instanceDataLoaded)
                // {
                    for (int x = 0; x < GameData.CHUNK_SIZE; x++)
                    {
                        for (int z = 0; z < GameData.CHUNK_SIZE; z++)
                        {
                            for (int y = 0; y < GameData.WORLD_HEIGHT_LIMIT; y++)
                            {
                                Item block = chunksReadyToRender[chunkIndex][x,y,z];
                                if (block == Item.air)
                                    continue;

                                Vector3 blockPos = new Vector3(
                                    (globalChunkX*GameData.CHUNK_SIZE + x), y, (globalChunkZ*GameData.CHUNK_SIZE + z)
                                );

                                GameObject colliderObj = new GameObject();
                                colliderObj.layer = LayerMask.NameToLayer("BlockCollider");
                                colliderObj.transform.position = blockPos;
                                colliderObj.AddComponent<BoxCollider>();

                                float factor = block == Item.water ? 50 : 1;
                                Matrix4x4 matrix = Matrix4x4.TRS(blockPos, Quaternion.identity, factor*Vector3.one);
                                blockMatrixLists[(int)block].Add(matrix);
                            }
                        }
                    }
                //     instanceDataLoaded = true;
                // }
            }
        }
    }

    void Update()
    {
        // NOTE: I should probably set batch buffers aside beforehand, and only update them when needed.
        //       Whether or not most blocks are touching air doesn't change often, so why calculate every Update() call?
        //       This precludes frustum culling though.


        // cameraPosition = camera.transform.position;
        // cameraForward = camera.transform.forward;

        // NOTE: Item[,,] has size (1 * 16 * 16 * 128) * (2*RENDER_DISTANCE + 1)^2 ~ 2-3MB.
        //       It's probably worth storing this.
        //       Also, what I keep in memory can be less than the actual render distance, if space really matters (doubtful)
        //(int x, int z) playerChunkCoords = player.GetChunkCoords();
        // int chunkIndex = 0;
        // for (int chunkX = playerChunkCoords.x - GameData.RENDER_DISTANCE; chunkX <= playerChunkCoords.x + GameData.RENDER_DISTANCE; chunkX++)
        // {
        //     for (int chunkZ = playerChunkCoords.z - GameData.RENDER_DISTANCE ; chunkZ <= playerChunkCoords.z + GameData.RENDER_DISTANCE; chunkZ++)
        //     {
                //String chunkFile = String.Format("{0}/worlds/world{1}/chunks/{2}_{3}.dat", Application.persistentDataPath, world, chunkX, chunkZ);

                // NOTE: Instead of using File.Exists(), why not just ask WorldGenerator, since it should know?

                // if (File.Exists(chunkFile))
                // {
                //     using (FileStream file = File.Open(chunkFile, FileMode.Open))
                //         currentChunkToRender = (Item[,,])formatter.Deserialize(file);


                //currentChunkToRender = chunksReadyToRender[chunkIndex];

                

                // Render chunk
                for (int i = 0; i < numberOfBlocks; i++)
                {
                    blockMatrixListCount = blockMatrixLists[i].Count;
                    for (int j = 0; j < blockMatrixListCount; j++)
                    {
                        currentMatrix = blockMatrixLists[i][j];
                        blockPosition.x = currentMatrix[0,3];
                        blockPosition.y = currentMatrix[1,3];
                        blockPosition.z = currentMatrix[2,3];
                        int chunkIndex = getChunkIndexByBlockPosition(blockPosition.x, blockPosition.z);
                        currentChunkToRender = chunksReadyToRender[chunkIndex];
                        (int blockChunkPosX, int blockChunkPosY, int blockChunkPosZ) = getBlockPositionInChunk(blockPosition);

                        // Vector3 viewportPosition = camera.WorldToViewportPoint(blockPosition);
                        // bool notInFrustum = viewportPosition.z < 0 || viewportPosition.x < 0 || viewportPosition.x > 1 || viewportPosition.y < 0 || viewportPosition.y > 1;

                        // I just need the currently rendering chunk (convert blockPosition to chunkPosition to 
                        // calculate index into chunksReadyToRender)
                        //
                        // Once I have the chunk, I have to convert blockPosition to an index into

                        /*

                        Vector3 blockPos = new Vector3(
                            (chunkX*GameData.CHUNK_SIZE + x), y, (chunkZ*GameData.CHUNK_SIZE + z)
                        );

                        */

                        //(int chunkIndexX, int chunkIndexY, int chunkIndexZ) = getChunkIndexByBlockPosition(currentMatrix[0,3], currentMatrix[1,3], currentMatrix[2,3]);
                        // if (!notInFrustum)
                        // {
                            if (i == (int)Item.water)
                            {
                                currentBatch.Add(currentMatrix);
                                currentBatchCount++;
                            }
                            else
                            {
                                bool xTouchingAir = (blockChunkPosX > 0 && currentChunkToRender[blockChunkPosX - 1, blockChunkPosY, blockChunkPosZ] == Item.air) || (blockChunkPosX < GameData.CHUNK_SIZE - 1 && currentChunkToRender[blockChunkPosX + 1, blockChunkPosY, blockChunkPosZ] == Item.air);
                                bool yTouchingAir = (blockChunkPosY > 0 && currentChunkToRender[blockChunkPosX, blockChunkPosY - 1, blockChunkPosZ] == Item.air) || (blockChunkPosY < GameData.WORLD_HEIGHT_LIMIT - 1 && currentChunkToRender[blockChunkPosX, blockChunkPosY + 1, blockChunkPosZ] == Item.air);
                                bool zTouchingAir = (blockChunkPosZ > 0 && currentChunkToRender[blockChunkPosX, blockChunkPosY, blockChunkPosZ - 1] == Item.air) || (blockChunkPosZ < GameData.CHUNK_SIZE - 1 && currentChunkToRender[blockChunkPosX, blockChunkPosY, blockChunkPosZ + 1] == Item.air);
                                if (xTouchingAir || yTouchingAir || zTouchingAir)
                                {
                                    currentBatch.Add(currentMatrix);
                                    currentBatchCount++;
                                }
                            }
                       //}

                        if (currentBatchCount == batchLimit || j == blockMatrixListCount - 1)
                        {
                            if (i == (int)Item.water)
                            {
                                /*
                                    0 = +z
                                    1 = -y
                                    2 = -x
                                    3 = +x
                                    4 = +y
                                    5 = -z
                                */

                                for (int k = 0; k < 6; k++)
                                {

                                    Graphics.DrawMeshInstanced(waterMesh, k, blockMaterials[i], currentBatch);
                                }
                            }
                            else
                            {
                                Graphics.DrawMeshInstanced(blockMesh, 0, blockMaterials[i], currentBatch);
                            }
                            currentBatch.Clear();
                            currentBatchCount = 0;
                        }
                    }

                    //blockMatrixLists[i].Clear(); // Let's remove these Clear()s by manually storing the actual count and just overwriting
                }

                //chunkIndex++;
        //     }
        // }
    }

    // NOTE: This is indexing into a 1D array containing all chunks. Don't be mislead by the general name.
    int getChunkIndexByBlockPosition(float blockPosX, float blockPosZ)
    {
        int globalChunkX = (int)Math.Floor(blockPosX / GameData.CHUNK_SIZE);
        int globalChunkZ = (int)Math.Floor(blockPosZ / GameData.CHUNK_SIZE);
        int transformedChunkX = globalChunkX + GameData.RENDER_DISTANCE;
        int transformedChunkZ = globalChunkZ + GameData.RENDER_DISTANCE;
        return transformedChunkX*(2*GameData.RENDER_DISTANCE + 1) + transformedChunkZ;
    }

    (int, int, int) getBlockPositionInChunk(Vector3 blockPosition)
    {
        int blockPosX = (int)blockPosition.x;
        int blockPosZ = (int)blockPosition.z;
        int blockChunkPosX;
        int blockChunkPosZ;

        if (blockPosX >= 0 || ((-blockPosX) % 16 == 0))
            blockChunkPosX = blockPosX % GameData.CHUNK_SIZE;
        else
            blockChunkPosX = (blockPosX % GameData.CHUNK_SIZE) + GameData.CHUNK_SIZE;

        if (blockPosZ >= 0 || ((-blockPosZ) % 16 == 0))
            blockChunkPosZ = blockPosZ % GameData.CHUNK_SIZE;
        else
            blockChunkPosZ = (blockPosZ % GameData.CHUNK_SIZE) + GameData.CHUNK_SIZE;

        return (blockChunkPosX, (int)blockPosition.y, blockChunkPosZ);

        // NOTE: This might break for water, since its mesh size is different
        //          - Although, the scale shouldn't affect its position
    }

    Mesh GenerateBlockMesh()
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

    void SetFaceUVs(Vector2[] uvs, int startIndex, float cellWidth, float cellHeight, int atlasX, int atlasY)
    {
        float uStart = atlasX * cellWidth;
        float vStart = atlasY * cellHeight;

        uvs[startIndex + 0] = new Vector2(uStart, vStart);
        uvs[startIndex + 1] = new Vector2(uStart + cellWidth, vStart);
        uvs[startIndex + 2] = new Vector2(uStart + cellWidth, vStart + cellHeight);
        uvs[startIndex + 3] = new Vector2(uStart, vStart + cellHeight);
    }
}
