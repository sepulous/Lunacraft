using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public enum AstronautType
{
    WHITE, GREEN, BLUE, PINK, YELLOW
}

/*

Mob IDs:
    0 - Green mob
    1 - Brown mob
    2 - Space giraffe
    3 - White astronaut
    4 - Green astronaut
    5 - Blue astronaut
    6 - Pink astronaut
    7 - Yellow astronaut

[mobID, aggression, posX, posY, posZ, rotY]

*/

public class MobHelpers
{
    public static float[] GetMobsInChunk(int moon, int chunkX, int chunkZ)
    {
        ulong chunkID = CombineChunkCoordinates(chunkX, chunkZ);
        string mobFilePath = $"{Application.persistentDataPath}/moons/moon{moon}/mobs/{chunkID}.dat";

        if (!File.Exists(mobFilePath))
            return null;

        long mobFileSizeInBytes = (new FileInfo(mobFilePath)).Length;
        byte[] mobDataBytes = new byte[mobFileSizeInBytes];
        using (FileStream mobFile = File.Open(mobFilePath, FileMode.Open, FileAccess.Read))
            mobFile.Read(mobDataBytes, 0, mobDataBytes.Length);

        float[] mobData = new float[mobDataBytes.Length / sizeof(float)];
        Buffer.BlockCopy(mobDataBytes, 0, mobData, 0, mobDataBytes.Length);

        return mobData;
    }

    public static void SaveMobsToChunk(float[] mobData, int moon, int chunkX, int chunkZ)
    {
        ulong chunkID = CombineChunkCoordinates(chunkX, chunkZ);
        string mobFolder = $"{Application.persistentDataPath}/moons/moon{moon}/mobs";
        string mobFilePath = $"{Application.persistentDataPath}/moons/moon{moon}/mobs/{chunkID}.dat";
        if (!Directory.Exists(mobFolder))
            Directory.CreateDirectory(mobFolder);

        byte[] mobDataBytes = new byte[mobData.Length * sizeof(float)];
        Buffer.BlockCopy(mobData, 0, mobDataBytes, 0, mobDataBytes.Length);

        using (FileStream mobFile = File.Open(mobFilePath, FileMode.Create, FileAccess.Write))
            mobFile.Write(mobDataBytes, 0, mobDataBytes.Length);
    }

    private static ulong CombineChunkCoordinates(int chunkX, int chunkZ)
    {
        ulong encoding = (ulong)((uint)chunkX);
        encoding <<= 32;
        encoding |= (ulong)((uint)chunkZ);
        return encoding;
    }
}
