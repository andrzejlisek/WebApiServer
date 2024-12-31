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

        public static void IdleTimerTick()
        {
            if (CommandArgs.Debug == 2)
            {
                Console.WriteLine("Conn IdleTimer tick begin");
            }
            List<int> TimeoutId = new List<int>();
            foreach (KeyValuePair<int, ApiConn> item in ApiConn_)
            {
                item.Value.IdleCounter++;
                if ((CommandArgs.Timeout > 0) && (item.Value.IdleCounter >= CommandArgs.Timeout))
                {
                    TimeoutId.Add(item.Key);
                }
                if (CommandArgs.Debug == 2)
                {
                    Console.WriteLine("Conn " + item.Key + " idle: " + item.Value.IdleCounter);
                }
            }
            if (TimeoutId.Count > 0)
            {
                KeyValue XI = new KeyValue();
                KeyValue XO = new KeyValue();
                for (int i = 0; i < TimeoutId.Count; i++)
                {
                    XI.ParamSet("ConnId", TimeoutId[i]);
                    ConnClose(XI, XO);
                    if (CommandArgs.Debug == 2)
                    {
                        Console.WriteLine("Conn " + TimeoutId[i] + " closed");
                    }
                }
            }
            if (CommandArgs.Debug == 2)
            {
                Console.WriteLine("Conn IdleTimer tick end");
            }
        }

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
            __.IdleCounter = 0;
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
                __.IdleCounter = 0;
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
                __.IdleCounter = 0;
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
                __.IdleCounter = 0;
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

        public int IdleCounter = 0;

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
