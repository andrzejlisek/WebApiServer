/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package webapiserver;

import java.io.InputStream;
import java.io.OutputStream;
import java.net.Socket;

/**
 *
 * @author xxx
 */
public class ApiConnNet extends ApiConn
{
    Thread LoopThr;
    Socket TCPC;
    InputStream NSXI;
    OutputStream NSXO;
    byte[] StreamBuf = new byte[1000000];
    
    public void Open(String Address, boolean Push_) throws Exception
    {
        if (!RegexTest.Match(Address, CommandArgs.RegNet))
        {
            throw new Exception("Network pattern mismatch \"" + Address + "\"");
        }
        RecvBuf.clear();
        RecvBufL = 0;
        Push = Push_;
        int Idx = Address.indexOf(':');
        TCPC = new Socket(Address.substring(0, Idx), Integer.parseInt(Address.substring(Idx + 1)));
        NSXI = TCPC.getInputStream();
        NSXO = TCPC.getOutputStream();
        
        LoopThr = new Thread(() -> { NetLoop(); });
        LoopThr.start();
    }

    public void NetLoop()
    {
        while (true)
        {
            try
            {
                int Avail = 0;
                try
                {
                    Avail = NSXI.read(StreamBuf, 0, StreamBuf.length);
                    if (Avail > 0)
                    {
                        RecvProcess(StreamBuf, Avail);
                    }
                    else
                    {
                        if (Avail < 0)
                        {
                            break;
                        }
                        if (Status() == 0)
                        {
                            break;
                        }
                    }
                }
                catch (Exception E)
                {
                }
            }
            catch (Exception E)
            {
                break;
            }
        }
    }

    public int Status()
    {
        if (NSXI == null)
        {
            return 0;
        }
        if (NSXO == null)
        {
            return 0;
        }
        if (TCPC == null)
        {
            return 0;
        }
        if ((TCPC.isConnected()) && (!TCPC.isClosed()))
        {
            return 1;
        }
        return 0;
    }

    public void Close()
    {
        try
        {
            TCPC.close();
        }
        catch (Exception E)
        {
            
        }
        TCPC = null;
    }

    public void Send(byte[] Data) throws Exception
    {
        try
        {
            NSXO.write(Data, 0, Data.length);
            NSXO.flush();
        }
        catch (Exception E)
        {
            
        }
    }
}
