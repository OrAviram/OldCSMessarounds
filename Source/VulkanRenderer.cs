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

        private VulkanDebugger debugger;
        private Window mainWindow;

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
            Swapchain = new VulkanSwapchain(LogicalDevice, Surface);
        }

        public void Dispose()
        {
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