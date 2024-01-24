/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package webapiserver;

import java.io.BufferedReader;
import java.io.BufferedWriter;
import java.io.FileReader;
import java.io.FileWriter;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.Base64;
import java.util.HashMap;
import java.util.Locale;
import java.util.concurrent.locks.ReentrantLock;

public class KeyValue
{
    static ArrayList<KeyValue> PoolObj = new ArrayList<>();
    static ReentrantLock PoolMtx = new ReentrantLock(true);
    
    public static KeyValue PoolAssign()
    {
        PoolMtx.lock();
        KeyValue KV;
        for (int i = 0; i < PoolObj.size(); i++)
        {
            if (PoolObj.get(i).Free)
            {
                KV = PoolObj.get(i);
                KV.Free = false;
                PoolMtx.unlock();
                return KV;
            }
        }
        KV = new KeyValue();
        PoolObj.add(KV);
        PoolMtx.unlock();
        return KV;
    }

    public static void PoolRelease(KeyValue KV)
    {
        PoolMtx.lock();
        int KVI = PoolObj.indexOf(KV);
        if (KVI >= 0)
        {
            PoolObj.get(KVI).Free = true;
        }
        PoolMtx.unlock();
    }

    private boolean Free = false;
    
    private HashMap<String, String> Raw = new HashMap<>();
    
    public int CaseMode = 0;

    private String Case(String S)
    {
        if (CaseMode > 0)
        {
            return S.toUpperCase(Locale.ROOT);
        }
        if (CaseMode < 0)
        {
            return S.toLowerCase(Locale.ROOT);
        }
        return S;
    }
    
    public static String BinaryEncode(byte[] Data)
    {
        return Base64.getEncoder().encodeToString(Data);
    }
    
    public static byte[] BinaryDecode(String Data)
    {
        return Base64.getDecoder().decode(Data);
    }
    
    public static String StringEncode(String Data)
    {
        return BinaryEncode(Data.getBytes(StandardCharsets.UTF_8));
    }
    
    public static String StringDecode(String Data)
    {
        return (new String(BinaryDecode(Data), StandardCharsets.UTF_8));
    }
    
    public void FileLoad(String FileName)
    {
        ParamClear();
        try
        {
            FileReader F_ = new FileReader(FileName);
            BufferedReader F = new BufferedReader(F_);
            String S = F.readLine();
            while (S != null)
            {
                int I = S.indexOf("=");
                if (I >= 0)
                {
                    String RawK = Case(S.substring(0, I));
                    if (!Raw.containsKey(RawK))
                    {
                        if (S.length() > (I + 1))
                        {
                            Raw.put(RawK, S.substring(I + 1));
                        }
                        else
                        {
                            Raw.put(RawK, "");
                        }
                    }
                }
                S = F.readLine();
            }
            F.close();
            F_.close();
        }
        catch (Exception __)
        {

        }
    }

    public void StringLoad(String Data)
    {
        ParamClear();
        String[] Data_ = Data.split("\\r?\\n|\\r");
        for (int II = 0; II < Data_.length; II++)
        {
            String S = Data_[II];
            int I = S.indexOf("=");
            if (I >= 0)
            {
                String RawK = Case(S.substring(0, I));
                if (!Raw.containsKey(RawK))
                {
                    if (S.length() > (I + 1))
                    {
                        Raw.put(RawK, S.substring(I + 1));
                    }
                    else
                    {
                        Raw.put(RawK, "");
                    }
                }
            }
        }
    }
    
    public void FileSave(String FileName)
    {
        try
        {
            FileWriter F_ = new FileWriter(FileName);
            BufferedWriter F = new BufferedWriter(F_);
            for (HashMap.Entry<String, String> item : Raw.entrySet())
            {
                F.write(Case(item.getKey()));
                F.write("=");
                F.write(item.getValue());
                F.newLine();
            }
            F.close();
            F_.close();
        }
        catch (Exception __)
        {

        }
    }

    public String StringSave()
    {
        StringBuilder SB = new StringBuilder();
        for (HashMap.Entry<String, String> item : Raw.entrySet())
        {
            SB.append(Case(item.getKey()));
            SB.append("=");
            SB.append(item.getValue());
            SB.append("\n");
        }
        return SB.toString();
    }
    
