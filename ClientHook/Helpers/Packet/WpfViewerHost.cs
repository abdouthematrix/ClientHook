using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace ClientHook
{
    /// <summary>
    /// Hosts a dedicated STA thread with its own WPF Application/Dispatcher
    /// for PacketViewerWindow. Ensures safe UI updates from hook detours.
    /// </summary>
    internal static class WpfViewerHost
    {
        private static readonly object _lock = new object();
        private static Dispatcher _dispatcher;
        private static PacketViewerWindow _window;

        /// <summary>
        /// Queues a captured packet to be shown in the viewer.
        /// Safe to call from PacketHook detours.
        /// </summary>
        public static void ShowPacket(PacketDirection direction, byte[] data)
        {
            EnsureStarted();
            _dispatcher.InvokeAsync(() => _window.AddPacket(direction, data));
        }

        private static void EnsureStarted()
        {
            if (_dispatcher != null)
            {
                return;
            }

            lock (_lock)
            {
                if (_dispatcher != null)
                {
                    return;
                }

                var ready = new ManualResetEventSlim(false);

                var thread = new Thread(() =>
                {
                    // Create WPF Application if none exists
                    if (Application.Current == null)
                    {
                        new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                    }

                    _window = new PacketViewerWindow();
                    _dispatcher = Dispatcher.CurrentDispatcher;
                    ready.Set();

                    Dispatcher.Run();
                });

                thread.SetApartmentState(ApartmentState.STA);
                thread.IsBackground = true;
                thread.Name = "ClientHook Packet Viewer";
                thread.Start();

                ready.Wait();
            }
        }
    }
}
