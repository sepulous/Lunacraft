using System;
using System.IO;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum Direction
{
    POSITIVE_X,
    NEGATIVE_X,
    POSITIVE_Z,
    NEGATIVE_Z
}

public struct NewChunkRowData
{
    public bool ready;
    public Direction direction;
}

public class ChunkGenerator : MonoBehaviour
{
    public static ChunkGenerator Instance { get; private set; }
    private BlockID[,][,,] loadedChunks;
    private BlockID[][,,] newChunkRow;
    private Player player;
    private int maxChunkExtent;
    private int currentPlayerChunkX;
    private int currentPlayerChunkZ;
    private bool generatingNewChunkRow = false;
    private NewChunkRowData newChunkRowData;
    private int currentMoon;
    private ulong currentSeed;
    
    // For progress bar in main menu
    private bool initialChunksFinished = false;
    private int totalInitialChunkCount = 1;
    private int currentInitialChunkCount = 0;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        if (Instance != null && Instance != this)
            Destroy(this);
        else
            Instance = this;

        currentPlayerChunkX = 0; // Player spawns at origin
        currentPlayerChunkZ = 0;
        // TODO: This isn't generally true. Have to store player position when world is saved.

        maxChunkExtent = (int)(GameData.MAX_RENDER_DISTANCE / GameData.CHUNK_SIZE);

        loadedChunks = new BlockID[2*maxChunkExtent + 1, 2*maxChunkExtent + 1][,,];
        for (int x = 0; x < 2*maxChunkExtent + 1; x++)
            for (int z = 0; z < 2*maxChunkExtent + 1; z++)
                loadedChunks[x, z] = new BlockID[GameData.CHUNK_SIZE, GameData.WORLD_HEIGHT_LIMIT, GameData.CHUNK_SIZE];

