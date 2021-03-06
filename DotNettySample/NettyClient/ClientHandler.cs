﻿using DotNetty.Handlers.Flow;
using DotNetty.Transport.Channels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Handlers.Timeout;
using System.Net;
using System.Timers;

namespace NettyClient
{
    public class ClientHandler : FlowControlHandler
    {
        readonly IByteBuffer initialMessage;
        Timer timer;

        public ClientHandler()
        {
            this.initialMessage = Unpooled.Buffer(256);
            byte[] messageBytes = Encoding.UTF8.GetBytes("Hello world");
            this.initialMessage.WriteBytes(messageBytes);
        }

        public override void ChannelActive(IChannelHandlerContext context)
        {
            timer = null;
            MainWindow.SetText(@"--- Client is active ---");
            context.WriteAndFlushAsync(this.initialMessage);
        }

        public override void ChannelInactive(IChannelHandlerContext context)
        {
            //掉线了
            MainWindow.SetText(@"--- Client is inactive ---");
            //断线重连           
            timer = new System.Timers.Timer(4000);
            timer.Elapsed += new System.Timers.ElapsedEventHandler((s, x) =>
            {
                if (!context.Channel.Active)
                {
                   context.Channel.ConnectAsync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8007));
                }
            });
            timer.Enabled = true;
            timer.Start();
            base.ChannelInactive(context);
        }

        public override void ChannelRead(IChannelHandlerContext context, object msg)
        {
            var byteBuffer = msg as IByteBuffer;
            if (byteBuffer != null)
            {
                MainWindow.SetText("Received from server: " + byteBuffer.ToString(Encoding.UTF8));
            }
            //context.WriteAsync(msg);
        }

        public override void ChannelReadComplete(IChannelHandlerContext context) => context.Flush();

        public override void UserEventTriggered(IChannelHandlerContext context, object evt)
        {
            base.UserEventTriggered(context, evt);
            if (evt is IdleStateEvent)
            {
                var e = evt as IdleStateEvent;
                switch (e.State)
                {
                    //长期没收到服务器推送数据
                    case IdleState.ReaderIdle:
                        {
                            //可以重新连接
                            if (!context.Channel.Active)
                                context.ConnectAsync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8007));
                        }
                        break;
                    //长期未向服务器发送数据
                    case IdleState.WriterIdle:
                        {
                            //发送心跳包
                            byte[] messageBytes = Encoding.UTF8.GetBytes("heartbeat");
                            context.WriteAndFlushAsync(messageBytes);
                        }
                        break;
                    //All
                    case IdleState.AllIdle:
                        {

                        }
                        break;
                }
            }
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            MainWindow.SetText("Client Exception: " + exception);
            context.CloseAsync();
        }

        //发送心跳包
        private void sendHeartbeatPacket(IChannelHandlerContext context)
        {

        }
    }
}
