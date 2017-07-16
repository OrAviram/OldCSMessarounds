using System;
using SDL2;
using Version = SharpVulkan.Version;

namespace LearningCSharp
{
    static class Program
    {
        static unsafe void Main()
        {
            InitSDL();
            using (VulkanRenderer renderer = new VulkanRenderer("Vulkan Sandbox", new Version(1, 0, 0), "Unknown Engine", new Version(1, 0, 0)))
            {
                Window window = new Window(1200, 700, "Vulkan Sandbox", renderer);
                while (!window.IsClosed)
                {
                    while (SDL.SDL_PollEvent(out SDL.SDL_Event e) != 0)
                    {
                        SDL.SDL_WindowEvent winEvent = e.window;
                        if (winEvent.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE)
                            Window.FromID(winEvent.windowID).Close();
                    }
                }
            }
            QuitSDL();
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