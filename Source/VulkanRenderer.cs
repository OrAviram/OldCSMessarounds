using System;
using System.Runtime.InteropServices;
using SharpVulkan;
using Version = SharpVulkan.Version;

namespace LearningCSharp
{
    public unsafe class VulkanRenderer : IDisposable
    {
        public VulkanInstance Instance { get; private set; }
        public VulkanPhysicalDevice PhysicalDevice { get; private set; }
        public LogicalDevice LogicalDevice { get; private set; }
        public VulkanSurface Surface { get; private set; }
        public VulkanSwapchain Swapchain { get; private set; }

        public Shader VertexShader { get; private set; }
        public Shader FragmentShader { get; private set; }
        public GraphicsPipeline Pipeline { get; private set; }

        public VulkanCommandPool CommandPool { get; private set; }
        private CommandBuffer[] commandBuffers;

        private VulkanDebugger debugger;
        private Window mainWindow;
        private VulkanSemaphore imageAvailableSemaphore;
        private VulkanSemaphore renderFinishedSemaphore;

        public VulkanRenderer(string applicationName, Version applicationVersion, string engineName, Version engineVersion, Window mainWindow)
        {
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
            fixed(ApplicationInfo* appInfoPtr = &appInfo)
                Instance = new VulkanInstance(appInfoPtr, VulkanUtils.Extensions, VulkanUtils.ValidationLayers);

            debugger = new VulkanDebugger(Instance.NativeInstance);
            PhysicalDevice = VulkanUtils.PickBestGPU(Instance.NativeInstance.PhysicalDevices);
            if (!PhysicalDevice.IsValid)
                throw new Exception("No suitable device found!");

            LogicalDevice = new LogicalDevice(PhysicalDevice, VulkanUtils.DeviceExtensions);
            Surface = new VulkanSurface(mainWindow, Instance, PhysicalDevice);

            VertexShader = Shader.LoadShader("Shaders/vert.spv", LogicalDevice, ShaderStageFlags.Vertex);
            FragmentShader = Shader.LoadShader("Shaders/frag.spv", LogicalDevice, ShaderStageFlags.Fragment);
            Pipeline = new GraphicsPipeline(LogicalDevice, new Shader[] { VertexShader, FragmentShader }, Surface);

            Swapchain = new VulkanSwapchain(LogicalDevice, Surface, Pipeline.RenderPass.NativeRenderPass);
            CommandPool = new VulkanCommandPool(LogicalDevice.NativeDevice, (uint)PhysicalDevice.QueueFamilyIndices.graphicsFamily);
            commandBuffers = new CommandBuffer[Swapchain.ImageCount];
            CreateCommandBuffers();

            imageAvailableSemaphore = new VulkanSemaphore(LogicalDevice.NativeDevice);
            renderFinishedSemaphore = new VulkanSemaphore(LogicalDevice.NativeDevice);
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
                    commandBuffer->Draw(6, 1, 0, 0);

                    commandBuffer->EndRenderPass();
                    commandBuffer->End();
                }
            }
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
            LogicalDevice.GraphicsQueue.WaitIdle();
            LogicalDevice.NativeDevice.WaitIdle();

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