using System;
using System.Numerics;
using System.Runtime.InteropServices;
using SharpVulkan;
using Version = SharpVulkan.Version;
using Buffer = SharpVulkan.Buffer;

namespace LearningCSharp
{
    public struct Vertex
    {
        public Vector2 position;
        public Vector3 color;

        public Vertex(Vector2 position, Vector3 color)
        {
            this.position = position;
            this.color = color;
        }

        public static VertexInputBindingDescription BindingDescription
        {
            get
            {
                return new VertexInputBindingDescription
                {
                    Binding = 0,
                    InputRate = VertexInputRate.Vertex,
                    Stride = (uint)Marshal.SizeOf(typeof(Vertex)),
                };
            }
        }

        public static VertexInputAttributeDescription[] AttributeDescriptions
        {
            get
            {
                return new VertexInputAttributeDescription[]
                {
                    new VertexInputAttributeDescription { Binding = 0, Location = 0, Format = Format.R32G32SFloat, Offset = (uint)Marshal.OffsetOf(typeof(Vertex), "position") },
                    new VertexInputAttributeDescription { Binding = 0, Location = 1, Format = Format.R32G32B32SFloat, Offset = (uint)Marshal.OffsetOf(typeof(Vertex), "color") },
                };
            }
        }
    }

    public struct Triangle
    {
        public Vertex a;
        public Vertex b;
        public Vertex c;

        public Vertex[] AsArray => new Vertex[] { a, b, c };
        
        public Triangle(Vertex a, Vertex b, Vertex c)
        {
            this.a = a;
            this.b = b;
            this.c = c;
        }
    }

    public unsafe class VulkanRenderer : IDisposable
    {
        public VulkanInstance Instance { get; private set; }
        public VulkanPhysicalDevice PhysicalDevice { get; private set; }
        public LogicalDevice LogicalDevice { get; private set; }
        public VulkanSurface Surface { get; private set; }
        public VulkanSwapchain Swapchain { get; private set; }

        public Buffer VertexBuffer { get; private set; }
        public DeviceMemory DeviceMemory { get; private set; }

        public Shader VertexShader { get; private set; }
        public Shader FragmentShader { get; private set; }
        public GraphicsPipeline Pipeline { get; private set; }

        public VulkanCommandPool CommandPool { get; private set; }
        private CommandBuffer[] commandBuffers;

        private VulkanDebugger debugger;
        private Window mainWindow;
        private VulkanSemaphore imageAvailableSemaphore;
        private VulkanSemaphore renderFinishedSemaphore;
        private Triangle[] triangles;

        private const string VERTEX_SHADER_FILE_PATH = "Shaders/vert.spv";
        private const string FRAGMENT_SHADER_FILE_PATH = "Shaders/frag.spv";

        public VulkanRenderer(string applicationName, Version applicationVersion, string engineName, Version engineVersion, Window mainWindow, Triangle[] triangles)
        {
            this.triangles = triangles;
            this.mainWindow = mainWindow;

            ApplicationInfo appInfo = new ApplicationInfo
            {
                StructureType = StructureType.ApplicationInfo,
                ApiVersion = Vulkan.ApiVersion,
                ApplicationName = Marshal.StringToHGlobalAnsi(applicationName),
                ApplicationVersion = applicationVersion,
                EngineName = Marshal.StringToHGlobalAnsi(engineName),
                EngineVersion = engineVersion,
            };

            try
            {
                Initialize(ref appInfo);
            }
            finally
            {
                Marshal.FreeHGlobal(appInfo.ApplicationName);
                Marshal.FreeHGlobal(appInfo.EngineName);
            }
        }

