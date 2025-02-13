using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.Profiling;
using TMPro;
using Cinemachine;

[Serializable]
public struct PlayerData
{
    public float positionX;
    public float positionY;
    public float positionZ;
    public float rotationY;
    public float cameraRotationX;
}

public class Player : MonoBehaviour
{
    private GameObject camera;
    private PauseMenu pauseMenu;

    private float flySpeed = 0.8F;

    public Material selectionCubeMaterial;
    private GameObject selectionCube;
    private RaycastHit selectionInfo;
    private float maxSelectionDistance = 6F;
    private bool destroyedBlock;
    private bool placedBlock;
    private string playerDataPath;

    // Inventory: 10x5 (including hotbar)

    void Awake()
    {
        int moon = PlayerPrefs.GetInt("moon");
        playerDataPath = string.Format("{0}/moons/moon{1}/player.dat", Application.persistentDataPath, moon);
        PlayerData playerData;
        if (!File.Exists(playerDataPath))
        {
            playerData = new PlayerData();
            playerData.positionX = 1;
            playerData.positionY = 100;
            playerData.positionZ = 1;
            playerData.rotationY = 0;
            playerData.cameraRotationX = 0;
            
            using (FileStream fileStream = new FileStream(playerDataPath, FileMode.Create, FileAccess.Write))
                (new BinaryFormatter()).Serialize(fileStream, playerData);
        }
        else
        {
            using (FileStream fileStream = new FileStream(playerDataPath, FileMode.Open, FileAccess.Read))
                playerData = (PlayerData)(new BinaryFormatter()).Deserialize(fileStream);
        }

        GetComponent<CharacterController>().enabled = false;
        transform.position = new Vector3(playerData.positionX, playerData.positionY, playerData.positionZ);
        GetComponent<FPSController>().SetPlayerRotationY(playerData.rotationY);
        GetComponent<FPSController>().SetCameraRotationX(playerData.cameraRotationX);
        GetComponent<CharacterController>().enabled = true;
    }

    void Start()
    {
        pauseMenu = GameObject.Find("Pause Menu").GetComponent<PauseMenu>();

        camera = GameObject.Find("PlayerCamera");
        Cursor.lockState = CursorLockMode.Locked;

        selectionCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        selectionCube.GetComponent<BoxCollider>().enabled = false;
        selectionCube.GetComponent<Renderer>().material = selectionCubeMaterial;
        selectionCube.transform.localScale = 1.005F * selectionCube.transform.localScale;
        selectionCube.SetActive(false);
        selectionCube.hideFlags = HideFlags.HideInHierarchy;
    }

    void Update()
    {
        bool lookingAtBlock = Physics.Raycast(camera.transform.position, camera.transform.forward, out selectionInfo, maxSelectionDistance, LayerMask.GetMask("Block"));
        if (lookingAtBlock)
        {
            selectionCube.SetActive(true);

            if (selectionCube.transform.position != selectionInfo.collider.gameObject.transform.position)
                selectionCube.transform.position = selectionInfo.collider.gameObject.transform.position;
        }
        else
        {
            selectionCube.SetActive(false);
        }

        if (!pauseMenu.IsPaused() && selectionCube.activeSelf)
        {
            destroyedBlock = Input.GetMouseButtonDown(0);
            placedBlock = Input.GetMouseButtonDown(1);
        }

        // if (Input.GetKey(KeyCode.Space))
        // {
        //     gameObject.transform.Translate(0, flySpeed, 0);
        // }

        // if (Input.GetKey(KeyCode.LeftShift))
        // {
        //     gameObject.transform.Translate(0, -flySpeed, 0);
        // }
    }

    public void SavePlayerData()
    {
        PlayerData playerData = new PlayerData();
        playerData.positionX = transform.position.x;
        playerData.positionY = transform.position.y;
        playerData.positionZ = transform.position.z;
        playerData.rotationY = GetComponent<FPSController>().GetPlayerRotationY();
        playerData.cameraRotationX = GetComponent<FPSController>().GetCameraRotationX();
        using (FileStream fileStream = new FileStream(playerDataPath, FileMode.Create, FileAccess.Write))
            (new BinaryFormatter()).Serialize(fileStream, playerData);
    }

    public (int, int) GetChunkCoords()
    {
        int chunkX = Mathf.FloorToInt(transform.position.x / GameData.CHUNK_SIZE);
        int chunkZ = Mathf.FloorToInt(transform.position.z / GameData.CHUNK_SIZE);
        return (chunkX, chunkZ);
    }

    public bool DestroyedBlock()
    {
        return destroyedBlock;
    }

    public void ResetDestroyedBlock()
    {
        destroyedBlock = false;
    }

    public bool PlacedBlock()
    {
        return placedBlock;
    }

    public void ResetPlacedBlock()
    {
        placedBlock = false;
    }

    public GameObject GetSelectedBlock()
    {
        return selectionInfo.collider.gameObject;
    }
}
