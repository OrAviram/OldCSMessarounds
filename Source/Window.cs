using System;
using System.Drawing;
using System.Collections.Generic;
using SDL2;

namespace LearningCSharp
{
    public class Window
    {
        private static Dictionary<IntPtr, Window> windows = new Dictionary<IntPtr, Window>();
        private static VulkanRenderer defaultRenderer;

        #region Window properties.
        public Size Size
        {
            get
            {
                SDL.SDL_GetWindowSize(Handle, out int width, out int height);
                return new Size(width, height);
            }
            set { SDL.SDL_SetWindowSize(Handle, value.Width, value.Height); }
        }

        public int Width
        {
            get { return Size.Width; }
            set { Size = new Size(value, Size.Height); }
        }

        public int Height
        {
            get { return Size.Height; }
            set { Size = new Size(Size.Width, value); }
        }

        public string Title
        {
            get { return SDL.SDL_GetWindowTitle(Handle); }
            set { SDL.SDL_SetWindowTitle(Handle, value); }
        }

        public IntPtr Surface => SDL.SDL_GetWindowSurface(Handle);

        public uint ID => SDL.SDL_GetWindowID(Handle);
        #endregion

        public IntPtr Handle { get; private set; }
        public bool IsClosed { get; private set; }

        private VulkanRenderer renderer;

        private Window() { }

        public Window(int width, int height, string title, VulkanRenderer renderer)
        {
            this.renderer = renderer;
            if (defaultRenderer == null)
                defaultRenderer = renderer;

            Handle = SDL.SDL_CreateWindow(title, SDL.SDL_WINDOWPOS_UNDEFINED, SDL.SDL_WINDOWPOS_UNDEFINED, width, height, SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN);
            if (Handle == IntPtr.Zero)
                throw new Exception("Failed to create SDL window! Message: " + SDL.SDL_GetError());

            windows.Add(Handle, this);
        }

        public static Window FromHandle(IntPtr handle)
        {
            if (windows.ContainsKey(handle))
                return windows[handle];
            else
            {
                Window win = new Window
                {
                    Handle = handle,
                    renderer = defaultRenderer,
                };
                windows.Add(handle, win);
                return win;
            }
        }

        public static Window FromID(uint id)
        {
            return FromHandle(SDL.SDL_GetWindowFromID(id));
        }

        public void Show()
        {
            SDL.SDL_ShowWindow(Handle);
        }

        public void Hide()
        {
            SDL.SDL_HideWindow(Handle);
        }

        public void Close()
        {
            SDL.SDL_DestroyWindow(Handle);
            IsClosed = true;
        }
    }
}