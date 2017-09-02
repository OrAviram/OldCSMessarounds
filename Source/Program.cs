using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SDL2;
using SharpVulkan;

using Version = SharpVulkan.Version;
using Semaphore = SharpVulkan.Semaphore;

namespace LearningCSharp
{
    struct QueueFamilyIndices
    {
        public const uint INVALID_INDEX = uint.MaxValue;
        public static QueueFamilyIndices Invalid { get; } = new QueueFamilyIndices { graphicsFamily = INVALID_INDEX, presentationFamily = INVALID_INDEX };

        public uint graphicsFamily;
        public uint presentationFamily;

        public bool IsValid => graphicsFamily != INVALID_INDEX && presentationFamily != INVALID_INDEX;
        public bool IsSingleIndex => graphicsFamily == presentationFamily && IsValid;
        public uint[] ToUniqueArray => IsSingleIndex ? new uint[] { graphicsFamily } : new uint[] { graphicsFamily, presentationFamily };
    }

    struct SwapchainInfo
    {
        public SurfaceFormat surfaceFormat;
        public PresentMode presentMode;
        public SurfaceCapabilities surfaceCapabilities;
        public Extent2D imageExtent;
        public uint imageCount;
    }

    unsafe struct Shader
    {
        public ShaderModule module;
        public PipelineShaderStageCreateInfo pipelineStage;
        private IntPtr unmanagedEntryPointName;

        public Shader(IntPtr unmanagedEntryPointName) : this()
        {
            this.unmanagedEntryPointName = unmanagedEntryPointName;
        }

        public void Destroy(Device device)
        {
            device.DestroyShaderModule(module);
            Marshal.FreeHGlobal(unmanagedEntryPointName);
        }
    }

    unsafe struct Vertex
    {
        public Vector3 position;
        public Vector4 color;

        public static readonly Tuple<VertexInputAttributeDescription, VertexInputAttributeDescription> attributeDescriptions = new Tuple<VertexInputAttributeDescription, VertexInputAttributeDescription>
            (new VertexInputAttributeDescription
            {
                Binding = 0,
                Format = Format.R32G32B32SFloat,
                Location = 0,
                Offset = (uint)Marshal.OffsetOf<Vertex>("position"),
            },
            new VertexInputAttributeDescription
            {
                Binding = 0,
                Format = Format.R32G32B32A32SFloat,
                Location = 1,
                Offset = (uint)Marshal.OffsetOf<Vertex>("color"),
            });

        public static readonly VertexInputBindingDescription bindingDescription = new VertexInputBindingDescription
        {
            Binding = 0,
            InputRate = VertexInputRate.Vertex,
            Stride = (uint)Marshal.SizeOf<Vertex>(),
        };

        public override string ToString()
        {
            return $"{{ Position: {position}, Color: {color} }}";
        }
    }

    unsafe struct Buffer
    {
        public SharpVulkan.Buffer buffer;
        public DeviceMemory memory;
        public readonly ulong size;

        public Buffer(ulong size) : this()
        {
            this.size = size;
        }

        public void Destroy(Device device)
        {
            device.DestroyBuffer(buffer);
            device.FreeMemory(memory);
        }
    }

    struct MVPMatrices
    {
        public static MVPMatrices Identity { get; } = new MVPMatrices { model = Matrix4x4.Identity, view = Matrix4x4.Identity, projection = Matrix4x4.Identity };

        public Matrix4x4 model;
        public Matrix4x4 view;
        public Matrix4x4 projection;
    }

    struct UniformBuffer
    {
        public Buffer buffer;
        public DescriptorSet descriptorSet;

        public void Destroy(Device device)
        {
            buffer.Destroy(device);
        }
    }

    static unsafe class Program
    {
        const float RECTANGLE_SIZE = .5f;

        delegate void DebugReportCallbackDel(DebugReportFlags flags, DebugReportObjectType objectType, ulong obj, PointerSize location, int code, string layerPrefix, string message, IntPtr userData);

        static Instance instance;
        static DebugReportCallback debugReportCallback;
        static Surface surface;
        static PhysicalDevice physicalDevice;
        static Device logicalDevice;
        static Swapchain swapchain;
        static RenderPass renderPass;
        static CommandPool commandPool;

        static DescriptorSetLayout descriptorSetLayout;
        static DescriptorPool descriptorPool;
        static UniformBuffer mvpMatricesBuffer;

        static Shader vertexShader;
        static Shader fragmentShader;
        static Pipeline graphicsPipeline;
        static PipelineLayout pipelineLayout;

        static Semaphore imageAvailableSemaphore;
        static Semaphore renderFinishedSemaphore;

        static Buffer vertexBuffer;
        static Buffer indexBuffer;

        static Dictionary<uint, Queue> queues = new Dictionary<uint, Queue>();
        static Image[] swapchainImages;
        static ImageView[] swapchainImageViews;
        static Framebuffer[] frameBuffers;
        static CommandBuffer[] commandBuffers;

        static QueueFamilyIndices queueFamilyIndices = QueueFamilyIndices.Invalid;
        static SwapchainInfo swapchainInfo;

        static Viewport viewport;

        static IntPtr window;

        static readonly Vertex[] vertices = new Vertex[]
        {
            new Vertex { position = new Vector3(-RECTANGLE_SIZE, -RECTANGLE_SIZE, 0), color = new Vector4(1, 1, 1, 1) },
            new Vertex { position = new Vector3(RECTANGLE_SIZE, -RECTANGLE_SIZE, 0), color = new Vector4(0, 0, 0, 1) },
            new Vertex { position = new Vector3(RECTANGLE_SIZE, RECTANGLE_SIZE, 0), color = new Vector4(1, 1, 1, 1) },
            new Vertex { position = new Vector3(-RECTANGLE_SIZE, RECTANGLE_SIZE, 0), color = new Vector4(0, 0, 0, 1) },
        };
        static readonly uint[] indices = new uint[] { 0, 1, 2, 2, 3, 0 };
        static readonly Vector4 clearColor = new Vector4(0, .25f, .15f, 1);

        static readonly MVPMatrices[] mvpMatricesArray = new MVPMatrices[1];
        static ref MVPMatrices MVPMatrices => ref mvpMatricesArray[0];

