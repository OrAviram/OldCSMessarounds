using System;
using System.Runtime.InteropServices;
using SharpVulkan;
using Buffer = SharpVulkan.Buffer;

namespace LearningCSharp
{
    public unsafe class Buffer<T> : IDisposable
        where T : struct
    {
        public Buffer NativeBuffer { get; private set; }
        public DeviceMemory Memory { get; private set; }
        public uint BufferSize { get; private set; }

        public readonly uint typeSizeInBytes = (uint)Marshal.SizeOf(typeof(T));

        private T[] _data;
        public T[] Data
        {
            get { return _data; }
            set
            {
                _data = value;
                byte[] data = new byte[BufferSize];

                int dataIndex = 0;
                for (int bufferDataIndex = 0; bufferDataIndex < value.Length; bufferDataIndex++)
                {
                    IntPtr currentBytePointer = Marshal.UnsafeAddrOfPinnedArrayElement(value, bufferDataIndex);
                    for (int byteIndex = 0; byteIndex < typeSizeInBytes; byteIndex++)
                    {
                        data[dataIndex++] = *((byte*)currentBytePointer.ToPointer());
                        currentBytePointer += sizeof(byte);
                    }
                }

                // This is here just for debugging when I want to see the data as a float array.
                //float[] floats = new float[data.Length / sizeof(float)];
                //for (int i = 0; i < floats.Length; i++)
                //{
                //    IntPtr fPtr = Marshal.UnsafeAddrOfPinnedArrayElement(floats, i);
                //    for (int j = 0; j < sizeof(float); j++)
                //    {
                //        *((byte*)(fPtr.ToPointer())) = data[i * sizeof(float) + j];
                //        fPtr += sizeof(byte);
                //    }
                //}

                IntPtr bufferDataPointer = device.MapMemory(Memory, 0, BufferSize, MemoryMapFlags.None);
                Marshal.Copy(data, 0, bufferDataPointer, (int)BufferSize);
                device.UnmapMemory(Memory);

                //bufferDataPointer = device.MapMemory(Memory, 0, BufferSize, MemoryMapFlags.None);
                //Console.WriteLine(((UniformMVPMatrices*)(bufferDataPointer.ToPointer()))->model);
                //Console.WriteLine(((UniformMVPMatrices*)(bufferDataPointer.ToPointer()))->view);
                //Console.WriteLine(((UniformMVPMatrices*)(bufferDataPointer.ToPointer()))->projection);
                //device.UnmapMemory(Memory);
            }
        }

        private Device device;

        public Buffer(PhysicalDevice physicalDevice, Device device, T[] bufferData, BufferUsageFlags usage)
        {
            this.device = device;

            BufferSize = (uint)(typeSizeInBytes * bufferData.Length);
            
            BufferCreateInfo createInfo = new BufferCreateInfo
            {
                StructureType = StructureType.BufferCreateInfo,
                Size = BufferSize,
                SharingMode = SharingMode.Exclusive,
                Usage = usage,
            };
            NativeBuffer = device.CreateBuffer(ref createInfo);
            
            device.GetBufferMemoryRequirements(NativeBuffer, out MemoryRequirements memoryRequirements);
            physicalDevice.GetMemoryProperties(out PhysicalDeviceMemoryProperties memoryProperties);

            uint memoryTypeIndex = 0;
            for (int i = 0; i < memoryProperties.MemoryTypeCount; i++)
            {
                MemoryType* memoryType = &memoryProperties.MemoryTypes.Value0 + i;
                if ((memoryRequirements.MemoryTypeBits & (1 << i)) != 0 && memoryType->PropertyFlags.HasFlag(MemoryPropertyFlags.HostVisible | MemoryPropertyFlags.HostCoherent))
                {
                    memoryTypeIndex = (uint)i;
                    break;
                }
            }
            AllocateMemory(memoryRequirements, memoryTypeIndex);
            Data = bufferData;
        }

        void AllocateMemory(MemoryRequirements memoryRequirements, uint memoryTypeIndex)
        {
            MemoryAllocateInfo allocateInfo = new MemoryAllocateInfo
            {
                StructureType = StructureType.MemoryAllocateInfo,
                AllocationSize = memoryRequirements.Size,
                MemoryTypeIndex = memoryTypeIndex,
            };
            Memory = device.AllocateMemory(ref allocateInfo);
            device.BindBufferMemory(NativeBuffer, Memory, 0);
        }

        public virtual void Dispose()
        {
            device.DestroyBuffer(NativeBuffer);
            device.FreeMemory(Memory);
            GC.SuppressFinalize(this);
        }

        ~Buffer()
        {
            Dispose();
        }
    }
}