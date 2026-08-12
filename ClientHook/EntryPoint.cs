using System;
using System.Threading;
using EasyHook;

namespace ClientHook
{
    public class Main : IEntryPoint
    {
        private HookInterface _interface;
        private ClientConfig _cfg;

        public Main(RemoteHooking.IContext context, string channelName)
        {            
        }

        public void Run(RemoteHooking.IContext context, string channelName)
        {
            // Connect to host IPC channel
            _interface = RemoteHooking.IpcConnectClient<HookInterface>(channelName);
            // Initialize logging with the interface
            Log.Init(_interface);
            Log.Info("[Entry] ClientHook DLL loaded (C# EasyHook)");

            _cfg = ClientConfig.Load();  
            Log.Info("[HookManager] Installing early hooks...");
            ConnectHook.Install(_cfg);
            WebsiteHook.Install(_cfg);
            FlashHook.Install();

            // Keep thread alive
            try
            {
                while (true) Thread.Sleep(100);
            }
            catch
            {
                
            }
            ConnectHook.UnInstall();
            WebsiteHook.UnInstall();
            FlashHook.UnInstall();
            // Finalise cleanup of hooks
            LocalHook.Release();
        }
    }
}
