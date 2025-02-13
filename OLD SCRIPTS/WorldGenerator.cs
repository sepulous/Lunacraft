using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
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


*/

public class WorldGenerator : MonoBehaviour
{
    private Player player;
    private int world;
    private ushort xChunkGenSeed;
    private ushort zChunkGenSeed;

    private BinaryFormatter formatter;
    private Item[,,] currentChunkToGenerate;

    void Awake()
    {
        player = GameObject.Find("PlayerCapsule").GetComponent<Player>();
        formatter = new BinaryFormatter();
        world = PlayerPrefs.GetInt("world", 0);
        using (FileStream levelDataFile  = File.Open(String.Format("{0}/worlds/world{1}/level.dat", Application.persistentDataPath, world), FileMode.Open))
        {
            LevelData levelData = (LevelData)(new BinaryFormatter()).Deserialize(levelDataFile);
            ushort seed = levelData.seed;
            xChunkGenSeed = (ushort)(seed & 0b1010101010101010);
            zChunkGenSeed = (ushort)(seed & 0b0101010101010101);
        }

        // Initial generation
        (int x, int z) playerChunkCoords = player.GetChunkCoords();
        for (int chunkX = playerChunkCoords.x - GameData.RENDER_DISTANCE; chunkX <= playerChunkCoords.x + GameData.RENDER_DISTANCE; chunkX++)
        {
            for (int chunkZ = playerChunkCoords.z - GameData.RENDER_DISTANCE; chunkZ <= playerChunkCoords.z + GameData.RENDER_DISTANCE; chunkZ++)
            {
                String chunkFile = String.Format("{0}/worlds/world{1}/chunks/{2}_{3}.dat", Application.persistentDataPath, world, chunkX, chunkZ);
                // Could store chunk ranges that have been generated to avoid a file check every update loop
                if (true || !File.Exists(chunkFile))
                {
                    currentChunkToGenerate = GenerateChunk(chunkX, chunkZ);
                    using (FileStream file = File.Create(chunkFile))
                        formatter.Serialize(file, currentChunkToGenerate);
                }
            }
        }
    }

    void Update()
    {
        // TODO: Should improve this to prioritize unloaded regions that the player is moving toward

        // (int x, int z) playerChunkCoords = player.GetChunkCoords();
        // for (int chunkX = playerChunkCoords.x - GameData.RENDER_DISTANCE; chunkX <= playerChunkCoords.x + GameData.RENDER_DISTANCE; chunkX++)
        // {
        //     for (int chunkZ = playerChunkCoords.z - GameData.RENDER_DISTANCE; chunkZ <= playerChunkCoords.z + GameData.RENDER_DISTANCE; chunkZ++)
        //     {
        //         String chunkFile = String.Format("{0}/worlds/world{1}/chunks/{2}_{3}.dat", Application.persistentDataPath, world, chunkX, chunkZ);
        //         // Could store chunk ranges that have been generated to avoid a file check every update loop
        //         if (!File.Exists(chunkFile))
        //         {
        //             currentChunkToGenerate = GenerateChunk(chunkX, chunkZ);
        //             using (FileStream file = File.Create(chunkFile))
        //                 formatter.Serialize(file, currentChunkToGenerate);
        //         }
        //     }
        // }
    }

    // int[,] GenerateHeightMap(int chunkX, int chunkZ, float amplitude, float frequency, float persistence, int octaves)
    // {
    //     chunkX += 100; // TODO: Remove, just added this to change the terrain
    //     chunkZ += 100;
    //     int[,] heightMap = new int[GameData.CHUNK_SIZE,GameData.CHUNK_SIZE];
    //     float frequency0 = frequency;
    //     float amplitude0 = amplitude;
    //     for (int x = 0; x < GameData.CHUNK_SIZE; x++)
    //     {
    //         for (int z = 0; z < GameData.CHUNK_SIZE; z++)
    //         {
    //             frequency = frequency0;
    //             amplitude = amplitude0;
    //             float heightLimit = 0F;
    //             for (int i = 0; i < octaves; i++)
    //             {
    //                 float xArg = (((x + chunkX*GameData.CHUNK_SIZE) + 0.5F + xChunkGenSeed) / 16F) * frequency;
    //                 float zArg = (((z + chunkZ*GameData.CHUNK_SIZE) + 0.5F + zChunkGenSeed) / 16F) * frequency;
    //                 heightLimit += Mathf.PerlinNoise(xArg, zArg) * amplitude;
    //                 frequency *= 2F;
    //                 amplitude *= persistence;
    //             }
    //             heightMap[x,z] = (int)heightLimit;
    //         }
    //     }
    //     return heightMap;
    // }

    // Item[,,] GenerateChunk(int chunkX, int chunkZ)
    // {
    //     Item[,,] chunk = new Item[GameData.CHUNK_SIZE, GameData.WORLD_HEIGHT_LIMIT, GameData.CHUNK_SIZE];

    //     int[,] rockHeightMap   = GenerateHeightMap(chunkX, chunkZ, 20F, 0.4F, 0.8F, 2);
    //     int[,] gravelHeightMap = GenerateHeightMap(chunkX, chunkZ, 4F, 0.4F, 0.6F, 2);
    //     int[,] dirtHeightMap   = GenerateHeightMap(chunkX, chunkZ, 3F, 0.4F, 0.6F, 2);
    //     int[,] sandHeightMap   = GenerateHeightMap(chunkX, chunkZ, 6F, 0.4F, 0.8F, 2);

    //     for (int x = 0; x < GameData.CHUNK_SIZE; x++)
    //     {
    //         for (int z = 0; z < GameData.CHUNK_SIZE; z++)
    //         {
    //             int rockHeightLimit = rockHeightMap[x,z];
    //             int gravelHeightLimit = gravelHeightMap[x,z];
    //             int dirtHeightLimit = dirtHeightMap[x,z];
    //             int sandHeightLimit = sandHeightMap[x,z];
    //             int y = 0;

    //             // Base rock
    //             while (y < 50 + rockHeightLimit)
    //             {
    //                 chunk[x,y,z] = Item.rock;
    //                 y++;
    //             }

    //             // Base gravel
    //             while (y < 50 + rockHeightLimit + gravelHeightLimit)
    //             {
    //                 chunk[x,y,z] = Item.gravel;
    //                 y++;
    //             }

    //             // Base dirt
    //             while (y < 50 + rockHeightLimit + gravelHeightLimit + dirtHeightLimit)
    //             {
    //                 chunk[x,y,z] = Item.dirt;
    //                 y++;
    //             }

    //             if (50 + rockHeightLimit + gravelHeightLimit + dirtHeightLimit < GameData.GROUND_LEVEL)
    //             {
    //                 while (y <= GameData.GROUND_LEVEL && y < 50 + rockHeightLimit + gravelHeightLimit + dirtHeightLimit + sandHeightLimit)
    //                 {
    //                     chunk[x,y,z] = Item.sand;
    //                     y++;
    //                 }

    //                 while (y <= GameData.GROUND_LEVEL)
    //                 {
    //                     chunk[x,y,z] = Item.water;
    //                     y++;
    //                 }
    //             }
    //             else
    //             {
    //                 chunk[x,y,z] = Item.topsoil;
    //                 y++;
    //             }

    //             // while (y < GameData.WORLD_HEIGHT_LIMIT)
    //             // {
    //             //     chunk[x, y, z] = Item.air;
    //             //     y++;
    //             // }
    //         }
    //     }

    //     return chunk;
    // }
}
