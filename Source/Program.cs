using System;
using System.Linq;
using System.Threading;
using System.Runtime.InteropServices;
using SDL2;
using SharpVulkan;
using Version = SharpVulkan.Version;

namespace LearningCSharp
{
    struct QueueFamilyIndices
    {
        public const uint INVALID_INDEX = uint.MaxValue;
        public static QueueFamilyIndices Invalid { get; } = new QueueFamilyIndices { graphicsFamily = INVALID_INDEX };

        public uint graphicsFamily;
        public bool IsValid => graphicsFamily != INVALID_INDEX;
    }

    static unsafe class Program
    {
        delegate void DebugReportCallbackDel(DebugReportFlags flags, DebugReportObjectType objectType, ulong obj, PointerSize location, int code, string layerPrefix, string message, IntPtr userData);

        static Instance instance;
        static DebugReportCallback debugReportCallback;
        static PhysicalDevice physicalDevice;
        static QueueFamilyIndices queueFamilyIndices;

        static readonly string[] extensions = new string[] { "VK_EXT_debug_report" };
        static readonly string[] validationLayers = new string[] { "VK_LAYER_LUNARG_standard_validation" };

        static void Main()
        {
            IntPtr window = SDL.SDL_CreateWindow("Vulkan Sandbox", SDL.SDL_WINDOWPOS_UNDEFINED, SDL.SDL_WINDOWPOS_UNDEFINED, 1000, 500, SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN);
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
            ChoosePhysicalDevice();
        }

        static void Deinitialize()
        {
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

            int currentExtensionOrLayerIndex = 0;
            ExtensionProperties[] availableExtensionProperties = Vulkan.GetInstanceExtensionProperties();
            IntPtr[] availableExtensions = new IntPtr[extensions.Length];
            for (int i = 0; i < availableExtensionProperties.Length; i++)
            {
                fixed (void* extension = &availableExtensionProperties[i].ExtensionName.Value0)
                {
                    string name = Marshal.PtrToStringAnsi((IntPtr)extension);
                    if (extensions.Contains(name))
                        availableExtensions[currentExtensionOrLayerIndex++] = (IntPtr)extension;
                }
            }
            if (availableExtensions.Contains(IntPtr.Zero))
                throw new Exception("Not all extensions supported!");

            currentExtensionOrLayerIndex = 0;
            LayerProperties[] availableValidationLayerProperties = Vulkan.InstanceLayerProperties;
            IntPtr[] availableValidationLayers = new IntPtr[validationLayers.Length];
            for (int i = 0; i < availableValidationLayerProperties.Length; i++)
            {
                fixed (void* layer = &availableValidationLayerProperties[i].LayerName.Value0)
                {
                    string name = Marshal.PtrToStringAnsi((IntPtr)layer);
                    if (validationLayers.Contains(name))
                        availableValidationLayers[currentExtensionOrLayerIndex++] = (IntPtr)layer;
                }
            }
            if (availableValidationLayers.Contains(IntPtr.Zero))
                throw new Exception("Not all validation layers supported!");

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

        static void ChoosePhysicalDevice()
        {
            PhysicalDevice[] physicalDevices = instance.PhysicalDevices;
            for (int physicalDeviceIndex = 0; physicalDeviceIndex < physicalDevices.Length; physicalDeviceIndex++)
            {
                physicalDevice = physicalDevices[physicalDeviceIndex];
                QueueFamilyProperties[] queueFamilies = physicalDevice.QueueFamilyProperties;
                for (uint queueFamily = 0; queueFamily < queueFamilies.Length; queueFamily++)
                {
                    if (queueFamilies[queueFamily].QueueFlags.HasFlag(QueueFlags.Graphics))
                        queueFamilyIndices.graphicsFamily = queueFamily;
                }
                if (queueFamilyIndices.IsValid)
                    break;
            }
            if (!queueFamilyIndices.IsValid)
                throw new Exception("No suitable physical device found!");
        }
    }
}