        static readonly string[] extensions = new string[] { "VK_EXT_debug_report", "VK_KHR_surface", "VK_KHR_win32_surface" };
        static readonly string[] validationLayers = new string[] { "VK_LAYER_LUNARG_standard_validation" };
        static readonly string[] deviceExtensions = new string[] { "VK_KHR_swapchain" };

        static void Main()
        {
            SDL.SDL_Init(SDL.SDL_INIT_VIDEO);
            window = SDL.SDL_CreateWindow("Vulkan Sandbox", SDL.SDL_WINDOWPOS_UNDEFINED, SDL.SDL_WINDOWPOS_UNDEFINED, 1300, 800, SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN | SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE);
            CreateViewport();
            InitVulkan();

            bool running = true;
            while (running)
            {
                while (SDL.SDL_PollEvent(out SDL.SDL_Event e) != 0)
                {
                    if (e.type == SDL.SDL_EventType.SDL_QUIT)
                    {
                        running = false;
                        break;
                    }
                    else if (e.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_RESIZED)
                    {
                        CreateViewport();
                        RecreateSwapChain();
                    }
                }
                UpdateApplication();
                DrawFrame();
            }
            DeinitVulkan();
            SDL.SDL_DestroyWindow(window);
            SDL.SDL_Quit();

            // So I can see all the final logs.
            Console.ReadKey(true);
        }

        static void CreateViewport()
        {
            SDL.SDL_GetWindowSize(window, out int width, out int height);
            viewport = new Viewport
            {
                Width = width,
                Height = height,
                MaxDepth = 1,
                MinDepth = 0,
                X = 0,
                Y = 0,
            };
        }

        static void CleanUpSwapchain()
        {
            foreach (Framebuffer frameBuffer in frameBuffers)
                logicalDevice.DestroyFramebuffer(frameBuffer);

            fixed (CommandBuffer* commandBuffersPtr = &commandBuffers[0])
                logicalDevice.FreeCommandBuffers(commandPool, (uint)commandBuffers.Length, commandBuffersPtr);

            logicalDevice.DestroyPipeline(graphicsPipeline);
            logicalDevice.DestroyPipelineLayout(pipelineLayout);
            logicalDevice.DestroyRenderPass(renderPass);

            for (int i = 0; i < swapchainImageViews.Length; i++)
                logicalDevice.DestroyImageView(swapchainImageViews[i]);

            logicalDevice.DestroySwapchain(swapchain);
        }

        static void RecreateSwapChain()
        {
            logicalDevice.WaitIdle();
            CleanUpSwapchain();

            CreateSwapchain();
            CreateRenderPass();
            CreatePipelineLayout();
            CreateGraphicsPipeline();
            CreateFrameBuffers();
            AllocateCommandBuffers();
        }

        static void InitVulkan()
        {
            CreateInstance();
            SetupDebugReport();
            CreateSurface();
            ChoosePhysicalDevice();
            CreateLogicalDevice();

            CreateSwapchain();
            CreateRenderPass();

            CreateDescriptorSetLayout();
            CreateDescriptorPool();
            mvpMatricesBuffer = CreateUniformBuffer((ulong)Marshal.SizeOf<MVPMatrices>(), 0);

            vertexShader = LoadShader("Shaders/vert.spv", ShaderStageFlags.Vertex);
            fragmentShader = LoadShader("Shaders/frag.spv", ShaderStageFlags.Fragment);
            CreatePipelineLayout();
            CreateGraphicsPipeline();

            CreateFrameBuffers();
            CreateCommandPool();
            vertexBuffer = MakeBufferWithStagingBuffer(vertices, BufferUsageFlags.VertexBuffer);
            indexBuffer = MakeBufferWithStagingBuffer(indices, BufferUsageFlags.IndexBuffer);
            AllocateCommandBuffers();

            CreateSemaphores();
        }

        static void UpdateApplication()
        {
            MVPMatrices.model = Matrix4x4.Identity;
            MVPMatrices.view = Matrix4x4.Identity;
            MVPMatrices.projection = Matrix4x4.Identity;

            mvpMatricesBuffer.buffer.SetBufferData(mvpMatricesArray);
            //Console.WriteLine(mvpMatricesBuffer.buffer.GetBufferData<MVPMatrices>()[0].model);
            //Console.WriteLine(mvpMatricesBuffer.buffer.GetBufferData<MVPMatrices>()[0].view);
            //Console.WriteLine(mvpMatricesBuffer.buffer.GetBufferData<MVPMatrices>()[0].projection);
        }

        static void DrawFrame()
        {
            Queue graphicsQueue = queues[queueFamilyIndices.graphicsFamily];
            graphicsQueue.WaitIdle();
            uint imageIndex = logicalDevice.AcquireNextImage(swapchain, ulong.MaxValue, imageAvailableSemaphore, Fence.Null);

            Semaphore* imageAvailableSemaphorePointer = stackalloc Semaphore[1];
            *imageAvailableSemaphorePointer = imageAvailableSemaphore;

            Semaphore* renderFinishedSemaphorePointer = stackalloc Semaphore[1];
            *renderFinishedSemaphorePointer = renderFinishedSemaphore;

            Swapchain* swapchainPointer = stackalloc Swapchain[1];
            *swapchainPointer = swapchain;

            PipelineStageFlags* pipelineStageFlags = stackalloc PipelineStageFlags[1];
            *pipelineStageFlags = PipelineStageFlags.ColorAttachmentOutput;
            
            SubmitInfo submitInfo = new SubmitInfo
            {
                StructureType = StructureType.SubmitInfo,
                WaitSemaphoreCount = 1,
                WaitSemaphores = (IntPtr)imageAvailableSemaphorePointer,
                WaitDstStageMask = (IntPtr)pipelineStageFlags,
                CommandBufferCount = 1,
                CommandBuffers = Marshal.UnsafeAddrOfPinnedArrayElement(commandBuffers, (int)imageIndex),
                SignalSemaphoreCount = 1,
                SignalSemaphores = (IntPtr)renderFinishedSemaphorePointer,
            };
            graphicsQueue.Submit(1, &submitInfo, Fence.Null);

            PresentInfo presentInfo = new PresentInfo
            {
                StructureType = StructureType.PresentInfo,
                WaitSemaphoreCount = 1,
                WaitSemaphores = (IntPtr)renderFinishedSemaphorePointer,
                SwapchainCount = 1,
                Swapchains = (IntPtr)swapchainPointer,
                ImageIndices = new IntPtr(&imageIndex),
                Results = IntPtr.Zero,
            };
            graphicsQueue.Present(ref presentInfo);
        }

