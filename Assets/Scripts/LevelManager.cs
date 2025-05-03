using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.IO;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Runtime.Serialization.Formatters.Binary;

enum LoadingState {NONE, GENERATING, DONE_GENERATING};

public class LevelManager : MonoBehaviour
{
    public List<GameObject> moons;
    public GameObject moonSettingsMenu;
    public GameObject loadingMenu;
    private Mesh blockMesh;
    private List<Material> blockMaterials;

    private LoadingState loadingState = LoadingState.NONE;
    private int totalProgressCount = 0;
    private int currentProgressCount = 0;

    // Mob prefabs
    public GameObject greenMobPrefab;
    public GameObject brownMobPrefab;
    public GameObject spaceGiraffePrefab;

    public GameObject minilight_pz;
    public GameObject minilight_nz;
    public GameObject minilight_px;
    public GameObject minilight_nx;

    void Awake()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        string moonsFolder = string.Format("{0}/moons", Application.persistentDataPath);
        if (!Directory.Exists(moonsFolder))
            Directory.CreateDirectory(moonsFolder);

        for (int moon = 0; moon < 4; moon++)
        {
            string moonName = moons[moon].name;
            string moonFolder = string.Format("{0}/moons/moon{1}", Application.persistentDataPath, moon);
            string chunkFolder = string.Format("{0}/moons/moon{1}/chunks", Application.persistentDataPath, moon);

            if (!Directory.Exists(moonFolder))
                Directory.CreateDirectory(moonFolder);

            if (!Directory.Exists(chunkFolder))
                Directory.CreateDirectory(chunkFolder);

            string moonDataFile = string.Format("{0}/moons/moon{1}/moon.dat", Application.persistentDataPath, moon);
            if (!File.Exists(moonDataFile))
            {
                moons[moon].transform.Find("Text").GetComponent<Text>().text = moonName + " -Unexplored-";
            }
            else
            {
                MoonData moonData;
                using (FileStream file = File.Open(moonDataFile, FileMode.Open))
                    moonData = (MoonData)(new BinaryFormatter()).Deserialize(file);
                if (moonData.distanceTraveled == 0)
                    moons[moon].transform.Find("Text").GetComponent<Text>().text = $"{moonName} - 0 SQ KM";
                else
                    moons[moon].transform.Find("Text").GetComponent<Text>().text = $"{moonName} - {moonData.distanceTraveled:F2} SQ KM";
            }
        }

        // Prepare materials
        var blocks = Enum.GetValues(typeof(BlockID));
        int numberOfBlocks = blocks.Length;
        blockMaterials = MeshData.GetBlockMaterials();
        blockMesh = MeshData.GetBlockMesh();

