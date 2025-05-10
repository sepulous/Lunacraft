using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;

public class ChunkHelpers
{
    private static int[][] HEIGHT_MAPS = new int[4][] {
        new int[GameData.CHUNK_SIZE*GameData.CHUNK_SIZE],
        new int[GameData.CHUNK_SIZE*GameData.CHUNK_SIZE],
        new int[GameData.CHUNK_SIZE*GameData.CHUNK_SIZE],
        new int[GameData.CHUNK_SIZE*GameData.CHUNK_SIZE]
    };
    private static readonly int STRUCTURE_PADDING = 3; // How many blocks into the chunk structures can be spawned (keeping structures completely inside a chunk is simplest)
    // topBlock == BlockID.air || topBlock == BlockID.water || topBlock == BlockID.sulphur_crystal || topBlock == BlockID.boron_crystal || topBlock == BlockID.blue_crystal || topBlock == BlockID.glass
    private static readonly List<BlockID> NON_OPAQUE_BLOCKS = new List<BlockID> {
        BlockID.air, BlockID.water, BlockID.sulphur_crystal, BlockID.boron_crystal, BlockID.blue_crystal, BlockID.glass, BlockID.minilight_px, BlockID.minilight_nx, BlockID.minilight_pz, BlockID.minilight_nz
    };
    private static readonly (int, int, int)[][] CRYSTAL_PLANT_SHAPES = {
        new (int, int, int)[]
        {
            (2, 0, 0), // max extent

            (0, 1, 0), (0, 2, 0), (0, 3, 0), (-1, 2, 0), (1, 2, 0), (0, 2, -1),
            (0, 3, 1), (0, 3, 2), (0, 2, 2), (0, 4, 2), (-1, 3, 2)
        },
        new (int, int, int)[]
        {
            (3, 0, 0), // max extent

            (0, 1, 0), (0, 2, 0), (0, 3, 0), (0, 3, 1), (0, 3, 2), (0, 3, 3),
            (0, 2, 3), (0, 4, 3), (1, 3, 2), (2, 3, 2)
        },
        new (int, int, int)[]
        {
            (2, 0, 0), // max extent

            (0, 1, 0), (0, 2, 0), (0, 3, 0), (1, 3, 0), (2, 3, 0), (0, 2, -1),
            (0, 2, -2), (0, 2, 1), (0, 2, 2), (0, 1, 2), (0, 3, 2), (-1, 2, 2), (-2, 2, 2)
        },
        new (int, int, int)[]
        {
            (3, 0, 0), // max extent

            (0, 1, 0), (0, 2, 0), (0, 3, 0), (0, 4, 0), (-1, 4, 0), (-2, 4, 0),
            (-3, 4, 0), (1, 4, 0), (2, 4, 0), (3, 4, 0), (0, 4, 1), (0, 4, 2), (0, 4, 3)
        },
        new (int, int, int)[]
        {
            (3, 0, 0), // max extent

            (0, 1, 0), (0, 2, 0), (0, 3, 0), (0, 4, 0), (-1, 4, 0), (-2, 4, 0),
            (-3, 4, 0), (-3, 5, 0), (-3, 6, 0), (-3, 4, 1), (-3, 4, 2), (1, 4, 0), (2, 4, 0),
            (2, 4, 1), (2, 4, 2), (2, 4, -1), (2, 4, -2), (2, 5, 0), (2, 6, 0), (2, 3, 0), (2, 2, 0)
        },
        new (int, int, int)[]
        {
            (3, 0, 0), // max extent

            (0, 1, 0), (0, 2, 0), (-1, 2, 0), (-2, 2, 0), (-2, 1, 0), (-2, 2, 1),
            (-2, 2, -1), (1, 2, 0), (2, 2, 0), (2, 2, 1), (2, 2, -1), (2, 3, 0),
            (2, 1, 0), (0, 2, 1), (0, 2, 2), (0, 2, 3), (1, 2, 3), (0, 1, 3), (0, 3, 3)
        },
        new (int, int, int)[]
        {
            (3, 0, 0), // max extent

            (0, 1, 0), (0, 2, 0), (0, 3, 0), (-1, 3, 0), (-2, 3, 0), (-3, 3, 0),
            (-3, 4, 0), (-3, 5, 0), (-3, 2, 0), (-3, 1, 0), (1, 3, 0), (2, 3, 0),
            (3, 3, 0), (3, 3, 1), (3, 3, 2), (3, 3, -1), (3, 3, -2), (0, 3, 1),
            (0, 3, 2), (0, 3, 3), (-1, 3, 3), (-2, 3, 3), (1, 3, 3), (2, 3, 3), (0, 4, 3),
            (0, 5, 3), (0, 2, 3), (0, 1, 3)
        },
        new (int, int, int)[]
        {
            (3, 0, 0), // max extent

            (0, 1, 0), (0, 2, 0), (0, 3, 0), (-1, 3, 0), (-2, 3, 0), (-2, 4, 0),
            (-2, 5, 0), (-2, 2, 0), (-2, 1, 0), (1, 3, 0), (1, 4, 0), (2, 3, 0),
            (2, 2, 0), (0, 3, 1), (0, 3, 2), (0, 3, 3), (1, 3, 3), (0, 4, 3),
            (-1, 3, 3), (-2, 3, 3)
        }
    };

    private static readonly (BlockID, int, int, int)[][] GREEN_LIGHT_TREE_SHAPES = {
        new (BlockID, int, int, int)[]
        {
            (BlockID.moon_leaf, 8, 0, 0), // max extent

            (BlockID.moon_leaf, 0, 1, 1), (BlockID.moon_leaf, 0, 2, 1), (BlockID.moon_leaf, -1, 1, 1), (BlockID.moon_leaf, -1, 2, 1),
            (BlockID.moon_leaf, 1, 1, 1), (BlockID.moon_leaf, 1, 2, 1), (BlockID.moon_leaf, 2, 2, 1), (BlockID.moon_leaf, 1, 2, 2),
            (BlockID.moon_leaf, 0, 1, 2), (BlockID.moon_leaf, -1, 2, 2), (BlockID.moon_leaf, -1, 3, 2), (BlockID.moon_leaf, -1, 3, 1),
            (BlockID.moon_leaf, -2, 4, 1), (BlockID.moon_leaf, -2, 4, 2), (BlockID.moon_leaf, -2, 3, 2), (BlockID.moon_leaf, -2, 3, 1),
            (BlockID.moon_leaf, -3, 5, 1), (BlockID.moon_leaf, -3, 5, 2), (BlockID.moon_leaf, -4, 6, 2), (BlockID.moon_leaf, -4, 5, 2),
            (BlockID.moon_leaf, -5, 7, 2), (BlockID.moon_leaf, -6, 7, 2), (BlockID.moon_leaf, -7, 7, 2), (BlockID.moon_leaf, -7, 6, 2),
            (BlockID.moon_leaf, -7, 6, 1), (BlockID.moon_leaf, 2, 3, 1), (BlockID.moon_leaf, 2, 3, 2), (BlockID.moon_leaf, 2, 4, 1),
            (BlockID.moon_leaf, 2, 4, 2), (BlockID.moon_leaf, 3, 5, 2), (BlockID.moon_leaf, 3, 5, 1), (BlockID.moon_leaf, 4, 5, 1),
            (BlockID.moon_leaf, 4, 6, 1), (BlockID.moon_leaf, 5, 6, 1), (BlockID.moon_leaf, 6, 6, 1), (BlockID.moon_leaf, 6, 5, 1),
            (BlockID.moon_leaf, 6, 5, 2),

            (BlockID.light, 7, 3, 2), (BlockID.light, 6, 3, 2), (BlockID.light, 7, 3, 1), (BlockID.light, 6, 3, 1),
            (BlockID.light, 7, 4, 2), (BlockID.light, 6, 4, 2), (BlockID.light, 7, 4, 1), (BlockID.light, 6, 4, 1),
            (BlockID.light, -8, 4, 0), (BlockID.light, -8, 5, 0), (BlockID.light, -7, 4, 0), (BlockID.light, -7, 5, 0),
            (BlockID.light, -7, 4, 1), (BlockID.light, -7, 5, 1), (BlockID.light, -8, 5, 1)
        },
        new (BlockID, int, int, int)[]
        {
            (BlockID.moon_leaf, 9, 0, 0), // max extent

            (BlockID.moon_leaf, 1, 1, 0), (BlockID.moon_leaf, 1, 1, 1), (BlockID.moon_leaf, 1, 1, -1), (BlockID.moon_leaf, 1, 2, 1),
            (BlockID.moon_leaf, 1, 2, -1), (BlockID.moon_leaf, 2, 1, 1), (BlockID.moon_leaf, 2, 1, 0), (BlockID.moon_leaf, 2, 2, 1),
            (BlockID.moon_leaf, 2, 2, 0), (BlockID.moon_leaf, 1, 2, 2), (BlockID.moon_leaf, 2, 2, 2), (BlockID.moon_leaf, 1, 3, 2),
            (BlockID.moon_leaf, 2, 3, 2), (BlockID.moon_leaf, 2, 4, 2), (BlockID.moon_leaf, 1, 4, 3), (BlockID.moon_leaf, 2, 4, 3),
            (BlockID.moon_leaf, 2, 5, 3), (BlockID.moon_leaf, 1, 5, 3), (BlockID.moon_leaf, 2, 5, 4), (BlockID.moon_leaf, 1, 5, 4),
            (BlockID.moon_leaf, 1, 6, 4), (BlockID.moon_leaf, 1, 6, 5), (BlockID.moon_leaf, 1, 6, 6), (BlockID.moon_leaf, 0, 6, 6),
            (BlockID.moon_leaf, 0, 5, 6), (BlockID.moon_leaf, 0, 5, 7), (BlockID.moon_leaf, 1, 2, -2), (BlockID.moon_leaf, 1, 3, -2),
            (BlockID.moon_leaf, 1, 4, -2), (BlockID.moon_leaf, 2, 2, -1), (BlockID.moon_leaf, 2, 3, -1), (BlockID.moon_leaf, 2, 3, -2),
            (BlockID.moon_leaf, 2, 4, -2), (BlockID.moon_leaf, 2, 5, -2), (BlockID.moon_leaf, 2, 4, -3), (BlockID.moon_leaf, 2, 5, -3),
            (BlockID.moon_leaf, 2, 6, -3), (BlockID.moon_leaf, 3, 5, -3), (BlockID.moon_leaf, 3, 6, -3), (BlockID.moon_leaf, 3, 6, -4),
            (BlockID.moon_leaf, 2, 6, -4), (BlockID.moon_leaf, 3, 7, -4), (BlockID.moon_leaf, 2, 7, -4), (BlockID.moon_leaf, 3, 7, -5),
            (BlockID.moon_leaf, 2, 7, -5), (BlockID.moon_leaf, 3, 6, -6), (BlockID.moon_leaf, 3, 7, -6), (BlockID.moon_leaf, 4, 6, -6),
            (BlockID.moon_leaf, 4, 6, -7), (BlockID.moon_leaf, 3, 6, -7), (BlockID.moon_leaf, 4, 5, -7), (BlockID.moon_leaf, 4, 5, -8),

            (BlockID.light, 5, 3, -8), (BlockID.light, 5, 3, -9), (BlockID.light, 4, 3, -8), (BlockID.light, 4, 3, -9),
            (BlockID.light, 5, 4, -8), (BlockID.light, 5, 4, -9), (BlockID.light, 4, 4, -8), (BlockID.light, 4, 4, -9),
            (BlockID.light, -1, 3, 8), (BlockID.light, -1, 3, 7), (BlockID.light, 0, 3, 8), (BlockID.light, 0, 3, 7),
            (BlockID.light, -1, 4, 8), (BlockID.light, -1, 4, 7), (BlockID.light, 0, 4, 8), (BlockID.light, 0, 4, 7)
        },
        new (BlockID, int, int, int)[]
        {
            (BlockID.moon_leaf, 11, 0, 0), // max extent

            // TODO: Lower max extent to 7
            // x: -6 to 8  ->  -7 to 7
            // z: -11 to 3  ->  -7 to 7

            (BlockID.moon_leaf, 1, 1, 0), (BlockID.moon_leaf, 1, 2, 0), (BlockID.moon_leaf, 1, 3, 0), (BlockID.moon_leaf, 1, 1, -1),
            (BlockID.moon_leaf, 1, 2, -1), (BlockID.moon_leaf, 1, 3, -1), (BlockID.moon_leaf, 0, 1, -1), (BlockID.moon_leaf, 0, 2, -1),
            (BlockID.moon_leaf, 0, 3, -1), (BlockID.moon_leaf, -1, 4, -1), (BlockID.moon_leaf, -1, 3, -1), (BlockID.moon_leaf, -1, 2, -1),
            (BlockID.moon_leaf, 2, 3, 0), (BlockID.moon_leaf, 2, 4, 0), (BlockID.moon_leaf, 2, 3, -1), (BlockID.moon_leaf, 2, 4, -1),
            (BlockID.moon_leaf, 2, 5, -1), (BlockID.moon_leaf, 3, 5, -1), (BlockID.moon_leaf, 3, 5, 0), (BlockID.moon_leaf, 3, 6, 0),
            (BlockID.moon_leaf, 4, 6, 0), (BlockID.moon_leaf, 4, 7, 0), (BlockID.moon_leaf, 5, 7, 0), (BlockID.moon_leaf, 6, 7, 0),
            (BlockID.moon_leaf, 6, 6, 0), (BlockID.moon_leaf, 7, 6, 0), (BlockID.moon_leaf, -1, 3, 0), (BlockID.moon_leaf, -1, 3, -2),
            (BlockID.moon_leaf, -1, 4, -2), (BlockID.moon_leaf, -2, 4, -1), (BlockID.moon_leaf, -2, 4, 0), (BlockID.moon_leaf, -2, 5, 0),
            (BlockID.moon_leaf, -3, 5, 0), (BlockID.moon_leaf, -3, 6, 0), (BlockID.moon_leaf, -3, 5, -1), (BlockID.moon_leaf, -4, 5, -1),
            (BlockID.moon_leaf, -4, 6, -1), (BlockID.moon_leaf, -4, 6, 0), (BlockID.moon_leaf, -4, 7, 0), (BlockID.moon_leaf, -5, 6, 0),
            (BlockID.moon_leaf, -5, 7, 0), (BlockID.moon_leaf, -5, 7, 1), (BlockID.moon_leaf, -6, 7, 0), (BlockID.moon_leaf, -6, 7, 1),
            (BlockID.moon_leaf, -6, 7, 2), (BlockID.moon_leaf, 1, 1, -2), (BlockID.moon_leaf, 1, 1, -3), (BlockID.moon_leaf, 0, 1, -2),
            (BlockID.moon_leaf, 0, 1, -3), (BlockID.moon_leaf, 1, 2, -2), (BlockID.moon_leaf, 1, 2, -3), (BlockID.moon_leaf, 0, 2, -2),
            (BlockID.moon_leaf, 0, 2, -3), (BlockID.moon_leaf, 0, 2, -4), (BlockID.moon_leaf, 1, 3, -3), (BlockID.moon_leaf, 0, 3, -4),
            (BlockID.moon_leaf, 1, 3, -4), (BlockID.moon_leaf, 0, 4, -4), (BlockID.moon_leaf, 1, 4, -4), (BlockID.moon_leaf, 1, 4, -5),
            (BlockID.moon_leaf, 1, 5, -5), (BlockID.moon_leaf, 1, 6, -5), (BlockID.moon_leaf, 0, 4, -5), (BlockID.moon_leaf, 0, 5, -5),
            (BlockID.moon_leaf, 0, 6, -6), (BlockID.moon_leaf, 1, 6, -6), (BlockID.moon_leaf, 1, 7, -6), (BlockID.moon_leaf, 1, 7, -7),
            (BlockID.moon_leaf, 0, 7, -7), (BlockID.moon_leaf, 1, 8, -7), (BlockID.moon_leaf, 1, 8, -9), (BlockID.moon_leaf, 1, 7, -9),
            (BlockID.moon_leaf, 1, 7, -10), (BlockID.moon_leaf, 0, 7, -9), (BlockID.moon_leaf, 0, 7, -10), (BlockID.moon_leaf, -1, 7, -10),
            (BlockID.moon_leaf, 0, 7, -8), (BlockID.moon_leaf, 1, 7, -8), (BlockID.moon_leaf, 1, 8, -8),

            (BlockID.light, 0, 5, -11), (BlockID.light, 0, 6, -11), (BlockID.light, 0, 5, -10), (BlockID.light, 0, 6, -10),
            (BlockID.light, -1, 5, -11), (BlockID.light, -1, 6, -11), (BlockID.light, -1, 6, -10), (BlockID.light, -5, 5, 3),
            (BlockID.light, -5, 5, 2), (BlockID.light, -6, 5, 3), (BlockID.light, -6, 5, 2), (BlockID.light, -5, 6, 3),
            (BlockID.light, -5, 6, 2), (BlockID.light, -6, 6, 3), (BlockID.light, -6, 6, 2), (BlockID.light, 8, 4, 0),
            (BlockID.light, 8, 4, 1), (BlockID.light, 7, 4, 1), (BlockID.light, 7, 5, 1), (BlockID.light, 7, 5, 0),
            (BlockID.light, 8, 5, 0)
        },
        new (BlockID, int, int, int)[]
        {
            (BlockID.moon_leaf, 9, 0, 0), // max extent

            // x: -5 to 11  ->  -8 to 8 (DONE)
            // z: -10 to 7  ->  -9 to 8 (DONE)

            (BlockID.moon_leaf, -2, 1, 1), (BlockID.moon_leaf, -2, 2, 1), (BlockID.moon_leaf, -2, 3, 1), (BlockID.moon_leaf, -2, 4, 1),
            (BlockID.moon_leaf, -3, 1, 0), (BlockID.moon_leaf, -2, 1, -1), (BlockID.moon_leaf, -1, 1, -1), (BlockID.moon_leaf, -1, 1, 2),
            (BlockID.moon_leaf, 0, 1, 1), (BlockID.moon_leaf, 0, 1, 0), (BlockID.moon_leaf, -1, 1, 1), (BlockID.moon_leaf, -1, 2, 1),
            (BlockID.moon_leaf, -2, 1, 0), (BlockID.moon_leaf, -2, 2, 0), (BlockID.moon_leaf, -2, 3, 0), (BlockID.moon_leaf, -2, 4, 0),
            (BlockID.moon_leaf, -2, 5, 0), (BlockID.moon_leaf, -1, 4, -2), (BlockID.moon_leaf, -2, 4, -2), (BlockID.moon_leaf, -2, 4, -1),
            (BlockID.moon_leaf, -1, 4, -1), (BlockID.moon_leaf, -1, 1, 0), (BlockID.moon_leaf, -1, 2, 0), (BlockID.moon_leaf, -1, 3, 0),
            (BlockID.moon_leaf, -1, 4, 1), (BlockID.moon_leaf, 0, 3, 0), (BlockID.moon_leaf, 0, 4, 0), (BlockID.moon_leaf, 0, 5, 0),
            (BlockID.moon_leaf, 0, 5, -1), (BlockID.moon_leaf, 0, 6, -1), (BlockID.moon_leaf, -2, 5, -2), (BlockID.moon_leaf, 1, 5, 0),
            (BlockID.moon_leaf, 1, 6, 0), (BlockID.moon_leaf, 1, 7, 0), (BlockID.moon_leaf, 1, 6, -1), (BlockID.moon_leaf, 1, 7, -1),
            (BlockID.moon_leaf, 2, 7, 0), (BlockID.moon_leaf, 2, 8, 0), (BlockID.moon_leaf, 2, 8, -1), (BlockID.moon_leaf, 2, 9, -1),
            (BlockID.moon_leaf, 3, 8, -1), (BlockID.moon_leaf, 3, 9, -1), (BlockID.moon_leaf, 4, 9, -1), (BlockID.moon_leaf, 5, 9, -1),
            (BlockID.moon_leaf, 6, 9, -1), (BlockID.moon_leaf, 6, 8, -1), (BlockID.moon_leaf, 7, 8, -1), (BlockID.moon_leaf, 7, 7, -1),
            (BlockID.moon_leaf, 8, 7, -1), (BlockID.moon_leaf, 7, 7, 0), (BlockID.moon_leaf, -1, 4, 2), (BlockID.moon_leaf, -1, 5, 2),
            (BlockID.moon_leaf, -1, 6, 2), (BlockID.moon_leaf, -1, 7, 2), (BlockID.moon_leaf, -2, 5, 2), (BlockID.moon_leaf, -2, 6, 2),
            (BlockID.moon_leaf, -2, 7, 2), (BlockID.moon_leaf, -2, 7, 3), (BlockID.moon_leaf, -2, 8, 3), (BlockID.moon_leaf, -1, 8, 3),
            (BlockID.moon_leaf, -1, 8, 4), (BlockID.moon_leaf, -1, 8, 5), (BlockID.moon_leaf, -1, 8, 6), (BlockID.moon_leaf, -1, 7, 6),
            (BlockID.moon_leaf, 0, 7, 6), (BlockID.moon_leaf, -1, 7, 7), (BlockID.moon_leaf, -1, 6, 7), (BlockID.moon_leaf, 0, 6, 6),
            (BlockID.moon_leaf, -3, 5, -1), (BlockID.moon_leaf, -3, 6, -1), (BlockID.moon_leaf, -3, 6, 0), (BlockID.moon_leaf, -4, 7, -1),
            (BlockID.moon_leaf, -4, 8, -1), (BlockID.moon_leaf, -4, 7, 0), (BlockID.moon_leaf, -5, 8, 0), (BlockID.moon_leaf, -5, 9, 0),
            (BlockID.moon_leaf, -5, 9, -1), (BlockID.moon_leaf, -6, 9, 0), (BlockID.moon_leaf, -7, 9, 0), (BlockID.moon_leaf, -1, 5, -3),
            (BlockID.moon_leaf, -1, 5, -3), (BlockID.moon_leaf, -2, 5, -3), (BlockID.moon_leaf, -1, 6, -3), (BlockID.moon_leaf, -2, 6, -3),
            (BlockID.moon_leaf, -2, 6, -4), (BlockID.moon_leaf, -2, 7, -4), (BlockID.moon_leaf, -1, 7, -4), (BlockID.moon_leaf, -1, 8, -5),
            (BlockID.moon_leaf, -1, 8, -6), (BlockID.moon_leaf, -2, 8, -5), (BlockID.moon_leaf, -2, 8, -6), (BlockID.moon_leaf, -2, 8, -7),
            (BlockID.moon_leaf, -1, 7, -7), (BlockID.moon_leaf, -1, 6, -8), (BlockID.moon_leaf, -1, 7, -8), (BlockID.moon_leaf, -2, 7, -8),
            (BlockID.moon_leaf, -1, 6, -9), (BlockID.moon_leaf, -2, 6, -9),

            (BlockID.light, -7, 7, 0), (BlockID.light, -8, 7, 0), (BlockID.light, -7, 7, 1), (BlockID.light, -8, 7, 1),
            (BlockID.light, -7, 8, 0), (BlockID.light, -8, 8, 0), (BlockID.light, -7, 8, 1), (BlockID.light, -8, 8, 1),
            (BlockID.light, -1, 4, -9), (BlockID.light, -2, 4, -9), (BlockID.light, -1, 4, -8), (BlockID.light, -2, 4, -8),
            (BlockID.light, -1, 5, -9), (BlockID.light, -2, 5, -9), (BlockID.light, -1, 5, -8), (BlockID.light, -2, 5, -8),
            (BlockID.light, 7, 5, 0), (BlockID.light, 7, 5, -1), (BlockID.light, 8, 5, -1), (BlockID.light, 7, 6, 0),
            (BlockID.light, 8, 6, 0), (BlockID.light, 7, 6, -1), (BlockID.light, 8, 6, -1), (BlockID.light, 0, 4, 8),
            (BlockID.light, -1, 4, 8), (BlockID.light, 0, 4, 7), (BlockID.light, -1, 4, 7), (BlockID.light, -1, 5, 8),
            (BlockID.light, 0, 5, 7), (BlockID.light, -1, 5, 7)
        },
        new (BlockID, int, int, int)[]
        {
            (BlockID.moon_leaf, 8, 0, 0), // max extent

            // x: -7 to 9  ->  -8 to 8 (DONE)
            // z: -7 to 8

            (BlockID.moon_leaf, 0, 2, 0), (BlockID.moon_leaf, 0, 3, 0), (BlockID.moon_leaf, 0, 4, 0), (BlockID.moon_leaf, -1, 2, 0),
            (BlockID.moon_leaf, -1, 3, 0), (BlockID.moon_leaf, -1, 4, 0), (BlockID.moon_leaf, -1, 1, -1), (BlockID.moon_leaf, -1, 2, -1),
            (BlockID.moon_leaf, -1, 3, -1), (BlockID.moon_leaf, -1, 4, -1), (BlockID.moon_leaf, 1, 3, 0), (BlockID.moon_leaf, 1, 4, 0),
            (BlockID.moon_leaf, 1, 3, -1), (BlockID.moon_leaf, 1, 4, -1), (BlockID.moon_leaf, 0, 1, -1), (BlockID.moon_leaf, 0, 2, -1),
            (BlockID.moon_leaf, 0, 3, -1), (BlockID.moon_leaf, 0, 2, -2), (BlockID.moon_leaf, 0, 3, -2), (BlockID.moon_leaf, 0, 4, -2),
            (BlockID.moon_leaf, -1, 2, -2), (BlockID.moon_leaf, -1, 3, -2), (BlockID.moon_leaf, -1, 4, -2), (BlockID.moon_leaf, -1, 5, -2),
            (BlockID.moon_leaf, 0, 4, -3), (BlockID.moon_leaf, 0, 5, -3), (BlockID.moon_leaf, -1, 3, -3), (BlockID.moon_leaf, -1, 4, -3),
            (BlockID.moon_leaf, -1, 5, -3), (BlockID.moon_leaf, -1, 6, -3), (BlockID.moon_leaf, 0, 5, -4), (BlockID.moon_leaf, -1, 5, -4),
            (BlockID.moon_leaf, 0, 6, -4), (BlockID.moon_leaf, -1, 6, -4), (BlockID.moon_leaf, 0, 5, -5), (BlockID.moon_leaf, -1, 5, -5),
            (BlockID.moon_leaf, -1, 4, -5), (BlockID.moon_leaf, -1, 3, -6), (BlockID.moon_leaf, -1, 4, -6), (BlockID.moon_leaf, -2, 4, 0),
            (BlockID.moon_leaf, -2, 4, 1), (BlockID.moon_leaf, -2, 5, 0), (BlockID.moon_leaf, -2, 5, 1), (BlockID.moon_leaf, -3, 5, 1),
            (BlockID.moon_leaf, -3, 6, 1), (BlockID.moon_leaf, -4, 6, 1), (BlockID.moon_leaf, -5, 6, 1), (BlockID.moon_leaf, -4, 6, 2),
            (BlockID.moon_leaf, -5, 6, 2), (BlockID.moon_leaf, -5, 5, 2), (BlockID.moon_leaf, -6, 5, 2), (BlockID.moon_leaf, -6, 5, 1),
            (BlockID.moon_leaf, -6, 4, 2), (BlockID.moon_leaf, -7, 4, 2), (BlockID.moon_leaf, -7, 3, 2), (BlockID.moon_leaf, -7, 2, 2),
            (BlockID.moon_leaf, -7, 1, 2), (BlockID.moon_leaf, -8, 1, 2), (BlockID.moon_leaf, -8, 2, 2), (BlockID.moon_leaf, -8, 1, 3),
            (BlockID.moon_leaf, 0, 3, 1), (BlockID.moon_leaf, 0, 4, 1), (BlockID.moon_leaf, 0, 5, 1), (BlockID.moon_leaf, -1, 4, 1),
            (BlockID.moon_leaf, -1, 5, 1), (BlockID.moon_leaf, -1, 6, 2), (BlockID.moon_leaf, -1, 6, 3), (BlockID.moon_leaf, -1, 6, 4),
            (BlockID.moon_leaf, 0, 6, 2), (BlockID.moon_leaf, 0, 6, 3), (BlockID.moon_leaf, 0, 7, 3), (BlockID.moon_leaf, -1, 7, 3),
            (BlockID.moon_leaf, -1, 7, 4), (BlockID.moon_leaf, -1, 7, 5), (BlockID.moon_leaf, -1, 8, 5), (BlockID.moon_leaf, -1, 8, 6),
            (BlockID.moon_leaf, -1, 8, 7), (BlockID.moon_leaf, -1, 7, 7), (BlockID.moon_leaf, 0, 7, 7), (BlockID.moon_leaf, 2, 4, 0),
            (BlockID.moon_leaf, 2, 4, -1), (BlockID.moon_leaf, 2, 5, 0), (BlockID.moon_leaf, 2, 5, -1), (BlockID.moon_leaf, 2, 6, 0),
            (BlockID.moon_leaf, 2, 6, -1), (BlockID.moon_leaf, 3, 6, -1), (BlockID.moon_leaf, 3, 7, -1), (BlockID.moon_leaf, 3, 7, 0),
            (BlockID.moon_leaf, 4, 7, 0), (BlockID.moon_leaf, 4, 7, -1), (BlockID.moon_leaf, 5, 7, -1), (BlockID.moon_leaf, 4, 8, -1),
            (BlockID.moon_leaf, 5, 8, -1), (BlockID.moon_leaf, 6, 8, -1), (BlockID.moon_leaf, 6, 9, -1), (BlockID.moon_leaf, 7, 8, -1),
            (BlockID.moon_leaf, 7, 8, -2),

            (BlockID.light, 0, 5, 8), (BlockID.light, -1, 5, 8), (BlockID.light, 0, 5, 7), (BlockID.light, -1, 5, 7),
            (BlockID.light, -1, 6, 8), (BlockID.light, 0, 6, 7), (BlockID.light, -1, 6, 7), (BlockID.light, 8, 6, -3),
            (BlockID.light, 7, 6, -3), (BlockID.light, 8, 6, -2), (BlockID.light, 7, 6, -2), (BlockID.light, 8, 7, -3),
            (BlockID.light, 7, 7, -3), (BlockID.light, 8, 7, -2), (BlockID.light, 7, 7, -2), (BlockID.light, -2, 1, -7),
            (BlockID.light, -1, 1, -7), (BlockID.light, -2, 1, -6), (BlockID.light, -1, 1, -6), (BlockID.light, -1, 2, -7),
            (BlockID.light, -2, 2, -6), (BlockID.light, -1, 2, -6)
        }
    };

