using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class BlockReserve
{
    // private ObjectPool<GameObject> topsoilReserve;
    // private ObjectPool<GameObject> dirtReserve;
    // private ObjectPool<GameObject> gravelReserve;
    // private ObjectPool<GameObject> sandReserve;
    // private ObjectPool<GameObject> waterReserve;
    // private ObjectPool<GameObject> rockReserve;
    private Dictionary<BlockID, ObjectPool<GameObject>> reserves;
    private int topsoilReserveCapacity = 20000;
    private int dirtReserveCapacity = 1000;
    private int gravelReserveCapacity = 800;
    private int sandReserveCapacity = 8000;
    private int waterReserveCapacity = 20000;
    private int rockReserveCapacity = 600;
    private List<Material> blockMaterials;
    private Mesh blockMesh;

    public BlockReserve(List<Material> materials, Mesh mesh)
    {
        blockMaterials = materials;
        blockMesh = mesh;

        reserves = new Dictionary<BlockID, ObjectPool<GameObject>>();

        reserves.Add(BlockID.topsoil, new ObjectPool<GameObject>(
            CreateTopsoil,
            OnGetFromPool,
            OnReturnToPool,
            OnDestroyPooledObject,
            true,
            topsoilReserveCapacity,
            topsoilReserveCapacity
        ));

        reserves.Add(BlockID.dirt, new ObjectPool<GameObject>(
            CreateDirt,
            OnGetFromPool,
            OnReturnToPool,
            OnDestroyPooledObject,
            true,
            dirtReserveCapacity,
            dirtReserveCapacity
        ));

        reserves.Add(BlockID.gravel, new ObjectPool<GameObject>(
            CreateGravel,
            OnGetFromPool,
            OnReturnToPool,
            OnDestroyPooledObject,
            true,
            gravelReserveCapacity,
            gravelReserveCapacity
        ));

        reserves.Add(BlockID.sand, new ObjectPool<GameObject>(
            CreateSand,
            OnGetFromPool,
            OnReturnToPool,
            OnDestroyPooledObject,
            true,
            sandReserveCapacity,
            sandReserveCapacity
        ));

        reserves.Add(BlockID.water, new ObjectPool<GameObject>(
            CreateWater,
            OnGetFromPool,
            OnReturnToPool,
            OnDestroyPooledObject,
            true,
            waterReserveCapacity,
            waterReserveCapacity
        ));

        reserves.Add(BlockID.rock, new ObjectPool<GameObject>(
            CreateRock,
            OnGetFromPool,
            OnReturnToPool,
            OnDestroyPooledObject,
            true,
            rockReserveCapacity,
            rockReserveCapacity
        ));
    }

    public bool IsReserveBlock(BlockID blockID)
    {
        return reserves.ContainsKey(blockID);
    }

    // Wrapper function for pool.Get()
    public GameObject GetBlock(BlockID blockID)
    {
        return reserves[blockID].Get();
    }

    // Wrapper function for pool.Release()
    public void ReleaseBlock(GameObject blockObj)
    {
        BlockID blockID = blockObj.GetComponent<BlockData>().blockID;
        reserves[blockID].Release(blockObj);
    }

    // When an object is taken from the pool, activate it.
    void OnGetFromPool(GameObject pooledObject)
    {
        pooledObject.SetActive(true);
    }

    // When an object is returned to the pool, deactivate it.
    void OnReturnToPool(GameObject blockObj)
    {
        blockObj.SetActive(false);
        blockObj.transform.SetParent(null);
        blockObj.transform.position = new Vector3(0, -100, 0);
    }

    // When the pool discards an object, destroy the GameObject.
    void OnDestroyPooledObject(GameObject blockObj)
    {
        GameObject.Destroy(blockObj);
    }

    // Creates new block instance if reserve is empty
    GameObject CreateTopsoil()
    {
        GameObject blockObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blockObj.hideFlags = HideFlags.HideInHierarchy;
        blockObj.transform.position = new Vector3(0, -100, 0);
        blockObj.layer = LayerMask.NameToLayer("Block");
        blockObj.GetComponent<MeshFilter>().mesh = blockMesh;
        blockObj.GetComponent<Renderer>().material = blockMaterials[(int)BlockID.topsoil];
        blockObj.GetComponent<BoxCollider>().size = new Vector3(0.95F, 0.95F, 0.95F);
        return blockObj;
    }

    // Creates new block instance if reserve is empty
    GameObject CreateDirt()
    {
        GameObject blockObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blockObj.hideFlags = HideFlags.HideInHierarchy;
        blockObj.transform.position = new Vector3(0, -100, 0);
        blockObj.layer = LayerMask.NameToLayer("Block");
        blockObj.GetComponent<MeshFilter>().mesh = blockMesh;
        blockObj.GetComponent<Renderer>().material = blockMaterials[(int)BlockID.dirt];
        blockObj.GetComponent<BoxCollider>().size = new Vector3(0.95F, 0.95F, 0.95F);
        return blockObj;
    }

    // Creates new block instance if reserve is empty
    GameObject CreateGravel()
    {
        GameObject blockObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blockObj.hideFlags = HideFlags.HideInHierarchy;
        blockObj.transform.position = new Vector3(0, -100, 0);
        blockObj.layer = LayerMask.NameToLayer("Block");
        blockObj.GetComponent<MeshFilter>().mesh = blockMesh;
        blockObj.GetComponent<Renderer>().material = blockMaterials[(int)BlockID.gravel];
        blockObj.GetComponent<BoxCollider>().size = new Vector3(0.95F, 0.95F, 0.95F);
        return blockObj;
    }

    // Creates new block instance if reserve is empty
    GameObject CreateSand()
    {
        GameObject blockObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blockObj.hideFlags = HideFlags.HideInHierarchy;
        blockObj.transform.position = new Vector3(0, -100, 0);
        blockObj.layer = LayerMask.NameToLayer("Block");
        blockObj.GetComponent<MeshFilter>().mesh = blockMesh;
        blockObj.GetComponent<Renderer>().material = blockMaterials[(int)BlockID.sand];
        blockObj.GetComponent<BoxCollider>().size = new Vector3(0.95F, 0.95F, 0.95F);
        return blockObj;
    }

    // Creates new block instance if reserve is empty
    GameObject CreateWater()
    {
        GameObject blockObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blockObj.hideFlags = HideFlags.HideInHierarchy;
        blockObj.transform.position = new Vector3(0, -100, 0);
        blockObj.layer = LayerMask.NameToLayer("Block");
        blockObj.GetComponent<MeshFilter>().mesh = blockMesh;
        blockObj.GetComponent<Renderer>().material = blockMaterials[(int)BlockID.water];
        blockObj.GetComponent<BoxCollider>().size = new Vector3(0.95F, 0.95F, 0.95F);
        return blockObj;
    }

    // Creates new block instance if reserve is empty
    GameObject CreateRock()
    {
        GameObject blockObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blockObj.hideFlags = HideFlags.HideInHierarchy;
        blockObj.transform.position = new Vector3(0, -100, 0);
        blockObj.layer = LayerMask.NameToLayer("Block");
        blockObj.GetComponent<MeshFilter>().mesh = blockMesh;
        blockObj.GetComponent<Renderer>().material = blockMaterials[(int)BlockID.rock];
        blockObj.GetComponent<BoxCollider>().size = new Vector3(0.95F, 0.95F, 0.95F);
        return blockObj;
    }
}
