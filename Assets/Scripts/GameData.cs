using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameData : ScriptableObject
{
    public const int GROUND_LEVEL = 64;
    public const int WORLD_HEIGHT_LIMIT = 128;
    public const int CHUNK_SIZE = 64;
    public const int MAX_RENDER_DISTANCE = 48 * 16; // (blocks per slider value) * (max slider value)
}
