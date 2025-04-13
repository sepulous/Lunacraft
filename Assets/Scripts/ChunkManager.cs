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
    public int newPlayerChunkX;
    public int newPlayerChunkZ;
}

public class ChunkManager : MonoBehaviour
{
    private GameObject chunkParent = null;

    private Player player;
    private GameObject camera;
    private int playerChunkX;
    private int playerChunkZ;
    MoonData moonData;
    private int moon;
    private int renderDistance;

    private List<Material> blockMaterials;
    private Array blocks;
    private int numberOfBlocks;
    private Mesh blockMesh;

    private Options options;

    private Queue<RowRenderTask> rowRenderTasks;
    private RowRenderTask currentRowRenderTask;
    private float chunkGenerationRate = 0;

    private bool generating = false;

    // Brown mobs
    private bool brownMobExploded = false;
    private Vector3 brownMobExplodePos = Vector3.zero;

    /*

    Periodically check whether any chunks that should have loaded are loaded, and create/load them if not
        - Main rendering code should check whether a chunk is already queued

    */

    void Awake()
    {
        blockMesh = MeshData.GetBlockMesh();
        blocks = Enum.GetValues(typeof(BlockID));
        numberOfBlocks = blocks.Length;
        blockMaterials = MeshData.GetBlockMaterials();
        moon = PlayerPrefs.GetInt("moon");
    }

    void Start()
    {
        rowRenderTasks = new Queue<RowRenderTask>();

        options = OptionsManager.GetCurrentOptions();
        renderDistance = options.renderDistance;
        
        player = GameObject.Find("Player").GetComponent<Player>();
        camera = GameObject.Find("PlayerCamera");
        (playerChunkX, playerChunkZ) = player.GetChunkCoords();

        chunkParent = GameObject.Find("ChunkParent");

        string moonDataFile = string.Format("{0}/moons/moon{1}/moon.dat", Application.persistentDataPath, moon);
        using (FileStream file = File.Open(moonDataFile, FileMode.Open, FileAccess.Read))
            moonData = (MoonData)(new BinaryFormatter()).Deserialize(file);
    }

