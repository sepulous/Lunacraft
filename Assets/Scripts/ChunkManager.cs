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

    public GameObject brownMobPrefab;
    public GameObject greenMobPrefab;
    public GameObject spaceGiraffePrefab;

    public GameObject minilight_pz;
    public GameObject minilight_nz;
    public GameObject minilight_px;
    public GameObject minilight_nx;

    public GameObject blockPrefab;

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
        //renderDistance = 1;

        player = GameObject.Find("Player").GetComponent<Player>();
        camera = GameObject.Find("PlayerCamera");
        (playerChunkX, playerChunkZ) = player.GetChunkCoords();

        chunkParent = GameObject.Find("ChunkParent");

        string moonDataFile = string.Format("{0}/moons/moon{1}/moon.dat", Application.persistentDataPath, moon);
        using (FileStream file = File.Open(moonDataFile, FileMode.Open, FileAccess.Read))
            moonData = (MoonData)(new BinaryFormatter()).Deserialize(file);
    }

    /*
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
    */

    void Update()
    {
        // Handle changed render distance
        int newRenderDistance = OptionsManager.GetCurrentOptions().renderDistance;
        if (newRenderDistance < renderDistance)
        {
            for (int i = 0; i < chunkParent.transform.childCount; i++)
            {
                GameObject chunk = chunkParent.transform.GetChild(i).gameObject;
                ChunkData chunkData = chunk.GetComponent<ChunkData>();
                if (chunkData.globalPosX > playerChunkX + newRenderDistance + 1 || chunkData.globalPosX < playerChunkX - newRenderDistance - 1
                 || chunkData.globalPosZ > playerChunkZ + newRenderDistance + 1 || chunkData.globalPosZ < playerChunkZ - newRenderDistance - 1)
                {
                    ChunkHelpers.SaveChunkToFile(chunkData.blocks, moon, chunkData.globalPosX, chunkData.globalPosZ);
                    Destroy(chunk);
                }
                else if (chunkData.globalPosX == playerChunkX + newRenderDistance + 1 || chunkData.globalPosX == playerChunkX - newRenderDistance - 1
                      || chunkData.globalPosZ == playerChunkZ + newRenderDistance + 1 || chunkData.globalPosZ == playerChunkZ - newRenderDistance - 1)
                {
                    Transform[] childBlocks = new Transform[chunk.transform.childCount];
                    for (int j = 0; j < chunk.transform.childCount; j++)
                        childBlocks[j] = chunk.transform.GetChild(j);

                    foreach (Transform childBlock in childBlocks)
                        Destroy(childBlock.gameObject);

                    ChunkHelpers.SaveChunkToFile(chunkData.blocks, moon, chunkData.globalPosX, chunkData.globalPosZ);
                }
            }
        }
        else if (newRenderDistance > renderDistance)
        {
            //rowRenderTasks.Clear(); // BAD IDEA
            StartCoroutine(ExpandRenderDistance(renderDistance, newRenderDistance));
        }
        renderDistance = newRenderDistance;

        //renderDistance = OptionsManager.GetCurrentOptions().renderDistance;

        // Brown mob exploding
        if (brownMobExploded)
        {
            Vector3 spherePos = brownMobExplodePos + 2F * Vector3.down;

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
                        if (x * x + y * y + z * z <= 3 * 3)
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
                (int hitBlockPosX, int hitBlockPosY, int hitBlockPosZ) = ChunkHelpers.GetLocalBlockPos((int)hitBlockGlobalPos.x, (int)hitBlockGlobalPos.y, (int)hitBlockGlobalPos.z);
                BlockID hitBlockID = (BlockID)parentChunkData.blocks[ChunkHelpers.GetChunkIndex(hitBlockPosX, hitBlockPosY, hitBlockPosZ)];

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
                        parentChunkData.blocks[ChunkHelpers.GetChunkIndex(hitBlockPosX, hitBlockPosY, hitBlockPosZ)] = (byte)BlockID.air;

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
                        (int neighborPosX, int neighborPosY, int neighborPosZ) = ChunkHelpers.GetLocalBlockPos((int)neighborGlobalPos.x, (int)neighborGlobalPos.y, (int)neighborGlobalPos.z);
                        BlockID neighborBlock = (BlockID)neighborChunkData.blocks[ChunkHelpers.GetChunkIndex(neighborPosX, neighborPosY, neighborPosZ)];
                        if (neighborBlock != BlockID.air)
                        {
                            blocksBroughtBack.Add(neighborGlobalPos);
                            //SpawnBlock(neighborBlock, neighborChunkData.gameObject, neighborLocalPos, new Vector3(neighborChunkX, 0, neighborChunkZ));
                            SpawnBlock(neighborChunkData.gameObject, neighborChunkX, neighborChunkZ, neighborBlock, neighborPosX, neighborPosY, neighborPosZ);
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
            if (selectedBlockData.localPosY != 0)
            {
                ChunkData selectedBlockChunkData = selectedBlock.transform.parent.GetComponent<ChunkData>();
                selectedBlockChunkData.blocks[ChunkHelpers.GetChunkIndex(selectedBlockData.localPosX, selectedBlockData.localPosY, selectedBlockData.localPosZ)] = (byte)BlockID.air;

                // Drop block item
                selectedBlock.transform.SetParent(null);
                if (selectedBlockData.blockID == BlockID.topsoil)
                    selectedBlock.GetComponent<Renderer>().material.SetTexture("_BaseTexture", Resources.Load<Texture2D>("Textures/Blocks/dirt"));
                Dropped droppedScript = selectedBlock.AddComponent<Dropped>();
                if (selectedBlockData.blockID.ToString().StartsWith("minilight"))
                    droppedScript.itemID = ItemID.minilight;
                else
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
                        (int neighborPosX, int neighborPosY, int neighborPosZ) = ChunkHelpers.GetLocalBlockPos((int)neighborGlobalPos.x, (int)neighborGlobalPos.y, (int)neighborGlobalPos.z);
                        BlockID neighborBlock = (BlockID)neighborChunkData.blocks[ChunkHelpers.GetChunkIndex(neighborPosX, neighborPosY, neighborPosZ)];
                        if (neighborBlock != BlockID.air)
                            SpawnBlock(neighborChunkData.gameObject, neighborChunkX, neighborChunkZ, neighborBlock, neighborPosX, neighborPosY, neighborPosZ);
                    }
                }

                // Remove block object
                player.ResetDestroyedBlock();
            }
        }

        // Mob spawning (temporary)
        if (player.SpawnedMob())
        {
            RaycastHit hit;
            if (Physics.Raycast(camera.transform.position, camera.transform.forward, out hit, Mathf.Infinity, LayerMask.GetMask("Block")))
            {
                Vector3 selectedBlockPos = player.GetSelectedBlock().transform.position;
                Vector3 faceNormal = hit.normal;
                ItemID mobEgg = player.GetSelectedItem();
                if (mobEgg == ItemID.green_mob_egg)
                {
                    Vector3 newBlockPos = selectedBlockPos + faceNormal;
                    Instantiate(greenMobPrefab, newBlockPos, Quaternion.identity);
                }
                else if (mobEgg == ItemID.brown_mob_egg)
                {
                    Vector3 newBlockPos = selectedBlockPos + faceNormal;
                    Instantiate(brownMobPrefab, newBlockPos, Quaternion.identity);
                }
                else if (mobEgg == ItemID.space_giraffe_egg)
                {
                    Vector3 newBlockPos = selectedBlockPos + faceNormal + 2 * Vector3.up;
                    Instantiate(spaceGiraffePrefab, newBlockPos, Quaternion.identity);
                }
            }

            player.ResetSpawnedMob();
        }

        // Block placing
        if (player.PlacedBlock())
        {
            ItemID selectedItem = player.GetSelectedItem();
            if (Enum.TryParse(selectedItem.ToString(), out BlockID selectedBlockID) || selectedItem.ToString().StartsWith("minilight")) // Item is a block
            {
                RaycastHit hit;
                if (Physics.Raycast(camera.transform.position, camera.transform.forward, out hit, Mathf.Infinity, LayerMask.GetMask("Block")))
                {
                    Vector3 selectedBlockPos = player.GetSelectedBlock().transform.position;
                    Vector3 faceNormal = hit.normal;
                    Vector3 newBlockPos = selectedBlockPos + faceNormal;

                    if (!((faceNormal == Vector3.up && newBlockPos.y == GameData.WORLD_HEIGHT_LIMIT)))
                    {
                        // Check if block is already there
                        // The player can see through corners if they try, so it's technically possible to select a hidden face
                        if ((Physics.OverlapBox(newBlockPos, new Vector3(0.5F, 0.5F, 0.5F), Quaternion.identity, LayerMask.GetMask("Block"))).Length == 0)
                        {
                            GameObject blockObj;
                            BlockData blockData;

                            if (selectedItem.ToString().StartsWith("minilight"))
                            {
                                if (faceNormal == Vector3.forward) // minilight_nz
                                {
                                    blockObj = Instantiate(minilight_nz);
                                    blockObj.transform.position = new Vector3(newBlockPos.x, newBlockPos.y, newBlockPos.z - 0.485F);

                                    blockData = blockObj.AddComponent<BlockData>();
                                    blockData.blockID = BlockID.minilight_nz;
                                }
                                else if (faceNormal == Vector3.back) // minilight_pz
                                {
                                    blockObj = Instantiate(minilight_pz);
                                    blockObj.transform.position = new Vector3(newBlockPos.x, newBlockPos.y, newBlockPos.z + 0.485F);

                                    blockData = blockObj.AddComponent<BlockData>();
                                    blockData.blockID = BlockID.minilight_pz;
                                }
                                else if (faceNormal == Vector3.right) // minilight_nx
                                {
                                    blockObj = Instantiate(minilight_nx);
                                    blockObj.transform.position = new Vector3(newBlockPos.x - 0.485F, newBlockPos.y, newBlockPos.z);

                                    blockData = blockObj.AddComponent<BlockData>();
                                    blockData.blockID = BlockID.minilight_nx;
                                }
                                else // minilight_px (Vector3.left)
                                {
                                    blockObj = Instantiate(minilight_px);
                                    blockObj.transform.position = new Vector3(newBlockPos.x + 0.485F, newBlockPos.y, newBlockPos.z);

                                    blockData = blockObj.AddComponent<BlockData>();
                                    blockData.blockID = BlockID.minilight_px;
                                }
                            }
                            else
                            {
                                blockObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                blockObj.hideFlags = HideFlags.HideInHierarchy;
                                blockObj.layer = LayerMask.NameToLayer("Block");
                                blockObj.GetComponent<MeshFilter>().mesh = blockMesh;
                                blockObj.GetComponent<Renderer>().material = blockMaterials[(int)selectedBlockID];
                                blockObj.GetComponent<BoxCollider>().size = new Vector3(0.98F, 0.98F, 0.98F);
                                blockObj.transform.position = new Vector3(newBlockPos.x, newBlockPos.y, newBlockPos.z);

                                blockData = blockObj.AddComponent<BlockData>();
                                blockData.blockID = selectedBlockID;
                            }

                            (int localBlockPosX, int localBlockPosY, int localBlockPosZ) = ChunkHelpers.GetLocalBlockPos(blockObj.transform.position);
                            blockData.localPosX = localBlockPosX;
                            blockData.localPosY = localBlockPosY;
                            blockData.localPosZ = localBlockPosZ;

                            if (selectedBlockID == BlockID.light)
                            {
                                Light light = blockObj.AddComponent<Light>();
                                light.type = LightType.Point;
                            }

                            // Update parent chunk
                            (int blockChunkX, int blockChunkZ) = ChunkHelpers.GetChunkCoordsByBlockPos(blockObj.transform.position);
                            GameObject parentChunk = chunkParent.transform.Find($"Chunk ({blockChunkX},{blockChunkZ})").gameObject;
                            blockObj.transform.parent = parentChunk.transform;
                            parentChunk.GetComponent<ChunkData>().blocks[ChunkHelpers.GetChunkIndex(blockData.localPosX, blockData.localPosY, blockData.localPosZ)] = (byte)blockData.blockID;

                            player.DecrementSelectedItem();
                        }
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
            task.newPlayerChunkX = newPlayerChunkX;
            task.newPlayerChunkZ = newPlayerChunkZ;
            rowRenderTasks.Enqueue(task);

            // Update distance traveled
            player.distanceTraveledThisSession += Mathf.Pow((GameData.CHUNK_SIZE / 1000F), 2) * (2*renderDistance + 1); // km^2
        }

        if (newPlayerChunkZ != playerChunkZ)
        {
            RowRenderTask task = new RowRenderTask();
            task.finished = false;
            task.direction = (newPlayerChunkZ - playerChunkZ == 1) ? XZDirection.POSITIVE_Z : XZDirection.NEGATIVE_Z;
            task.newPlayerChunkX = newPlayerChunkX;
            task.newPlayerChunkZ = newPlayerChunkZ;
            rowRenderTasks.Enqueue(task);

            // Update distance traveled
            player.distanceTraveledThisSession += Mathf.Pow((GameData.CHUNK_SIZE / 1000F), 2) * (2*renderDistance + 1); // km^2
        }

        playerChunkX = newPlayerChunkX;
        playerChunkZ = newPlayerChunkZ;

        if ((currentRowRenderTask == null || currentRowRenderTask.finished) && rowRenderTasks.Count > 0)
        {
            currentRowRenderTask = rowRenderTasks.Dequeue();
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
        for (int chunkX = playerChunkX - newRenderDistance - 1; chunkX <= playerChunkX + newRenderDistance + 1; chunkX++)
        {
            for (int chunkZ = playerChunkZ - newRenderDistance - 1; chunkZ <= playerChunkZ + newRenderDistance + 1; chunkZ++)
            {
                if (chunkX < playerChunkX - oldRenderDistance - 1 || chunkX > playerChunkX + oldRenderDistance + 1
                 || chunkZ < playerChunkZ - oldRenderDistance - 1 || chunkZ > playerChunkZ + oldRenderDistance + 1)
                {
                    GameObject chunk = new GameObject($"Chunk ({chunkX},{chunkZ})");
                    chunk.transform.SetParent(chunkParent.transform);
                    chunk.tag = "Chunk";
                    ChunkData chunkData = chunk.AddComponent<ChunkData>();
                    chunkData.globalPosX = chunkX;
                    chunkData.globalPosZ = chunkZ;
                    chunkData.blocks = new byte[GameData.CHUNK_SIZE * GameData.CHUNK_SIZE * GameData.WORLD_HEIGHT_LIMIT];
                    if (!ChunkHelpers.ChunkFileExists(moon, chunkX, chunkZ))
                    {
                        ChunkHelpers.GenerateChunk(chunkData.blocks, chunkX, chunkZ, moonData);
                        ChunkHelpers.SaveChunkToFile(chunkData.blocks, moon, chunkX, chunkZ);
                    }
                    else
                    {
                        ChunkHelpers.GetChunkFromFile(chunkData.blocks, moon, chunkX, chunkZ);
                    }
                }
            }
        }

        // Render
        ChunkData[] adjacentChunkData;
        for (int chunkX = playerChunkX - newRenderDistance; chunkX <= playerChunkX + newRenderDistance; chunkX++)
        {
            for (int chunkZ = playerChunkZ - newRenderDistance; chunkZ <= playerChunkZ + newRenderDistance; chunkZ++)
            {
                if (
                    Mathf.Abs(chunkX - playerChunkX) < newRenderDistance + 1 && Mathf.Abs(chunkZ - playerChunkZ) < newRenderDistance + 1
                && (Mathf.Abs(chunkX - playerChunkX) > oldRenderDistance || Mathf.Abs(chunkZ - playerChunkZ) > oldRenderDistance))
                {
                    GameObject chunkObj = chunkParent.transform.Find($"Chunk ({chunkX},{chunkZ})").gameObject;
                    ChunkData chunkData = chunkObj.GetComponent<ChunkData>();
                    adjacentChunkData = ChunkHelpers.GetAdjacentChunkData(chunkParent, chunkX, chunkZ);
                    for (int localBlockX = 0; localBlockX < GameData.CHUNK_SIZE; localBlockX++)
                    {
                        for (int localBlockZ = 0; localBlockZ < GameData.CHUNK_SIZE; localBlockZ++)
                        {
                            for (int localBlockY = 0; localBlockY < GameData.WORLD_HEIGHT_LIMIT; localBlockY++)
                            {
                                BlockID block = (BlockID)chunkData.blocks[ChunkHelpers.GetChunkIndex(localBlockX, localBlockY, localBlockZ)];
                                if (block != BlockID.air && ChunkHelpers.BlockShouldBeRendered(block, adjacentChunkData, chunkData, localBlockX, localBlockY, localBlockZ))
                                    SpawnBlock(chunkObj, chunkX, chunkZ, block, localBlockX, localBlockY, localBlockZ);
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
        int playerPosY = (int)player.transform.position.y;
        XZDirection moveDirection = currentRowRenderTask.direction;

        var stopWatch = new System.Diagnostics.Stopwatch();
        stopWatch.Start();

        if (moveDirection == XZDirection.POSITIVE_X || moveDirection == XZDirection.NEGATIVE_X) // Moving along x axis
        {
            int sign = moveDirection == XZDirection.POSITIVE_X ? 1 : -1;
            int chunkX = playerChunkX + sign * renderDistance;
            int borderChunkX = chunkX + sign;

            // Unload old chunks
            for (int chunkZ = playerChunkZ - renderDistance - 1; chunkZ <= playerChunkZ + renderDistance + 1; chunkZ++)
            {
                // Destroy distant chunk and bring it back to hold block data
                GameObject distantRenderedChunk = chunkParent.transform.Find($"Chunk ({playerChunkX - sign * (renderDistance + 1)},{chunkZ})").gameObject;
                if (distantRenderedChunk != null)
                {
                    ChunkData distantRenderedChunkData = distantRenderedChunk.GetComponent<ChunkData>();
                    ChunkHelpers.SaveChunkToFile(distantRenderedChunkData.blocks, moon, distantRenderedChunkData.globalPosX, distantRenderedChunkData.globalPosZ);
                    Destroy(distantRenderedChunk);

                    GameObject newDistantRenderedChunk = new GameObject($"Chunk ({distantRenderedChunkData.globalPosX},{distantRenderedChunkData.globalPosZ})");
                    newDistantRenderedChunk.transform.SetParent(chunkParent.transform);
                    newDistantRenderedChunk.tag = "Chunk";
                    ChunkData newDistantRenderedChunkData = newDistantRenderedChunk.AddComponent<ChunkData>();
                    newDistantRenderedChunkData.globalPosX = distantRenderedChunkData.globalPosX;
                    newDistantRenderedChunkData.globalPosZ = distantRenderedChunkData.globalPosZ;
                    newDistantRenderedChunkData.blocks = distantRenderedChunkData.blocks;
                    yield return null;
                }

                // Create border chunk objects with block data
                GameObject borderChunk = new GameObject($"Chunk ({borderChunkX},{chunkZ})");
                borderChunk.transform.SetParent(chunkParent.transform);
                borderChunk.tag = "Chunk";
                ChunkData borderChunkData = borderChunk.AddComponent<ChunkData>();
                borderChunkData.globalPosX = borderChunkX;
                borderChunkData.globalPosZ = chunkZ;
                borderChunkData.blocks = new byte[GameData.CHUNK_SIZE * GameData.CHUNK_SIZE * GameData.WORLD_HEIGHT_LIMIT];
                if (!ChunkHelpers.ChunkFileExists(moon, borderChunkX, chunkZ))
                {
                    ChunkHelpers.GenerateChunk(borderChunkData.blocks, borderChunkX, chunkZ, moonData);
                }
                else
                {
                    ChunkHelpers.GetChunkFromFile(borderChunkData.blocks, moon, borderChunkX, chunkZ);
                }
                yield return null;
            }

            // Render new chunks
            ChunkData[] adjacentChunkData;
            for (int chunkZ = playerChunkZ - renderDistance; chunkZ <= playerChunkZ + renderDistance; chunkZ++)
            {
                GameObject chunk = chunkParent.transform.Find($"Chunk ({chunkX},{chunkZ})").gameObject;
                ChunkData chunkData = chunk.GetComponent<ChunkData>();
                adjacentChunkData = ChunkHelpers.GetAdjacentChunkData(chunkParent, chunkX, chunkZ);

                // Find highest starting level for y loop
                int centerBlockX = (int)(GameData.CHUNK_SIZE / 2);
                int centerBlockZ = (int)(GameData.CHUNK_SIZE / 2);
                bool hasAstronautLair = chunkData.blocks[ChunkHelpers.GetChunkIndex(centerBlockX, 0, centerBlockZ)] == (byte)BlockID.gravel;
                int yStart = Mathf.Min(hasAstronautLair ? 38 : 55, playerPosY - 2);

                int chunkIndex = yStart;
                for (int localBlockX = 0; localBlockX < GameData.CHUNK_SIZE; localBlockX++)
                {
                    for (int localBlockZ = 0; localBlockZ < GameData.CHUNK_SIZE; localBlockZ++)
                    {
                        for (int localBlockY = yStart; localBlockY < GameData.WORLD_HEIGHT_LIMIT; localBlockY++)
                        {
                            BlockID block = (BlockID)chunkData.blocks[chunkIndex++];
                            if (block != BlockID.air)
                            {
                                bool shouldBeRendered;
                                if (block == BlockID.topsoil)
                                    shouldBeRendered = true;
                                else if (localBlockX > 0 && localBlockX < GameData.CHUNK_SIZE - 1 && localBlockZ > 0 && localBlockZ < GameData.CHUNK_SIZE - 1)
                                    shouldBeRendered = ChunkHelpers.BlockShouldBeRenderedINNER(block, chunkData, localBlockX, localBlockY, localBlockZ);
                                else
                                    shouldBeRendered = ChunkHelpers.BlockShouldBeRendered(block, adjacentChunkData, chunkData, localBlockX, localBlockY, localBlockZ);

                                if (shouldBeRendered)
                                    SpawnBlock(chunk, chunkX, chunkZ, block, localBlockX, localBlockY, localBlockZ);
                            }
                        }
                        chunkIndex += yStart;
                    }
                    yield return null;
                }
            }

            // Spawn mobs
            for (int chunkZ = playerChunkZ - renderDistance; chunkZ <= playerChunkZ + renderDistance; chunkZ++)
            {
                float[] mobData = MobHelpers.GetMobsInChunk(moon, chunkX, chunkZ);
                if (mobData != null)
                {
                    for (int i = 0; i < mobData.Length; i += 6)
                    {
                        Vector3 mobPosition = new Vector3(
                            mobData[i + 2],
                            mobData[i + 3],
                            mobData[i + 4]
                        );

                        Quaternion mobRotation = Quaternion.Euler(
                            0,
                            mobData[i + 5],
                            0
                        );

                        if (mobData[i] == 0) // Green mob
                        {
                            GameObject mob = Instantiate(greenMobPrefab, mobPosition, mobRotation, null);
                            mob.tag = "Mob";
                        }
                        else if (mobData[i] == 1) // Brown mob
                        {
                            GameObject mob = Instantiate(brownMobPrefab, mobPosition, mobRotation, null);
                            mob.GetComponent<BrownMob>().aggressive = (mobData[i + 1] == 1);
                            mob.tag = "Mob";
                        }
                        else if (mobData[i] == 2) // Space giraffe
                        {
                            GameObject mob = Instantiate(spaceGiraffePrefab, mobPosition, mobRotation, null);
                            mob.tag = "Mob";
                        }
                    }
                }
            }
        }
        else // Moving along z axis
        {
            int sign = moveDirection == XZDirection.POSITIVE_Z ? 1 : -1;
            int chunkZ = playerChunkZ + sign * renderDistance;
            int borderChunkZ = chunkZ + sign;

            for (int chunkX = playerChunkX - renderDistance - 1; chunkX <= playerChunkX + renderDistance + 1; chunkX++)
            {
                GameObject distantRenderedChunk = chunkParent.transform.Find($"Chunk ({chunkX},{playerChunkZ - sign * (renderDistance + 1)})").gameObject;
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
                    yield return null;
                }

                // Create border chunk objects with block data
                GameObject borderChunk = new GameObject($"Chunk ({chunkX},{borderChunkZ})");
                borderChunk.transform.SetParent(chunkParent.transform);
                borderChunk.tag = "Chunk";
                ChunkData borderChunkData = borderChunk.AddComponent<ChunkData>();
                borderChunkData.globalPosX = chunkX;
                borderChunkData.globalPosZ = borderChunkZ;
                borderChunkData.blocks = new byte[GameData.CHUNK_SIZE * GameData.CHUNK_SIZE * GameData.WORLD_HEIGHT_LIMIT];
                if (!ChunkHelpers.ChunkFileExists(moon, chunkX, borderChunkZ))
                {
                    ChunkHelpers.GenerateChunk(borderChunkData.blocks, chunkX, borderChunkZ, moonData);
                }
                else
                {
                    ChunkHelpers.GetChunkFromFile(borderChunkData.blocks, moon, chunkX, borderChunkZ);
                }
                yield return null;
            }

            // Render new chunks
            ChunkData[] adjacentChunkData;
            for (int chunkX = playerChunkX - renderDistance; chunkX <= playerChunkX + renderDistance; chunkX++)
            {
                GameObject chunk = chunkParent.transform.Find($"Chunk ({chunkX},{chunkZ})").gameObject;
                ChunkData chunkData = chunk.GetComponent<ChunkData>();
                adjacentChunkData = ChunkHelpers.GetAdjacentChunkData(chunkParent, chunkX, chunkZ);

                // Find highest starting level for y loop
                int centerBlockX = (int)(GameData.CHUNK_SIZE / 2);
                int centerBlockZ = (int)(GameData.CHUNK_SIZE / 2);
                bool hasAstronautLair = chunkData.blocks[ChunkHelpers.GetChunkIndex(centerBlockX, 0, centerBlockZ)] == (byte)BlockID.gravel;
                int yStart = Mathf.Min(hasAstronautLair ? 38 : 55, playerPosY - 2);

                int chunkIndex = yStart;
                for (int localBlockX = 0; localBlockX < GameData.CHUNK_SIZE; localBlockX++)
                {
                    for (int localBlockZ = 0; localBlockZ < GameData.CHUNK_SIZE; localBlockZ++)
                    {
                        for (int localBlockY = yStart; localBlockY < GameData.WORLD_HEIGHT_LIMIT; localBlockY++)
                        {
                            BlockID block = (BlockID)chunkData.blocks[chunkIndex++];
                            if (block != BlockID.air)
                            {
                                bool shouldBeRendered;
                                if (block == BlockID.topsoil)
                                    shouldBeRendered = true;
                                else if (localBlockX > 0 && localBlockX < GameData.CHUNK_SIZE - 1 && localBlockZ > 0 && localBlockZ < GameData.CHUNK_SIZE - 1)
                                    shouldBeRendered = ChunkHelpers.BlockShouldBeRenderedINNER(block, chunkData, localBlockX, localBlockY, localBlockZ);
                                else
                                    shouldBeRendered = ChunkHelpers.BlockShouldBeRendered(block, adjacentChunkData, chunkData, localBlockX, localBlockY, localBlockZ);

                                if (shouldBeRendered)
                                    SpawnBlock(chunk, chunkX, chunkZ, block, localBlockX, localBlockY, localBlockZ);
                            }
                        }
                        chunkIndex += yStart;
                    }
                    yield return null;
                }
            }

            // Spawn mobs
            for (int chunkX = playerChunkX - renderDistance; chunkX <= playerChunkX + renderDistance; chunkX++)
            {
                float[] mobData = MobHelpers.GetMobsInChunk(moon, chunkX, chunkZ);
                if (mobData != null)
                {
                    for (int i = 0; i < mobData.Length; i += 6)
                    {
                        Vector3 mobPosition = new Vector3(
                            mobData[i + 2],
                            mobData[i + 3],
                            mobData[i + 4]
                        );

                        Quaternion mobRotation = Quaternion.Euler(
                            0,
                            mobData[i + 5],
                            0
                        );

                        if (mobData[i] == 0) // Green mob
                        {
                            GameObject mob = Instantiate(greenMobPrefab, mobPosition, mobRotation, null);
                            mob.tag = "Mob";
                        }
                        else if (mobData[i] == 1) // Brown mob
                        {
                            GameObject mob = Instantiate(brownMobPrefab, mobPosition, mobRotation, null);
                            mob.GetComponent<BrownMob>().aggressive = (mobData[i + 1] == 1);
                            mob.tag = "Mob";
                        }
                        else if (mobData[i] == 2) // Space giraffe
                        {
                            GameObject mob = Instantiate(spaceGiraffePrefab, mobPosition, mobRotation, null);
                            mob.tag = "Mob";
                        }
                    }
                }
            }
        }

        currentRowRenderTask.finished = true;

        stopWatch.Stop();
        chunkGenerationRate = (float)(2 * renderDistance + 1) / stopWatch.Elapsed.Seconds;
    }

    private void SpawnBlock(GameObject parentChunk, int chunkX, int chunkZ, BlockID blockID, int localBlockX, int localBlockY, int localBlockZ)
    {
        Vector3 globalPosition = new Vector3(
            (chunkX * GameData.CHUNK_SIZE + localBlockX), localBlockY, (chunkZ * GameData.CHUNK_SIZE + localBlockZ)
        );
        GameObject blockObj = Instantiate(blockPrefab, globalPosition, Quaternion.identity, parentChunk.transform);
        blockObj.GetComponent<Renderer>().material = blockMaterials[(int)blockID];

        BlockData blockData = blockObj.GetComponent<BlockData>();
        blockData.blockID = blockID;
        blockData.localPosX = localBlockX;
        blockData.localPosY = localBlockY;
        blockData.localPosZ = localBlockZ;

        if (blockID == BlockID.light)
        {
            Light light = blockObj.AddComponent<Light>();
            light.type = LightType.Point;
        }
    }

    public float GetChunkGenerationRate()
    {
        return chunkGenerationRate;
    }

    public int GetRenderDistance()
    {
        return renderDistance;
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
