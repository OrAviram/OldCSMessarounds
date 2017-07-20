using System;
using System.Numerics;
using System.Threading;
using SDL2;
using Version = SharpVulkan.Version;

namespace LearningCSharp
{
    static class Program
    {
        static Window window;
        static VulkanRenderer renderer;

        static unsafe void Main()
        {
            InitSDL();

            const float HEIGHT = 1;

            Triangle triangleA = new Triangle(
                new Vertex(new Vector2(-.5f, -HEIGHT), Vector3.UnitX),
                new Vertex(new Vector2(0, HEIGHT), Vector3.UnitY),
                new Vertex(new Vector2(-1, HEIGHT), Vector3.UnitZ));

            Triangle triangleB = new Triangle(
                new Vertex(new Vector2(.5f, -HEIGHT), Vector3.UnitX),
                new Vertex(new Vector2(1, HEIGHT), Vector3.UnitZ),
                new Vertex(new Vector2(0, HEIGHT), Vector3.UnitY));

            Triangle triangleC = new Triangle(
                new Vertex(new Vector2(0, -.5f), Vector3.One),
                new Vertex(new Vector2(1, 1), Vector3.One),
                new Vertex(new Vector2(-1, 1), Vector3.One));

            window = new Window(1200, 700, "Vulkan Sandbox");
            using (renderer = new VulkanRenderer("Vulkan Sandbox", new Version(1, 0, 0), "Unknown Engine", new Version(1, 0, 0), window, new Triangle[] { triangleC, triangleA, triangleB }))
            {
                while (!window.IsClosed)
                {
                    renderer.DrawFrame();
                    while (SDL.SDL_PollEvent(out SDL.SDL_Event e) != 0)
                    {
                        SDL.SDL_WindowEvent winEvent = e.window;
                        if (winEvent.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE)
                            Window.FromID(winEvent.windowID).Close();
                        else if (winEvent.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_RESIZED)
                        {
                            renderer.RecreateSwapchain();
                            renderer.RecreateSwapchain();
                        }
                    }
                    Thread.Sleep(10);
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