    void Update()
    {
        // Handle changed render distance
        // int newRenderDistance = OptionsManager.GetCurrentOptions().renderDistance;
        // if (newRenderDistance < renderDistance)
        // {
        //     GameObject[] chunkObjects = GameObject.FindGameObjectsWithTag("Chunk");
        //     for (int i = 0; i < chunkObjects.Length; i++)
        //     {
        //         GameObject chunkObj = chunkObjects[i];
        //         ChunkData chunkData = chunkObj.GetComponent<ChunkData>();
        //         if (chunkData.globalPosX > playerChunkX + newRenderDistance + 1 || chunkData.globalPosX < playerChunkX - newRenderDistance - 1
        //          || chunkData.globalPosZ > playerChunkZ + newRenderDistance + 1 || chunkData.globalPosZ < playerChunkZ - newRenderDistance - 1)
        //         {
        //             ChunkHelpers.SaveChunkToFile(chunkData.blocks, moon, chunkData.globalPosX, chunkData.globalPosZ);
        //             Destroy(chunkObj);
        //         }
        //         else if (chunkData.globalPosX == playerChunkX + newRenderDistance + 1 || chunkData.globalPosX == playerChunkX - newRenderDistance - 1
        //               || chunkData.globalPosZ == playerChunkZ + newRenderDistance + 1 || chunkData.globalPosZ == playerChunkZ - newRenderDistance - 1)
        //         {
        //             Transform[] childBlocks = new Transform[chunkObj.transform.childCount];
        //             for (int j = 0; j < chunkObj.transform.childCount; j++)
        //                 childBlocks[j] = chunkObj.transform.GetChild(j);

        //             foreach (Transform childBlock in childBlocks)
        //                 Destroy(childBlock.gameObject);

        //             ChunkHelpers.SaveChunkToFile(chunkData.blocks, moon, chunkData.globalPosX, chunkData.globalPosZ);
        //         }
        //     }
        // }
        // else if (newRenderDistance > renderDistance)
        // {
        //     rowRenderTasks.Clear();
        //     StartCoroutine(ExpandRenderDistance(renderDistance, newRenderDistance));
        // }
        // renderDistance = newRenderDistance;

        renderDistance = OptionsManager.GetCurrentOptions().renderDistance;

        /*

        Method 1:
            Use a bigger sphere to instantiate the blocks that will be exploded, as well as their neighbors, and then use the existing code.

        Method 2:
            Similar to Method 1, but instead of instantiating all of them, only instantiate the neighbor blocks and blocks that aren't destroyed


        How about this: Get all discrete global block positions within some distance from the sphere center, and use those to find the appropriate chunks
                        and access their block data

        */

        // Brown mob exploding
        if (brownMobExploded)
        {
            // Vector3 spherePos = brownMobExplodePos + 2F*Vector3.down;
            // Collider[] hits = Physics.OverlapSphere(spherePos, 4F, LayerMask.GetMask("Block"));
            // if (hits.Length > 0)
            // {
            //     // Destroy blocks
            //     foreach (Collider hit in hits)
            //     {
            //         GameObject hitBlock = hit.gameObject;
            //         BlockData hitBlockData = hitBlock.GetComponent<BlockData>();
            //         ChunkData hitBlockChunkData = hitBlock.transform.parent.GetComponent<ChunkData>();
            //         float distanceFromExplodePoint = (hitBlock.transform.position - spherePos).magnitude;

            //         if (distanceFromExplodePoint <= 3.9F || UnityEngine.Random.Range(0, 10) <= 8)
            //         {
            //             BlockID blockToDrop = hitBlockData.blockID;

            //             // Alchemy
            //             BlockID alchemyProduct = Alchemy.GetAlchemyProduct(hitBlockData.blockID);
            //             if (alchemyProduct != BlockID.unknown && UnityEngine.Random.Range(0, 5) == 0) // 20% chance for alchemy
            //                 blockToDrop = alchemyProduct;

            //             // Drop
            //             hitBlockChunkData.blocks[hitBlockData.localPosX, hitBlockData.localPosY, hitBlockData.localPosZ] = BlockID.air;
            //             if (blockToDrop == alchemyProduct)
            //                 hitBlock.GetComponent<Renderer>().material.SetTexture("_BaseTexture", Resources.Load<Texture2D>("Textures/Blocks/" + blockToDrop.ToString()));
            //             Dropped droppedScript = hitBlock.AddComponent<Dropped>();
            //             droppedScript.itemID = (ItemID)Enum.Parse(typeof(ItemID), blockToDrop.ToString());
            //             droppedScript.Drop();
            //         }
            //     }

            //     // Patch holes in map
            //     foreach (Collider hit in hits)
            //     {
            //         GameObject hitBlock = hit.gameObject;
            //         BlockData hitBlockData = hitBlock.GetComponent<BlockData>();
            //         ChunkData hitBlockChunkData = hitBlock.transform.parent.GetComponent<ChunkData>();
            //         hitBlock.transform.SetParent(null); // Should be done here, since we need the parent right above
                    
            //         int selectedGlobalPosX = (int)hitBlock.transform.position.x;
            //         int selectedGlobalPosY = (int)hitBlock.transform.position.y;
            //         int selectedGlobalPosZ = (int)hitBlock.transform.position.z;
            //         Vector3[] neighborGlobalPositions = {
            //             new Vector3(selectedGlobalPosX - 1, selectedGlobalPosY, selectedGlobalPosZ),
            //             new Vector3(selectedGlobalPosX, selectedGlobalPosY - 1, selectedGlobalPosZ),
            //             new Vector3(selectedGlobalPosX, selectedGlobalPosY, selectedGlobalPosZ - 1),
            //             new Vector3(selectedGlobalPosX + 1, selectedGlobalPosY, selectedGlobalPosZ),
            //             new Vector3(selectedGlobalPosX, selectedGlobalPosY + 1, selectedGlobalPosZ),
            //             new Vector3(selectedGlobalPosX, selectedGlobalPosY, selectedGlobalPosZ + 1)
            //         };
            //         foreach (Vector3 neighborGlobalPos in neighborGlobalPositions)
            //         {
            //             Collider[] neighborCheck = Physics.OverlapBox(neighborGlobalPos, new Vector3(0.1F, 0.1F, 0.1F));
            //             if (neighborCheck.Length == 0) // Neighbor of destroyed block doesn't already exist
            //             {
            //                 int neighborChunkX = Mathf.FloorToInt(neighborGlobalPos.x / GameData.CHUNK_SIZE);
            //                 int neighborChunkZ = Mathf.FloorToInt(neighborGlobalPos.z / GameData.CHUNK_SIZE);
            //                 ChunkData neighborChunkData = chunkParent.transform.Find($"Chunk ({neighborChunkX},{neighborChunkZ})").GetComponent<ChunkData>();
            //                 Vector3 neighborLocalPos = ChunkHelpers.GetLocalBlockPos(neighborGlobalPos);
            //                 BlockID neighborBlock = neighborChunkData.blocks[(int)neighborLocalPos.x, (int)neighborLocalPos.y, (int)neighborLocalPos.z];
            //                 if (neighborBlock != BlockID.air)
            //                     SpawnBlock(neighborBlock, neighborChunkData.gameObject, neighborLocalPos, new Vector3(neighborChunkX, 0, neighborChunkZ));
            //             }
            //         }
            //     }
            // }

            Vector3 spherePos = brownMobExplodePos + 2F*Vector3.down;

            // Get hit block data (has to be calculated since most aren't instantiated yet)
            int explodePosX = (int)brownMobExplodePos.x;
            int explodePosY = (int)brownMobExplodePos.y;
            int explodePosZ = (int)brownMobExplodePos.z;
            List<Vector3> hitBlockGlobalPositions = new List<Vector3>();
            for (int x = -3; x <= 3; x++)
            {
                for (int y = -3; y <= 3; y++)
                {
                    for (int z = -3; z <= 3; z++)
                    {
                        if (x*x + y*y + z*z <= 3*3)
                            hitBlockGlobalPositions.Add(new Vector3(explodePosX + x, explodePosY + y, explodePosZ + z));
                    }
                }
            }

            // Destroy blocks
            foreach (Vector3 hitBlockGlobalPos in hitBlockGlobalPositions)
            {
                int parentChunkX = Mathf.FloorToInt(hitBlockGlobalPos.x / GameData.CHUNK_SIZE);
                int parentChunkZ = Mathf.FloorToInt(hitBlockGlobalPos.z / GameData.CHUNK_SIZE);
                ChunkData parentChunkData = chunkParent.transform.Find($"Chunk ({parentChunkX},{parentChunkZ})").GetComponent<ChunkData>();
                Vector3 hitBlockLocalPos = ChunkHelpers.GetLocalBlockPos(hitBlockGlobalPos);
                BlockID hitBlockID = parentChunkData.blocks[(int)hitBlockLocalPos.x, (int)hitBlockLocalPos.y, (int)hitBlockLocalPos.z];

                if (hitBlockID != BlockID.air)
                {
                    float distanceFromExplodePoint = (hitBlockGlobalPos - spherePos).magnitude;
                    if (distanceFromExplodePoint <= 2.5F || UnityEngine.Random.Range(0, 10) <= 8)
                    {
                        BlockID blockToDrop = hitBlockID;

                        // Alchemy
                        BlockID alchemyProduct = Alchemy.GetAlchemyProduct(hitBlockID);
                        if (alchemyProduct != BlockID.unknown && UnityEngine.Random.Range(0, 5) == 0) // 20% chance for alchemy, when possible
                            blockToDrop = alchemyProduct;

                        // Drop
                        parentChunkData.blocks[(int)hitBlockLocalPos.x, (int)hitBlockLocalPos.y, (int)hitBlockLocalPos.z] = BlockID.air;

                        GameObject hitBlock;
                        Collider[] hits = Physics.OverlapBox(hitBlockGlobalPos, new Vector3(0.1F, 0.1F, 0.1F), Quaternion.identity, LayerMask.GetMask("Block"));
                        if (hits.Length > 0) // Get existing block to drop
                        {
                            hitBlock = hits[0].gameObject;
                        }
                        else // Create block to drop
                        {
                            hitBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            hitBlock.transform.position = hitBlockGlobalPos;
                            hitBlock.hideFlags = HideFlags.HideInHierarchy;
                            hitBlock.GetComponent<MeshFilter>().mesh = blockMesh;
                            hitBlock.GetComponent<Renderer>().material = blockMaterials[(int)blockToDrop];
                        }

                        if (blockToDrop == alchemyProduct)
                            hitBlock.GetComponent<Renderer>().material.SetTexture("_BaseTexture", Resources.Load<Texture2D>("Textures/Blocks/" + blockToDrop.ToString()));
                        Dropped droppedScript = hitBlock.AddComponent<Dropped>();
                        droppedScript.itemID = (ItemID)Enum.Parse(typeof(ItemID), blockToDrop.ToString());
                        droppedScript.Drop();
                    }
                }
            }

            // Patch holes in map
            List<Vector3> blocksBroughtBack = new List<Vector3>();
            foreach (Vector3 hitBlockGlobalPos in hitBlockGlobalPositions)
            {
                int hitGlobalPosX = (int)hitBlockGlobalPos.x;
                int hitGlobalPosY = (int)hitBlockGlobalPos.y;
                int hitGlobalPosZ = (int)hitBlockGlobalPos.z;
                Vector3[] neighborGlobalPositions = {
                    new Vector3(hitGlobalPosX - 1, hitGlobalPosY, hitGlobalPosZ),
                    new Vector3(hitGlobalPosX, hitGlobalPosY - 1, hitGlobalPosZ),
                    new Vector3(hitGlobalPosX, hitGlobalPosY, hitGlobalPosZ - 1),
                    new Vector3(hitGlobalPosX + 1, hitGlobalPosY, hitGlobalPosZ),
                    new Vector3(hitGlobalPosX, hitGlobalPosY + 1, hitGlobalPosZ),
                    new Vector3(hitGlobalPosX, hitGlobalPosY, hitGlobalPosZ + 1)
                };
                foreach (Vector3 neighborGlobalPos in neighborGlobalPositions)
                {
                    if (blocksBroughtBack.Contains(neighborGlobalPos)) // Don't bring back blocks that were just exploded
                        continue;

                    Collider[] neighborCheck = Physics.OverlapBox(neighborGlobalPos, new Vector3(0.1F, 0.1F, 0.1F));
                    if (neighborCheck.Length == 0) // Neighbor of destroyed block doesn't already exist
                    {
                        int neighborChunkX = Mathf.FloorToInt(neighborGlobalPos.x / GameData.CHUNK_SIZE);
                        int neighborChunkZ = Mathf.FloorToInt(neighborGlobalPos.z / GameData.CHUNK_SIZE);
                        ChunkData neighborChunkData = chunkParent.transform.Find($"Chunk ({neighborChunkX},{neighborChunkZ})").GetComponent<ChunkData>();
                        Vector3 neighborLocalPos = ChunkHelpers.GetLocalBlockPos(neighborGlobalPos);
                        BlockID neighborBlock = neighborChunkData.blocks[(int)neighborLocalPos.x, (int)neighborLocalPos.y, (int)neighborLocalPos.z];
                        if (neighborBlock != BlockID.air)
                        {
                            blocksBroughtBack.Add(neighborGlobalPos);
                            SpawnBlock(neighborBlock, neighborChunkData.gameObject, neighborLocalPos, new Vector3(neighborChunkX, 0, neighborChunkZ));
                        }
                    }
                }
            }

            brownMobExploded = false;
        }

        // Block destruction
        if (player.DestroyedBlock())
        {
            // Update chunk data
            GameObject selectedBlock = player.GetSelectedBlock();
            BlockData selectedBlockData = selectedBlock.GetComponent<BlockData>();
            ChunkData selectedBlockChunkData = selectedBlock.transform.parent.GetComponent<ChunkData>();
            selectedBlockChunkData.blocks[selectedBlockData.localPosX, selectedBlockData.localPosY, selectedBlockData.localPosZ] = BlockID.air;

            // Drop block item
            selectedBlock.transform.SetParent(null);
            if (selectedBlockData.blockID == BlockID.topsoil)
                selectedBlock.GetComponent<Renderer>().material.SetTexture("_BaseTexture", Resources.Load<Texture2D>("Textures/Blocks/dirt"));
            Dropped droppedScript = selectedBlock.AddComponent<Dropped>();
            droppedScript.itemID = (ItemID)Enum.Parse(typeof(ItemID), selectedBlockData.blockID.ToString());
            droppedScript.Drop();

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
            foreach (Vector3 neighborGlobalPos in neighborGlobalPositions)
            {
                Collider[] hits = Physics.OverlapBox(neighborGlobalPos, new Vector3(0.1F, 0.1F, 0.1F));
                if (hits.Length == 0) // Neighbor of destroyed block doesn't already exist
                {
                    int neighborChunkX = Mathf.FloorToInt(neighborGlobalPos.x / GameData.CHUNK_SIZE);
                    int neighborChunkZ = Mathf.FloorToInt(neighborGlobalPos.z / GameData.CHUNK_SIZE);
                    ChunkData neighborChunkData = chunkParent.transform.Find($"Chunk ({neighborChunkX},{neighborChunkZ})").GetComponent<ChunkData>();
                    Vector3 neighborLocalPos = ChunkHelpers.GetLocalBlockPos(neighborGlobalPos);
                    BlockID neighborBlock = neighborChunkData.blocks[(int)neighborLocalPos.x, (int)neighborLocalPos.y, (int)neighborLocalPos.z];
                    if (neighborBlock != BlockID.air)
                        SpawnBlock(neighborBlock, neighborChunkData.gameObject, neighborLocalPos, new Vector3(neighborChunkX, 0, neighborChunkZ));
                }
            }

            // Remove block object
            player.ResetDestroyedBlock();
        }

        // Block placing
        if (player.PlacedBlock())
        {
            ItemID selectedItem = player.GetSelectedItem();
            if (Enum.TryParse(selectedItem.ToString(), out BlockID selectedBlockID)) // Item is a block
            {
                RaycastHit hit;
                if (Physics.Raycast(camera.transform.position, camera.transform.forward, out hit, Mathf.Infinity, LayerMask.GetMask("Block")))
                {
                    Vector3 selectedBlockPos = player.GetSelectedBlock().transform.position;
                    Vector3 faceNormal = hit.normal;
                    Vector3 newBlockPos = selectedBlockPos + faceNormal;

                    // Check if block is already there
                    // The player can see through corners if they try, so it's technically possible to select a hidden face
                    if ((Physics.OverlapBox(newBlockPos, new Vector3(0.5F, 0.5F, 0.5F), Quaternion.identity, LayerMask.GetMask("Block"))).Length == 0)
                    {

                        // int mobType = UnityEngine.Random.Range(0, 2);

                        // if (mobType == 0)
                        // {
                        //     GameObject greenMob = Instantiate(Resources.Load("Mobs/Green Mob/Green Mob", typeof(GameObject))) as GameObject;
                        //     greenMob.transform.position = new Vector3(newBlockPos.x, newBlockPos.y + 0.01F, newBlockPos.z);
                        //     greenMob.AddComponent<GreenMob>();
                        // }
                        // else
                        // {
                        //     GameObject brownMob = Instantiate(Resources.Load("Mobs/Brown Mob/Brown Mob", typeof(GameObject))) as GameObject;
                        //     brownMob.transform.position = new Vector3(newBlockPos.x, newBlockPos.y + 0.01F, newBlockPos.z);
                        //     brownMob.AddComponent<BrownMob>();
                        // }

                        GameObject blockObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        blockObj.hideFlags = HideFlags.HideInHierarchy;
                        blockObj.layer = LayerMask.NameToLayer("Block");
                        blockObj.GetComponent<MeshFilter>().mesh = blockMesh;
                        blockObj.GetComponent<Renderer>().material = blockMaterials[(int)selectedBlockID];
                        blockObj.GetComponent<BoxCollider>().size = new Vector3(0.98F, 0.98F, 0.98F);
                        blockObj.transform.position = new Vector3(newBlockPos.x, newBlockPos.y, newBlockPos.z);

                        BlockData blockData = blockObj.AddComponent<BlockData>();
                        blockData.blockID = selectedBlockID;
                        Vector3 localBlockPos = ChunkHelpers.GetLocalBlockPos(blockObj.transform.position);
                        blockData.localPosX = (int)localBlockPos.x;
                        blockData.localPosY = (int)localBlockPos.y;
                        blockData.localPosZ = (int)localBlockPos.z;

                        if (selectedBlockID == BlockID.light)
                        {
                            Light light = blockObj.AddComponent<Light>();
                            light.type = LightType.Point;
                        }

                        // Update parent chunk
                        (int blockChunkX, int blockChunkZ) = ChunkHelpers.GetChunkCoordsByBlockPos(blockObj.transform.position);
                        GameObject parentChunk = chunkParent.transform.Find($"Chunk ({blockChunkX},{blockChunkZ})").gameObject;
                        blockObj.transform.parent = parentChunk.transform;
                        parentChunk.GetComponent<ChunkData>().blocks[blockData.localPosX, blockData.localPosY, blockData.localPosZ] = selectedBlockID;
                        // foreach (GameObject chunk in GameObject.FindGameObjectsWithTag("Chunk"))
                        // {
                        //     ChunkData chunkData = chunk.GetComponent<ChunkData>();
                        //     if (chunkData.globalPosX == blockChunkX && chunkData.globalPosZ == blockChunkZ)
                        //     {
                        //         blockObj.transform.parent = chunk.transform;
                        //         chunkData.blocks[blockData.localPosX, blockData.localPosY, blockData.localPosZ] = selectedBlockID;
                        //         break;
                        //     }
                        // }

                        player.DecrementSelectedItem();
                    }
                }
            }
            player.ResetPlacedBlock();
        }

        (int newPlayerChunkX, int newPlayerChunkZ) = player.GetChunkCoords();

        // Render chunks
        if (newPlayerChunkX != playerChunkX)
        {
            RowRenderTask task = new RowRenderTask();
            task.finished = false;
            task.direction = (newPlayerChunkX - playerChunkX == 1) ? XZDirection.POSITIVE_X : XZDirection.NEGATIVE_X;
            //task.globalCenterCoordX = (task.direction == XZDirection.POSITIVE_X) ? newPlayerChunkX + renderDistance : newPlayerChunkX - renderDistance;
            task.newPlayerChunkX = newPlayerChunkX;
            //task.globalCenterCoordZ = playerChunkZ;
            task.newPlayerChunkZ = newPlayerChunkZ;
            rowRenderTasks.Enqueue(task);
        }

        if (newPlayerChunkZ != playerChunkZ)
        {
            RowRenderTask task = new RowRenderTask();
            task.finished = false;
            task.direction = (newPlayerChunkZ - playerChunkZ == 1) ? XZDirection.POSITIVE_Z : XZDirection.NEGATIVE_Z;
            //task.globalCenterCoordX = playerChunkX;
            task.newPlayerChunkX = newPlayerChunkX;
            //task.globalCenterCoordZ = (task.direction == XZDirection.POSITIVE_Z) ? newPlayerChunkZ + renderDistance : newPlayerChunkZ - renderDistance;
            task.newPlayerChunkZ = newPlayerChunkZ;
            rowRenderTasks.Enqueue(task);
        }

        playerChunkX = newPlayerChunkX;
        playerChunkZ = newPlayerChunkZ;

        if ((currentRowRenderTask == null || currentRowRenderTask.finished) && rowRenderTasks.Count > 0)
        {
            currentRowRenderTask = rowRenderTasks.Dequeue();
            //UnloadDistantChunks(currentRowRenderTask.direction, newPlayerChunkX, newPlayerChunkZ);
            StartCoroutine(RenderNewRow());
        }
    }

