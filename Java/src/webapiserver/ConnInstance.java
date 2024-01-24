/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package webapiserver;

import java.io.InputStream;
import java.io.OutputStream;
import java.net.ServerSocket;
import java.net.Socket;
import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.util.ArrayList;
import java.util.Base64;
import java.util.concurrent.locks.ReentrantLock;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

public class ConnInstance 
{
    static ReentrantLock Mtx;

    String ClientId = "";
    
    int InstanceNo = 0;
    
    boolean ConnectionWork = true;

    static
    {
        Mtx = new ReentrantLock(true);
    }

    public ConnInstance(int InstanceNo_)
    {
        InstanceNo = InstanceNo_;
    }
    
    static short BytesToInt16(byte B1, byte B2)
    {
        int B1_ = B1;
        int B2_ = B2;
        if (B1_ < 0) { B1_ += 256; }
        if (B2_ < 0) { B2_ += 256; }
        return (short)((B1_) | ((B2_) << 8));
    }

    static long BytesToInt64(byte B1, byte B2, byte B3, byte B4, byte B5, byte B6, byte B7, byte B8)
    {
        long B1_ = B1;
        long B2_ = B2;
        long B3_ = B3;
        long B4_ = B4;
        long B5_ = B5;
        long B6_ = B6;
        long B7_ = B7;
        long B8_ = B8;
        if (B1_ < 0) { B1_ += 256; }
        if (B2_ < 0) { B2_ += 256; }
        if (B3_ < 0) { B3_ += 256; }
        if (B4_ < 0) { B4_ += 256; }
        if (B5_ < 0) { B5_ += 256; }
        if (B6_ < 0) { B6_ += 256; }
        if (B7_ < 0) { B7_ += 256; }
        if (B8_ < 0) { B8_ += 256; }
        return (long)((B1_) | ((B2_) << 8) | ((B3_) << 16) | ((B4_) << 24) | ((B5_) << 32) | ((B6_) << 40) | ((B7_) << 48) | ((B8_) << 56));
    }
    
    static ServerSocket server;
    
    public static boolean StartListen(int PortNo)
    {
        try
        {
            System.out.println(PortNo);
            server = new ServerSocket(PortNo);
            return true;
        }
        catch (Exception E)
        {
            return false;
        }
    }

    Thread Thr;
    
    public void Start()
    {
        Thr = new Thread(() -> { StartWork(); });
        Thr.start();
    }
    
    InputStream stream_i;
    OutputStream stream_o;
            
    private void StartWork()
    {
        try
        {
            System.out.println("Instance " + InstanceNo + " - Waiting for connection");
            Socket client = server.accept();

            stream_i = client.getInputStream();
            stream_o = client.getOutputStream();

            System.out.println("Instance " + InstanceNo + " - Handshaking");
            
            Mtx.lock();
            WebApiServer.NewInstance();
            Mtx.unlock();

            // enter to an infinite cycle to be able to handle every change in stream
            String BufS = "";
            ArrayList<Byte> BufB = new ArrayList<Byte>();
            boolean ServerHandshake = true;
            while (ServerHandshake)
            {
                while (stream_i.available() == 0)
                {
                }
                byte[] bytes = new byte[stream_i.available()];
                stream_i.read(bytes, 0, bytes.length);
                BufS = BufS + (new String(bytes, 0, bytes.length, StandardCharsets.UTF_8));
                if (BufS.indexOf("GET") == 0)
                {
                    BufS = BufS.replace("\r\n", "\n");
                    int BufEnd = BufS.indexOf("\n\n") + 2;
                    if (BufEnd > 0)
                    {
                        // 1. Obtain the value of the "Sec-WebSocket-Key" request header without any leading or trailing whitespace
                        // 2. Concatenate it with "258EAFA5-E914-47DA-95CA-C5AB0DC85B11" (a special GUID specified by RFC 6455)
                        // 3. Compute SHA-1 and Base64 hash of the new value
                        // 4. Write the hash back as the value of "Sec-WebSocket-Accept" response header in an HTTP response
                        Matcher match = Pattern.compile("Sec-WebSocket-Key: (.*)").matcher(BufS);
                        match.find();
                        String swk = match.group(1).trim();
                        String swka = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
                        ClientId = swk;

                        byte[] swkaSha1 = MessageDigest.getInstance("SHA-1").digest((swka).getBytes(StandardCharsets.UTF_8));
                        String swkaSha1Base64 = Base64.getEncoder().encodeToString(swkaSha1);

                        // HTTP/1.1 defines the sequence CR LF as the end-of-line marker
                        byte[] response = (
                            "HTTP/1.1 101 Switching Protocols\r\n" +
                            "Connection: Upgrade\r\n" +
                            "Upgrade: websocket\r\n" +
                            "Sec-WebSocket-Accept: " + swkaSha1Base64 + "\r\n\r\n").getBytes(StandardCharsets.UTF_8);

                        stream_o.write(response, 0, response.length);

                        ServerHandshake = false;
                        System.out.println("Instance " + InstanceNo + " - " + ClientId + " - Connected");
                    }
                }
            }
            while (ConnectionWork)
            {
                while (stream_i.available() == 0)
                {
                }
                byte[] bytes = new byte[stream_i.available()];
                stream_i.read(bytes, 0, bytes.length);
                
                for (int I = 0; I < bytes.length; I++)
                {
                    BufB.add(bytes[I]);
                }

                String Resp = DataFrameToString(BufB);
                while (Resp.length() > 0)
                {
                    //Mtx.lock();
                    KeyValue MessageI = KeyValue.PoolAssign();
                    KeyValue MessageO = KeyValue.PoolAssign();
                    MessageI.StringLoad(Resp);
                    MessageO.ParamClear();
                    WebApiServer.ApiAction(MessageI, MessageO, this, ClientId);
                    Resp = MessageO.StringSave();
                    KeyValue.PoolRelease(MessageI);
                    KeyValue.PoolRelease(MessageO);
                    //Mtx.unlock();

                    if (Resp.length() > 0)
                    {
                        byte[] RespX = DataStringToFrame(Resp);
                        stream_o.write(RespX, 0, RespX.length);
                    }
                    
                    Resp = DataFrameToString(BufB);
                }
            }
            stream_i.close();
            stream_o.close();
            client.close();
            System.out.println("Instance " + InstanceNo + " - " + ClientId + " - Disconnected");
        }
        catch (Exception E)
        {
            ConnectionWork = false;
            if ("".equals(ClientId))
            {
                System.out.println("Instance " + InstanceNo + " - Connection error: " + E.getMessage());
            }
            else
            {
                System.out.println("Instance " + InstanceNo + " - " + ClientId + " - Connection error: " + E.getMessage());
            }
        }
    }
    
