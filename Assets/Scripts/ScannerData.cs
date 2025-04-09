using System.Collections.Generic;

class ScannerData
{
    public enum DataType {TYPE, COMPOSITION, VALUE};
    private static Dictionary<ItemID, Dictionary<DataType, string>> data = new Dictionary<ItemID, Dictionary<DataType, string>>
    {
        {
            ItemID.aluminum,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Metal"},
                {DataType.COMPOSITION, "Aluminum (Refined)"},
                {DataType.VALUE, "9.3 kC"}
            }
        },
        {
            ItemID.aluminum_ore,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Metal"},
                {DataType.COMPOSITION, "Aluminum (Impure)"},
                {DataType.VALUE, "4.6 kC"}
            }
        },
        {
            ItemID.amethyst_ore,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Mineral"},
                {DataType.COMPOSITION, "Amethyst Ore"},
                {DataType.VALUE, "0.8 kC"}
            }
        },
        {
            ItemID.beryllium,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Mineral"},
                {DataType.COMPOSITION, "Beryllium Ore"},
                {DataType.VALUE, "6.0 kC"}
            }
        },
        {
            ItemID.blue_crystal,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Crystal"},
                {DataType.COMPOSITION, "Aluminum Oxide"},
                {DataType.VALUE, "38.0 kC"}
            }
        },
        {
            ItemID.boron_crystal,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Crystal"},
                {DataType.COMPOSITION, "Boron Rubelite"},
                {DataType.VALUE, "108.3 kC"}
            }
        },
        {
            ItemID.calcite,
            new Dictionary<DataType, string> {
                {DataType.TYPE, ""},
                {DataType.COMPOSITION, ""},
                {DataType.VALUE, ""}
            }
        },
        {
            ItemID.carbon,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Mineral"},
                {DataType.COMPOSITION, "Calcite Ore"},
                {DataType.VALUE, "0.4 kC"}
            }
        },
        {
            ItemID.chalchanthite,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Mineral"},
                {DataType.COMPOSITION, "Chalcanthite"},
                {DataType.VALUE, "4.8 kC"}
            }
        },
        {
            ItemID.dirt,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Exolunar soil"},
                {DataType.COMPOSITION, "Disintegrated basaltic rock"},
                {DataType.VALUE, "0.0 kC"}
            }
        },
        {
            ItemID.feldspar,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Mineral"},
                {DataType.COMPOSITION, "Quartz/Feldspar"},
                {DataType.VALUE, "0.2 kC"}
            }
        },
        {
            ItemID.glass,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Hyperglass"},
                {DataType.COMPOSITION, "Synthetic Silicate"},
                {DataType.VALUE, "18.0 kC"}
            }
        },
        {
            ItemID.gold_ore,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Transition Metal"},
                {DataType.COMPOSITION, "Elemental Gold"},
                {DataType.VALUE, "81.9 kC"}
            }
        },
        {
            ItemID.granite,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Mineral"},
                {DataType.COMPOSITION, "Pink Granite"},
                {DataType.VALUE, "0.8 kC"}
            }
        },
        {
            ItemID.graphite,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Mineral"},
                {DataType.COMPOSITION, "Graphite"},
                {DataType.VALUE, "1.0 kC"}
            }
        },
        {
            ItemID.gravel,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Exolunar gravel"},
                {DataType.COMPOSITION, "Disintigrated basaltic rock"},
                {DataType.VALUE, "0.0 kC"}
            }
        },
        {
            ItemID.light,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Light Source"},
                {DataType.COMPOSITION, "Luminous organic material"},
                {DataType.VALUE, "4.5 kC"}
            }
        },
        {
            ItemID.magnetite,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Ore"},
                {DataType.COMPOSITION, "Magnetite"},
                {DataType.VALUE, "8.1 kC"}
            }
        },
        {
            ItemID.molybdenum_ore,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Transition Metal"},
                {DataType.COMPOSITION, "Elemental Molybdenum"},
                {DataType.VALUE, "11.3 kC"}
            }
        },
        {
            ItemID.moon_bark,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Xenobiological"},
                {DataType.COMPOSITION, "Fibrous silicates"},
                {DataType.VALUE, "???"}
            }
        },
        {
            ItemID.moon_leaf,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Xenobiological"},
                {DataType.COMPOSITION, "Photosynthetic organic material"},
                {DataType.VALUE, "0.3 kC"}
            }
        },
        {
            ItemID.moon_wood,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Xenobiological"},
                {DataType.COMPOSITION, "Fibrous silicates"},
                {DataType.VALUE, "4.0 kC"}
            }
        },
        {
            ItemID.neptunium,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Radioactive"},
                {DataType.COMPOSITION, "Neptunium"},
                {DataType.VALUE, "15000.0 kc"}
            }
        },
        {
            ItemID.notchium,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Unknown Metal"},
                {DataType.COMPOSITION, "Notchium (Refined)"},
                {DataType.VALUE, "???"}
            }
        },
        {
            ItemID.notchium_ore,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Unknown Metal"},
                {DataType.COMPOSITION, "Notchium"},
                {DataType.VALUE, "???"}
            }
        },
        {
            ItemID.phosphate,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Mineral"},
                {DataType.COMPOSITION, "Hydrous Phosphate of Copper, Aluminum"},
                {DataType.VALUE, "6.6 kC"}
            }
        },
        {
            ItemID.polymer,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Security Structure"},
                {DataType.COMPOSITION, "Polymer"},
                {DataType.VALUE, "150.0 kc"}
            }
        },
        {
            ItemID.quartz_ore,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Mineral"},
                {DataType.COMPOSITION, "Quartz Ore"},
                {DataType.VALUE, "1.2 kC"}
            }
        },
        {
            ItemID.rock,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Mineral"},
                {DataType.COMPOSITION, "Basaltic Rock"},
                {DataType.VALUE, "0.0 kC"}
            }
        },
        {
            ItemID.sand,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Exolunar sand"},
                {DataType.COMPOSITION, "Silicate mix"},
                {DataType.VALUE, "0.0 kC"}
            }
        },
        {
            ItemID.shale_gravel,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Ore"},
                {DataType.COMPOSITION, "Petrochem Shale"},
                {DataType.VALUE, "40.0 kC"}
            }
        },
        {
            ItemID.silver_ore,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Transition Metal"},
                {DataType.COMPOSITION, "Elemental Silver"},
                {DataType.VALUE, "52.0 kC"}
            }
        },
        {
            ItemID.snow,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Snow"},
                {DataType.COMPOSITION, "Crystalline hydrogen fluoride"},
                {DataType.VALUE, "???"}
            }
        },
        {
            ItemID.sulphur_crystal,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Crystal"},
                {DataType.COMPOSITION, "Elemental Sulphur"},
                {DataType.VALUE, "38.8 kC"}
            }
        },
        {
            ItemID.sulphur_ore,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Mineral"},
                {DataType.COMPOSITION, "Sulphur Ore"},
                {DataType.VALUE, "1.3 kC"}
            }
        },
        {
            ItemID.titanium,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Transition Metal"},
                {DataType.COMPOSITION, "Titanium (Refined)"},
                {DataType.VALUE, "40.6 kC"}
            }
        },
        {
            ItemID.titanium_ore,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Transition Metal"},
                {DataType.COMPOSITION, "Titanium"},
                {DataType.VALUE, "20.3 kC"}
            }
        },
        {
            ItemID.water,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Exolunar Ice"},
                {DataType.COMPOSITION, "Hydrogen Fluoride"},
                {DataType.VALUE, "0.0 kC"}
            }
        },
        {
            ItemID.xenostone,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Xenobiological"},
                {DataType.COMPOSITION, "Crystalline silicates"},
                {DataType.VALUE, "2.1 kC"}
            }
        },
        {
            ItemID.zircon_ore,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Mineral"},
                {DataType.COMPOSITION, "Zircon Ore"},
                {DataType.VALUE, "3.3 kc"}
            }
        },
        {
            ItemID.beacon,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Beacon"},
                {DataType.COMPOSITION, "Quantum Transmitter"},
                {DataType.VALUE, "480.0 kC"}
            }
        },
        {
            ItemID.disk,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Data"},
                {DataType.COMPOSITION, "Split items with right click."},
                {DataType.VALUE, ""}
            }
        },
        {
            ItemID.disk1,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Data"},
                {DataType.COMPOSITION, "Encase biology and machine to heal."},
                {DataType.VALUE, ""}
            }
        },
        {
            ItemID.disk2,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Data"},
                {DataType.COMPOSITION, "Aluminum batteries can be made from glowplants."},
                {DataType.VALUE, ""}
            }
        },
        {
            ItemID.disk3,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Data"},
                {DataType.COMPOSITION, "The slug pistol uses a magnet and needs energy."},
                {DataType.VALUE, ""}
            }
        },
        {
            ItemID.disk4,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Data"},
                {DataType.COMPOSITION, "Jetpacks use a lot of energy and need a strong case."},
                {DataType.VALUE, ""}
            }
        },
        {
            ItemID.disk5,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Data"},
                {DataType.COMPOSITION, "The best machines are made from the best aluminum."},
                {DataType.VALUE, ""}
            }
        },
        {
            ItemID.disk6,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Data"},
                {DataType.COMPOSITION, "Crystals have an exotic energy in them."},
                {DataType.VALUE, ""}
            }
        },
        {
            ItemID.disk7,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Data"},
                {DataType.COMPOSITION, "My boss\'s slug pistol uses an energy orb."},
                {DataType.VALUE, ""}
            }
        },
        {
            ItemID.disk8,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Data"},
                {DataType.COMPOSITION, "Refine minerals to build useful items."},
                {DataType.VALUE, ""}
            }
        },
        {
            ItemID.disk9,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Data"},
                {DataType.COMPOSITION, "Pistols are made in a pistol-shape."},
                {DataType.VALUE, ""}
            }
        },
        {
            ItemID.disk10,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Data"},
                {DataType.COMPOSITION, "Turrets are powered, armored, and armed."},
                {DataType.VALUE, ""}
            }
        },
        {
            ItemID.disk11,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Data"},
                {DataType.COMPOSITION, "Drills use two machines, a power source, and a metal bit."},
                {DataType.VALUE, ""}
            }
        },
        {
            ItemID.disk12,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Data"},
                {DataType.COMPOSITION, "Energy orbs require neptunium."},
                {DataType.VALUE, ""}
            }
        },
        {
            ItemID.disk13,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Data"},
                {DataType.COMPOSITION, "Neptunium comes from the rarest crystal."},
                {DataType.VALUE, ""}
            }
        },
        {
            ItemID.disk14,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Data"},
                {DataType.COMPOSITION, "Combine alien bio and machines to manipulate time."},
                {DataType.VALUE, ""}
            }
        },
        {
            ItemID.disk15,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Data"},
                {DataType.COMPOSITION, "Jetpacks use three energy units."},
                {DataType.VALUE, ""}
            }
        },
        {
            ItemID.disk16,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Data"},
                {DataType.COMPOSITION, "Quality metal is needed for pistol stocks and barrels."},
                {DataType.VALUE, ""}
            }
        },
        {
            ItemID.disk17,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Data"},
                {DataType.COMPOSITION, "Dilute biogel to make post-it lights."},
                {DataType.VALUE, ""}
            }
        },
        {
            ItemID.disk18,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Data"},
                {DataType.COMPOSITION, "Barren moon? Distill wood for xenobiologicals."},
                {DataType.VALUE, ""}
            }
        },
        {
            ItemID.drill_t1,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Drill"},
                {DataType.COMPOSITION, "Electromechanical"},
                {DataType.VALUE, "120.0 kC"}
            }
        },
        {
            ItemID.drill_t2,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Drill II"},
                {DataType.COMPOSITION, "Electromechanical"},
                {DataType.VALUE, "240.0 kC"}
            }
        },
        {
            ItemID.drill_t3,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Drill III"},
                {DataType.COMPOSITION, "Electromechanical"},
                {DataType.VALUE, "960.0 kC"}
            }
        },
        {
            ItemID.jetpack_t1,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Jetpack I"},
                {DataType.COMPOSITION, "Microfusion Cell"},
                {DataType.VALUE, "250.0 kC"}
            }
        },
        {
            ItemID.jetpack_t2,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Jetpack II"},
                {DataType.COMPOSITION, "Multifusion Cell"},
                {DataType.VALUE, "500.0 kC"}
            }
        },
        {
            ItemID.jetpack_t3,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Jetpack III"},
                {DataType.COMPOSITION, "Alien Technology"},
                {DataType.VALUE, "3050.0 kC"}
            }
        },
        {
            ItemID.mechanism,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Mechanism"},
                {DataType.COMPOSITION, ""},
                {DataType.VALUE, ""}
            }
        },
        {
            ItemID.medkit,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Medkit"},
                {DataType.COMPOSITION, "Therapeutic Nanos"},
                {DataType.VALUE, "300.0 kC"}
            }
        },
        {
            ItemID.slug_pistol_t1,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Slug Pistol"},
                {DataType.COMPOSITION, "Electromechanical"},
                {DataType.VALUE, "199.9 kC"}
            }
        },
        {
            ItemID.slug_pistol_t2,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Slug Pistol II"},
                {DataType.COMPOSITION, "Electromagnetic"},
                {DataType.VALUE, "400.0 kC"}
            }
        },
        {
            ItemID.slug_pistol_t3,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Slug Pistol III"},
                {DataType.COMPOSITION, "Gravitic Mechanism"},
                {DataType.VALUE, "1400.0 kC"}
            }
        },
        {
            ItemID.turret_t1,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Security Robot"},
                {DataType.COMPOSITION, "Polymer"},
                {DataType.VALUE, "150.0 kC"}
            }
        },
        {
            ItemID.turret_t2,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Security Robot"},
                {DataType.COMPOSITION, "Polymer"},
                {DataType.VALUE, "450.0 kC"}
            }
        },
        {
            ItemID.turret_t3,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Security Robot"},
                {DataType.COMPOSITION, "Polymer"},
                {DataType.VALUE, "900.0 kC"}
            }
        },
        {
            ItemID.chronobooster,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Alien device"},
                {DataType.COMPOSITION, "Unknown"},
                {DataType.VALUE, "???"}
            }
        },
        {
            ItemID.chronowinder,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Alien device"},
                {DataType.COMPOSITION, "Unknown"},
                {DataType.VALUE, "???"}
            }
        },
        {
            ItemID.battery,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Energy Source"},
                {DataType.COMPOSITION, "Electrochemical Cells"},
                {DataType.VALUE, "90.5 kC"}
            }
        },
        {
            ItemID.energy_orb,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Energy Source"},
                {DataType.COMPOSITION, "Alien Technology"},
                {DataType.VALUE, "???"}
            }
        },
        {
            ItemID.power_crystal,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Energy Source"},
                {DataType.COMPOSITION, "Alien Technology"},
                {DataType.VALUE, "12050.0 kC"}
            }
        },
        {
            ItemID.minilight,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Electrical Light"},
                {DataType.COMPOSITION, "Mixed"},
                {DataType.VALUE, "10.0 kC"}
            }
        },
        {
            ItemID.magnet,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Magnet"},
                {DataType.COMPOSITION, "Iron Oxide/Barium Carbonate"},
                {DataType.VALUE, "50.0 kC"}
            }
        },
        {
            ItemID.biogel,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Xenobiological"},
                {DataType.COMPOSITION, "Biochemical Silicates"},
                {DataType.VALUE, "60.0 kC"}
            }
        },
        {
            ItemID.gold,
            new Dictionary<DataType, string> {
                {DataType.TYPE, "Currency"},
                {DataType.COMPOSITION, "Gold"},
                {DataType.VALUE, "1000.0 kC"}
            }
        },
    };

    public static Dictionary<DataType, string> GetItemInfo(ItemID itemID)
    {
        if (!data.ContainsKey(itemID))
        {
            return null;
        }
        else
        {
            return data[itemID];
        }
    }
}
