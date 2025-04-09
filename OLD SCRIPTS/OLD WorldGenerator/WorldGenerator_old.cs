using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using UnityEngine.Rendering;
using System.Runtime.Serialization.Formatters.Binary;

/*
    Save file:
        * World time
        * Distance explored (squared kilometers)
        

    Chunk file:
        * Blocks
        * Mobs

    Player data:
        * Player position/orientation
        * Player inventory
        * Suit status + health


    Optimizations:
        * Project each block position onto the viewport and determine which 3 faces to render based on which quadrant
          it lies in
            - But I have to know where the player is facing to know which faces are visible

        * Try to make GPU instancing work with a single UV-wrapped material
            - I think I was originally using a cube mesh with six submeshes, so DrawMeshInstanced() expected six faces and
              didn't know it should apply the same material to all. What if using the single-submesh cube mesh fixes this?
              Performance gains could be enormous.

        * Find an efficient way to determine if a block is totally occluded by other blocks

        ALSO: Figure out if the Unity game preview and actual game FPS differ by some facts
            - Apparently it's about 1.5, not sure if it's really a constant factor though
*/

public class WorldGenerator_old : MonoBehaviour
{
    private Player player;
    private Camera camera;
    private int world;
    private ushort seed;
    private ushort xChunkGenSeed;
    private ushort zChunkGenSeed;

    private BinaryFormatter formatter;

    private Array blocks;
    private int numberOfBlocks;
    private Mesh blockMesh;
    //private List<List<Material>> blockMaterials;
    private List<Material> blockMaterials;
    private List<List<Matrix4x4>> blockMatrixLists;
    private List<Matrix4x4> currentBatch;
    private int currentBatchCount = 0;

    private int[,] meshLists = {
        {0, 1, 3, 4, 5},
        {1, 2, 3, 4, 5},
        {1, 2, 3, 4, 5},
        {0, 1, 2, 3, 5},
        {0, 1, 2, 3, 5},
        {0, 1, 2, 4, 5},
        {0, 1, 2, 4, 5},
        {0, 1, 3, 4, 5}
    };

    // Update() stuff
    private Vector3 cameraForward;
    private Vector3 cameraPosition;
    private Vector3 blockPosition;
    private Vector3 displacement;
    private Matrix4x4 currentMatrix;

