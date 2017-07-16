//using System;
//using System.Linq;
//using System.Diagnostics;
//using System.Collections.Generic;
//using System.Runtime.InteropServices;
//using SharpVulkan;
//using Version = SharpVulkan.Version;

//namespace LearningCSharp
//{
//    public sealed unsafe class VulkanInstance : IDisposable
//    {
//        #if DEBUG
//        public const bool EnableValidationLayers = true;
//        #else
//        public const bool EnableValidationLayers = false;
//        #endif

//        public Instance NativeInstance { get; private set; }
//        public ApplicationInfo AppInfo { get; private set; }
//        public PhysicalDevice PhysicalDevice { get; private set; }
//        public uint GraphicsQueueFamilyIndex { get; private set; }
//        public uint PresentationQueueFamilyIndex { get; private set; }
//        public Surface Surface { get; private set; }

//        public SurfaceCapabilities SurfaceCapabilities { get; private set; }
//        public SurfaceFormat SwapchainImageFormat { get; private set; }
//        public Extent2D SwapchainImageExtent { get; private set; }
//        public PresentMode SurfacePresentMode { get; private set; }

//        private string[] _deviceExtensions = new string[] { "VK_KHR_swapchain" };
//        public string[] DeviceExtensionNames
//        {
//            get { return _deviceExtensions.ToArray(); }
//        }

//        private Window mainWindow;

//        private DebugReportCallback debugReportCallback;
//        private delegate void DebugCallbackDel(DebugReportFlags flags, DebugReportObjectType objType, ulong obj, PointerSize location, int code, string layerPrefix, string msg, IntPtr userData);

//        private struct QueueFamilyIndices
//        {
//            public static QueueFamilyIndices Default { get; } = new QueueFamilyIndices(-1, -1);

//            public int graphicsFamily;
//            public int presentFamily;

//            public QueueFamilyIndices(int graphicsFamily = -1, int presentFamily = -1)
//            {
//                this.graphicsFamily = graphicsFamily;
//                this.presentFamily = presentFamily;
//            }

//            public bool IsComplete => graphicsFamily >= 0 && presentFamily >= 0;
//            public bool SingleQueue => graphicsFamily == presentFamily && IsComplete;
//        }

//        private struct SwapchainSupportDetails
//        {
//            public SurfaceCapabilities capabilities;
//            public SurfaceFormat[] formats;
//            public PresentMode[] presentModes;
//        }

//        enum GPURating : byte
//        {
//            Unsuitable = 0,
//            Suitable = 1,
//            HasSingleQueue = 2
//        }

//        public VulkanInstance(string applicationName, Version applicationVersion, string engineName, Version engineVersion, Window mainWindow)
//        {
//            this.mainWindow = mainWindow;

//            ApplicationInfo appInfo = new ApplicationInfo
//            {
//                StructureType = StructureType.ApplicationInfo,
//                ApiVersion = Vulkan.ApiVersion,
//                ApplicationName = Marshal.StringToHGlobalAnsi(applicationName),
//                ApplicationVersion = applicationVersion,
//                EngineName = Marshal.StringToHGlobalAnsi(engineName),
//                EngineVersion = engineVersion,
//            };
//            AppInfo = appInfo;

//            string[] extensions = GetExtensions();
//            IntPtr[] extensionsPtrPtr = extensions.Select(Marshal.StringToHGlobalAnsi).ToArray();

//            fixed (void* extensionsPtr = &extensionsPtrPtr[0])
//            {
//                IntPtr validationLayers = IntPtr.Zero;
//                uint validationLayersCount = 0;
//                if (EnableValidationLayers)
//                {
//                    string[] layers = GetValidationLayers();
//                    IntPtr[] layersPtrPtr = layers.Select(Marshal.StringToHGlobalAnsi).ToArray();
//                    fixed (void* layersPtr = &layersPtrPtr[0])
//                    {
//                        validationLayers = (IntPtr)layersPtr;
//                        validationLayersCount = (uint)layers.Length;
//                    }
//                }

//                InstanceCreateInfo createInfo = new InstanceCreateInfo()
//                {
//                    StructureType = StructureType.InstanceCreateInfo,
//                    ApplicationInfo = (IntPtr)(&appInfo),
//                    EnabledLayerCount = validationLayersCount,
//                    EnabledLayerNames = validationLayers,
//                    EnabledExtensionCount = (uint)extensions.Length,
//                    EnabledExtensionNames = (IntPtr)extensionsPtr,
//                };
//                NativeInstance = Vulkan.CreateInstance(ref createInfo);
//            }

//            foreach (IntPtr name in extensionsPtrPtr)
//                Marshal.FreeHGlobal(name);

//            Marshal.FreeHGlobal(appInfo.ApplicationName);
//            Marshal.FreeHGlobal(appInfo.EngineName);

//            if (EnableValidationLayers)
//                SetupDebugCallback();

//            CreateSurface();
//            PickPhysicalDevice();

