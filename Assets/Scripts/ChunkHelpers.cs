using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ChunkHelpers
{
    private static readonly int STRUCTURE_PADDING = 11; // How many blocks into the chunk structures can be spawned (keeping structures completely inside a chunk is simplest)
    private static readonly (int, int, int)[][] CRYSTAL_PLANT_SHAPES = {
        new (int, int, int)[]
        {
            (0, 1, 0), (0, 2, 0), (0, 3, 0), (-1, 2, 0), (1, 2, 0), (0, 2, -1),
            (0, 3, 1), (0, 3, 2), (0, 2, 2), (0, 4, 2), (-1, 3, 2)
        },
        new (int, int, int)[]
        {
            (0, 1, 0), (0, 2, 0), (0, 3, 0), (0, 3, 1), (0, 3, 2), (0, 3, 3),
            (0, 2, 3), (0, 4, 3), (1, 3, 2), (2, 3, 2)
        },
        new (int, int, int)[]
        {
            (0, 1, 0), (0, 2, 0), (0, 3, 0), (1, 3, 0), (2, 3, 0), (0, 2, -1),
            (0, 2, -2), (0, 2, 1), (0, 2, 2), (0, 1, 2), (0, 3, 2), (-1, 2, 2), (-2, 2, 2)
        },
        new (int, int, int)[]
        {
            (0, 1, 0), (0, 2, 0), (0, 3, 0), (0, 4, 0), (-1, 4, 0), (-2, 4, 0),
            (-3, 4, 0), (1, 4, 0), (2, 4, 0), (3, 4, 0), (0, 4, 1), (0, 4, 2), (0, 4, 3)
        },
        new (int, int, int)[]
        {
            (0, 1, 0), (0, 2, 0), (0, 3, 0), (0, 4, 0), (-1, 4, 0), (-2, 4, 0),
            (-3, 4, 0), (-3, 5, 0), (-3, 6, 0), (-3, 4, 1), (-3, 4, 2), (1, 4, 0), (2, 4, 0),
            (2, 4, 1), (2, 4, 2), (2, 4, -1), (2, 4, -2), (2, 5, 0), (2, 6, 0), (2, 3, 0), (2, 2, 0)
        },
        new (int, int, int)[]
        {
            (0, 1, 0), (0, 2, 0), (-1, 2, 0), (-2, 2, 0), (-2, 1, 0), (-2, 2, 1),
            (-2, 2, -1), (1, 2, 0), (2, 2, 0), (2, 2, 1), (2, 2, -1), (2, 3, 0),
            (2, 1, 0), (0, 2, 1), (0, 2, 2), (0, 2, 3), (1, 2, 3), (0, 1, 3), (0, 3, 3)
        },
        new (int, int, int)[]
        {
            (0, 1, 0), (0, 2, 0), (0, 3, 0), (-1, 3, 0), (-2, 3, 0), (-3, 3, 0),
            (-3, 4, 0), (-3, 5, 0), (-3, 2, 0), (-3, 1, 0), (1, 3, 0), (2, 3, 0),
            (3, 3, 0), (3, 3, 1), (3, 3, 2), (3, 3, -1), (3, 3, -2), (0, 3, 1),
            (0, 3, 2), (0, 3, 3), (-1, 3, 3), (-2, 3, 3), (1, 3, 3), (2, 3, 3), (0, 4, 3),
            (0, 5, 3), (0, 2, 3), (0, 1, 3)
        },
        new (int, int, int)[]
        {
            (0, 1, 0), (0, 2, 0), (0, 3, 0), (-1, 3, 0), (-2, 3, 0), (-2, 4, 0),
            (-2, 5, 0), (-2, 2, 0), (-2, 1, 0), (1, 3, 0), (1, 4, 0), (2, 3, 0),
            (2, 2, 0), (0, 3, 1), (0, 3, 2), (0, 3, 3), (1, 3, 3), (0, 4, 3),
            (-1, 3, 3), (-2, 3, 3)
        }
    };

    private static readonly (BlockID, int, int, int)[][] GREEN_LIGHT_TREE_SHAPES = {
        new (BlockID, int, int, int)[]
        {
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

            (BlockID.light, 0, 5, -11), (BlockID.light, 0, 6, -11), (BlockID.light, 0, 5, -10), (BlockID.light, 0, 6, -10),
            (BlockID.light, -1, 5, -11), (BlockID.light, -1, 6, -11), (BlockID.light, -1, 6, -10), (BlockID.light, -5, 5, 3),
            (BlockID.light, -5, 5, 2), (BlockID.light, -6, 5, 3), (BlockID.light, -6, 5, 2), (BlockID.light, -5, 6, 3),
            (BlockID.light, -5, 6, 2), (BlockID.light, -6, 6, 3), (BlockID.light, -6, 6, 2), (BlockID.light, 8, 4, 0),
            (BlockID.light, 8, 4, 1), (BlockID.light, 7, 4, 1), (BlockID.light, 7, 5, 1), (BlockID.light, 7, 5, 0),
            (BlockID.light, 8, 5, 0)
        },
        new (BlockID, int, int, int)[]
        {
            (BlockID.moon_leaf, 11, 0, 0), // max extent

            // TODO: x and z can be shifted to lower the max extent to 9
            // x: -5 to 11  ->  -8 to 8
            // z: -10 to 7  ->  -9 to 8

            (BlockID.moon_leaf, 1, 1, 0), (BlockID.moon_leaf, 1, 2, 0), (BlockID.moon_leaf, 1, 3, 0), (BlockID.moon_leaf, 1, 4, 0),
            (BlockID.moon_leaf, 0, 1, -1), (BlockID.moon_leaf, 1, 1, -2), (BlockID.moon_leaf, 2, 1, -2), (BlockID.moon_leaf, 2, 1, 1),
            (BlockID.moon_leaf, 3, 1, 0), (BlockID.moon_leaf, 3, 1, -1), (BlockID.moon_leaf, 2, 1, 0), (BlockID.moon_leaf, 2, 2, 0),
            (BlockID.moon_leaf, 1, 1, -1), (BlockID.moon_leaf, 1, 2, -1), (BlockID.moon_leaf, 1, 3, -1), (BlockID.moon_leaf, 1, 4, -1),
            (BlockID.moon_leaf, 1, 5, -1), (BlockID.moon_leaf, 2, 4, -3), (BlockID.moon_leaf, 1, 4, -3), (BlockID.moon_leaf, 1, 4, -2),
            (BlockID.moon_leaf, 2, 4, -2), (BlockID.moon_leaf, 2, 1, -1), (BlockID.moon_leaf, 2, 2, -1), (BlockID.moon_leaf, 2, 3, -1),
            (BlockID.moon_leaf, 2, 4, 0), (BlockID.moon_leaf, 3, 3, -1), (BlockID.moon_leaf, 3, 4, -1), (BlockID.moon_leaf, 3, 5, -1),
            (BlockID.moon_leaf, 3, 5, -2), (BlockID.moon_leaf, 3, 6, -2), (BlockID.moon_leaf, 1, 5, -3), (BlockID.moon_leaf, 4, 5, -1),
            (BlockID.moon_leaf, 4, 6, -1), (BlockID.moon_leaf, 4, 7, -1), (BlockID.moon_leaf, 4, 6, -2), (BlockID.moon_leaf, 4, 7, -2),
            (BlockID.moon_leaf, 5, 7, -1), (BlockID.moon_leaf, 5, 8, -1), (BlockID.moon_leaf, 5, 8, -2), (BlockID.moon_leaf, 5, 9, -2),
            (BlockID.moon_leaf, 6, 8, -2), (BlockID.moon_leaf, 6, 9, -2), (BlockID.moon_leaf, 7, 9, -2), (BlockID.moon_leaf, 8, 9, -2),
            (BlockID.moon_leaf, 9, 9, -2), (BlockID.moon_leaf, 9, 8, -2), (BlockID.moon_leaf, 10, 8, -2), (BlockID.moon_leaf, 10, 7, -2),
            (BlockID.moon_leaf, 11, 7, -2), (BlockID.moon_leaf, 10, 7, -1), (BlockID.moon_leaf, 2, 4, 1), (BlockID.moon_leaf, 2, 5, 1),
            (BlockID.moon_leaf, 2, 6, 1), (BlockID.moon_leaf, 2, 7, 1), (BlockID.moon_leaf, 1, 5, 1), (BlockID.moon_leaf, 1, 6, 1),
            (BlockID.moon_leaf, 1, 7, 1), (BlockID.moon_leaf, 1, 7, 2), (BlockID.moon_leaf, 1, 8, 2), (BlockID.moon_leaf, 2, 8, 2),
            (BlockID.moon_leaf, 2, 8, 3), (BlockID.moon_leaf, 2, 8, 4), (BlockID.moon_leaf, 2, 8, 5), (BlockID.moon_leaf, 2, 7, 5),
            (BlockID.moon_leaf, 3, 7, 5), (BlockID.moon_leaf, 2, 7, 6), (BlockID.moon_leaf, 2, 6, 6), (BlockID.moon_leaf, 3, 6, 6),
            (BlockID.moon_leaf, 0, 5, -2), (BlockID.moon_leaf, 0, 6, -2), (BlockID.moon_leaf, 0, 6, -1), (BlockID.moon_leaf, -1, 7, -2),
            (BlockID.moon_leaf, -1, 8, -2), (BlockID.moon_leaf, -1, 7, -1), (BlockID.moon_leaf, -2, 8, -1), (BlockID.moon_leaf, -2, 9, -1),
            (BlockID.moon_leaf, -2, 9, -2), (BlockID.moon_leaf, -3, 9, -1), (BlockID.moon_leaf, -4, 9, -1), (BlockID.moon_leaf, 2, 5, -4),
            (BlockID.moon_leaf, 2, 5, -4), (BlockID.moon_leaf, 1, 5, -4), (BlockID.moon_leaf, 2, 6, -4), (BlockID.moon_leaf, 1, 6, -4),
            (BlockID.moon_leaf, 1, 6, -5), (BlockID.moon_leaf, 1, 7, -5), (BlockID.moon_leaf, 2, 7, -5), (BlockID.moon_leaf, 2, 8, -6),
            (BlockID.moon_leaf, 2, 8, -7), (BlockID.moon_leaf, 1, 8, -6), (BlockID.moon_leaf, 1, 8, -7), (BlockID.moon_leaf, 1, 8, -8),
            (BlockID.moon_leaf, 2, 7, -8), (BlockID.moon_leaf, 2, 6, -9), (BlockID.moon_leaf, 2, 7, -9), (BlockID.moon_leaf, 1, 7, -9),
            (BlockID.moon_leaf, 2, 6, -10), (BlockID.moon_leaf, 1, 6, -10),

            (BlockID.light, -4, 7, -1), (BlockID.light, -5, 7, -1), (BlockID.light, -4, 7, 0), (BlockID.light, -5, 7, 0),
            (BlockID.light, -4, 8, -1), (BlockID.light, -5, 8, -1), (BlockID.light, -4, 8, 0), (BlockID.light, -5, 8, 0),
            (BlockID.light, 2, 4, -10), (BlockID.light, 1, 4, -10), (BlockID.light, 2, 4, -9), (BlockID.light, 1, 4, -9),
            (BlockID.light, 2, 5, -10), (BlockID.light, 1, 5, -10), (BlockID.light, 2, 5, -9), (BlockID.light, 1, 5, -9),
            (BlockID.light, 10, 5, -1), (BlockID.light, 10, 5, -2), (BlockID.light, 11, 5, -2), (BlockID.light, 10, 6, -1),
            (BlockID.light, 11, 6, -1), (BlockID.light, 10, 6, -2), (BlockID.light, 11, 6, -2), (BlockID.light, 3, 4, 7),
            (BlockID.light, 2, 4, 7), (BlockID.light, 3, 4, 6), (BlockID.light, 2, 4, 6), (BlockID.light, 2, 5, 7),
            (BlockID.light, 3, 5, 6), (BlockID.light, 2, 5, 6)
        },
        new (BlockID, int, int, int)[]
        {
            (BlockID.moon_leaf, 9, 0, 0), // max extent

            // x: -7 to 9  ->  -8 to 8
            // z: -7 to 8
            // TODO: Shift x left to lower max extent by 1

            (BlockID.moon_leaf, 1, 2, 0), (BlockID.moon_leaf, 1, 3, 0), (BlockID.moon_leaf, 1, 4, 0), (BlockID.moon_leaf, 0, 2, 0),
            (BlockID.moon_leaf, 0, 3, 0), (BlockID.moon_leaf, 0, 4, 0), (BlockID.moon_leaf, 0, 1, -1), (BlockID.moon_leaf, 0, 2, -1),
            (BlockID.moon_leaf, 0, 3, -1), (BlockID.moon_leaf, 0, 4, -1), (BlockID.moon_leaf, 2, 3, 0), (BlockID.moon_leaf, 2, 4, 0),
            (BlockID.moon_leaf, 2, 3, -1), (BlockID.moon_leaf, 2, 4, -1), (BlockID.moon_leaf, 1, 1, -1), (BlockID.moon_leaf, 1, 2, -1),
            (BlockID.moon_leaf, 1, 3, -1), (BlockID.moon_leaf, 1, 2, -2), (BlockID.moon_leaf, 1, 3, -2), (BlockID.moon_leaf, 1, 4, -2),
            (BlockID.moon_leaf, 0, 2, -2), (BlockID.moon_leaf, 0, 3, -2), (BlockID.moon_leaf, 0, 4, -2), (BlockID.moon_leaf, 0, 5, -2),
            (BlockID.moon_leaf, 1, 4, -3), (BlockID.moon_leaf, 1, 5, -3), (BlockID.moon_leaf, 0, 3, -3), (BlockID.moon_leaf, 0, 4, -3),
            (BlockID.moon_leaf, 0, 5, -3), (BlockID.moon_leaf, 0, 6, -3), (BlockID.moon_leaf, 1, 5, -4), (BlockID.moon_leaf, 0, 5, -4),
            (BlockID.moon_leaf, 1, 6, -4), (BlockID.moon_leaf, 0, 6, -4), (BlockID.moon_leaf, 1, 5, -5), (BlockID.moon_leaf, 0, 5, -5),
            (BlockID.moon_leaf, 0, 4, -5), (BlockID.moon_leaf, 0, 3, -6), (BlockID.moon_leaf, 0, 4, -6), (BlockID.moon_leaf, -1, 4, 0),
            (BlockID.moon_leaf, -1, 4, 1), (BlockID.moon_leaf, -1, 5, 0), (BlockID.moon_leaf, -1, 5, 1), (BlockID.moon_leaf, -2, 5, 1),
            (BlockID.moon_leaf, -2, 6, 1), (BlockID.moon_leaf, -3, 6, 1), (BlockID.moon_leaf, -4, 6, 1), (BlockID.moon_leaf, -3, 6, 2),
            (BlockID.moon_leaf, -4, 6, 2), (BlockID.moon_leaf, -4, 5, 2), (BlockID.moon_leaf, -5, 5, 2), (BlockID.moon_leaf, -5, 5, 1),
            (BlockID.moon_leaf, -5, 4, 2), (BlockID.moon_leaf, -6, 4, 2), (BlockID.moon_leaf, -6, 3, 2), (BlockID.moon_leaf, -6, 2, 2),
            (BlockID.moon_leaf, -6, 1, 2), (BlockID.moon_leaf, -7, 1, 2), (BlockID.moon_leaf, -7, 2, 2), (BlockID.moon_leaf, -7, 1, 3),
            (BlockID.moon_leaf, 1, 3, 1), (BlockID.moon_leaf, 1, 4, 1), (BlockID.moon_leaf, 1, 5, 1), (BlockID.moon_leaf, 0, 4, 1),
            (BlockID.moon_leaf, 0, 5, 1), (BlockID.moon_leaf, 0, 6, 2), (BlockID.moon_leaf, 0, 6, 3), (BlockID.moon_leaf, 0, 6, 4),
            (BlockID.moon_leaf, 1, 6, 2), (BlockID.moon_leaf, 1, 6, 3), (BlockID.moon_leaf, 1, 7, 3), (BlockID.moon_leaf, 0, 7, 3),
            (BlockID.moon_leaf, 0, 7, 4), (BlockID.moon_leaf, 0, 7, 5), (BlockID.moon_leaf, 0, 8, 5), (BlockID.moon_leaf, 0, 8, 6),
            (BlockID.moon_leaf, 0, 8, 7), (BlockID.moon_leaf, 0, 7, 7), (BlockID.moon_leaf, 1, 7, 7), (BlockID.moon_leaf, 3, 4, 0),
            (BlockID.moon_leaf, 3, 4, -1), (BlockID.moon_leaf, 3, 5, 0), (BlockID.moon_leaf, 3, 5, -1), (BlockID.moon_leaf, 3, 6, 0),
            (BlockID.moon_leaf, 3, 6, -1), (BlockID.moon_leaf, 4, 6, -1), (BlockID.moon_leaf, 4, 7, -1), (BlockID.moon_leaf, 4, 7, 0),
            (BlockID.moon_leaf, 5, 7, 0), (BlockID.moon_leaf, 5, 7, -1), (BlockID.moon_leaf, 6, 7, -1), (BlockID.moon_leaf, 5, 8, -1),
            (BlockID.moon_leaf, 6, 8, -1), (BlockID.moon_leaf, 7, 8, -1), (BlockID.moon_leaf, 7, 9, -1), (BlockID.moon_leaf, 8, 8, -1),
            (BlockID.moon_leaf, 8, 8, -2),

            (BlockID.light, 1, 5, 8), (BlockID.light, 0, 5, 8), (BlockID.light, 1, 5, 7), (BlockID.light, 0, 5, 7),
            (BlockID.light, 0, 6, 8), (BlockID.light, 1, 6, 7), (BlockID.light, 0, 6, 7), (BlockID.light, 9, 6, -3),
            (BlockID.light, 8, 6, -3), (BlockID.light, 9, 6, -2), (BlockID.light, 8, 6, -2), (BlockID.light, 9, 7, -3),
            (BlockID.light, 8, 7, -3), (BlockID.light, 9, 7, -2), (BlockID.light, 8, 7, -2), (BlockID.light, -1, 1, -7),
            (BlockID.light, 0, 1, -7), (BlockID.light, -1, 1, -6), (BlockID.light, 0, 1, -6), (BlockID.light, 0, 2, -7),
            (BlockID.light, -1, 2, -6), (BlockID.light, 0, 2, -6)
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
        }
    };

    public static void SaveChunkToFile(BlockID[,,] chunk, int moon, int chunkX, int chunkZ)
    {
        ulong chunkID = CombineChunkCoordinates(chunkX, chunkZ);
        string chunkFilePath = string.Format("{0}/moons/moon{1}/chunks/{2}.sb", Application.persistentDataPath, moon, chunkID);

        byte[] chunk1D = new byte[GameData.CHUNK_SIZE * GameData.CHUNK_SIZE * GameData.WORLD_HEIGHT_LIMIT];
        int index = 0;
        for (int x = 0; x < GameData.CHUNK_SIZE; x++)
            for (int y = 0; y < GameData.WORLD_HEIGHT_LIMIT; y++)
                for (int z = 0; z < GameData.CHUNK_SIZE; z++)
                    chunk1D[index++] = (byte)chunk[x,y,z];

        using (FileStream chunkFile = new FileStream(chunkFilePath, FileMode.Create, FileAccess.Write))
        using (BufferedStream bufferedStream = new BufferedStream(chunkFile))
        {
            bufferedStream.Write(chunk1D, 0, chunk1D.Length);
        }
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
        using (BufferedStream bufferedStream = new BufferedStream(chunkFile))
            bufferedStream.Read(chunk1D, 0, chunk1D.Length);

        int index = 0;
        for (int x = 0; x < GameData.CHUNK_SIZE; x++)
        {
            for (int y = 0; y < GameData.WORLD_HEIGHT_LIMIT; y++)
            {
                for (int z = 0; z < GameData.CHUNK_SIZE; z++)
                {
                    chunk[x,y,z] = (BlockID)chunk1D[index++];
                }
            }
        }
    }

    public static void GenerateChunk(BlockID[,,] chunk, int chunkX, int chunkZ, ulong seed)
    {
        //
        // Generate terrain
        //
        int[,] rockHeightMap   = GenerateHeightMap(chunkX, chunkZ, seed, 16F, 0.4F, 0.4F, 4);
        int[,] gravelHeightMap = GenerateHeightMap(chunkX, chunkZ, seed, 4F, 0.4F, 0.6F, 2);
        int[,] dirtHeightMap   = GenerateHeightMap(chunkX, chunkZ, seed, 3F, 0.4F, 0.4F, 2);
        int[,] sandHeightMap   = GenerateHeightMap(chunkX, chunkZ, seed, 2F, 0.4F, 0.8F, 2);

        List<Vector3> structureCandidates = new List<Vector3>(30);
        int structureCandidateCount = 0;

        for (int x = 0; x < GameData.CHUNK_SIZE; x++)
        {
            for (int z = 0; z < GameData.CHUNK_SIZE; z++)
            {
                int rockHeightLimit   = rockHeightMap[x,z];
                int gravelHeightLimit = gravelHeightMap[x,z];
                int dirtHeightLimit   = dirtHeightMap[x,z];
                int sandHeightLimit   = sandHeightMap[x,z];
                int y = 0;

                // Base rock
                while (y < 50 + rockHeightLimit)
                {
                    chunk[x,y,z] = BlockID.rock;
                    y++;
                }

                // Base gravel
                while (y < 50 + rockHeightLimit + gravelHeightLimit)
                {
                    chunk[x,y,z] = BlockID.gravel;
                    y++;
                }

                // Base dirt
                while (y < 50 + rockHeightLimit + gravelHeightLimit + dirtHeightLimit)
                {
                    chunk[x,y,z] = BlockID.dirt;
                    y++;
                }

                if (50 + rockHeightLimit + gravelHeightLimit + dirtHeightLimit < GameData.GROUND_LEVEL) // Below ground level; fill rest in with sand/water
                {
                    while (y < GameData.GROUND_LEVEL && y < 50 + rockHeightLimit + gravelHeightLimit + dirtHeightLimit + sandHeightLimit)
                    {
                        chunk[x,y,z] = BlockID.sand;
                        if (y == GameData.GROUND_LEVEL - 1 && x > STRUCTURE_PADDING && z > STRUCTURE_PADDING && x < GameData.CHUNK_SIZE - STRUCTURE_PADDING && z < GameData.CHUNK_SIZE - STRUCTURE_PADDING)
                            structureCandidates.Add(new Vector3(x, y, z));
                        y++;
                    }

                    while (y < GameData.GROUND_LEVEL)
                    {
                        chunk[x,y,z] = BlockID.water;
                        y++;
                    }
                }
                else // Above ground level; finish terrain by placing topsoil
                {
                    chunk[x,y,z] = BlockID.topsoil;
                    if (x > STRUCTURE_PADDING && z > STRUCTURE_PADDING && x < GameData.CHUNK_SIZE - STRUCTURE_PADDING && z < GameData.CHUNK_SIZE - STRUCTURE_PADDING)
                        structureCandidates.Add(new Vector3(x, y, z));
                    y++;
                }

                while (y < GameData.WORLD_HEIGHT_LIMIT)
                {
                    chunk[x,y,z] = BlockID.air;
                    y++;
                }
            }
        }

        // Ensure the same seed puts the same structures in the same places
        UnityEngine.Random.State initialRandomState = UnityEngine.Random.state;
        int structureSeed = (int)((seed >> 32) ^ (seed & 0xFFFFFFFF)) ^ chunkX ^ chunkZ;
        UnityEngine.Random.InitState(structureSeed);

        //
        // Ores
        //
        int numberOfVeins = UnityEngine.Random.Range(0, 10);
        if (numberOfVeins < 7) // 70% chance to spawn 1 ore vein
            numberOfVeins = 1;
        else if (numberOfVeins < 9) // 20% chance to spawn 2 ore veins
            numberOfVeins = 2;
        else // 10% chance to spawn 3 ore veins
            numberOfVeins = 3;

        if (structureCandidates.Count > 0)
        {
            for (int i = 0; i < numberOfVeins; i++)
            {
                
                int seedBlockIndex = UnityEngine.Random.Range(0, structureCandidates.Count);
                Vector3 seedBlock = structureCandidates[seedBlockIndex];
                //structureCandidates.RemoveAt(seedBlockIndex);

                int ore = UnityEngine.Random.Range(1, 101);
                BlockID oreID;
                if (ore <= 33) // 33%
                    oreID = BlockID.magnetite;
                else if (ore <= 58) // 25%
                    oreID = BlockID.aluminum_ore;
                else if (ore <= 75) // 17%
                    oreID = BlockID.titanium_ore;
                else if (ore <= 88) // 13%
                    oreID = BlockID.gold_ore;
                else if (ore <= 96) // 8%
                    oreID = BlockID.notchium_ore;
                else // 4%
                    oreID = BlockID.blue_crystal;

                int veinSize;
                if (oreID == BlockID.magnetite || oreID == BlockID.aluminum_ore || oreID == BlockID.titanium_ore)
                    veinSize = UnityEngine.Random.Range(2, 7); // 2-6 magnetite/aluminum/titanium
                else
                    veinSize = UnityEngine.Random.Range(1, 5); // 1-4 notchium/gold ore
                Vector3 currentBlock = seedBlock;
                chunk[(int)seedBlock.x, (int)seedBlock.y, (int)seedBlock.z] = oreID;
                for (int count = 0; count < veinSize; count++)
                {
                    int nextDirection = UnityEngine.Random.Range(1, 6);
                    if (nextDirection == 1) // Forward
                    {
                        currentBlock += new Vector3(0, 0, 1);
                    }
                    else if (nextDirection == 2) // Backward
                    {
                        currentBlock += new Vector3(0, 0, -1);
                    }
                    else if (nextDirection == 3) // Right
                    {
                        currentBlock += new Vector3(1, 0, 0);
                    }
                    else if (nextDirection == 4) // Left
                    {
                        currentBlock += new Vector3(-1, 0, 0);
                    }
                    else // Down
                    {
                        currentBlock += new Vector3(0, -1, 0);
                    }

                    BlockID currentBlockID = chunk[(int)currentBlock.x, (int)currentBlock.y, (int)currentBlock.z];
                    if (currentBlockID == BlockID.topsoil || currentBlockID == BlockID.sand) // Only overwrite topsoil or sand
                        chunk[(int)currentBlock.x, (int)currentBlock.y, (int)currentBlock.z] = oreID;
                }
            }
        }

        //
        // Astronaut lairs
        //
        int spawnAstronautLair = UnityEngine.Random.Range(0, 100); // 1% chance, each chunk
        const int lairDepth = 26;
        if (spawnAstronautLair == 0 && structureCandidates.Count > 0)
        {
            int centerBlockIndex = UnityEngine.Random.Range(0, structureCandidates.Count);
            Vector3 centerBlock = structureCandidates[centerBlockIndex];
            structureCandidates.RemoveAt(centerBlockIndex);

            int centerBlockX = (int)centerBlock.x;
            int centerBlockY = (int)centerBlock.y;
            int centerBlockZ = (int)centerBlock.z;

            // Carve
            for (int xOffset = -3; xOffset <= 3; xOffset++)
            {
                for (int zOffset = -3; zOffset <= 3; zOffset++)
                {
                    for (int yOffset = -6; yOffset <= lairDepth; yOffset++)
                    {
                        chunk[centerBlockX + xOffset, centerBlockY - yOffset, centerBlockZ + zOffset] = BlockID.air;
                    }
                }
            }

            // Decorate with polymer and light
            bool leftSideDone = false;
            bool rightSideDone = false;
            bool frontSideDone = false;
            bool backSideDone = false;
            for (int y = centerBlockY - lairDepth; y < GameData.WORLD_HEIGHT_LIMIT; y++)
            {
                if (chunk[centerBlockX - 4, y, centerBlockZ] != BlockID.air && !leftSideDone)
                {
                    chunk[centerBlockX - 4, y, centerBlockZ] = BlockID.polymer;
                    if ((y - (centerBlockY - lairDepth)) % 8 == 0)
                        chunk[centerBlockX - 4, y, centerBlockZ] = BlockID.light;
                }
                else
                {
                    leftSideDone = true;
                }
                
                if (chunk[centerBlockX + 4, y, centerBlockZ] != BlockID.air && !rightSideDone)
                {
                    chunk[centerBlockX + 4, y, centerBlockZ] = BlockID.polymer;
                    if ((y - (centerBlockY - lairDepth)) % 8 == 0)
                        chunk[centerBlockX + 4, y, centerBlockZ] = BlockID.light;
                }
                else
                {
                    rightSideDone = true;
                }

                if (chunk[centerBlockX, y, centerBlockZ + 4] != BlockID.air && !frontSideDone)
                {
                    chunk[centerBlockX, y, centerBlockZ + 4] = BlockID.polymer;
                    if ((y - (centerBlockY - lairDepth)) % 8 == 0)
                        chunk[centerBlockX, y, centerBlockZ + 4] = BlockID.light;
                }
                else
                {
                    frontSideDone = true;
                }

                if (chunk[centerBlockX, y, centerBlockZ - 4] != BlockID.air && !backSideDone)
                {
                    chunk[centerBlockX, y, centerBlockZ - 4] = BlockID.polymer;
                    if ((y - (centerBlockY - lairDepth)) % 8 == 0)
                        chunk[centerBlockX, y, centerBlockZ - 4] = BlockID.light;
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
        int spawnCrystalPlant = UnityEngine.Random.Range(0, 10);
        if (spawnCrystalPlant == 0 && structureCandidates.Count > 0)
        {
            int crystalPlantType = UnityEngine.Random.Range(1, 4);
            BlockID crystal;
            if (crystalPlantType == 1)
                crystal = BlockID.blue_crystal;
            else if (crystalPlantType == 2)
                crystal = BlockID.sulphur_crystal;
            else
                crystal = BlockID.boron_crystal;

            int crystalPlantOrientation = UnityEngine.Random.Range(1, 5);

            int baseBlockIndex = UnityEngine.Random.Range(0, structureCandidates.Count);
            Vector3 baseBlockPosition = structureCandidates[baseBlockIndex];
            structureCandidates.RemoveAt(baseBlockIndex);

            int crystalPlantShape = UnityEngine.Random.Range(0, CRYSTAL_PLANT_SHAPES.Length);
            var shapeOffsets = CRYSTAL_PLANT_SHAPES[crystalPlantShape];
            foreach (var offset in shapeOffsets)
            {
                (int offsetX, int offsetY, int offsetZ) = offset;
                if (crystalPlantOrientation == 2) // 90 degrees
                    (offsetX, offsetZ) = (-offsetZ, offsetX);
                else if (crystalPlantOrientation == 3) // 180 degrees
                    (offsetX, offsetZ) = (-offsetX, -offsetZ);
                else if (crystalPlantOrientation == 4) // 270 degrees
                    (offsetX, offsetZ) = (offsetZ, -offsetX);

                chunk[(int)baseBlockPosition.x + offsetX, (int)baseBlockPosition.y + offsetY, (int)baseBlockPosition.z + offsetZ] = crystal;
            }
        }

        //
        // Trees
        //
        int spawnTree = UnityEngine.Random.Range(0, 2);
        if (spawnTree == 0 && structureCandidates.Count > 0)
        {
            int numberOfTrees = UnityEngine.Random.Range(2, 7);

            for (int i = 0; i < numberOfTrees; i++)
            {
                if (structureCandidates.Count > 0)
                {
                    int treeType = UnityEngine.Random.Range(0, 3);
                    int treeOrientation = UnityEngine.Random.Range(1, 5);

                    int baseBlockIndex = UnityEngine.Random.Range(0, structureCandidates.Count);
                    Vector3 baseBlockPosition = structureCandidates[baseBlockIndex];
                    structureCandidates.RemoveAt(baseBlockIndex);

                    int treeShape;
                    (BlockID, int, int, int)[] treeData;
                    if (treeType == 0) // Green light tree
                    {
                        treeShape = UnityEngine.Random.Range(0, GREEN_LIGHT_TREE_SHAPES.Length);
                        treeData = GREEN_LIGHT_TREE_SHAPES[treeShape];
                    }
                    else if (treeType == 1) // Spiral light tree
                    {
                        treeShape = UnityEngine.Random.Range(0, SPIRAL_LIGHT_TREE_SHAPES.Length);
                        treeData = SPIRAL_LIGHT_TREE_SHAPES[treeShape];
                    }
                    else // Color wood tree
                    {
                        treeShape = UnityEngine.Random.Range(0, COLOR_WOOD_TREE_SHAPES.Length);
                        treeData = COLOR_WOOD_TREE_SHAPES[treeShape];
                    }
                    
                    foreach (var data in treeData)
                    {
                        (BlockID treeBlock, int offsetX, int offsetY, int offsetZ) = data;
                        if (treeOrientation == 2) // 90 degrees
                            (offsetX, offsetZ) = (-offsetZ, offsetX);
                        else if (treeOrientation == 3) // 180 degrees
                            (offsetX, offsetZ) = (-offsetX, -offsetZ);
                        else if (treeOrientation == 4) // 270 degrees
                            (offsetX, offsetZ) = (offsetZ, -offsetX);

                        int treeBlockX = (int)baseBlockPosition.x + offsetX;
                        int treeBlockY = (int)baseBlockPosition.y + offsetY;
                        int treeBlockZ = (int)baseBlockPosition.z + offsetZ;
                        
                        if (!(offsetY == 1 && chunk[treeBlockX, treeBlockY - 1, treeBlockZ] == BlockID.air))
                            chunk[treeBlockX, treeBlockY, treeBlockZ] = treeBlock;
                    }
                }
            }
        }

        UnityEngine.Random.state = initialRandomState; // Reset so we don't interfere with anything else (but maybe every use of Random should be based on the seed?)
    }

    private static int[,] GenerateHeightMap(int chunkX, int chunkZ, ulong seed, float amplitude, float frequency, float persistence, int octaves)
    {
        int[,] heightMap = new int[GameData.CHUNK_SIZE,GameData.CHUNK_SIZE];
        float frequency0 = frequency;
        float amplitude0 = amplitude;
        uint xSeed = (uint)((seed & 0b1010101010101010101010101010101010101010101010101010101010101010) >> 48);
        uint zSeed = (uint)((seed & 0b0101010101010101010101010101010101010101010101010101010101010101) >> 48);
        for (int x = 0; x < GameData.CHUNK_SIZE; x++)
        {
            for (int z = 0; z < GameData.CHUNK_SIZE; z++)
            {
                frequency = frequency0;
                amplitude = amplitude0;
                float heightLimit = 0F;
                for (int i = 0; i < octaves; i++)
                {
                    float xArg = (((x + chunkX*GameData.CHUNK_SIZE) + xSeed) / 32F) * frequency; // TODO: FIGURE OUT WHAT 16F IS (I think it's just to prevent overflows), or grid size
                    float zArg = (((z + chunkZ*GameData.CHUNK_SIZE) + zSeed) / 32F) * frequency; //
                    heightLimit += Mathf.PerlinNoise(xArg, zArg) * amplitude;
                    frequency *= 3F;
                    amplitude *= persistence;
                }
                heightMap[x,z] = (int)heightLimit;
            }
        }
        return heightMap;
    }

    public static Vector3 GetLocalBlockPos(Vector3 globalBlockPos)
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

        return new Vector3(localBlockPosX, globalBlockPos.y, localBlockPosZ);
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

    public static ChunkData[] GetAdjacentChunkData(int globalChunkX, int globalChunkZ)
    {
        ChunkData[] adjacentChunkData = new ChunkData[4];

        foreach (GameObject adjacentChunkObject in GameObject.FindGameObjectsWithTag("Chunk"))
        {
            ChunkData chunkData = adjacentChunkObject.GetComponent<ChunkData>();

            // Left ~ 0
            if (chunkData.globalPosZ == globalChunkZ && chunkData.globalPosX == globalChunkX - 1)
                adjacentChunkData[0] = chunkData;

            // Right ~ 1
            if (chunkData.globalPosZ == globalChunkZ && chunkData.globalPosX == globalChunkX + 1)
                adjacentChunkData[1] = chunkData;
            
            // Front ~ 2
            if (chunkData.globalPosX == globalChunkX && chunkData.globalPosZ == globalChunkZ + 1)
                adjacentChunkData[2] = chunkData;

            // Back ~ 3
            if (chunkData.globalPosX == globalChunkX && chunkData.globalPosZ == globalChunkZ - 1)
                adjacentChunkData[3] = chunkData;
        }

        return adjacentChunkData;
    }

    public static bool BlockShouldBeRendered(BlockID block, ChunkData[] adjacentChunkData, ChunkData chunkData, Vector3 localBlockPosition)
    {
        int localBlockX = (int)localBlockPosition.x;
        int localBlockY = (int)localBlockPosition.y;
        int localBlockZ = (int)localBlockPosition.z;

        bool frontTest = false;
        bool leftTest = false;
        bool rightTest = false;
        bool backTest = false;
        bool topTest = false;
        bool bottomTest = false;

        if (block == BlockID.water)
        {
            // QUESTION: Does water need to worry about crystals?

            // Top and bottom checks (BlockID.rock is just a lazy way of not rendering invalid y positions)
            BlockID topBlock = (localBlockY < GameData.WORLD_HEIGHT_LIMIT - 1) ? chunkData.blocks[localBlockX, localBlockY + 1, localBlockZ] : BlockID.rock;
            topTest = topBlock == BlockID.air || topBlock == BlockID.sulphur_crystal || topBlock == BlockID.boron_crystal || topBlock == BlockID.blue_crystal;

            BlockID bottomBlock = (localBlockY > 0) ? chunkData.blocks[localBlockX, localBlockY - 1, localBlockZ] : BlockID.rock;
            bottomTest = bottomBlock == BlockID.air || bottomBlock == BlockID.sulphur_crystal || bottomBlock == BlockID.boron_crystal || bottomBlock == BlockID.blue_crystal;

            if (topTest || bottomTest)
                return true;

            // Front and back checks
            BlockID frontBlock = (localBlockZ == GameData.CHUNK_SIZE - 1) ? adjacentChunkData[2].blocks[localBlockX, localBlockY, 0] : chunkData.blocks[localBlockX, localBlockY, localBlockZ + 1];
            frontTest = frontBlock == BlockID.air || frontBlock == BlockID.sulphur_crystal || frontBlock == BlockID.boron_crystal || frontBlock == BlockID.blue_crystal;

            BlockID backBlock = (localBlockZ == 0) ? adjacentChunkData[3].blocks[localBlockX, localBlockY, GameData.CHUNK_SIZE - 1] : chunkData.blocks[localBlockX, localBlockY, localBlockZ - 1];
            backTest = backBlock == BlockID.air || backBlock == BlockID.sulphur_crystal || backBlock == BlockID.boron_crystal || backBlock == BlockID.blue_crystal;

            if (frontTest || backTest)
                return true;

            // Left and right checks
            BlockID leftBlock = (localBlockX == 0) ? adjacentChunkData[0].blocks[GameData.CHUNK_SIZE - 1, localBlockY, localBlockZ] : chunkData.blocks[localBlockX - 1, localBlockY, localBlockZ];
            leftTest = leftBlock == BlockID.air || leftBlock == BlockID.sulphur_crystal || leftBlock == BlockID.boron_crystal || leftBlock == BlockID.blue_crystal;

            BlockID rightBlock = (localBlockX == GameData.CHUNK_SIZE - 1) ? adjacentChunkData[1].blocks[0, localBlockY, localBlockZ] : chunkData.blocks[localBlockX + 1, localBlockY, localBlockZ];
            rightTest = rightBlock == BlockID.air || rightBlock == BlockID.sulphur_crystal || rightBlock == BlockID.boron_crystal || rightBlock == BlockID.blue_crystal;

            return leftTest || rightTest;
        }
        else
        {
            // Top and bottom checks (BlockID.rock is just a lazy way of not rendering invalid y positions)
            BlockID topBlock = (localBlockY < GameData.WORLD_HEIGHT_LIMIT - 1) ? chunkData.blocks[localBlockX, localBlockY + 1, localBlockZ] : BlockID.rock;
            topTest = topBlock == BlockID.air || topBlock == BlockID.water || topBlock == BlockID.sulphur_crystal || topBlock == BlockID.boron_crystal || topBlock == BlockID.blue_crystal;

            BlockID bottomBlock = (localBlockY > 0) ? chunkData.blocks[localBlockX, localBlockY - 1, localBlockZ] : BlockID.rock;
            bottomTest = bottomBlock == BlockID.air || bottomBlock == BlockID.water || bottomBlock == BlockID.sulphur_crystal || bottomBlock == BlockID.boron_crystal || bottomBlock == BlockID.blue_crystal;

            if (topTest || bottomTest)
                return true;

            // Front and back checks
            BlockID frontBlock = (localBlockZ == GameData.CHUNK_SIZE - 1) ? adjacentChunkData[2].blocks[localBlockX, localBlockY, 0] : chunkData.blocks[localBlockX, localBlockY, localBlockZ + 1];
            frontTest = frontBlock == BlockID.air || frontBlock == BlockID.water || frontBlock == BlockID.sulphur_crystal || frontBlock == BlockID.boron_crystal || frontBlock == BlockID.blue_crystal;

            BlockID backBlock = (localBlockZ == 0) ? adjacentChunkData[3].blocks[localBlockX, localBlockY, GameData.CHUNK_SIZE - 1] : chunkData.blocks[localBlockX, localBlockY, localBlockZ - 1];
            backTest = backBlock == BlockID.air || backBlock == BlockID.water || backBlock == BlockID.sulphur_crystal || backBlock == BlockID.boron_crystal || backBlock == BlockID.blue_crystal;

            if (frontTest || backTest)
                return true;

            // Left and right checks
            BlockID leftBlock = (localBlockX == 0) ? adjacentChunkData[0].blocks[GameData.CHUNK_SIZE - 1, localBlockY, localBlockZ] : chunkData.blocks[localBlockX - 1, localBlockY, localBlockZ];
            leftTest = leftBlock == BlockID.air || leftBlock == BlockID.water || leftBlock == BlockID.sulphur_crystal || leftBlock == BlockID.boron_crystal || leftBlock == BlockID.blue_crystal;

            BlockID rightBlock = (localBlockX == GameData.CHUNK_SIZE - 1) ? adjacentChunkData[1].blocks[0, localBlockY, localBlockZ] : chunkData.blocks[localBlockX + 1, localBlockY, localBlockZ];
            rightTest = rightBlock == BlockID.air || rightBlock == BlockID.water || rightBlock == BlockID.sulphur_crystal || rightBlock == BlockID.boron_crystal || rightBlock == BlockID.blue_crystal;

            return leftTest || rightTest;
        }
    }
}
