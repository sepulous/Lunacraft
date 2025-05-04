using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class InventorySlot
{
    public ItemID itemID;
    public int amount;

    public InventorySlot(ItemID itemID, int amount)
    {
        this.itemID = itemID;
        this.amount = amount;
    }
}

[Serializable]
public class Inventory
{
    public InventorySlot[][] slots;

    public Inventory(bool creativeMode)
    {
        slots = new InventorySlot[5][];
        for (int i = 0; i < 5; i++)
        {
            slots[i] = new InventorySlot[10];
            for (int j = 0; j < 10; j++)
                slots[i][j] = new InventorySlot(ItemID.none, 0);
        }

        
        if (creativeMode) // Default creative loadout
        {
            slots[4][0] = new InventorySlot(ItemID.aluminum, 999);
            slots[4][1] = new InventorySlot(ItemID.aluminum_ore, 999);
            slots[4][2] = new InventorySlot(ItemID.amethyst_ore, 999);
            slots[4][3] = new InventorySlot(ItemID.beryllium, 999);
            slots[4][4] = new InventorySlot(ItemID.blue_crystal, 999);
            slots[4][5] = new InventorySlot(ItemID.boron_crystal, 999);
            slots[4][6] = new InventorySlot(ItemID.sulphur_crystal, 999);
            slots[4][7] = new InventorySlot(ItemID.calcite, 999);
            slots[4][8] = new InventorySlot(ItemID.carbon, 999);
            slots[4][9] = new InventorySlot(ItemID.chalchanthite, 999);

            slots[3][0] = new InventorySlot(ItemID.dirt, 999);
            slots[3][1] = new InventorySlot(ItemID.feldspar, 999);
            slots[3][2] = new InventorySlot(ItemID.glass, 999);
            slots[3][3] = new InventorySlot(ItemID.gold_ore, 999);
            slots[3][4] = new InventorySlot(ItemID.granite, 999);
            slots[3][5] = new InventorySlot(ItemID.graphite, 999);
            slots[3][6] = new InventorySlot(ItemID.gravel, 999);
            slots[3][7] = new InventorySlot(ItemID.light, 999);
            slots[3][8] = new InventorySlot(ItemID.magnetite, 999);
            slots[3][9] = new InventorySlot(ItemID.molybdenum_ore, 999);

            slots[2][0] = new InventorySlot(ItemID.moon_bark, 999);
            slots[2][1] = new InventorySlot(ItemID.moon_leaf, 999);
            slots[2][2] = new InventorySlot(ItemID.moon_wood, 999);
            slots[2][3] = new InventorySlot(ItemID.neptunium, 999);
            slots[2][4] = new InventorySlot(ItemID.notchium, 999);
            slots[2][5] = new InventorySlot(ItemID.notchium_ore, 999);
            slots[2][6] = new InventorySlot(ItemID.phosphate, 999);
            slots[2][7] = new InventorySlot(ItemID.polymer, 999);
            slots[2][8] = new InventorySlot(ItemID.quartz_ore, 999);
            slots[2][9] = new InventorySlot(ItemID.rock, 999);

            slots[1][0] = new InventorySlot(ItemID.sand, 999);
            slots[1][1] = new InventorySlot(ItemID.zircon_ore, 999);
            slots[1][2] = new InventorySlot(ItemID.silver_ore, 999);
            slots[1][3] = new InventorySlot(ItemID.shale_gravel, 999);
            slots[1][4] = new InventorySlot(ItemID.minilight, 999);

            slots[0][0] = new InventorySlot(ItemID.drill_t3, 1);
            slots[0][1] = new InventorySlot(ItemID.slug_pistol_t3, 1);
            slots[0][2] = new InventorySlot(ItemID.medkit, 1);
            slots[0][3] = new InventorySlot(ItemID.disk, 1);
            slots[0][4] = new InventorySlot(ItemID.chronobooster, 1);
            slots[0][5] = new InventorySlot(ItemID.chronowinder, 1);
            slots[0][6] = new InventorySlot(ItemID.camera, 1);
        }
        else // Default explore loadout
        {
            slots[0][0] = new InventorySlot(ItemID.drill_t1, 1);
            slots[0][2] = new InventorySlot(ItemID.slug_pistol_t1, 1);
            slots[0][3] = new InventorySlot(ItemID.medkit, 1);
            slots[0][4] = new InventorySlot(ItemID.disk, 1);
            slots[0][9] = new InventorySlot(ItemID.camera, 1);
        }
    }
}

