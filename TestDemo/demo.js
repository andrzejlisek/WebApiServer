function StringEncode(str)
{
    return btoa(encodeURIComponent(str).replace(/%([0-9A-F]{2})/g,
        function toSolidBytes(match, p1) {
            return String.fromCharCode('0x' + p1);
    }));
}

function StringDecode(str)
{
    // Going backwards: from bytestream, to percent-encoding, to original string.
    return decodeURIComponent(atob(str).split('').map(function(c) {
        return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
    }).join(''));
}


function CorrectEscape(S)
{
    let X = "";
    for (let I = 0; I < S.length; I++)
    {
        X = X + S[I];
        if (S[I] == '\\')
        {
            X = X + "\\";
        }
    }
    return X;
}


function GetFilesBtn()
{
    return GetFiles(document.getElementById("FileName").value);
}

function GetFiles(PathName)
{
    WSIO.GetFiles(PathName, document.getElementById("FileFilter").value).then(
    (Params) => {
        let Params_Path = Params.Path;
        let Params_Parent = Params.Parent;
        let FileList = Params.Files;

        let FileData = "";
        FileData = FileData + "<a href=\"javascript:void()\" onclick=\"return GetFiles(\'" + CorrectEscape(Params_Parent) + "\');\">" + Params_Parent + "</a><br>";
        FileData = FileData + "<a href=\"javascript:void()\" onclick=\"return GetFiles(\'" + CorrectEscape(Params_Path) + "\');\">" + Params_Path + "</a><br>";
        
        let L = FileList.length;
        FileData = FileData + "<table border=\"1\"><tr><td>Name</td><td>Type</td><td>Size</td><td>Date</td></tr>";
        for (let I = 0; I < L; I++)
        {
            let ItemType = 0;
            if (FileList[I].Dir) { ItemType += 1; }
            if (FileList[I].File) { ItemType += 2; }
        
            FileData = FileData + "<tr>";

            FileData = FileData + "<td>";
            switch (ItemType)
            {
                default:
                    FileData = FileData + "<a>";
                    break;
                case 1:
                    FileData = FileData + "<a href=\"javascript:void(0)\" onclick=\"return GetFiles(\'" + CorrectEscape(Params_Path + "/" + FileList[I].Name) + "\');\">";
                    break;
                case 2:
                    FileData = FileData + "<a href=\"javascript:void(0)\" onclick=\"return GetFileName(\'" + CorrectEscape(Params_Path + "/" + FileList[I].Name) + "\');\">";
                    break;
            }
            FileData = FileData + FileList[I].Name + "</a></td>";
            FileData = FileData + "<td>";
            if ((ItemType == 1) || (ItemType == 3)) FileData = FileData + "Directory";
            if ((ItemType == 2) || (ItemType == 3)) FileData = FileData + "File";
            FileData = FileData + "</td>";

            FileData = FileData + "<td>" + FileList[I].Size + "</td>";
            FileData = FileData + "<td>" + FileList[I].Date + "</td>";

            FileData = FileData + "</tr>";
        }
        FileData = FileData + "</table>";

        document.getElementById("FileDemo").innerHTML = FileData;
    }
    ).catch((ErrMsg) => {
        Info("GetFiles error: " + ErrMsg);
    });
    return false;
}

function GetFileName(Name)
{
    document.getElementById("FileName").value = Name;
}

function FileLoad()
{
    WSIO.FileOpen(document.getElementById("FileName").value, true, false, false, false).then(
    (Params1) => {
        if (Params1.FileId > 0)
        {
            WSIO.FileRead(Params1.FileId, Params1.Size).then(
            (Data) => {
                document.getElementById("FileText").value = StringDecode(Data);

                WSIO.FileClose(Params1.FileId).then(
                (Params2) => {
                }).catch((ErrMsg) => {
                    Info("FileClose error: " + ErrMsg);
                });
            }).catch((ErrMsg) => {
                Info("FileRead error: " + ErrMsg);
            });
        }
    }).catch((ErrMsg) => {
        Info("FileOpen error: " + ErrMsg);
    });
}

function FileSave()
{
    WSIO.FileOpen(document.getElementById("FileName").value, false, true, false, true).then(
    (Params1) => {
        if (Params1.FileId > 0)
        {
            WSIO.FileWrite(Params1.FileId, StringEncode(document.getElementById("FileText").value)).then(
            (Data) => {
                WSIO.FileClose(Params1.FileId).then(
                (Params2) => {
                }).catch((ErrMsg) => {
                    Info("FileClose error: " + ErrMsg);
                });
            }).catch((ErrMsg) => {
                Info("FileWrite error: " + ErrMsg);
            });
        }
    }).catch((ErrMsg) => {
        Info("FileOpen error: " + ErrMsg);
    });
}

function FileDelete()
{
    WSIO.FileDelete(document.getElementById("FileName").value).then(
    (Params) => {
    }
    ).catch((ErrMsg) => {
        Info("FileDelete error: " + ErrMsg);
    });
}

let ConnId = 0;

function BtnConnOpen()
{
    WSIO.ConnOpen(document.getElementById("ConnAddr").value, "appl", true).then(
    (Params) => {
        ConnId = Params.ConnId;
        Info("Connection opening: " + Params.ConnId);
    }
    ).catch((ErrMsg) => {
        Info("ConnOpen error: " + ErrMsg);
    });
}

function BtnConnInfo()
{
    WSIO.ConnInfo(ConnId).then(
    (Params) => {
        Info("Connection state: " + Params.Status + " " + Params.Push);
    }
    ).catch((ErrMsg) => {
        Info("ConnInfo error: " + ErrMsg);
    });
}

function BtnConnClose()
{
    WSIO.ConnClose(ConnId).then(
    (Params) => {
        Info("Connection closing");
    }
    ).catch((ErrMsg) => {
        Info("ConnClose error: " + ErrMsg);
    });
}

function BtnConnSend()
{
    WSIO.ConnSend(ConnId, StringEncode(document.getElementById("FileText").value)).then(
    (Params) => {
        Info("Sending");
    }
    ).catch((ErrMsg) => {
        Info("ConnSend error: " + ErrMsg);
    });
}

function BtnConnRecv()
{
    WSIO.ConnRecv(ConnId).then(
    (Params) => {
        Info(StringDecode(Params));
    }
    ).catch((ErrMsg) => {
        Info("ConnRecv error: " + ErrMsg);
    });
}
