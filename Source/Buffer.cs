using System;
using System.Runtime.InteropServices;
using Buffer = SharpVulkan.Buffer;

namespace LearningCSharp
{
    public unsafe class Buffer<T>
        where T : struct
    {
        public Buffer NativeBuffer { get; private set; }

        public Buffer(T[] bufferData)
        {
            int sizeInFloats = Marshal.SizeOf(typeof(T)) / sizeof(float);
            float[] data = new float[sizeInFloats * bufferData.Length];

            int dataIndex = 0;
            for (int bufferDataIndex = 0; bufferDataIndex < bufferData.Length; bufferDataIndex++)
            {
                IntPtr currentFloatPtr = Marshal.UnsafeAddrOfPinnedArrayElement(bufferData, bufferDataIndex);
                for (int floatIndex = 0; floatIndex < sizeInFloats; floatIndex++)
                {
                    data[dataIndex++] = *((float*)currentFloatPtr.ToPointer());
                    currentFloatPtr += sizeof(float);
                }
            }
        }
    }
}