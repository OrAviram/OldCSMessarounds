using System;
using System.Numerics;
using System.Diagnostics;
using System.Runtime.InteropServices;
using NativeInteropEx;

namespace LearningCSharp
{
    unsafe static class Program
    {
        [StructLayout(LayoutKind.Explicit)]
        unsafe struct Vector
        {
            [FieldOffset(0)]
            public fixed float components[4];

            //[FieldOffset(0)]
            //public float x;
            //
            //[FieldOffset(sizeof(float))]
            //public float y;
            //
            //[FieldOffset(2 * sizeof(float))]
            //public float z;
            //
            //[FieldOffset(3 * sizeof(float))]
            //public float w;

            public float this[int componentIndex]
            {
                get
                {
                    fixed (float* comps = components)
                        return comps[componentIndex];
                }
                set
                {
                    fixed (float* comps = components)
                        comps[componentIndex] = value;
                }
            }

            public Vector(float x, float y, float z, float w)
            {
                fixed (float* comps = components)
                {
                    comps[0] = x;
                    comps[1] = y;
                    comps[2] = z;
                    comps[3] = w;
                }

                //this.x = x;
                //this.y = y;
                //this.z = z;
                //this.w = w;
            }

            //public static Vector Add(Vector a, Vector b)
            //{
            //    return new Vector(a.x + b.x, a.y + b.y, a.z + b.z, a.w + b.w);
            //}

            public static Vector AddSIMD(Vector a, Vector b)
            {
                float[] aBad = new float[8];
                float[] bBad = new float[8];

                System.Buffer.MemoryCopy(a.components, Marshal.UnsafeAddrOfPinnedArrayElement(aBad, 0).ToPointer(), 4 * sizeof(float), 4 * sizeof(float));
                System.Buffer.MemoryCopy(b.components, Marshal.UnsafeAddrOfPinnedArrayElement(bBad, 0).ToPointer(), 4 * sizeof(float), 4 * sizeof(float));

                Vector<float> va = new Vector<float>(aBad);
                Vector<float> vb = new Vector<float>(bBad);

                float[] result = new float[8];
                (va + vb).CopyTo(result);

                return new Vector(result[0], result[1], result[2], result[3]);
            }

            public override string ToString()
            {
                return string.Format("({0}, {1}, {2}, {3})", this[0], this[1], this[2], this[3]);
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        struct Test
        {
            [FieldOffset(0)]
            public fixed float test[16];
        }

        public static void Main()
        {
            //Console.WriteLine(System.Numerics.Vector.IsHardwareAccelerated);
            //Console.WriteLine(Marshal.SizeOf<Vector>());
            //
            //Vector v = new Vector();
            //for (int i = 0; i < 4; i++)
            //    v.components[i] = i + 1;
            //
            //Console.WriteLine(v.x);
            //Console.WriteLine(v.y);
            //Console.WriteLine(v.z);
            //Console.WriteLine(v.w);

            Stopwatch watch = new Stopwatch();
            const long iterationsCount = 9999;

            Vector4 result1 = new Vector4();
            watch.Start();
            for (long i = 0; i < iterationsCount; i++)
            {
                Vector4 v1 = new Vector4(i, 2 * i, 3 * i, 4 * i);
                Vector4 v2 = new Vector4(5 * i, 6 * i, -1 * i, 0.534f * i);
            
                result1 = v1 + v2;
                //Console.Write(v1 + v2);
            }
            watch.Stop();
            Console.WriteLine();
            Logger.Log(watch.ElapsedTicks, ConsoleColor.Red);
            
            Vector result2 = new Vector();
            watch.Start();
            for (long i = 0; i < iterationsCount; i++)
            {
                Vector v1 = new Vector(i, 2 * i, 3 * i, 4 * i);
                Vector v2 = new Vector(5 * i, 6 * i, -1 * i, 0.534f * i);
            
                result2 = Vector.AddSIMD(v1, v2);
                //Console.Write(Vector.Add(v1, v2));
            }
            watch.Stop();
            Console.WriteLine();
            Logger.Log(watch.ElapsedTicks, ConsoleColor.Red);

            Console.WriteLine(result1);
            Console.WriteLine(result2);
            
            watch.Reset();
            
            Console.ReadKey(true);
            return;

            //Vector a = new Vector(2, 3, 45, 6);
            //Vector b = new Vector(-3, 15, 2.5f, 321);
            //Vector result = new Vector();
            //
            //watch.Start();
            //result = Vector.AddSIMD(a, b);
            //watch.Stop();
            //Console.WriteLine("Without SIMD: " + watch.ElapsedTicks + " ticks with result " + result + ".");
            
            //watch.Start();
            //result = Vector.AddSIMD(a, b);
            //watch.Stop();
            //Console.WriteLine("With SIMD: " + watch.ElapsedTicks + " ticks with result " + result + ".");

            Console.ReadKey(true);
        }
    }
}