using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeighborLookupTableGenerator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ulong[] masks = new ulong[64];

            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    bool isLeft = x - 1 >= 0;
                    bool isRight = x + 1 < 8;
                    bool isTop = y + 1 < 8;
                    bool isBottom = y - 1 >= 0;

                    int index = y * 8 + x;

                    ulong mask = 0;

                    if(isLeft)
                    {
                        int leftIndex = index - 1;
                        mask |= (1UL << leftIndex);

                        if(isBottom)
                        {
                            int leftBottomIndex = leftIndex - 8;
                            mask |= (1UL << leftBottomIndex);
                        }

                        if(isTop)
                        {
                            int leftTopIndex = leftIndex + 8;
                            mask |= (1UL << leftTopIndex);
                        }
                    }

                    if (isRight)
                    {
                        int rightIndex = index + 1;
                        mask |= (1UL << rightIndex);

                        if (isBottom)
                        {
                            int rightBottomIndex = rightIndex - 8;
                            mask |= (1UL << rightBottomIndex);
                        }

                        if (isTop)
                        {
                            int rightTopIndex = rightIndex + 8;
                            mask |= (1UL << rightTopIndex);
                        }
                    }

                    if(isTop)
                    {
                        int topIndex = index + 8;
                        mask |= (1UL << topIndex);
                    }

                    if (isBottom)
                    {
                        int bottomIndex = index - 8;
                        mask |= (1UL << bottomIndex);
                    }

                    masks[index] = mask;
                }
            }

            StringBuilder sb = new StringBuilder(10000);

            sb.AppendLine("static readonly ulong[] NeighborLookups = new ulong[64]");
            sb.AppendLine("{");
            foreach (var mask in masks)
            {
                sb.Append("0b");
                sb.Append(Convert.ToString((long)mask, 2));
                sb.AppendLine(",");
            }
            sb.Remove(sb.Length - 2, 2);
            sb.AppendLine();
            sb.AppendLine("};");

            Console.Write(sb.ToString());

            Console.ReadLine();
        }
    }
}

