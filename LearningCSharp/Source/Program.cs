using System;
using System.Numerics;
using System.Reflection;
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
        
        /// <summary>
        /// Returns a zero of f on [a, b] if it exists, and null otherwise.
        /// </summary>
        static float? FindZeroOn(float a, float b, Func<float, float> f)
        {
            if (Math.Sign(f(a)) == Math.Sign(f(b)))
                return null;

            float l = a;
            float r = b;
            float avg = (a + b) / 2;
            while (Math.Abs(f(avg)) > 0.001f)
            {
                avg = (l + r) / 2;
                if (f(r) * f(avg) > 0)
                    r = avg;
                else
                    l = avg;
            }
            return (l + r) / 2;
        }

        static float Sqrt(float x) => (float)Math.Sqrt(x);
        static float Sin(float x) => (float)Math.Sin(x);

        public static void Main()
        {
            //Console.ReadKey();
            //return;

            /*
            const int n = 1000000;
            float s = 1;
            int c = 1;
            for (int i = 1; i < n; i++)
            {
                if (c == 1)
                {
                    s = 1 / (1 / s + 1);
                    c = (int)(1 / s);
                    continue;
                }

                if (c % 2 == 0)
                    c /= 2;
                else
                    c = 3 * c + 1;
            }
            Console.WriteLine(s);
            Console.ReadKey();
            return;
            
            // NOTE: Does not work well with large intervals when there are many zeroes between.
            //       I need to find a different way to check if there is a solution.

            const float a = -100;
            const float b = 100;

            Func<float, float> f = x => x * x * x * x * Sin(x) * Sin(1/x) + 3 * x - 5 + 7 * Sin(x) + 1/(x * x);
            float? x0 = FindZeroOn(a, b, x => f(x));

            if (x0.HasValue)
            {
                Logger.Log("Solution = " + x0 + ".");
                Logger.Log("Verification: f(solution) = " + f(x0.Value) + ".");
            }
            else
                Logger.Log("No solution on [" + a + ", " + b + "].");

            Console.ReadKey();
            return;
            */

            Logger.Log("Information about the SIMD stuff:", ConsoleColor.Yellow);
            Console.WriteLine(System.Numerics.Vector.IsHardwareAccelerated);
            Console.WriteLine(Vector<float>.Count);
            Console.WriteLine(Marshal.SizeOf<Vector>());

            Logger.Log("Actual Tests (first Microsoft's, then mine, then mine with no SIMD):", ConsoleColor.Yellow);

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

            watch.Reset();

            Vector result3 = new Vector();
            watch.Start();
            for (long i = 0; i < iterationsCount; i++)
            {
                Vector v1 = new Vector(i, 2 * i, 3 * i, 4 * i);
                Vector v2 = new Vector(5 * i, 6 * i, -1 * i, 0.534f * i);

                result3 = Vector.Add(v1, v2);
            }
            watch.Stop();
            Logger.Log(watch.ElapsedTicks, ConsoleColor.Red);

            Console.WriteLine(result1);
            Console.WriteLine(result2);
            Console.WriteLine(result3);
            
            Console.ReadKey(true);
            return;
        }
    }
}