    private static readonly (BlockID, int, int, int)[][] COLOR_WOOD_TREE_SHAPES = {
        new (BlockID, int, int, int)[]
        {
            (BlockID.moon_bark, 5, 0, 0), // max extent

            (BlockID.moon_bark, -1, 1, 0), (BlockID.moon_bark, -2, 1, 0), (BlockID.moon_bark, -2, 1, -1), (BlockID.moon_bark, -1, 1, -1),
            (BlockID.moon_bark, -2, 2, 0), (BlockID.moon_bark, -1, 2, -1), (BlockID.moon_bark, -2, 2, 1), (BlockID.moon_bark, -2, 3, 1),
            (BlockID.moon_bark, -2, 4, 1), (BlockID.moon_bark, -3, 4, 1), (BlockID.moon_bark, -3, 5, 1), (BlockID.moon_bark, -3, 6, 1),
            (BlockID.moon_bark, -3, 7, 1), (BlockID.moon_bark, -3, 8, 1), (BlockID.moon_bark, -3, 9, 1), (BlockID.moon_bark, -4, 8, 1),
            (BlockID.moon_bark, -4, 9, 1), (BlockID.moon_bark, -4, 10, 1), (BlockID.moon_bark, -4, 10, 2), (BlockID.moon_bark, -4, 11, 2),
            (BlockID.moon_bark, -4, 12, 2), (BlockID.moon_bark, 0, 2, -1), (BlockID.moon_bark, 0, 3, -1), (BlockID.moon_bark, 0, 3, -2),
            (BlockID.moon_bark, 0, 4, -2), (BlockID.moon_bark, 0, 5, -2), (BlockID.moon_bark, 0, 6, -2), (BlockID.moon_bark, 0, 7, -2),
            (BlockID.moon_bark, 0, 8, -2), (BlockID.moon_bark, 1, 7, -2), (BlockID.moon_bark, 1, 8, -2), (BlockID.moon_bark, 1, 9, -2),
            (BlockID.moon_bark, 1, 10, -2), (BlockID.moon_bark, 1, 11, -2), (BlockID.moon_bark, 1, 12, -2), (BlockID.moon_bark, 2, 10, -2),
            (BlockID.moon_bark, 2, 11, -2), (BlockID.moon_bark, 2, 12, -2), (BlockID.moon_bark, 1, 12, -3), (BlockID.moon_bark, 1, 13, -3),
            (BlockID.moon_bark, 1, 14, -3), (BlockID.moon_bark, 1, 15, -3), (BlockID.moon_bark, 1, 16, -3), (BlockID.moon_bark, 0, 16, -3),
            (BlockID.moon_bark, 0, 17, -3), (BlockID.moon_bark, 0, 18, -3), (BlockID.moon_bark, 0, 19, -3),

            (BlockID.light, -1, 21, -4), (BlockID.light, 1, 21, -4), (BlockID.light, -1, 21, -2), (BlockID.light, 0, 21, -2),
            (BlockID.light, -1, 21, -3), (BlockID.light, 0, 21, -3), (BlockID.light, 1, 21, -3), (BlockID.light, 0, 21, -4),
            (BlockID.light, -5, 13, 3), (BlockID.light, -4, 13, 3), (BlockID.light, -3, 13, 3), (BlockID.light, -5, 13, 2),
            (BlockID.light, -4, 13, 2), (BlockID.light, -3, 13, 2), (BlockID.light, -4, 13, 1),

            (BlockID.chalchanthite, 0, 20, -3), (BlockID.chalchanthite, 0, 20, -4), (BlockID.chalchanthite, 1, 20, -4), (BlockID.chalchanthite, 1, 20, -3),
            (BlockID.chalchanthite, 1, 20, -2), (BlockID.chalchanthite, 0, 20, -2), (BlockID.chalchanthite, -1, 20, -2), (BlockID.chalchanthite, -1, 20, -3),
            (BlockID.chalchanthite, 0, 22, -4), (BlockID.chalchanthite, 1, 22, -4), (BlockID.chalchanthite, -1, 22, -3), (BlockID.chalchanthite, 0, 22, -3),
            (BlockID.chalchanthite, 1, 22, -3), (BlockID.chalchanthite, 0, 23, -3), (BlockID.chalchanthite, 1, 23, -3), (BlockID.chalchanthite, 0, 22, -2),
            (BlockID.chalchanthite, 1, 22, -2),

            (BlockID.feldspar, -5, 14, 1), (BlockID.feldspar, -5, 14, 2), (BlockID.feldspar, -5, 14, 3), (BlockID.feldspar, -4, 14, 1),
            (BlockID.feldspar, -4, 14, 2), (BlockID.feldspar, -4, 14, 3), (BlockID.feldspar, -3, 14, 2), (BlockID.feldspar, -3, 14, 3),
            (BlockID.feldspar, -4, 15, 2)
        },
        new (BlockID, int, int, int)[]
        {
            (BlockID.moon_bark, 5, 0, 0), // max extent

            (BlockID.moon_bark, -1, 1, 0), (BlockID.moon_bark, -2, 1, 0), (BlockID.moon_bark, -2, 1, -1), (BlockID.moon_bark, -1, 1, -1),
            (BlockID.moon_bark, -2, 2, 0), (BlockID.moon_bark, -1, 2, -1), (BlockID.moon_bark, -2, 2, 1), (BlockID.moon_bark, -2, 3, 1),
            (BlockID.moon_bark, -2, 4, 1), (BlockID.moon_bark, -3, 4, 1), (BlockID.moon_bark, -3, 5, 1), (BlockID.moon_bark, -3, 6, 1),
            (BlockID.moon_bark, -3, 7, 1), (BlockID.moon_bark, -3, 8, 1), (BlockID.moon_bark, -3, 9, 1), (BlockID.moon_bark, -4, 8, 1),
            (BlockID.moon_bark, -4, 9, 1), (BlockID.moon_bark, -4, 10, 1), (BlockID.moon_bark, -4, 10, 2), (BlockID.moon_bark, -4, 11, 2),
            (BlockID.moon_bark, -4, 12, 2), (BlockID.moon_bark, 0, 2, -1), (BlockID.moon_bark, 0, 3, -1), (BlockID.moon_bark, 0, 3, -2),
            (BlockID.moon_bark, 0, 4, -2), (BlockID.moon_bark, 0, 5, -2), (BlockID.moon_bark, 0, 6, -2), (BlockID.moon_bark, 0, 7, -2),
            (BlockID.moon_bark, 0, 8, -2), (BlockID.moon_bark, 1, 7, -2), (BlockID.moon_bark, 1, 8, -2), (BlockID.moon_bark, 1, 9, -2),
            (BlockID.moon_bark, 1, 10, -2), (BlockID.moon_bark, 1, 11, -2), (BlockID.moon_bark, 1, 12, -2), (BlockID.moon_bark, 2, 10, -2),
            (BlockID.moon_bark, 2, 11, -2), (BlockID.moon_bark, 2, 12, -2), (BlockID.moon_bark, 1, 12, -3), (BlockID.moon_bark, 1, 13, -3),
            (BlockID.moon_bark, 1, 14, -3), (BlockID.moon_bark, 1, 15, -3), (BlockID.moon_bark, 1, 16, -3), (BlockID.moon_bark, 0, 16, -3),
            (BlockID.moon_bark, 0, 17, -3), (BlockID.moon_bark, 0, 18, -3), (BlockID.moon_bark, 0, 19, -3),

            (BlockID.light, -1, 21, -2), (BlockID.light, 0, 21, -2), (BlockID.light, -1, 21, -3), (BlockID.light, 0, 21, -3),
            (BlockID.light, 1, 21, -3), (BlockID.light, 0, 21, -4), (BlockID.light, -5, 13, 3), (BlockID.light, -4, 13, 3),
            (BlockID.light, -3, 13, 3), (BlockID.light, -5, 13, 2), (BlockID.light, -4, 13, 2), (BlockID.light, -3, 13, 2),
            (BlockID.light, -4, 13, 1),

            (BlockID.feldspar, 0, 20, -3), (BlockID.feldspar, -1, 22, -4), (BlockID.feldspar, 0, 22, -4), (BlockID.feldspar, 1, 22, -4),
            (BlockID.feldspar, -1, 22, -3), (BlockID.feldspar, 0, 22, -3), (BlockID.feldspar, 1, 22, -3), (BlockID.feldspar, -1, 23, -3),
            (BlockID.feldspar, 0, 23, -3), (BlockID.feldspar, -1, 22, -2), (BlockID.feldspar, 0, 22, -2), (BlockID.feldspar, 1, 22, -2),

            (BlockID.chalchanthite, -5, 14, 1), (BlockID.chalchanthite, -5, 14, 2), (BlockID.chalchanthite, -5, 14, 3), (BlockID.chalchanthite, -4, 14, 1),
            (BlockID.chalchanthite, -4, 14, 2), (BlockID.chalchanthite, -4, 14, 3), (BlockID.chalchanthite, -3, 14, 2), (BlockID.chalchanthite, -3, 14, 3),
            (BlockID.chalchanthite, -4, 15, 2)
        },
        new (BlockID, int, int, int)[]
        {
            (BlockID.moon_bark, 5, 0, 0), // max extent

            (BlockID.moon_bark, -1, 1, 0), (BlockID.moon_bark, -2, 1, 0), (BlockID.moon_bark, -2, 1, -1), (BlockID.moon_bark, -1, 1, -1),
            (BlockID.moon_bark, -2, 2, 0), (BlockID.moon_bark, -1, 2, -1), (BlockID.moon_bark, -2, 2, 1), (BlockID.moon_bark, -2, 3, 1),
            (BlockID.moon_bark, -2, 4, 1), (BlockID.moon_bark, -3, 4, 1), (BlockID.moon_bark, -3, 5, 1), (BlockID.moon_bark, -3, 6, 1),
            (BlockID.moon_bark, -3, 7, 1), (BlockID.moon_bark, -3, 8, 1), (BlockID.moon_bark, -3, 9, 1), (BlockID.moon_bark, -4, 8, 1),
            (BlockID.moon_bark, -4, 9, 1), (BlockID.moon_bark, -4, 10, 1), (BlockID.moon_bark, -4, 10, 2), (BlockID.moon_bark, -4, 11, 2),
            (BlockID.moon_bark, -4, 12, 2), (BlockID.moon_bark, 0, 2, -1), (BlockID.moon_bark, 0, 3, -1), (BlockID.moon_bark, 0, 3, -2),
            (BlockID.moon_bark, 0, 4, -2), (BlockID.moon_bark, 0, 5, -2), (BlockID.moon_bark, 0, 6, -2), (BlockID.moon_bark, 0, 7, -2),
            (BlockID.moon_bark, 0, 8, -2), (BlockID.moon_bark, 1, 7, -2), (BlockID.moon_bark, 1, 8, -2), (BlockID.moon_bark, 1, 9, -2),
            (BlockID.moon_bark, 1, 10, -2), (BlockID.moon_bark, 1, 11, -2), (BlockID.moon_bark, 1, 12, -2), (BlockID.moon_bark, 2, 10, -2),
            (BlockID.moon_bark, 2, 11, -2), (BlockID.moon_bark, 2, 12, -2), (BlockID.moon_bark, 1, 12, -3), (BlockID.moon_bark, 1, 13, -3),
            (BlockID.moon_bark, 1, 14, -3), (BlockID.moon_bark, 1, 15, -3), (BlockID.moon_bark, 1, 16, -3), (BlockID.moon_bark, 0, 16, -3),
            (BlockID.moon_bark, 0, 17, -3), (BlockID.moon_bark, 0, 18, -3), (BlockID.moon_bark, 0, 19, -3),

            (BlockID.light, -1, 21, -2), (BlockID.light, 0, 21, -2), (BlockID.light, -1, 21, -3), (BlockID.light, 0, 21, -3),
            (BlockID.light, 1, 21, -3), (BlockID.light, 0, 21, -4), (BlockID.light, -5, 13, 3), (BlockID.light, -4, 13, 3),
            (BlockID.light, -3, 13, 3), (BlockID.light, -5, 13, 2), (BlockID.light, -4, 13, 2), (BlockID.light, -3, 13, 1),
            (BlockID.light, -3, 13, 2), (BlockID.light, -4, 13, 1),

            (BlockID.chalchanthite, 0, 20, -3), (BlockID.chalchanthite, -1, 22, -4), (BlockID.chalchanthite, 0, 22, -4), (BlockID.chalchanthite, 1, 22, -4),
            (BlockID.chalchanthite, -1, 22, -3), (BlockID.chalchanthite, 0, 22, -3), (BlockID.chalchanthite, 1, 22, -3), (BlockID.chalchanthite, -1, 23, -3),
            (BlockID.chalchanthite, 0, 23, -3), (BlockID.chalchanthite, 1, 23, -3), (BlockID.chalchanthite, -1, 22, -2), (BlockID.chalchanthite, 0, 22, -2),
            (BlockID.chalchanthite, 1, 22, -2),

            (BlockID.sulphur_ore, -5, 14, 2), (BlockID.sulphur_ore, -5, 14, 3), (BlockID.sulphur_ore, -4, 14, 1), (BlockID.sulphur_ore, -4, 14, 2),
            (BlockID.sulphur_ore, -4, 14, 3), (BlockID.sulphur_ore, -3, 14, 2), (BlockID.sulphur_ore, -3, 14, 3), (BlockID.sulphur_ore, -4, 15, 2)
        },
        new (BlockID, int, int, int)[]
        {
            (BlockID.moon_bark, 5, 0, 0), // max extent

            (BlockID.moon_bark, -1, 1, 0), (BlockID.moon_bark, -2, 1, 0), (BlockID.moon_bark, -2, 1, -1), (BlockID.moon_bark, -1, 1, -1),
            (BlockID.moon_bark, -2, 2, 0), (BlockID.moon_bark, -1, 2, -1), (BlockID.moon_bark, -2, 2, 1), (BlockID.moon_bark, -2, 3, 1),
            (BlockID.moon_bark, -2, 4, 1), (BlockID.moon_bark, -3, 4, 1), (BlockID.moon_bark, -3, 5, 1), (BlockID.moon_bark, -3, 6, 1),
            (BlockID.moon_bark, -3, 7, 1), (BlockID.moon_bark, -3, 8, 1), (BlockID.moon_bark, -3, 9, 1), (BlockID.moon_bark, -4, 8, 1),
            (BlockID.moon_bark, -4, 9, 1), (BlockID.moon_bark, -4, 10, 1), (BlockID.moon_bark, -4, 10, 2), (BlockID.moon_bark, -4, 11, 2),
            (BlockID.moon_bark, -4, 12, 2), (BlockID.moon_bark, 0, 2, -1), (BlockID.moon_bark, 0, 3, -1), (BlockID.moon_bark, 0, 3, -2),
            (BlockID.moon_bark, 0, 4, -2), (BlockID.moon_bark, 0, 5, -2), (BlockID.moon_bark, 0, 6, -2), (BlockID.moon_bark, 0, 7, -2),
            (BlockID.moon_bark, 0, 8, -2), (BlockID.moon_bark, 1, 7, -2), (BlockID.moon_bark, 1, 8, -2), (BlockID.moon_bark, 1, 9, -2),
            (BlockID.moon_bark, 1, 10, -2), (BlockID.moon_bark, 1, 11, -2), (BlockID.moon_bark, 1, 12, -2), (BlockID.moon_bark, 2, 10, -2),
            (BlockID.moon_bark, 2, 11, -2), (BlockID.moon_bark, 2, 12, -2), (BlockID.moon_bark, 1, 12, -3), (BlockID.moon_bark, 1, 13, -3),
            (BlockID.moon_bark, 1, 14, -3), (BlockID.moon_bark, 1, 15, -3), (BlockID.moon_bark, 1, 16, -3), (BlockID.moon_bark, 0, 16, -3),
            (BlockID.moon_bark, 0, 17, -3), (BlockID.moon_bark, 0, 18, -3), (BlockID.moon_bark, 0, 19, -3),

            (BlockID.light, -1, 21, -2), (BlockID.light, 0, 21, -2), (BlockID.light, -1, 21, -3), (BlockID.light, 0, 21, -3),
            (BlockID.light, 1, 21, -3), (BlockID.light, 0, 21, -4), (BlockID.light, -5, 13, 3), (BlockID.light, -4, 13, 3),
            (BlockID.light, -3, 13, 3), (BlockID.light, -5, 13, 2), (BlockID.light, -4, 13, 2), (BlockID.light, -3, 13, 2),
            (BlockID.light, -4, 13, 1),

            (BlockID.sulphur_ore, 0, 20, -3), (BlockID.sulphur_ore, -1, 22, -4), (BlockID.sulphur_ore, 0, 22, -4), (BlockID.sulphur_ore, 1, 22, -4),
            (BlockID.sulphur_ore, -1, 22, -3), (BlockID.sulphur_ore, 0, 22, -3), (BlockID.sulphur_ore, 1, 22, -3), (BlockID.sulphur_ore, 0, 23, -3),
            (BlockID.sulphur_ore, 1, 23, -3), (BlockID.sulphur_ore, -1, 22, -2), (BlockID.sulphur_ore, 0, 22, -2), (BlockID.sulphur_ore, 1, 22, -2),

            (BlockID.chalchanthite, -5, 14, 1), (BlockID.chalchanthite, -5, 14, 2), (BlockID.chalchanthite, -5, 14, 3), (BlockID.chalchanthite, -4, 14, 1),
            (BlockID.chalchanthite, -4, 14, 2), (BlockID.chalchanthite, -4, 14, 3), (BlockID.chalchanthite, -3, 14, 2), (BlockID.chalchanthite, -3, 14, 3),
            (BlockID.chalchanthite, -4, 15, 2)
        },
        new (BlockID, int, int, int)[]
        {
            (BlockID.moon_bark, 5, 0, 0), // max extent

            (BlockID.moon_bark, -1, 1, 0), (BlockID.moon_bark, -2, 1, 0), (BlockID.moon_bark, -2, 1, -1), (BlockID.moon_bark, -1, 1, -1),
            (BlockID.moon_bark, -2, 2, 0), (BlockID.moon_bark, -1, 2, -1), (BlockID.moon_bark, -2, 2, 1), (BlockID.moon_bark, -2, 3, 1),
            (BlockID.moon_bark, -2, 4, 1), (BlockID.moon_bark, -3, 4, 1), (BlockID.moon_bark, -3, 5, 1), (BlockID.moon_bark, -3, 6, 1),
            (BlockID.moon_bark, -3, 7, 1), (BlockID.moon_bark, -3, 8, 1), (BlockID.moon_bark, -3, 9, 1), (BlockID.moon_bark, -4, 8, 1),
            (BlockID.moon_bark, -4, 9, 1), (BlockID.moon_bark, -4, 10, 1), (BlockID.moon_bark, -4, 10, 2), (BlockID.moon_bark, -4, 11, 2),
            (BlockID.moon_bark, -4, 12, 2), (BlockID.moon_bark, 0, 2, -1), (BlockID.moon_bark, 0, 3, -1), (BlockID.moon_bark, 0, 3, -2),
            (BlockID.moon_bark, 0, 4, -2), (BlockID.moon_bark, 0, 5, -2), (BlockID.moon_bark, 0, 6, -2), (BlockID.moon_bark, 0, 7, -2),
            (BlockID.moon_bark, 0, 8, -2), (BlockID.moon_bark, 1, 7, -2), (BlockID.moon_bark, 1, 8, -2), (BlockID.moon_bark, 1, 9, -2),
            (BlockID.moon_bark, 1, 10, -2), (BlockID.moon_bark, 1, 11, -2), (BlockID.moon_bark, 1, 12, -2), (BlockID.moon_bark, 2, 10, -2),
            (BlockID.moon_bark, 2, 11, -2), (BlockID.moon_bark, 2, 12, -2), (BlockID.moon_bark, 1, 12, -3), (BlockID.moon_bark, 1, 13, -3),
            (BlockID.moon_bark, 1, 14, -3), (BlockID.moon_bark, 1, 15, -3), (BlockID.moon_bark, 1, 16, -3), (BlockID.moon_bark, 0, 16, -3),
            (BlockID.moon_bark, 0, 17, -3), (BlockID.moon_bark, 0, 18, -3), (BlockID.moon_bark, 0, 19, -3),

            (BlockID.light, -1, 21, -4), (BlockID.light, 1, 21, -4), (BlockID.light, -1, 21, -2), (BlockID.light, 0, 21, -2),
            (BlockID.light, -1, 21, -3), (BlockID.light, 0, 21, -3), (BlockID.light, 1, 21, -3), (BlockID.light, 0, 21, -4),
            (BlockID.light, -5, 13, 3), (BlockID.light, -4, 13, 3), (BlockID.light, -3, 13, 3), (BlockID.light, -5, 13, 2),
            (BlockID.light, -4, 13, 2), (BlockID.light, -3, 13, 2), (BlockID.light, -4, 13, 1), (BlockID.light, -3, 13, 1),

            (BlockID.feldspar, 0, 20, -4), (BlockID.feldspar, 1, 20, -4), (BlockID.feldspar, 1, 20, -3), (BlockID.feldspar, 1, 20, -2),
            (BlockID.feldspar, 0, 20, -2), (BlockID.feldspar, -1, 20, -2), (BlockID.feldspar, -1, 20, -3), (BlockID.feldspar, 0, 20, -3),
            (BlockID.feldspar, 0, 22, -4), (BlockID.feldspar, 1, 22, -4), (BlockID.feldspar, -1, 22, -3), (BlockID.feldspar, 0, 22, -3),
            (BlockID.feldspar, 1, 22, -3), (BlockID.feldspar, 0, 23, -3), (BlockID.feldspar, 1, 23, -3), (BlockID.feldspar, 0, 22, -2),
            (BlockID.feldspar, 1, 22, -2),

            (BlockID.sulphur_ore, -5, 14, 2), (BlockID.sulphur_ore, -5, 14, 3), (BlockID.sulphur_ore, -4, 14, 1), (BlockID.sulphur_ore, -4, 14, 2),
            (BlockID.sulphur_ore, -4, 14, 3), (BlockID.sulphur_ore, -3, 14, 2), (BlockID.sulphur_ore, -3, 14, 3), (BlockID.sulphur_ore, -4, 15, 2)
        },
        new (BlockID, int, int, int)[]
        {
            (BlockID.moon_bark, 5, 0, 0), // max extent

            (BlockID.moon_bark, -1, 1, 0), (BlockID.moon_bark, -2, 1, 0), (BlockID.moon_bark, -2, 1, -1), (BlockID.moon_bark, -1, 1, -1),
            (BlockID.moon_bark, -2, 2, 0), (BlockID.moon_bark, -1, 2, -1), (BlockID.moon_bark, -2, 2, 1), (BlockID.moon_bark, -2, 3, 1),
            (BlockID.moon_bark, -2, 4, 1), (BlockID.moon_bark, -3, 4, 1), (BlockID.moon_bark, -3, 5, 1), (BlockID.moon_bark, -3, 6, 1),
            (BlockID.moon_bark, -3, 7, 1), (BlockID.moon_bark, -3, 8, 1), (BlockID.moon_bark, -3, 9, 1), (BlockID.moon_bark, -4, 8, 1),
            (BlockID.moon_bark, -4, 9, 1), (BlockID.moon_bark, -4, 10, 1), (BlockID.moon_bark, -4, 10, 2), (BlockID.moon_bark, -4, 11, 2),
            (BlockID.moon_bark, -4, 12, 2), (BlockID.moon_bark, 0, 2, -1), (BlockID.moon_bark, 0, 3, -1), (BlockID.moon_bark, 0, 3, -2),
            (BlockID.moon_bark, 0, 4, -2), (BlockID.moon_bark, 0, 5, -2), (BlockID.moon_bark, 0, 6, -2), (BlockID.moon_bark, 0, 7, -2),
            (BlockID.moon_bark, 0, 8, -2), (BlockID.moon_bark, 1, 7, -2), (BlockID.moon_bark, 1, 8, -2), (BlockID.moon_bark, 1, 9, -2),
            (BlockID.moon_bark, 1, 10, -2), (BlockID.moon_bark, 1, 11, -2), (BlockID.moon_bark, 1, 12, -2), (BlockID.moon_bark, 2, 10, -2),
            (BlockID.moon_bark, 2, 11, -2), (BlockID.moon_bark, 2, 12, -2), (BlockID.moon_bark, 1, 12, -3), (BlockID.moon_bark, 1, 13, -3),
            (BlockID.moon_bark, 1, 14, -3), (BlockID.moon_bark, 1, 15, -3), (BlockID.moon_bark, 1, 16, -3), (BlockID.moon_bark, 0, 16, -3),
            (BlockID.moon_bark, 0, 17, -3), (BlockID.moon_bark, 0, 18, -3), (BlockID.moon_bark, 0, 19, -3),

            (BlockID.light, -1, 21, -2), (BlockID.light, 0, 21, -2), (BlockID.light, -1, 21, -3), (BlockID.light, 0, 21, -3),
            (BlockID.light, 1, 21, -3), (BlockID.light, 0, 21, -4), (BlockID.light, -5, 13, 3), (BlockID.light, -4, 13, 3),
            (BlockID.light, -3, 13, 3), (BlockID.light, -5, 13, 2), (BlockID.light, -4, 13, 2), (BlockID.light, -3, 13, 2),
            (BlockID.light, -4, 13, 1),

            (BlockID.sulphur_ore, 0, 20, -3), (BlockID.sulphur_ore, -1, 22, -4), (BlockID.sulphur_ore, 0, 22, -4), (BlockID.sulphur_ore, 1, 22, -4),
            (BlockID.sulphur_ore, -1, 22, -3), (BlockID.sulphur_ore, 0, 22, -3), (BlockID.sulphur_ore, 1, 22, -3), (BlockID.sulphur_ore, -1, 22, -2),
            (BlockID.sulphur_ore, 0, 22, -2), (BlockID.sulphur_ore, 1, 22, -2), (BlockID.sulphur_ore, -1, 23, -3), (BlockID.sulphur_ore, 0, 23, -3),

            (BlockID.feldspar, -5, 14, 1), (BlockID.feldspar, -5, 14, 2), (BlockID.feldspar, -5, 14, 3), (BlockID.feldspar, -4, 14, 1),
            (BlockID.feldspar, -4, 14, 2), (BlockID.feldspar, -4, 14, 3), (BlockID.feldspar, -3, 14, 2), (BlockID.feldspar, -3, 14, 3),
            (BlockID.feldspar, -4, 15, 2)
        },
        new (BlockID, int, int, int)[]
        {
            (BlockID.moon_bark, 5, 0, 0), // max extent

            (BlockID.moon_bark, -1, 1, 0), (BlockID.moon_bark, -2, 1, 0), (BlockID.moon_bark, -2, 1, -1), (BlockID.moon_bark, -1, 1, -1),
            (BlockID.moon_bark, -2, 2, 0), (BlockID.moon_bark, -1, 2, -1), (BlockID.moon_bark, -2, 2, 1), (BlockID.moon_bark, -2, 3, 1),
            (BlockID.moon_bark, -2, 4, 1), (BlockID.moon_bark, -3, 4, 1), (BlockID.moon_bark, -3, 5, 1), (BlockID.moon_bark, -3, 6, 1),
            (BlockID.moon_bark, -3, 7, 1), (BlockID.moon_bark, -3, 8, 1), (BlockID.moon_bark, -3, 9, 1), (BlockID.moon_bark, -4, 8, 1),
            (BlockID.moon_bark, -4, 9, 1), (BlockID.moon_bark, -4, 10, 1), (BlockID.moon_bark, -4, 10, 2), (BlockID.moon_bark, -4, 11, 2),
            (BlockID.moon_bark, -4, 12, 2), (BlockID.moon_bark, 0, 2, -1), (BlockID.moon_bark, 0, 3, -1), (BlockID.moon_bark, 0, 3, -2),
            (BlockID.moon_bark, 0, 4, -2), (BlockID.moon_bark, 0, 5, -2), (BlockID.moon_bark, 0, 6, -2), (BlockID.moon_bark, 0, 7, -2),
            (BlockID.moon_bark, 0, 8, -2), (BlockID.moon_bark, 1, 7, -2), (BlockID.moon_bark, 1, 8, -2), (BlockID.moon_bark, 1, 9, -2),
            (BlockID.moon_bark, 1, 10, -2), (BlockID.moon_bark, 1, 11, -2), (BlockID.moon_bark, 1, 12, -2), (BlockID.moon_bark, 2, 10, -2),
            (BlockID.moon_bark, 2, 11, -2), (BlockID.moon_bark, 2, 12, -2), (BlockID.moon_bark, 1, 12, -3), (BlockID.moon_bark, 1, 13, -3),
            (BlockID.moon_bark, 1, 14, -3), (BlockID.moon_bark, 1, 15, -3), (BlockID.moon_bark, 1, 16, -3), (BlockID.moon_bark, 0, 16, -3),
            (BlockID.moon_bark, 0, 17, -3), (BlockID.moon_bark, 0, 18, -3), (BlockID.moon_bark, 0, 19, -3),

            (BlockID.light, -1, 21, -2), (BlockID.light, 0, 21, -2), (BlockID.light, -1, 21, -3), (BlockID.light, 0, 21, -3),
            (BlockID.light, 1, 21, -3), (BlockID.light, 0, 21, -4), (BlockID.light, -5, 13, 3), (BlockID.light, -4, 13, 3),
            (BlockID.light, -3, 13, 3), (BlockID.light, -5, 13, 2), (BlockID.light, -4, 13, 2), (BlockID.light, -3, 13, 2),
            (BlockID.light, -4, 13, 1),

            (BlockID.feldspar, 0, 20, -3), (BlockID.feldspar, -1, 22, -4), (BlockID.feldspar, 0, 22, -4), (BlockID.feldspar, 1, 22, -4),
            (BlockID.feldspar, -1, 22, -3), (BlockID.feldspar, 0, 22, -3), (BlockID.feldspar, 1, 22, -3), (BlockID.feldspar, -1, 22, -2),
            (BlockID.feldspar, 0, 22, -2), (BlockID.feldspar, 1, 22, -2), (BlockID.feldspar, -1, 23, -3), (BlockID.feldspar, 0, 23, -3),

            (BlockID.feldspar, -5, 14, 1), (BlockID.feldspar, -5, 14, 2), (BlockID.feldspar, -5, 14, 3), (BlockID.feldspar, -4, 14, 1),
            (BlockID.feldspar, -4, 14, 2), (BlockID.feldspar, -4, 14, 3), (BlockID.feldspar, -3, 14, 2), (BlockID.feldspar, -3, 14, 3),
            (BlockID.feldspar, -4, 15, 2)
        },
        new (BlockID, int, int, int)[]
        {
            (BlockID.moon_bark, 5, 0, 0), // max extent

            (BlockID.moon_bark, -1, 1, 0), (BlockID.moon_bark, -2, 1, 0), (BlockID.moon_bark, -2, 1, -1), (BlockID.moon_bark, -1, 1, -1),
            (BlockID.moon_bark, -2, 2, 0), (BlockID.moon_bark, -1, 2, -1), (BlockID.moon_bark, -2, 2, 1), (BlockID.moon_bark, -2, 3, 1),
            (BlockID.moon_bark, -2, 4, 1), (BlockID.moon_bark, -3, 4, 1), (BlockID.moon_bark, -3, 5, 1), (BlockID.moon_bark, -3, 6, 1),
            (BlockID.moon_bark, -3, 7, 1), (BlockID.moon_bark, -3, 8, 1), (BlockID.moon_bark, -3, 9, 1), (BlockID.moon_bark, -4, 8, 1),
            (BlockID.moon_bark, -4, 9, 1), (BlockID.moon_bark, -4, 10, 1), (BlockID.moon_bark, -4, 10, 2), (BlockID.moon_bark, -4, 11, 2),
            (BlockID.moon_bark, -4, 12, 2), (BlockID.moon_bark, 0, 2, -1), (BlockID.moon_bark, 0, 3, -1), (BlockID.moon_bark, 0, 3, -2),
            (BlockID.moon_bark, 0, 4, -2), (BlockID.moon_bark, 0, 5, -2), (BlockID.moon_bark, 0, 6, -2), (BlockID.moon_bark, 0, 7, -2),
            (BlockID.moon_bark, 0, 8, -2), (BlockID.moon_bark, 1, 7, -2), (BlockID.moon_bark, 1, 8, -2), (BlockID.moon_bark, 1, 9, -2),
            (BlockID.moon_bark, 1, 10, -2), (BlockID.moon_bark, 1, 11, -2), (BlockID.moon_bark, 1, 12, -2), (BlockID.moon_bark, 2, 10, -2),
            (BlockID.moon_bark, 2, 11, -2), (BlockID.moon_bark, 2, 12, -2), (BlockID.moon_bark, 1, 12, -3), (BlockID.moon_bark, 1, 13, -3),
            (BlockID.moon_bark, 1, 14, -3), (BlockID.moon_bark, 1, 15, -3), (BlockID.moon_bark, 1, 16, -3), (BlockID.moon_bark, 0, 16, -3),
            (BlockID.moon_bark, 0, 17, -3), (BlockID.moon_bark, 0, 18, -3), (BlockID.moon_bark, 0, 19, -3),

            (BlockID.light, -1, 21, -2), (BlockID.light, 0, 21, -2), (BlockID.light, -1, 21, -3), (BlockID.light, 0, 21, -3),
            (BlockID.light, 1, 21, -3), (BlockID.light, 0, 21, -4), (BlockID.light, -5, 13, 3), (BlockID.light, -4, 13, 3),
            (BlockID.light, -3, 13, 3), (BlockID.light, -5, 13, 2), (BlockID.light, -4, 13, 2), (BlockID.light, -3, 13, 1),
            (BlockID.light, -3, 13, 2), (BlockID.light, -4, 13, 1),

            (BlockID.chalchanthite, 0, 20, -3), (BlockID.chalchanthite, -1, 22, -4), (BlockID.chalchanthite, 0, 22, -4), (BlockID.chalchanthite, 1, 22, -4),
            (BlockID.chalchanthite, -1, 22, -3), (BlockID.chalchanthite, 0, 22, -3), (BlockID.chalchanthite, 1, 22, -3), (BlockID.chalchanthite, -1, 23, -3),
            (BlockID.chalchanthite, 0, 23, -3), (BlockID.chalchanthite, 1, 23, -3), (BlockID.chalchanthite, -1, 22, -2), (BlockID.chalchanthite, 0, 22, -2),
            (BlockID.chalchanthite, 1, 22, -2),

            (BlockID.chalchanthite, -5, 14, 2), (BlockID.chalchanthite, -5, 14, 3), (BlockID.chalchanthite, -4, 14, 1), (BlockID.chalchanthite, -4, 14, 2),
            (BlockID.chalchanthite, -4, 14, 3), (BlockID.chalchanthite, -3, 14, 2), (BlockID.chalchanthite, -3, 14, 3), (BlockID.chalchanthite, -4, 15, 2)
        },
        new (BlockID, int, int, int)[]
        {
            (BlockID.moon_bark, 6, 0, 0), // max extent

            (BlockID.moon_bark, 1, 1, 0), (BlockID.moon_bark, 0, 1, -1), (BlockID.moon_bark, 1, 1, -1), (BlockID.moon_bark, 2, 1, -1),
            (BlockID.moon_bark, 0, 2, -1), (BlockID.moon_bark, 1, 2, -1), (BlockID.moon_bark, 2, 2, -1), (BlockID.moon_bark, 2, 1, -2),
            (BlockID.moon_bark, 1, 1, -2), (BlockID.moon_bark, 1, 2, -2), (BlockID.moon_bark, 0, 2, -2), (BlockID.moon_bark, 0, 3, -2),
            (BlockID.moon_bark, -1, 3, -2), (BlockID.moon_bark, -1, 4, -2), (BlockID.moon_bark, -1, 4, -3), (BlockID.moon_bark, -1, 5, -3),
            (BlockID.moon_bark, -1, 6, -3), (BlockID.moon_bark, -1, 7, -3), (BlockID.moon_bark, -1, 6, -4), (BlockID.moon_bark, -1, 7, -4),
            (BlockID.moon_bark, -1, 8, -4), (BlockID.moon_bark, -1, 9, -4), (BlockID.moon_bark, -2, 7, -4), (BlockID.moon_bark, -2, 8, -4),
            (BlockID.moon_bark, -2, 9, -4), (BlockID.moon_bark, -2, 8, -5), (BlockID.moon_bark, -2, 9, -5), (BlockID.moon_bark, -2, 10, -5),
            (BlockID.moon_bark, -2, 11, -5), (BlockID.moon_bark, -2, 12, -5), (BlockID.moon_bark, -2, 13, -5), (BlockID.moon_bark, -2, 14, -5),
            (BlockID.moon_bark, -2, 15, -5), (BlockID.moon_bark, -1, 9, -5), (BlockID.moon_bark, -1, 10, -5), (BlockID.moon_bark, -1, 11, -5),
            (BlockID.moon_bark, -1, 14, -5), (BlockID.moon_bark, -1, 15, -5), (BlockID.moon_bark, -1, 15, -4), (BlockID.moon_bark, 2, 2, 0),
            (BlockID.moon_bark, 2, 3, 0), (BlockID.moon_bark, 2, 3, 1), (BlockID.moon_bark, 2, 4, 1), (BlockID.moon_bark, 3, 4, 1),
            (BlockID.moon_bark, 3, 5, 1), (BlockID.moon_bark, 3, 6, 1), (BlockID.moon_bark, 3, 7, 1), (BlockID.moon_bark, 3, 7, 2),
            (BlockID.moon_bark, 3, 8, 2), (BlockID.moon_bark, 3, 9, 2), (BlockID.moon_bark, 3, 10, 2), (BlockID.moon_bark, 3, 11, 2),
            (BlockID.moon_bark, 3, 12, 2), (BlockID.moon_bark, 4, 11, 2), (BlockID.moon_bark, 4, 12, 2), (BlockID.moon_bark, 4, 13, 2),
            (BlockID.moon_bark, 4, 14, 2), (BlockID.moon_bark, 5, 12, 2), (BlockID.moon_bark, 5, 13, 2), (BlockID.moon_bark, 5, 14, 2),
            (BlockID.moon_bark, 5, 14, 3), (BlockID.moon_bark, 4, 14, 3),

            (BlockID.light, 5, 16, 4), (BlockID.light, 4, 16, 3), (BlockID.light, 5, 16, 3), (BlockID.light, 6, 16, 3),
            (BlockID.light, 5, 16, 2), (BlockID.light, 4, 16, 2), (BlockID.light, -3, 17, -6), (BlockID.light, -3, 17, -5),
            (BlockID.light, -3, 17, -4), (BlockID.light, -2, 17, -6), (BlockID.light, -2, 17, -5), (BlockID.light, -2, 17, -4),
            (BlockID.light, -1, 17, -5),

            (BlockID.feldspar, -1, 16, -6), (BlockID.feldspar, -1, 16, -5), (BlockID.feldspar, -1, 16, -4), (BlockID.feldspar, -2, 16, -6),
            (BlockID.feldspar, -2, 16, -5), (BlockID.feldspar, -2, 16, -4), (BlockID.feldspar, -3, 16, -5), (BlockID.feldspar, -3, 16, -4),
            (BlockID.feldspar, -1, 18, -5), (BlockID.feldspar, -1, 18, -4), (BlockID.feldspar, -2, 18, -6), (BlockID.feldspar, -2, 18, -5),
            (BlockID.feldspar, -2, 18, -4), (BlockID.feldspar, -3, 18, -6), (BlockID.feldspar, -3, 18, -5),

            (BlockID.chalchanthite, 4, 15, 4), (BlockID.chalchanthite, 4, 15, 3), (BlockID.chalchanthite, 4, 15, 2), (BlockID.chalchanthite, 5, 15, 4),
            (BlockID.chalchanthite, 5, 15, 3), (BlockID.chalchanthite, 5, 15, 2), (BlockID.chalchanthite, 6, 15, 3), (BlockID.chalchanthite, 6, 15, 2),
            (BlockID.chalchanthite, 4, 17, 3), (BlockID.chalchanthite, 5, 17, 4), (BlockID.chalchanthite, 5, 17, 3), (BlockID.chalchanthite, 5, 17, 2),
            (BlockID.chalchanthite, 6, 17, 4), (BlockID.chalchanthite, 6, 17, 3)
        },
        new (BlockID, int, int, int)[]
        {
            (BlockID.moon_bark, 6, 0, 0), // max extent

            (BlockID.moon_bark, 1, 1, 0), (BlockID.moon_bark, 0, 1, -1), (BlockID.moon_bark, 1, 1, -1), (BlockID.moon_bark, 2, 1, -1),
            (BlockID.moon_bark, 0, 2, -1), (BlockID.moon_bark, 1, 2, -1), (BlockID.moon_bark, 2, 2, -1), (BlockID.moon_bark, 2, 1, -2),
            (BlockID.moon_bark, 1, 1, -2), (BlockID.moon_bark, 1, 2, -2), (BlockID.moon_bark, 0, 2, -2), (BlockID.moon_bark, 0, 3, -2),
            (BlockID.moon_bark, -1, 3, -2), (BlockID.moon_bark, -1, 4, -2), (BlockID.moon_bark, -1, 4, -3), (BlockID.moon_bark, -1, 5, -3),
            (BlockID.moon_bark, -1, 6, -3), (BlockID.moon_bark, -1, 7, -3), (BlockID.moon_bark, -1, 6, -4), (BlockID.moon_bark, -1, 7, -4),
            (BlockID.moon_bark, -1, 8, -4), (BlockID.moon_bark, -1, 9, -4), (BlockID.moon_bark, -2, 7, -4), (BlockID.moon_bark, -2, 8, -4),
            (BlockID.moon_bark, -2, 9, -4), (BlockID.moon_bark, -2, 8, -5), (BlockID.moon_bark, -2, 9, -5), (BlockID.moon_bark, -2, 10, -5),
            (BlockID.moon_bark, -2, 11, -5), (BlockID.moon_bark, -2, 12, -5), (BlockID.moon_bark, -2, 13, -5), (BlockID.moon_bark, -2, 14, -5),
            (BlockID.moon_bark, -2, 15, -5), (BlockID.moon_bark, -1, 9, -5), (BlockID.moon_bark, -1, 10, -5), (BlockID.moon_bark, -1, 11, -5),
            (BlockID.moon_bark, -1, 14, -5), (BlockID.moon_bark, -1, 15, -5), (BlockID.moon_bark, -1, 15, -4), (BlockID.moon_bark, 2, 2, 0),
            (BlockID.moon_bark, 2, 3, 0), (BlockID.moon_bark, 2, 3, 1), (BlockID.moon_bark, 2, 4, 1), (BlockID.moon_bark, 3, 4, 1),
            (BlockID.moon_bark, 3, 5, 1), (BlockID.moon_bark, 3, 6, 1), (BlockID.moon_bark, 3, 7, 1), (BlockID.moon_bark, 3, 7, 2),
            (BlockID.moon_bark, 3, 8, 2), (BlockID.moon_bark, 3, 9, 2), (BlockID.moon_bark, 3, 10, 2), (BlockID.moon_bark, 3, 11, 2),
            (BlockID.moon_bark, 3, 12, 2), (BlockID.moon_bark, 4, 11, 2), (BlockID.moon_bark, 4, 12, 2), (BlockID.moon_bark, 4, 13, 2),
            (BlockID.moon_bark, 4, 14, 2), (BlockID.moon_bark, 5, 12, 2), (BlockID.moon_bark, 5, 13, 2), (BlockID.moon_bark, 5, 14, 2),
            (BlockID.moon_bark, 5, 14, 3), (BlockID.moon_bark, 4, 14, 3),

            (BlockID.light, 5, 16, 4), (BlockID.light, 4, 16, 3), (BlockID.light, 5, 16, 3), (BlockID.light, 6, 16, 3),
            (BlockID.light, 5, 16, 2), (BlockID.light, 4, 16, 2), (BlockID.light, -3, 17, -6), (BlockID.light, -3, 17, -5),
            (BlockID.light, -3, 17, -4), (BlockID.light, -2, 17, -6), (BlockID.light, -2, 17, -5), (BlockID.light, -2, 17, -4),
            (BlockID.light, -1, 17, -5),

            (BlockID.chalchanthite, -1, 16, -6), (BlockID.chalchanthite, -1, 16, -5), (BlockID.chalchanthite, -1, 16, -4), (BlockID.chalchanthite, -2, 16, -6),
            (BlockID.chalchanthite, -2, 16, -5), (BlockID.chalchanthite, -2, 16, -4), (BlockID.chalchanthite, -3, 16, -5), (BlockID.chalchanthite, -3, 16, -4),
            (BlockID.chalchanthite, -1, 18, -5), (BlockID.chalchanthite, -1, 18, -4), (BlockID.chalchanthite, -2, 18, -6), (BlockID.chalchanthite, -2, 18, -5),
            (BlockID.chalchanthite, -2, 18, -4), (BlockID.chalchanthite, -3, 18, -6), (BlockID.chalchanthite, -3, 18, -5),

            (BlockID.sulphur_ore, 4, 15, 4), (BlockID.sulphur_ore, 4, 15, 3), (BlockID.sulphur_ore, 4, 15, 2), (BlockID.sulphur_ore, 5, 15, 4),
            (BlockID.sulphur_ore, 5, 15, 3), (BlockID.sulphur_ore, 5, 15, 2), (BlockID.sulphur_ore, 6, 15, 3), (BlockID.sulphur_ore, 6, 15, 2),
            (BlockID.sulphur_ore, 4, 17, 3), (BlockID.sulphur_ore, 5, 17, 4), (BlockID.sulphur_ore, 5, 17, 3), (BlockID.sulphur_ore, 5, 17, 2),
            (BlockID.sulphur_ore, 6, 17, 4), (BlockID.sulphur_ore, 6, 17, 3)
        },
        new (BlockID, int, int, int)[]
        {
            (BlockID.moon_bark, 6, 0, 0), // max extent

            (BlockID.moon_bark, 1, 1, 0), (BlockID.moon_bark, 0, 1, -1), (BlockID.moon_bark, 1, 1, -1), (BlockID.moon_bark, 2, 1, -1),
            (BlockID.moon_bark, 0, 2, -1), (BlockID.moon_bark, 1, 2, -1), (BlockID.moon_bark, 2, 2, -1), (BlockID.moon_bark, 2, 1, -2),
            (BlockID.moon_bark, 1, 1, -2), (BlockID.moon_bark, 1, 2, -2), (BlockID.moon_bark, 0, 2, -2), (BlockID.moon_bark, 0, 3, -2),
            (BlockID.moon_bark, -1, 3, -2), (BlockID.moon_bark, -1, 4, -2), (BlockID.moon_bark, -1, 4, -3), (BlockID.moon_bark, -1, 5, -3),
            (BlockID.moon_bark, -1, 6, -3), (BlockID.moon_bark, -1, 7, -3), (BlockID.moon_bark, -1, 6, -4), (BlockID.moon_bark, -1, 7, -4),
            (BlockID.moon_bark, -1, 8, -4), (BlockID.moon_bark, -1, 9, -4), (BlockID.moon_bark, -2, 7, -4), (BlockID.moon_bark, -2, 8, -4),
            (BlockID.moon_bark, -2, 9, -4), (BlockID.moon_bark, -2, 8, -5), (BlockID.moon_bark, -2, 9, -5), (BlockID.moon_bark, -2, 10, -5),
            (BlockID.moon_bark, -2, 11, -5), (BlockID.moon_bark, -2, 12, -5), (BlockID.moon_bark, -2, 13, -5), (BlockID.moon_bark, -2, 14, -5),
            (BlockID.moon_bark, -2, 15, -5), (BlockID.moon_bark, -1, 9, -5), (BlockID.moon_bark, -1, 10, -5), (BlockID.moon_bark, -1, 11, -5),
            (BlockID.moon_bark, -1, 14, -5), (BlockID.moon_bark, -1, 15, -5), (BlockID.moon_bark, -1, 15, -4), (BlockID.moon_bark, 2, 2, 0),
            (BlockID.moon_bark, 2, 3, 0), (BlockID.moon_bark, 2, 3, 1), (BlockID.moon_bark, 2, 4, 1), (BlockID.moon_bark, 3, 4, 1),
            (BlockID.moon_bark, 3, 5, 1), (BlockID.moon_bark, 3, 6, 1), (BlockID.moon_bark, 3, 7, 1), (BlockID.moon_bark, 3, 7, 2),
            (BlockID.moon_bark, 3, 8, 2), (BlockID.moon_bark, 3, 9, 2), (BlockID.moon_bark, 3, 10, 2), (BlockID.moon_bark, 3, 11, 2),
            (BlockID.moon_bark, 3, 12, 2), (BlockID.moon_bark, 4, 11, 2), (BlockID.moon_bark, 4, 12, 2), (BlockID.moon_bark, 4, 13, 2),
            (BlockID.moon_bark, 4, 14, 2), (BlockID.moon_bark, 5, 12, 2), (BlockID.moon_bark, 5, 13, 2), (BlockID.moon_bark, 5, 14, 2),
            (BlockID.moon_bark, 5, 14, 3), (BlockID.moon_bark, 4, 14, 3),

            (BlockID.light, 5, 16, 4), (BlockID.light, 4, 16, 3), (BlockID.light, 5, 16, 3), (BlockID.light, 6, 16, 3),
            (BlockID.light, 5, 16, 2), (BlockID.light, 4, 16, 2), (BlockID.light, -3, 17, -6), (BlockID.light, -3, 17, -5),
            (BlockID.light, -3, 17, -4), (BlockID.light, -2, 17, -6), (BlockID.light, -2, 17, -5), (BlockID.light, -2, 17, -4),
            (BlockID.light, -1, 17, -5),

            (BlockID.sulphur_ore, -1, 16, -6), (BlockID.sulphur_ore, -1, 16, -5), (BlockID.sulphur_ore, -1, 16, -4), (BlockID.sulphur_ore, -2, 16, -6),
            (BlockID.sulphur_ore, -2, 16, -5), (BlockID.sulphur_ore, -2, 16, -4), (BlockID.sulphur_ore, -3, 16, -5), (BlockID.sulphur_ore, -3, 16, -4),
            (BlockID.sulphur_ore, -1, 18, -5), (BlockID.sulphur_ore, -1, 18, -4), (BlockID.sulphur_ore, -2, 18, -6), (BlockID.sulphur_ore, -2, 18, -5),
            (BlockID.sulphur_ore, -2, 18, -4), (BlockID.sulphur_ore, -3, 18, -6), (BlockID.sulphur_ore, -3, 18, -5),

            (BlockID.chalchanthite, 4, 15, 4), (BlockID.chalchanthite, 4, 15, 3), (BlockID.chalchanthite, 4, 15, 2), (BlockID.chalchanthite, 5, 15, 4),
            (BlockID.chalchanthite, 5, 15, 3), (BlockID.chalchanthite, 5, 15, 2), (BlockID.chalchanthite, 6, 15, 3), (BlockID.chalchanthite, 6, 15, 2),
            (BlockID.chalchanthite, 4, 17, 3), (BlockID.chalchanthite, 5, 17, 4), (BlockID.chalchanthite, 5, 17, 3), (BlockID.chalchanthite, 5, 17, 2),
            (BlockID.chalchanthite, 6, 17, 4), (BlockID.chalchanthite, 6, 17, 3)
        },
        new (BlockID, int, int, int)[]
        {
            (BlockID.moon_bark, 6, 0, 0), // max extent

            (BlockID.moon_bark, 1, 1, 0), (BlockID.moon_bark, 0, 1, -1), (BlockID.moon_bark, 1, 1, -1), (BlockID.moon_bark, 2, 1, -1),
            (BlockID.moon_bark, 0, 2, -1), (BlockID.moon_bark, 1, 2, -1), (BlockID.moon_bark, 2, 2, -1), (BlockID.moon_bark, 2, 1, -2),
            (BlockID.moon_bark, 1, 1, -2), (BlockID.moon_bark, 1, 2, -2), (BlockID.moon_bark, 0, 2, -2), (BlockID.moon_bark, 0, 3, -2),
            (BlockID.moon_bark, -1, 3, -2), (BlockID.moon_bark, -1, 4, -2), (BlockID.moon_bark, -1, 4, -3), (BlockID.moon_bark, -1, 5, -3),
            (BlockID.moon_bark, -1, 6, -3), (BlockID.moon_bark, -1, 7, -3), (BlockID.moon_bark, -1, 6, -4), (BlockID.moon_bark, -1, 7, -4),
            (BlockID.moon_bark, -1, 8, -4), (BlockID.moon_bark, -1, 9, -4), (BlockID.moon_bark, -2, 7, -4), (BlockID.moon_bark, -2, 8, -4),
            (BlockID.moon_bark, -2, 9, -4), (BlockID.moon_bark, -2, 8, -5), (BlockID.moon_bark, -2, 9, -5), (BlockID.moon_bark, -2, 10, -5),
            (BlockID.moon_bark, -2, 11, -5), (BlockID.moon_bark, -2, 12, -5), (BlockID.moon_bark, -2, 13, -5), (BlockID.moon_bark, -2, 14, -5),
            (BlockID.moon_bark, -2, 15, -5), (BlockID.moon_bark, -1, 9, -5), (BlockID.moon_bark, -1, 10, -5), (BlockID.moon_bark, -1, 11, -5),
            (BlockID.moon_bark, -1, 14, -5), (BlockID.moon_bark, -1, 15, -5), (BlockID.moon_bark, -1, 15, -4), (BlockID.moon_bark, 2, 2, 0),
            (BlockID.moon_bark, 2, 3, 0), (BlockID.moon_bark, 2, 3, 1), (BlockID.moon_bark, 2, 4, 1), (BlockID.moon_bark, 3, 4, 1),
            (BlockID.moon_bark, 3, 5, 1), (BlockID.moon_bark, 3, 6, 1), (BlockID.moon_bark, 3, 7, 1), (BlockID.moon_bark, 3, 7, 2),
            (BlockID.moon_bark, 3, 8, 2), (BlockID.moon_bark, 3, 9, 2), (BlockID.moon_bark, 3, 10, 2), (BlockID.moon_bark, 3, 11, 2),
            (BlockID.moon_bark, 3, 12, 2), (BlockID.moon_bark, 4, 11, 2), (BlockID.moon_bark, 4, 12, 2), (BlockID.moon_bark, 4, 13, 2),
            (BlockID.moon_bark, 4, 14, 2), (BlockID.moon_bark, 5, 12, 2), (BlockID.moon_bark, 5, 13, 2), (BlockID.moon_bark, 5, 14, 2),
            (BlockID.moon_bark, 5, 14, 3), (BlockID.moon_bark, 4, 14, 3),

            (BlockID.light, 5, 16, 4), (BlockID.light, 4, 16, 3), (BlockID.light, 5, 16, 3), (BlockID.light, 6, 16, 3),
            (BlockID.light, 5, 16, 2), (BlockID.light, 4, 16, 2), (BlockID.light, -3, 17, -6), (BlockID.light, -3, 17, -5),
            (BlockID.light, -3, 17, -4), (BlockID.light, -2, 17, -6), (BlockID.light, -2, 17, -5), (BlockID.light, -2, 17, -4),
            (BlockID.light, -1, 17, -5),

            (BlockID.feldspar, -1, 16, -6), (BlockID.feldspar, -1, 16, -5), (BlockID.feldspar, -1, 16, -4), (BlockID.feldspar, -2, 16, -6),
            (BlockID.feldspar, -2, 16, -5), (BlockID.feldspar, -2, 16, -4), (BlockID.feldspar, -3, 16, -5), (BlockID.feldspar, -3, 16, -4),
            (BlockID.feldspar, -1, 18, -5), (BlockID.feldspar, -1, 18, -4), (BlockID.feldspar, -2, 18, -6), (BlockID.feldspar, -2, 18, -5),
            (BlockID.feldspar, -2, 18, -4), (BlockID.feldspar, -3, 18, -6), (BlockID.feldspar, -3, 18, -5),

            (BlockID.sulphur_ore, 4, 15, 4), (BlockID.sulphur_ore, 4, 15, 3), (BlockID.sulphur_ore, 4, 15, 2), (BlockID.sulphur_ore, 5, 15, 4),
            (BlockID.sulphur_ore, 5, 15, 3), (BlockID.sulphur_ore, 5, 15, 2), (BlockID.sulphur_ore, 6, 15, 3), (BlockID.sulphur_ore, 6, 15, 2),
            (BlockID.sulphur_ore, 4, 17, 3), (BlockID.sulphur_ore, 5, 17, 4), (BlockID.sulphur_ore, 5, 17, 3), (BlockID.sulphur_ore, 5, 17, 2),
            (BlockID.sulphur_ore, 6, 17, 4), (BlockID.sulphur_ore, 6, 17, 3)
        },
        new (BlockID, int, int, int)[]
        {
            (BlockID.moon_bark, 6, 0, 0), // max extent

            (BlockID.moon_bark, 1, 1, 0), (BlockID.moon_bark, 0, 1, -1), (BlockID.moon_bark, 1, 1, -1), (BlockID.moon_bark, 2, 1, -1),
            (BlockID.moon_bark, 0, 2, -1), (BlockID.moon_bark, 1, 2, -1), (BlockID.moon_bark, 2, 2, -1), (BlockID.moon_bark, 2, 1, -2),
            (BlockID.moon_bark, 1, 1, -2), (BlockID.moon_bark, 1, 2, -2), (BlockID.moon_bark, 0, 2, -2), (BlockID.moon_bark, 0, 3, -2),
            (BlockID.moon_bark, -1, 3, -2), (BlockID.moon_bark, -1, 4, -2), (BlockID.moon_bark, -1, 4, -3), (BlockID.moon_bark, -1, 5, -3),
            (BlockID.moon_bark, -1, 6, -3), (BlockID.moon_bark, -1, 7, -3), (BlockID.moon_bark, -1, 6, -4), (BlockID.moon_bark, -1, 7, -4),
            (BlockID.moon_bark, -1, 8, -4), (BlockID.moon_bark, -1, 9, -4), (BlockID.moon_bark, -2, 7, -4), (BlockID.moon_bark, -2, 8, -4),
            (BlockID.moon_bark, -2, 9, -4), (BlockID.moon_bark, -2, 8, -5), (BlockID.moon_bark, -2, 9, -5), (BlockID.moon_bark, -2, 10, -5),
            (BlockID.moon_bark, -2, 11, -5), (BlockID.moon_bark, -2, 12, -5), (BlockID.moon_bark, -2, 13, -5), (BlockID.moon_bark, -2, 14, -5),
            (BlockID.moon_bark, -2, 15, -5), (BlockID.moon_bark, -1, 9, -5), (BlockID.moon_bark, -1, 10, -5), (BlockID.moon_bark, -1, 11, -5),
            (BlockID.moon_bark, -1, 14, -5), (BlockID.moon_bark, -1, 15, -5), (BlockID.moon_bark, -1, 15, -4), (BlockID.moon_bark, 2, 2, 0),
            (BlockID.moon_bark, 2, 3, 0), (BlockID.moon_bark, 2, 3, 1), (BlockID.moon_bark, 2, 4, 1), (BlockID.moon_bark, 3, 4, 1),
            (BlockID.moon_bark, 3, 5, 1), (BlockID.moon_bark, 3, 6, 1), (BlockID.moon_bark, 3, 7, 1), (BlockID.moon_bark, 3, 7, 2),
            (BlockID.moon_bark, 3, 8, 2), (BlockID.moon_bark, 3, 9, 2), (BlockID.moon_bark, 3, 10, 2), (BlockID.moon_bark, 3, 11, 2),
            (BlockID.moon_bark, 3, 12, 2), (BlockID.moon_bark, 4, 11, 2), (BlockID.moon_bark, 4, 12, 2), (BlockID.moon_bark, 4, 13, 2),
            (BlockID.moon_bark, 4, 14, 2), (BlockID.moon_bark, 5, 12, 2), (BlockID.moon_bark, 5, 13, 2), (BlockID.moon_bark, 5, 14, 2),
            (BlockID.moon_bark, 5, 14, 3), (BlockID.moon_bark, 4, 14, 3),

            (BlockID.light, 5, 16, 4), (BlockID.light, 4, 16, 3), (BlockID.light, 5, 16, 3), (BlockID.light, 6, 16, 3),
            (BlockID.light, 5, 16, 2), (BlockID.light, 4, 16, 2), (BlockID.light, -3, 17, -6), (BlockID.light, -3, 17, -5),
            (BlockID.light, -3, 17, -4), (BlockID.light, -2, 17, -6), (BlockID.light, -2, 17, -5), (BlockID.light, -2, 17, -4),
            (BlockID.light, -1, 17, -5),

            (BlockID.sulphur_ore, -1, 16, -6), (BlockID.sulphur_ore, -1, 16, -5), (BlockID.sulphur_ore, -1, 16, -4), (BlockID.sulphur_ore, -2, 16, -6),
            (BlockID.sulphur_ore, -2, 16, -5), (BlockID.sulphur_ore, -2, 16, -4), (BlockID.sulphur_ore, -3, 16, -5), (BlockID.sulphur_ore, -3, 16, -4),
            (BlockID.sulphur_ore, -1, 18, -5), (BlockID.sulphur_ore, -1, 18, -4), (BlockID.sulphur_ore, -2, 18, -6), (BlockID.sulphur_ore, -2, 18, -5),
            (BlockID.sulphur_ore, -2, 18, -4), (BlockID.sulphur_ore, -3, 18, -6), (BlockID.sulphur_ore, -3, 18, -5),

            (BlockID.feldspar, 4, 15, 4), (BlockID.feldspar, 4, 15, 3), (BlockID.feldspar, 4, 15, 2), (BlockID.feldspar, 5, 15, 4),
            (BlockID.feldspar, 5, 15, 3), (BlockID.feldspar, 5, 15, 2), (BlockID.feldspar, 6, 15, 3), (BlockID.feldspar, 6, 15, 2),
            (BlockID.feldspar, 4, 17, 3), (BlockID.feldspar, 5, 17, 4), (BlockID.feldspar, 5, 17, 3), (BlockID.feldspar, 5, 17, 2),
            (BlockID.feldspar, 6, 17, 4), (BlockID.feldspar, 6, 17, 3)
        },
        new (BlockID, int, int, int)[]
        {
            (BlockID.moon_bark, 6, 0, 0), // max extent

            (BlockID.moon_bark, 1, 1, 0), (BlockID.moon_bark, 0, 1, -1), (BlockID.moon_bark, 1, 1, -1), (BlockID.moon_bark, 2, 1, -1),
            (BlockID.moon_bark, 0, 2, -1), (BlockID.moon_bark, 1, 2, -1), (BlockID.moon_bark, 2, 2, -1), (BlockID.moon_bark, 2, 1, -2),
            (BlockID.moon_bark, 1, 1, -2), (BlockID.moon_bark, 1, 2, -2), (BlockID.moon_bark, 0, 2, -2), (BlockID.moon_bark, 0, 3, -2),
            (BlockID.moon_bark, -1, 3, -2), (BlockID.moon_bark, -1, 4, -2), (BlockID.moon_bark, -1, 4, -3), (BlockID.moon_bark, -1, 5, -3),
            (BlockID.moon_bark, -1, 6, -3), (BlockID.moon_bark, -1, 7, -3), (BlockID.moon_bark, -1, 6, -4), (BlockID.moon_bark, -1, 7, -4),
            (BlockID.moon_bark, -1, 8, -4), (BlockID.moon_bark, -1, 9, -4), (BlockID.moon_bark, -2, 7, -4), (BlockID.moon_bark, -2, 8, -4),
            (BlockID.moon_bark, -2, 9, -4), (BlockID.moon_bark, -2, 8, -5), (BlockID.moon_bark, -2, 9, -5), (BlockID.moon_bark, -2, 10, -5),
            (BlockID.moon_bark, -2, 11, -5), (BlockID.moon_bark, -2, 12, -5), (BlockID.moon_bark, -2, 13, -5), (BlockID.moon_bark, -2, 14, -5),
            (BlockID.moon_bark, -2, 15, -5), (BlockID.moon_bark, -1, 9, -5), (BlockID.moon_bark, -1, 10, -5), (BlockID.moon_bark, -1, 11, -5),
            (BlockID.moon_bark, -1, 14, -5), (BlockID.moon_bark, -1, 15, -5), (BlockID.moon_bark, -1, 15, -4), (BlockID.moon_bark, 2, 2, 0),
            (BlockID.moon_bark, 2, 3, 0), (BlockID.moon_bark, 2, 3, 1), (BlockID.moon_bark, 2, 4, 1), (BlockID.moon_bark, 3, 4, 1),
            (BlockID.moon_bark, 3, 5, 1), (BlockID.moon_bark, 3, 6, 1), (BlockID.moon_bark, 3, 7, 1), (BlockID.moon_bark, 3, 7, 2),
            (BlockID.moon_bark, 3, 8, 2), (BlockID.moon_bark, 3, 9, 2), (BlockID.moon_bark, 3, 10, 2), (BlockID.moon_bark, 3, 11, 2),
            (BlockID.moon_bark, 3, 12, 2), (BlockID.moon_bark, 4, 11, 2), (BlockID.moon_bark, 4, 12, 2), (BlockID.moon_bark, 4, 13, 2),
            (BlockID.moon_bark, 4, 14, 2), (BlockID.moon_bark, 5, 12, 2), (BlockID.moon_bark, 5, 13, 2), (BlockID.moon_bark, 5, 14, 2),
            (BlockID.moon_bark, 5, 14, 3), (BlockID.moon_bark, 4, 14, 3),

            (BlockID.light, 5, 16, 4), (BlockID.light, 4, 16, 3), (BlockID.light, 5, 16, 3), (BlockID.light, 6, 16, 3),
            (BlockID.light, 5, 16, 2), (BlockID.light, 4, 16, 2), (BlockID.light, -3, 17, -6), (BlockID.light, -3, 17, -5),
            (BlockID.light, -3, 17, -4), (BlockID.light, -2, 17, -6), (BlockID.light, -2, 17, -5), (BlockID.light, -2, 17, -4),
            (BlockID.light, -1, 17, -5),

            (BlockID.sulphur_ore, -1, 16, -6), (BlockID.sulphur_ore, -1, 16, -5), (BlockID.sulphur_ore, -1, 16, -4), (BlockID.sulphur_ore, -2, 16, -6),
            (BlockID.sulphur_ore, -2, 16, -5), (BlockID.sulphur_ore, -2, 16, -4), (BlockID.sulphur_ore, -3, 16, -5), (BlockID.sulphur_ore, -3, 16, -4),
            (BlockID.sulphur_ore, -1, 18, -5), (BlockID.sulphur_ore, -1, 18, -4), (BlockID.sulphur_ore, -2, 18, -6), (BlockID.sulphur_ore, -2, 18, -5),
            (BlockID.sulphur_ore, -2, 18, -4), (BlockID.sulphur_ore, -3, 18, -6), (BlockID.sulphur_ore, -3, 18, -5),

            (BlockID.sulphur_ore, 4, 15, 4), (BlockID.sulphur_ore, 4, 15, 3), (BlockID.sulphur_ore, 4, 15, 2), (BlockID.sulphur_ore, 5, 15, 4),
            (BlockID.sulphur_ore, 5, 15, 3), (BlockID.sulphur_ore, 5, 15, 2), (BlockID.sulphur_ore, 6, 15, 3), (BlockID.sulphur_ore, 6, 15, 2),
            (BlockID.sulphur_ore, 4, 17, 3), (BlockID.sulphur_ore, 5, 17, 4), (BlockID.sulphur_ore, 5, 17, 3), (BlockID.sulphur_ore, 5, 17, 2),
            (BlockID.sulphur_ore, 6, 17, 4), (BlockID.sulphur_ore, 6, 17, 3)
        },
        new (BlockID, int, int, int)[]
        {
            (BlockID.moon_bark, 3, 0, 0), // max extent

            (BlockID.moon_bark, -2, 1, 0), (BlockID.moon_bark, -3, 1, -1), (BlockID.moon_bark, -2, 1, -1), (BlockID.moon_bark, -2, 2, -1),
            (BlockID.moon_bark, -1, 2, -1), (BlockID.moon_bark, -1, 3, -1), (BlockID.moon_bark, -1, 3, 0), (BlockID.moon_bark, -1, 4, 0),
            (BlockID.moon_bark, 0, 4, 0), (BlockID.moon_bark, 0, 4, -1), (BlockID.moon_bark, 0, 5, 0), (BlockID.moon_bark, 0, 5, -1),
            (BlockID.moon_bark, 0, 6, 0), (BlockID.moon_bark, 1, 5, 0), (BlockID.moon_bark, 1, 6, 0), (BlockID.moon_bark, 1, 7, 0),
            (BlockID.moon_bark, 1, 8, 0), (BlockID.moon_bark, 1, 9, 0), (BlockID.moon_bark, 1, 7, -1), (BlockID.moon_bark, 1, 8, -1),
            (BlockID.moon_bark, 1, 9, -1), (BlockID.moon_bark, 1, 10, -1), (BlockID.moon_bark, 2, 10, -1), (BlockID.moon_bark, 2, 11, -1),
            (BlockID.moon_bark, 2, 12, -1), (BlockID.moon_bark, 2, 13, -1), (BlockID.moon_bark, 2, 13, 0), (BlockID.moon_bark, 2, 14, 0),
            (BlockID.moon_bark, 2, 15, 0), (BlockID.moon_bark, 3, 15, 0), (BlockID.moon_bark, 3, 16, 0), (BlockID.moon_bark, 3, 17, 0),
            (BlockID.moon_bark, 3, 18, 0), (BlockID.moon_bark, 3, 18, 1), (BlockID.moon_bark, 3, 19, 1), (BlockID.moon_bark, 2, 19, 1),

            (BlockID.feldspar, 1, 20, 0), (BlockID.feldspar, 2, 20, 0), (BlockID.feldspar, 1, 20, 1), (BlockID.feldspar, 2, 20, 1),
            (BlockID.feldspar, 3, 20, 1), (BlockID.feldspar, 1, 20, 2), (BlockID.feldspar, 3, 20, 2), (BlockID.feldspar, 1, 22, 0),
            (BlockID.feldspar, 2, 22, 0), (BlockID.feldspar, 3, 22, 0), (BlockID.feldspar, 1, 22, 1), (BlockID.feldspar, 2, 22, 1),
            (BlockID.feldspar, 3, 22, 1), (BlockID.feldspar, 2, 22, 2), (BlockID.feldspar, 3, 22, 2),

            (BlockID.light, 1, 21, 0), (BlockID.light, 1, 21, 1), (BlockID.light, 1, 21, 2), (BlockID.light, 2, 21, 0),
            (BlockID.light, 2, 21, 1), (BlockID.light, 2, 21, 2), (BlockID.light, 3, 21, 0), (BlockID.light, 3, 21, 1)
        },
        new (BlockID, int, int, int)[]
        {
            (BlockID.moon_bark, 3, 0, 0), // max extent

            (BlockID.moon_bark, -2, 1, 0), (BlockID.moon_bark, -3, 1, -1), (BlockID.moon_bark, -2, 1, -1), (BlockID.moon_bark, -2, 2, -1),
            (BlockID.moon_bark, -1, 2, -1), (BlockID.moon_bark, -1, 3, -1), (BlockID.moon_bark, -1, 3, 0), (BlockID.moon_bark, -1, 4, 0),
            (BlockID.moon_bark, 0, 4, 0), (BlockID.moon_bark, 0, 4, -1), (BlockID.moon_bark, 0, 5, 0), (BlockID.moon_bark, 0, 5, -1),
            (BlockID.moon_bark, 0, 6, 0), (BlockID.moon_bark, 1, 5, 0), (BlockID.moon_bark, 1, 6, 0), (BlockID.moon_bark, 1, 7, 0),
            (BlockID.moon_bark, 1, 8, 0), (BlockID.moon_bark, 1, 9, 0), (BlockID.moon_bark, 1, 7, -1), (BlockID.moon_bark, 1, 8, -1),
            (BlockID.moon_bark, 1, 9, -1), (BlockID.moon_bark, 1, 10, -1), (BlockID.moon_bark, 2, 10, -1), (BlockID.moon_bark, 2, 11, -1),
            (BlockID.moon_bark, 2, 12, -1), (BlockID.moon_bark, 2, 13, -1), (BlockID.moon_bark, 2, 13, 0), (BlockID.moon_bark, 2, 14, 0),
            (BlockID.moon_bark, 2, 15, 0), (BlockID.moon_bark, 3, 15, 0), (BlockID.moon_bark, 3, 16, 0), (BlockID.moon_bark, 3, 17, 0),
            (BlockID.moon_bark, 3, 18, 0), (BlockID.moon_bark, 3, 18, 1), (BlockID.moon_bark, 3, 19, 1), (BlockID.moon_bark, 2, 19, 1),

            (BlockID.chalchanthite, 1, 20, 0), (BlockID.chalchanthite, 2, 20, 0), (BlockID.chalchanthite, 1, 20, 1), (BlockID.chalchanthite, 2, 20, 1),
            (BlockID.chalchanthite, 3, 20, 1), (BlockID.chalchanthite, 1, 20, 2), (BlockID.chalchanthite, 3, 20, 2), (BlockID.chalchanthite, 1, 22, 0),
            (BlockID.chalchanthite, 2, 22, 0), (BlockID.chalchanthite, 3, 22, 0), (BlockID.chalchanthite, 1, 22, 1), (BlockID.chalchanthite, 2, 22, 1),
            (BlockID.chalchanthite, 3, 22, 1),

            (BlockID.light, 1, 21, 0), (BlockID.light, 1, 21, 1), (BlockID.light, 1, 21, 2), (BlockID.light, 2, 21, 0),
            (BlockID.light, 2, 21, 1), (BlockID.light, 2, 21, 2), (BlockID.light, 3, 21, 0), (BlockID.light, 3, 21, 1)
        },
        new (BlockID, int, int, int)[]
        {
            (BlockID.moon_bark, 3, 0, 0), // max extent

            (BlockID.moon_bark, -2, 1, 0), (BlockID.moon_bark, -3, 1, -1), (BlockID.moon_bark, -2, 1, -1), (BlockID.moon_bark, -2, 2, -1),
            (BlockID.moon_bark, -1, 2, -1), (BlockID.moon_bark, -1, 3, -1), (BlockID.moon_bark, -1, 3, 0), (BlockID.moon_bark, -1, 4, 0),
            (BlockID.moon_bark, 0, 4, 0), (BlockID.moon_bark, 0, 4, -1), (BlockID.moon_bark, 0, 5, 0), (BlockID.moon_bark, 0, 5, -1),
            (BlockID.moon_bark, 0, 6, 0), (BlockID.moon_bark, 1, 5, 0), (BlockID.moon_bark, 1, 6, 0), (BlockID.moon_bark, 1, 7, 0),
            (BlockID.moon_bark, 1, 8, 0), (BlockID.moon_bark, 1, 9, 0), (BlockID.moon_bark, 1, 7, -1), (BlockID.moon_bark, 1, 8, -1),
            (BlockID.moon_bark, 1, 9, -1), (BlockID.moon_bark, 1, 10, -1), (BlockID.moon_bark, 2, 10, -1), (BlockID.moon_bark, 2, 11, -1),
            (BlockID.moon_bark, 2, 12, -1), (BlockID.moon_bark, 2, 13, -1), (BlockID.moon_bark, 2, 13, 0), (BlockID.moon_bark, 2, 14, 0),
            (BlockID.moon_bark, 2, 15, 0), (BlockID.moon_bark, 3, 15, 0), (BlockID.moon_bark, 3, 16, 0), (BlockID.moon_bark, 3, 17, 0),
            (BlockID.moon_bark, 3, 18, 0), (BlockID.moon_bark, 3, 18, 1), (BlockID.moon_bark, 3, 19, 1), (BlockID.moon_bark, 2, 19, 1),

            (BlockID.sulphur_ore, 2, 20, 0), (BlockID.sulphur_ore, 1, 20, 1), (BlockID.sulphur_ore, 2, 20, 1), (BlockID.sulphur_ore, 3, 20, 1),
            (BlockID.sulphur_ore, 1, 20, 2), (BlockID.sulphur_ore, 3, 20, 2), (BlockID.sulphur_ore, 1, 22, 0), (BlockID.sulphur_ore, 2, 22, 0),
            (BlockID.sulphur_ore, 3, 22, 0), (BlockID.sulphur_ore, 1, 22, 1), (BlockID.sulphur_ore, 2, 22, 1), (BlockID.sulphur_ore, 3, 22, 1),
            (BlockID.sulphur_ore, 2, 22, 2), (BlockID.sulphur_ore, 3, 22, 2),

            (BlockID.light, 1, 21, 0), (BlockID.light, 1, 21, 1), (BlockID.light, 1, 21, 2), (BlockID.light, 2, 21, 0),
            (BlockID.light, 2, 21, 1), (BlockID.light, 2, 21, 2), (BlockID.light, 3, 21, 0), (BlockID.light, 3, 21, 1)
        },
    };

