using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

enum XZDirection
{
    POSITIVE_Z,
    NEGATIVE_Z,
    POSITIVE_X,
    NEGATIVE_X
}

class RowRenderTask
{
    public bool finished;
    public XZDirection direction;
    public int globalCenterCoordX;
    public int globalCenterCoordZ;
}

public class ChunkManager : MonoBehaviour
{
    private Player player;
    private GameObject camera;
    private int playerChunkX;
    private int playerChunkZ;
    private int moon;
    private ulong seed;
    private int renderDistance;

    private List<Material> blockMaterials;
    private Array blocks;
    private int numberOfBlocks;
    private Mesh blockMesh;

    public Material waterMaterial;
    public Material sulphurCrystalMaterial;
    public Material blueCrystalMaterial;
    public Material boronCrystalMaterial;
    public Material lightMaterial;

    private Options options;

    private Queue<RowRenderTask> rowRenderTasks;
    private RowRenderTask currentRowRenderTask;
    private float chunkGenerationRate = 0;

    void Start()
    {
        rowRenderTasks = new Queue<RowRenderTask>();

        renderDistance = OptionsManager.GetCurrentOptions().renderDistance;

        options = OptionsManager.GetCurrentOptions();
        moon = PlayerPrefs.GetInt("moon");
        player = GameObject.Find("Player").GetComponent<Player>();
        camera = GameObject.Find("PlayerCamera");
        (playerChunkX, playerChunkZ) = player.GetChunkCoords();
        blockMesh = GenerateBlockMesh();
        blocks = Enum.GetValues(typeof(BlockID));
        numberOfBlocks = blocks.Length;

        string moonDataFile = string.Format("{0}/moons/moon{1}/moon.dat", Application.persistentDataPath, moon);
        MoonData moonData;
        using (FileStream file = File.Open(moonDataFile, FileMode.Open))
            moonData = (MoonData)(new BinaryFormatter()).Deserialize(file);
        seed = moonData.seed;

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
                material = new Material(Shader.Find("Custom/WaterShader"));
            }
            else if (blockName.EndsWith("crystal"))
            {
                material = new Material(Shader.Find("Custom/CrystalShader"));
                material.SetTexture("_MainTex", texture);
            }
            else if (blockName == "light")
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                material.SetTexture("_BaseMap", texture);
            }
            else
            {
                material = new Material(Shader.Find("Custom/OpaqueShader"));
                material.SetTexture("_BaseTexture", texture);
            }
            material.enableInstancing = true;

            blockMaterials.Add(material);
        }
    }

    void Update()
    {
        int newRenderDistance = OptionsManager.GetCurrentOptions().renderDistance;
        if (newRenderDistance < renderDistance)
        {
            GameObject[] chunkObjects = GameObject.FindGameObjectsWithTag("Chunk");
            for (int i = 0; i < chunkObjects.Length; i++)
            {
                GameObject chunkObj = chunkObjects[i];
                ChunkData chunkData = chunkObj.GetComponent<ChunkData>();
                if (chunkData.globalPosX > playerChunkX + newRenderDistance + 1 || chunkData.globalPosX < playerChunkX - newRenderDistance - 1
                 || chunkData.globalPosZ > playerChunkZ + newRenderDistance + 1 || chunkData.globalPosZ < playerChunkZ - newRenderDistance - 1)
                {
                    ChunkHelpers.SaveChunkToFile(chunkData.blocks, moon, chunkData.globalPosX, chunkData.globalPosZ);
                    Destroy(chunkObj);
                }
                else if (chunkData.globalPosX == playerChunkX + newRenderDistance + 1 || chunkData.globalPosX == playerChunkX - newRenderDistance - 1
                      || chunkData.globalPosZ == playerChunkZ + newRenderDistance + 1 || chunkData.globalPosZ == playerChunkZ - newRenderDistance - 1)
                {
                    Transform[] childBlocks = new Transform[chunkObj.transform.childCount];
                    for (int j = 0; j < chunkObj.transform.childCount; j++)
                        childBlocks[j] = chunkObj.transform.GetChild(j);

                    foreach (Transform childBlock in childBlocks)
                        Destroy(childBlock.gameObject);

                    ChunkHelpers.SaveChunkToFile(chunkData.blocks, moon, chunkData.globalPosX, chunkData.globalPosZ);
                }
            }
        }
        else if (newRenderDistance > renderDistance)
        {
            rowRenderTasks.Clear();
            StartCoroutine(ExpandRenderDistance(renderDistance, newRenderDistance));
        }
        renderDistance = newRenderDistance;

        (int newPlayerChunkX, int newPlayerChunkZ) = player.GetChunkCoords();

        // Block destruction
        if (player.DestroyedBlock())
        {
            // Update chunk data
            GameObject selectedBlock = player.GetSelectedBlock();
            BlockData selectedBlockData = selectedBlock.GetComponent<BlockData>();
            ChunkData selectedBlockChunkData = selectedBlock.transform.parent.GetComponent<ChunkData>();
            selectedBlockChunkData.blocks[selectedBlockData.localPosX, selectedBlockData.localPosY, selectedBlockData.localPosZ] = BlockID.air;

            // Add neighboring blocks
            int selectedGlobalPosX = (int)selectedBlock.transform.position.x;
            int selectedGlobalPosY = (int)selectedBlock.transform.position.y;
            int selectedGlobalPosZ = (int)selectedBlock.transform.position.z;
            Vector3[] neighborGlobalPositions = {
                new Vector3(selectedGlobalPosX - 1, selectedGlobalPosY, selectedGlobalPosZ),
                new Vector3(selectedGlobalPosX, selectedGlobalPosY - 1, selectedGlobalPosZ),
                new Vector3(selectedGlobalPosX, selectedGlobalPosY, selectedGlobalPosZ - 1),
                new Vector3(selectedGlobalPosX + 1, selectedGlobalPosY, selectedGlobalPosZ),
                new Vector3(selectedGlobalPosX, selectedGlobalPosY + 1, selectedGlobalPosZ),
                new Vector3(selectedGlobalPosX, selectedGlobalPosY, selectedGlobalPosZ + 1)
            };
            foreach (Vector3 neighborPos in neighborGlobalPositions)
            {
                Collider[] hits = Physics.OverlapBox(neighborPos, new Vector3(0.1F, 0.1F, 0.1F));
                if (hits.Length == 0) // Neighbor of destroyed block doesn't already exist
                {
                    // Find chunk
                    ChunkData neighborChunkData = selectedBlockChunkData; // Have to initialize this to something because C# is fucking retarded
                    int neighborChunkX = Mathf.FloorToInt(neighborPos.x / GameData.CHUNK_SIZE);
                    int neighborChunkZ = Mathf.FloorToInt(neighborPos.z / GameData.CHUNK_SIZE);
                    foreach (GameObject chunk in GameObject.FindGameObjectsWithTag("Chunk"))
                    {
                        ChunkData chunkData = chunk.GetComponent<ChunkData>();
                        if (chunkData.globalPosX == neighborChunkX && chunkData.globalPosZ == neighborChunkZ)
                        {
                            neighborChunkData = chunkData;
                            break;
                        }
                    }

                    // SpawnBlock(BlockID block, GameObject parentChunk, Vector3 localBlockPosition, Vector3 globalChunkPosition)
                    // Get local position to index into chunk
                    Vector3 neighborLocalPos = ChunkHelpers.GetLocalBlockPos(neighborPos);

                    // Spawn block
                    BlockID neighborBlock = neighborChunkData.blocks[(int)neighborLocalPos.x, (int)neighborLocalPos.y, (int)neighborLocalPos.z];
                    if (neighborBlock != BlockID.air)
                        SpawnBlock(neighborBlock, neighborChunkData.gameObject, neighborLocalPos, new Vector3(neighborChunkData.globalPosX, 0, neighborChunkData.globalPosZ));
                }
            }

            // Remove block object
            Destroy(selectedBlock);
            player.ResetDestroyedBlock();
        }

        // Block placing
        if (player.PlacedBlock())
        {
            RaycastHit hit;
            if (Physics.Raycast(camera.transform.position, camera.transform.forward, out hit))
            {
                Vector3 selectedBlockPos = player.GetSelectedBlock().transform.position;
                Vector3 faceNormal = hit.normal;
                Vector3 newBlockPos = selectedBlockPos + faceNormal;

                GameObject blockObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                blockObj.hideFlags = HideFlags.HideInHierarchy;
                blockObj.layer = LayerMask.NameToLayer("Block");
                blockObj.GetComponent<MeshFilter>().mesh = blockMesh;
                blockObj.GetComponent<Renderer>().material = blockMaterials[(int)BlockID.light];
                blockObj.GetComponent<BoxCollider>().size = new Vector3(0.95F, 0.95F, 0.95F);
                blockObj.transform.position = new Vector3(newBlockPos.x, newBlockPos.y, newBlockPos.z);

                BlockData blockData = blockObj.AddComponent<BlockData>();
                blockData.blockID = BlockID.light;
                Vector3 localBlockPos = ChunkHelpers.GetLocalBlockPos(blockObj.transform.position);
                blockData.localPosX = (int)localBlockPos.x;
                blockData.localPosY = (int)localBlockPos.y;
                blockData.localPosZ = (int)localBlockPos.z;

                Light light = blockObj.AddComponent<Light>();
                light.type = LightType.Point;

                // Set parent
                (int blockChunkX, int blockChunkZ) = ChunkHelpers.GetChunkCoordsByBlockPos(blockObj.transform.position);
                foreach (GameObject chunk in GameObject.FindGameObjectsWithTag("Chunk"))
                {
                    ChunkData chunkData = chunk.GetComponent<ChunkData>();
                    if (chunkData.globalPosX == blockChunkX && chunkData.globalPosZ == blockChunkZ)
                    {
                        blockObj.transform.parent = chunk.transform;
                        chunkData.blocks[blockData.localPosX, blockData.localPosY, blockData.localPosZ] = BlockID.light;
                        break;
                    }
                }

                // IDEA: Maybe setting a bool in Player and having ChunkManager access the selected block isn't a good move, since there's
                //       no guarantee their Update()s will be synchronized. Maybe Player should define the GameObject immediately, and
                //       ChunkManager can access and reset that instead.

                player.ResetPlacedBlock();
            }
        }

        if (newPlayerChunkX != playerChunkX)
        {
            RowRenderTask task = new RowRenderTask();
            task.finished = false;
            task.direction = (newPlayerChunkX - playerChunkX == 1) ? XZDirection.POSITIVE_X : XZDirection.NEGATIVE_X;
            task.globalCenterCoordX = (task.direction == XZDirection.POSITIVE_X) ? newPlayerChunkX + renderDistance : newPlayerChunkX - renderDistance;
            task.globalCenterCoordZ = playerChunkZ;
            rowRenderTasks.Enqueue(task);
            playerChunkX = newPlayerChunkX;
        }

        if (newPlayerChunkZ != playerChunkZ)
        {
            RowRenderTask task = new RowRenderTask();
            task.finished = false;
            task.direction = (newPlayerChunkZ - playerChunkZ == 1) ? XZDirection.POSITIVE_Z : XZDirection.NEGATIVE_Z;
            task.globalCenterCoordX = playerChunkX;
            task.globalCenterCoordZ = (task.direction == XZDirection.POSITIVE_Z) ? newPlayerChunkZ + renderDistance : newPlayerChunkZ - renderDistance;
            rowRenderTasks.Enqueue(task);
            playerChunkZ = newPlayerChunkZ;
        }

        if ((currentRowRenderTask == null || currentRowRenderTask.finished) && rowRenderTasks.Count > 0)
        {
            currentRowRenderTask = rowRenderTasks.Dequeue();
            UnloadDistantChunks(currentRowRenderTask.direction, newPlayerChunkX, newPlayerChunkZ);
            StartCoroutine(RenderNewRow());
        }
    }

    // Created a new function for this because RenderNewRow() isn't well-suited
    private IEnumerator ExpandRenderDistance(int oldRenderDistance, int newRenderDistance)
    {
        // Create and prepare chunk objects
        for (int globalChunkX = playerChunkX - newRenderDistance - 1; globalChunkX <= playerChunkX + newRenderDistance + 1; globalChunkX++)
        {
            for (int globalChunkZ = playerChunkZ - newRenderDistance - 1; globalChunkZ <= playerChunkZ + newRenderDistance + 1; globalChunkZ++)
            {
                if (globalChunkX < playerChunkX - oldRenderDistance - 1 || globalChunkX > playerChunkX + oldRenderDistance + 1
                 || globalChunkZ < playerChunkZ - oldRenderDistance - 1 || globalChunkZ > playerChunkZ + oldRenderDistance + 1)
                {
                    GameObject chunk = new GameObject("Chunk");
                    chunk.tag = "Chunk";
                    ChunkData chunkData = chunk.AddComponent<ChunkData>();
                    chunkData.globalPosX = globalChunkX;
                    chunkData.globalPosZ = globalChunkZ;
                    chunkData.blocks = new BlockID[GameData.CHUNK_SIZE, GameData.WORLD_HEIGHT_LIMIT, GameData.CHUNK_SIZE];
                    if (!ChunkHelpers.ChunkFileExists(moon, globalChunkX, globalChunkZ))
                    {
                        ChunkHelpers.GenerateChunk(chunkData.blocks, globalChunkX, globalChunkZ, seed);
                        ChunkHelpers.SaveChunkToFile(chunkData.blocks, moon, globalChunkX, globalChunkZ);
                    }
                    else
                    {
                        ChunkHelpers.GetChunkFromFile(chunkData.blocks, moon, globalChunkX, globalChunkZ);
                    }
                }
            }
        }

        // Render
        GameObject[] chunkObjects = GameObject.FindGameObjectsWithTag("Chunk");
        ChunkData[] adjacentChunkData;
        foreach (GameObject chunkObj in chunkObjects)
        {
            ChunkData chunkData = chunkObj.GetComponent<ChunkData>();
            if (
                Mathf.Abs(chunkData.globalPosX - playerChunkX) < newRenderDistance + 1 && Mathf.Abs(chunkData.globalPosZ - playerChunkZ) < newRenderDistance + 1
             && (Mathf.Abs(chunkData.globalPosX - playerChunkX) > oldRenderDistance || Mathf.Abs(chunkData.globalPosZ - playerChunkZ) > oldRenderDistance))
            {
                adjacentChunkData = ChunkHelpers.GetAdjacentChunkData(chunkData.globalPosX, chunkData.globalPosZ);
                Vector3 globalChunkPosition = new Vector3(chunkData.globalPosX, 0, chunkData.globalPosZ);
                for (int localBlockX = 0; localBlockX < GameData.CHUNK_SIZE; localBlockX++)
                {
                    for (int localBlockY = 0; localBlockY < GameData.WORLD_HEIGHT_LIMIT; localBlockY++)
                    {
                        for (int localBlockZ = 0; localBlockZ < GameData.CHUNK_SIZE; localBlockZ++)
                        {
                            BlockID block = chunkData.blocks[localBlockX, localBlockY, localBlockZ];
                            Vector3 localBlockPosition = new Vector3(localBlockX, localBlockY, localBlockZ);
                            if (block != BlockID.air && ChunkHelpers.BlockShouldBeRendered(block, adjacentChunkData, chunkData, localBlockPosition))
                            {
                                SpawnBlock(block, chunkObj, localBlockPosition, globalChunkPosition);
                            }
                        }
                    }
                    yield return null;
                }
            }
        }
    }

    private IEnumerator RenderNewRow()
    {
        var stopWatch = new System.Diagnostics.Stopwatch();
        stopWatch.Start();

        XZDirection moveDirection = currentRowRenderTask.direction;
        if (moveDirection == XZDirection.POSITIVE_X || moveDirection == XZDirection.NEGATIVE_X) // Moving along x axis
        {
            int globalChunkX = currentRowRenderTask.globalCenterCoordX;
            int globalCenterCoordZ = currentRowRenderTask.globalCenterCoordZ;

            int outerChunkX = (moveDirection == XZDirection.POSITIVE_X) ? globalChunkX + 1 : globalChunkX - 1;
            GameObject[] chunkObjects = GameObject.FindGameObjectsWithTag("Chunk");
            ChunkData[] adjacentChunkData;
            for (int globalChunkZ = globalCenterCoordZ - renderDistance - 1; globalChunkZ <= globalCenterCoordZ + renderDistance + 1; globalChunkZ++)
            {
                // Create next pre-prepared chunk row
                GameObject chunk = new GameObject("Chunk");
                chunk.tag = "Chunk";
                ChunkData chunkData = chunk.AddComponent<ChunkData>();
                chunkData.globalPosX = outerChunkX;
                chunkData.globalPosZ = globalChunkZ;
                chunkData.blocks = new BlockID[GameData.CHUNK_SIZE, GameData.WORLD_HEIGHT_LIMIT, GameData.CHUNK_SIZE];
                if (!ChunkHelpers.ChunkFileExists(moon, outerChunkX, globalChunkZ))
                {
                    ChunkHelpers.GenerateChunk(chunkData.blocks, outerChunkX, globalChunkZ, seed);
                    ChunkHelpers.SaveChunkToFile(chunkData.blocks, moon, outerChunkX, globalChunkZ);
                }
                else
                {
                    ChunkHelpers.GetChunkFromFile(chunkData.blocks, moon, outerChunkX, globalChunkZ);
                }

                // Render current pre-prepared chunk row
                foreach (GameObject chunkObject in chunkObjects)
                {
                    // When UnloadDistantChunks() destroys chunk objects, Unity sets them to null and doesn't immediately remove them.
                    // The 0 check is to ensure blocks aren't duplicated
                    if (chunkObject != null && chunkObject.transform.childCount == 0)
                    {
                        ChunkData _chunkData = chunkObject.GetComponent<ChunkData>();
                        if (_chunkData.globalPosX == globalChunkX && _chunkData.globalPosZ == globalChunkZ && globalChunkZ > globalCenterCoordZ - renderDistance - 1 && globalChunkZ < globalCenterCoordZ + renderDistance + 1)
                        {
                            adjacentChunkData = ChunkHelpers.GetAdjacentChunkData(globalChunkX, globalChunkZ);
                            Vector3 globalChunkPosition = new Vector3(globalChunkX, 0, globalChunkZ);
                            for (int localBlockX = 0; localBlockX < GameData.CHUNK_SIZE; localBlockX++)
                            {
                                for (int localBlockY = 0; localBlockY < GameData.WORLD_HEIGHT_LIMIT; localBlockY++)
                                {
                                    for (int localBlockZ = 0; localBlockZ < GameData.CHUNK_SIZE; localBlockZ++)
                                    {
                                        BlockID block = _chunkData.blocks[localBlockX, localBlockY, localBlockZ];
                                        Vector3 localBlockPosition = new Vector3(localBlockX, localBlockY, localBlockZ);
                                        if (block != BlockID.air && ChunkHelpers.BlockShouldBeRendered(block, adjacentChunkData, _chunkData, localBlockPosition))
                                        {
                                            SpawnBlock(block, chunkObject, localBlockPosition, globalChunkPosition);
                                        }
                                    }
                                }
                                yield return null;
                            }
                        }
                    }
                }
            }
        }
        else // Moving along z axis
        {
            // Generate (globalCenterCoordX - renderDistance, globalCenterCoordZ) ... (globalCenterCoordX + renderDistance, globalCenterCoordZ)
            int globalCenterCoordX = currentRowRenderTask.globalCenterCoordX;
            int globalChunkZ = currentRowRenderTask.globalCenterCoordZ;

            int outerChunkZ = (moveDirection == XZDirection.POSITIVE_Z) ? globalChunkZ + 1 : globalChunkZ - 1;
            GameObject[] chunkObjects = GameObject.FindGameObjectsWithTag("Chunk");
            ChunkData[] adjacentChunkData;
            for (int globalChunkX = globalCenterCoordX - renderDistance - 1; globalChunkX <= globalCenterCoordX + renderDistance + 1; globalChunkX++)
            {
                // Create next pre-prepared chunk row
                GameObject chunk = new GameObject("Chunk");
                chunk.tag = "Chunk";
                ChunkData chunkData = chunk.AddComponent<ChunkData>();
                chunkData.globalPosX = globalChunkX;
                chunkData.globalPosZ = outerChunkZ;
                chunkData.blocks = new BlockID[GameData.CHUNK_SIZE, GameData.WORLD_HEIGHT_LIMIT, GameData.CHUNK_SIZE];

                if (!ChunkHelpers.ChunkFileExists(moon, globalChunkX, outerChunkZ))
                {
                    ChunkHelpers.GenerateChunk(chunkData.blocks, globalChunkX, outerChunkZ, seed);
                    ChunkHelpers.SaveChunkToFile(chunkData.blocks, moon, globalChunkX, outerChunkZ);
                }
                else
                {
                    ChunkHelpers.GetChunkFromFile(chunkData.blocks, moon, globalChunkX, outerChunkZ);
                }

                // Render current pre-prepared chunk row
                foreach (GameObject chunkObject in chunkObjects)
                {
                    // When UnloadDistantChunks() destroys chunk objects, Unity sets them to null and doesn't immediately remove them.
                    // The 0 check is to ensure blocks aren't duplicated
                    if (chunkObject != null && chunkObject.transform.childCount == 0)
                    {
                        ChunkData _chunkData = chunkObject.GetComponent<ChunkData>();
                        if (_chunkData.globalPosZ == globalChunkZ && _chunkData.globalPosX == globalChunkX && globalChunkX > globalCenterCoordX - renderDistance - 1 && globalChunkX < globalCenterCoordX + renderDistance + 1)
                        {
                            adjacentChunkData = ChunkHelpers.GetAdjacentChunkData(globalChunkX, globalChunkZ);
                            Vector3 globalChunkPosition = new Vector3(globalChunkX, 0, globalChunkZ);
                            for (int localBlockX = 0; localBlockX < GameData.CHUNK_SIZE; localBlockX++)
                            {
                                for (int localBlockY = 0; localBlockY < GameData.WORLD_HEIGHT_LIMIT; localBlockY++)
                                {
                                    for (int localBlockZ = 0; localBlockZ < GameData.CHUNK_SIZE; localBlockZ++)
                                    {
                                        BlockID block = _chunkData.blocks[localBlockX, localBlockY, localBlockZ];
                                        Vector3 localBlockPosition = new Vector3(localBlockX, localBlockY, localBlockZ);
                                        if (block != BlockID.air && ChunkHelpers.BlockShouldBeRendered(block, adjacentChunkData, _chunkData, localBlockPosition))
                                        {
                                            SpawnBlock(block, chunkObject, localBlockPosition, globalChunkPosition);
                                        }
                                    }
                                }
                                yield return null;
                            }
                        }
                    }
                }
            }
        }

        currentRowRenderTask.finished = true;

        stopWatch.Stop();
        chunkGenerationRate = (float)(2*renderDistance + 1) / stopWatch.Elapsed.Seconds;
    }

    private void SpawnBlock(BlockID block, GameObject parentChunk, Vector3 localBlockPosition, Vector3 globalChunkPosition)
    {
        GameObject blockObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blockObj.hideFlags = HideFlags.HideInHierarchy;
        blockObj.layer = LayerMask.NameToLayer("Block");
        blockObj.GetComponent<MeshFilter>().mesh = blockMesh;
        blockObj.GetComponent<Renderer>().material = blockMaterials[(int)block];
        blockObj.GetComponent<BoxCollider>().size = new Vector3(0.95F, 0.95F, 0.95F);

        BlockData blockData = blockObj.AddComponent<BlockData>();
        blockData.blockID = block;
        blockData.localPosX = (int)localBlockPosition.x;
        blockData.localPosY = (int)localBlockPosition.y;
        blockData.localPosZ = (int)localBlockPosition.z;

        blockObj.transform.position = new Vector3(
            (globalChunkPosition.x*GameData.CHUNK_SIZE + localBlockPosition.x), localBlockPosition.y, (globalChunkPosition.z*GameData.CHUNK_SIZE + localBlockPosition.z)
        );
        blockObj.transform.SetParent(parentChunk.transform);

        if (block == BlockID.light)
        {
            Light light = blockObj.AddComponent<Light>();
            light.type = LightType.Point;
        }
    }

    private void UnloadDistantChunks(XZDirection moveDirection, int newPlayerChunkX, int newPlayerChunkZ)
    {
        GameObject[] chunkObjects = GameObject.FindGameObjectsWithTag("Chunk");
        for (int i = 0; i < chunkObjects.Length; i++)
        {
            ChunkData chunkData = chunkObjects[i].GetComponent<ChunkData>();
            if (moveDirection == XZDirection.POSITIVE_X || moveDirection == XZDirection.NEGATIVE_X)
            {
                // Destroy most distant chunk objects
                int outerChunkX = (moveDirection == XZDirection.POSITIVE_X) ? newPlayerChunkX - renderDistance - 2 : newPlayerChunkX + renderDistance + 2;
                if (chunkData.globalPosX == outerChunkX)
                    Destroy(chunkObjects[i]);

                // Reallocate second-most distant blocks
                int innerChunkX = (moveDirection == XZDirection.POSITIVE_X) ? newPlayerChunkX - renderDistance - 1 : newPlayerChunkX + renderDistance + 1;
                if (chunkData.globalPosX == innerChunkX)
                {
                    Transform[] childBlocks = new Transform[chunkObjects[i].transform.childCount];
                    for (int j = 0; j < chunkObjects[i].transform.childCount; j++)
                        childBlocks[j] = chunkObjects[i].transform.GetChild(j);

                    foreach (Transform childBlock in childBlocks)
                        Destroy(childBlock.gameObject);

                    ChunkHelpers.SaveChunkToFile(chunkData.blocks, moon, chunkData.globalPosX, chunkData.globalPosZ);
                }
            }
            else
            {
                // Destroy most distant chunk objects
                int outerChunkZ = (moveDirection == XZDirection.POSITIVE_Z) ? newPlayerChunkZ - renderDistance - 2 : newPlayerChunkZ + renderDistance + 2;
                if (chunkData.globalPosZ == outerChunkZ)
                    Destroy(chunkObjects[i]);

                // Reallocate second-most distant blocks
                int innerChunkZ = (moveDirection == XZDirection.POSITIVE_Z) ? newPlayerChunkZ - renderDistance - 1 : newPlayerChunkZ + renderDistance + 1;
                if (chunkData.globalPosZ == innerChunkZ)
                {
                    Transform[] childBlocks = new Transform[chunkObjects[i].transform.childCount];
                    for (int j = 0; j < chunkObjects[i].transform.childCount; j++)
                        childBlocks[j] = chunkObjects[i].transform.GetChild(j);

                    foreach (Transform childBlock in childBlocks)
                        Destroy(childBlock.gameObject);

                    ChunkHelpers.SaveChunkToFile(chunkData.blocks, moon, chunkData.globalPosX, chunkData.globalPosZ);
                }
            }
        }
    }

    public float GetChunkGenerationRate()
    {
        return chunkGenerationRate;
    }

    public void SaveAllChunksToFile()
    {
        GameObject[] chunkObjects = GameObject.FindGameObjectsWithTag("Chunk");
        foreach (GameObject chunkObj in chunkObjects)
        {
            ChunkData chunkData = chunkObj.GetComponent<ChunkData>();
            ChunkHelpers.SaveChunkToFile(chunkData.blocks, moon, chunkData.globalPosX, chunkData.globalPosZ);
        }
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