        void Initialize(ref ApplicationInfo appInfo)
        {
            fixed (ApplicationInfo* appInfoPtr = &appInfo)
                Instance = new VulkanInstance(appInfoPtr, VulkanUtils.Extensions, VulkanUtils.ValidationLayers);

            debugger = new VulkanDebugger(Instance.NativeInstance);
            PhysicalDevice = VulkanUtils.PickBestGPU(Instance.NativeInstance.PhysicalDevices);
            if (!PhysicalDevice.IsValid)
                throw new Exception("No suitable device found!");

            LogicalDevice = new LogicalDevice(PhysicalDevice, VulkanUtils.DeviceExtensions);
            Surface = new VulkanSurface(mainWindow, Instance, PhysicalDevice);

            VertexShader = Shader.LoadShader(VERTEX_SHADER_FILE_PATH, LogicalDevice, ShaderStageFlags.Vertex);
            FragmentShader = Shader.LoadShader(FRAGMENT_SHADER_FILE_PATH, LogicalDevice, ShaderStageFlags.Fragment);
            Pipeline = new GraphicsPipeline(LogicalDevice, new Shader[] { VertexShader, FragmentShader }, Surface);

            Swapchain = new VulkanSwapchain(LogicalDevice, Surface, Pipeline.RenderPass.NativeRenderPass);

            CreateVertexBuffer();
            CommandPool = new VulkanCommandPool(LogicalDevice.NativeDevice, (uint)PhysicalDevice.QueueFamilyIndices.graphicsFamily);
            commandBuffers = new CommandBuffer[Swapchain.ImageCount];
            CreateCommandBuffers();

            imageAvailableSemaphore = new VulkanSemaphore(LogicalDevice.NativeDevice);
            renderFinishedSemaphore = new VulkanSemaphore(LogicalDevice.NativeDevice);

        }

        void CreateVertexBuffer()
        {
            BufferCreateInfo createInfo = new BufferCreateInfo
            {
                StructureType = StructureType.BufferCreateInfo,
                Size = (uint)(Marshal.SizeOf(typeof(Triangle)) * triangles.Length),
                Usage = BufferUsageFlags.VertexBuffer,
                SharingMode = SharingMode.Exclusive,
            };
            VertexBuffer = LogicalDevice.NativeDevice.CreateBuffer(ref createInfo);

            LogicalDevice.NativeDevice.GetBufferMemoryRequirements(VertexBuffer, out MemoryRequirements bufferMemoryRequirements);
            PhysicalDevice.NativeDevice.GetMemoryProperties(out PhysicalDeviceMemoryProperties deviceMemoryProperties);

            uint memoryTypeIndex = 0;
            for (int i = 0; i < deviceMemoryProperties.MemoryTypeCount; i++)
            {
                MemoryType* memoryType = &deviceMemoryProperties.MemoryTypes.Value0 + i;
                if ((bufferMemoryRequirements.MemoryTypeBits & (1 << i)) != 0 && memoryType->PropertyFlags.HasFlag(MemoryPropertyFlags.HostVisible | MemoryPropertyFlags.HostCoherent))
                {
                    memoryTypeIndex = (uint)i;
                    break;
                }
            }

            MemoryAllocateInfo allocateInfo = new MemoryAllocateInfo
            {
                StructureType = StructureType.MemoryAllocateInfo,
                AllocationSize = bufferMemoryRequirements.Size,
                MemoryTypeIndex = memoryTypeIndex,
            };
            DeviceMemory = LogicalDevice.NativeDevice.AllocateMemory(ref allocateInfo);
            LogicalDevice.NativeDevice.BindBufferMemory(VertexBuffer, DeviceMemory, 0);

            IntPtr bufferDataPointer = LogicalDevice.NativeDevice.MapMemory(DeviceMemory, 0, createInfo.Size, MemoryMapFlags.None);
            float[] data = new float[Marshal.SizeOf(typeof(Triangle)) * triangles.Length / sizeof(float)];

            int currentVertex = 0;
            int currentData = 0;
            for (int triangleIndex = 0; triangleIndex < triangles.Length; triangleIndex++)
            {
                Triangle currentTri = triangles[triangleIndex];
                Vertex a = currentTri.AsArray[currentVertex++];
                Vertex b = currentTri.AsArray[currentVertex++];
                Vertex c = currentTri.AsArray[currentVertex];
                currentVertex = 0;

                data[currentData++] = a.position.X;
                data[currentData++] = a.position.Y;

                data[currentData++] = a.color.X;
                data[currentData++] = a.color.Y;
                data[currentData++] = a.color.Z;

                data[currentData++] = b.position.X;
                data[currentData++] = b.position.Y;

                data[currentData++] = b.color.X;
                data[currentData++] = b.color.Y;
                data[currentData++] = b.color.Z;

                data[currentData++] = c.position.X;
                data[currentData++] = c.position.Y;

                data[currentData++] = c.color.X;
                data[currentData++] = c.color.Y;
                data[currentData++] = c.color.Z;
            }
            Marshal.Copy(data, 0, bufferDataPointer, (int)createInfo.Size / sizeof(float));
            LogicalDevice.NativeDevice.UnmapMemory(DeviceMemory);
        }

