using System;
using SharpVulkan;

namespace LearningCSharp
{
    public unsafe class UniformBuffer<T> : Buffer<T>
        where T : struct
    {
        public DescriptorSetLayout DescriptorSetLayout { get; private set; }
        public DescriptorPool DescriptorPool { get; private set; }
        public DescriptorSet DescriptorSet { get; private set; }
        public uint Binding { get; private set; }

        private Device device;

        public UniformBuffer(PhysicalDevice physicalDevice, Device device, uint binding, ShaderStageFlags shaderStages, T[] bufferData)
            : base(physicalDevice, device, bufferData, BufferUsageFlags.UniformBuffer)
        {
            this.device = device;
            Binding = binding;

            DescriptorSetLayoutBinding layoutBinding = new DescriptorSetLayoutBinding
            {
                Binding = Binding,
                DescriptorType = DescriptorType.UniformBuffer,
                DescriptorCount = 1,
                StageFlags = shaderStages,
                ImmutableSamplers = IntPtr.Zero,
            };

            DescriptorSetLayoutCreateInfo createInfo = new DescriptorSetLayoutCreateInfo
            {
                StructureType = StructureType.DescriptorSetLayoutCreateInfo,
                BindingCount = 1,
                Bindings = new IntPtr(&layoutBinding),
            };
            DescriptorSetLayout = device.CreateDescriptorSetLayout(ref createInfo);
            CreateDescriptorPool();
            AllocateDescriptorSet();
        }

        void CreateDescriptorPool()
        {
            DescriptorPoolSize poolSize = new DescriptorPoolSize
            {
                Type = DescriptorType.UniformBuffer,
                DescriptorCount = 1,
            };

            DescriptorPoolCreateInfo createInfo = new DescriptorPoolCreateInfo
            {
                StructureType = StructureType.DescriptorPoolCreateInfo,
                PoolSizeCount = 1,
                PoolSizes = new IntPtr(&poolSize),
                MaxSets = 1,
            };
            DescriptorPool = device.CreateDescriptorPool(ref createInfo);
        }

        void AllocateDescriptorSet()
        {
            //DescriptorSetLayout* setLayout = stackalloc DescriptorSetLayout[1];
            //*setLayout = DescriptorSetLayout;

            DescriptorSetLayout[] setLayouts = new DescriptorSetLayout[] { DescriptorSetLayout };
            fixed (void* setLayoutsPtr = &setLayouts[0])
            {
                DescriptorSetAllocateInfo allocateInfo = new DescriptorSetAllocateInfo
                {
                    StructureType = StructureType.DescriptorSetAllocateInfo,
                    DescriptorPool = DescriptorPool,
                    DescriptorSetCount = 1,
                    //SetLayouts = (IntPtr)setLayout,
                    SetLayouts = (IntPtr)setLayoutsPtr,
                };
                DescriptorSet descriptorSet;
                device.AllocateDescriptorSets(ref allocateInfo, &descriptorSet);
                DescriptorSet = descriptorSet;

                DescriptorBufferInfo bufferInfo = new DescriptorBufferInfo
                {
                    Buffer = NativeBuffer,
                    Offset = 0,
                    Range = BufferSize,
                };

                WriteDescriptorSet descriptorWrite = new WriteDescriptorSet
                {
                    StructureType = StructureType.WriteDescriptorSet,
                    DestinationSet = DescriptorSet,
                    DestinationBinding = Binding,
                    DestinationArrayElement = 0,
                    DescriptorType = DescriptorType.UniformBuffer,
                    DescriptorCount = 1,
                    BufferInfo = new IntPtr(&bufferInfo),
                    ImageInfo = IntPtr.Zero,
                    TexelBufferView = IntPtr.Zero,
                };
                device.UpdateDescriptorSets(1, &descriptorWrite, 0, null);

                IntPtr bufferDataPointer = device.MapMemory(Memory, 0, BufferSize, MemoryMapFlags.None);
                Console.WriteLine(((UniformMVPMatrices*)(bufferDataPointer.ToPointer()))->model);
                Console.WriteLine(((UniformMVPMatrices*)(bufferDataPointer.ToPointer()))->view);
                Console.WriteLine(((UniformMVPMatrices*)(bufferDataPointer.ToPointer()))->projection);
                device.UnmapMemory(Memory);
            }
        }

        public override void Dispose()
        {
            device.DestroyDescriptorSetLayout(DescriptorSetLayout);
            device.DestroyDescriptorPool(DescriptorPool);
            base.Dispose();
        }

        ~UniformBuffer()
        {
            Dispose();
        }
    }
}