﻿using System;
using System.Collections.Generic;
using System.Net.Sockets;







namespace Solarmax
{
    public class TCPConnector : INetConnector
    {
        /// <summary>
        /// 网络socket
        /// </summary>
        private TcpClient           mSocket;

        /// <summary>
        /// 缓冲区
        /// </summary> 
        private NetStream           mNetStream;
        // 临时解析
        private int                 tempReadPacketLength;
        private int                 tempReadPacketType;
        private Byte[]              tempReadPacketData;

        private AsyncCallback       mReadCompleteCallback;
        private AsyncCallback       mSendCompleteCallback;

        /// <summary>
        /// 发送线程
        /// </summary>
        private AsyncThread         mSendThread = null;

        public TCPConnector() : base()
        {
            mSocket                 = null;
            mNetStream              = new NetStream(INetConnector.MAX_SOCKET_BUFFER_SIZE * 2);
            tempReadPacketLength    = 0;
            tempReadPacketType      = 0;
            tempReadPacketData      = null;

            mReadCompleteCallback   = new AsyncCallback(ReadComplete);
            mSendCompleteCallback   = new AsyncCallback(SendComplete);

            mSendThread             = new AsyncThread(SendLogic);
            mSendThread.Start();
        }

        public override bool Init()
        {
            base.Init();

            return true;
        }

        public override void Tick(float interval)
        {
            base.Tick(interval);

            doDecodeMessage();
        }

        public override void Destroy()
        {
            base.Destroy();

            mSendThread.Stop();

            DisConnect();
        }

        public override ConnectionType GetConnectionType()
        {
            return ConnectionType.TCP;
        }

        public override void Connect(string address, int port)
		{
			SetConnectStatus(ConnectionStatus.CONNECTING);
            base.Connect(address, port);
			mSocket = new TcpClient();
			AsyncThread connectThread = new AsyncThread ((thread) => {
				
				try
				{
					mSocket.NoDelay = true; // set the nodelay mark.
					mSocket.Connect(mRemoteHost.GetAddress(), mRemoteHost.GetPort());
				    mSocket.GetStream().BeginRead(mNetStream.AsyncPipeIn, 0, INetConnector.MAX_SOCKET_BUFFER_SIZE, mReadCompleteCallback, this);
                    SetConnectStatus(ConnectionStatus.CONNECTED);
                }
				catch(Exception e)
				{
					LoggerSystem.Instance.Error(e.Message);
					SetConnectStatus(ConnectionStatus.ERROR);
				}
                
				CallbackConnected(IsConnected());
			});

			connectThread.Start ();
        }

        public override void SendPacket(IPacket packet)
        {
            Byte[] buffer = null;
            mPacketFormat.GenerateBuffer(ref buffer, packet);

            mNetStream.PushOutStream(buffer);
        }

        public override void DisConnect()
        {
            if (IsConnected())
			{
				SetConnectStatus(ConnectionStatus.DISCONNECTED);
                mSocket.GetStream().Close();
                mSocket.Close();
				mSocket = null;
				mNetStream.Clear();

                CallbackDisconnected();
            }

        }

        private void ReadComplete(IAsyncResult ar)
        {
			if (!IsConnected ())
				return;

            int readLength = 0;
            try
            {
                readLength = mSocket.GetStream().EndRead(ar);
                //LoggerSystem.Instance.Info("读取到数据字节数:" + readLength);
                if (readLength > 0)
                {
                    mNetStream.FinishedIn(readLength);
                    mSocket.GetStream().BeginRead(mNetStream.AsyncPipeIn, 0, INetConnector.MAX_SOCKET_BUFFER_SIZE, mReadCompleteCallback, this);
                }
                else
                {
                    // error
                    LoggerSystem.Instance.Error("读取数据为0，将要断开此链接接:" + mRemoteHost.ToString());
                    DisConnect();
                }
            }
            catch (Exception e)
            {
                LoggerSystem.Instance.Error("链接：" + mRemoteHost.ToString() + ", 发生读取错误：" + e.Message);
                DisConnect();
            }
        }

        private void SendComplete(IAsyncResult ar)
        {
			if (!IsConnected ())
				return;

            int sendLength = 0;
            try
            {
                mSocket.GetStream().EndWrite(ar);
                sendLength = (int)ar.AsyncState;
                //LoggerSystem.Instance.Info("发送数据字节数：" + sendLength);
                if (sendLength > 0)
                {
					mNetStream.FinishedOut (sendLength);
                }
				else
				{
					// error
					LoggerSystem.Instance.Error("发送数据为0，将要断开此链接接:" + mRemoteHost.ToString());
					DisConnect();
				}
            }
            catch (Exception e)
            {
                LoggerSystem.Instance.Error("发生写入错误：" + e.Message);
                DisConnect();
            }
        }

        private void doDecodeMessage()
        {
            while (mNetStream.InStreamLength > 0 && mPacketFormat.CheckHavePacket(mNetStream.InStream, mNetStream.InStreamLength))
            {
                // 开始读取
                mPacketFormat.DecodePacket(mNetStream.InStream, ref tempReadPacketLength, ref tempReadPacketType, ref tempReadPacketData);

                mPacketHandlerManager.DispatchHandler(tempReadPacketType, tempReadPacketData);

                CallbackRecieved(tempReadPacketType, tempReadPacketData);

                // 偏移
                mNetStream.PopInStream(tempReadPacketLength);
            }
        }

        private void SendLogic(AsyncThread thread)
        {
            while (thread.IsWorking())
            {
                doSendMessage();

                System.Threading.Thread.Sleep(20);
            }
        }

        private void doSendMessage()
        {
            int length = mNetStream.OutStreamLength;
            if (IsConnected() && mNetStream.AsyncPipeOutIdle && length > 0 && mSocket.GetStream().CanWrite)
            {
                try
                {
                    mSocket.GetStream().BeginWrite(mNetStream.AsyncPipeOut, 0, length, mSendCompleteCallback, length);
                }
                catch (Exception e)
                {
                    LoggerSystem.Instance.Error("发送数据错误：" + e.Message);
                    DisConnect();
                }
            }
        }
    }
}