    private static readonly (BlockID, int, int, int)[][] SPIRAL_LIGHT_TREE_SHAPES = {
        new (BlockID, int, int, int)[]
        {
            (BlockID.moon_wood, 5, 0, 0), // max extent

            (BlockID.moon_wood, -1, 1, 0), (BlockID.moon_wood, -2, 1, 0), (BlockID.moon_wood, -3, 1, 0), (BlockID.moon_wood, -4, 1, 0),
            (BlockID.moon_wood, 0, 1, 1), (BlockID.moon_wood, -1, 1, 1), (BlockID.moon_wood, -2, 1, 1), (BlockID.moon_wood, -3, 1, 1),
            (BlockID.moon_wood, -1, 2, 1), (BlockID.moon_wood, -2, 2, 1), (BlockID.moon_wood, -3, 2, 1), (BlockID.moon_wood, -4, 2, 1),
            (BlockID.moon_wood, 0, 1, 2), (BlockID.moon_wood, -1, 1, 2), (BlockID.moon_wood, -2, 1, 2), (BlockID.moon_wood, -3, 1, 2),
            (BlockID.moon_wood, 0, 2, 2), (BlockID.moon_wood, -1, 2, 2), (BlockID.moon_wood, -2, 1, 3), (BlockID.moon_wood, -2, 2, 3),
            (BlockID.moon_wood, -2, 3, 3), (BlockID.moon_wood, -2, 4, 3), (BlockID.moon_wood, -2, 5, 3), (BlockID.moon_wood, -2, 6, 3),
            (BlockID.moon_wood, -2, 7, 3), (BlockID.moon_wood, -3, 4, 3), (BlockID.moon_wood, -3, 5, 3), (BlockID.moon_wood, -3, 6, 3),
            (BlockID.moon_wood, -2, 3, 4), (BlockID.moon_wood, -2, 4, 4), (BlockID.moon_wood, -2, 5, 4), (BlockID.moon_wood, -2, 6, 4),
            (BlockID.moon_wood, -1, 2, 4), (BlockID.moon_wood, -1, 3, 4), (BlockID.moon_wood, -1, 4, 4), (BlockID.moon_wood, -1, 1, 3),
            (BlockID.moon_wood, -1, 2, 3), (BlockID.moon_wood, -1, 3, 3), (BlockID.moon_wood, -1, 4, 3), (BlockID.moon_wood, 0, 2, 3),
            (BlockID.moon_wood, 0, 3, 3), (BlockID.moon_wood, -2, 6, 2), (BlockID.moon_wood, -1, 7, 2), (BlockID.moon_wood, -1, 8, 2),
            (BlockID.moon_wood, 0, 8, 2), (BlockID.moon_wood, 0, 8, 3), (BlockID.moon_wood, 0, 9, 3), (BlockID.moon_wood, -1, 6, 3),
            (BlockID.moon_wood, 1, 9, 3), (BlockID.moon_wood, 0, 9, 4), (BlockID.moon_wood, 0, 10, 4), (BlockID.moon_wood, 1, 10, 4),
            (BlockID.moon_wood, 0, 10, 5), (BlockID.moon_wood, 0, 11, 5), (BlockID.moon_wood, -1, 11, 5), (BlockID.moon_wood, -2, 11, 5),
            (BlockID.moon_wood, -2, 12, 5), (BlockID.moon_wood, -2, 12, 4), (BlockID.moon_wood, -2, 13, 4), (BlockID.moon_wood, -2, 13, 3),
            (BlockID.moon_wood, -3, 13, 4), (BlockID.moon_wood, -3, 13, 3), (BlockID.moon_wood, -2, 14, 3), (BlockID.moon_wood, -2, 14, 2),
            (BlockID.moon_wood, -4, 3, 1), (BlockID.moon_wood, -4, 1, -1), (BlockID.moon_wood, -3, 1, -1), (BlockID.moon_wood, -4, 2, 0),
            (BlockID.moon_wood, -4, 3, 0), (BlockID.moon_wood, -4, 4, 0), (BlockID.moon_wood, -3, 2, 0), (BlockID.moon_wood, -3, 3, 0),
            (BlockID.moon_wood, -3, 4, 0), (BlockID.moon_wood, -3, 4, -1), (BlockID.moon_wood, -4, 4, -1), (BlockID.moon_wood, -3, 5, -1),
            (BlockID.moon_wood, -4, 5, -1), (BlockID.moon_wood, -3, 5, -2), (BlockID.moon_wood, -4, 5, -2), (BlockID.moon_wood, -3, 6, -2),
            (BlockID.moon_wood, -2, 6, -2), (BlockID.moon_wood, -3, 6, -3), (BlockID.moon_wood, -2, 6, -3), (BlockID.moon_wood, -2, 7, -3),
            (BlockID.moon_wood, -1, 7, -3), (BlockID.moon_wood, -1, 8, -3), (BlockID.moon_wood, 0, 8, -3), (BlockID.moon_wood, 0, 9, -3),
            (BlockID.moon_wood, 0, 9, -2), (BlockID.moon_wood, 1, 9, -3), (BlockID.moon_wood, 1, 9, -2), (BlockID.moon_wood, 1, 10, -2),
            (BlockID.moon_wood, 1, 10, -1), (BlockID.moon_wood, 1, 11, -1), (BlockID.moon_wood, 2, 11, -1), (BlockID.moon_wood, 1, 11, 0),
            (BlockID.moon_wood, 2, 11, 0), (BlockID.moon_wood, 1, 12, 0), (BlockID.moon_wood, 2, 12, 0), (BlockID.moon_wood, 1, 12, 1),
            (BlockID.moon_wood, 0, 13, 1), (BlockID.moon_wood, 0, 13, 2), (BlockID.moon_wood, 0, 14, 1),

            (BlockID.light, -1, 14, 3), (BlockID.light, 0, 14, 3), (BlockID.light, -1, 15, 3), (BlockID.light, 0, 15, 3),
            (BlockID.light, 1, 15, 3), (BlockID.light, -2, 15, 2), (BlockID.light, -1, 15, 2), (BlockID.light, 0, 15, 2),
            (BlockID.light, 1, 15, 2), (BlockID.light, 0, 15, 1), (BlockID.light, 0, 14, 2), (BlockID.light, -1, 14, 2),
            (BlockID.light, 0, 16, 2), (BlockID.light, -1, 16, 2), (BlockID.light, 1, 14, 2)
        },
        new (BlockID, int, int, int)[]
        {
            (BlockID.moon_wood, 3, 0, 0), // max extent

            (BlockID.moon_wood, -1, 1, -1), (BlockID.moon_wood, -1, 2, -1), (BlockID.moon_wood, 0, 1, -2), (BlockID.moon_wood, 0, 2, -2),
            (BlockID.moon_wood, 0, 1, -3), (BlockID.moon_wood, -1, 1, -3), (BlockID.moon_wood, -2, 1, -3), (BlockID.moon_wood, -1, 2, -3),
            (BlockID.moon_wood, -2, 2, -3), (BlockID.moon_wood, -1, 1, -2), (BlockID.moon_wood, -2, 1, -2), (BlockID.moon_wood, -1, 2, -2),
            (BlockID.moon_wood, -2, 2, -2), (BlockID.moon_wood, -1, 3, -2), (BlockID.moon_wood, -2, 3, -2), (BlockID.moon_wood, -1, 4, -2),
            (BlockID.moon_wood, -1, 5, -2), (BlockID.moon_wood, -1, 6, -2), (BlockID.moon_wood, -1, 7, -2), (BlockID.moon_wood, -1, 8, -2),
            (BlockID.moon_wood, -1, 9, -2), (BlockID.moon_wood, -2, 4, -3), (BlockID.moon_wood, -1, 4, -3), (BlockID.moon_wood, -1, 5, -3),
            (BlockID.moon_wood, 0, 5, -3), (BlockID.moon_wood, 0, 4, -2), (BlockID.moon_wood, 0, 5, -2), (BlockID.moon_wood, 0, 6, -2),
            (BlockID.moon_wood, 0, 5, -1), (BlockID.moon_wood, 0, 6, -1), (BlockID.moon_wood, 0, 7, -1), (BlockID.moon_wood, -1, 6, -1),
            (BlockID.moon_wood, -1, 7, -1), (BlockID.moon_wood, -1, 8, -1), (BlockID.moon_wood, 0, 7, 0), (BlockID.moon_wood, 0, 7, 1),
            (BlockID.moon_wood, 0, 8, 1), (BlockID.moon_wood, 0, 8, 2), (BlockID.moon_wood, 0, 9, 2), (BlockID.moon_wood, -2, 7, -1),
            (BlockID.moon_wood, -2, 8, -1), (BlockID.moon_wood, -2, 9, -1), (BlockID.moon_wood, -3, 8, -1), (BlockID.moon_wood, -2, 8, -2),
            (BlockID.moon_wood, -2, 9, -2), (BlockID.moon_wood, -2, 10, -2), (BlockID.moon_wood, -3, 9, -2), (BlockID.moon_wood, -2, 9, -3),
            (BlockID.moon_wood, -2, 10, -3), (BlockID.moon_wood, -1, 10, -3), (BlockID.moon_wood, -1, 11, -3), (BlockID.moon_wood, 0, 11, -3),
            (BlockID.moon_wood, 0, 11, -2), (BlockID.moon_wood, 0, 12, -2), (BlockID.moon_wood, 0, 12, -1), (BlockID.moon_wood, 0, 13, -1),
            (BlockID.moon_wood, 0, 13, 0), (BlockID.moon_wood, -1, 13, 0), (BlockID.moon_wood, -2, 15, 0), (BlockID.moon_wood, -1, 14, 0),
            (BlockID.moon_wood, -2, 14, 0),

            (BlockID.light, -1, 8, 1), (BlockID.light, 1, 8, 1), (BlockID.light, -1, 9, 1), (BlockID.light, 0, 9, 1),
            (BlockID.light, 1, 9, 1), (BlockID.light, -1, 9, 2), (BlockID.light, 1, 9, 2), (BlockID.light, -1, 9, 3),
            (BlockID.light, 0, 9, 3), (BlockID.light, 1, 9, 3), (BlockID.light, -1, 8, 2), (BlockID.light, 0, 8, 3),
            (BlockID.light, 1, 8, 2), (BlockID.light, 1, 10, 2), (BlockID.light, 0, 10, 2), (BlockID.light, -1, 10, 2),
            (BlockID.light, 1, 10, 3), (BlockID.light, 0, 10, 3), (BlockID.light, 0, 10, 1), (BlockID.light, -1, 15, -1),
            (BlockID.light, -1, 15, 0), (BlockID.light, -1, 15, 1), (BlockID.light, -2, 15, -1), (BlockID.light, -2, 15, 1),
            (BlockID.light, -3, 15, -1), (BlockID.light, -3, 15, 0), (BlockID.light, -3, 15, 1), (BlockID.light, -1, 14, 1),
            (BlockID.light, -2, 14, 1), (BlockID.light, -3, 14, 1), (BlockID.light, -2, 14, -1), (BlockID.light, -3, 14, 0),
            (BlockID.light, -1, 16, 0), (BlockID.light, -2, 16, 0), (BlockID.light, -3, 16, 0), (BlockID.light, -2, 16, -1),
            (BlockID.light, -3, 16, -1), (BlockID.light, -2, 16, 1)
        },
        new (BlockID, int, int, int)[]
        {
            (BlockID.moon_wood, 5, 0, 0), // max extent

            (BlockID.moon_wood, 2, 1, -1), (BlockID.moon_wood, 2, 2, -1), (BlockID.moon_wood, 3, 2, -1), (BlockID.moon_wood, 3, 3, -1),
            (BlockID.moon_wood, 4, 3, -1), (BlockID.moon_wood, 4, 4, -1), (BlockID.moon_wood, 4, 4, 0), (BlockID.moon_wood, 4, 5, 0),
            (BlockID.moon_wood, 4, 5, 1), (BlockID.moon_wood, 3, 5, 1), (BlockID.moon_wood, 3, 6, 1), (BlockID.moon_wood, 2, 6, 1),
            (BlockID.moon_wood, 2, 7, 1), (BlockID.moon_wood, 2, 7, 2), (BlockID.moon_wood, 2, 8, 2), (BlockID.moon_wood, 2, 8, 3),
            (BlockID.moon_wood, 2, 8, 4), (BlockID.moon_wood, 2, 9, 4), (BlockID.moon_wood, 1, 9, 4), (BlockID.moon_wood, 0, 10, 4),
            (BlockID.moon_wood, -1, 10, 4), (BlockID.moon_wood, 1, 7, 1), (BlockID.moon_wood, 1, 7, 0), (BlockID.moon_wood, 1, 8, 0),
            (BlockID.moon_wood, 1, 8, -1), (BlockID.moon_wood, 1, 9, -1), (BlockID.moon_wood, 2, 9, -1), (BlockID.moon_wood, 2, 10, -1),
            (BlockID.moon_wood, 3, 10, -1), (BlockID.moon_wood, 3, 11, -1), (BlockID.moon_wood, 3, 12, -1), (BlockID.moon_wood, 3, 12, 0),
            (BlockID.moon_wood, 3, 13, 0), (BlockID.moon_wood, 2, 13, 0), (BlockID.moon_wood, 2, 14, 0), (BlockID.moon_wood, 2, 14, 1),
            (BlockID.moon_wood, 3, 1, 1), (BlockID.moon_wood, 2, 1, 1), (BlockID.moon_wood, 2, 2, 1), (BlockID.moon_wood, 1, 2, 1),
            (BlockID.moon_wood, 1, 3, 1), (BlockID.moon_wood, 0, 3, 1), (BlockID.moon_wood, -1, 3, 1), (BlockID.moon_wood, 0, 4, 1),
            (BlockID.moon_wood, -1, 4, 1), (BlockID.moon_wood, 1, 2, 0), (BlockID.moon_wood, 1, 3, 0), (BlockID.moon_wood, 0, 3, 0),
            (BlockID.moon_wood, -1, 3, 0), (BlockID.moon_wood, -1, 4, 0), (BlockID.moon_wood, -2, 4, 0), (BlockID.moon_wood, -2, 4, 1),
            (BlockID.moon_wood, -2, 5, 1), (BlockID.moon_wood, -3, 5, 1), (BlockID.moon_wood, -3, 5, 0), (BlockID.moon_wood, -3, 6, 0),
            (BlockID.moon_wood, -4, 6, 1), (BlockID.moon_wood, -5, 6, 1), (BlockID.moon_wood, -5, 7, 1), (BlockID.moon_wood, -5, 7, 0),
            (BlockID.moon_wood, -5, 8, 0), (BlockID.moon_wood, -5, 8, -1), (BlockID.moon_wood, -5, 9, -1), (BlockID.moon_wood, -4, 6, 0),
            (BlockID.moon_wood, -4, 7, 0), (BlockID.moon_wood, -4, 8, -1), (BlockID.moon_wood, -4, 9, -1), (BlockID.moon_wood, -4, 9, -2),
            (BlockID.moon_wood, -4, 10, -2), (BlockID.moon_wood, -4, 10, -3), (BlockID.moon_wood, -3, 10, -3), (BlockID.moon_wood, -3, 11, -3),
            (BlockID.moon_wood, 3, 1, 0), (BlockID.moon_wood, 3, 2, 0), (BlockID.moon_wood, 3, 3, 0), (BlockID.moon_wood, 3, 4, 0),
            (BlockID.moon_wood, 3, 5, 0), (BlockID.moon_wood, 3, 6, 0), (BlockID.moon_wood, 3, 7, 0), (BlockID.moon_wood, 2, 1, 0),
            (BlockID.moon_wood, 2, 2, 0), (BlockID.moon_wood, 2, 3, 0), (BlockID.moon_wood, 2, 4, 0), (BlockID.moon_wood, 2, 5, 0),
            (BlockID.moon_wood, 2, 6, 0), (BlockID.moon_wood, 2, 7, 0), (BlockID.moon_wood, 2, 8, 0),

            (BlockID.light, -1, 9, 3), (BlockID.light, -1, 9, 4), (BlockID.light, -1, 9, 5), (BlockID.light, 0, 9, 4),
            (BlockID.light, 0, 9, 5), (BlockID.light, 0, 10, 3), (BlockID.light, 0, 10, 5), (BlockID.light, -1, 10, 3),
            (BlockID.light, -1, 10, 5), (BlockID.light, 0, 11, 4), (BlockID.light, -1, 11, 3), (BlockID.light, -1, 11, 4),
            (BlockID.light, -1, 11, 5), (BlockID.light, -2, 11, 3), (BlockID.light, -2, 11, 4), (BlockID.light, -2, 11, 5),
            (BlockID.light, -2, 10, 4), (BlockID.light, -2, 12, -3), (BlockID.light, -3, 12, -3), (BlockID.light, -1, 13, -3),
            (BlockID.light, -2, 13, -3), (BlockID.light, -3, 13, -3), (BlockID.light, -1, 12, -2), (BlockID.light, -2, 12, -2),
            (BlockID.light, -3, 12, -2), (BlockID.light, -1, 13, -2), (BlockID.light, -2, 13, -2), (BlockID.light, -3, 13, -2),
            (BlockID.light, -2, 14, -3), (BlockID.light, -2, 14, -2), (BlockID.light, -3, 14, -2), (BlockID.light, -1, 14, -1),
            (BlockID.light, -2, 14, -1), (BlockID.light, -1, 13, -1), (BlockID.light, -2, 13, -1), (BlockID.light, -3, 13, -1),
            (BlockID.light, -2, 12, -1), (BlockID.light, 2, 15, 0), (BlockID.light, 1, 15, 0), (BlockID.light, 3, 16, 0),
            (BlockID.light, 2, 16, 0), (BlockID.light, 1, 16, 0), (BlockID.light, 3, 16, 1), (BlockID.light, 2, 16, 1),
            (BlockID.light, 1, 16, 1), (BlockID.light, 3, 16, 2), (BlockID.light, 2, 16, 2), (BlockID.light, 1, 16, 2),
            (BlockID.light, 3, 17, 0), (BlockID.light, 2, 17, 0), (BlockID.light, 3, 17, 1), (BlockID.light, 2, 17, 1),
            (BlockID.light, 1, 17, 1), (BlockID.light, 2, 17, 2), (BlockID.light, 1, 17, 2), (BlockID.light, 1, 15, 1),
            (BlockID.light, 3, 15, 1), (BlockID.light, 3, 15, 2), (BlockID.light, 2, 15, 1), (BlockID.light, 2, 15, 2)
        },
        new (BlockID, int, int, int)[]
        {
            (BlockID.moon_wood, 6, 0, 0), // max extent

            (BlockID.moon_wood, 0, 1, 0), (BlockID.moon_wood, -1, 1, 0), (BlockID.moon_wood, 0, 1, 1), (BlockID.moon_wood, 1, 1, 1),
            (BlockID.moon_wood, 1, 2, 2), (BlockID.moon_wood, 1, 3, 2), (BlockID.moon_wood, 2, 3, 2), (BlockID.moon_wood, 1, 4, 3),
            (BlockID.moon_wood, 1, 5, 3), (BlockID.moon_wood, 0, 5, 3), (BlockID.moon_wood, 0, 6, 4), (BlockID.moon_wood, -1, 6, 4),
            (BlockID.moon_wood, -1, 7, 4), (BlockID.moon_wood, -2, 7, 4), (BlockID.moon_wood, -2, 8, 4), (BlockID.moon_wood, -3, 8, 4),
            (BlockID.moon_wood, -3, 9, 4), (BlockID.moon_wood, -3, 9, 3), (BlockID.moon_wood, -4, 9, 3), (BlockID.moon_wood, -4, 10, 3),
            (BlockID.moon_wood, -5, 10, 3), (BlockID.moon_wood, -5, 10, 4), (BlockID.moon_wood, -1, 2, -1), (BlockID.moon_wood, -1, 3, -1),
            (BlockID.moon_wood, 0, 3, -1), (BlockID.moon_wood, 0, 4, -1), (BlockID.moon_wood, 0, 4, 0), (BlockID.moon_wood, 0, 5, 0),
            (BlockID.moon_wood, -1, 5, 0), (BlockID.moon_wood, -1, 6, 0), (BlockID.moon_wood, -2, 6, 0), (BlockID.moon_wood, -2, 7, 0),
            (BlockID.moon_wood, -3, 7, 0), (BlockID.moon_wood, -3, 8, 0), (BlockID.moon_wood, -3, 8, 1),

            (BlockID.light, -4, 8, 0), (BlockID.light, -4, 8, 1), (BlockID.light, -4, 9, 0), (BlockID.light, -4, 9, 1),
            (BlockID.light, -5, 9, 0), (BlockID.light, -5, 9, 1), (BlockID.light, -3, 11, 3), (BlockID.light, -4, 11, 3),
            (BlockID.light, -5, 11, 3), (BlockID.light, -6, 11, 3), (BlockID.light, -3, 12, 3), (BlockID.light, -4, 12, 3),
            (BlockID.light, -5, 12, 3), (BlockID.light, -6, 12, 3), (BlockID.light, -4, 13, 3), (BlockID.light, -5, 13, 3),
            (BlockID.light, -4, 11, 2), (BlockID.light, -5, 11, 2), (BlockID.light, -4, 12, 2), (BlockID.light, -5, 12, 2),
            (BlockID.light, -4, 11, 4), (BlockID.light, -5, 11, 4), (BlockID.light, -5, 12, 4)
        },
        new (BlockID, int, int, int)[]
        {
            (BlockID.moon_wood, 4, 0, 0), // max extent

            (BlockID.moon_wood, -1, 1, 1), (BlockID.moon_wood, 0, 1, 1), (BlockID.moon_wood, -1, 1, 0), (BlockID.moon_wood, 0, 1, 0),
            (BlockID.moon_wood, 0, 2, 0), (BlockID.moon_wood, 1, 1, 0), (BlockID.moon_wood, 1, 2, 0), (BlockID.moon_wood, 1, 3, 0),
            (BlockID.moon_wood, 1, 2, -1), (BlockID.moon_wood, 1, 3, -1), (BlockID.moon_wood, 0, 1, -1), (BlockID.moon_wood, 0, 2, -1),
            (BlockID.moon_wood, 0, 3, -1), (BlockID.moon_wood, 0, 3, -2), (BlockID.moon_wood, 0, 4, -2), (BlockID.moon_wood, -1, 4, -2),
            (BlockID.moon_wood, -1, 3, -1), (BlockID.moon_wood, -1, 4, -1), (BlockID.moon_wood, -1, 5, -2), (BlockID.moon_wood, -1, 5, -3),
            (BlockID.moon_wood, -1, 6, -3), (BlockID.moon_wood, -1, 7, -3), (BlockID.moon_wood, -2, 6, -3), (BlockID.moon_wood, -2, 7, -3),
            (BlockID.moon_wood, -2, 8, -3), (BlockID.moon_wood, -2, 9, -3), (BlockID.moon_wood, -1, 7, -4), (BlockID.moon_wood, -1, 8, -4),
            (BlockID.moon_wood, -1, 9, -4),  (BlockID.moon_wood, -1, 10, -4), (BlockID.moon_wood, -1, 10, -3), (BlockID.moon_wood, -1, 11, -3),
            (BlockID.moon_wood, -1, 11, -2), (BlockID.moon_wood, -1, 12, -2), (BlockID.moon_wood, 0, 11, -3), (BlockID.moon_wood, 0, 12, -3),
            (BlockID.moon_wood, 1, 13, -2), (BlockID.moon_wood, 0, 13, -2), (BlockID.moon_wood, 0, 13, -1), (BlockID.moon_wood, 1, 12, -2),
            (BlockID.moon_wood, 0, 12, -2), (BlockID.moon_wood, 0, 12, -1), (BlockID.moon_wood, 1, 14, -1), (BlockID.moon_wood, 1, 14, 0),
            (BlockID.moon_wood, 1, 15, 0),

            (BlockID.light, 1, 13, 1), (BlockID.light, 2, 13, 1), (BlockID.light, 1, 14, 1), (BlockID.light, 2, 14, 1),
            (BlockID.light, 1, 13, 0), (BlockID.light, 1, 13, -1), (BlockID.light, 2, 12, 0), (BlockID.light, 2, 13, 0),
            (BlockID.light, 2, 14, 0), (BlockID.light, 2, 15, 0), (BlockID.light, 1, 15, -1), (BlockID.light, 2, 13, -1),
            (BlockID.light, 2, 14, -1), (BlockID.light, 3, 13, 0), (BlockID.light, 3, 14, 0)
        },
        new (BlockID, int, int, int)[]
        {
            (BlockID.moon_wood, 4, 0, 0), // max extent

            (BlockID.moon_wood, -1, 1, 1), (BlockID.moon_wood, 0, 1, 1), (BlockID.moon_wood, 0, 1, 0), (BlockID.moon_wood, 1, 1, 0),
            (BlockID.moon_wood, -1, 1, -1), (BlockID.moon_wood, 0, 2, 2), (BlockID.moon_wood, -1, 2, 2), (BlockID.moon_wood, -1, 3, 2),
            (BlockID.moon_wood, -2, 3, 2), (BlockID.moon_wood, -2, 4, 2), (BlockID.moon_wood, -2, 4, 1), (BlockID.moon_wood, -2, 5, 1),
            (BlockID.moon_wood, -3, 5, 1), (BlockID.moon_wood, -3, 6, 1), (BlockID.moon_wood, 0, 2, -1), (BlockID.moon_wood, 0, 3, -2),
            (BlockID.moon_wood, 0, 4, -2), (BlockID.moon_wood, -1, 4, -2), (BlockID.moon_wood, -1, 5, -2), (BlockID.moon_wood, -2, 5, -2),
            (BlockID.moon_wood, -2, 6, -1), (BlockID.moon_wood, -2, 7, 0), (BlockID.moon_wood, -2, 8, 0), (BlockID.moon_wood, -3, 8, 0),
            (BlockID.moon_wood, -2, 8, 1), (BlockID.moon_wood, -2, 9, 1), (BlockID.moon_wood, -1, 9, 1), (BlockID.moon_wood, -1, 10, 0),
            (BlockID.moon_wood, -2, 10, 0), (BlockID.moon_wood, -1, 11, 0), (BlockID.moon_wood, 0, 11, 0), (BlockID.moon_wood, -2, 11, -1),
            (BlockID.moon_wood, -2, 11, -2), (BlockID.moon_wood, 0, 12, 1), (BlockID.moon_wood, -1, 12, 1), (BlockID.moon_wood, -1, 12, 2),
            (BlockID.moon_wood, -1, 13, 2), (BlockID.moon_wood, -2, 13, 2), (BlockID.moon_wood, -2, 13, 1), (BlockID.moon_wood, -2, 14, 1),
            (BlockID.moon_wood, -3, 14, 1), (BlockID.moon_wood, -2, 14, 0),

            (BlockID.light, -3, 6, 0), (BlockID.light, -3, 7, 0), (BlockID.light, -4, 6, 0), (BlockID.light, -4, 7, 0),
            (BlockID.light, -3, 6, -1), (BlockID.light, -3, 7, -1), (BlockID.light, -4, 6, -1), (BlockID.light, -1, 11, -2),
            (BlockID.light, -1, 12, -2), (BlockID.light, -2, 11, -3), (BlockID.light, -2, 12, -3), (BlockID.light, -2, 12, -2),
            (BlockID.light, -1, 12, -3), (BlockID.light, -1, 15, 0), (BlockID.light, -1, 15, -1), (BlockID.light, -2, 15, 0),
            (BlockID.light, -2, 15, -1), (BlockID.light, -1, 16, 0), (BlockID.light, -1, 16, -1), (BlockID.light, -2, 16, 0),
            (BlockID.light, -2, 16, -1)
        },
        new (BlockID, int, int, int)[]
        {
            (BlockID.moon_wood, 6, 0, 0), // max extent

            (BlockID.moon_wood, 0, 1, 0), (BlockID.moon_wood, 1, 1, 0), (BlockID.moon_wood, -1, 1, 0), (BlockID.moon_wood, 0, 1, 1),
            (BlockID.moon_wood, 1, 1, 1), (BlockID.moon_wood, 1, 1, 2), (BlockID.moon_wood, 2, 1, 1), (BlockID.moon_wood, 2, 1, 2),
            (BlockID.moon_wood, 2, 2, 1), (BlockID.moon_wood, 2, 2, 2), (BlockID.moon_wood, 3, 2, 2), (BlockID.moon_wood, 3, 3, 2),
            (BlockID.moon_wood, 4, 3, 2), (BlockID.moon_wood, 4, 4, 2), (BlockID.moon_wood, 5, 5, 2), (BlockID.moon_wood, 6, 6, 2),
            (BlockID.moon_wood, 6, 6, 3), (BlockID.moon_wood, 6, 6, 4), (BlockID.moon_wood, 6, 7, 4), (BlockID.moon_wood, 5, 7, 4),
            (BlockID.moon_wood, 0, 2, -1), (BlockID.moon_wood, -1, 2, -1), (BlockID.moon_wood, -1, 3, -2), (BlockID.moon_wood, -1, 4, -2),
            (BlockID.moon_wood, -2, 3, -2), (BlockID.moon_wood, 0, 4, -2), (BlockID.moon_wood, 0, 5, -2), (BlockID.moon_wood, 1, 5, -2),
            (BlockID.moon_wood, 1, 6, -3), (BlockID.moon_wood, 2, 6, -3), (BlockID.moon_wood, 1, 7, -2), (BlockID.moon_wood, 2, 7, -2),
            (BlockID.moon_wood, 1, 8, -2), (BlockID.moon_wood, 0, 8, -2), (BlockID.moon_wood, 0, 9, -2), (BlockID.moon_wood, -1, 9, -2),
            (BlockID.moon_wood, -1, 10, -2), (BlockID.moon_wood, -1, 10, -3), (BlockID.moon_wood, -1, 11, -3), (BlockID.moon_wood, -1, 11, -4),
            (BlockID.moon_wood, 0, 11, -3), (BlockID.moon_wood, 0, 11, -4), (BlockID.moon_wood, 0, 12, -3), (BlockID.moon_wood, 0, 12, -2),
            (BlockID.moon_wood, 0, 13, -2), (BlockID.moon_wood, 1, 13, -2), (BlockID.moon_wood, 1, 14, -2), (BlockID.moon_wood, 1, 15, -2),
            (BlockID.moon_wood, 1, 16, -2), (BlockID.moon_wood, 1, 16, -3), (BlockID.moon_wood, 2, 14, -2), (BlockID.moon_wood, 2, 15, -2),
            (BlockID.moon_wood, 2, 14, -1), (BlockID.moon_wood, 1, 14, -1),

            (BlockID.light, 4, 8, 5), (BlockID.light, 5, 8, 5), (BlockID.light, 4, 8, 4), (BlockID.light, 5, 8, 4),
            (BlockID.light, 4, 9, 5), (BlockID.light, 5, 9, 5), (BlockID.light, 4, 9, 4), (BlockID.light, 5, 9, 4),
            (BlockID.light, 0, 16, -3), (BlockID.light, 0, 16, -2), (BlockID.light, 0, 17, -3), (BlockID.light, 0, 17, -2),
            (BlockID.light, -1, 17, -3), (BlockID.light, -1, 17, -2)
        }
    };