[Serializable]
public class Spacesuit
{
    public InventorySlot helmetSlot;
    public InventorySlot batterySlot;
    public InventorySlot jetpackSlot;

    public Spacesuit(bool creativeMode)
    {
        helmetSlot = new InventorySlot(ItemID.none, 0);
        batterySlot = new InventorySlot(ItemID.battery, 1);
        jetpackSlot = new InventorySlot(creativeMode ? ItemID.jetpack_t3 : ItemID.jetpack_t1, 1);
    }
}

[Serializable]
public class Assembler
{
    public InventorySlot[][] inputSlots;

    public Assembler()
    {
        inputSlots = new InventorySlot[3][];
        for (int i = 0; i < 3; i++)
        {
            inputSlots[i] = new InventorySlot[3];
            for (int j = 0; j < 3; j++)
            {
                inputSlots[i][j] = new InventorySlot(ItemID.none, 0);
            }
        }
    }

    public List<(ItemID, int)> FindRecipeMatch()
    {
        List<(ItemID, int)> recipeMatch = null;

        List<(ItemID, int)> inputPattern = new List<(ItemID, int)>();
        List<(ItemID, int)> trimmedInputPattern = new List<(ItemID, int)>();
        for (int i = 2; i >= 0; i--)
        {
            for (int j = 0; j < 3; j++)
            {
                InventorySlot slot = inputSlots[i][j];
                inputPattern.Add((slot.itemID, slot.amount));
                if (slot.itemID != ItemID.none)
                    trimmedInputPattern.Add((slot.itemID, slot.amount));
            }
        }

        foreach (List<(ItemID, int)> recipe in CraftingRecipes.RECIPES)
        {
            bool recipeMatchFound = true;

            if (recipe.Count == 10) // Entire pattern is specified
            {
                for (int i = 1; i < 10; i++) // Skip first element; it's the result
                {
                    (ItemID, int) recipeItem = recipe[i];
                    (ItemID, int) inputItem = inputPattern[i-1];
                    if (recipeItem.Item1 != inputItem.Item1 || recipeItem.Item2 > inputItem.Item2) // Pattern doesn't match recipe
                    {
                        recipeMatchFound = false;
                        break;
                    }
                }
            }
            else // Only subpattern is specified
            {
                if (trimmedInputPattern.Count == recipe.Count - 1)
                {
                    for (int i = 1; i < recipe.Count; i++)
                    {
                        (ItemID, int) recipeItem = recipe[i];
                        (ItemID, int) inputItem = trimmedInputPattern[i-1];
                        if (recipeItem.Item1 != inputItem.Item1 || recipeItem.Item2 > inputItem.Item2) // Pattern doesn't match recipe
                        {
                            recipeMatchFound = false;
                            break;
                        }
                    }
                }
                else
                {
                    recipeMatchFound = false;
                }
            }

            if (recipeMatchFound)
            {
                recipeMatch = recipe;
                break;
            }
        }

        return recipeMatch;
    }
}

[Serializable]
public class InventorySystem
{
    public Inventory inventory;
    public InventorySlot scannerSlot;
    public Spacesuit spacesuit;
    public Assembler assembler;
    public int selectedHotbarSlot;
    private static List<ItemID> stackableItems = new List<ItemID> {
        ItemID.drill_t1, ItemID.drill_t2, ItemID.drill_t3,
        ItemID.slug_pistol_t1, ItemID.slug_pistol_t2, ItemID.slug_pistol_t3,
        ItemID.jetpack_t1, ItemID.jetpack_t2, ItemID.jetpack_t3,
        ItemID.camera, ItemID.chronobooster, ItemID.chronowinder, ItemID.medkit
    };

    public InventorySystem(bool creativeMode)
    {
        inventory = new Inventory(creativeMode);
        scannerSlot = new InventorySlot(ItemID.none, 0);
        spacesuit = new Spacesuit(creativeMode);
        assembler = new Assembler();
        selectedHotbarSlot = 1;
    }

    public InventorySystem(Inventory _inventory, InventorySlot _scannerSlot, Spacesuit _spacesuit, Assembler _assembler, int _selectedHotbarSlot)
    {
        inventory = _inventory;
        scannerSlot = _scannerSlot;
        spacesuit = _spacesuit;
        assembler = _assembler;
        selectedHotbarSlot = _selectedHotbarSlot;
    }

    public ItemID GetSelectedItem()
    {
        return inventory.slots[0][selectedHotbarSlot - 1].itemID;
    }

    public static bool ItemIsStackable(ItemID itemID)
    {
        return !(itemID.ToString().StartsWith("disk") || stackableItems.Contains(itemID));
    }
}
