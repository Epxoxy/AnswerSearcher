using System;
using System.Windows;
using System.Windows.Interop;

namespace AnswerSearcher
{
    using System;
    using System.Runtime.InteropServices;
    using System.Security;
    public class WindowHelper
    {
        private const int GwlExstyle = -20;
        private const int SwpFramechanged = 0x0020;
        private const int SwpNomove = 0x0002;
        private const int SwpNosize = 0x0001;
        private const int SwpNozorder = 0x0004;
        private const int WsExDlgmodalframe = 0x0001;

        public static readonly DependencyProperty ShowIconProperty =
          DependencyProperty.RegisterAttached(
            "ShowIcon",
            typeof(bool),
            typeof(WindowHelper),
            new FrameworkPropertyMetadata(true, new PropertyChangedCallback(OnShowIconChanged)));

        public static Boolean GetShowIcon(UIElement element)
        {
            return (Boolean)element.GetValue(ShowIconProperty);
        }

        public static void SetShowIcon(UIElement element, Boolean value)
        {
            element.SetValue(ShowIconProperty, value);
        }

        private static void OnShowIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var window = d as Window;
            if (window != null)
            {
                if (!(bool)e.NewValue)
                {
                    if (window.IsInitialized)
                        RemoveIcon(window);
                    else
                        window.SourceInitialized += delegate { RemoveIcon(window); };
                }
            }
        }

        private static void RemoveIcon(Window window)
        {
            // Get this window's handle
            var hwnd = new WindowInteropHelper(window).Handle;

            // Change the extended window style to not show a window icon
            int extendedStyle = UnsafeNativeMethods.GetWindowLong(hwnd, GwlExstyle);
            UnsafeNativeMethods.SetWindowLong(hwnd, GwlExstyle, extendedStyle | WsExDlgmodalframe);

            // Update the window's non-client area to reflect the changes
            UnsafeNativeMethods.SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SwpNomove |
              SwpNosize | SwpNozorder | SwpFramechanged);
        }

    }

    /// <devdoc>http://msdn.microsoft.com/en-us/library/ms182161.aspx</devdoc>
    [SuppressUnmanagedCodeSecurity]
    internal static class UnsafeNativeMethods
    {
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern IntPtr GetForegroundWindow();
        public static IntPtr SetClassLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            //check for x64
            if (IntPtr.Size > 4)
                return SetClassLongPtr64(hWnd, nIndex, dwNewLong);
            else
                return new IntPtr(SetClassLongPtr32(hWnd, nIndex, unchecked((uint)dwNewLong.ToInt32())));
        }
        [DllImport("user32.dll", EntryPoint = "SetClassLong")]
        public static extern uint SetClassLongPtr32(IntPtr hWnd, int nIndex, uint dwNewLong);
        [DllImport("user32.dll", EntryPoint = "SetClassLongPtr")]
        public static extern IntPtr SetClassLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
        [DllImport("user32.dll")]

        public static extern IntPtr GetSysColorBrush(int nIndex);
        public const int GWL_STYLE = -16;
        public const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        /// <devdoc>http://msdn.microsoft.com/en-us/library/windows/desktop/ms633545(v=vs.85).aspx</devdoc>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    }
}
