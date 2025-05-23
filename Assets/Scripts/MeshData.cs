using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// TODO: Should probably make an Initialize() function so everything can be prepared at once, and I know when this happens

public class MeshData
{
    private static Mesh blockMesh = null;
    private static List<Material> blockMaterials = null;
    private static List<Sprite> sprites = null;

    public static Sprite GetItemSprite(ItemID itemID)
    {
        if (sprites == null)
        {
            Array itemNames = Enum.GetValues(typeof(ItemID));
            int itemCount = itemNames.Length;
            sprites = new List<Sprite>(itemCount);
            for (int i = 0; i < itemCount; i++)
            {
                sprites.Add(Resources.Load<Sprite>($"Textures/Items/{itemNames.GetValue(i)}_item"));
            }
        }

        return sprites[(int)itemID];
    }

    public static Mesh GetBlockMesh()
    {
        if (blockMesh == null)
            blockMesh = GenerateBlockMesh();

        return blockMesh;
    }

    public static List<Material> GetBlockMaterials()
    {
        if (blockMaterials == null)
            blockMaterials = GenerateBlockMaterials();

        return blockMaterials;
    }

    private static List<Material> GenerateBlockMaterials()
    {
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
                if (blockName == "sulphur_crystal")
                    material = new Material(Shader.Find("Custom/SulphurCrystalShader"));
                else
                    material = new Material(Shader.Find("Custom/BoronBlueCrystalShader"));
                material.SetTexture("_MainTex", texture);
            }
            else if (blockName == "light")
            {
                material = new Material(Shader.Find("Custom/LightBlockShader"));
                material.SetTexture("_BaseTexture", texture);
            }
            else if (blockName == "glass")
            {
                material = Resources.Load<Material>("GlassMaterial");
            }
            else
            {
                material = new Material(Shader.Find("Custom/OpaqueShader"));
                material.SetTexture("_BaseTexture", texture);
            }
            material.enableInstancing = true;

            blockMaterials.Add(material);
        }

        return blockMaterials;
    }

    private static Mesh GenerateBlockMesh()
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

        UnityEngine.Object.Destroy(tempCube);

        return mesh;
    }

    private static Vector3[] CalculateNormals(Vector3[] vertices, int[] triangles)
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