        static void DeinitVulkan()
        {
            logicalDevice.WaitIdle();

            mvpMatricesBuffer.Destroy(logicalDevice);
            logicalDevice.DestroyDescriptorPool(descriptorPool);
            logicalDevice.DestroyDescriptorSetLayout(descriptorSetLayout);

            CleanUpSwapchain();

            vertexBuffer.Destroy(logicalDevice);
            indexBuffer.Destroy(logicalDevice);

            logicalDevice.DestroySemaphore(imageAvailableSemaphore);
            logicalDevice.DestroySemaphore(renderFinishedSemaphore);

            logicalDevice.DestroyCommandPool(commandPool);
            
            fragmentShader.Destroy(logicalDevice);
            vertexShader.Destroy(logicalDevice);

            logicalDevice.Destroy();
            instance.DestroySurface(surface);
            instance.DestroyDebugReportCallback(debugReportCallback);
            instance.Destroy();
        }

        static void CreateInstance()
        {
            ApplicationInfo appInfo = new ApplicationInfo
            {
                StructureType = StructureType.ApplicationInfo,
                ApiVersion = Vulkan.ApiVersion,
                ApplicationName = Marshal.StringToHGlobalAnsi("Vulkan Sandbox"),
                ApplicationVersion = new Version(1, 0, 0),
                EngineName = Marshal.StringToHGlobalAnsi("Vulkan Sandbox Engine"),
                EngineVersion = new Version(1, 0, 0),
            };
            IntPtr[] availableExtensions = GetNamePointers(extensions, Vulkan.GetInstanceExtensionProperties(), "extensions");
            IntPtr[] availableValidationLayers = GetNamePointers(validationLayers, Vulkan.InstanceLayerProperties, "validation layers");

            fixed (void* extensions = &availableExtensions[0])
            fixed (void* layers = &availableValidationLayers[0])
            {
                InstanceCreateInfo createInfo = new InstanceCreateInfo
                {
                    StructureType = StructureType.InstanceCreateInfo,
                    ApplicationInfo = new IntPtr(&appInfo),
                    EnabledExtensionCount = (uint)availableExtensions.Length,
                    EnabledExtensionNames = (IntPtr)extensions,
                    EnabledLayerCount = (uint)validationLayers.Length,
                    EnabledLayerNames = (IntPtr)layers,
                };
                instance = Vulkan.CreateInstance(ref createInfo);
            }
            Marshal.FreeHGlobal(appInfo.ApplicationName);
            Marshal.FreeHGlobal(appInfo.EngineName);
        }

        static void SetupDebugReport()
        {
            DebugReportCallbackCreateInfo createInfo = new DebugReportCallbackCreateInfo
            {
                StructureType = StructureType.DebugReportCallbackCreateInfo,
                Flags = (uint)(DebugReportFlags.Information | DebugReportFlags.Warning | DebugReportFlags.Error | DebugReportFlags.PerformanceWarning),
                Callback = Marshal.GetFunctionPointerForDelegate(new DebugReportCallbackDel(DebugReport)),
            };
            debugReportCallback = instance.CreateDebugReportCallback(ref createInfo);

            void DebugReport(DebugReportFlags flags, DebugReportObjectType objectType, ulong obj, PointerSize location, int code, string layerPrefix, string message, IntPtr userData)
            {
                switch (flags)
                {
                    case DebugReportFlags.Error:
                        Logger.Log("VULKAN ERROR: " + message, ConsoleColor.Red);
                        break;

                    case DebugReportFlags.Warning:
                        Logger.Log("VULKAN WARNING: " + message, ConsoleColor.Yellow);
                        break;

                    case DebugReportFlags.PerformanceWarning:
                        Logger.Log("VULKAN PERFORMANCE WARNING: " + message, ConsoleColor.Green);
                        break;

                    case DebugReportFlags.Information:
                        Logger.Log("VULKAN INFORMATION: " + message, ConsoleColor.Cyan);
                        break;

                    case DebugReportFlags.Debug:
                        Logger.Log("VULKAN DEBUG: " + message, ConsoleColor.Gray);
                        break;
                }
            }
        }

        static void CreateSurface()
        {
            SDL.SDL_SysWMinfo windowWMInfo = new SDL.SDL_SysWMinfo();
            SDL.SDL_GetWindowWMInfo(window, ref windowWMInfo);
            Win32SurfaceCreateInfo createInfo = new Win32SurfaceCreateInfo
            {
                StructureType = StructureType.Win32SurfaceCreateInfo,
                InstanceHandle = Marshal.GetHINSTANCE(typeof(Program).Module),
                WindowHandle = windowWMInfo.info.win.window,
            };
            surface = instance.CreateWin32Surface(ref createInfo);
        }

        static void ChoosePhysicalDevice()
        {
            PhysicalDevice[] physicalDevices = instance.PhysicalDevices;
            for (int physicalDeviceIndex = 0; physicalDeviceIndex < physicalDevices.Length; physicalDeviceIndex++)
            {
                physicalDevice = physicalDevices[physicalDeviceIndex];
                QueueFamilyProperties[] queueFamilies = physicalDevice.QueueFamilyProperties;
                for (uint queueFamilyIndex = 0; queueFamilyIndex < queueFamilies.Length; queueFamilyIndex++)
                {
                    QueueFamilyProperties queueFamily = queueFamilies[queueFamilyIndex];
                    if (physicalDevice.GetSurfaceSupport(queueFamilyIndex, surface))
                        queueFamilyIndices.presentationFamily = queueFamilyIndex;

                    if (queueFamily.QueueFlags.HasFlag(QueueFlags.Graphics))
                        queueFamilyIndices.graphicsFamily = queueFamilyIndex;
                }
                if (queueFamilyIndices.IsSingleIndex)
                    break;
            }
            if (!queueFamilyIndices.IsValid)
                throw new Exception("No suitable physical device found!");
        }

