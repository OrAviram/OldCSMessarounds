using System;
using System.Drawing;
using System.Collections.Generic;
using SDL2;

namespace LearningCSharp
{
    public class Window
    {
        private static Dictionary<IntPtr, Window> windows = new Dictionary<IntPtr, Window>();

        #region Window properties.
        public Size Size
        {
            get
            {
                SDL.SDL_GetWindowSize(SDLHandle, out int width, out int height);
                return new Size(width, height);
            }
            set { SDL.SDL_SetWindowSize(SDLHandle, value.Width, value.Height); }
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
            get { return SDL.SDL_GetWindowTitle(SDLHandle); }
            set { SDL.SDL_SetWindowTitle(SDLHandle, value); }
        }

        public uint ID => SDL.SDL_GetWindowID(SDLHandle);
        #endregion

        public IntPtr Win32Handle => systemInfo.info.win.window;

        public IntPtr SDLHandle { get; private set; }
        public bool IsClosed { get; private set; }

        private SDL.SDL_SysWMinfo systemInfo;

        private Window() { }

        public Window(int width, int height, string title)
        {
            SDLHandle = SDL.SDL_CreateWindow(title, SDL.SDL_WINDOWPOS_UNDEFINED, SDL.SDL_WINDOWPOS_UNDEFINED, width, height, SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN | SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE);
            if (SDLHandle == IntPtr.Zero)
                throw new Exception("Failed to create SDL window! Message: " + SDL.SDL_GetError());

            SDL.SDL_GetWindowWMInfo(SDLHandle, ref systemInfo);
            windows.Add(SDLHandle, this);
        }

        public static Window FromHandle(IntPtr handle)
        {
            if (windows.ContainsKey(handle))
                return windows[handle];
            else
            {
                Window win = new Window
                {
                    SDLHandle = handle,
                };
                SDL.SDL_GetWindowWMInfo(win.SDLHandle, ref win.systemInfo);
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
            SDL.SDL_ShowWindow(SDLHandle);
        }

        public void Hide()
        {
            SDL.SDL_HideWindow(SDLHandle);
        }

        public void Close()
        {
            SDL.SDL_DestroyWindow(SDLHandle);
            IsClosed = true;
        }
    }
}