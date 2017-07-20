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

        public readonly int typeSizeInBytes = Marshal.SizeOf(typeof(T));

        private Device device;

        public Buffer(PhysicalDevice physicalDevice, Device device, T[] bufferData, BufferUsageFlags usage)
        {
            this.device = device;

            BufferSize = (uint)(typeSizeInBytes * bufferData.Length);
            byte[] data = new byte[BufferSize];

            int dataIndex = 0;
            for (int bufferDataIndex = 0; bufferDataIndex < bufferData.Length; bufferDataIndex++)
            {
                IntPtr currentBytePointer = Marshal.UnsafeAddrOfPinnedArrayElement(bufferData, bufferDataIndex);
                for (int floatIndex = 0; floatIndex < typeSizeInBytes; floatIndex++)
                {
                    data[dataIndex++] = *((byte*)currentBytePointer.ToPointer());
                    currentBytePointer += sizeof(byte);
                }
            }

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
            AllocateMemory(memoryRequirements, memoryTypeIndex, data);
        }

        void AllocateMemory(MemoryRequirements memoryRequirements, uint memoryTypeIndex, byte[] data)
        {
            MemoryAllocateInfo allocateInfo = new MemoryAllocateInfo
            {
                StructureType = StructureType.MemoryAllocateInfo,
                AllocationSize = memoryRequirements.Size,
                MemoryTypeIndex = memoryTypeIndex,
            };
            Memory = device.AllocateMemory(ref allocateInfo);
            device.BindBufferMemory(NativeBuffer, Memory, 0);

            IntPtr bufferDataPointer = device.MapMemory(Memory, 0, BufferSize, MemoryMapFlags.None);
            Marshal.Copy(data, 0, bufferDataPointer, data.Length);
            device.UnmapMemory(Memory);
        }

        public void Dispose()
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