    public void BrownMobExplode(Vector3 globalPos)
    {
        brownMobExploded = true;
        brownMobExplodePos = globalPos;
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
                        ChunkHelpers.GenerateChunk(chunkData.blocks, globalChunkX, globalChunkZ, moonData);
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
            if (chunkObj != null)
            {
                ChunkData chunkData = chunkObj.GetComponent<ChunkData>();
                if (
                    Mathf.Abs(chunkData.globalPosX - playerChunkX) < newRenderDistance + 1 && Mathf.Abs(chunkData.globalPosZ - playerChunkZ) < newRenderDistance + 1
                && (Mathf.Abs(chunkData.globalPosX - playerChunkX) > oldRenderDistance || Mathf.Abs(chunkData.globalPosZ - playerChunkZ) > oldRenderDistance))
                {
                    adjacentChunkData = ChunkHelpers.GetAdjacentChunkData(chunkParent, chunkData.globalPosX, chunkData.globalPosZ);
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
    }

    private IEnumerator RenderNewRow()
    {
        int playerChunkX = currentRowRenderTask.newPlayerChunkX;
        int playerChunkZ = currentRowRenderTask.newPlayerChunkZ;
        XZDirection moveDirection = currentRowRenderTask.direction;

        var stopWatch = new System.Diagnostics.Stopwatch();
        stopWatch.Start();

        if (moveDirection == XZDirection.POSITIVE_X || moveDirection == XZDirection.NEGATIVE_X) // Moving along x axis
        {
            int sign = moveDirection == XZDirection.POSITIVE_X ? 1 : -1;
            int chunkX = playerChunkX + sign*renderDistance;
            int borderChunkX = chunkX + sign;

            for (int chunkZ = playerChunkZ - renderDistance - 1; chunkZ <= playerChunkZ + renderDistance + 1; chunkZ++)
            {
                GameObject distantRenderedChunk = chunkParent.transform.Find($"Chunk ({playerChunkX - sign*(renderDistance + 1)},{chunkZ})").gameObject;
                if (distantRenderedChunk != null)
                {
                    // Destroy distant chunk and its child blocks
                    // After a bit of experimenting, apparently this is only a small part of the lag spike
                    ChunkData distantRenderedChunkData = distantRenderedChunk.GetComponent<ChunkData>();
                    ChunkHelpers.SaveChunkToFile(distantRenderedChunkData.blocks, moon, distantRenderedChunkData.globalPosX, distantRenderedChunkData.globalPosZ);
                    Destroy(distantRenderedChunk);

                    // Bring it back to hold block data
                    GameObject newDistantRenderedChunk = new GameObject($"Chunk ({distantRenderedChunkData.globalPosX},{distantRenderedChunkData.globalPosZ})");
                    newDistantRenderedChunk.transform.SetParent(chunkParent.transform);
                    newDistantRenderedChunk.tag = "Chunk";
                    ChunkData newDistantRenderedChunkData = newDistantRenderedChunk.AddComponent<ChunkData>();
                    newDistantRenderedChunkData.globalPosX = distantRenderedChunkData.globalPosX;
                    newDistantRenderedChunkData.globalPosZ = distantRenderedChunkData.globalPosZ;
                    newDistantRenderedChunkData.blocks = distantRenderedChunkData.blocks;
                    //ChunkHelpers.GetChunkFromFile(newOldChunkData.blocks, moon, newOldChunkData.globalPosX, newOldChunkData.globalPosZ); // We know this chunk file exists
                    yield return null;
                }

                // Create border chunk objects with block data
                GameObject borderChunk = new GameObject($"Chunk ({borderChunkX},{chunkZ})");
                borderChunk.transform.SetParent(chunkParent.transform);
                borderChunk.tag = "Chunk";
                ChunkData borderChunkData = borderChunk.AddComponent<ChunkData>();
                borderChunkData.globalPosX = borderChunkX;
                borderChunkData.globalPosZ = chunkZ;
                borderChunkData.blocks = new BlockID[GameData.CHUNK_SIZE, GameData.WORLD_HEIGHT_LIMIT, GameData.CHUNK_SIZE];
                if (!ChunkHelpers.ChunkFileExists(moon, borderChunkX, chunkZ))
                {
                    ChunkHelpers.GenerateChunk(borderChunkData.blocks, borderChunkX, chunkZ, moonData);
                    //ChunkHelpers.SaveChunkToFile(borderChunkData.blocks, moon, borderChunkX, chunkZ);
                }
                else
                {
                    ChunkHelpers.GetChunkFromFile(borderChunkData.blocks, moon, borderChunkX, chunkZ);
                }
                yield return null;
            }

            // Render new chunks
            ChunkData[] adjacentChunkData;
            Vector3 localBlockPosition;
            Vector3 chunkPos = Vector3.zero;
            for (int chunkZ = playerChunkZ - renderDistance; chunkZ <= playerChunkZ + renderDistance; chunkZ++)
            {
                GameObject chunk = chunkParent.transform.Find($"Chunk ({chunkX},{chunkZ})").gameObject;
                ChunkData chunkData = chunk.GetComponent<ChunkData>();
                adjacentChunkData = ChunkHelpers.GetAdjacentChunkData(chunkParent, chunkX, chunkZ);
                chunkPos.x = chunkX;
                chunkPos.z = chunkZ;
                for (int localBlockX = 0; localBlockX < GameData.CHUNK_SIZE; localBlockX++)
                {
                    for (int localBlockY = 0; localBlockY < GameData.WORLD_HEIGHT_LIMIT; localBlockY++)
                    {
                        for (int localBlockZ = 0; localBlockZ < GameData.CHUNK_SIZE; localBlockZ++)
                        {
                            BlockID block = chunkData.blocks[localBlockX, localBlockY, localBlockZ];
                            if (block != BlockID.air)
                            {
                                localBlockPosition.x = localBlockX;
                                localBlockPosition.y = localBlockY;
                                localBlockPosition.z = localBlockZ;
                                if (block == BlockID.topsoil || ChunkHelpers.BlockShouldBeRendered(block, adjacentChunkData, chunkData, localBlockPosition))
                                {
                                    SpawnBlock(block, chunk, localBlockPosition, chunkPos);
                                }
                            }
                        }
                    }
                    yield return null;
                }
            }

            // int globalChunkX = currentRowRenderTask.globalCenterCoordX;
            // int globalCenterCoordZ = currentRowRenderTask.globalCenterCoordZ;

            // int outerChunkX = (moveDirection == XZDirection.POSITIVE_X) ? globalChunkX + 1 : globalChunkX - 1;
            // GameObject[] chunkObjects = GameObject.FindGameObjectsWithTag("Chunk");
            // ChunkData[] adjacentChunkData;
            // for (int globalChunkZ = globalCenterCoordZ - renderDistance - 1; globalChunkZ <= globalCenterCoordZ + renderDistance + 1; globalChunkZ++)
            // {
            //     // Create next pre-prepared chunk row
            //     GameObject chunk = new GameObject($"Chunk ({outerChunkX},{globalChunkZ})");
            //     chunk.tag = "Chunk";
            //     ChunkData chunkData = chunk.AddComponent<ChunkData>();
            //     chunkData.globalPosX = outerChunkX;
            //     chunkData.globalPosZ = globalChunkZ;
            //     chunkData.blocks = new BlockID[GameData.CHUNK_SIZE, GameData.WORLD_HEIGHT_LIMIT, GameData.CHUNK_SIZE];
            //     if (!ChunkHelpers.ChunkFileExists(moon, outerChunkX, globalChunkZ))
            //     {
            //         ChunkHelpers.GenerateChunk(chunkData.blocks, outerChunkX, globalChunkZ, moonData);
            //         ChunkHelpers.SaveChunkToFile(chunkData.blocks, moon, outerChunkX, globalChunkZ);
            //     }
            //     else
            //     {
            //         ChunkHelpers.GetChunkFromFile(chunkData.blocks, moon, outerChunkX, globalChunkZ);
            //     }

                // Render current pre-prepared chunk row
                // foreach (GameObject chunkObject in chunkObjects)
                // {
                //     // When UnloadDistantChunks() destroys chunk objects, Unity sets them to null and doesn't immediately remove them.
                //     // The 0 check is to ensure blocks aren't duplicated
                //     if (chunkObject != null && chunkObject.transform.childCount == 0)
                //     {
                //         ChunkData _chunkData = chunkObject.GetComponent<ChunkData>();
                //         if (_chunkData.globalPosX == globalChunkX && _chunkData.globalPosZ == globalChunkZ && globalChunkZ > globalCenterCoordZ - renderDistance - 1 && globalChunkZ < globalCenterCoordZ + renderDistance + 1)
                //         {
                //             adjacentChunkData = ChunkHelpers.GetAdjacentChunkData(globalChunkX, globalChunkZ);
                //             Vector3 globalChunkPosition = new Vector3(globalChunkX, 0, globalChunkZ);
                //             for (int localBlockX = 0; localBlockX < GameData.CHUNK_SIZE; localBlockX++)
                //             {
                //                 for (int localBlockY = 0; localBlockY < GameData.WORLD_HEIGHT_LIMIT; localBlockY++)
                //                 {
                //                     for (int localBlockZ = 0; localBlockZ < GameData.CHUNK_SIZE; localBlockZ++)
                //                     {
                //                         BlockID block = _chunkData.blocks[localBlockX, localBlockY, localBlockZ];
                //                         Vector3 localBlockPosition = new Vector3(localBlockX, localBlockY, localBlockZ);
                //                         if (block != BlockID.air && ChunkHelpers.BlockShouldBeRendered(block, adjacentChunkData, _chunkData, localBlockPosition))
                //                         {
                //                             SpawnBlock(block, chunkObject, localBlockPosition, globalChunkPosition);
                //                         }
                //                     }
                //                 }
                //                 yield return null;
                //             }
                //         }
                //     }
                // }
            // }
        }
        else // Moving along z axis
        {
            int sign = moveDirection == XZDirection.POSITIVE_Z ? 1 : -1;
            int chunkZ = playerChunkZ + sign*renderDistance;
            int borderChunkZ = chunkZ + sign;

            for (int chunkX = playerChunkX - renderDistance - 1; chunkX <= playerChunkX + renderDistance + 1; chunkX++)
            {
                GameObject distantRenderedChunk = chunkParent.transform.Find($"Chunk ({chunkX},{playerChunkZ - sign*(renderDistance + 1)})").gameObject;
                if (distantRenderedChunk != null)
                {
                    // Destroy distant chunk and its child blocks
                    ChunkData distantRenderedChunkData = distantRenderedChunk.GetComponent<ChunkData>();
                    ChunkHelpers.SaveChunkToFile(distantRenderedChunkData.blocks, moon, distantRenderedChunkData.globalPosX, distantRenderedChunkData.globalPosZ);
                    Destroy(distantRenderedChunk);

                    // Bring it back to hold block data
                    GameObject newDistantRenderedChunk = new GameObject($"Chunk ({distantRenderedChunkData.globalPosX},{distantRenderedChunkData.globalPosZ})");
                    newDistantRenderedChunk.transform.SetParent(chunkParent.transform);
                    newDistantRenderedChunk.tag = "Chunk";
                    ChunkData newDistantRenderedChunkData = newDistantRenderedChunk.AddComponent<ChunkData>();
                    newDistantRenderedChunkData.globalPosX = distantRenderedChunkData.globalPosX;
                    newDistantRenderedChunkData.globalPosZ = distantRenderedChunkData.globalPosZ;
                    newDistantRenderedChunkData.blocks = distantRenderedChunkData.blocks;
                    //ChunkHelpers.GetChunkFromFile(newOldChunkData.blocks, moon, newOldChunkData.globalPosX, newOldChunkData.globalPosZ); // We know this chunk file exists
                    yield return null;
                }

                // Create border chunk objects with block data
                GameObject borderChunk = new GameObject($"Chunk ({chunkX},{borderChunkZ})");
                borderChunk.transform.SetParent(chunkParent.transform);
                borderChunk.tag = "Chunk";
                ChunkData borderChunkData = borderChunk.AddComponent<ChunkData>();
                borderChunkData.globalPosX = chunkX;
                borderChunkData.globalPosZ = borderChunkZ;
                borderChunkData.blocks = new BlockID[GameData.CHUNK_SIZE, GameData.WORLD_HEIGHT_LIMIT, GameData.CHUNK_SIZE];
                if (!ChunkHelpers.ChunkFileExists(moon, chunkX, borderChunkZ))
                {
                    ChunkHelpers.GenerateChunk(borderChunkData.blocks, chunkX, borderChunkZ, moonData);
                    //ChunkHelpers.SaveChunkToFile(borderChunkData.blocks, moon, chunkX, borderChunkZ);
                }
                else
                {
                    ChunkHelpers.GetChunkFromFile(borderChunkData.blocks, moon, chunkX, borderChunkZ);
                }
                yield return null;
            }

            // Render new chunks
            ChunkData[] adjacentChunkData;
            Vector3 localBlockPosition;
            Vector3 chunkPos = Vector3.zero;
            for (int chunkX = playerChunkX - renderDistance; chunkX <= playerChunkX + renderDistance; chunkX++)
            {
                GameObject newChunk = chunkParent.transform.Find($"Chunk ({chunkX},{chunkZ})").gameObject;
                ChunkData chunkData = newChunk.GetComponent<ChunkData>();
                adjacentChunkData = ChunkHelpers.GetAdjacentChunkData(chunkParent, chunkX, chunkZ);
                chunkPos.x = chunkX;
                chunkPos.z = chunkZ;
                for (int localBlockX = 0; localBlockX < GameData.CHUNK_SIZE; localBlockX++)
                {
                    for (int localBlockY = 0; localBlockY < GameData.WORLD_HEIGHT_LIMIT; localBlockY++)
                    {
                        for (int localBlockZ = 0; localBlockZ < GameData.CHUNK_SIZE; localBlockZ++)
                        {
                            BlockID block = chunkData.blocks[localBlockX, localBlockY, localBlockZ];
                            if (block != BlockID.air)
                            {
                                localBlockPosition.x = localBlockX;
                                localBlockPosition.y = localBlockY;
                                localBlockPosition.z = localBlockZ;
                                if (block == BlockID.topsoil || ChunkHelpers.BlockShouldBeRendered(block, adjacentChunkData, chunkData, localBlockPosition))
                                {
                                    SpawnBlock(block, newChunk, localBlockPosition, chunkPos);
                                }
                            }
                        }
                    }
                    yield return null;
                }
            }

            // Generate (globalCenterCoordX - renderDistance, globalCenterCoordZ) ... (globalCenterCoordX + renderDistance, globalCenterCoordZ)
            // int globalCenterCoordX = currentRowRenderTask.globalCenterCoordX;
            // int globalChunkZ = currentRowRenderTask.globalCenterCoordZ;

            // int outerChunkZ = (moveDirection == XZDirection.POSITIVE_Z) ? globalChunkZ + 1 : globalChunkZ - 1;
            // GameObject[] chunkObjects = GameObject.FindGameObjectsWithTag("Chunk");
            // ChunkData[] adjacentChunkData;
            // for (int globalChunkX = globalCenterCoordX - renderDistance - 1; globalChunkX <= globalCenterCoordX + renderDistance + 1; globalChunkX++)
            // {
            //     // Create next pre-prepared chunk row
            //     GameObject chunk = new GameObject($"Chunk ({globalChunkX},{globalChunkZ})");
            //     chunk.tag = "Chunk";
            //     ChunkData chunkData = chunk.AddComponent<ChunkData>();
            //     chunkData.globalPosX = globalChunkX;
            //     chunkData.globalPosZ = outerChunkZ;
            //     chunkData.blocks = new BlockID[GameData.CHUNK_SIZE, GameData.WORLD_HEIGHT_LIMIT, GameData.CHUNK_SIZE];

            //     if (!ChunkHelpers.ChunkFileExists(moon, globalChunkX, outerChunkZ))
            //     {
            //         ChunkHelpers.GenerateChunk(chunkData.blocks, globalChunkX, outerChunkZ, moonData);
            //         ChunkHelpers.SaveChunkToFile(chunkData.blocks, moon, globalChunkX, outerChunkZ);
            //     }
            //     else
            //     {
            //         ChunkHelpers.GetChunkFromFile(chunkData.blocks, moon, globalChunkX, outerChunkZ);
            //     }

            //     // Render current pre-prepared chunk row
            //     foreach (GameObject chunkObject in chunkObjects)
            //     {
            //         // When UnloadDistantChunks() destroys chunk objects, Unity sets them to null and doesn't immediately remove them.
            //         // The 0 check is to ensure blocks aren't duplicated
            //         if (chunkObject != null && chunkObject.transform.childCount == 0)
            //         {
            //             ChunkData _chunkData = chunkObject.GetComponent<ChunkData>();
            //             if (_chunkData.globalPosZ == globalChunkZ && _chunkData.globalPosX == globalChunkX && globalChunkX > globalCenterCoordX - renderDistance - 1 && globalChunkX < globalCenterCoordX + renderDistance + 1)
            //             {
            //                 adjacentChunkData = ChunkHelpers.GetAdjacentChunkData(globalChunkX, globalChunkZ);
            //                 Vector3 globalChunkPosition = new Vector3(globalChunkX, 0, globalChunkZ);
            //                 for (int localBlockX = 0; localBlockX < GameData.CHUNK_SIZE; localBlockX++)
            //                 {
            //                     for (int localBlockY = 0; localBlockY < GameData.WORLD_HEIGHT_LIMIT; localBlockY++)
            //                     {
            //                         for (int localBlockZ = 0; localBlockZ < GameData.CHUNK_SIZE; localBlockZ++)
            //                         {
            //                             BlockID block = _chunkData.blocks[localBlockX, localBlockY, localBlockZ];
            //                             Vector3 localBlockPosition = new Vector3(localBlockX, localBlockY, localBlockZ);
            //                             if (block != BlockID.air && ChunkHelpers.BlockShouldBeRendered(block, adjacentChunkData, _chunkData, localBlockPosition))
            //                             {
            //                                 SpawnBlock(block, chunkObject, localBlockPosition, globalChunkPosition);
            //                             }
            //                         }
            //                     }
            //                     yield return null;
            //                 }
            //             }
            //         }
            //     }
            // }
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

        if (block == BlockID.water)
        {
            PhysicsMaterial physMat = blockObj.GetComponent<BoxCollider>().material;
            physMat.staticFriction = 0.2F;
            physMat.dynamicFriction = 0.2F;
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
}
