﻿using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SDL2;
using SharpVulkan;
using Version = SharpVulkan.Version;

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

    static unsafe class Program
    {
        delegate void DebugReportCallbackDel(DebugReportFlags flags, DebugReportObjectType objectType, ulong obj, PointerSize location, int code, string layerPrefix, string message, IntPtr userData);

        static Instance instance;
        static DebugReportCallback debugReportCallback;
        static Surface surface;
        static PhysicalDevice physicalDevice;
        static Device logicalDevice;
        static Swapchain swapchain;
        static RenderPass renderPass;

        static Shader vertexShader;
        static Shader fragmentShader;
        static Pipeline graphicsPipeline;
        static PipelineLayout pipelineLayout;

        static Dictionary<uint, Queue> queues = new Dictionary<uint, Queue>();
        static Image[] swapchainImages;
        static ImageView[] swapchainImageViews;

        static QueueFamilyIndices queueFamilyIndices = QueueFamilyIndices.Invalid;
        static SwapchainInfo swapchainInfo;

        static IntPtr window;

        static readonly string[] extensions = new string[] { "VK_EXT_debug_report", "VK_KHR_surface", "VK_KHR_win32_surface" };
        static readonly string[] validationLayers = new string[] { "VK_LAYER_LUNARG_standard_validation" };
        static readonly string[] deviceExtensions = new string[] { "VK_KHR_swapchain" };

        static void Main()
        {
            window = SDL.SDL_CreateWindow("Vulkan Sandbox", SDL.SDL_WINDOWPOS_UNDEFINED, SDL.SDL_WINDOWPOS_UNDEFINED, 1000, 500, SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN);
            bool running = true;

            Initialize();

            while (running)
            {
                while (SDL.SDL_PollEvent(out SDL.SDL_Event e) != 0)
                {
                    if (e.type == SDL.SDL_EventType.SDL_QUIT)
                    {
                        running = false;
                        break;
                    }
                }
                // Just so I won't burn the CPU...
                Thread.Sleep(5);
            }
            Deinitialize();
            SDL.SDL_DestroyWindow(window);
        }

        static void Initialize()
        {
            CreateInstance();
            SetupDebugReport();
            CreateSurface();
            ChoosePhysicalDevice();
            CreateLogicalDevice();

            CreateSwapchain();
            CreateRenderPass();

            vertexShader = LoadShader("Shaders/vert.spv", ShaderStageFlags.Vertex);
            fragmentShader = LoadShader("Shaders/frag.spv", ShaderStageFlags.Fragment);
            CreatePipelineLayout();
            CreateGraphicsPipeline();
        }

        static void Deinitialize()
        {
            logicalDevice.DestroyPipelineLayout(pipelineLayout);
            logicalDevice.DestroyPipeline(graphicsPipeline);
            fragmentShader.Destroy(logicalDevice);
            vertexShader.Destroy(logicalDevice);

            logicalDevice.DestroyRenderPass(renderPass);
            for (int i = 0; i < swapchainImageViews.Length; i++)
                logicalDevice.DestroyImageView(swapchainImageViews[i]);

            logicalDevice.DestroySwapchain(swapchain);

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

            RenderPassCreateInfo createInfo = new RenderPassCreateInfo
            {
                StructureType = StructureType.RenderPassCreateInfo,
                AttachmentCount = 1,
                Attachments = new IntPtr(&colorAttachment),
                DependencyCount = 0,
                Dependencies = IntPtr.Zero,
                SubpassCount = 1,
                Subpasses = new IntPtr(&subpass),
            };
            renderPass = logicalDevice.CreateRenderPass(ref createInfo);
        }

        static void CreatePipelineLayout()
        {
            PipelineLayoutCreateInfo createInfo = new PipelineLayoutCreateInfo
            {
                StructureType = StructureType.PipelineLayoutCreateInfo,
                PushConstantRangeCount = 0,
                PushConstantRanges = IntPtr.Zero,
                SetLayoutCount = 0,
                SetLayouts = IntPtr.Zero,
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

            PipelineVertexInputStateCreateInfo vertexInputState = new PipelineVertexInputStateCreateInfo
            {
                StructureType = StructureType.PipelineVertexInputStateCreateInfo,
                VertexAttributeDescriptionCount = 0,
                VertexAttributeDescriptions = IntPtr.Zero,
                VertexBindingDescriptionCount = 0,
                VertexBindingDescriptions = IntPtr.Zero,
            };

            SDL.SDL_GetWindowSize(window, out int width, out int height);
            Viewport viewport = new Viewport
            {
                Width = width,
                Height = height,
                MaxDepth = 1,
                MinDepth = 0,
                X = 0,
                Y = 0,
            };
            Rect2D scissor = new Rect2D
            {
                Extent = swapchainInfo.imageExtent,
                Offset = new Offset2D(0, 0),
            };
            PipelineViewportStateCreateInfo viewportState = new PipelineViewportStateCreateInfo
            {
                StructureType = StructureType.PipelineViewportStateCreateInfo,
                ScissorCount = 1,
                Scissors = new IntPtr(&scissor),
                ViewportCount = 1,
                Viewports = new IntPtr(&viewport),
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