        newChunkRow = new BlockID[2*maxChunkExtent + 1][,,];
        for (int k = 0; k < 2*maxChunkExtent + 1; k++)
            newChunkRow[k] = new BlockID[GameData.CHUNK_SIZE, GameData.WORLD_HEIGHT_LIMIT, GameData.CHUNK_SIZE];
    }

    void Start()
    {
        
    }

    void Update()
    {
        // if (SceneManager.GetActiveScene().buildIndex == 1)
        // {
        //     if (player == null)
        //         player = GameObject.Find("PlayerCapsule").GetComponent<Player>();

        //     (int newPlayerChunkX, int newPlayerChunkZ) = player.GetChunkCoords();

        //     if (!generatingNewChunkRow && newPlayerChunkX - currentPlayerChunkX != 0)
        //     {
        //         generatingNewChunkRow = true;

        //         int sign = Math.Sign(newPlayerChunkX - currentPlayerChunkX);
        //         newChunkRowData.direction = (sign == 1) ? Direction.POSITIVE_X : Direction.NEGATIVE_X;
        //         int newChunkX = currentPlayerChunkX + sign*maxChunkExtent + sign;

        //         StartCoroutine(GenerateOrLoadNewChunks(newChunkX, newPlayerChunkX));
        //     }

        //     if (!generatingNewChunkRow && newPlayerChunkZ - currentPlayerChunkZ != 0) // Only update z once x is done updating
        //     {
        //         generatingNewChunkRow = true;

        //         int sign = Math.Sign(newPlayerChunkZ - currentPlayerChunkZ);
        //         newChunkRowData.direction = (sign == 1) ? Direction.POSITIVE_Z : Direction.NEGATIVE_Z;
        //         int newChunkZ = currentPlayerChunkZ + sign*maxChunkExtent + sign;

        //         StartCoroutine(GenerateOrLoadNewChunks(newChunkZ, newPlayerChunkZ));
        //     }
        // }
    }

    private void AddNewChunkRow()
    {
        // if (!generatingNewChunkRow && newChunkRowData.ready)
        // {
            if (newChunkRowData.direction == Direction.POSITIVE_X)
            {
                for (int z = 0; z < 2*maxChunkExtent + 1; z++)
                {
                    for (int x = 0; x < 2*maxChunkExtent; x++)
                        loadedChunks[x, z] = loadedChunks[x + 1, z];

                    loadedChunks[2*maxChunkExtent, z] = newChunkRow[z];
                }
            }
            else if (newChunkRowData.direction == Direction.NEGATIVE_X)
            {
                for (int z = 0; z < 2*maxChunkExtent + 1; z++)
                {
                    for (int x = 2*maxChunkExtent; x > 0; x--)
                        loadedChunks[x, z] = loadedChunks[x - 1, z];

                    loadedChunks[0, z] = newChunkRow[z];
                }
            }
            else if (newChunkRowData.direction == Direction.POSITIVE_Z)
            {
                for (int x = 0; x < 2*maxChunkExtent + 1; x++)
                {
                    for (int z = 0; z < 2*maxChunkExtent; z++)
                        loadedChunks[x, z] = loadedChunks[x, z + 1];

                    loadedChunks[x, 2*maxChunkExtent] = newChunkRow[x];
                }
            }
            else // NEGATIVE_Z
            {
                for (int x = 0; x < 2*maxChunkExtent + 1; x++)
                {
                    for (int z = 2*maxChunkExtent; z > 0; z--)
                        loadedChunks[x, z] = loadedChunks[x, z - 1];

                    loadedChunks[x, 0] = newChunkRow[x];
                }
            }

            newChunkRowData.ready = false; // Update finished
        //}
    }

    private IEnumerator GenerateOrLoadNewChunks(int newChunk, int newPlayerChunkCoord)
    {
        if (newChunkRowData.direction == Direction.POSITIVE_X || newChunkRowData.direction == Direction.NEGATIVE_X)
        {
            for (int chunkZ = -maxChunkExtent; chunkZ <= maxChunkExtent; chunkZ++)
            {
                if (ChunkHelpers.ChunkFileExists(currentMoon, newChunk, currentPlayerChunkZ + chunkZ))
                {
                    ChunkHelpers.GetChunkFromFile(newChunkRow[chunkZ + maxChunkExtent], currentMoon, newChunk, currentPlayerChunkZ + chunkZ);
                }
                else
                {
                    ChunkHelpers.GenerateChunk(newChunkRow[chunkZ + maxChunkExtent], newChunk, currentPlayerChunkZ + chunkZ, currentSeed);
                    ChunkHelpers.SaveChunkToFile(newChunkRow[chunkZ + maxChunkExtent], currentMoon, newChunk, currentPlayerChunkZ + chunkZ);
                }
                yield return null;
            }
            currentPlayerChunkX = newPlayerChunkCoord;
            AddNewChunkRow();
        }
        else
        {
            for (int chunkX = -maxChunkExtent; chunkX <= maxChunkExtent; chunkX++)
            {
                if (ChunkHelpers.ChunkFileExists(currentMoon, currentPlayerChunkX + chunkX, newChunk))
                {
                    ChunkHelpers.GetChunkFromFile(newChunkRow[chunkX + maxChunkExtent], currentMoon, currentPlayerChunkX + chunkX, newChunk);
                }
                else
                {
                    ChunkHelpers.GenerateChunk(newChunkRow[chunkX + maxChunkExtent], currentPlayerChunkX + chunkX, newChunk, currentSeed);
                    ChunkHelpers.SaveChunkToFile(newChunkRow[chunkX + maxChunkExtent], currentMoon, currentPlayerChunkX + chunkX, newChunk);
                }
                yield return null;
            }
            currentPlayerChunkZ = newPlayerChunkCoord;
            AddNewChunkRow();
        }

        newChunkRowData.ready = true;
        generatingNewChunkRow = false;
    }

    public IEnumerator GenerateInitialChunks(int moon, MoonData moonData)
    {
        currentMoon = moon;
        currentSeed = moonData.seed;

        BlockID[,,] tempChunk = new BlockID[GameData.CHUNK_SIZE, GameData.WORLD_HEIGHT_LIMIT, GameData.CHUNK_SIZE]; // Used if chunk falls outside of array

        // Estimate how long it takes to generate a chunk to decide how many to generate initially
        var watch = System.Diagnostics.Stopwatch.StartNew();
        ChunkHelpers.GenerateChunk(loadedChunks[maxChunkExtent, maxChunkExtent], 0, 0, moonData.seed); // (maxChunkExtent, maxChunkExtent) indexes center of array, where player "is"
        ChunkHelpers.SaveChunkToFile(loadedChunks[maxChunkExtent, maxChunkExtent], moon, 0, 0);        //
        var timeToGenerateOneChunk = watch.ElapsedMilliseconds;

        // Generate initial patch
        // float timeLimit = 3000; // Don't make the player wait longer than ~3 seconds
        // int initialPatchSize = maxChunkExtent + (int)Mathf.Ceil(Mathf.Sqrt(timeLimit * 1.2F / timeToGenerateOneChunk)); // On my system it overestimates the total amount of time by about 20% (a sample size of 1 isn't very representative)
        // int initialPatchExtent = (int)Mathf.Ceil(0.5F * initialPatchSize) + 1;
        int initialPatchExtent = maxChunkExtent; // could add some amount
        totalInitialChunkCount = (2*initialPatchExtent + 1)*(2*initialPatchExtent + 1) - 1;
        for (int chunkX = -initialPatchExtent; chunkX <= initialPatchExtent; chunkX++)
        {
            for (int chunkZ = -initialPatchExtent; chunkZ <= initialPatchExtent; chunkZ++)
            {
                if (chunkX == 0 && chunkZ == 0) // Already generated this one
                    continue;
                
                if (chunkX >= -maxChunkExtent && chunkX <= maxChunkExtent && chunkZ >= -maxChunkExtent && chunkZ <= maxChunkExtent)
                {
                    ChunkHelpers.GenerateChunk(loadedChunks[chunkX + maxChunkExtent, chunkZ + maxChunkExtent], chunkX, chunkZ, moonData.seed);
                    ChunkHelpers.SaveChunkToFile(loadedChunks[chunkX + maxChunkExtent, chunkZ + maxChunkExtent], moon, chunkX, chunkZ);
                }
                else
                {
                    ChunkHelpers.GenerateChunk(tempChunk, chunkX, chunkZ, moonData.seed);
                    ChunkHelpers.SaveChunkToFile(tempChunk, moon, chunkX, chunkZ);
                }
                currentInitialChunkCount++;
                yield return null;
            }
        }
        initialChunksFinished = true;
    }

    public void LoadInitialChunks(int moon)
    {
        currentMoon = moon;

        for (int chunkX = -maxChunkExtent; chunkX <= maxChunkExtent; chunkX++) // TODO: Factor player's actual starting position in
        {
            for (int chunkZ = -maxChunkExtent; chunkZ <= maxChunkExtent; chunkZ++) // TODO: Factor player's actual starting position in
            {
                ChunkHelpers.GetChunkFromFile(loadedChunks[maxChunkExtent + chunkX, maxChunkExtent + chunkZ], moon, chunkX, chunkZ);
            }
        }
    }

    public void SaveAllChunksToFile()
    {
        for (int x = -maxChunkExtent; x <= maxChunkExtent; x++)
            for (int z = -maxChunkExtent; z <= maxChunkExtent; z++)
                ChunkHelpers.SaveChunkToFile(loadedChunks[x + maxChunkExtent, z + maxChunkExtent], currentMoon, currentPlayerChunkX + x, currentPlayerChunkZ + z);
    }

    public int GetCurrentInitialChunkCount()
    {
        return currentInitialChunkCount;
    }

    public int GetTotalInitialChunkCount()
    {
        return totalInitialChunkCount;
    }

    public BlockID[,][,,] GetLoadedChunks()
    {
        return loadedChunks;
    }

    public bool InitialChunksFinished()
    {
        return initialChunksFinished;
    }
}