    public static void SaveChunkToFile(BlockID[,,] chunk, int moon, int chunkX, int chunkZ)
    {
        ulong chunkID = CombineChunkCoordinates(chunkX, chunkZ);
        string chunkFilePath = string.Format("{0}/moons/moon{1}/chunks/{2}.sb", Application.persistentDataPath, moon, chunkID);

        byte[] chunk1D = new byte[GameData.CHUNK_SIZE * GameData.CHUNK_SIZE * GameData.WORLD_HEIGHT_LIMIT];
        int index = 0;
        for (int x = 0; x < GameData.CHUNK_SIZE; x++)
            for (int z = 0; z < GameData.CHUNK_SIZE; z++)
                for (int y = 0; y < GameData.WORLD_HEIGHT_LIMIT; y++)
                    chunk1D[index++] = (byte)chunk[x,z,y];

        using (FileStream chunkFile = new FileStream(chunkFilePath, FileMode.Create, FileAccess.Write))
            chunkFile.Write(chunk1D, 0, chunk1D.Length);
    }

    public static bool ChunkFileExists(int moon, int chunkX, int chunkZ)
    {
        ulong chunkID = CombineChunkCoordinates(chunkX, chunkZ);
        string chunkFilePath = string.Format("{0}/moons/moon{1}/chunks/{2}.sb", Application.persistentDataPath, moon, chunkID);
        return File.Exists(chunkFilePath);
    }

