using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using TMPro;

[Serializable]
public struct PlayerData
{
    public float positionX;
    public float positionY;
    public float positionZ;
    public float rotationY;
    public float cameraRotationX;
    public InventorySystem inventorySystem;
    public int health;
    public int suitStatus;
}

public class Player : MonoBehaviour
{
    public GameObject invTip;

    private int health = 100;
    private int suitStatus = 100;
    private float lastHealthUpdate = 0;
    private GameObject camera;
    private PauseMenu pauseMenu;
    public GameObject rootUIObj;
    private GameObject inventoryUI;
    private GameObject hotbarUI;
    public GameObject jetpackUI;
    public GameObject crosshairUI;

    public GameObject deathMenu;
    public Image healthImage;
    public Image suitStatusImage;
    private float flySpeed = 0.8F;
    public Material[] selectionCubeMaterials;
    private GameObject selectionCube;
    private RaycastHit selectionInfo;
    private float maxSelectionDistance = 9F;
    private bool destroyedBlock;
    private bool placedBlock;
    private string playerDataPath;
    private Mesh blockMesh;
    private List<Material> blockMaterials;
    public float distanceTraveledThisSession = 0;

    // Inventory
    private GameObject heldStack;
    private ItemID heldItemID = ItemID.none;
    private int heldItemAmount = 0;

    // Scanner
    private GameObject scannerSlotObj;

    // Spacesuit
    private GameObject spacesuitObj;
    private float jetpackFuel = 1.0F; // 0.0 - 1.0
    public RectTransform jetpackLevelUI;
    private float jetpackLevelUIDefaultHeight;

    // Assembler
    private GameObject assemblerObj;

    private InventorySystem inventorySystem;

    // Held items
    public GameObject handObj;
    private Animator handAnimator;
    public Animator handParentAnimator;
    private GameObject drillObj;
    private Animator drillAnimator;
    private GameObject heldBlockObj;
    private GameObject heldSpriteObj;
    private GameObject pistolObj;
    private Animator pistolAnimator;
    private Animator pistolSlideAnimator;

    // Pistol stuff
    public GameObject slugPrefab;
    private float pistolChargeTime = 0;

    // Mining
    private float blockMineTime = 0;

    // Sounds
    private AudioSource blockBreakSource;
    public AudioClip blockBreakClip;
    private AudioSource blockPlaceSource;
    public AudioClip blockPlaceClip;
    private AudioSource craftSource;
    public AudioClip craftClip;
    private AudioSource scannerSource;
    public AudioClip scannerClip;
    private AudioSource drillPowerupSource;
    public AudioClip drillPowerupClip;
    private AudioSource drillingSource;
    public AudioClip drillingClip;
    private AudioSource hurtSource;
    public AudioClip hurtClip;
    private AudioSource pistolSource;
    public AudioClip pistolClip;
    private AudioSource pickupSource;
    public AudioClip pickupClip;

    public Material spriteMaterial;

    void Awake()
    {
        Options currentOptions = OptionsManager.GetCurrentOptions();
        camera = GameObject.Find("PlayerCamera");

        int moon = PlayerPrefs.GetInt("moon");
        string moonDataFile = string.Format("{0}/moons/moon{1}/moon.dat", Application.persistentDataPath, moon);
        MoonData moonData = new MoonData();
        using (FileStream file = File.Open(moonDataFile, FileMode.Open, FileAccess.Read))
            moonData = (MoonData)(new BinaryFormatter()).Deserialize(file);

        float sfxVolume = currentOptions.sfxVolume;
        RenderSettings.fogColor = new Color(RenderSettings.fogColor.r, RenderSettings.fogColor.g, RenderSettings.fogColor.b, currentOptions.showFog ? 1 : 0);

        // Audio
        blockBreakSource = gameObject.AddComponent<AudioSource>();
        blockBreakSource.volume = sfxVolume;
        blockBreakSource.clip = blockBreakClip;
        blockPlaceSource = gameObject.AddComponent<AudioSource>();
        blockPlaceSource.volume = sfxVolume;
        blockPlaceSource.clip = blockPlaceClip;
        craftSource = gameObject.AddComponent<AudioSource>();
        craftSource.volume = sfxVolume;
        craftSource.clip = craftClip;
        scannerSource = gameObject.AddComponent<AudioSource>();
        scannerSource.volume = sfxVolume;
        scannerSource.clip = scannerClip;
        drillPowerupSource = gameObject.AddComponent<AudioSource>();
        drillPowerupSource.volume = sfxVolume;
        drillPowerupSource.clip = drillPowerupClip;
        drillingSource = gameObject.AddComponent<AudioSource>();
        drillingSource.volume = sfxVolume;
        drillingSource.clip = drillingClip;
        drillingSource.loop = true;
        hurtSource = gameObject.AddComponent<AudioSource>();
        hurtSource.volume = sfxVolume;
        hurtSource.clip = hurtClip;
        pistolSource = gameObject.AddComponent<AudioSource>();
        pistolSource.volume = sfxVolume;
        pistolSource.clip = pistolClip;
        pickupSource = gameObject.AddComponent<AudioSource>();
        pickupSource.volume = sfxVolume;
        pickupSource.clip = pickupClip;

        // UI shit
        inventoryUI = rootUIObj.transform.Find("Inventory").gameObject;
        hotbarUI = rootUIObj.transform.Find("Hotbar").gameObject;
        heldStack = rootUIObj.transform.Find("HeldStack").gameObject;
        scannerSlotObj = inventoryUI.transform.Find("ScannerSlot").gameObject;
        spacesuitObj = inventoryUI.transform.Find("Spacesuit").gameObject;
        assemblerObj = inventoryUI.transform.Find("Assembler").gameObject;
        drillObj = handObj.transform.Find("Drill").gameObject;
        heldBlockObj = handObj.transform.Find("HeldBlock").gameObject;
        heldSpriteObj = handObj.transform.Find("HeldSprite").gameObject;
        pistolObj = handObj.transform.Find("Pistol").gameObject;

        // Animators
        handAnimator = handObj.GetComponent<Animator>();
        pistolAnimator = pistolObj.GetComponent<Animator>();
        pistolSlideAnimator = pistolObj.transform.Find("Pistol Slide").GetComponent<Animator>();
        drillAnimator = drillObj.transform.Find("Drill Bit").GetComponent<Animator>();

        // Block mesh/materials
        blockMesh = MeshData.GetBlockMesh();
        blockMaterials = MeshData.GetBlockMaterials();

        playerDataPath = string.Format("{0}/moons/moon{1}/player.dat", Application.persistentDataPath, moon);
        bool newMoon = !File.Exists(playerDataPath);
        jetpackLevelUIDefaultHeight = jetpackLevelUI.sizeDelta.y;
        PlayerData playerData;
        if (newMoon)
        {
            playerData = new PlayerData();

            // Default position/orientation
            playerData.positionX = GameData.CHUNK_SIZE / 2F;
            playerData.positionY = 100;
            playerData.positionZ = GameData.CHUNK_SIZE / 2F;
            playerData.rotationY = 0;
            playerData.cameraRotationX = 0;
            playerData.inventorySystem = new InventorySystem(moonData.isCreative);
            playerData.health = 100;
            playerData.suitStatus = 100;
            
            using (FileStream fileStream = new FileStream(playerDataPath, FileMode.Create, FileAccess.Write))
                (new BinaryFormatter()).Serialize(fileStream, playerData);

            //invTip.SetActive(true);
        }
        else
        {
            using (FileStream fileStream = new FileStream(playerDataPath, FileMode.Open, FileAccess.Read))
                playerData = (PlayerData)(new BinaryFormatter()).Deserialize(fileStream);
        }

        // Place/orient player
        GetComponent<CharacterController>().enabled = false;
        transform.position = new Vector3(playerData.positionX, playerData.positionY, playerData.positionZ);
        GetComponent<FPSController>().SetPlayerRotationY(playerData.rotationY);
        GetComponent<FPSController>().SetCameraRotationX(playerData.cameraRotationX);
        GetComponent<CharacterController>().enabled = true;

        // Inventory/spacesuit
        inventorySystem = playerData.inventorySystem;
        UpdateInventoryUI();

        // Health and suit status
        health = playerData.health;
        suitStatus = playerData.suitStatus;
    }

