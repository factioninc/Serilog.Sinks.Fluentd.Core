using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Serilog.fluentd
{
    public class FluentdHandler : IDisposable
    {

        public class FluentdHandlerSettings
        {
            public string Tag { get; set; }
            public IPAddress Host { get; set; }
            public int Port { get; set; }
            public int MaxBuffer { get; set; } //TODO: for when we do retries
            public int Timeout { get; set; }
            public int RetryInterval { get; set; } //TODO: for when we do retries

        }

        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);
        private FluentdHandlerSettings Settings { get; }
        private TcpClient Client { get; set; }
        private MessagePacker MessagePacker { get; set; }
        public byte[] queue { get; set; }

        private FluentdHandler(FluentdHandlerSettings settings)
        {
            this.Settings = settings;
        }

        public static async Task<FluentdHandler> CreateHandler(string tag, FluentdHandlerSettings settings)
        {
            var handler = new FluentdHandler(settings);
            await handler.Connect();
            return handler;
        }

        private async Task Connect()
        {
            if (this.Client != null)
            {
                this.Client = new TcpClient();
                this.Client.SendTimeout = this.Settings.Timeout;
                await this.Client.ConnectAsync(this.Settings.Host, this.Settings.Port);
            }
        }

        public async Task Emit(string label, params object[] obj)
        {
            var packed = MessagePacker.MakePacket(label, DateTime.Now, obj);
            await Send(packed);
        }

        private async Task Send(byte[] packedMessage)
        {
            await semaphore.WaitAsync();

            try
            {
                if (this.queue != null)
                {
                    var temp = new byte[queue.Length + packedMessage.Length];
                    Buffer.BlockCopy(queue, 0, temp, 0, queue.Length);
                    Buffer.BlockCopy(packedMessage, 0, temp, queue.Length, packedMessage.Length);

                    this.queue = temp;
                    packedMessage = temp;
                }

                if (!packedMessage.Any())
                {
                    return;
                }

                try {
                    await Connect();
                    await this.Client.GetStream().WriteAsync(packedMessage, 0, packedMessage.Length);
                    await this.Client.GetStream().FlushAsync();
                    queue = null;
                } catch {
                    Disconnect();
                    //TODO: Retry
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        private void Disconnect()
        {
            this.Client = null;
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}