    public static void GetChunkFromFile(BlockID[,,] chunk, int moon, int chunkX, int chunkZ)
    {
        ulong chunkID = CombineChunkCoordinates(chunkX, chunkZ);
        string chunkFilePath = string.Format("{0}/moons/moon{1}/chunks/{2}.sb", Application.persistentDataPath, moon, chunkID);

        byte[] chunk1D = new byte[GameData.CHUNK_SIZE * GameData.CHUNK_SIZE * GameData.WORLD_HEIGHT_LIMIT];
        
        using (FileStream chunkFile = new FileStream(chunkFilePath, FileMode.Open, FileAccess.Read))
            chunkFile.Read(chunk1D, 0, chunk1D.Length);

        int index = 0;
        for (int x = 0; x < GameData.CHUNK_SIZE; x++)
            for (int z = 0; z < GameData.CHUNK_SIZE; z++)
                for (int y = 0; y < GameData.WORLD_HEIGHT_LIMIT; y++)
                    chunk[x,z,y] = (BlockID)chunk1D[index++];
    }

    public static void GenerateChunk(BlockID[,,] chunk, int chunkX, int chunkZ, MoonData moonData)
    {
        //
        // Generate terrain
        //
        float rockHeightFrequency = 0.3F + 0.05F*moonData.terrainRoughness;
        int[] rockHeightMap   = GenerateHeightMap(0, chunkX, chunkZ, moonData.seed, 16F, 0.4F, rockHeightFrequency, 4);
        int[] gravelHeightMap = GenerateHeightMap(1, chunkX, chunkZ, moonData.seed, 4F, 0.4F, 0.6F, 2);
        int[] dirtHeightMap   = GenerateHeightMap(2, chunkX, chunkZ, moonData.seed, 3F, 0.4F, 0.4F, 3);
        int[] sandHeightMap   = GenerateHeightMap(3, chunkX, chunkZ, moonData.seed, 2F, 0.4F, 0.8F, 2);

        for (int x = 0; x < GameData.CHUNK_SIZE; x++)
        {
            for (int z = 0; z < GameData.CHUNK_SIZE; z++)
            {
                int rockHeightLimit   = rockHeightMap[z + GameData.CHUNK_SIZE*x];
                int gravelHeightLimit = gravelHeightMap[z + GameData.CHUNK_SIZE*x];
                int dirtHeightLimit   = dirtHeightMap[z + GameData.CHUNK_SIZE*x];
                int sandHeightLimit   = sandHeightMap[z + GameData.CHUNK_SIZE*x];
                int y = 0;

                // public static void Fill<T>(T[] array, T value, int startIndex, int count);

                // Base rock
                while (y < 50 + rockHeightLimit)
                {
                    chunk[x,z,y] = BlockID.rock;
                    y++;
                }

                // Base gravel
                while (y < 50 + rockHeightLimit + gravelHeightLimit)
                {
                    chunk[x,z,y] = BlockID.gravel;
                    y++;
                }

                // Base dirt
                while (y < 50 + rockHeightLimit + gravelHeightLimit + dirtHeightLimit)
                {
                    chunk[x,z,y] = BlockID.dirt;
                    y++;
                }

                if (50 + rockHeightLimit + gravelHeightLimit + dirtHeightLimit < GameData.GROUND_LEVEL) // Below ground level; fill rest in with sand/water
                {
                    while (y < GameData.GROUND_LEVEL && y < 50 + rockHeightLimit + gravelHeightLimit + dirtHeightLimit + sandHeightLimit)
                    {
                        chunk[x,z,y] = BlockID.sand;
                        y++;
                    }

                    while (y < GameData.GROUND_LEVEL)
                    {
                        chunk[x,z,y] = BlockID.water;
                        y++;
                    }
                }
                else // Above ground level; finish terrain by placing topsoil
                {
                    chunk[x,z,y] = BlockID.topsoil;
                    y++;
                }

                while (y < GameData.WORLD_HEIGHT_LIMIT)
                {
                    chunk[x,z,y] = BlockID.air;
                    y++;
                }
            }
        }

        // Ensure the same seed puts the same structures in the same places
        UnityEngine.Random.State initialRandomState = UnityEngine.Random.state;
        int structureSeed = (int)((moonData.seed >> 32) ^ (moonData.seed & 0xFFFFFFFF)) ^ chunkX ^ chunkZ;
        UnityEngine.Random.InitState(structureSeed);

        //
        // Ores
        //
        int oreSpawnChance = UnityEngine.Random.Range(0, 10);
        if (oreSpawnChance <= 3)
        {
            int seedBlockX = UnityEngine.Random.Range(5, GameData.CHUNK_SIZE - 6);
            int seedBlockZ = UnityEngine.Random.Range(5, GameData.CHUNK_SIZE - 6);
            int seedBlockY = -1;
            for (int y = 63; y < GameData.WORLD_HEIGHT_LIMIT; y++)
            {
                if (chunk[seedBlockX, seedBlockZ, y+1] == BlockID.air)
                {
                    seedBlockY = y;
                    break;
                }
            }

            int ore = UnityEngine.Random.Range(1, 101);
            int veinSize;
            BlockID oreID;
            if (ore <= 36) // 36%
            {
                oreID = BlockID.magnetite;
                veinSize = UnityEngine.Random.Range(2, 7);
            }
            else if (ore <= 60) // 24%
            {
                oreID = BlockID.aluminum_ore;
                veinSize = UnityEngine.Random.Range(2, 7);
            }
            else if (ore <= 78) // 18%
            {
                oreID = BlockID.titanium_ore;
                veinSize = UnityEngine.Random.Range(2, 7);
            }
            else if (ore <= 91) // 13%
            {
                oreID = BlockID.gold_ore;
                veinSize = UnityEngine.Random.Range(1, 5);
            }
            else if (ore <= 98) // 7%
            {
                oreID = BlockID.notchium_ore;
                veinSize = UnityEngine.Random.Range(1, 5);
            }
            else // 2%
            {
                oreID = BlockID.blue_crystal;
                veinSize = UnityEngine.Random.Range(1, 5);
            }

            int currentBlockX = seedBlockX;
            int currentBlockY = seedBlockY;
            int currentBlockZ = seedBlockZ;
            chunk[seedBlockX, seedBlockZ, seedBlockY] = oreID;
            for (int count = 0; count < veinSize; count++)
            {
                int nextDirection = UnityEngine.Random.Range(1, 6);
                if (nextDirection == 1) // Forward
                {
                    currentBlockZ++;
                }
                else if (nextDirection == 2) // Backward
                {
                    currentBlockZ--;
                }
                else if (nextDirection == 3) // Right
                {
                    currentBlockX++;
                }
                else if (nextDirection == 4) // Left
                {
                    currentBlockX--;
                }
                else // Down
                {
                    currentBlockY--;
                }

                BlockID currentBlockID = chunk[currentBlockX, currentBlockZ, currentBlockY];
                if (currentBlockID != BlockID.air)
                    chunk[currentBlockX, currentBlockZ, currentBlockY] = oreID;
            }
        }

        //
        // Astronaut lairs
        //
        int spawnAstronautLair = UnityEngine.Random.Range(0, 100); // 1% chance, each chunk
        const int lairDepth = 26;
        if (spawnAstronautLair == 69)
        {
            int centerBlockX = (int)(GameData.CHUNK_SIZE / 2);
            int centerBlockZ = (int)(GameData.CHUNK_SIZE / 2);
            int centerBlockY = -1;
            for (int y = 64; y < GameData.WORLD_HEIGHT_LIMIT; y++)
            {
                if (chunk[centerBlockX, centerBlockZ, y+1] == BlockID.air)
                {
                    centerBlockY = y;
                    break;
                }
            }

            // Carve
            for (int xOffset = -3; xOffset <= 3; xOffset++)
            {
                for (int zOffset = -3; zOffset <= 3; zOffset++)
                {
                    for (int yOffset = -6; yOffset <= lairDepth; yOffset++)
                    {
                        chunk[centerBlockX + xOffset, centerBlockZ + zOffset, centerBlockY - yOffset] = BlockID.air;
                    }
                }
            }

            // Place light block at center of bottom so I can easily check whether a chunk contains an astronaut lair for faster rendering
            chunk[centerBlockX, centerBlockZ, 0] = BlockID.gravel;

            // Decorate with polymer and light
            bool leftSideDone = false;
            bool rightSideDone = false;
            bool frontSideDone = false;
            bool backSideDone = false;
            for (int y = centerBlockY - lairDepth; y < GameData.WORLD_HEIGHT_LIMIT; y++)
            {
                if (chunk[centerBlockX - 4, centerBlockZ, y] != BlockID.air && !leftSideDone)
                {
                    chunk[centerBlockX - 4, centerBlockZ, y] = BlockID.polymer;
                    if ((y - (centerBlockY - lairDepth)) % 8 == 0)
                        chunk[centerBlockX - 4, centerBlockZ, y] = BlockID.light;
                }
                else
                {
                    leftSideDone = true;
                }
                
                if (chunk[centerBlockX + 4, centerBlockZ, y] != BlockID.air && !rightSideDone)
                {
                    chunk[centerBlockX + 4, centerBlockZ, y] = BlockID.polymer;
                    if ((y - (centerBlockY - lairDepth)) % 8 == 0)
                        chunk[centerBlockX + 4, centerBlockZ, y] = BlockID.light;
                }
                else
                {
                    rightSideDone = true;
                }

                if (chunk[centerBlockX, centerBlockZ + 4, y] != BlockID.air && !frontSideDone)
                {
                    chunk[centerBlockX, centerBlockZ + 4, y] = BlockID.polymer;
                    if ((y - (centerBlockY - lairDepth)) % 8 == 0)
                        chunk[centerBlockX, centerBlockZ + 4, y] = BlockID.light;
                }
                else
                {
                    frontSideDone = true;
                }

                if (chunk[centerBlockX, centerBlockZ - 4, y] != BlockID.air && !backSideDone)
                {
                    chunk[centerBlockX, centerBlockZ - 4, y] = BlockID.polymer;
                    if ((y - (centerBlockY - lairDepth)) % 8 == 0)
                        chunk[centerBlockX, centerBlockZ - 4, y] = BlockID.light;
                }
                else
                {
                    backSideDone = true;
                }

                if (leftSideDone && rightSideDone && frontSideDone && backSideDone)
                    break;
            }
        }

        //
        // Crystal plants
        //
        int spawnCrystalPlant = UnityEngine.Random.Range(0, 40);
        if (spawnCrystalPlant == 0)
        {
            int crystalPlantType = UnityEngine.Random.Range(0, 5);
            BlockID crystal;
            if (crystalPlantType == 0)
                crystal = BlockID.blue_crystal;
            else if (crystalPlantType < 3)
                crystal = BlockID.sulphur_crystal;
            else
                crystal = BlockID.boron_crystal;

            int crystalPlantOrientation = UnityEngine.Random.Range(1, 5);
            int crystalPlantShape = UnityEngine.Random.Range(0, CRYSTAL_PLANT_SHAPES.Length);
            var shapeOffsets = CRYSTAL_PLANT_SHAPES[crystalPlantShape];

            int paddingNeeded = shapeOffsets[0].Item1;
            int baseBlockX = UnityEngine.Random.Range(paddingNeeded, GameData.CHUNK_SIZE - paddingNeeded);
            int baseBlockZ = UnityEngine.Random.Range(paddingNeeded, GameData.CHUNK_SIZE - paddingNeeded);
            int baseBlockY;
            for (int y = 63; y < GameData.WORLD_HEIGHT_LIMIT; y++)
            {
                if (chunk[baseBlockX, baseBlockZ, y+1] == BlockID.air)
                {
                    if (chunk[baseBlockX, baseBlockZ, y] == BlockID.topsoil || chunk[baseBlockX, baseBlockZ, y] == BlockID.sand)
                    {
                        baseBlockY = y;
                        for (int i = 1; i < shapeOffsets.Length; i++)
                        {
                            (int offsetX, int offsetY, int offsetZ) = shapeOffsets[i];
                            if (crystalPlantOrientation == 2) // 90 degrees
                                (offsetX, offsetZ) = (-offsetZ, offsetX);
                            else if (crystalPlantOrientation == 3) // 180 degrees
                                (offsetX, offsetZ) = (-offsetX, -offsetZ);
                            else if (crystalPlantOrientation == 4) // 270 degrees
                                (offsetX, offsetZ) = (offsetZ, -offsetX);

                            chunk[baseBlockX + offsetX, baseBlockZ + offsetZ, baseBlockY + offsetY] = crystal;
                        }
                    }
                    break;
                }
            }
        }

        //
        // Trees
        //
        int numberOfTrees = UnityEngine.Random.Range(0, moonData.treeCover);
        for (int i = 0; i < numberOfTrees; i++)
        {
            int treeType = UnityEngine.Random.Range(0, 10);
            int treeOrientation = UnityEngine.Random.Range(1, 5);

            int treeShape;
            (BlockID, int, int, int)[] treeData;
            if (treeType < 5) // Green light tree
            {
                treeShape = UnityEngine.Random.Range(0, GREEN_LIGHT_TREE_SHAPES.Length);
                treeData = GREEN_LIGHT_TREE_SHAPES[treeShape];
            }
            else if (treeType < 8) // Color wood tree
            {
                treeShape = UnityEngine.Random.Range(0, COLOR_WOOD_TREE_SHAPES.Length);
                treeData = COLOR_WOOD_TREE_SHAPES[treeShape];
                
            }
            else // Spiral light tree
            {
                treeShape = UnityEngine.Random.Range(0, SPIRAL_LIGHT_TREE_SHAPES.Length);
                treeData = SPIRAL_LIGHT_TREE_SHAPES[treeShape];
            }

            int paddingNeeded = treeData[0].Item2;
            int baseBlockX = UnityEngine.Random.Range(paddingNeeded, GameData.CHUNK_SIZE - paddingNeeded);
            int baseBlockZ = UnityEngine.Random.Range(paddingNeeded, GameData.CHUNK_SIZE - paddingNeeded);
            int baseBlockY;
            for (int y = 63; y < GameData.WORLD_HEIGHT_LIMIT; y++)
            {
                if (chunk[baseBlockX, baseBlockZ, y+1] == BlockID.air)
                {
                    if (chunk[baseBlockX, baseBlockZ, y] == BlockID.topsoil || chunk[baseBlockX, baseBlockZ, y] == BlockID.sand)
                    {
                        baseBlockY = y;
                        //chunk[baseBlockX, baseBlockY, baseBlockZ] = BlockID.notchium;
                        for (int j = 1; j < treeData.Length; j++)
                        {
                            (BlockID treeBlock, int offsetX, int offsetY, int offsetZ) = treeData[j];
                            if (treeOrientation == 2) // 90 degrees
                                (offsetX, offsetZ) = (-offsetZ, offsetX);
                            else if (treeOrientation == 3) // 180 degrees
                                (offsetX, offsetZ) = (-offsetX, -offsetZ);
                            else if (treeOrientation == 4) // 270 degrees
                                (offsetX, offsetZ) = (offsetZ, -offsetX);
                            
                            chunk[baseBlockX + offsetX, baseBlockZ + offsetZ, baseBlockY + offsetY] = treeBlock;
                        }
                    }
                    break;
                }
            }
        }

        //
        // Mobs
        //
        if (moonData.wildlifeLevel > 0)
        {
            int mobSpawnChance = UnityEngine.Random.Range(0, 10 - 2*moonData.wildlifeLevel);
            if (mobSpawnChance == 0)
            {
                int mobCount = UnityEngine.Random.Range(1, moonData.wildlifeLevel + 1);
                MobData[] mobs = new MobData[mobCount];
                for (int i = 0; i < mobCount; i++)
                {
                    int mobType = UnityEngine.Random.Range(1, 101);
                    if (mobType <= 30)
                        mobType = 0;
                    else if (mobType <= 60)
                        mobType = 1;
                    else if (mobType <= 90)
                        mobType = 2;
                    else
                        mobType = 3;

                    if (mobType == 0 || mobType == 1) // Green/brown mobs (green ~ 0, brown ~ 1)
                    {
                        int localPosX = UnityEngine.Random.Range(2, GameData.CHUNK_SIZE - 2);
                        int localPosZ = UnityEngine.Random.Range(2, GameData.CHUNK_SIZE - 2);
                        int localPosY = 0;
                        for (int j = 63; j < GameData.WORLD_HEIGHT_LIMIT; j++)
                        {
                            if (chunk[localPosX, localPosZ, j] == BlockID.air)
                            {
                                localPosY = j;
                                break;
                            }
                        }

                        Vector3 globalPos = new Vector3(
                            chunkX*GameData.CHUNK_SIZE + localPosX,
                            localPosY,
                            chunkZ*GameData.CHUNK_SIZE + localPosZ
                        );

                        MobData mobData = new MobData();
                        mobData.mobID = mobType;
                        mobData.astronautType = AstronautType.WHITE; // Doesn't matter
                        mobData.positionX = globalPos.x;
                        mobData.positionY = globalPos.y + 1;
                        mobData.positionZ = globalPos.z;
                        mobData.rotationY = 0;
                        mobData.aggressive = false;
                        mobs[i] = mobData;
                    }
                    else if (mobType == 2) // Giraffe herd
                    {
                        int herdSize = UnityEngine.Random.Range(1, (int)Mathf.Min(5, mobCount - i));
                        int herdCenterX = UnityEngine.Random.Range(2, GameData.CHUNK_SIZE - 2);
                        int herdCenterZ = UnityEngine.Random.Range(2, GameData.CHUNK_SIZE - 2);
                        for (int j = 0; j < herdSize; j++)
                        {
                            int giraffePosX = herdCenterX + UnityEngine.Random.Range(Mathf.Max(-5, -herdCenterX), Mathf.Min(6, GameData.CHUNK_SIZE - herdCenterX));
                            int giraffePosZ = herdCenterZ + UnityEngine.Random.Range(Mathf.Max(-5, -herdCenterZ), Mathf.Min(6, GameData.CHUNK_SIZE - herdCenterZ));
                            int giraffePosY = 0;
                            for (int k = 63; k < GameData.WORLD_HEIGHT_LIMIT; k++)
                            {
                                if (chunk[giraffePosX, giraffePosZ, k] == BlockID.air)
                                {
                                    giraffePosY = k + 2;
                                    break;
                                }
                            }

                            Vector3 globalPos = new Vector3(
                                chunkX*GameData.CHUNK_SIZE + giraffePosX,
                                giraffePosY,
                                chunkZ*GameData.CHUNK_SIZE + giraffePosZ
                            );

                            MobData mobData = new MobData();
                            mobData.mobID = mobType;
                            mobData.astronautType = AstronautType.WHITE; // Doesn't matter
                            mobData.positionX = globalPos.x;
                            mobData.positionY = globalPos.y;
                            mobData.positionZ = globalPos.z;
                            mobData.rotationY = 0;
                            mobData.aggressive = false;
                            mobs[i + j] = mobData;
                        }
                        i += herdSize;
                    }
                    else // Astronaut(s)
                    {

                    }
                }
                MobHelpers.SaveMobsToChunk(mobs, moonData.moon, chunkX, chunkZ);
            }
        }

        UnityEngine.Random.state = initialRandomState; // Reset so we don't interfere with anything else (but maybe every use of Random should be based on the seed?)
    }

