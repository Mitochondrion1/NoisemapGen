using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;

namespace PerlinNoiseUI
{
    internal class NoiseGenerator
    {
        /**
         * Randomizes rotation angles (in units of PI/16 radians) for each chunk border intecsection point
         * 
         * @pre arrDimLength > 0
         */
        public static int[,] RandomizeRotations(int seed, int arrDimLength)
        {
            Random random = new Random(Seed: seed);
            int[,] values = new int[arrDimLength + 1, arrDimLength + 1];
            int currentSteps = 1, stepsLeft = 1, timesLeft = 2;
            int currentX = (values.GetLength(1) - 1) / 2, currentY = values.GetLength(0) / 2, dir = 0;
            values[currentY, currentX] = random.Next() % 32;

            // Generate the vectors in a spiral pattern from the center
            for (int i = 0; i < values.GetLength(0) * values.GetLength(1) - 1; i++)
            {
                if (timesLeft == 0)
                {
                    currentSteps++;
                    stepsLeft = currentSteps;
                    timesLeft = 2;
                }
                switch (dir % 4)
                {
                    case 0:
                        currentX++;
                        break;
                    case 1:
                        currentY--;
                        break;
                    case 2:
                        currentX--;
                        break;
                    case 3:
                        currentY++;
                        break;
                    default:
                        break;
                }
                values[currentY, currentX] = random.Next() % 32;
                stepsLeft--;
                if (stepsLeft == 0)
                {
                    timesLeft--;
                    stepsLeft = currentSteps;
                    dir++;
                }
            }
            /* linear chunk generation
            for (int i = 0; i < values.GetLength(0); i++)
            {
                for (int j = 0; j < values.GetLength(1); j++)
                {
                    values[i, j] = random.Next() % 32;
                }
            }
            */
            return values;
        }

        /**
         * Create a vector array based on rotations
         * 
         * @pre rotations.GetLength(0) > 0 && rotations.GetLength(1) > 0
         */
        public static Vector2[,] RandomizeRotatedVectors(int[,] rotations)
        {
            Vector2[,] vects = new Vector2[rotations.GetLength(0), rotations.GetLength(1)];
            for (int i = 0; i < vects.GetLength(0); i++)
            {
                for (int j = 0; j < vects.GetLength(1); j++)
                {
                    vects[i, j] = Vector2.CreateRotatedUnitVector((float)(rotations[i, j] * (2 * Math.PI / 32)));
                }
            }
            return vects;
        }

        /**
         * Creates a basic height map
         * 
         * @param vects - rotated vectors
         * @param pixelsPerChunk - the number of pixels on each side of a chunk
         * @param side - the coordinaes of the top-left corner of a chunk (? - idk i dont remember what it is)
         * 
         * @pre vects.GetLength(0) > 1 && vects.GetLength(1) > 1
         * @pre pixelsPerChunk > 0
         */
        public static float[,] InitialHeightMap(Vector2[,] vects, int pixelsPerChunk, (int, int) side)
        {
            float[,] heightMap = new float[(vects.GetLength(0) - 1) * pixelsPerChunk, (vects.GetLength(1) - 1) * pixelsPerChunk];
            Vector2 v;
            for (int i = 0; i < heightMap.GetLength(0); i++)
            {
                for (int j = 0; j < heightMap.GetLength(1); j++)
                {
                    v = new Vector2(i - pixelsPerChunk * (i / pixelsPerChunk + side.Item1),
                        j - pixelsPerChunk * (j / pixelsPerChunk + side.Item2));
                    heightMap[i, j] = v * vects[i / pixelsPerChunk + side.Item1, j / pixelsPerChunk + side.Item2];
                }
            }
            return heightMap;
        }

        /**
         * Creates a byte array for a noisemap bitmap
         * 
         * @param heights - the array of heights
         * @param pixelsPerChunk - the number of pixels on each side of a chunk
         * 
         * @pre heights.GetLength(0) > 0 && heights.GetLength(1) > 0
         * @pre pixelsPerChunk > 0
         * @pre heights.GetLength(0) % pixelsPerChunk = 0
         * @pre heights.GetLength(1) % pixelsPerChunk = 0
         */
        public static byte[,] CreateBmpByteArray(float[,] heights, int pixelsPerChunk)
        {
            byte[,] bmpByteArray = new byte[heights.GetLength(0), heights.GetLength(1)];
            float[,] newHeights = new float[heights.GetLength(0), heights.GetLength(1)];
            float adder = (float)(pixelsPerChunk * Math.Sqrt(2));
            for (int i = 0; i < newHeights.GetLength(0); i++)
            {
                for (int j = 0; j < newHeights.GetLength(1); j++)
                {
                    newHeights[i, j] = heights[i, j] + adder;
                }
            }
            adder *= 2;
            float multiplier = 255 / adder;
            for (int i = 0; i < newHeights.GetLength(0); i++)
            {
                for (int j = 0; j < newHeights.GetLength(1); j++)
                {
                    bmpByteArray[i, j] = (byte)(int)(newHeights[i, j] * multiplier);
                }
            }
            return bmpByteArray;
        }