        static void CreateLogicalDevice()
        {
            uint* queuePriorities = stackalloc uint[1];
            *queuePriorities = 1;

            uint[] queueFamilyIndices = Program.queueFamilyIndices.ToUniqueArray;
            DeviceQueueCreateInfo* queueCreateInfos = stackalloc DeviceQueueCreateInfo[queueFamilyIndices.Length];
            for (int i = 0; i < queueFamilyIndices.Length; i++)
            {
                queueCreateInfos[i] = new DeviceQueueCreateInfo
                {
                    StructureType = StructureType.DeviceQueueCreateInfo,
                    QueueCount = 1,
                    QueueFamilyIndex = queueFamilyIndices[i],
                    QueuePriorities = (IntPtr)queuePriorities,
                };
            }

            IntPtr[] extensionNames = GetNamePointers(deviceExtensions, physicalDevice.GetDeviceExtensionProperties(), "device extensions");
            fixed (IntPtr* extensionNamesPtr = &extensionNames[0])
            {
                IntPtr extensionNamesIntPtr = IntPtr.Zero;
                uint extensionsCount = 0;
                if (*extensionNamesPtr != IntPtr.Zero)
                {
                    extensionsCount = (uint)deviceExtensions.Length;
                    extensionNamesIntPtr = (IntPtr)extensionNamesPtr;
                }

                DeviceCreateInfo createInfo = new DeviceCreateInfo
                {
                    StructureType = StructureType.DeviceCreateInfo,
                    EnabledExtensionCount = extensionsCount,
                    EnabledExtensionNames = extensionNamesIntPtr,
                    EnabledFeatures = IntPtr.Zero,
                    EnabledLayerCount = 0,
                    EnabledLayerNames = IntPtr.Zero,
                    QueueCreateInfoCount = (uint)queueFamilyIndices.Length,
                    QueueCreateInfos = (IntPtr)queueCreateInfos,
                };
                logicalDevice = physicalDevice.CreateDevice(ref createInfo);
            }

            for (int i = 0; i < queueFamilyIndices.Length; i++)
            {
                uint index = queueFamilyIndices[i];
                if (queues.ContainsKey(index))
                    continue;

                queues.Add(index, logicalDevice.GetQueue(index, 0));
            }
        }

        static void CreateSwapchain()
        {
            physicalDevice.GetSurfaceCapabilities(surface, out swapchainInfo.surfaceCapabilities);
            swapchainInfo.surfaceFormat = ChooseSurfaceFormat(physicalDevice.GetSurfaceFormats(surface));
            swapchainInfo.presentMode = ChoosePresentMode(physicalDevice.GetSurfacePresentModes(surface));
            swapchainInfo.imageExtent = ChooseImageExtent(swapchainInfo.surfaceCapabilities);

            swapchainInfo.imageCount = swapchainInfo.surfaceCapabilities.MinImageCount + 1;
            if (swapchainInfo.surfaceCapabilities.MaxImageCount > 0 && swapchainInfo.imageCount > swapchainInfo.surfaceCapabilities.MaxImageCount)
                swapchainInfo.imageCount = swapchainInfo.surfaceCapabilities.MaxImageCount;

            SwapchainCreateInfo createInfo = new SwapchainCreateInfo
            {
                StructureType = StructureType.SwapchainCreateInfo,
                Surface = surface,
                MinImageCount = swapchainInfo.imageCount,
                ImageFormat = swapchainInfo.surfaceFormat.Format,
                ImageColorSpace = swapchainInfo.surfaceFormat.ColorSpace,
                ImageExtent = swapchainInfo.imageExtent,
                ImageArrayLayers = 1,
                ImageUsage = ImageUsageFlags.ColorAttachment,
                PreTransform = swapchainInfo.surfaceCapabilities.CurrentTransform,
                CompositeAlpha = CompositeAlphaFlags.Opaque,
                OldSwapchain = Swapchain.Null,
                Clipped = true,
                PresentMode = swapchainInfo.presentMode,
            };

            if (!queueFamilyIndices.IsSingleIndex)
            {
                createInfo.ImageSharingMode = SharingMode.Concurrent;
                createInfo.QueueFamilyIndexCount = 2;
                createInfo.QueueFamilyIndices = Marshal.UnsafeAddrOfPinnedArrayElement(queueFamilyIndices.ToUniqueArray, 0);
            }
            else
            {
                createInfo.ImageSharingMode = SharingMode.Exclusive;
                createInfo.QueueFamilyIndexCount = 0;
                createInfo.QueueFamilyIndices = IntPtr.Zero;
            }
            swapchain = logicalDevice.CreateSwapchain(ref createInfo);
            swapchainImages = logicalDevice.GetSwapchainImages(swapchain);

            swapchainImageViews = new ImageView[swapchainImages.Length];
            for (int i = 0; i < swapchainImageViews.Length; i++)
            {
                ImageViewCreateInfo imageViewCreateInfo = new ImageViewCreateInfo
                {
                    StructureType = StructureType.ImageViewCreateInfo,
                    Components = new ComponentMapping(ComponentSwizzle.Identity),
                    Format = swapchainInfo.surfaceFormat.Format,
                    Image = swapchainImages[i],
                    SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.Color, 0, 1, 0, 1),
                    ViewType = ImageViewType.Image2D,
                };
                swapchainImageViews[i] = logicalDevice.CreateImageView(ref imageViewCreateInfo);
            }
        }