    private static int[] GenerateHeightMap(int heightMapIndex, int chunkX, int chunkZ, ulong seed, float amplitude, float frequency, float persistence, int octaves)
    {
        int[] heightMap = HEIGHT_MAPS[heightMapIndex];
        float frequency0 = frequency;
        float amplitude0 = amplitude;
        uint xSeed = (uint)((seed & 0b1010101010101010101010101010101010101010101010101010101010101010) >> 48);
        uint zSeed = (uint)((seed & 0b0101010101010101010101010101010101010101010101010101010101010101) >> 48);
        float heightLimit;
        for (int x = 0; x < GameData.CHUNK_SIZE; x++)
        {
            for (int z = 0; z < GameData.CHUNK_SIZE; z++)
            {
                frequency = frequency0;
                amplitude = amplitude0;
                heightLimit = 0F;
                for (int i = 0; i < octaves; i++)
                {
                    float xArg = (((x + chunkX*GameData.CHUNK_SIZE) + xSeed) / 32F) * frequency; // TODO: FIGURE OUT WHAT 16F IS (I think it's just to prevent overflows), or grid size
                    float zArg = (((z + chunkZ*GameData.CHUNK_SIZE) + zSeed) / 32F) * frequency; //
                    heightLimit += Mathf.PerlinNoise(xArg, zArg) * amplitude;
                    frequency *= 3F;
                    amplitude *= persistence;
                }
                heightMap[z + GameData.CHUNK_SIZE*x] = (int)heightLimit;
            }
        }
        return heightMap;
    }