        void CreateCommandBuffers()
        {
            for (int i = 0; i < Swapchain.ImageCount; i++)
            {
                fixed (CommandBuffer* commandBuffer = &commandBuffers[i])
                {
                    CommandBufferAllocateInfo allocateInfo = new CommandBufferAllocateInfo
                    {
                        StructureType = StructureType.CommandBufferAllocateInfo,
                        CommandBufferCount = (uint)commandBuffers.Length,
                        CommandPool = CommandPool.NativeCommandPool,
                        Level = CommandBufferLevel.Primary,
                    };
                    LogicalDevice.NativeDevice.AllocateCommandBuffers(ref allocateInfo, commandBuffer);

                    CommandBufferBeginInfo beginInfo = new CommandBufferBeginInfo
                    {
                        StructureType = StructureType.CommandBufferBeginInfo,
                        Flags = CommandBufferUsageFlags.SimultaneousUse,
                        InheritanceInfo = IntPtr.Zero,
                    };

                    ClearValue clearColor = new ClearValue
                    {
                        Color = new ClearColorValue
                        {
                            Float32 = new ClearColorValue.Float32Array { Value0 = 0, Value1 = 0, Value2 = 0, Value3 = 1 },
                        },
                    };

                    RenderPassBeginInfo renderPassBeginInfo = new RenderPassBeginInfo
                    {
                        StructureType = StructureType.RenderPassBeginInfo,
                        RenderPass = Pipeline.RenderPass.NativeRenderPass,
                        Framebuffer = Swapchain.GetFrameBuffer(i).NativeFrameBuffer,
                        RenderArea = new Rect2D(new Offset2D(0, 0), Surface.ImageExtents),
                        ClearValueCount = 1,
                        ClearValues = new IntPtr(&clearColor)
                    };
                    commandBuffer->Begin(ref beginInfo);
                    commandBuffer->BeginRenderPass(ref renderPassBeginInfo, SubpassContents.Inline);

                    commandBuffer->BindPipeline(PipelineBindPoint.Graphics, Pipeline.NativePipeline);

                    Buffer[] buffers = new Buffer[] { VertexBuffer };
                    ulong[] offsets = new ulong[] { 0 };
                    fixed (Buffer* buffersPtr = &buffers[0])
                    fixed (ulong* offsetsPtr = &offsets[0])
                        commandBuffer->BindVertexBuffers(0, 1, buffersPtr, offsetsPtr);

                    commandBuffer->Draw((uint)triangles.Length * 3, 1, 0, 0);
                    
                    commandBuffer->EndRenderPass();
                    commandBuffer->End();
                }
            }
        }