        // Delete chunks that may have persisted from Game scene (marked with DontDestroyOnLoad() from below)
        GameObject chunkParent = GameObject.Find("ChunkParent");
        if (chunkParent != null)
            Destroy(chunkParent);
    }

    public void Update()
    {
        if (loadingState == LoadingState.GENERATING)
        {
            Slider progressBar = GameObject.Find("Progress Bar").GetComponent<Slider>();
            progressBar.value = (float)currentProgressCount / totalProgressCount;
        }
        else if (loadingState == LoadingState.DONE_GENERATING)
        {
            GameObject.Find("Generating").GetComponent<Text>().text = "Entering moon...";
            SceneManager.LoadScene("Game");
        }
    }

    public void LoadMoon(int moon)
    {
        PlayerPrefs.SetInt("moon", moon);
        string moonDataFile = string.Format("{0}/moons/moon{1}/moon.dat", Application.persistentDataPath, moon);

        // TODO: Save options

        if (!File.Exists(moonDataFile))
        {
            moonSettingsMenu.SetActive(true);
        }
        else
        {
            MoonData moonData;
            using (FileStream file = File.Open(moonDataFile, FileMode.Open))
                moonData = (MoonData)(new BinaryFormatter()).Deserialize(file);
            GenerateMoon(moon, moonData, false);
        }
    }

    public void GenerateMoon(int moon, MoonData moonData, bool newMoon)
    {
        moonSettingsMenu.SetActive(false);
        loadingMenu.SetActive(true);
        if (!newMoon)
            GameObject.Find("Generating").GetComponent<Text>().text = "Loading moon...";

        StartCoroutine(_GenerateMoon(moon, moonData));
    }

    private IEnumerator _GenerateMoon(int moon, MoonData moonData)
    {
        int renderDistance = OptionsManager.GetCurrentOptions().renderDistance;
        //int renderDistance = 1;
        totalProgressCount = GameData.CHUNK_SIZE*(2*renderDistance + 1)*(2*renderDistance + 1);
        loadingState = LoadingState.GENERATING;

        int playerChunkX, playerChunkZ;
        string playerDataPath = string.Format("{0}/moons/moon{1}/player.dat", Application.persistentDataPath, moon);
        if (!File.Exists(playerDataPath))
        {
            playerChunkX = playerChunkZ = 0;
        }
        else
        {
            PlayerData playerData;
            using (FileStream fileStream = new FileStream(playerDataPath, FileMode.Open, FileAccess.Read))
                playerData = (PlayerData)(new BinaryFormatter()).Deserialize(fileStream);

            playerChunkX = Mathf.FloorToInt(playerData.positionX / GameData.CHUNK_SIZE);
            playerChunkZ = Mathf.FloorToInt(playerData.positionZ / GameData.CHUNK_SIZE);
        }

        GameObject chunkParent = new GameObject("ChunkParent");
        DontDestroyOnLoad(chunkParent);

        // Create chunk objects (have to exist before calling ChunkHelpers.GetAdjacentChunkData())
        int chunkCount = 0;
        for (int chunkX = playerChunkX - renderDistance - 1; chunkX <= playerChunkX + renderDistance + 1; chunkX++)
        {
            for (int chunkZ = playerChunkZ - renderDistance - 1; chunkZ <= playerChunkZ + renderDistance + 1; chunkZ++)
            {
                GameObject chunk = new GameObject($"Chunk ({chunkX},{chunkZ})");
                chunk.transform.SetParent(chunkParent.transform);
                DontDestroyOnLoad(chunk); // Must persist into Game scene
                chunk.tag = "Chunk";
                ChunkData chunkData = chunk.AddComponent<ChunkData>();
                chunkData.globalPosX = chunkX;
                chunkData.globalPosZ = chunkZ;
                chunkData.blocks = new BlockID[GameData.CHUNK_SIZE, GameData.WORLD_HEIGHT_LIMIT, GameData.CHUNK_SIZE];
                if (ChunkHelpers.ChunkFileExists(moon, chunkX, chunkZ))
                {
                    ChunkHelpers.GetChunkFromFile(chunkData.blocks, moon, chunkX, chunkZ);
                }
                else
                {
                    ChunkHelpers.GenerateChunk(chunkData.blocks, chunkX, chunkZ, moonData);
                    ChunkHelpers.SaveChunkToFile(chunkData.blocks, moon, chunkX, chunkZ);
                }
                chunkCount++;
                yield return null;
            }
        }

        ChunkData[] adjacentChunkData;
        for (int chunkX = playerChunkX - renderDistance; chunkX <= playerChunkX + renderDistance; chunkX++)
        {
            for (int chunkZ = playerChunkZ - renderDistance; chunkZ <= playerChunkZ + renderDistance; chunkZ++)
            {
                // Instantiate blocks
                GameObject chunk = chunkParent.transform.Find($"Chunk ({chunkX},{chunkZ})").gameObject;
                ChunkData chunkData = chunk.GetComponent<ChunkData>();
                adjacentChunkData = ChunkHelpers.GetAdjacentChunkData(chunkParent, chunkX, chunkZ);

                for (int localBlockX = 0; localBlockX < GameData.CHUNK_SIZE; localBlockX++)
                {
                    for (int localBlockY = 0; localBlockY < GameData.WORLD_HEIGHT_LIMIT; localBlockY++)
                    {
                        for (int localBlockZ = 0; localBlockZ < GameData.CHUNK_SIZE; localBlockZ++)
                        {
                            BlockID block = chunkData.blocks[localBlockX, localBlockY, localBlockZ];
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
                                {
                                    GameObject blockObj;
                                    BlockData blockData;

                                    if (block.ToString().StartsWith("minilight"))
                                    {
                                        if (block == BlockID.minilight_nz) // minilight_nz
                                        {
                                            blockObj = Instantiate(minilight_nz);
                                            //blockObj.transform.position = new Vector3(newBlockPos.x, newBlockPos.y, newBlockPos.z - 0.485F);
                                            blockObj.transform.position = new Vector3(
                                                (chunkX*GameData.CHUNK_SIZE + localBlockX), localBlockY, (chunkZ*GameData.CHUNK_SIZE + localBlockZ - 0.485F)
                                            );
                                        }
                                        else if (block == BlockID.minilight_pz) // minilight_pz
                                        {
                                            blockObj = Instantiate(minilight_pz);
                                            //blockObj.transform.position = new Vector3(newBlockPos.x, newBlockPos.y, newBlockPos.z + 0.485F);
                                            blockObj.transform.position = new Vector3(
                                                (chunkX*GameData.CHUNK_SIZE + localBlockX), localBlockY, (chunkZ*GameData.CHUNK_SIZE + localBlockZ + 0.485F)
                                            );
                                        }
                                        else if (block == BlockID.minilight_nx) // minilight_nx
                                        {
                                            blockObj = Instantiate(minilight_nx);
                                            //blockObj.transform.position = new Vector3(newBlockPos.x - 0.485F, newBlockPos.y, newBlockPos.z);
                                            blockObj.transform.position = new Vector3(
                                                (chunkX*GameData.CHUNK_SIZE + localBlockX - 0.485F), localBlockY, (chunkZ*GameData.CHUNK_SIZE + localBlockZ)
                                            );
                                        }
                                        else // minilight_px (Vector3.left)
                                        {
                                            blockObj = Instantiate(minilight_px);
                                            //blockObj.transform.position = new Vector3(newBlockPos.x + 0.485F, newBlockPos.y, newBlockPos.z);
                                            blockObj.transform.position = new Vector3(
                                                (chunkX*GameData.CHUNK_SIZE + localBlockX + 0.485F), localBlockY, (chunkZ*GameData.CHUNK_SIZE + localBlockZ)
                                            );
                                        }
                                    }
                                    else
                                    {
                                        blockObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                        blockObj.hideFlags = HideFlags.HideInHierarchy;
                                        blockObj.layer = LayerMask.NameToLayer("Block");
                                        blockObj.GetComponent<MeshFilter>().mesh = blockMesh;
                                        blockObj.GetComponent<Renderer>().material = blockMaterials[(int)block];
                                        blockObj.GetComponent<BoxCollider>().size = new Vector3(0.98F, 0.98F, 0.98F);
                                        //blockObj.transform.position = new Vector3(newBlockPos.x, newBlockPos.y, newBlockPos.z);
                                        blockObj.transform.position = new Vector3(
                                            (chunkX*GameData.CHUNK_SIZE + localBlockX), localBlockY, (chunkZ*GameData.CHUNK_SIZE + localBlockZ)
                                        );
                                    }

                                    blockData = blockObj.AddComponent<BlockData>();
                                    blockData.blockID = block;
                                    blockData.localPosX = localBlockX;
                                    blockData.localPosY = localBlockY;
                                    blockData.localPosZ = localBlockZ;

                                    blockObj.transform.SetParent(chunk.transform);

                                    if (block == BlockID.light)
                                    {
                                        Light light = blockObj.AddComponent<Light>();
                                        light.type = LightType.Point;
                                    }
                                }
                            }
                        }
                    }
                    currentProgressCount++;
                    yield return null;
                }
            }
        }

        // Spawn mobs
        for (int chunkX = playerChunkX - renderDistance; chunkX <= playerChunkX + renderDistance; chunkX++)
        {
            for (int chunkZ = playerChunkZ - renderDistance; chunkZ <= playerChunkZ + renderDistance; chunkZ++)
            {
                Transform chunkTransform = chunkParent.transform.Find($"Chunk ({chunkX},{chunkZ})");
                MobData[] mobs = MobHelpers.GetMobsInChunk(moon, chunkX, chunkZ);
                if (mobs != null)
                {
                    foreach (MobData mobData in mobs)
                    {
                        if (mobData == null)
                        {
                            Debug.Log("NULL MOB DATA");
                            continue;
                        }

                        Vector3 mobPosition = new Vector3(
                            mobData.positionX,
                            mobData.positionY,
                            mobData.positionZ
                        );
                        Quaternion mobRotation = Quaternion.Euler(
                            0,
                            mobData.rotationY,
                            0
                        );

                        if (mobData.mobID == 0) // Green mob
                        {
                            GameObject mob = Instantiate(greenMobPrefab, mobPosition, mobRotation, chunkTransform);
                        }
                        else if (mobData.mobID == 1) // Brown mob
                        {
                            GameObject mob = Instantiate(brownMobPrefab, mobPosition, mobRotation, chunkTransform);
                            mob.GetComponent<BrownMob>().aggressive = mobData.aggressive;
                        }
                        else if (mobData.mobID == 2) // Space giraffe
                        {
                            GameObject mob = Instantiate(spaceGiraffePrefab, mobPosition, mobRotation, chunkTransform);
                        }
                    }
                }
            }
        }

        loadingState = LoadingState.DONE_GENERATING;
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
}
