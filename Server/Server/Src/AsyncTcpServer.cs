using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Tools
{
    public delegate byte[] AsyncMessageEvent(byte[] data);

    class AsyncTcpServer
    {
        private TcpListener mServer;
        private CancellationTokenSource mTokenSource;
        private CancellationToken mToken;

        private Action<TcpClient> onClientConnect;
        private AsyncMessageEvent onMessage;
        private List<Task> tasks;

        public AsyncTcpServer(IPAddress ip, int port)
        {
            mServer = new TcpListener(ip, port);            
        }

        public AsyncTcpServer OnConnect(Action<TcpClient> callback)
        {
            onClientConnect = callback;
            return this;
        }

        public AsyncTcpServer OnMessage(AsyncMessageEvent callback)
        {
            onMessage = callback;
            return this;
        }

        public async void Start(int bufSize = 8192)
        {
            tasks = new List<Task>();

            mTokenSource = CancellationTokenSource.CreateLinkedTokenSource(new CancellationToken());
            mToken = mTokenSource.Token;

            try
            {
                mServer.Start();

                while (!mToken.IsCancellationRequested)
                {
                    await Task.Run(async () =>
                    {
                        var client = mServer.AcceptTcpClientAsync();
                        var result = await client;

                        onClientConnect?.Invoke(result);

                        // Setup Task
                        Task task = Task.Run(async () =>
                        {
                            var buffer = new byte[bufSize];
                            var count = 0;

                            do
                            {
                                count = await result.GetStream().ReadAsync(buffer, 0, buffer.Length, mToken);
                                byte[] data = onMessage?.Invoke(buffer);
                                await result.GetStream().WriteAsync(data, 0, data.Length, mToken);

                                if (mToken.IsCancellationRequested)
                                    break;

                            } while (count > 0);

                        }, mToken);

                        // Setup OnCompleted
                        // TODO: Verify a better way of delete a task.
                        task.GetAwaiter().OnCompleted(() => {
                            tasks.Remove(task);
                        });

                        // Add To TaskList
                        tasks.Add(task);
                    }, mToken);                    
                }
                mServer.Stop();
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public void Stop()
        {            
            mTokenSource.Cancel();
            Task.WhenAll(tasks.ToArray());            
        }
    }
}