        public void RecreateSwapchain()
        {
            LogicalDevice.NativeDevice.WaitIdle();

            Swapchain.Dispose(false);
            Surface.Dispose(false);
            Pipeline.Dispose(false);
            FragmentShader.Dispose(false);
            VertexShader.Dispose(false);

            VertexShader.ConstructLoad(VERTEX_SHADER_FILE_PATH, LogicalDevice, ShaderStageFlags.Vertex);
            FragmentShader.ConstructLoad(FRAGMENT_SHADER_FILE_PATH, LogicalDevice, ShaderStageFlags.Fragment);
            Pipeline.Construct(LogicalDevice, new Shader[] { VertexShader, FragmentShader }, Surface);
            Surface.Construct(mainWindow, Instance, PhysicalDevice);
            Swapchain.Construct(LogicalDevice, Surface, Pipeline.RenderPass.NativeRenderPass);

            fixed (CommandBuffer* commandBuffersPtr = &commandBuffers[0])
                LogicalDevice.NativeDevice.FreeCommandBuffers(CommandPool.NativeCommandPool, (uint)commandBuffers.Length, commandBuffersPtr);

            CreateCommandBuffers();
        }

        public void DrawFrame()
        {
            Device device = LogicalDevice.NativeDevice;
            uint imageIndex = device.AcquireNextImage(Swapchain.NativeSwapchain, ulong.MaxValue, imageAvailableSemaphore.NativeSemaphore, Fence.Null);

            Semaphore[] waitSemaphores = new Semaphore[] { imageAvailableSemaphore.NativeSemaphore };
            Semaphore[] signalSemaphores = new Semaphore[] { renderFinishedSemaphore.NativeSemaphore };
            PipelineStageFlags[] waitStages = new PipelineStageFlags[] { PipelineStageFlags.ColorAttachmentOutput };
            Swapchain[] swapchains = new Swapchain[] { Swapchain.NativeSwapchain };

            fixed(void* waitSemaphoresPtr = &waitSemaphores[0])
            fixed(void* waitStagesPtr = &waitStages[0])
            fixed(void* commandBufferPtr = &commandBuffers[imageIndex])
            fixed(void* signalSemaphoresPtr = &signalSemaphores[0])
            fixed(void* swapchainsPtr = &swapchains[0])
            {
                IntPtr signalSemaphoresCsPtr = (IntPtr)signalSemaphoresPtr;
                SubmitInfo submitInfo = new SubmitInfo
                {
                    StructureType = StructureType.SubmitInfo,
                    WaitSemaphoreCount = 1,
                    WaitSemaphores = (IntPtr)waitSemaphoresPtr,
                    WaitDstStageMask = (IntPtr)waitStagesPtr,
                    CommandBufferCount = 1,
                    CommandBuffers = (IntPtr)commandBufferPtr,
                    SignalSemaphoreCount = 1,
                    SignalSemaphores = signalSemaphoresCsPtr,
                };
                LogicalDevice.GraphicsQueue.Submit(1, &submitInfo, Fence.Null);

                PresentInfo presentInfo = new PresentInfo
                {
                    StructureType = StructureType.PresentInfo,
                    WaitSemaphoreCount = 1,
                    WaitSemaphores = signalSemaphoresCsPtr,
                    SwapchainCount = 1,
                    Swapchains = (IntPtr)swapchainsPtr,
                    ImageIndices = new IntPtr(&imageIndex),
                    Results = IntPtr.Zero,
                };
                LogicalDevice.GraphicsQueue.Present(ref presentInfo);
                LogicalDevice.GraphicsQueue.WaitIdle();
            }
        }

        public void Dispose()
        {
            LogicalDevice.NativeDevice.WaitIdle();

            LogicalDevice.NativeDevice.DestroyBuffer(VertexBuffer);
            LogicalDevice.NativeDevice.FreeMemory(DeviceMemory);

            imageAvailableSemaphore.Dispose();
            renderFinishedSemaphore.Dispose();

            CommandPool.Dispose();
            Swapchain.Dispose();

            Pipeline.Dispose();
            FragmentShader.Dispose();
            VertexShader.Dispose();
            
            Surface.Dispose();
            LogicalDevice.Dispose();
            debugger.Dispose();
            Instance.Dispose();
            GC.SuppressFinalize(this);
        }

        ~VulkanRenderer()
        {
            Dispose();
        }
    }
}