    void Start()
    {
        blockMesh = GenerateBlockMesh();
        Debug.Log(blockMesh.subMeshCount);

        blocks = Enum.GetValues(typeof(Item));
        numberOfBlocks = blocks.Length;

        //blockMaterials = new List<List<Material>>();
        blockMaterials = new List<Material>();
        for (int i = 0; i < numberOfBlocks; i++)
        {
            String blockName = blocks.GetValue(i).ToString();
            //blockMaterials.Add(new List<Material>());
            //for (int j = 0; j < 6; j++)
            //{
                String texturePath = String.Format("Textures/Blocks/{0}/{1}", blockName, blockName);
                Texture2D texture = Resources.Load<Texture2D>(texturePath);
                texture.filterMode = FilterMode.Point;
                texture.alphaIsTransparency = true;

                Material material = new Material(Shader.Find("Standard"));
                material.mainTexture = texture;
                material.enableInstancing = true;
                material.SetFloat("_Smoothness", 0F);
                material.SetFloat("_Glossiness", 0F);
                material.SetFloat("_Metallic", 0F);
                material.SetFloat("_SpecularHighlights", 0F);

                blockMaterials.Add(material);
            //}
        }

        // These will be indexed the same way as blockMaterials so I don't have to
        // store the block ID and search each one; just index directly
        blockMatrixLists = new List<List<Matrix4x4>>();
        for (int i = 0; i < numberOfBlocks; i++)
            blockMatrixLists.Add(new List<Matrix4x4>());

        currentBatch = new List<Matrix4x4>();

        camera = GameObject.Find("MainCamera").GetComponent<Camera>();

        formatter = new BinaryFormatter();

        player = GameObject.Find("PlayerCapsule").GetComponent<Player>();
        world = PlayerPrefs.GetInt("world", 0);
        using (FileStream levelDataFile  = File.Open(String.Format("{0}/worlds/world{1}/level.dat", Application.persistentDataPath, world), FileMode.Open))
        {
            LevelData levelData = (LevelData)(new BinaryFormatter()).Deserialize(levelDataFile);
            seed = levelData.seed;
            xChunkGenSeed = (ushort)(seed & 0b1010101010101010);
            zChunkGenSeed = (ushort)(seed & 0b0101010101010101);
        }

        (int x, int z) playerChunkCoords = player.GetChunkCoords();
        for (int chunkX = playerChunkCoords.x - 3; chunkX <= playerChunkCoords.x + 3; chunkX++)
        {
            for (int chunkZ = playerChunkCoords.z - 3; chunkZ <= playerChunkCoords.z + 3; chunkZ++)
            {
                String chunkFile = String.Format("{0}/worlds/world{1}/chunks/{2}_{3}.dat", Application.persistentDataPath, world, chunkX, chunkZ);
                Item[,,] chunkData;

                // Load/create chunk
                // TODO: Remove false once generation algorithm is finished
                if (false && File.Exists(chunkFile)) // Load
                {
                    using (FileStream file = File.Open(chunkFile, FileMode.Open))
                        chunkData = (Item[,,])formatter.Deserialize(file);
                }
                else // Create
                {
                    chunkData = GenerateChunk(chunkX, chunkZ);
                    using (FileStream file = File.Create(chunkFile))
                        formatter.Serialize(file, chunkData);
                }

                // Prepare chunk for rendering
                for (int x = 0; x < GameData.CHUNK_SIZE; x++)
                {
                    for (int z = 0; z < GameData.CHUNK_SIZE; z++)
                    {
                        for (int y = 0; y < GameData.WORLD_HEIGHT_LIMIT; y++)
                        {
                            if (y > 63 && chunkData[x, y, z] != Item.air)
                            {
                                Vector3 blockPos = new Vector3(
                                    (chunkX*GameData.CHUNK_SIZE + x), y, (chunkZ*GameData.CHUNK_SIZE + z)
                                );

                                // GameObject colliderObj = new GameObject();
                                // colliderObj.transform.position = blockPos;
                                // BoxCollider boxCollider = colliderObj.AddComponent<BoxCollider>();
                                //boxCollider.size = new Vector3(2F, 2F, 2F);
                                //boxCollider.size = new Vector3(1, 1, 1);

                                Matrix4x4 matrix = Matrix4x4.TRS(blockPos, Quaternion.identity, Vector3.one);
                                blockMatrixLists[(byte)chunkData[x, y, z]].Add(matrix);
                            }
                        }
                    }
                }
            }
        }
    }

