/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package webapiserver;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.concurrent.locks.ReentrantLock;
import static webapiserver.ApiFile.ApiFile_;
import static webapiserver.ApiFile.FileClose;

public class ApiConn
{
    static
    {
        ApiConn_ = new HashMap<>();
    }
    
    static HashMap<Integer, ApiConn> ApiConn_;
    static int ApiConnN = 0;
    
    public static void IdleTimerTick()
    {
        if (CommandArgs.Debug == 2)
        {
            System.out.println("Conn IdleTimer tick begin");
        }
        ArrayList<Integer> TimeoutId = new ArrayList<Integer>();
        ApiConn_.forEach((Key, Value) -> {
            Value.IdleCounter++;
            if ((CommandArgs.Timeout > 0) && (Value.IdleCounter >= CommandArgs.Timeout))
            {
                TimeoutId.add(Key);
            }
            if (CommandArgs.Debug == 2)
            {
                System.out.println("Conn " + Key + " idle: " + Value.IdleCounter);
            }
        });
        if (TimeoutId.size() > 0)
        {
            KeyValue XI = new KeyValue();
            KeyValue XO = new KeyValue();
            for (int i = 0; i < TimeoutId.size(); i++)
            {
                XI.ParamSet("ConnId", TimeoutId.get(i));
                ConnClose(XI, XO);
                if (CommandArgs.Debug == 2)
                {
                    System.out.println("Conn " + TimeoutId.get(i) + " closed");
                }
            }
        }
        if (CommandArgs.Debug == 2)
        {
            System.out.println("Conn IdleTimer tick end");
        }
    }
    
    public static void ConnOpen(KeyValue MessageI, KeyValue MessageO, ConnInstance ConnInstance_)
    {
        String Address = MessageI.ParamGetS("Address");
        boolean Push_ = MessageI.ParamGetB("Push");
        String Type = MessageI.ParamGetS("Type");
        ApiConn __;
        switch (Type.toUpperCase())
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
        ApiConn_.put(ApiConnN, __);
        MessageO.ParamSet("ConnId", ApiConnN);
        try
        {
            __.Open(Address, Push_);
        }
        catch (Exception E)
        {
            MessageO.ParamSet("FileId", 0);
            WebApiServer.CatchError(MessageO, E);
        }
    }
    
    public static void ConnInfo(KeyValue MessageI, KeyValue MessageO)
    {
        int ConnId = MessageI.ParamGetI("ConnId");
        MessageO.ParamSet("ConnId", ConnId);
        if (ApiConn_.containsKey(ConnId))
        {
            ApiConn __ = ApiConn_.get(ConnId);
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
        if (ApiConn_.containsKey(ConnId))
        {
            ApiConn __ = ApiConn_.get(ConnId);
            try
            {
                __.Close();
            }
            catch (Exception E)
            {
                WebApiServer.CatchError(MessageO, E);
            }
            ApiConn_.remove(ConnId);
        }
    }

    public static void ConnSend(KeyValue MessageI, KeyValue MessageO)
    {
        int ConnId = MessageI.ParamGetI("ConnId");
        String Data = MessageI.ParamGetS("Data");
        MessageO.ParamSet("ConnId", ConnId);
        if (ApiConn_.containsKey(ConnId))
        {
            ApiConn __ = ApiConn_.get(ConnId);
            __.IdleCounter = 0;
            try
            {
                __.Send(KeyValue.BinaryDecode(Data));
            }
            catch (Exception E)
            {
                WebApiServer.CatchError(MessageO, E);
            }
        }
    }

    public static void ConnRecv(KeyValue MessageI, KeyValue MessageO)
    {
        int ConnId = MessageI.ParamGetI("ConnId");
        if (ApiConn_.containsKey(ConnId))
        {
            ApiConn __ = ApiConn_.get(ConnId);
            __.IdleCounter = 0;
            try
            {
                byte[] Temp = __.Recv();
                MessageO.ParamSet("Data", KeyValue.BinaryEncode(Temp));
            }
            catch (Exception E)
            {
                WebApiServer.CatchError(MessageO, E);
            }
        }
    }

    protected int ConnId = 0;
    protected ConnInstance ConnInstance_;
    
    protected ReentrantLock Mtx = new ReentrantLock(true);

    protected boolean Push = false;
    
    protected ArrayList<byte[]> RecvBuf = new ArrayList<>();
    protected int RecvBufL = 0;

    public int IdleCounter = 0;
    
    public void Open(String Address, boolean Push_) throws Exception
    {
        return;
    }
    
    public int Status()
    {
        return 0;
    }

    public void Close()
    {
        return;
    }

    public void Send(byte[] Data) throws Exception
    {
        return;
    }

    public byte[] Recv() throws Exception
    {
        Mtx.lock();
        byte[] Data = new byte[RecvBufL];
        int Offset = 0;
        for (int I = 0; I < RecvBuf.size(); I++)
        {
            byte[] Item = RecvBuf.get(I);
            System.arraycopy(Item, 0, Data, Offset, Item.length);
            Offset += Item.length;
        }
        RecvBuf.clear();
        RecvBufL = 0;
        Mtx.unlock();
        return Data;
    }
    
    protected void RecvProcess(byte[] Data, int DataLength)
    {
        if (DataLength > 0)
        {
            Mtx.lock();
            byte[] Data_ = new byte[DataLength];
            System.arraycopy(Data, 0, Data_, 0, DataLength);
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
                RecvBuf.add(Data_);
                RecvBufL += DataLength;
            }
            Mtx.unlock();
        }
    }
}
