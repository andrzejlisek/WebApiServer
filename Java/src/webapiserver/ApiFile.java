/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package webapiserver;

import java.io.File;
import java.io.FilenameFilter;
import java.io.RandomAccessFile;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Calendar;
import java.util.HashMap;

public class ApiFile
{
    static
    {
        ApiFile_ = new HashMap<>();
    }
    
    static HashMap<Integer, ApiFile> ApiFile_;
    static int ApiFileN = 0;
    
    File F = null;
    RandomAccessFile FS;
    public int IdleCounter = 0;

    
    public static void IdleTimerTick()
    {
        if (CommandArgs.Debug == 2)
        {
            System.out.println("File IdleTimer tick begin");
        }
        ArrayList<Integer> TimeoutId = new ArrayList<Integer>();
        ApiFile_.forEach((Key, Value) -> {
            Value.IdleCounter++;
            if ((CommandArgs.Timeout > 0) && (Value.IdleCounter >= CommandArgs.Timeout))
            {
                TimeoutId.add(Key);
            }
            if (CommandArgs.Debug == 2)
            {
                System.out.println("File " + Key + " idle: " + Value.IdleCounter);
            }
        });
        if (TimeoutId.size() > 0)
        {
            KeyValue XI = new KeyValue();
            KeyValue XO = new KeyValue();
            for (int i = 0; i < TimeoutId.size(); i++)
            {
                XI.ParamSet("FileId", TimeoutId.get(i));
                FileClose(XI, XO);
                if (CommandArgs.Debug == 2)
                {
                    System.out.println("File " + TimeoutId.get(i) + " closed");
                }
            }
        }
        if (CommandArgs.Debug == 2)
        {
            System.out.println("File IdleTimer tick end");
        }
    }
    
    private static boolean FileDeleteDir(File FileName)
    {
        File[] FileDir = FileName.listFiles();
        if (FileDir != null)
        {
            for (File FileDirItem : FileDir)
            {
                FileDeleteDir(FileDirItem);
            }
        }
        return FileName.delete();
    }    

    public static void FileDelete(KeyValue MessageI, KeyValue MessageO)
    {
        String FileName = CommandArgs.PathMountToReal(MessageI.ParamGetS("Path"));
        try
        {
            if (FileName.length() == 0)
            {
                throw new Exception("Invalid file path");
            }
            File FileDel = new File(FileName);
            System.out.println(FileDel.exists());
            if (FileDel.exists())
            {
                FileDeleteDir(FileDel);
            }
        }
        catch (Exception E)
        {
            WebApiServer.CatchError(MessageO, E);
        }
    }
    
    public static void FileOpen(KeyValue MessageI, KeyValue MessageO)
    {
        String FileName = CommandArgs.PathMountToReal(MessageI.ParamGetS("Path"));
        try
        {
            if (FileName.length() == 0)
            {
                throw new Exception("Invalid file path");
            }
            boolean Read = MessageI.ParamGetB("Read");
            boolean Write = MessageI.ParamGetB("Write");
            boolean Append = MessageI.ParamGetB("Append");
            boolean Truncate = MessageI.ParamGetB("Truncate");
            ApiFile __ = new ApiFile();

            __.F = new File(FileName);
            __.IdleCounter = 0;
            if (Write)
            {
                File FileDir = new File(__.F.getParent());
                if (!FileDir.exists())
                {
                    FileDir.mkdir();
                }
            }
            if (Read && (!Write))
            {
                __.FS = new RandomAccessFile(__.F, "r");
            }
            else
            {
                __.FS = new RandomAccessFile(__.F, "rw");
            }
            ApiFileN++;
            ApiFile_.put(ApiFileN, __);

            MessageI.ParamSet("FileId", ApiFileN);
            FileInfo(MessageI, MessageO);
            MessageO.ParamSet("FileId", ApiFileN);
        }
        catch (Exception E)
        {
            MessageO.ParamSet("FileId", 0);
            WebApiServer.CatchError(MessageO, E);
        }
    }

    public static void FileInfo(KeyValue MessageI, KeyValue MessageO)
    {
        int FileId = MessageI.ParamGetI("FileId");
        MessageO.ParamSet("FileId", FileId);
        if (ApiFile_.containsKey(FileId))
        {
            ApiFile __ = ApiFile_.get(FileId);
            __.IdleCounter = 0;
            try
            {
                MessageO.ParamSet("Size", __.FS.length());
            }
            catch (Exception E)
            {
                MessageO.ParamSet("Size", -1);
            }
            try
            {
                MessageO.ParamSet("Position", __.FS.length());
            }
            catch (Exception E)
            {
                MessageO.ParamSet("Position", -1);
            }
        }
        else
        {
            MessageO.ParamSet("Size", 0);
            MessageO.ParamSet("Position", 0);
        }
    }

    public static void FileSeek(KeyValue MessageI, KeyValue MessageO)
    {
        int FileId = MessageI.ParamGetI("FileId");
        long Pos = MessageI.ParamGetI("Position");
        if (ApiFile_.containsKey(FileId))
        {
            ApiFile_.get(FileId).IdleCounter = 0;
            try
            {
                ApiFile_.get(FileId).FS.seek(Pos);
            }
            catch (Exception E)
            {
                WebApiServer.CatchError(MessageO, E);
            }
        }
    }