        static void CreateRenderPass()
        {
            AttachmentDescription colorAttachment = new AttachmentDescription
            {
                Format = swapchainInfo.surfaceFormat.Format,
                Samples = SampleCountFlags.Sample1,
                LoadOperation = AttachmentLoadOperation.Clear,
                StoreOperation = AttachmentStoreOperation.Store,
                StencilLoadOperation = AttachmentLoadOperation.DontCare,
                StencilStoreOperation = AttachmentStoreOperation.DontCare,
                InitialLayout = ImageLayout.Undefined,
                FinalLayout = ImageLayout.PresentSource,
            };

            AttachmentReference colorAttachmentReference = new AttachmentReference
            {
                Attachment = 0,
                Layout = ImageLayout.ColorAttachmentOptimal,
            };

            SubpassDescription subpass = new SubpassDescription
            {
                PipelineBindPoint = PipelineBindPoint.Graphics,
                ColorAttachmentCount = 1,
                ColorAttachments = new IntPtr(&colorAttachmentReference),
            };

            SubpassDependency dependency = new SubpassDependency
            {
                SourceSubpass = Vulkan.QueueFamilyIgnored,
                DestinationSubpass = 0,
                SourceStageMask = PipelineStageFlags.ColorAttachmentOutput,
                SourceAccessMask = AccessFlags.None,
                DestinationStageMask = PipelineStageFlags.ColorAttachmentOutput,
                DestinationAccessMask = AccessFlags.ColorAttachmentRead | AccessFlags.ColorAttachmentWrite,
            };

            RenderPassCreateInfo createInfo = new RenderPassCreateInfo
            {
                StructureType = StructureType.RenderPassCreateInfo,
                AttachmentCount = 1,
                Attachments = new IntPtr(&colorAttachment),
                DependencyCount = 1,
                Dependencies = new IntPtr(&dependency),
                SubpassCount = 1,
                Subpasses = new IntPtr(&subpass),
            };
            renderPass = logicalDevice.CreateRenderPass(ref createInfo);
        }

        static void CreateDescriptorSetLayout()
        {
            DescriptorSetLayoutBinding binding = new DescriptorSetLayoutBinding
            {
                Binding = 0,
                DescriptorCount = 1,
                DescriptorType = DescriptorType.UniformBuffer,
                StageFlags = ShaderStageFlags.AllGraphics,
                ImmutableSamplers = IntPtr.Zero,
            };

            DescriptorSetLayoutCreateInfo createInfo = new DescriptorSetLayoutCreateInfo
            {
                StructureType = StructureType.DescriptorSetLayoutCreateInfo,
                BindingCount = 1,
                Bindings = new IntPtr(&binding),
            };
            descriptorSetLayout = logicalDevice.CreateDescriptorSetLayout(ref createInfo);
        }

        static void CreateDescriptorPool()
        {
            DescriptorPoolSize poolSize = new DescriptorPoolSize
            {
                DescriptorCount = 1,
                Type = DescriptorType.UniformBuffer,
            };

            DescriptorPoolCreateInfo createInfo = new DescriptorPoolCreateInfo
            {
                StructureType = StructureType.DescriptorPoolCreateInfo,
                MaxSets = 1,
                PoolSizeCount = 1,
                PoolSizes = new IntPtr(&poolSize),
            };
            descriptorPool = logicalDevice.CreateDescriptorPool(ref createInfo);
        }

        static void CreatePipelineLayout()
        {
            DescriptorSetLayout* setLayouts = stackalloc DescriptorSetLayout[1];
            *setLayouts = descriptorSetLayout;

            PipelineLayoutCreateInfo createInfo = new PipelineLayoutCreateInfo
            {
                StructureType = StructureType.PipelineLayoutCreateInfo,
                PushConstantRangeCount = 0,
                PushConstantRanges = IntPtr.Zero,
                SetLayoutCount = 1,
                SetLayouts = (IntPtr)setLayouts,
            };
            pipelineLayout = logicalDevice.CreatePipelineLayout(ref createInfo);
        }

