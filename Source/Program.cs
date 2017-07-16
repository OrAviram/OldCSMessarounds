﻿using System;
using SDL2;
using SharpVulkan;
using Version = SharpVulkan.Version;

namespace LearningCSharp
{
    static class Program
    {
        static unsafe void Main()
        {
            InitSDL();

            VulkanRenderer renderer = new VulkanRenderer("Vulkan Sandbox", new Version(1, 0, 0), "Unknown Engine", new Version(1, 0, 0));

            renderer.Dispose();
            Console.ReadKey();

            //Window window = new Window(1200, 700, "Vulkan Sandbox");
            //using (VulkanInstance instance = new VulkanInstance("Vulkan Sandbox", new Version(1, 0, 0), "Test Vulkan Engine", new Version(1, 0, 0), window))
            //using (LogicalDevice device = new LogicalDevice(instance))
            //{
            //    while (!window.IsClosed)
            //    {
            //        while (SDL.SDL_PollEvent(out SDL.SDL_Event e) != 0)
            //        {
            //            SDL.SDL_WindowEvent winEvent = e.window;
            //            if (winEvent.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE)
            //                Window.FromID(winEvent.windowID).Close();
            //        }
            //    }
            //}
            //QuitSDL();
        }

        static void InitSDL()
        {
            int result = SDL.SDL_Init(SDL.SDL_INIT_VIDEO);
            if (result < 0)
                throw new Exception("Faild to initialize SDL! Message: " + SDL.SDL_GetError());
        }

        static void QuitSDL()
        {
            SDL.SDL_Quit();
        }
    }
}