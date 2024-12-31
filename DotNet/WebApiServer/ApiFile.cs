using System;
using System.Collections.Generic;
using System.IO;

namespace WebApiServer
{
    public class ApiFile
    {
        static ApiFile()
        {
            ApiFile_ = new Dictionary<int, ApiFile>();
        }

        static Dictionary<int, ApiFile> ApiFile_;
        static int ApiFileN = 0;

        FileStream FS = null;
        BinaryReader FS_R = null;
        BinaryWriter FS_W = null;
        public int IdleCounter = 0;

        public static void IdleTimerTick()
        {
            if (CommandArgs.Debug == 2)
            {
                Console.WriteLine("File IdleTimer tick begin");
            }
            List<int> TimeoutId = new List<int>();
            foreach (KeyValuePair<int, ApiFile> item in ApiFile_)
            {
                item.Value.IdleCounter++;
                if ((CommandArgs.Timeout > 0) && (item.Value.IdleCounter >= CommandArgs.Timeout))
                {
                    TimeoutId.Add(item.Key);
                }
                if (CommandArgs.Debug == 2)
                {
                    Console.WriteLine("File " + item.Key + " idle: " + item.Value.IdleCounter);
                }
            }
            if (TimeoutId.Count > 0)
            {
                KeyValue XI = new KeyValue();
                KeyValue XO = new KeyValue();
                for (int i = 0; i < TimeoutId.Count; i++)
                {
                    XI.ParamSet("FileId", TimeoutId[i]);
                    FileClose(XI, XO);
                    if (CommandArgs.Debug == 2)
                    {
                        Console.WriteLine("File " + TimeoutId[i] + " closed");
                    }
                }
            }
            if (CommandArgs.Debug == 2)
            {
                Console.WriteLine("File IdleTimer tick end");
            }
        }

        public static void FileDelete(KeyValue MessageI, KeyValue MessageO)
        {
            string FileName = CommandArgs.PathMountToReal(MessageI.ParamGetS("Path"));
            try
            {
                if (FileName.Length == 0)
                {
                    throw new Exception("Invalid file path");
                }
                if (Directory.Exists(FileName))
                {
                    Directory.Delete(FileName, true);
                }
                if (File.Exists(FileName))
                {
                    File.Delete(FileName);
                }
            }
            catch (Exception E)
            {
                MainClass.CatchError(MessageO, E);
            }
        }

        public static void FileOpen(KeyValue MessageI, KeyValue MessageO)
        {
            string FileName = CommandArgs.PathMountToReal(MessageI.ParamGetS("Path"));
            try
            {
                if (FileName.Length == 0)
                {
                    throw new Exception("Invalid file path");
                }
                bool Read = MessageI.ParamGetB("Read");
                bool Write = MessageI.ParamGetB("Write");
                bool Append = MessageI.ParamGetB("Append");
                bool Truncate = MessageI.ParamGetB("Truncate");
                ApiFile __ = new ApiFile();
                __.IdleCounter = 0;

                FileMode FileMode_;
                FileAccess FileAccess_;
                if (Read && Write)
                {
                    FileAccess_ = FileAccess.ReadWrite;
                }
                else
                {
                    if (Write)
                    {
                        FileAccess_ = FileAccess.Write;
                    }
                    else
                    {
                        FileAccess_ = FileAccess.Read;
                    }
                }
                if (Truncate)
                {
                    FileMode_ = FileMode.Create;
                }
                else
                {
                    if (Append)
                    {
                        FileMode_ = FileMode.Append;
                    }
                    else
                    {
                        FileMode_ = FileMode.Open;
                    }
                }
                if (Write)
                {
                    if (!Directory.Exists(Path.GetDirectoryName(FileName)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(FileName));
                    }
                }
                __.FS = new FileStream(FileName, FileMode_, FileAccess_);
                if (Read)
                {
                    __.FS_R = new BinaryReader(__.FS);
                }
                if (Write)
                {
                    __.FS_W = new BinaryWriter(__.FS);
                }
                ApiFileN++;
                ApiFile_.Add(ApiFileN, __);

                MessageI.ParamSet("FileId", ApiFileN);
                FileInfo(MessageI, MessageO);
                MessageO.ParamSet("FileId", ApiFileN);
            }
            catch (Exception E)
            {
                MessageO.ParamSet("FileId", 0);
                MainClass.CatchError(MessageO, E);
            }
        }