    void Start()
    {
        pauseMenu = GameObject.Find("Pause Menu").GetComponent<PauseMenu>();

        Cursor.lockState = CursorLockMode.Locked;

        selectionCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        selectionCube.GetComponent<BoxCollider>().enabled = false;
        selectionCube.GetComponent<Renderer>().material = selectionCubeMaterials[0];
        selectionCube.transform.localScale = 1.005F * selectionCube.transform.localScale;
        selectionCube.SetActive(false);
        selectionCube.hideFlags = HideFlags.HideInHierarchy;

        heldBlockObj.GetComponent<MeshFilter>().mesh = blockMesh;
    }

    void Update()
    {
        if (health == 0)
        {
            GetComponent<CharacterController>().enabled = false;
            deathMenu.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Options currentOptions = OptionsManager.GetCurrentOptions();
            ItemID selectedItem = inventorySystem.GetSelectedItem();

            // Update SFX volume
            float sfxVolume = currentOptions.sfxVolume;
            blockBreakSource.volume = sfxVolume;
            craftSource.volume = sfxVolume;
            scannerSource.volume = sfxVolume;
            drillPowerupSource.volume = sfxVolume;
            drillingSource.volume = sfxVolume;
            hurtSource.volume = sfxVolume;
            pistolSource.volume = sfxVolume;
            pickupSource.volume = sfxVolume;

            // Hide/show GUI
            hotbarUI.SetActive(inventoryUI.activeSelf || currentOptions.showGUI);
            jetpackUI.SetActive(inventoryUI.activeSelf || currentOptions.showGUI);
            crosshairUI.SetActive(currentOptions.showGUI);

            // Jetpack level
            jetpackLevelUI.sizeDelta = new Vector2(jetpackLevelUI.sizeDelta.x, jetpackFuel * jetpackLevelUIDefaultHeight);

            // Replenish suit status/health
            ItemID batteryItem = inventorySystem.spacesuit.batterySlot.itemID;
            if (Time.time - lastHealthUpdate > 0.8F)
            {
                if (suitStatus < 100 && (batteryItem == ItemID.battery || batteryItem == ItemID.power_crystal || batteryItem == ItemID.energy_orb))
                {
                    if (batteryItem == ItemID.battery)
                        suitStatus++;
                    else if (batteryItem == ItemID.power_crystal)
                        suitStatus = Mathf.Min(100, suitStatus + 2);
                    else
                        suitStatus = Mathf.Min(100, suitStatus + 3);
                }
                if (suitStatus > 0 && health < 100)
                {
                    health++;
                }
                lastHealthUpdate = Time.time;
            }

            // Selection cube
            bool lookingAtBlock = Physics.Raycast(camera.transform.position, camera.transform.forward, out selectionInfo, maxSelectionDistance, LayerMask.GetMask("Block"));
            if (lookingAtBlock)
            {
                if (selectionCube.transform.position != selectionInfo.collider.gameObject.transform.position)
                {
                    blockMineTime = 0;
                    selectionCube.GetComponent<Renderer>().material = selectionCubeMaterials[0];
                }

                BlockData selectedBlockData = selectionInfo.collider.gameObject.GetComponent<BlockData>();
                string selectedBlockIDName = selectedBlockData.blockID.ToString();
                if (selectedBlockIDName.StartsWith("minilight"))
                {
                    selectionCube.transform.localScale = 1.005F * (new Vector3(0.3F, 0.3F, 0.02F));
                }
                else
                {
                    selectionCube.transform.localScale = new Vector3(1.005F, 1.005F, 1.005F);
                }
                selectionCube.transform.position = selectionInfo.collider.gameObject.transform.position;
                selectionCube.transform.localRotation = selectionInfo.collider.gameObject.transform.rotation;

                selectionCube.SetActive(true);
            }
            else
            {
                selectionCube.SetActive(false);
            }

            // Block placement
            if (!pauseMenu.IsPaused() && !inventoryUI.activeSelf && selectionCube.activeSelf)
            {
                ItemID selected = inventorySystem.inventory.slots[0][inventorySystem.selectedHotbarSlot - 1].itemID;
                placedBlock = Input.GetMouseButtonDown(1) && (Enum.TryParse(selected.ToString(), out BlockID _) || selected.ToString().StartsWith("minilight"));
                if (placedBlock)
                {
                    handObj.GetComponent<Animator>().SetTrigger("PlacedBlock");
                    blockPlaceSource.Play();
                }
            }

            // Item pickup
            Collider[] droppedItems = Physics.OverlapSphere(camera.transform.position, 3F, LayerMask.GetMask("Dropped"));
            foreach (Collider dropped in droppedItems)
            {
                GameObject droppedObj = dropped.gameObject;
                Vector3 displacement = camera.transform.position - droppedObj.transform.position;
                Dropped droppedScript = droppedObj.GetComponent<Dropped>();
                if (!droppedScript.thrown || droppedScript.readyForPickup)
                {
                    if (displacement.magnitude < 1F) // Pick item up
                    {
                        ItemID droppedItemID = droppedScript.itemID;
                        if (droppedItemID == ItemID.topsoil)
                            droppedItemID = ItemID.dirt;
                        AddToInventory(droppedItemID, droppedScript.amount);
                        Destroy(droppedObj);
                        pickupSource.Play();
                    }
                    else // Move item toward player
                    {
                        droppedScript.movingTowardPlayer = true;
                        droppedObj.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation;
                        droppedObj.GetComponent<BoxCollider>().isTrigger = true;
                        droppedObj.GetComponent<Rigidbody>().linearVelocity = 6F * displacement.normalized;
                    }
                }
            }

            // if (Input.GetKeyDown(KeyCode.Y))
            // {
            //     Debug.Log($"{inventorySystem.assembler.inputSlots[2][0].itemID.ToString()}, {inventorySystem.assembler.inputSlots[2][1].itemID.ToString()}, {inventorySystem.assembler.inputSlots[2][2].itemID.ToString()}");
            //     Debug.Log($"{inventorySystem.assembler.inputSlots[1][0].itemID.ToString()}, {inventorySystem.assembler.inputSlots[1][1].itemID.ToString()}, {inventorySystem.assembler.inputSlots[1][2].itemID.ToString()}");
            //     Debug.Log($"{inventorySystem.assembler.inputSlots[0][0].itemID.ToString()}, {inventorySystem.assembler.inputSlots[0][1].itemID.ToString()}, {inventorySystem.assembler.inputSlots[0][2].itemID.ToString()}");
            // }

            //
            // Hand animations + mining logic
            //
            if (!pauseMenu.IsPaused() && !inventoryUI.activeSelf)
            {
                if (Input.GetMouseButton(0))
                {
                    if (selectedItem.ToString().StartsWith("drill")) // Drilling
                    {
                        if (!drillPowerupSource.isPlaying && !drillingSource.isPlaying)
                        {
                            drillPowerupSource.Play();
                            drillingSource.PlayScheduled(AudioSettings.dspTime + drillPowerupSource.clip.length);
                        }
                        handAnimator.SetBool("IsDrilling", true); // Extension
                        handParentAnimator.SetBool("IsDrilling", true); // Shaking
                        drillObj.transform.Find("Drill Bit").GetComponent<Animator>().SetBool("IsDrilling", true);

                        if (selectionCube.activeSelf)
                        {
                            blockMineTime += Time.deltaTime; // BUG: This *may* be framerate dependent. Make sure it isn't.
                            float totalMineTime;
                            if (selectedItem.ToString() == "drill_t1")
                                totalMineTime = 2;
                            else if (selectedItem.ToString() == "drill_t2")
                                totalMineTime = 1;
                            else
                                totalMineTime = 0.5F;

                            int blockMineLevel = (int)((5 * blockMineTime) / totalMineTime);
                            if (blockMineLevel == 5)
                            {
                                selectionCube.GetComponent<Renderer>().material = selectionCubeMaterials[0];
                                blockMineTime = 0;
                                destroyedBlock = true;
                                blockBreakSource.Play();
                            }
                            else
                            {
                                selectionCube.GetComponent<Renderer>().material = selectionCubeMaterials[blockMineLevel];
                            }
                        }
                        else
                        {
                            selectionCube.GetComponent<Renderer>().material = selectionCubeMaterials[0];
                            blockMineTime = 0;
                            destroyedBlock = false;
                        }
                    }
                    else if (selectedItem.ToString().StartsWith("slug")) // Charge pistol
                    {
                        pistolAnimator.SetBool("IsCharging", true);
                        pistolSlideAnimator.SetBool("IsCharging", true);
                        pistolChargeTime += Time.deltaTime;
                    }
                    else // Punching
                    {
                        handAnimator.SetBool("IsPunching", true);
                    }
                }
                else if (Input.GetMouseButtonUp(0))
                {
                    CancelAllAnimations();

                    if (selectedItem.ToString().StartsWith("drill"))
                    {
                        selectionCube.GetComponent<Renderer>().material = selectionCubeMaterials[0];
                        blockMineTime = 0;
                        drillPowerupSource.Stop();
                        drillingSource.Stop();
                    }
                    else if (selectedItem.ToString().StartsWith("slug")) // Shoot pistol
                    {
                        // Instantiate prefab with appropriate velocity
                        GameObject slug = Instantiate(slugPrefab, pistolObj.transform.position, camera.transform.rotation);
                        Slug slugComponent = slug.GetComponent<Slug>();
                        slugComponent.initialPosition = pistolObj.transform.position;
                        int pistolLevel = selectedItem.ToString()[^1] - '0';
                        float chargeAmount = pistolLevel * Mathf.Clamp(pistolChargeTime, 0, 5); // 5 seconds is maximum charge
                        slugComponent.initialVelocity = (15*chargeAmount + 2) * (camera.transform.forward - 0.02F*camera.transform.right);
                        slugComponent.initialAngleInDegrees = camera.transform.eulerAngles.x;
                        pistolChargeTime = 0;
                        pistolSource.Play();
                    }
                }
            }
            else
            {
                CancelAllAnimations();
            }

            // Toggle inventory
            if (Input.GetKeyDown(KeyCode.E) && !pauseMenu.IsPaused())
            {
                //invTip.SetActive(false);

                if (inventoryUI.activeSelf)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }

                inventoryUI.SetActive(!inventoryUI.activeSelf);
            }

            InventorySlot oldSelectedSlot = inventorySystem.inventory.slots[0][inventorySystem.selectedHotbarSlot - 1];

            if (!pauseMenu.IsPaused() && !inventoryUI.activeSelf)
            {
                // Change hotbar selection (number key)
                for (int i = 0; i <= 9; i++)
                {
                    if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha0 + i)))
                    {
                        if (i == 0)
                            inventorySystem.selectedHotbarSlot = 10;
                        else
                            inventorySystem.selectedHotbarSlot = i;

                        CancelAllAnimations();
                        drillPowerupSource.Stop();
                        drillingSource.Stop();

                        break;
                    }
                }

                // Change hotbar selection (scroll)
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (scroll != 0)
                {
                    if (scroll < 0f)
                    {
                        if (inventorySystem.selectedHotbarSlot == 10)
                            inventorySystem.selectedHotbarSlot = 1;
                        else
                            inventorySystem.selectedHotbarSlot++;
                    }
                    else if (scroll > 0f)
                    {
                        if (inventorySystem.selectedHotbarSlot == 1)
                            inventorySystem.selectedHotbarSlot = 10;
                        else
                            inventorySystem.selectedHotbarSlot--;
                    }
                    CancelAllAnimations();
                    drillPowerupSource.Stop();
                    drillingSource.Stop();
                }
            }

            InventorySlot selectedSlot = inventorySystem.inventory.slots[0][inventorySystem.selectedHotbarSlot - 1];

            // Chronobooster/chronowinder
            if (oldSelectedSlot.itemID != ItemID.chronobooster && selectedSlot.itemID == ItemID.chronobooster)
                GameObject.Find("WorldClock").GetComponent<WorldClock>().Speedup();
            else if (oldSelectedSlot.itemID == ItemID.chronobooster && selectedSlot.itemID != ItemID.chronobooster)
                GameObject.Find("WorldClock").GetComponent<WorldClock>().Slowdown();

            if (oldSelectedSlot.itemID != ItemID.chronowinder && selectedSlot.itemID == ItemID.chronowinder)
                GameObject.Find("WorldClock").GetComponent<WorldClock>().Reverse();
            else if (oldSelectedSlot.itemID == ItemID.chronowinder && selectedSlot.itemID != ItemID.chronowinder)
                GameObject.Find("WorldClock").GetComponent<WorldClock>().Unreverse();

            // Update held item
            if (selectedSlot.itemID.ToString().StartsWith("drill"))
            {
                drillObj.SetActive(true);
                heldBlockObj.SetActive(false);
                heldSpriteObj.SetActive(false);
                pistolObj.SetActive(false);
            }
            else if (selectedSlot.itemID.ToString().StartsWith("slug"))
            {
                pistolObj.SetActive(true);
                drillObj.SetActive(false);
                heldBlockObj.SetActive(false);
                heldSpriteObj.SetActive(false);
            }
            else
            {
                drillObj.SetActive(false);
                pistolObj.SetActive(false);

                if (Enum.TryParse(selectedSlot.itemID.ToString(), out BlockID selectedBlockID)) // Holding a block
                {
                    heldBlockObj.GetComponent<Renderer>().material = blockMaterials[(int)selectedBlockID];
                    heldBlockObj.SetActive(true);
                    heldSpriteObj.SetActive(false);
                }
                else // Holding a non-block item
                {
                    heldBlockObj.SetActive(false);
                    if (selectedSlot.itemID != ItemID.none)
                    {
                        heldSpriteObj.SetActive(true);
                        //heldSpriteObj.GetComponent<Renderer>().material.SetTexture("_BaseMap", Resources.Load<Texture2D>($"Textures/Items/{selectedSlot.itemID.ToString()}_item"));
                        heldSpriteObj.GetComponent<Renderer>().material.SetTexture("_BaseMap", MeshData.GetItemSprite(selectedSlot.itemID).texture);
                    }
                    else
                    {
                        heldSpriteObj.SetActive(false);
                    }
                }
            }

            /*

                k = i*10 + (j+1)

                i = floor(k / 10)
                j = (k % 10) - 1

            */

            // Throw item out (with 'q')
            if (Input.GetKeyDown(KeyCode.Q) && !inventoryUI.activeSelf && !pauseMenu.IsPaused())
            {
                ThrowStack(selectedSlot.itemID, 1);
                selectedSlot.amount--;
                if (selectedSlot.amount == 0)
                    selectedSlot.itemID = ItemID.none;
            }

            // Moving items around inventory
            if (inventoryUI.activeSelf)
            {
                heldStack.transform.position = Input.mousePosition;

                bool leftClick = Input.GetMouseButtonDown(0);
                bool rightClick = Input.GetMouseButtonDown(1);
                if (leftClick || rightClick)
                {
                    InventorySlot hoveredSlot = null;
                    bool hoveringOnAssemblerOutput = false;
                    bool holdingStack = heldItemAmount > 0 && heldItemID != ItemID.none;
                    float mousePosRatioX = Input.mousePosition.x / 2560F;
                    float mousePosRatioY = Input.mousePosition.y / 1440F;

                    // Check inventory slots
                    for (int i = 0; i < 5; i++)
                    {
                        for (int j = 0; j < 10; j++)
                        {
                            float xLeft = (634F / 2560F) + j*(135F / 2560F);
                            float xRight = xLeft + (118F / 2560F);
                            float yBottom = (74F / 1440F) + i*(135F / 1440F);
                            float yTop = yBottom + (118F / 1440F);
                            if (mousePosRatioX > xLeft && mousePosRatioX < xRight && mousePosRatioY > yBottom && mousePosRatioY < yTop)
                            {
                                hoveredSlot = inventorySystem.inventory.slots[i][j];
                                goto HoveredSlotFound;
                            }
                        }
                    }

                    // Check assembler slots
                    for (int i = 0; i < 3; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            float xLeft = (1496F / 2560F) + j*(120F / 2560F);
                            float xRight = xLeft + (104F / 2560F);
                            float yBottom = (908F / 1440F) + i*(120F / 1440F);
                            float yTop = yBottom + (104F / 1440F);
                            if (mousePosRatioX > xLeft && mousePosRatioX < xRight && mousePosRatioY > yBottom && mousePosRatioY < yTop)
                            {
                                hoveredSlot = inventorySystem.assembler.inputSlots[i][j];
                                goto HoveredSlotFound;
                            }
                        }
                    }
                    if (mousePosRatioX > (1958F / 2560F) && mousePosRatioX < (2058F / 2560F) && mousePosRatioY > (908F / 1440F) && mousePosRatioY < (1012F / 1440F))
                    {
                        hoveringOnAssemblerOutput = true;
                        goto HoveredSlotFound;
                    }

                    // Check scanner slot
                    if (mousePosRatioX >= (484F / 2560F) && mousePosRatioX <= (588F / 2560F) && mousePosRatioY >= (1149F / 1440F) && mousePosRatioY <= (1250F / 1440F))
                    {
                        hoveredSlot = inventorySystem.scannerSlot;
                        goto HoveredSlotFound;
                    }

                    // Check spacesuit slots
                    if (mousePosRatioX >= (1228F / 2560F) && mousePosRatioX <= (1325F / 2560F))
                    {
                        if (mousePosRatioY >= (867F / 1440F) && mousePosRatioY <= (974F / 1440F))
                            hoveredSlot = inventorySystem.spacesuit.jetpackSlot;
                        else if (mousePosRatioY >= (1056F / 1440F) && mousePosRatioY <= (1154F / 1440F))
                            hoveredSlot = inventorySystem.spacesuit.batterySlot;
                        else if (mousePosRatioY >= (1209F / 1440F) && mousePosRatioY <= (1308F / 1440F))
                            hoveredSlot = inventorySystem.spacesuit.helmetSlot;
                    }

                    // Throwing item out
                    if (holdingStack && (mousePosRatioX <= (600F / 2560F) || mousePosRatioX >= (1980F / 2560F)) && mousePosRatioY <= (880F / 1440F))
                    {
                        ThrowStack(heldItemID, heldItemAmount);
                        heldItemID = ItemID.none;
                        heldItemAmount = 0;
                    }

                HoveredSlotFound:
                    if (hoveredSlot != null || hoveringOnAssemblerOutput) // Player is hovering over inventory slot
                    {
                        if (hoveringOnAssemblerOutput)
                        {
                            List<(ItemID, int)> recipeMatch = inventorySystem.assembler.FindRecipeMatch();
                            if (recipeMatch != null) // Can take from output slot
                            {
                                // Use input items
                                for (int i = 0; i < 3; i++)
                                {
                                    for (int j = 0; j < 3; j++)
                                    {
                                        InventorySlot inputSlot = inventorySystem.assembler.inputSlots[i][j];
                                        if (inputSlot.itemID != ItemID.none)
                                        {
                                            foreach (var recipeItem in recipeMatch)
                                            {
                                                ItemID recipeItemID = recipeItem.Item1;
                                                int recipeItemAmount = recipeItem.Item2;
                                                if (recipeItemID == inputSlot.itemID)
                                                {
                                                    inputSlot.amount -= recipeItemAmount;
                                                    if (inputSlot.amount == 0)
                                                        inputSlot.itemID = ItemID.none;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }

                                if (Input.GetKey(KeyCode.LeftShift))
                                {
                                    AddToInventory(recipeMatch[0].Item1, recipeMatch[0].Item2);
                                }
                                else if (!holdingStack) // Put output in hand
                                {
                                    heldItemID = recipeMatch[0].Item1;
                                    heldItemAmount = recipeMatch[0].Item2;
                                }
                                craftSource.Play();
                            }
                        }
                        else
                        {
                            bool hoveredSlotHasStack = hoveredSlot.itemID != ItemID.none;
                            bool sameItemID = heldItemID == hoveredSlot.itemID;
                            
                            if (leftClick)
                            {
                                if (!holdingStack) // Not holding anything
                                {
                                    if (hoveredSlotHasStack) // Pick up stack
                                    {
                                        if (Input.GetKey(KeyCode.LeftShift))
                                        {
                                            ItemID hoveredItemID = hoveredSlot.itemID;
                                            int hoveredItemAmount = hoveredSlot.amount;
                                            hoveredSlot.itemID = ItemID.none;
                                            hoveredSlot.amount = 0;
                                            AddToInventory(hoveredItemID, hoveredItemAmount);
                                        }
                                        else
                                        {
                                            heldItemID = hoveredSlot.itemID;
                                            heldItemAmount = hoveredSlot.amount;
                                            hoveredSlot.itemID = ItemID.none;
                                            hoveredSlot.amount = 0;
                                        }
                                    }
                                }
                                else // Holding a stack
                                {
                                    if (hoveredSlotHasStack) // Slot has a stack
                                    {
                                        if (Input.GetKey(KeyCode.LeftShift))
                                        {
                                            ItemID hoveredItemID = hoveredSlot.itemID;
                                            int hoveredItemAmount = hoveredSlot.amount;
                                            hoveredSlot.itemID = ItemID.none;
                                            hoveredSlot.amount = 0;
                                            AddToInventory(hoveredItemID, hoveredItemAmount);
                                        }
                                        else
                                        {
                                            if (sameItemID) // Add as many to slot stack as possible
                                            {
                                                int amountToAdd = Mathf.Clamp(heldItemAmount, 0, 999 - hoveredSlot.amount);
                                                hoveredSlot.amount += amountToAdd;
                                                heldItemAmount -= amountToAdd;
                                            }
                                            else // Swap slot stack with held stack
                                            {
                                                ItemID hoveredItemID = hoveredSlot.itemID;
                                                int hoveredItemAmount = hoveredSlot.amount;

                                                hoveredSlot.itemID = heldItemID;
                                                hoveredSlot.amount = heldItemAmount;
                                                heldItemID = hoveredItemID;
                                                heldItemAmount = hoveredItemAmount;
                                                //(hoveredSlot.itemID, hoveredSlot.amount) = (heldItemID, heldItemAmount);

                                                if (hoveredSlot == inventorySystem.scannerSlot)
                                                    scannerSource.Play();
                                            }
                                        }
                                    }
                                    else // Place stack in empty slot
                                    {
                                        hoveredSlot.itemID = heldItemID;
                                        hoveredSlot.amount = heldItemAmount;
                                        heldItemID = ItemID.none;
                                        heldItemAmount = 0;

                                        if (hoveredSlot == inventorySystem.scannerSlot)
                                            scannerSource.Play();
                                    }
                                }
                            }
                            else if (rightClick)
                            {
                                if (!holdingStack) // Not holding anything
                                {
                                    if (hoveredSlotHasStack) // Pick up half of stack
                                    {
                                        if (hoveredSlot.amount == 1)
                                        {
                                            heldItemID = hoveredSlot.itemID;
                                            heldItemAmount = 1;
                                            hoveredSlot.itemID = ItemID.none;
                                            hoveredSlot.amount = 0;
                                        }
                                        else
                                        {
                                            int amountToTake = Mathf.FloorToInt(hoveredSlot.amount / 2);
                                            hoveredSlot.amount -= amountToTake;
                                            heldItemID = hoveredSlot.itemID;
                                            heldItemAmount = amountToTake;
                                        }
                                    }
                                }
                                else // Holding a stack
                                {
                                    if (hoveredSlotHasStack)
                                    {
                                        if (sameItemID && hoveredSlot.amount < 999) // Add one to slot stack (if possible)
                                        {
                                            hoveredSlot.amount++;
                                            heldItemAmount--;
                                        }
                                        else // Swap slot stack with held stack
                                        {
                                            ItemID hoveredItemID = hoveredSlot.itemID;
                                            int hoveredItemAmount = hoveredSlot.amount;
                                            
                                            hoveredSlot.itemID = heldItemID;
                                            hoveredSlot.amount = heldItemAmount;
                                            heldItemID = hoveredItemID;
                                            heldItemAmount = hoveredItemAmount;
                                            //(hoveredSlot.itemID, hoveredSlot.amount) = (heldItemID, heldItemAmount);

                                            if (hoveredSlot == inventorySystem.scannerSlot)
                                                scannerSource.Play();
                                        }
                                    }
                                    else // Place one into empty slot
                                    {
                                        hoveredSlot.itemID = heldItemID;
                                        hoveredSlot.amount = 1;
                                        heldItemAmount--;

                                        if (hoveredSlot == inventorySystem.scannerSlot)
                                            scannerSource.Play();
                                    }
                                }
                            }
                        }
                    }
                }
            }

            UpdateInventoryUI();
        }
    }

    public void Damage(int amount)
    {
        health = Mathf.Max(0, health - amount);
        suitStatus = Mathf.Max(0, (int)(suitStatus - 0.8F*amount));
        if (amount == 25 && !blockBreakSource.isPlaying) // Brown mob; play "explode" sound
        {
            blockBreakSource.Play();
        }
    }

    private void ThrowStack(ItemID itemID, int amount)
    {
        GameObject blockObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blockObj.transform.position = camera.transform.position;
        blockObj.hideFlags = HideFlags.HideInHierarchy;

        Dropped droppedScript = blockObj.AddComponent<Dropped>();
        droppedScript.itemID = itemID;
        droppedScript.amount = amount;
        droppedScript.thrown = true;
        droppedScript.readyForPickup = false;

        if (Enum.TryParse(itemID.ToString(), out BlockID blockID)) // Throwing block (cube)
        {
            blockObj.GetComponent<MeshFilter>().mesh = blockMesh;
            blockObj.GetComponent<Renderer>().material = blockMaterials[(int)blockID];
        }
        else // Throwing non-block item (sprite)
        {
            droppedScript.isSprite = true;
            blockObj.GetComponent<Renderer>().material = spriteMaterial;

            if (itemID.ToString() == "none")
                blockObj.GetComponent<Renderer>().material.SetTexture("_BaseMap", MeshData.GetItemSprite(ItemID.none).texture);
            else
                blockObj.GetComponent<Renderer>().material.SetTexture("_BaseMap", MeshData.GetItemSprite(itemID).texture);
        }
        
        droppedScript.Drop();
    }

    private void CancelAllAnimations()
    {
        handAnimator.SetBool("IsPunching", false);
        pistolAnimator.SetBool("IsCharging", false);
        pistolSlideAnimator.SetBool("IsCharging", false);
        handAnimator.SetBool("IsDrilling", false);
        handParentAnimator.SetBool("IsDrilling", false);
        drillAnimator.SetBool("IsDrilling", false);
    }

    public bool IsDead()
    {
        return health == 0;
    }

    public ItemID GetSelectedItem()
    {
        return inventorySystem.GetSelectedItem();
    }

    public void AddToInventory(ItemID itemID, int amount)
    {
        int firstEmptyRow = -1;
        int firstEmptyCol = -1;
        bool addedToStack = false;
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                InventorySlot slot = inventorySystem.inventory.slots[i][j];

                // Find first empty slot (to be used if there's no existing stack)
                if (slot.itemID == ItemID.none && firstEmptyRow == -1)
                {
                    firstEmptyRow = i;
                    firstEmptyCol = j;
                }

                // Add to existing stack (if it exists)
                if (slot.itemID == itemID)
                {
                    slot.amount += amount;
                    addedToStack = true;
                    break;
                }
            }
        }

        if (!addedToStack && firstEmptyRow != -1) // No existing stack, but empty slot exists
        {
            inventorySystem.inventory.slots[firstEmptyRow][firstEmptyCol] = new InventorySlot(itemID, amount);
        }
    }

    private void UpdateInventoryUI()
    {
        // Update inventory slots
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                int k = i*10 + (j+1);
                InventorySlot slot = inventorySystem.inventory.slots[i][j];
                string itemName = slot.itemID.ToString();
                int amount = slot.amount;
                GameObject slotObj;

                if (i == 0) // Hotbar
                    slotObj = hotbarUI.transform.Find($"Slot {k}").gameObject;
                else
                    slotObj = inventoryUI.transform.Find($"Slot {k}").gameObject;

                // Set icon
                Image slotImage = slotObj.GetComponent<Image>();
                if (slot.itemID == ItemID.none)
                    slotImage.sprite = MeshData.GetItemSprite(ItemID.none);
                else
                    slotImage.sprite = MeshData.GetItemSprite(slot.itemID);

                // Set amount
                TMP_Text amountText = slotObj.transform.GetChild(0).GetComponent<TMP_Text>();
                if (amount < 2 || !InventorySystem.ItemIsStackable(slot.itemID))
                    amountText.text = "";
                else
                    amountText.text = $"{amount}";
            }
        }

        // Update selected hotbar slot
        foreach (Transform slot in hotbarUI.transform)
        {
            if (slot.gameObject.name.StartsWith("Slot"))
            {
                bool selected = slot.gameObject.name == $"Slot {inventorySystem.selectedHotbarSlot}";
                slot.Find("Selected").gameObject.SetActive(selected);
            }
        }

        // Update scanner
        if (inventorySystem.scannerSlot.itemID != ItemID.none)
        {
            Dictionary<ScannerData.DataType, string> scannerData = ScannerData.GetItemInfo(inventorySystem.scannerSlot.itemID);
            Image scannerSlotImage = scannerSlotObj.GetComponent<Image>();
            TMP_Text scannerSlotAmountText = scannerSlotObj.transform.Find("Amount").GetComponent<TMP_Text>();
            TMP_Text scannerSlotDescText = scannerSlotObj.transform.Find("Description").GetComponent<TMP_Text>();
            if (scannerData != null)
            {
                string itemType = scannerData[ScannerData.DataType.TYPE];
                string itemComposition = scannerData[ScannerData.DataType.COMPOSITION];
                string itemValue = scannerData[ScannerData.DataType.VALUE];

                scannerSlotImage.sprite = MeshData.GetItemSprite(inventorySystem.scannerSlot.itemID);
                scannerSlotAmountText.text = inventorySystem.scannerSlot.amount > 1 ? $"{inventorySystem.scannerSlot.amount}" : "";
                if (inventorySystem.scannerSlot.itemID.ToString().StartsWith("disk"))
                    scannerSlotDescText.text = $"TYPE: {itemType}\n\nCONTENTS: {itemComposition}";
                else
                    scannerSlotDescText.text = $"TYPE: {itemType}\n\nCOMPOSITION: {itemComposition}\n\nVALUE: {itemValue}";
            }
            else // No scanner data; just show item in slot
            {
                scannerSlotImage.sprite = MeshData.GetItemSprite(inventorySystem.scannerSlot.itemID);
                scannerSlotAmountText.text = inventorySystem.scannerSlot.amount > 1 ? $"{inventorySystem.scannerSlot.amount}" : "";
                scannerSlotDescText.text = "";
            }
        }
        else
        {
            Image scannerSlotImage = scannerSlotObj.GetComponent<Image>();
            scannerSlotImage.sprite = MeshData.GetItemSprite(ItemID.none);

            TMP_Text scannerSlotAmountText = scannerSlotObj.transform.Find("Amount").GetComponent<TMP_Text>();
            scannerSlotAmountText.text = "";

            TMP_Text scannerSlotDescText = scannerSlotObj.transform.Find("Description").GetComponent<TMP_Text>();
            scannerSlotDescText.text = "Insert item to receive scanning information.";
        }

        // Update held stack UI
        Image heldStackImage = heldStack.GetComponent<Image>();
        if (heldItemID == ItemID.none || heldItemAmount == 0)
            heldStackImage.sprite = MeshData.GetItemSprite(ItemID.none);
        else
            heldStackImage.sprite = MeshData.GetItemSprite(heldItemID);

        TMP_Text heldStackAmountText = heldStack.transform.GetChild(0).GetComponent<TMP_Text>();
        if (heldItemAmount < 2 || !InventorySystem.ItemIsStackable(heldItemID))
            heldStackAmountText.text = "";
        else
            heldStackAmountText.text = $"{heldItemAmount}";

        // Update spacesuit
        Image helmetSlotImage = spacesuitObj.transform.Find("HelmetSlot").GetComponent<Image>();
        TMP_Text helmetSlotAmount = helmetSlotImage.transform.GetChild(0).GetComponent<TMP_Text>();
        helmetSlotAmount.text = inventorySystem.spacesuit.helmetSlot.amount > 1 ? $"{inventorySystem.spacesuit.helmetSlot.amount}" : "";
        if (inventorySystem.spacesuit.helmetSlot.itemID == ItemID.none || inventorySystem.spacesuit.helmetSlot.amount == 0)
            helmetSlotImage.sprite = MeshData.GetItemSprite(ItemID.none);
        else
            helmetSlotImage.sprite = MeshData.GetItemSprite(inventorySystem.spacesuit.helmetSlot.itemID);
        
        Image batterySlotImage = spacesuitObj.transform.Find("BatterySlot").GetComponent<Image>();
        TMP_Text batterySlotAmount = batterySlotImage.transform.GetChild(0).GetComponent<TMP_Text>();
        batterySlotAmount.text = inventorySystem.spacesuit.batterySlot.amount > 1 ? $"{inventorySystem.spacesuit.batterySlot.amount}" : "";
        if (inventorySystem.spacesuit.batterySlot.itemID == ItemID.none || inventorySystem.spacesuit.batterySlot.amount == 0)
            batterySlotImage.sprite = MeshData.GetItemSprite(ItemID.none);
        else
            batterySlotImage.sprite = MeshData.GetItemSprite(inventorySystem.spacesuit.batterySlot.itemID);

        Image jetpackSlotImage = spacesuitObj.transform.Find("JetpackSlot").GetComponent<Image>();
        TMP_Text jetpackSlotAmount = jetpackSlotImage.transform.GetChild(0).GetComponent<TMP_Text>();
        jetpackSlotAmount.text = inventorySystem.spacesuit.jetpackSlot.amount > 1 ? $"{inventorySystem.spacesuit.jetpackSlot.amount}" : "";
        if (inventorySystem.spacesuit.jetpackSlot.itemID == ItemID.none || inventorySystem.spacesuit.jetpackSlot.amount == 0)
            jetpackSlotImage.sprite = MeshData.GetItemSprite(ItemID.none);
        else
            jetpackSlotImage.sprite = MeshData.GetItemSprite(inventorySystem.spacesuit.jetpackSlot.itemID);

        // Update assembler input grid
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                int k = (2 - i)*3 + (j+1);
                InventorySlot inputSlot = inventorySystem.assembler.inputSlots[i][j];
                GameObject slotObj = assemblerObj.transform.Find($"Slot {k}").gameObject;

                // Set icon
                Image slotImage = slotObj.GetComponent<Image>();
                if (inputSlot.itemID == ItemID.none || inputSlot.amount < 1)
                    slotImage.sprite = MeshData.GetItemSprite(ItemID.none);
                else
                    slotImage.sprite = MeshData.GetItemSprite(inputSlot.itemID);

                // Set amount
                TMP_Text amountText = slotObj.transform.GetChild(0).GetComponent<TMP_Text>();
                if (inputSlot.amount < 2 || !InventorySystem.ItemIsStackable(inputSlot.itemID))
                    amountText.text = "";
                else
                    amountText.text = $"{inputSlot.amount}";
            }
        }

        // Update assembler output
        List<(ItemID, int)> recipeMatch = inventorySystem.assembler.FindRecipeMatch();
        Image assemblerOutputImage = assemblerObj.transform.Find("Output").GetComponent<Image>();
        TMP_Text assemblerOutputAmount = assemblerOutputImage.transform.Find("Amount").GetComponent<TMP_Text>();
        if (recipeMatch != null)
        {
            (ItemID, int) recipeOutput = recipeMatch[0];
            assemblerOutputImage.sprite = MeshData.GetItemSprite(recipeOutput.Item1);
            assemblerOutputAmount.text = recipeOutput.Item2 > 1 ? $"{recipeOutput.Item2}" : "";
        }
        else
        {
            assemblerOutputImage.sprite = MeshData.GetItemSprite(ItemID.none);
            assemblerOutputAmount.text = "";
        }

        // Update health and suit status
        healthImage.fillAmount = health / 100F;
        suitStatusImage.fillAmount = suitStatus / 100F;
    }

    public void BackToMainMenu()
    {
        // Reset player data
        PlayerData playerData = new PlayerData();
        playerData.positionX = GameData.CHUNK_SIZE / 2F;
        playerData.positionY = 100;
        playerData.positionZ = GameData.CHUNK_SIZE / 2F;
        playerData.rotationY = GetComponent<FPSController>().GetPlayerRotationY();
        playerData.cameraRotationX = GetComponent<FPSController>().GetCameraRotationX();
        playerData.inventorySystem = inventorySystem;
        playerData.health = 100;
        playerData.suitStatus = 100;
        using (FileStream fileStream = new FileStream(playerDataPath, FileMode.Create, FileAccess.Write))
            (new BinaryFormatter()).Serialize(fileStream, playerData);

        SceneManager.LoadScene("MainMenu");
    }

    public int GetJetpackLevel()
    {
        if (inventorySystem.spacesuit.jetpackSlot.itemID == ItemID.jetpack_t1)
            return 1;
        else if (inventorySystem.spacesuit.jetpackSlot.itemID == ItemID.jetpack_t2)
            return 2;
        else if (inventorySystem.spacesuit.jetpackSlot.itemID == ItemID.jetpack_t3)
            return 3;
        else
            return 0;
    }

    public float GetJetpackFuel()
    {
        return jetpackFuel;
    }

    public void SetJetpackFuel(float level)
    {
        jetpackFuel = Mathf.Clamp(level, 0, 1);
    }

    public void DecrementSelectedItem()
    {
        InventorySlot selectedSlot = inventorySystem.inventory.slots[0][inventorySystem.selectedHotbarSlot - 1];
        selectedSlot.amount = Mathf.Clamp(selectedSlot.amount - 1, 0, 999);
        if (selectedSlot.amount == 0)
            selectedSlot.itemID = ItemID.none;
        UpdateInventoryUI();
    }

    public void SavePlayerData()
    {
        // Update player data
        PlayerData playerData = new PlayerData();
        playerData.positionX = transform.position.x;
        playerData.positionY = transform.position.y;
        playerData.positionZ = transform.position.z;
        playerData.rotationY = GetComponent<FPSController>().GetPlayerRotationY();
        playerData.cameraRotationX = GetComponent<FPSController>().GetCameraRotationX();
        playerData.inventorySystem = inventorySystem;
        playerData.health = health;
        playerData.suitStatus = suitStatus;
        using (FileStream fileStream = new FileStream(playerDataPath, FileMode.Create, FileAccess.Write))
            (new BinaryFormatter()).Serialize(fileStream, playerData);

        // Update distance traveled
        int moon = PlayerPrefs.GetInt("moon");
        string moonDataFile = string.Format("{0}/moons/moon{1}/moon.dat", Application.persistentDataPath, moon);
        MoonData moonData = new MoonData();
        using (FileStream file = File.Open(moonDataFile, FileMode.Open, FileAccess.Read))
            moonData = (MoonData)(new BinaryFormatter()).Deserialize(file);
        moonData.distanceTraveled += distanceTraveledThisSession;
        using (FileStream file = File.Open(moonDataFile, FileMode.Create, FileAccess.Write))
            (new BinaryFormatter()).Serialize(file, moonData);
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
        if (selectionCube.activeSelf)
            return selectionInfo.collider.gameObject;
        else
            return null;
    }
}
