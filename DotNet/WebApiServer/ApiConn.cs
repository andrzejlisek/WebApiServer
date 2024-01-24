using System;
using System.Collections.Generic;
using System.Threading;

namespace WebApiServer
{
    public class ApiConn
    {
        static ApiConn()
        {
            ApiConn_ = new Dictionary<int, ApiConn>();
        }

        static Dictionary<int, ApiConn> ApiConn_;
        static int ApiConnN = 0;

        public static void ConnOpen(KeyValue MessageI, KeyValue MessageO, ConnInstance ConnInstance_)
        {
            string Address = MessageI.ParamGetS("Address");
            bool Push_ = MessageI.ParamGetB("Push");
            string Type = MessageI.ParamGetS("Type");
            ApiConn __;
            switch (Type.ToUpperInvariant())
            {
                default:
                    __ = new ApiConnNet();
                    break;
                case "CMD":
                    __ = new ApiConnApp();
                    break;
            }
            ApiConnN++;
            __.ConnId = ApiConnN;
            __.ConnInstance_ = ConnInstance_;
            ApiConn_.Add(ApiConnN, __);
            MessageO.ParamSet("ConnId", ApiConnN);
            try
            {
                __.Open(Address, Push_);
            }
            catch (Exception E)
            {
                MessageO.ParamSet("FileId", 0);
                MainClass.CatchError(MessageO, E);
            }
        }

        public static void ConnInfo(KeyValue MessageI, KeyValue MessageO)
        {
            int ConnId = MessageI.ParamGetI("ConnId");
            MessageO.ParamSet("ConnId", ConnId);
            if (ApiConn_.ContainsKey(ConnId))
            {
                ApiConn __ = ApiConn_[ConnId];
                MessageO.ParamSet("Status", __.Status());
                MessageO.ParamSet("Push", __.Push);
            }
            else
            {
                MessageO.ParamSet("Status", -1);
                MessageO.ParamSet("Push", false);
            }
        }

        public static void ConnClose(KeyValue MessageI, KeyValue MessageO)
        {
            int ConnId = MessageI.ParamGetI("ConnId");
            MessageO.ParamSet("ConnId", ConnId);
            if (ApiConn_.ContainsKey(ConnId))
            {
                ApiConn __ = ApiConn_[ConnId];
                try
                {
                    __.Close();
                }
                catch (Exception E)
                {
                    MainClass.CatchError(MessageO, E);
                }
                ApiConn_.Remove(ConnId);
            }
        }

        public static void ConnSend(KeyValue MessageI, KeyValue MessageO)
        {
            int ConnId = MessageI.ParamGetI("ConnId");
            string Data = MessageI.ParamGetS("Data");
            MessageO.ParamSet("ConnId", ConnId);
            if (ApiConn_.ContainsKey(ConnId))
            {
                ApiConn __ = ApiConn_[ConnId];
                try
                {
                    __.Send(KeyValue.BinaryDecode(Data));
                }
                catch (Exception E)
                {
                    MainClass.CatchError(MessageO, E);
                }
            }
        }

        public static void ConnRecv(KeyValue MessageI, KeyValue MessageO)
        {
            int ConnId = MessageI.ParamGetI("ConnId");
            if (ApiConn_.ContainsKey(ConnId))
            {
                ApiConn __ = ApiConn_[ConnId];
                try
                {
                    byte[] Temp = __.Recv();
                    MessageO.ParamSet("Data", KeyValue.BinaryEncode(Temp));
                }
                catch (Exception E)
                {
                    MainClass.CatchError(MessageO, E);
                }
            }
        }

        protected int ConnId = 0;
        protected ConnInstance ConnInstance_;

        protected Mutex Mtx = new Mutex();

        protected bool Push = false;

        protected List<byte[]> RecvBuf = new List<byte[]>();
        protected int RecvBufL = 0;

        public virtual void Open(string Address, bool Push_)
        {
            return;
        }

        public virtual int Status()
        {
            return 0;
        }

        public virtual void Close()
        {
            return;
        }

        public virtual void Send(byte[] Data)
        {
            return;
        }

        public virtual byte[] Recv()
        {
            Mtx.WaitOne();
            byte[] Data = new byte[RecvBufL];
            int Offset = 0;
            for (int I = 0; I < RecvBuf.Count; I++)
            {
                byte[] Item = RecvBuf[I];
                Array.Copy(Item, 0, Data, Offset, Item.Length);
                Offset += Item.Length;
            }
            RecvBuf.Clear();
            RecvBufL = 0;
            Mtx.ReleaseMutex();
            return Data;
        }

        protected void RecvProcess(byte[] Data, int DataLength)
        {
            if (DataLength > 0)
            {
                Mtx.WaitOne();
                byte[] Data_ = new byte[DataLength];
                Array.Copy(Data, 0, Data_, 0, DataLength);
                if (Push)
                {
                    KeyValue MsgX = KeyValue.PoolAssign();
                    MsgX.ParamClear();
                    MsgX.ParamSet("Type", "Connection");
                    MsgX.ParamSet("ConnId", ConnId);
                    MsgX.ParamSet("Error", "");
                    MsgX.ParamSet("Data", Data_);
                    ConnInstance_.Push(MsgX);
                    KeyValue.PoolRelease(MsgX);
                }
                else
                {
                    RecvBuf.Add(Data_);
                    RecvBufL += DataLength;
                }
                Mtx.ReleaseMutex();
            }
        }
    }
}