    public static (int, int, int) GetLocalBlockPos(int globalBlockPosX, int globalBlockPosY, int globalBlockPosZ)
    {
        int chunkX = Mathf.Abs(Mathf.FloorToInt((float)globalBlockPosX / GameData.CHUNK_SIZE));
        int chunkZ = Mathf.Abs(Mathf.FloorToInt((float)globalBlockPosZ / GameData.CHUNK_SIZE));
        int localBlockPosX;
        int localBlockPosZ;

        if (globalBlockPosX >= 0 || ((-globalBlockPosX) % GameData.CHUNK_SIZE == 0))
            localBlockPosX = globalBlockPosX % GameData.CHUNK_SIZE;
        else
            localBlockPosX = globalBlockPosX + chunkX*GameData.CHUNK_SIZE;

        if (globalBlockPosZ >= 0 || ((-globalBlockPosZ) % GameData.CHUNK_SIZE == 0))
            localBlockPosZ = globalBlockPosZ % GameData.CHUNK_SIZE;
        else
            localBlockPosZ = globalBlockPosZ + chunkZ*GameData.CHUNK_SIZE;

        return (localBlockPosX, globalBlockPosY, localBlockPosZ);
    }

    public static (int, int, int) GetLocalBlockPos(Vector3 globalBlockPos)
    {
        int chunkX = Mathf.Abs(Mathf.FloorToInt(globalBlockPos.x / GameData.CHUNK_SIZE));
        int chunkZ = Mathf.Abs(Mathf.FloorToInt(globalBlockPos.z / GameData.CHUNK_SIZE));
        int globalBlockPosX = (int)globalBlockPos.x;
        int globalBlockPosZ = (int)globalBlockPos.z;
        int localBlockPosX;
        int localBlockPosZ;

        if (globalBlockPosX >= 0 || ((-globalBlockPosX) % GameData.CHUNK_SIZE == 0))
            localBlockPosX = globalBlockPosX % GameData.CHUNK_SIZE;
        else
            localBlockPosX = globalBlockPosX + chunkX*GameData.CHUNK_SIZE;

        if (globalBlockPosZ >= 0 || ((-globalBlockPosZ) % GameData.CHUNK_SIZE == 0))
            localBlockPosZ = globalBlockPosZ % GameData.CHUNK_SIZE;
        else
            localBlockPosZ = globalBlockPosZ + chunkZ*GameData.CHUNK_SIZE;

        return (localBlockPosX, (int)globalBlockPos.y, localBlockPosZ);
    }
    