//            SwapchainSupportDetails a = QuerySwapchainSupport();
//            SurfaceCapabilities = a.capabilities;
//            SwapchainImageExtent = ChooseSwapExtent();
//            SwapchainImageFormat = ChooseSwapSurfaceFormat(a.formats);
//            SurfacePresentMode = ChooseSwapPresentMode(a.presentModes);
//        }

//        void SetupDebugCallback()
//        {
//            DebugCallbackDel debugCallback = DebugCallback;

//            DebugReportCallbackCreateInfo createInfo = new DebugReportCallbackCreateInfo
//            {
//                StructureType = StructureType.DebugReportCallbackCreateInfo,
//                Flags = (uint)(/*DebugReportFlags.Debug |*/ DebugReportFlags.Error | DebugReportFlags.Information | DebugReportFlags.PerformanceWarning | DebugReportFlags.Warning),
//                Callback = Marshal.GetFunctionPointerForDelegate(debugCallback),
//            };
//            debugReportCallback = NativeInstance.CreateDebugReportCallback(ref createInfo);
//        }

//        void DebugCallback(DebugReportFlags flags, DebugReportObjectType objType, ulong obj, PointerSize location, int code, string layerPrefix, string msg, IntPtr userData)
//        {
//            switch (flags)
//            {
//                case DebugReportFlags.Error:
//                    Logger.Log("VULKAN ERROR: " + msg, ConsoleColor.Red);
//                    break;

//                case DebugReportFlags.Warning:
//                    Logger.Log("VULKAN WARNING: " + msg, ConsoleColor.Yellow);
//                    break;

//                case DebugReportFlags.PerformanceWarning:
//                    Logger.Log("VULKAN PERFORMANCE WARNING: " + msg, ConsoleColor.Green);
//                    break;

//                case DebugReportFlags.Information:
//                    Logger.Log("VULKAN INFORMATION: " + msg, ConsoleColor.Cyan);
//                    break;

//                case DebugReportFlags.Debug:
//                    Logger.Log("VULKAN DEBUG: " + msg, ConsoleColor.Gray);
//                    break;
//            }
//        }

//        string[] GetValidationLayers()
//        {
//            string[] desiredLayers = new string[] { "VK_LAYER_LUNARG_standard_validation" };

//            LayerProperties[] availableLayerProperties = Vulkan.InstanceLayerProperties;
//            List<string> availableLayers = new List<string>();
//            for (int i = 0; i < availableLayerProperties.Length; i++)
//            {
//                fixed (void* namePtr = &availableLayerProperties[i].LayerName)
//                    availableLayers.Add(Marshal.PtrToStringAnsi(new IntPtr(namePtr)));
//            }

//            string[] enabledLayers = desiredLayers.Where(availableLayers.Contains).ToArray();
//            foreach (string desiredLayer in desiredLayers)
//            {
//                if (!enabledLayers.Contains(desiredLayer))
//                    throw new Exception("Desired validation layer '" + desiredLayer + "' is not supported!");
//            }
//            return enabledLayers;
//        }

//        string[] GetExtensions()
//        {
//            List<string> desiredExtensions = new List<string> { "VK_KHR_surface", "VK_KHR_win32_surface" };
//            if (EnableValidationLayers)
//                desiredExtensions.Add("VK_EXT_debug_report");

//            ExtensionProperties[] availableExtensionProperties = Vulkan.GetInstanceExtensionProperties();
//            List<string> availableExtentions = new List<string>();
//            for (int i = 0; i < availableExtensionProperties.Length; i++)
//            {
//                fixed (void* namePtr = &availableExtensionProperties[i].ExtensionName)
//                    availableExtentions.Add(Marshal.PtrToStringAnsi(new IntPtr(namePtr)));
//            }

//            string[] enabledExtensions = desiredExtensions.Where(availableExtentions.Contains).ToArray();
//            foreach (string desiredExtention in desiredExtensions)
//            {
//                if (!enabledExtensions.Contains(desiredExtention))
//                    throw new Exception("Desired extention '" + desiredExtention + "' is not supported!");
//            }
//            return enabledExtensions;
//        }

//        void CreateSurface()
//        {
//            Win32SurfaceCreateInfo createInfo = new Win32SurfaceCreateInfo
//            {
//                StructureType = StructureType.Win32SurfaceCreateInfo,
//                InstanceHandle = Marshal.GetHINSTANCE(GetType().Module),
//                WindowHandle = mainWindow.Handle,
//            };
//            Surface = NativeInstance.CreateWin32Surface(ref createInfo);
//        }

//        Extent2D ChooseSwapExtent()
//        {
//            if (SurfaceCapabilities.CurrentExtent.Width != uint.MaxValue)
//            {
//                return SurfaceCapabilities.CurrentExtent;
//            }
//            else
//            {
//                Extent2D actualExtent = new Extent2D((uint)mainWindow.Width, (uint)mainWindow.Height);

