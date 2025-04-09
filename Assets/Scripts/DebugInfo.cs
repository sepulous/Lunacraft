using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Profiling;
using TMPro;

public class DebugInfo : MonoBehaviour
{
    public GameObject debugInfo;
    public Player player;
    public ChunkManager chunkManager;
    public TMP_Text fpsText;
    public TMP_Text globalPosText;
    public TMP_Text chunkPosText;
    public TMP_Text selectedPosText;
    public TMP_Text memoryReservedText;
    public TMP_Text memoryUsedText;
    public TMP_Text memoryFreeText;
    public TMP_Text chunkGenText;
    public TMP_Text seedText;
    public ToggleButton debugInfoOptionButton;
    private bool debugInfoShown = false;

    // FPS data
    private List<int> lastFpsValues;
    private float timeElapsed = 0;
    private int frames = 0;
    private int avgFps = 0;

    void Awake()
    {
        lastFpsValues = new List<int>();
        debugInfoShown = OptionsManager.GetCurrentOptions().showDebugInfo;
        debugInfo.SetActive(debugInfoShown);

        // Set seed (doesn't change)
        int moon = PlayerPrefs.GetInt("moon");
        string moonDataFile = string.Format("{0}/moons/moon{1}/moon.dat", Application.persistentDataPath, moon);
        using (FileStream file = File.Open(moonDataFile, FileMode.Open, FileAccess.Read))
        {
            MoonData moonData = (MoonData)(new BinaryFormatter()).Deserialize(file);
            seedText.text = $"Seed: {moonData.seed}";
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F3))
        {
            ToggleDebugInfo();
        }

        if (debugInfoShown)
        {
            // FPS
            timeElapsed += Time.deltaTime;
            frames++;
            if (frames == 30)
            {
                int fps = (int)(frames / timeElapsed);
                lastFpsValues.Add(fps);
                if (lastFpsValues.Count == 5)
                {
                    avgFps = 0;
                    for (int i = 0; i < 5; i++)
                        avgFps += lastFpsValues[i];
                    avgFps /= 5;
                    lastFpsValues.Clear();
                }
                fpsText.text = string.Format("FPS: {0} (avg: {1})", fps, avgFps);
                timeElapsed = 0;
                frames = 0;
            }

            // Player's global/chunk positions
            (int playerChunkX, int playerChunkZ) = player.GetChunkCoords();
            globalPosText.text = string.Format("Global Position: ({0}, {1}, {2})", player.transform.position.x.ToString("0.0"), player.transform.position.y.ToString("0.0"), player.transform.position.z.ToString("0.0"));
            chunkPosText.text = string.Format("Chunk Position: ({0}, {1})", playerChunkX, playerChunkZ);

            // Selected block position
            GameObject selectedBlock = player.GetSelectedBlock();
            if (selectedBlock != null)
            {
                Vector3 selectedBlockPos = selectedBlock.transform.position;
                selectedPosText.text = $"Selected Position: ({(int)selectedBlockPos.x}, {(int)selectedBlockPos.y}, {(int)selectedBlockPos.z})";
            }
            else
            {
                selectedPosText.text = $"Selected Position: ";
            }

            // Memory usage
            int reservedMemory = (int)(Profiler.GetTotalReservedMemoryLong() / 1000000);
            int usedMemory = (int)(Profiler.GetTotalAllocatedMemoryLong() / 1000000);
            int freeMemory = reservedMemory - usedMemory;
            memoryReservedText.text = $"Memory Reserved: {reservedMemory} MB";
            memoryUsedText.text = $"Memory Used: {usedMemory} MB";
            memoryFreeText.text = $"Memory Free: {freeMemory} MB";

            // Chunk generation rate
            float chunksPerSecond = chunkManager.GetChunkGenerationRate();
            chunkGenText.text = $"Chunk Generation: {chunksPerSecond:F2}/s";
        }
    }

    public void ToggleDebugInfo()
    {
        debugInfoShown = !debugInfoShown;
        debugInfo.SetActive(debugInfoShown);
        debugInfoOptionButton.ToggleEnabled();
        
        Options options = OptionsManager.GetCurrentOptions();
        options.showDebugInfo = debugInfoShown;
        OptionsManager.UpdateCurrentOptions(options);
    }
}
