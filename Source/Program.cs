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

            [FieldOffset(0)]
            public float x;
            
            [FieldOffset(sizeof(float))]
            public float y;
            
            [FieldOffset(2 * sizeof(float))]
            public float z;
            
            [FieldOffset(3 * sizeof(float))]
            public float w;

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
                this.x = x;
                this.y = y;
                this.z = z;
                this.w = w;
            }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public static Vector Add(Vector a, Vector b)
            {
                return new Vector(a.x + b.x, a.y + b.y, a.z + b.z, a.w + b.w);
            }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public static Vector AddSIMD(Vector a, Vector b)
            {
                Vector<float> va = VectorView.Get<float>((IntPtr)a.components, 0);
                Vector<float> vb = VectorView.Get<float>((IntPtr)b.components, 0);

                Vector result = new Vector();
                VectorView.Set((IntPtr)result.components, 0, va + vb);
                return result;
            }

            public override string ToString()
            {
                return string.Format("({0}, {1}, {2}, {3})", this[0], this[1], this[2], this[3]);
            }
        }

        public static void Main()
        {
            Logger.Log("Information about the SIMD stuff:", ConsoleColor.Yellow);
            Console.WriteLine(System.Numerics.Vector.IsHardwareAccelerated);
            Console.WriteLine(Vector<float>.Count);
            Console.WriteLine(Marshal.SizeOf<Vector>());

            Logger.Log("Actual Tests (first Microsoft's, then mine):", ConsoleColor.Yellow);

            Stopwatch watch = new Stopwatch();
            const long iterationsCount = 99999999;

            Vector4 result1 = new Vector4();
            watch.Start();
            for (long i = 0; i < iterationsCount; i++)
            {
                Vector4 v1 = new Vector4(i, 2 * i, 3 * i, 4 * i);
                Vector4 v2 = new Vector4(5 * i, 6 * i, -1 * i, 0.534f * i);
            
                result1 = v1 + v2;
            }
            watch.Stop();
            Logger.Log(watch.ElapsedTicks, ConsoleColor.Red);

            watch.Reset();
            
            Vector result2 = new Vector();
            watch.Start();
            for (long i = 0; i < iterationsCount; i++)
            {
                Vector v1 = new Vector(i, 2 * i, 3 * i, 4 * i);
                Vector v2 = new Vector(5 * i, 6 * i, -1 * i, 0.534f * i);
            
                result2 = Vector.AddSIMD(v1, v2);
            }
            watch.Stop();
            Logger.Log(watch.ElapsedTicks, ConsoleColor.Red);

            Console.WriteLine(result1);
            Console.WriteLine(result2);
            
            Console.ReadKey(true);
            return;
        }
    }
}