    Mesh GenerateBlockMesh()
    {
        GameObject tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Mesh mesh = tempCube.GetComponent<MeshFilter>().mesh;

        float size = 1F;
        Vector3[] vertices = {
            new Vector3(0, size, 0),
            new Vector3(0, 0, 0),
            new Vector3(size, size, 0),
            new Vector3(size, 0, 0),

            new Vector3(0, 0, size),
            new Vector3(size, 0, size),
            new Vector3(0, size, size),
            new Vector3(size, size, size),

            new Vector3(0, size, 0),
            new Vector3(size, size, 0),

            new Vector3(0, size, 0),
            new Vector3(0, size, size),

            new Vector3(size, size, 0),
            new Vector3(size, size, size),
        };

        int[] triangles = {
            0, 2, 1, // front
            1, 2, 3,
            4, 5, 6, // back
            5, 7, 6,
            6, 7, 8, //top
            7, 9 ,8, 
            1, 3, 4, //bottom
            3, 5, 4,
            1, 11,10,// left
            1, 4, 11,
            3, 12, 5,//right
            5, 12, 13
        };

        Vector2[] uvs = {
            new Vector2(0, 0.6667f),
            new Vector2(0.25f, 0.6667f),
            new Vector2(0, 0.3333f),
            new Vector2(0.25f, 0.3333f),

            new Vector2(0.5f, 0.6667f),
            new Vector2(0.5f, 0.3333f),
            new Vector2(0.75f, 0.6667f),
            new Vector2(0.75f, 0.3333f),

            new Vector2(1, 0.6667f),
            new Vector2(1, 0.3333f),

            new Vector2(0.25f, 1),
            new Vector2(0.5f, 1),

            new Vector2(0.25f, 0),
            new Vector2(0.5f, 0),
        };

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.Optimize();
        mesh.RecalculateNormals();

        return mesh;
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

    void Update()
    {
        // cameraForward = camera.transform.forward;
        // cameraPosition = camera.transform.position;

        // // Split the x-z plane into four cones to determine which face to skip rendering
        // // float angleXZ = Mathf.Atan2(cameraForward.z, cameraForward.x);
        // // angleXZ += 6.283F; // 2pi
        // // int meshIndex = (int)(angleXZ / 0.785F) % 8; // 0.785 ~ pi/4 radians

        // // Store currentBatch.Count instead of recomputing it over and over
        // int blockMatrixListsCount;
        // for (int i = 0; i < numberOfBlocks; i++)
        // {
        //     blockMatrixListsCount = blockMatrixLists[i].Count;
        //     for (int j = 0; j < blockMatrixListsCount; j++)
        //     {
        //         currentMatrix = blockMatrixLists[i][j];
        //         blockPosition.x = currentMatrix[0,3];
        //         blockPosition.y = currentMatrix[1,3];
        //         blockPosition.z = currentMatrix[2,3];
        //         displacement = blockPosition - cameraPosition;
        //         if (displacement.magnitude < 9 || Vector3.Dot(cameraForward, displacement.normalized) > 0.72F) // |θ| < 45*
        //         {
        //             currentBatch.Add(currentMatrix);
        //             currentBatchCount++;
        //         }

        //         if (currentBatchCount == 1023 || j == blockMatrixListsCount - 1)
        //         {
        //             // for (int k = 0; k < 5; k++)
        //             // {
        //                 //int subMesh = meshLists[meshIndex,k];
        //                 //Graphics.DrawMeshInstanced(blockMesh, subMesh, blockMaterials[i][subMesh], currentBatch);
        //             Graphics.DrawMeshInstanced(blockMesh, 0, blockMaterials[i], currentBatch);
        //             //}
        //             currentBatch.Clear();
        //             currentBatchCount = 0;
        //         }
        //     }
        // }
    }

    /*

    Octaves:
        Low - Smooth
        High - Noisy

        Lunacraft is ~3



    Ground Level: 64
        - Everything down is rock





    */

    Item[,,] GenerateChunk(int chunkX, int chunkZ)
    {
        Item[,,] chunk = new Item[GameData.CHUNK_SIZE, GameData.WORLD_HEIGHT_LIMIT, GameData.CHUNK_SIZE];

        float heightLimit;
        float frequency;
        float amplitude;
        float persistence = 0.8F;
        int octaves = 2;

        for (int x = 0; x < GameData.CHUNK_SIZE; x++)
        {
            for (int z = 0; z < GameData.CHUNK_SIZE; z++)
            {
                heightLimit = 0F;
                frequency = 0.3F;
                amplitude = 60F;
                for (int i = 0; i < octaves; i++)
                {
                    float xArg = (((x + chunkX*GameData.CHUNK_SIZE) + 0.5F + xChunkGenSeed) / 16F) * frequency;
                    float zArg = (((z + chunkZ*GameData.CHUNK_SIZE) + 0.5F + zChunkGenSeed) / 16F) * frequency;
                    heightLimit += Mathf.PerlinNoise(xArg, zArg) * amplitude;
                    frequency *= 2F;
                    amplitude *= persistence;
                }

                int smoothHeightLimit = (int)heightLimit;

                for (int y = 0; y < GameData.WORLD_HEIGHT_LIMIT; y++)
                {
                    if (y <= 50)
                    {
                        chunk[x, y, z] = Item.rock;
                    }
                    else if (y <= smoothHeightLimit)
                    {
                        if (y < (int)(0.8F * smoothHeightLimit))
                            chunk[x, y, z] = Item.rock;
                        else if (y < (int)(0.878F * smoothHeightLimit))
                            chunk[x, y, z] = Item.gravel;
                        else if (y < smoothHeightLimit - 1)
                            chunk[x, y, z] = Item.dirt;
                        else
                            chunk[x, y, z] = Item.topsoil;
                    }
                    else if (y <= 64)
                    {
                        chunk[x, y, z] = Item.water;
                    }
                    else
                    {
                        chunk[x, y, z] = Item.air;
                    }
                    
                }
            }
        }

        return chunk;
    }
}
