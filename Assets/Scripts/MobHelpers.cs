using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

[Serializable]
public enum AstronautType
{
    WHITE, GREEN, BLUE, PINK, YELLOW
}

[Serializable]
public class MobData
{
    public int mobID;
    public AstronautType astronautType;
    public float positionX;
    public float positionY;
    public float positionZ;
    public float rotationY;
    public bool aggressive;
}

public class MobHelpers
{
    public static MobData[] GetMobsInChunk(int moon, int chunkX, int chunkZ)
    {
        ulong chunkID = CombineChunkCoordinates(chunkX, chunkZ);
        string mobFilePath = string.Format("{0}/moons/moon{1}/mobs/{2}.dat", Application.persistentDataPath, moon, chunkID);
        if (!File.Exists(mobFilePath))
            return null;
            
        using (FileStream mobFile = File.Open(mobFilePath, FileMode.Open, FileAccess.Read))
            return (MobData[])(new BinaryFormatter()).Deserialize(mobFile);
    }

    public static void SaveMobsToChunk(MobData[] mobs, int moon, int chunkX, int chunkZ)
    {
        ulong chunkID = CombineChunkCoordinates(chunkX, chunkZ);
        string mobFolder = string.Format("{0}/moons/moon{1}/mobs", Application.persistentDataPath, moon);
        string mobFilePath = string.Format("{0}/moons/moon{1}/mobs/{2}.dat", Application.persistentDataPath, moon, chunkID);
        if (!Directory.Exists(mobFolder))
            Directory.CreateDirectory(mobFolder);

        using (FileStream mobFile = File.Open(mobFilePath, FileMode.Create, FileAccess.Write))
            (new BinaryFormatter()).Serialize(mobFile, mobs);
    }

    private static ulong CombineChunkCoordinates(int chunkX, int chunkZ)
    {
        ulong encoding = (ulong)((uint)chunkX);
        encoding <<= 32;
        encoding |= (ulong)((uint)chunkZ);
        return encoding;
    }
}
