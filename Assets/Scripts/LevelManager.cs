using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.IO;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Runtime.Serialization.Formatters.Binary;

[Serializable]
public class MoonData
{
    public ulong seed;
    public long distanceTraveled;
    public long worldTime;
    public bool isCreative;
    public float treeCover;
    public float terrainRoughness;
    public float exoticTerrain;
    public float wildlifeLevel;
}

enum LoadingState {NONE, GENERATING, DONE_GENERATING};

public class LevelManager : MonoBehaviour
{
    public List<GameObject> moons;
    public GameObject moonSettingsMenu;
    public GameObject loadingMenu;
    private Mesh blockMesh;
    public Material waterMaterial;
    public Material sulphurCrystalMaterial;
    public Material blueCrystalMaterial;
    public Material boronCrystalMaterial;
    public Material lightMaterial;
    private List<Material> blockMaterials;

    private LoadingState loadingState = LoadingState.NONE;
    private int totalProgressCount = 0;
    private int currentProgressCount = 0;

    void Awake()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        blockMesh = GenerateBlockMesh();

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
                moons[moon].transform.Find("Text").GetComponent<Text>().text = string.Format("{0} - {1} SQ KM", moonName, moonData.distanceTraveled);
            }
        }

        // Prepare materials
        var blocks = Enum.GetValues(typeof(BlockID));
        int numberOfBlocks = blocks.Length;
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

        // Delete chunks that may have persisted from Game scene (marked with DontDestroyOnLoad() from below)
        foreach (GameObject chunk in GameObject.FindGameObjectsWithTag("Chunk"))
            Destroy(chunk);
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

        // Create chunk objects (have to exist before calling ChunkHelpers.GetAdjacentChunkData())
        GameObject[] chunksFromGame = GameObject.FindGameObjectsWithTag("Chunk");
        for (int chunkX = playerChunkX - renderDistance - 1; chunkX <= playerChunkX + renderDistance + 1; chunkX++)
        {
            for (int chunkZ = playerChunkZ - renderDistance - 1; chunkZ <= playerChunkZ + renderDistance + 1; chunkZ++)
            {
                GameObject chunk = new GameObject("Chunk");
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
                    ChunkHelpers.GenerateChunk(chunkData.blocks, chunkX, chunkZ, moonData.seed);
                    ChunkHelpers.SaveChunkToFile(chunkData.blocks, moon, chunkX, chunkZ);
                }
                yield return null;
            }
        }

        // Instantiate blocks
        ChunkData[] adjacentChunkData;
        foreach (GameObject chunkObj in GameObject.FindGameObjectsWithTag("Chunk"))
        {
            if (chunkObj.transform.childCount == 0)
            {
                ChunkData chunkData = chunkObj.GetComponent<ChunkData>();
                if (chunkData.globalPosX >= playerChunkX - renderDistance && chunkData.globalPosX <= playerChunkX + renderDistance && chunkData.globalPosZ >= playerChunkZ - renderDistance && chunkData.globalPosZ <= playerChunkZ + renderDistance)
                {
                    adjacentChunkData = ChunkHelpers.GetAdjacentChunkData(chunkData.globalPosX, chunkData.globalPosZ);
                    for (int localBlockX = 0; localBlockX < GameData.CHUNK_SIZE; localBlockX++)
                    {
                        for (int localBlockY = 0; localBlockY < GameData.WORLD_HEIGHT_LIMIT; localBlockY++)
                        {
                            for (int localBlockZ = 0; localBlockZ < GameData.CHUNK_SIZE; localBlockZ++)
                            {
                                BlockID block = chunkData.blocks[localBlockX, localBlockY, localBlockZ];
                                if (block != BlockID.air && ChunkHelpers.BlockShouldBeRendered(block, adjacentChunkData, chunkData, new Vector3(localBlockX, localBlockY, localBlockZ)))
                                {
                                    GameObject blockObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                    blockObj.hideFlags = HideFlags.HideInHierarchy;
                                    blockObj.layer = LayerMask.NameToLayer("Block");
                                    blockObj.GetComponent<MeshFilter>().mesh = blockMesh;
                                    blockObj.GetComponent<Renderer>().material = blockMaterials[(int)block];

                                    BlockData blockData = blockObj.AddComponent<BlockData>();
                                    blockData.blockID = block;
                                    blockData.localPosX = localBlockX;
                                    blockData.localPosY = localBlockY;
                                    blockData.localPosZ = localBlockZ;

                                    blockObj.transform.position = new Vector3(
                                        (chunkData.globalPosX*GameData.CHUNK_SIZE + localBlockX), localBlockY, (chunkData.globalPosZ*GameData.CHUNK_SIZE + localBlockZ)
                                    );

                                    blockObj.transform.SetParent(chunkObj.transform);

                                    if (block == BlockID.light)
                                    {
                                        Light light = blockObj.AddComponent<Light>();
                                        light.type = LightType.Point;
                                    }
                                }
                            }
                        }
                        currentProgressCount++;
                        yield return null;
                    }
                }
            }
        }

        loadingState = LoadingState.DONE_GENERATING;
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
