/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package webapiserver;

import java.io.InputStream;
import java.io.OutputStream;
import java.util.ArrayList;

/**
 *
 * @author xxx
 */
public class ApiConnApp extends ApiConn
{
    Process App;
    OutputStream StrI;
    InputStream StrO;
    InputStream StrE;
    byte[] StreamBufO = new byte[1000000];
    byte[] StreamBufE = new byte[1000000];
    
    void LoopOut()
    {
        while (true)
        {
            try
            {
                int Avail = StrO.read(StreamBufO);
                if (Avail > 0)
                {
                    RecvProcess(StreamBufO, Avail);
                }
                else
                {
                    if (Status() == 0)
                    {
                        break;
                    }
                }
            }
            catch (Exception E)
            {
                break;
            }
        }
        try
        {
            StrO.close();
        }
        catch (Exception E)
        {
            
        }
    }

    void LoopErr()
    {
        while (true)
        {
            try
            {
                int Avail = StrE.read(StreamBufE);
                if (Avail > 0)
                {
                    RecvProcess(StreamBufE, Avail);
                }
                else
                {
                    if (Status() == 0)
                    {
                        break;
                    }
                }
            }
            catch (Exception E)
            {
                break;
            }
        }
        try
        {
            StrE.close();
        }
        catch (Exception E)
        {
            
        }
    }
    
    Thread LoopOutThr;
    Thread LoopErrThr;
    
    public void Open(String Address, boolean Push_) throws Exception
    {
        if (!RegexTest.Match(Address, CommandArgs.RegCmd))
        {
            throw new Exception("Command pattern mismatch \"" + Address + "\"");
        }
        RecvBuf.clear();
        RecvBufL = 0;
        Push = Push_;
        ArrayList<String> Cmd = new ArrayList<>();
        boolean InQuote1 = false;
        boolean InQuote2 = false;
        char LastC = ' ';
        String CmdItem = "";
        for (int I = 0; I < Address.length(); I++)
        {
            switch (Address.charAt(I))
            {
                case ' ':
                    if ((!InQuote1) && (!InQuote2))
                    {
                        Cmd.add(CmdItem);
                        CmdItem = "";
                    }
                    else
                    {
                        CmdItem = CmdItem + ' ';
                    }
                    break;
                case '\'':
                    if (InQuote1)
                    {
                        if (LastC != '\\')
                        {
                            InQuote1 = false;
                        }
                        else
                        {
                            CmdItem = CmdItem + '\'';
                        }
                    }
                    else
                    {
                        InQuote1 = true;
                    }
                    break;
                case '\"':
                    if (InQuote2)
                    {
                        if (LastC != '\\')
                        {
                            InQuote2 = false;
                        }
                        else
                        {
                            CmdItem = CmdItem + '\"';
                        }
                    }
                    else
                    {
                        InQuote2 = true;
                    }
                    break;
                default:
                    CmdItem = CmdItem + Address.charAt(I);
                    break;
            }
            LastC = Address.charAt(I);
        }
        if (!"".equals(CmdItem))
        {
            Cmd.add(CmdItem);
        }
            
        ProcessBuilder PB = new ProcessBuilder(Cmd);
        PB.redirectInput(ProcessBuilder.Redirect.PIPE);
        PB.redirectError(ProcessBuilder.Redirect.PIPE);
        PB.redirectOutput(ProcessBuilder.Redirect.PIPE);

        App = PB.start();
        StrI = App.getOutputStream();
        StrO = App.getInputStream();
        StrE = App.getErrorStream();

        LoopOutThr = new Thread(() -> { LoopOut(); });
        LoopOutThr.start();
        LoopErrThr = new Thread(() -> { LoopErr(); });
        LoopErrThr.start();
    }
    
    public int Status()
    {
        if (App.isAlive())
        {
            return 1;
        }
        return 0;
    }

    public void Close()
    {
        try
        {
            App.destroy();
        }
        catch (Exception E)
        {
            
        }
        try
        {
            StrI.close();
        }
        catch (Exception E)
        {
            
        }
        App = null;
    }

    public void Send(byte[] Data) throws Exception
    {
        StrI.write(Data);
        StrI.flush();
    }
}