    public String Print()
    {
        String S = "";
        for (HashMap.Entry<String, String> item : Raw.entrySet())
        {
            S = S + Case(item.getKey());
            S = S + "=";
            S = S + item.getValue();
            S = S + System.lineSeparator();
        }
        return S;
    }

    public void ParamClear()
    {
        Raw.clear();
    }

    public void ParamRemove(String Name)
    {
        if (Raw.containsKey(Case(Name)))
        {
            Raw.remove(Case(Name));
        }
    }

    public void ParamSet(String NameValue)
    {
        int I = NameValue.indexOf("=");
        if (I >= 0)
        {
            String RawK = Case(NameValue.substring(0, I));
            if (!Raw.containsKey(RawK))
            {
                if (NameValue.length() > (I + 1))
                {
                    Raw.put(RawK, NameValue.substring(I + 1));
                }
                else
                {
                    Raw.put(RawK, "");
                }
            }
        }
    }

    public void ParamSet(String Name, String Value)
    {
        if (Raw.containsKey(Case(Name)))
        {
            Raw.put(Case(Name), Value);
        }
        else
        {
            Raw.put(Case(Name), Value);
        }
    }

    public void ParamSet(String Name, int Value)
    {
        ParamSet(Name, String.valueOf(Value));
    }

    public void ParamSet(String Name, long Value)
    {
        ParamSet(Name, String.valueOf(Value));
    }

    public void ParamSet(String Name, boolean Value)
    {
        ParamSet(Name, Value ? "1" : "0");
    }

    public void ParamSet(String Name, byte[] Value)
    {
        ParamSet(Name, BinaryEncode(Value));
    }
    
    public boolean ParamGet(String Name, String[] Value)
    {
        if (Raw.containsKey(Case(Name)))
        {
            Value[0] = Raw.get(Case(Name));
            return true;
        }
        return false;
    }

    public boolean ParamGet(String Name, int[] Value)
    {
        if (Raw.containsKey(Case(Name)))
        {
            try
            {
                Value[0] = Integer.parseInt(Raw.get(Case(Name)));
                return true;
            }
            catch (Exception __)
            {

            }
        }
        return false;
    }

    public boolean ParamGet(String Name, long[] Value)
    {
        if (Raw.containsKey(Case(Name)))
        {
            try
            {
                Value[0] = Long.parseLong(Raw.get(Case(Name)));
                return true;
            }
            catch (Exception __)
            {

            }
        }
        return false;
    }

    public boolean ParamGet(String Name, boolean[] Value)
    {
        if (Raw.containsKey(Case(Name)))
        {
            switch (Raw.get(Case(Name)).toUpperCase(Locale.ROOT))
            {
                case "1":
                case "TRUE":
                case "YES":
                case "T":
                case "Y":
                    Value[0] = true;
                    return true;
                case "0":
                case "FALSE":
                case "NO":
                case "F":
                case "N":
                    Value[0] = false;
                    return true;
            }
        }
        return false;
    }

    public String ParamGetS(String Name, String X)
    {
        String[] X_ = { X };
        ParamGet(Name, X_);
        return X_[0];
    }

    public int ParamGetI(String Name, int X)
    {
        int[] X_ = { X };
        ParamGet(Name, X_);
        return X_[0];
    }

    public long ParamGetL(String Name, long X)
    {
        long[] X_ = { X };
        ParamGet(Name, X_);
        return X_[0];
    }

    public boolean ParamGetB(String Name, boolean X)
    {
        boolean[] X_ = { X };
        ParamGet(Name, X_);
        return X_[0];
    }

    public byte[] ParamGetD(String Name, byte[] X)
    {
        return BinaryDecode(ParamGetS(Name, BinaryEncode(X)));
    }
    
    public String ParamGetS(String Name)
    {
        String[] X_ = { "" };
        ParamGet(Name, X_);
        return X_[0];
    }

    public int ParamGetI(String Name)
    {
        int[] X_ = { 0 };
        ParamGet(Name, X_);
        return X_[0];
    }

    public long ParamGetL(String Name)
    {
        long[] X_ = { 0 };
        ParamGet(Name, X_);
        return X_[0];
    }

    public boolean ParamGetB(String Name)
    {
        boolean[] X_ = { false };
        ParamGet(Name, X_);
        return X_[0];
    }

    public byte[] ParamGetD(String Name)
    {
        return BinaryDecode(ParamGetS(Name));
    }
    
    public boolean ParamExists(String Name)
    {
        return Raw.containsKey(Case(Name));
    }
}