    public static (int, int) GetChunkCoordsByBlockPos(Vector3 globalBlockPos)
    {
        int globalChunkX = Mathf.FloorToInt(globalBlockPos.x / GameData.CHUNK_SIZE);
        int globalChunkZ = Mathf.FloorToInt(globalBlockPos.z / GameData.CHUNK_SIZE);
        return (globalChunkX, globalChunkZ);
    }

    private static ulong CombineChunkCoordinates(int chunkX, int chunkZ)
    {
        ulong encoding = (ulong)((uint)chunkX);
        encoding <<= 32;
        encoding |= (ulong)((uint)chunkZ);
        return encoding;
    }

    // This function assumes the neighboring chunks exist and have their data loaded.
    // This should be guaranteed by the generation code.
    public static ChunkData[] GetAdjacentChunkData(GameObject chunkParent, int globalChunkX, int globalChunkZ)
    {
        ChunkData[] adjacentChunkData = new ChunkData[4];
        adjacentChunkData[0] = chunkParent.transform.Find($"Chunk ({globalChunkX-1},{globalChunkZ})").GetComponent<ChunkData>(); // Left ~ 0
        adjacentChunkData[1] = chunkParent.transform.Find($"Chunk ({globalChunkX+1},{globalChunkZ})").GetComponent<ChunkData>(); // Right ~ 1
        adjacentChunkData[2] = chunkParent.transform.Find($"Chunk ({globalChunkX},{globalChunkZ+1})").GetComponent<ChunkData>(); // Front ~ 2
        adjacentChunkData[3] = chunkParent.transform.Find($"Chunk ({globalChunkX},{globalChunkZ-1})").GetComponent<ChunkData>(); // Back ~ 3

        return adjacentChunkData;
    }

    public static bool BlockShouldBeRenderedINNER(BlockID block, ChunkData chunkData, int localBlockX, int localBlockY, int localBlockZ)
    {
        bool frontTest = false;
        bool leftTest = false;
        bool rightTest = false;
        bool backTest = false;
        bool topTest = false;
        bool bottomTest = false;

        int idCheck = (block == BlockID.water) ? 37 : 36;

        // Top check
        if (localBlockY < GameData.WORLD_HEIGHT_LIMIT - 1 && (int)chunkData.blocks[localBlockX, localBlockZ, localBlockY + 1] > idCheck)
            return true;

        // Bottom check
        if (localBlockY > 0 && (int)chunkData.blocks[localBlockX, localBlockZ, localBlockY - 1] > idCheck)
            return true;

        // Front and back checks
        backTest = (int)chunkData.blocks[localBlockX, localBlockZ - 1, localBlockY] > idCheck;
        frontTest = (int)chunkData.blocks[localBlockX, localBlockZ + 1, localBlockY] > idCheck;

        if (frontTest || backTest)
            return true;

        // Left and right checks
        leftTest = (int)chunkData.blocks[localBlockX - 1, localBlockZ, localBlockY] > idCheck;
        rightTest = (int)chunkData.blocks[localBlockX + 1, localBlockZ, localBlockY] > idCheck;

        return leftTest || rightTest;
    }

    public static bool BlockShouldBeRendered(BlockID block, ChunkData[] adjacentChunkData, ChunkData chunkData, int localBlockX, int localBlockY, int localBlockZ)
    {
        bool frontTest = false;
        bool leftTest = false;
        bool rightTest = false;
        bool backTest = false;
        bool topTest = false;
        bool bottomTest = false;

        int idCheck = (block == BlockID.water) ? 37 : 36;

        // Top and bottom checks (BlockID.rock is just a lazy way of not rendering invalid y positions)
        // BlockID topBlock = (localBlockY < GameData.WORLD_HEIGHT_LIMIT - 1) ? chunkData.blocks[localBlockX, localBlockY + 1, localBlockZ] : BlockID.rock;
        // topTest = (int)topBlock > 36;
        //topTest = (localBlockY < GameData.WORLD_HEIGHT_LIMIT - 1) ? (int)chunkData.blocks[localBlockX, localBlockY + 1, localBlockZ] > 36 : false;
        if (localBlockY < GameData.WORLD_HEIGHT_LIMIT - 1 && (int)chunkData.blocks[localBlockX, localBlockZ, localBlockY + 1] > idCheck)
            return true;

        // BlockID bottomBlock = (localBlockY > 0) ? chunkData.blocks[localBlockX, localBlockY - 1, localBlockZ] : BlockID.rock;
        // bottomTest = (int)bottomBlock > 36;
        if (localBlockY > 0 && (int)chunkData.blocks[localBlockX, localBlockZ, localBlockY - 1] > idCheck)
            return true;

        // if (topTest || bottomTest)
        //     return true;

        // Front and back checks
        BlockID backBlock = (localBlockZ == 0) ? adjacentChunkData[3].blocks[localBlockX, GameData.CHUNK_SIZE - 1, localBlockY] : chunkData.blocks[localBlockX, localBlockZ - 1, localBlockY];
        backTest = (int)backBlock > idCheck;

        BlockID frontBlock = (localBlockZ == GameData.CHUNK_SIZE - 1) ? adjacentChunkData[2].blocks[localBlockX, 0, localBlockY] : chunkData.blocks[localBlockX, localBlockZ + 1, localBlockY];
        frontTest = (int)frontBlock > idCheck;

        if (frontTest || backTest)
            return true;

        // Left and right checks
        BlockID leftBlock = (localBlockX == 0) ? adjacentChunkData[0].blocks[GameData.CHUNK_SIZE - 1, localBlockZ, localBlockY] : chunkData.blocks[localBlockX - 1, localBlockZ, localBlockY];
        leftTest = (int)leftBlock > idCheck;

        BlockID rightBlock = (localBlockX == GameData.CHUNK_SIZE - 1) ? adjacentChunkData[1].blocks[0, localBlockZ, localBlockY] : chunkData.blocks[localBlockX + 1, localBlockZ, localBlockY];
        rightTest = (int)rightBlock > idCheck;

        return leftTest || rightTest;
    }
}