        static void CreateGraphicsPipeline()
        {
            PipelineColorBlendAttachmentState colorAttachment = new PipelineColorBlendAttachmentState
            {
                ColorWriteMask = ColorComponentFlags.R | ColorComponentFlags.G | ColorComponentFlags.B | ColorComponentFlags.A,
                BlendEnable = false,
                SourceColorBlendFactor = BlendFactor.One,
                DestinationColorBlendFactor = BlendFactor.Zero,
                ColorBlendOperation = BlendOperation.Add,
                SourceAlphaBlendFactor = BlendFactor.One,
                DestinationAlphaBlendFactor = BlendFactor.Zero,
                AlphaBlendOperation = BlendOperation.Add,
            };
            PipelineColorBlendStateCreateInfo colorBlendStage = new PipelineColorBlendStateCreateInfo
            {
                StructureType = StructureType.PipelineColorBlendStateCreateInfo,
                LogicOperationEnable = false,
                LogicOperation = LogicOperation.Copy,
                AttachmentCount = 1,
                Attachments = new IntPtr(&colorAttachment),
                BlendConstants = new PipelineColorBlendStateCreateInfo.BlendConstantsArray(),
            };

            DynamicState* dynamicStates = stackalloc DynamicState[2];
            dynamicStates[0] = DynamicState.Viewport;
            dynamicStates[1] = DynamicState.LineWidth;
            PipelineDynamicStateCreateInfo dynamicState = new PipelineDynamicStateCreateInfo
            {
                StructureType = StructureType.PipelineDynamicStateCreateInfo,
                DynamicStateCount = 2,
                DynamicStates = (IntPtr)dynamicStates,
            };

            PipelineInputAssemblyStateCreateInfo inputAssemblyState = new PipelineInputAssemblyStateCreateInfo
            {
                StructureType = StructureType.PipelineInputAssemblyStateCreateInfo,
                Topology = PrimitiveTopology.TriangleList,
                PrimitiveRestartEnable = false,
            };

            PipelineMultisampleStateCreateInfo multisamplingState = new PipelineMultisampleStateCreateInfo
            {
                StructureType = StructureType.PipelineMultisampleStateCreateInfo,
                AlphaToCoverageEnable = false,
                AlphaToOneEnable = false,
                MinSampleShading = 1,
                RasterizationSamples = SampleCountFlags.Sample1,
                SampleMask = IntPtr.Zero,
                SampleShadingEnable = false,
            };

            PipelineRasterizationStateCreateInfo rasterizationState = new PipelineRasterizationStateCreateInfo
            {
                StructureType = StructureType.PipelineRasterizationStateCreateInfo,
                CullMode = CullModeFlags.Back,
                FrontFace = FrontFace.Clockwise,
                DepthBiasEnable = false,
                DepthBiasConstantFactor = 0,
                DepthBiasClamp = 0,
                DepthBiasSlopeFactor = 0,
                DepthClampEnable = false,
                LineWidth = 1,
                PolygonMode = PolygonMode.Fill,
                RasterizerDiscardEnable = false,
            };

            VertexInputBindingDescription* vertexInputBindingDescriptions = stackalloc VertexInputBindingDescription[1];
            *vertexInputBindingDescriptions = Vertex.bindingDescription;

            VertexInputAttributeDescription* vertexInputAttributeDescriptions = stackalloc VertexInputAttributeDescription[2];
            vertexInputAttributeDescriptions[0] = Vertex.attributeDescriptions.Item1;
            vertexInputAttributeDescriptions[1] = Vertex.attributeDescriptions.Item2;

            PipelineVertexInputStateCreateInfo vertexInputState = new PipelineVertexInputStateCreateInfo
            {
                StructureType = StructureType.PipelineVertexInputStateCreateInfo,
                VertexAttributeDescriptionCount = 2,
                VertexAttributeDescriptions = (IntPtr)vertexInputAttributeDescriptions,
                VertexBindingDescriptionCount = 1,
                VertexBindingDescriptions = (IntPtr)vertexInputBindingDescriptions,
            };

            Rect2D scissor = new Rect2D
            {
                Extent = swapchainInfo.imageExtent,
                Offset = new Offset2D(0, 0),
            };
            IntPtr viewportPtr;
            fixed (void* viewportPointer = &viewport)
                viewportPtr = (IntPtr)viewportPointer;

            PipelineViewportStateCreateInfo viewportState = new PipelineViewportStateCreateInfo
            {
                StructureType = StructureType.PipelineViewportStateCreateInfo,
                ScissorCount = 1,
                Scissors = new IntPtr(&scissor),
                ViewportCount = 1,
                Viewports = viewportPtr,
            };

            PipelineShaderStageCreateInfo* shaderStages = stackalloc PipelineShaderStageCreateInfo[2];
            shaderStages[0] = vertexShader.pipelineStage;
            shaderStages[1] = fragmentShader.pipelineStage;

            GraphicsPipelineCreateInfo createInfo = new GraphicsPipelineCreateInfo
            {
                StructureType = StructureType.GraphicsPipelineCreateInfo,

                Layout = pipelineLayout,
                RenderPass = renderPass,
                Subpass = 0,
                BasePipelineIndex = 0,
                BasePipelineHandle = Pipeline.Null,

                ColorBlendState = new IntPtr(&colorBlendStage),
                DepthStencilState = IntPtr.Zero,
                DynamicState = new IntPtr(&dynamicState),
                InputAssemblyState = new IntPtr(&inputAssemblyState),
                MultisampleState = new IntPtr(&multisamplingState),
                RasterizationState = new IntPtr(&rasterizationState),
                VertexInputState = new IntPtr(&vertexInputState),
                ViewportState = new IntPtr(&viewportState),

                StageCount = 2,
                Stages = (IntPtr)shaderStages,
                TessellationState = IntPtr.Zero,
            };
            graphicsPipeline = logicalDevice.CreateGraphicsPipelines(PipelineCache.Null, 1, &createInfo);
        }

        static void CreateFrameBuffers()
        {
            frameBuffers = new Framebuffer[swapchainImages.Length];
            for (int i = 0; i < frameBuffers.Length; i++)
            {
                FramebufferCreateInfo createInfo = new FramebufferCreateInfo
                {
                    StructureType = StructureType.FramebufferCreateInfo,
                    RenderPass = renderPass,
                    AttachmentCount = 1,
                    Attachments = Marshal.UnsafeAddrOfPinnedArrayElement(swapchainImageViews, i),
                    Width = swapchainInfo.imageExtent.Width,
                    Height = swapchainInfo.imageExtent.Height,
                    Layers = 1,
                };
                frameBuffers[i] = logicalDevice.CreateFramebuffer(ref createInfo);
            }
        }

        static void CreateCommandPool()
        {
            CommandPoolCreateInfo createInfo = new CommandPoolCreateInfo
            {
                StructureType = StructureType.CommandPoolCreateInfo,
                Flags = CommandPoolCreateFlags.None,
                QueueFamilyIndex = queueFamilyIndices.graphicsFamily,
            };
            commandPool = logicalDevice.CreateCommandPool(ref createInfo);
        }

        static void AllocateCommandBuffers()
        {
            commandBuffers = new CommandBuffer[swapchainImages.Length];
            for (int i = 0; i < commandBuffers.Length; i++)
            {
                CommandBufferAllocateInfo allocateInfo = new CommandBufferAllocateInfo
                {
                    StructureType = StructureType.CommandBufferAllocateInfo,
                    CommandBufferCount = 1,
                    CommandPool = commandPool,
                    Level = CommandBufferLevel.Primary,
                };
                CommandBuffer* buffer = (CommandBuffer*)Marshal.UnsafeAddrOfPinnedArrayElement(commandBuffers, i).ToPointer();
                logicalDevice.AllocateCommandBuffers(ref allocateInfo, buffer);

                CommandBufferBeginInfo beginInfo = new CommandBufferBeginInfo
                {
                    StructureType = StructureType.CommandBufferBeginInfo,
                    Flags = CommandBufferUsageFlags.SimultaneousUse,
                    InheritanceInfo = IntPtr.Zero,
                };
                buffer->Begin(ref beginInfo);

                ClearValue clearValue = new ClearValue
                {
                    Color = new ClearColorValue
                    {
                        Float32 = new ClearColorValue.Float32Array { Value0 = clearColor.X, Value1 = clearColor.Y, Value2 = clearColor.Z, Value3 = clearColor.W }
                    }
                };
                RenderPassBeginInfo renderPassBeginInfo = new RenderPassBeginInfo
                {
                    StructureType = StructureType.RenderPassBeginInfo,
                    ClearValueCount = 1,
                    ClearValues = new IntPtr(&clearValue),
                    Framebuffer = frameBuffers[i],
                    RenderArea = new Rect2D(new Offset2D(0, 0), swapchainInfo.imageExtent),
                    RenderPass = renderPass,
                };
                buffer->BeginRenderPass(ref renderPassBeginInfo, SubpassContents.Inline);

                fixed (Viewport* viewportPtr = &viewport)
                   buffer->SetViewport(0, 1, viewportPtr);

                buffer->BindPipeline(PipelineBindPoint.Graphics, graphicsPipeline);
                fixed (SharpVulkan.Buffer* dataBuffer = &vertexBuffer.buffer)
                {
                    ulong* offsets = stackalloc ulong[1];
                    *offsets = 0;
                    buffer->BindVertexBuffers(0, 1, dataBuffer, offsets);
                }
                buffer->BindIndexBuffer(indexBuffer.buffer, 0, IndexType.UInt32);

                DescriptorSet* descriptorSets = stackalloc DescriptorSet[1];
                *descriptorSets = mvpMatricesBuffer.descriptorSet;
                buffer->BindDescriptorSets(PipelineBindPoint.Graphics, pipelineLayout, 0, 1, descriptorSets, 0, null);

                buffer->DrawIndexed((uint)indices.Length, 1, 0, 0, 0);

                buffer->EndRenderPass();
                buffer->End();
            }
        }