//                actualExtent.Width = Math.Max(SurfaceCapabilities.MinImageExtent.Width, Math.Min(SurfaceCapabilities.MaxImageExtent.Width, actualExtent.Width));
//                actualExtent.Height = Math.Max(SurfaceCapabilities.MinImageExtent.Height, Math.Min(SurfaceCapabilities.MaxImageExtent.Height, actualExtent.Height));

//                return actualExtent;
//            }
//        }

//        PresentMode ChooseSwapPresentMode(PresentMode[] availablePresentModes)
//        {
//            PresentMode bestMode = PresentMode.Fifo;
//            foreach (PresentMode presentMode in availablePresentModes)
//            {
//                if (presentMode == PresentMode.Mailbox)
//                    return presentMode;
//                else if (presentMode == PresentMode.Immediate)
//                    bestMode = presentMode;
//            }
//            return bestMode;
//        }

//        SurfaceFormat ChooseSwapSurfaceFormat(SurfaceFormat[] availableFormats)
//        {
//            if (availableFormats.Length == 1 && availableFormats[0].Format == Format.Undefined)
//                return new SurfaceFormat() { Format = Format.B8G8R8A8UNorm, ColorSpace = ColorSpace.SRgbNonlinear };

//            foreach (SurfaceFormat format in availableFormats)
//            {
//                if (format.Format == Format.B8G8R8A8UNorm && format.ColorSpace == ColorSpace.SRgbNonlinear)
//                    return format;
//            }
//            return availableFormats[0];
//        }

//        SwapchainSupportDetails QuerySwapchainSupport()
//        {
//            PhysicalDevice.GetSurfaceCapabilities(Surface, out SurfaceCapabilities capabilities);
//            SwapchainSupportDetails details = new SwapchainSupportDetails
//            {
//                formats = PhysicalDevice.GetSurfaceFormats(Surface),
//                presentModes = PhysicalDevice.GetSurfacePresentModes(Surface),
//                capabilities = capabilities,
//            };//BALAKASHHABADNSAKDHSADLA
//            return details;
//        }

//        void PickPhysicalDevice()
//        {
//            PhysicalDevice[] devices = NativeInstance.PhysicalDevices;
//            KeyValuePair<GPURating, PhysicalDevice> bestDevice = new KeyValuePair<GPURating, PhysicalDevice>();
//            foreach (var device in devices)
//            {
//                GPURating rating = RateDevice(device);
//                if (rating == GPURating.Unsuitable)
//                    continue;

//                if (rating > bestDevice.Key)
//                    bestDevice = new KeyValuePair<GPURating, PhysicalDevice>(rating, device);
//            }
//            PhysicalDevice = bestDevice.Value;

//            if (PhysicalDevice == PhysicalDevice.Null)
//                throw new Exception("No suitable graphics card found!");
//        }
        
//        GPURating RateDevice(PhysicalDevice device)
//        {
//            bool extensionsSupported = DeviceExtensionsSupported(device);
//            if (extensionsSupported)
//            {
//                QueueFamilyIndices queueFamilyIndices = FindQueueFamilies(device);
//                if (queueFamilyIndices.IsComplete)
//                {
//                    GraphicsQueueFamilyIndex = (uint)queueFamilyIndices.graphicsFamily;
//                    PresentationQueueFamilyIndex = (uint)queueFamilyIndices.presentFamily;

//                    if (queueFamilyIndices.SingleQueue)
//                        return GPURating.HasSingleQueue;

//                    return GPURating.Suitable;
//                }
//            }
//            return GPURating.Unsuitable;
//        }

//        bool DeviceExtensionsSupported(PhysicalDevice device)
//        {
//            ExtensionProperties[] extensions = device.GetDeviceExtensionProperties();
//            foreach (string extensionName in DeviceExtensionNames)
//            {
//                if (!extensions.Any((e) => extensionName == Marshal.PtrToStringAnsi(new IntPtr(&e.ExtensionName))))
//                    return false;
//            }
//            return true;
//        }
        
//        QueueFamilyIndices FindQueueFamilies(PhysicalDevice device)
//        {
//            QueueFamilyIndices indices = QueueFamilyIndices.Default;

//            QueueFamilyProperties[] queueFamilies = device.QueueFamilyProperties;
//            for (int i = 0; i < queueFamilies.Length; i++)
//            {
//                RawBool supportsPresentation = device.GetSurfaceSupport((uint)i, Surface);
//                if (supportsPresentation)
//                    indices.presentFamily = i;

//                QueueFamilyProperties queueFamily = queueFamilies[i];
//                if (queueFamily.QueueCount > 0 && (queueFamily.QueueFlags & QueueFlags.Graphics) == 0)
//                    indices.graphicsFamily = i;

//                if (indices.IsComplete)
//                    break;
//            }
//            return indices;
//        }
        
//        public void Dispose()
//        {
//            if (EnableValidationLayers)
//                NativeInstance.DestroyDebugReportCallback(debugReportCallback);

//            NativeInstance.DestroySurface(Surface);
//            NativeInstance.Destroy();
//            GC.SuppressFinalize(this);
//        }

//        ~VulkanInstance()
//        {
//            Dispose();
//        }
//    }
//}