        /**
         * Builds a grayscale noisemap bitmap
         * 
         * @param bytes - the byte array for the bitmap
         */
        public static unsafe Bitmap CreateBitmapFromBytes(byte[,] bytes)
        {
            Bitmap bmp = new Bitmap(bytes.GetLength(1), bytes.GetLength(0), System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            System.Drawing.Imaging.BitmapData bitmapData = bmp.LockBits(new Rectangle(0, 0, bytes.GetLength(1), bytes.GetLength(0)),
                System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            for (int y = 0; y < bytes.GetLength(0); y++)
            {
                byte* row = (byte*)bitmapData.Scan0 + bitmapData.Stride * y;
                for (int x = 0; x < bytes.GetLength(1); x++)
                {
                    byte grayShade = bytes[y, x];
                    row[x * 3] = grayShade;
                    row[x * 3 + 1] = grayShade;
                    row[x * 3 + 2] = grayShade;
                }
            }
            bmp.UnlockBits(bitmapData);
            return bmp;
        }

        /**
         * Combines height maps horizontally using linear interpolation
         * 
         * @param h1 - a height map
         * @param h2 - anoter height map
         * @param pixelsPerChunk - the number of pixels on each side of a chunk
         */
        public static float[,] CombineHeightMapsH(float[,] h1, float[,] h2, int pixelsPerChunk)
        {
            float[,] h3 = new float[h1.GetLength(0), h1.GetLength(1)];
            for (int i = 0; i < h3.GetLength(0); i++)
            {
                for (int j = 0; j < h3.GetLength(1); j++)
                {
                    h3[i, j] = h1[i, j] + SmoothSteppingFunc((float)(j % pixelsPerChunk) / pixelsPerChunk) * (h2[i, j] - h1[i, j]);
                }
            }
            return h3;
        }

        /**
         * Combines height maps vertically using linear interpolation
         * 
         * @param h1 - a height map
         * @param h2 - anoter height map
         * @param pixelsPerChunk - the number of pixels on each side of a chunk
         */
        public static float[,] CombineHeightMapsV(float[,] h1, float[,] h2, int pixelsPerChunk)
        {
            float[,] h3 = new float[h1.GetLength(0), h1.GetLength(1)];
            for (int j = 0; j < h3.GetLength(1); j++)
            {
                for (int i = 0; i < h3.GetLength(0); i++)
                {
                    h3[i, j] = h1[i, j] + SmoothSteppingFunc((float)(i % pixelsPerChunk) / pixelsPerChunk) * (h2[i, j] - h1[i, j]);
                }
            }
            return h3;
        }

        /**
         * A linear interpolation function
         * 
         * @pre 0 <= x <= 1
         * 
         * @post 0 <= $ret <= 1
         */
        public static float SmoothSteppingFunc(float x)
        {
            return x * x * x * (10 + x * (6 * x - 15));
            //return (float)(6 * Math.Pow(x, 5) - 15 * Math.Pow(x, 4) + 10 * Math.Pow(x, 3));
        }

        /**
         * Generates a final noisemap for a single noise layer
         * 
         * @param seed - the seed for the noisemap generation
         * @param chunksPerSide - the nbumber of chunks on each side of the noismap
         * @param pixelsPerChunk - the number of pixels on each side of a chunk
         * 
         * @pre chunksPerSide > 0
         * @pre pixelsPerChunk > 0
         */
        public static float[,] GenerateLayer(int seed, int chunksPerSide, int pixelsPerChunk)
        {
            int[,] rots = RandomizeRotations(seed, chunksPerSide);
            Vector2[,] vects = RandomizeRotatedVectors(rots);

            float[,] heights0 = InitialHeightMap(vects, pixelsPerChunk, (0, 0));
            float[,] heights1 = InitialHeightMap(vects, pixelsPerChunk, (0, 1));
            float[,] heights2 = InitialHeightMap(vects, pixelsPerChunk, (1, 0));
            float[,] heights3 = InitialHeightMap(vects, pixelsPerChunk, (1, 1));
            float[,] heights01 = CombineHeightMapsH(heights0, heights1, pixelsPerChunk);
            float[,] heights23 = CombineHeightMapsH(heights2, heights3, pixelsPerChunk);
            float[,] heightsF = CombineHeightMapsV(heights01, heights23, pixelsPerChunk);
            //Bitmap bmp = CreateBitmapFromBytes(CreateBmpByteArray(heightsF, pixelsPerChunk));
            //bmp.Save("heights" + seed + ".bmp");
            return heightsF;
        }

        /**
         * Generates a list of multiple layers of noise
         * 
         * @param seed - the seed for the noisemap generation
         * @param chunksPerSide - the nbumber of chunks on each side of the noismap
         * @param pixelsPerChunk - the number of pixels on each side of a chunk
         * @param numOfLayes - the number of layers generated
         * 
         * @return a list of layers
         * 
         * @pre chunksPerSide > 0
         * @pre pixelsPerChunk > 0
         * @pre numOfLayers > 0
         * 
         * @post $ret.Size() == numOfLayers
         */
        public static List<float[,]> GenerateLayers(int seed, int chunksPerSide, int pixelsPerChunk, int numOfLayers)
        {
            List<float[,]> layers = new List<float[,]>(numOfLayers);
            for (int i = 0; i < numOfLayers; i++)
                layers.Add(GenerateLayer(seed, chunksPerSide * (int)Math.Pow(2, i), pixelsPerChunk / (int)Math.Pow(2, i)));
            return layers;
        }

        /**
         * Combines layers of noise
         * 
         * @param layers - a list of the layers
         * @param influenceCoeficient - the parameter of influence decay of more detailed layers
         * 
         * @return a height map
         * 
         * @pre layers.Size() >= 1
         * @pre 0 <= influenceCoeficient <=1
         */
        public static float[,] CombineLayers(List<float[,]> layers, float influenceCoeficient)
        {
            float[,] heights = layers.ElementAt(0);
            for (int i = 1; i < layers.Count; i++)
            {
                for (int j = 0; j < heights.GetLength(0); j++)
                {
                    for (int k = 0; k < heights.GetLength(1); k++)
                    {
                        heights[j, k] += (float)Math.Pow(influenceCoeficient, i) * layers.ElementAt(i)[j, k];
                    }
                }
            }
            return heights;
        }

        /**
         * Generates a the content of a .obj file of the noisemap
         * 
         * @param heights - a height map
         * @param seed - the seed used to generate the noisemap
         * 
         * @returns a string that fits the .obj format and represents the noisemap
         * 
         * @pre heights.GetLength(0) > 0 && heights.GetLength(1) > 0
         */
        public static string ObjectString(float[,] heights, int seed)
        {
            string str = "";
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < heights.GetLength(0); i++)
            {
                for (int j = 0; j < heights.GetLength(1); j++)
                {
                    str = "v " + i + " " + heights[i, j] + " " + j + "\n";
                    stringBuilder.Append(str);
                }
            }
            for (int i = 0; i < heights.GetLength(0) - 1; i++)
            {
                for (int j = 0; j < heights.GetLength(1) - 1; j++)
                {
                    str = "f " + ((j + 1) + i * heights.GetLength(1)) + " " +
                        ((j + 1) + i * heights.GetLength(1) + 1) + " " +
                        ((j + 1) + (i + 1) * heights.GetLength(1)) + "\n";
                    stringBuilder.Append(str);
                    str = "f " + ((j + 1) + i * heights.GetLength(1) + 1) + " " +
                        ((j + 1) + (i + 1) * heights.GetLength(1)) + " " +
                        ((j + 1) + (i + 1) * heights.GetLength(1) + 1) + "\n";
                    stringBuilder.Append(str);
                }
            }
            return stringBuilder.ToString();
        }

        /**
         * Generates a string of heights of the noisemap that can be written into a file
         * The first line contains the dimensions of the noisemap
         * Each line afterwards contains the height of a single pixel, going line by line from left to right in the map
         * 
         * @param heights - a height map
         * @param seed - the seed used to generate the noisemap
         * 
         * @pre heights.GetLength(0) > 0 && heights.GetLength(1) > 0
         */
        public static string HeightDataString(float[,] heights, int seed)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(heights.GetLength(0) + " " + heights.GetLength(1) + "\n");
            for (int i = 0; i < heights.GetLength(0); i++)
            {
                for (int j = 0; j < heights.GetLength(1); j++)
                {
                    stringBuilder.Append(heights[i, j] + "\n");
                }
            }
            return stringBuilder.ToString().Trim();
        }
    }
}