        static void CreateSemaphores()
        {
            SemaphoreCreateInfo createInfo = new SemaphoreCreateInfo
            {
                StructureType = StructureType.SemaphoreCreateInfo,
            };
            imageAvailableSemaphore = logicalDevice.CreateSemaphore(ref createInfo);
            renderFinishedSemaphore = logicalDevice.CreateSemaphore(ref createInfo);
        }

        static SurfaceFormat ChooseSurfaceFormat(SurfaceFormat[] availableFormats)
        {
            if (availableFormats.Length == 1 && availableFormats[0].Format == Format.Undefined)
                return new SurfaceFormat { Format = Format.B8G8R8A8UNorm, ColorSpace = ColorSpace.SRgbNonlinear };

            foreach (SurfaceFormat format in availableFormats)
            {
                if (format.Format == Format.B8G8R8A8UNorm && format.ColorSpace == ColorSpace.SRgbNonlinear)
                    return format;
            }
            return availableFormats[0];
        }

        static PresentMode ChoosePresentMode(PresentMode[] availablePresentModes)
        {
            PresentMode bestPresentMode = PresentMode.Fifo;
            foreach (PresentMode presentMode in availablePresentModes)
            {
                if (presentMode == PresentMode.Mailbox)
                    return presentMode;

                if (presentMode == PresentMode.Immediate)
                    bestPresentMode = presentMode;
            }
            return bestPresentMode;
        }

        static Extent2D ChooseImageExtent(SurfaceCapabilities capabilities)
        {
            if (capabilities.CurrentExtent.Width != uint.MaxValue)
                return capabilities.CurrentExtent;

            SDL.SDL_GetWindowSize(window, out int width, out int height);
            Extent2D actualExtent = new Extent2D((uint)width, (uint)height);

            actualExtent.Width = actualExtent.Width.Clamp(capabilities.MinImageExtent.Width, capabilities.MaxImageExtent.Width);
            actualExtent.Height = actualExtent.Width.Clamp(capabilities.MinImageExtent.Height, capabilities.MaxImageExtent.Height);

            return actualExtent;
        }

        static Shader LoadShader(string filePath, ShaderStageFlags stage)
        {
            byte[] shader = File.ReadAllBytes(filePath);
            ShaderModuleCreateInfo createInfo = new ShaderModuleCreateInfo
            {
                StructureType = StructureType.ShaderModuleCreateInfo,
                Code = Marshal.UnsafeAddrOfPinnedArrayElement(shader, 0),
                CodeSize = shader.Length,
            };
            ShaderModule module = logicalDevice.CreateShaderModule(ref createInfo);

            IntPtr unmanagedEntryPointName = Marshal.StringToHGlobalAnsi("main");
            PipelineShaderStageCreateInfo pipelineStage = new PipelineShaderStageCreateInfo
            {
                StructureType = StructureType.PipelineShaderStageCreateInfo,
                Module = module,
                Name = unmanagedEntryPointName,
                Stage = stage,
            };
            return new Shader(unmanagedEntryPointName) { module = module, pipelineStage = pipelineStage };
        }

        static Buffer CreateBuffer(ulong size, BufferUsageFlags usage, MemoryPropertyFlags memoryPropertyFlags = MemoryPropertyFlags.HostVisible | MemoryPropertyFlags.HostCoherent)
        {
            Buffer buffer = new Buffer(size);
            BufferCreateInfo createInfo = new BufferCreateInfo
            {
                StructureType = StructureType.BufferCreateInfo,
                SharingMode = SharingMode.Exclusive,
                Size = size,
                Usage = usage,
            };
            buffer.buffer = logicalDevice.CreateBuffer(ref createInfo);

            logicalDevice.GetBufferMemoryRequirements(buffer.buffer, out MemoryRequirements memoryRequirements);
            physicalDevice.GetMemoryProperties(out PhysicalDeviceMemoryProperties memoryProperties);

            uint memoryTypeIndex = 0;
            for (uint i = 0; i < memoryProperties.MemoryTypeCount; i++)
            {
                MemoryType* memoryType = &memoryProperties.MemoryTypes.Value0 + i;
                if ((memoryRequirements.MemoryTypeBits & (1 << (int)i)) != 0 && memoryType->PropertyFlags.HasFlag(memoryPropertyFlags))
                {
                    memoryTypeIndex = i;
                    break;
                }
            }

            MemoryAllocateInfo allocateInfo = new MemoryAllocateInfo
            {
                StructureType = StructureType.MemoryAllocateInfo,
                AllocationSize = memoryRequirements.Size,
                MemoryTypeIndex = memoryTypeIndex,
            };
            buffer.memory = logicalDevice.AllocateMemory(ref allocateInfo);
            logicalDevice.BindBufferMemory(buffer.buffer, buffer.memory, 0);
            return buffer;
        }

        static Buffer CreateBuffer<T>(T[] data, BufferUsageFlags usage, MemoryPropertyFlags memoryPropertyFlags = MemoryPropertyFlags.HostVisible | MemoryPropertyFlags.HostCoherent)
            where T : struct
        {
            ulong size = (ulong)(Marshal.SizeOf<T>() * data.Length);
            Buffer buffer = CreateBuffer(size, usage, memoryPropertyFlags);

            SetBufferData(buffer, data);
            return buffer;
        }

