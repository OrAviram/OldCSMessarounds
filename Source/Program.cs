using System;
using System.Numerics;
using SDL2;
using SharpVulkan;

namespace LearningCSharp
{
    static class Program
    {
        static void Main()
        {
            IntPtr window = SDL.SDL_CreateWindow("Vulkan Sandbox", SDL.SDL_WINDOWPOS_UNDEFINED, SDL.SDL_WINDOWPOS_UNDEFINED, 1000, 500, SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN);
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
                }
            }
            SDL.SDL_DestroyWindow(window);
        }
    }
}