        public static void FileInfo(KeyValue MessageI, KeyValue MessageO)
        {
            int FileId = MessageI.ParamGetI("FileId");
            MessageO.ParamSet("FileId", FileId);
            if (ApiFile_.ContainsKey(FileId))
            {
                ApiFile __ = ApiFile_[FileId];
                __.IdleCounter = 0;
                try
                {
                    MessageO.ParamSet("Size", __.FS.Length);
                }
                catch (Exception E)
                {
                    MessageO.ParamSet("Size", -1);
                }
                try
                {
                    MessageO.ParamSet("Position", __.FS.Position);
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
            if (ApiFile_.ContainsKey(FileId))
            {
                ApiFile_[FileId].IdleCounter = 0;
                try
                {
                    ApiFile_[FileId].FS.Seek(Pos, SeekOrigin.Begin);
                }
                catch (Exception E)
                {
                    MainClass.CatchError(MessageO, E);
                }
            }
        }

        public static void FileClose(KeyValue MessageI, KeyValue MessageO)
        {
            int FileId = MessageI.ParamGetI("FileId");
            if (ApiFile_.ContainsKey(FileId))
            {
                try
                {
                    ApiFile __ = ApiFile_[FileId];
                    if (__.FS_R != null)
                    {
                        __.FS_R.Close();
                    }
                    if (__.FS_W != null)
                    {
                        __.FS_W.Close();
                    }
                    if (__.FS != null)
                    {
                        __.FS.Close();
                    }
                }
                catch (Exception E)
                {
                    MainClass.CatchError(MessageO, E);
                }
                ApiFile_.Remove(FileId);
            }
        }

        public static void FileRead(KeyValue MessageI, KeyValue MessageO)
        {
            int FileId = MessageI.ParamGetI("FileId");
            int Count = MessageI.ParamGetI("Count");
            MessageO.ParamSet("FileId", FileId);
            MessageO.ParamSet("Data", "");
            if (ApiFile_.ContainsKey(FileId))
            {
                ApiFile __ = ApiFile_[FileId];
                __.IdleCounter = 0;
                try
                {
                    byte[] Temp = new byte[Count];
                    __.FS_R.Read(Temp, 0, Temp.Length);
                    MessageO.ParamSet("Data", KeyValue.BinaryEncode(Temp));
                }
                catch (Exception E)
                {
                    MainClass.CatchError(MessageO, E);
                }
            }
            else
            {
            }
        }

        public static void FileWrite(KeyValue MessageI, KeyValue MessageO)
        {
            int FileId = MessageI.ParamGetI("FileId");
            string Data = MessageI.ParamGetS("Data");
            MessageO.ParamSet("FileId", FileId);
            if (ApiFile_.ContainsKey(FileId))
            {
                ApiFile __ = ApiFile_[FileId];
                __.IdleCounter = 0;
                try
                {
                    byte[] Temp = KeyValue.BinaryDecode(Data);
                    __.FS_W.Write(Temp, 0, Temp.Length);
                }
                catch (Exception E)
                {
                    MainClass.CatchError(MessageO, E);
                }
            }
        }

        public static void GetDir(KeyValue MessageI, KeyValue MessageO)
        {
            string PathName_ = MessageI.ParamGetS("Path");
            string Filter = MessageI.ParamGetS("Filter");
            if (("?".Equals(PathName_)) || ("".Equals(PathName_)))
            {
                MessageO.ParamSet("Path", "?");
                MessageO.ParamSet("Parent", "?");
                //string[] ItemName = Directory.GetLogicalDrives();
                //Array.Sort(ItemName);
                string[] ItemName = CommandArgs.PathMountList();

                MessageO.ParamSet("ItemCount", ItemName.Length);

                for (int i = 0; i < ItemName.Length; i++)
                {
                    MessageO.ParamSet("Item" + i.ToString() + "Name", ItemName[i]);
                    MessageO.ParamSet("Item" + i.ToString() + "Dir", true);
                    MessageO.ParamSet("Item" + i.ToString() + "File", false);
                    MessageO.ParamSet("Item" + i.ToString() + "Date", 0);
                    MessageO.ParamSet("Item" + i.ToString() + "Size", 0);
                }
            }
            else
            {
                string PathName = CommandArgs.PathMountToReal(PathName_);
                MessageO.ParamSet("Path", CommandArgs.PathRealToMount(PathName, true));
                if ((PathName.Length > 0) && Directory.Exists(PathName))
                {
                    List<string> ItemName = new List<string>();
                    List<bool> ItemDir = new List<bool>();
                    List<bool> ItemFile = new List<bool>();
                    List<int> ItemDate = new List<int>();
                    List<int> ItemSize = new List<int>();

                    if (Directory.GetParent(PathName) != null)
                    {
                        MessageO.ParamSet("Parent", CommandArgs.PathRealToMount(Directory.GetParent(PathName).FullName, true));
                    }
                    else
                    {
                        MessageO.ParamSet("Parent", "?");
                    }

                    string[] Temp;
                    Temp = Directory.GetDirectories(PathName);
                    for (int i = 0; i < Temp.Length; i++)
                    {
                        DirectoryInfo DI = new DirectoryInfo(Temp[i]);
                        if (RegexTest.Match(DI.Name, Filter))
                        {
                            int FileDate = (DI.LastWriteTime.Year * 10000) + (DI.LastWriteTime.Month * 100) + DI.LastWriteTime.Day;
                            ItemName.Add(DI.Name);
                            ItemDir.Add(true);
                            ItemFile.Add(false);
                            ItemDate.Add(FileDate);
                            ItemSize.Add(0);
                        }
                    }

                    Temp = Directory.GetFiles(PathName);
                    for (int i = 0; i < Temp.Length; i++)
                    {
                        FileInfo FI = new FileInfo(Temp[i]);
                        if (RegexTest.Match(FI.Name, Filter))
                        {
                            int FileDate = (FI.LastWriteTime.Year * 10000) + (FI.LastWriteTime.Month * 100) + FI.LastWriteTime.Day;
                            ItemName.Add(FI.Name);
                            ItemDir.Add(false);
                            ItemFile.Add(true);
                            ItemDate.Add(FileDate);
                            ItemSize.Add((int)FI.Length);
                        }
                    }


                    for (int i = 0; i < ItemName.Count; i++)
                    {
                        for (int ii = 0; ii < ItemName.Count; ii++)
                        {
                            if (ItemName[i].CompareTo(ItemName[ii]) < 0)
                            {
                                string TempS = ItemName[i];
                                ItemName[i] = ItemName[ii];
                                ItemName[ii] = TempS;

                                bool TempB = ItemDir[i];
                                ItemDir[i] = ItemDir[ii];
                                ItemDir[ii] = TempB;

                                TempB = ItemFile[i];
                                ItemFile[i] = ItemFile[ii];
                                ItemFile[ii] = TempB;

                                int TempI = ItemDate[i];
                                ItemDate[i] = ItemDate[ii];
                                ItemDate[ii] = TempI;

                                TempI = ItemSize[i];
                                ItemSize[i] = ItemSize[ii];
                                ItemSize[ii] = TempI;
                            }
                        }
                    }

                    MessageO.ParamSet("ItemCount", ItemName.Count);

                    for (int i = 0; i < ItemName.Count; i++)
                    {
                        MessageO.ParamSet("Item" + i.ToString() + "Name", ItemName[i]);
                        MessageO.ParamSet("Item" + i.ToString() + "Dir", ItemDir[i]);
                        MessageO.ParamSet("Item" + i.ToString() + "File", ItemFile[i]);
                        MessageO.ParamSet("Item" + i.ToString() + "Date", ItemDate[i]);
                        MessageO.ParamSet("Item" + i.ToString() + "Size", ItemSize[i]);
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
}