        static void CopyBuffer(SharpVulkan.Buffer source, SharpVulkan.Buffer destination, ulong size)
        {
            CommandBufferAllocateInfo allocateInfo = new CommandBufferAllocateInfo
            {
                StructureType = StructureType.CommandBufferAllocateInfo,
                Level = CommandBufferLevel.Primary,
                CommandPool = commandPool,
                CommandBufferCount = 1,
            };
            CommandBuffer commandBuffer = CommandBuffer.Null;
            logicalDevice.AllocateCommandBuffers(ref allocateInfo, &commandBuffer);

            CommandBufferBeginInfo beginInfo = new CommandBufferBeginInfo
            {
                StructureType = StructureType.CommandBufferBeginInfo,
                Flags = CommandBufferUsageFlags.OneTimeSubmit,
            };
            commandBuffer.Begin(ref beginInfo);

            BufferCopy copyRegion = new BufferCopy
            {
                SourceOffset = 0,
                DestinationOffset = 0,
                Size = size,
            };
            commandBuffer.CopyBuffer(source, destination, 1, &copyRegion);
            commandBuffer.End();

            SubmitInfo submitInfo = new SubmitInfo
            {
                StructureType = StructureType.SubmitInfo,
                CommandBufferCount = 1,
                CommandBuffers = new IntPtr(&commandBuffer),
            };

            Queue graphicsQueue = queues[queueFamilyIndices.graphicsFamily];
            graphicsQueue.Submit(1, &submitInfo, Fence.Null);
            graphicsQueue.WaitIdle();

            logicalDevice.FreeCommandBuffers(commandPool, 1, &commandBuffer);
        }

        static void SetBufferData<T>(this Buffer buffer, T[] data)
            where T : struct
        {
            ulong size = (ulong)(Marshal.SizeOf<T>() * data.Length);
            if (size != buffer.size)
                throw new ArgumentException("Size of buffer data must be the same as the size of the buffer!");

            IntPtr memory = logicalDevice.MapMemory(buffer.memory, 0, size, MemoryMapFlags.None);
            System.Buffer.MemoryCopy(Marshal.UnsafeAddrOfPinnedArrayElement(data, 0).ToPointer(), memory.ToPointer(), (uint)size, (uint)size);
            logicalDevice.UnmapMemory(buffer.memory);
        }

        static T[] GetBufferData<T>(this Buffer buffer)
            where T : struct
        {
            T[] result = new T[(int)buffer.size / Marshal.SizeOf<T>()];
            IntPtr memory = logicalDevice.MapMemory(buffer.memory, 0, buffer.size, MemoryMapFlags.None);
            System.Buffer.MemoryCopy(memory.ToPointer(), Marshal.UnsafeAddrOfPinnedArrayElement(result, 0).ToPointer(), (uint)buffer.size, (uint)buffer.size);
            logicalDevice.UnmapMemory(buffer.memory);
            return result;
        }

        static Buffer MakeBufferWithStagingBuffer<T>(T[] data, BufferUsageFlags usage)
            where T : struct
        {
            Buffer stagingBuffer = CreateBuffer(data, BufferUsageFlags.TransferSource);

            ulong size = (ulong)(Marshal.SizeOf<T>() * data.Length);
            Buffer buffer = CreateBuffer(size, usage | BufferUsageFlags.TransferDestination, MemoryPropertyFlags.DeviceLocal);

            CopyBuffer(stagingBuffer.buffer, buffer.buffer, size);
            stagingBuffer.Destroy(logicalDevice);

            return buffer;
        }

        static DescriptorSet AllocateDescriptorSet()
        {
            DescriptorSetLayout* setLayout = stackalloc DescriptorSetLayout[1];
            *setLayout = descriptorSetLayout;

            DescriptorSetAllocateInfo allocateInfo = new DescriptorSetAllocateInfo
            {
                StructureType = StructureType.DescriptorSetAllocateInfo,
                DescriptorPool = descriptorPool,
                DescriptorSetCount = 1,
                SetLayouts = (IntPtr)setLayout,
            };
            DescriptorSet set = DescriptorSet.Null;
            logicalDevice.AllocateDescriptorSets(ref allocateInfo, &set);
            return set;
        }

        static UniformBuffer CreateUniformBuffer(ulong size, uint binding)
        {
            UniformBuffer buffer = new UniformBuffer
            {
                buffer = CreateBuffer(size, BufferUsageFlags.UniformBuffer),
                descriptorSet = AllocateDescriptorSet(),
            };

            DescriptorBufferInfo bufferInfo = new DescriptorBufferInfo
            {
                Buffer = buffer.buffer.buffer,
                Offset = 0,
                Range = buffer.buffer.size,
            };

            WriteDescriptorSet descriptorWrite = new WriteDescriptorSet
            {
                StructureType = StructureType.WriteDescriptorSet,
                BufferInfo = new IntPtr(&bufferInfo),
                DescriptorCount = 1,
                DescriptorType = DescriptorType.UniformBuffer,
                DestinationArrayElement = 0,
                DestinationBinding = binding,
                DestinationSet = buffer.descriptorSet,
                ImageInfo = IntPtr.Zero,
                TexelBufferView = IntPtr.Zero,
            };
            logicalDevice.UpdateDescriptorSets(1, &descriptorWrite, 0, null);
            return buffer;
        }

        static IntPtr[] GetNamePointers<T>(string[] desiredNames, T[] availablePropertiesArray, string supportedObjectsNameOnFail)
            where T : struct
        {
            IntPtr[] pointers = new IntPtr[desiredNames.Length];
            int currentPointerIndex = 0;
            for (int i = 0; i < availablePropertiesArray.Length; i++)
            {
                IntPtr pointer = Marshal.UnsafeAddrOfPinnedArrayElement(availablePropertiesArray, i);
                if (desiredNames.Contains(Marshal.PtrToStringAnsi(pointer)))
                    pointers[currentPointerIndex++] = pointer;
            }
            if (pointers.Contains(IntPtr.Zero))
                throw new Exception("Not all " + supportedObjectsNameOnFail + " supported!");

            if (pointers.Length == 0)
                pointers = new IntPtr[] { IntPtr.Zero };

            return pointers;
        }
    }
}