# WebApiServer

One of the most universal application technologies is using ordinary web browser as interpreter and use HTML, JavaScript, WebAssembly for application implementation\.

The main problem is very limited access to operating system\. For instance, these elements are not directly available \(the list is not exhaustive\):


* Local file read or write\.
* Connect into any service using any protocol other than WebSocket or HTTP protocol\.
* Run any system command\.

The **WebApiServer** makes workaround of these limitations by running as WebSocket server\. If you run the **WebApiServer**, the compatible application can nearly directly acces into file system and for instance, there will be possible to read file list within any directory and read contents from any file\.

# Server architecture

The application is available in version **Java** and **DotNet**\. Both versions has the same functions\. In the web browser application, the management is automated and the required library consists of **WSIO\.js** and **WSIOKV\.js** files\. 

For example, if you want to open file, the application connects to the server, request specified file by WebSocket connection and gets the contents of file\.

Current version of the WebApiServer allows to:


* Directory list\.
* File read, write, delete\.
* Run system command with redirecting standard streams\.
* Connect into any network service, the connection protocol must be implemented in client application\.

The simple demonstration is in **TestDemo** subdirectory in project repository\.

# Parameters and configuration

The application has command line as **parameter=value** pattern\. You have to run the server with appropriate parameters\. These parameters are following:


* **PORT** \- Server listening port number\. If not specified, program will be run in regular expression test mode\.
* **CMD** \- Allowed command regular expression\. If not specified, none command will be available\.
* **NET** \- Allowed network addresses regular expression\. If not specified, none address will be available\.
* **TIMEOUT** \- If greater than **0**, there is the timeout of idle or connection idle in minutes\. Idle and unused files and connections will be closed after the time\.
* **LOOPWAIT** \- If greater than **0**, there is the waiting time in milliseconds within loop, while server does not receive any messages\. The longer time reduces CPU usage while waiting, but may cause less smooth client application responsiveness\.
* **Single letter** \- Directory mounting\. You can specify any letter and any directory\. Mounting whole system is not possible and will be potentially insecure\.

If value contains spaces or special characters, you can use the quotation characters\.

Example running command with listen on 8888, allow any system command, any network address and gives access into three directories:

```
WebApiServer PORT=8888 "CMD=^.*$" "NET=^.*$" A=/home/user/somedir B=/mnt/disk/otherdir Z=/usr/shared/files
```

# Regular expression test

If you not specify the **PORT** parameter, the application will run the regular expression test\. In this mode, there are several commands, every command should be prefixed by digit character\.


* **00** \- Exit from program\.
* **01** \- Clear all test cases\.
* **02** \- Use **CMD** parameter as test pattern\.
* **03** \- Use **NET** parameter as test pattern\.
* **1** \- Set test pattern\. The new pattern must be entered as string concatenated with first character\. For example, for set pattern `^.*$` you have to input `1^.*$` and press **Enter**\.
* **2** \- Add test case\. The new case must be concatenated with first character\. For add case `testaddr`, you have to input `2testaddr` and press **Enter**\.
* **3** \- Remove test case\. If the case exisits on test case list, the case will be removed\. For remove case `testaddr`, you have to input `3testaddr` and press **Enter**\.

After input any command, there will be printed current pattern and matching with every test case\.

## Test example

Assume, that you want to create regular expression, which allows to run executable files from /usr/bin/ subdirectory only\. The test cases should be match:

```
/usr/bin/telnet - match
/usr/bin/ssh - match
```

The test case should be not match:

```
usr/bin/telnet
/usr/share/mc
ssh
```

You can use the pattern:

```
^\/usr/bin/.*$
```

For test the pattern and the test cases, you have to run **WebApiServer** in tegular expression test and input following commands:

```
01
1^\/usr/bin/.*$
2/usr/bin/telnet
2/usr/bin/ssh
2usr/bin/telnet
2/usr/share/mc
2ssh
```

You will be informed, which cases matches the pattern\.

## HTML and JavaScript test

In the TestDemo subdirectory, there is the file **regextest\.html**, which contains the HTML\+JavaScript regex tester\.

This application consists of thw fields:


* **First field** \- Input the regular expression pattern\.
* **Second field** \- Input all test cases, one case per line\.

Below the fields, there will be shown the test result for every test case against pattern\.

Below the test result, there will be printes the commands for **WebApiServer** running in test mode, for perform the same regular expression test\.