    public static void FileClose(KeyValue MessageI, KeyValue MessageO)
    {
        int FileId = MessageI.ParamGetI("FileId");
        if (ApiFile_.containsKey(FileId))
        {
            ApiFile __ = ApiFile_.get(FileId);
            if (__.FS != null)
            {
                try
                {
                    __.FS.close();
                }
                catch (Exception E)
                {
                    WebApiServer.CatchError(MessageO, E);
                }
            }
            ApiFile_.remove(FileId);
        }
    }

    public static void FileRead(KeyValue MessageI, KeyValue MessageO)
    {
        int FileId = MessageI.ParamGetI("FileId");
        int Count = MessageI.ParamGetI("Count");
        MessageO.ParamSet("FileId", FileId);
        MessageO.ParamSet("Data", "");
        if (ApiFile_.containsKey(FileId))
        {
            ApiFile __ = ApiFile_.get(FileId);
            __.IdleCounter = 0;
            try
            {
                byte[] Temp = new byte[Count];
                __.FS.read(Temp, 0, Temp.length);
                MessageO.ParamSet("Data", KeyValue.BinaryEncode(Temp));
            }
            catch (Exception E)
            {
                WebApiServer.CatchError(MessageO, E);
            }
        }
        else
        {
        }
    }

    public static void FileWrite(KeyValue MessageI, KeyValue MessageO)
    {
        int FileId = MessageI.ParamGetI("FileId");
        String Data = MessageI.ParamGetS("Data");
        MessageO.ParamSet("FileId", FileId);
        if (ApiFile_.containsKey(FileId))
        {
            ApiFile __ = ApiFile_.get(FileId);
            __.IdleCounter = 0;
            try
            {
                byte[] Temp = KeyValue.BinaryDecode(Data);
                __.FS.write(Temp, 0, Temp.length);
            }
            catch (Exception E)
            {
                WebApiServer.CatchError(MessageO, E);
            }
        }
    }

    public static void GetDir(KeyValue MessageI, KeyValue MessageO)
    {
        String PathName_ = MessageI.ParamGetS("Path");
        String Filter = MessageI.ParamGetS("Filter");
        if (("?".equals(PathName_)) || ("".equals(PathName_)))
        {
            MessageO.ParamSet("Path", "?");
            MessageO.ParamSet("Parent", "?");
            //File[] DirF_ = File.listRoots();
            String[] ItemName = CommandArgs.PathMountList();

            MessageO.ParamSet("ItemCount", ItemName.length);

            for (int i = 0; i < ItemName.length; i++)
            {
                MessageO.ParamSet("Item" + String.valueOf(i) + "Name", ItemName[i]);
                MessageO.ParamSet("Item" + String.valueOf(i) + "Dir", true);
                MessageO.ParamSet("Item" + String.valueOf(i) + "File", false);
                MessageO.ParamSet("Item" + String.valueOf(i) + "Date", 0);
                MessageO.ParamSet("Item" + String.valueOf(i) + "Size", 0);
            }
        }
        else
        {
            String PathName = CommandArgs.PathMountToReal(PathName_);
            MessageO.ParamSet("Path", CommandArgs.PathRealToMount(PathName, true));
            File DirF = new File(PathName);
            if (DirF.exists())
            {
                File[] DirF_ = null;
                FilenameFilter FNF = (File dir, String name) ->
                {
                    return RegexTest.Match(name, Filter);
                };
                DirF_ = DirF.listFiles(FNF);

                Arrays.sort(DirF_, (F1, F2) -> { 
                    return F1.getName().compareTo(F2.getName());
                });
                
                if (DirF.getParent() != null)
                {
                    MessageO.ParamSet("Parent", CommandArgs.PathRealToMount(DirF.getParent(), true));
                }
                else
                {
                    MessageO.ParamSet("Parent", "?");
                }

                if (DirF_ != null)
                {
                    MessageO.ParamSet("ItemCount", DirF_.length);

                    for (int i = 0; i < DirF_.length; i++)
                    {
                        long ItemDate = DirF_[i].lastModified();
                        Calendar DT = Calendar.getInstance();
                        DT.setTimeInMillis(ItemDate);
                        int FileDate = (DT.get(Calendar.YEAR) * 10000) + ((DT.get(Calendar.MONTH) + 1) * 100) + (DT.get(Calendar.DAY_OF_MONTH));
                        if (DirF_[i].getName().length() == 0)
                        {
                            MessageO.ParamSet("Item" + String.valueOf(i) + "Name", DirF_[i].toString());
                        }
                        else
                        {
                            MessageO.ParamSet("Item" + String.valueOf(i) + "Name", DirF_[i].getName());
                        }
                        MessageO.ParamSet("Item" + String.valueOf(i) + "Dir", DirF_[i].isDirectory());
                        MessageO.ParamSet("Item" + String.valueOf(i) + "File", DirF_[i].isFile());
                        MessageO.ParamSet("Item" + String.valueOf(i) + "Date", FileDate);
                        MessageO.ParamSet("Item" + String.valueOf(i) + "Size", DirF_[i].length());
                    }
                }
                else
                {
                    MessageO.ParamSet("ItemCount", 0);
                }
            }
            else
            {
                MessageO.ParamSet("Parent", "?");
                MessageO.ParamSet("ItemCount", 0);
            }
        }
    }
}