    /// <summary>
    /// Decodes the string from WebSocket frame
    /// </summary>
    /// <returns>The decoded data.</returns>
    /// <param name="buffer">Buffer.</param>
    private String DataFrameToString(ArrayList<Byte> buffer_)
    {
        if (buffer_.isEmpty())
        {
            return "";
        }
        
        byte[] buffer = new byte[buffer_.size()];
        for (int I = 0; I < buffer.length; I++)
        {
            buffer[I] = buffer_.get(I);
        }
        
        int b = buffer[1];
        if (b < 0) { b += 256; }
        int dataLength = 0;
        int totalLength = 0;
        int keyIndex = 0;

        if (b - 128 <= 125)
        {
            dataLength = b - 128;
            keyIndex = 2;
            totalLength = dataLength + 6;
        }

        if (b - 128 == 126)
        {
            dataLength = BytesToInt16(buffer[3], buffer[2]);
            keyIndex = 4;
            totalLength = dataLength + 8;
        }

        if (b - 128 == 127)
        {
            dataLength = (int)BytesToInt64(buffer[9], buffer[8], buffer[7], buffer[6], buffer[5], buffer[4], buffer[3], buffer[2]);
            keyIndex = 10;
            totalLength = dataLength + 14;
        }

        if (totalLength > buffer.length)
        {
            return "";
        }

        byte[] key = new byte[] { buffer[keyIndex], buffer[keyIndex + 1], buffer[keyIndex + 2], buffer[keyIndex + 3] };

        int dataIndex = keyIndex + 4;
        int count = 0;
        for (int i = dataIndex; i < totalLength; i++)
        {
            buffer[i] = (byte)(buffer[i] ^ key[count % 4]);
            count++;
        }

        int Opcode = buffer[0];
        if (Opcode < 0)
        {
            Opcode += 256;
        }
        switch (Opcode)
        {
            default:
                buffer_.subList(0, totalLength).clear();
                return "";
            case 136:
                buffer_.subList(0, totalLength).clear();
                ConnectionWork = false;
                return "";
            case 129:
                String RawStr = new String(buffer, dataIndex, dataLength, StandardCharsets.UTF_8);
                buffer_.subList(0, totalLength).clear();
                return RawStr;
        }
    }

    /// <summary>
    /// Creates the WebSocket frame from string
    /// </summary>
    /// <returns>The frame from string.</returns>
    /// <param name="Message">Message.</param>
    private byte[] DataStringToFrame(String Message)
    {
        byte[] response;
        byte[] bytesRaw = Message.getBytes(StandardCharsets.UTF_8);
        byte[] frame = new byte[10];

        long indexStartRawData = -1;
        long length = (long)bytesRaw.length;

        // 0 - Fragment
        // 1 - Text
        // 2 - Binary
        // 8 - ClosedConnection
        // 9 - Ping
        // 10 - Pong
        int Opcode = 1;

        frame[0] = (byte)(128 + Opcode);
        if (length <= 125)
        {
            frame[1] = (byte)length;
            indexStartRawData = 2;
        }
        else if (length >= 126 && length <= 65535)
        {
            frame[1] = (byte)126;
            frame[2] = (byte)((length >> 8) & 255);
            frame[3] = (byte)(length & 255);
            indexStartRawData = 4;
        }
        else
        {
            frame[1] = (byte)127;
            frame[2] = (byte)((length >> 56) & 255);
            frame[3] = (byte)((length >> 48) & 255);
            frame[4] = (byte)((length >> 40) & 255);
            frame[5] = (byte)((length >> 32) & 255);
            frame[6] = (byte)((length >> 24) & 255);
            frame[7] = (byte)((length >> 16) & 255);
            frame[8] = (byte)((length >> 8) & 255);
            frame[9] = (byte)(length & 255);

            indexStartRawData = 10;
        }

        response = new byte[(int)(indexStartRawData + length)];

        int i, reponseIdx = 0;

        //Add the frame bytes to the reponse
        for (i = 0; i < indexStartRawData; i++)
        {
            response[reponseIdx] = frame[i];
            reponseIdx++;
        }

        //Add the data bytes to the response
        for (i = 0; i < length; i++)
        {
            response[reponseIdx] = bytesRaw[i];
            reponseIdx++;
        }

        return response;
    }
    
    public void Push(KeyValue MessageO)
    {
        MessageO.ParamSet("Id", 0);
        String Resp = MessageO.StringSave();
        if (Resp.length() > 0)
        {
            byte[] RespX = DataStringToFrame(Resp);
            try
            {
                stream_o.write(RespX, 0, RespX.length);
            }
            catch (Exception E)
            {
                System.out.println("Instance " + InstanceNo + " - " + ClientId + " - Push error: " + E.getMessage());
            }
        }
    }
}
