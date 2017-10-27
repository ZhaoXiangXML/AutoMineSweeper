using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Remote;
using System;
using System.Drawing;
using System.IO;

namespace AutoMineSweeper
{
    public sealed class Operator : IDisposable
    {
        private static readonly Operator instance = new Operator();

        private Operator()
        {
        }

        public static Operator Instance
        {
            get
            {
                return instance;
            }
        }

        public void Dispose()
        {
            GameSession.Dispose();
        }

        private WindowsDriver<WindowsElement> GameSession { get; set; }

        private WindowsElement RootElement { get; set; }

        public void Init()
        {
            DesiredCapabilities desktopCapabilities = new DesiredCapabilities();
            desktopCapabilities.SetCapability("app", "Root");
            var desktopSession = new WindowsDriver<WindowsElement>(new Uri("http://127.0.0.1:4723"), desktopCapabilities);

            var gameWindow = desktopSession.FindElementByName("Minesweeper");


            var gameWindowHandle = gameWindow.GetAttribute("NativeWindowHandle");
            gameWindowHandle = (int.Parse(gameWindowHandle)).ToString("x"); // Convert to Hex

            // Create session by attaching to game window
            DesiredCapabilities appCapabilities = new DesiredCapabilities();
            appCapabilities.SetCapability("appTopLevelWindow", gameWindowHandle);
            GameSession = new WindowsDriver<WindowsElement>(new Uri("http://127.0.0.1:4723"), appCapabilities);

            desktopSession.Dispose();

            RootElement = GameSession.FindElementByName("Minesweeper");
        }

        public void LeftClick(int x, int y)
        {
            new Actions(GameSession)
                .MoveToElement(RootElement, x, y)
                .Click()
                .Build()
                .Perform();
        }

        public void RightClick(int x, int y)
        {
            new Actions(GameSession)
                .MoveToElement(RootElement, x, y)
                .ContextClick()
                .Build()
                .Perform();
        }

        public void DoubleClick(int x, int y)
        {
            new Actions(GameSession)
                .MoveToElement(RootElement, x, y)
                .ClickAndHold()
                .ContextClick()
                .Release()
                .Build()
                .Perform();
        }

        public Bitmap GetScreenshot()
        {
            Bitmap screenshot;

            using (var ms = new MemoryStream(GameSession.GetScreenshot().AsByteArray))
            {
                screenshot = new Bitmap(ms);
            }
            return screenshot;
        }

        public IntPtr GetWindowsHandle()
        {
            return (IntPtr)Convert.ToUInt32(GameSession.CurrentWindowHandle, 16);
        }
    }
}
