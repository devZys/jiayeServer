@echo off

cd Msg

echo --delete all files in bin\Release\ [删除旧的生成文件]
del /q bin\Release\*

echo --delete all files in obj\Release\ [删除旧的生成文件]
del /q obj\Release\*

echo --copy proto files to local proto directory [复制proto文件到本地proto目录里]
copy /y ..\..\LegendProtocol\Msg\Proto\*.* ..\Proto\

echo --gen proto message to Msg.cs [生成解析代码,全部放到一个cs源码里]
cd ..\Gen
call __buildprotogenbat.bat %1 %2 %3

echo --compile Msg.cs [编译源码 生成DLL]
cd ..\Msg
C:\Windows\Microsoft.NET\Framework\v4.0.30319\Csc.exe /noconfig /nowarn:1701,1702 /nostdlib+ /errorreport:prompt /warn:4 /define:TRACE /reference:C:\Windows\Microsoft.NET\Framework\v2.0.50727\mscorlib.dll /reference:protobuf-net.dll /reference:C:\Windows\Microsoft.NET\Framework\v2.0.50727\System.dll /debug:pdbonly /filealign:512 /optimize+ /out:obj\Release\Msg.dll /target:library /utf8output Msg.cs Properties\AssemblyInfo.cs

echo --output to bin\Release\ [设置DLL输出路径]
copy /y obj\Release\Msg.dll bin\Release\Msg.dll
copy /y obj\Release\Msg.pdb bin\Release\Msg.pdb
copy /y protobuf-net.dll bin\Release\protobuf-net.dll

echo --precompile Msg.dll [生成专门序列化的DLL文件]
cd bin\Release
..\..\..\protobuf-net\Precompile\precompile.exe Msg.dll -o:MsgSerializer.dll -t:MsgSerializer

: 复制文件到指定文件夹
echo --copy dlls to bin path [复制生成的Msg.dll与MsgSerializer.dll以及protobuf-net.dll到当前Bin目录]
copy /y Msg.dll ..\..\..\Bin\Msg.dll
copy /y MsgSerializer.dll ..\..\..\Bin\MsgSerializer.dll
copy /y protobuf-net.dll ..\..\..\Bin\protobuf-net.dll

echo --copy Msg.cs to Msg project [复制生成的Msg.cs文件到LegendProtocol工程的Msg目录中]
copy /y ..\..\Msg.cs ..\..\..\..\LegendProtocol\Msg\Msg.cs

echo --copy dlls to lib project [复制生成的Msg.dll与MsgSerializer.dll以及protobuf-net.dll到服务器lib目录中]
copy /y Msg.dll ..\..\..\..\lib\Msg.dll
copy /y MsgSerializer.dll ..\..\..\..\lib\MsgSerializer.dll
copy /y protobuf-net.dll ..\..\..\..\lib\protobuf-net.dll

echo --copy dlls to Mahjong unity project [复制生成的Msg.dll与MsgSerializer.dll以及protobuf-net.dll到客户端Plugin目录中]
copy /y Msg.dll ..\..\..\..\..\LegendClient\Mahjong\Assets\Plugins\Msg.dll
copy /y MsgSerializer.dll ..\..\..\..\..\LegendClient\Mahjong\Assets\Plugins\MsgSerializer.dll
copy /y protobuf-net.dll ..\..\..\..\..\LegendClient\Mahjong\Assets\Plugins\protobuf-net.dll

echo --copy dlls to RunFast unity project [复制生成的Msg.dll与MsgSerializer.dll以及protobuf-net.dll到客户端Plugin目录中]
copy /y Msg.dll ..\..\..\..\..\LegendClient\RunFast\Assets\Plugins\Msg.dll
copy /y MsgSerializer.dll ..\..\..\..\..\LegendClient\RunFast\Assets\Plugins\MsgSerializer.dll
copy /y protobuf-net.dll ..\..\..\..\..\LegendClient\RunFast\Assets\Plugins\protobuf-net.dll

echo --copy dlls to YiYangWordPlate unity project [复制生成的Msg.dll与MsgSerializer.dll以及protobuf-net.dll到客户端Plugin目录中]
copy /y Msg.dll ..\..\..\..\..\LegendClient\YiYangWordPlate\Assets\Plugins\Msg.dll
copy /y MsgSerializer.dll ..\..\..\..\..\LegendClient\YiYangWordPlate\Assets\Plugins\MsgSerializer.dll
copy /y protobuf-net.dll ..\..\..\..\..\LegendClient\YiYangWordPlate\Assets\Plugins\protobuf-net.dll

echo --copy dlls to XingShaMahjong unity project [复制生成的Msg.dll与MsgSerializer.dll以及protobuf-net.dll到客户端Plugin目录中]
copy /y Msg.dll ..\..\..\..\..\LegendClient\XingShaMahjong\Assets\Plugins\Msg.dll
copy /y MsgSerializer.dll ..\..\..\..\..\LegendClient\XingShaMahjong\Assets\Plugins\MsgSerializer.dll
copy /y protobuf-net.dll ..\..\..\..\..\LegendClient\XingShaMahjong\Assets\Plugins\protobuf-net.dll

echo --copy dlls to deploy project [复制生成的Msg.dll与MsgSerializer.dll以及protobuf-net.dll到服务器Build目录中]
copy /y Msg.dll ..\..\..\..\..\PhotonServer\deploy\Build\Msg.dll
copy /y MsgSerializer.dll ..\..\..\..\..\PhotonServer\deploy\Build\MsgSerializer.dll
copy /y protobuf-net.dll ..\..\..\..\..\PhotonServer\deploy\Build\protobuf-net.dll

echo --copy dlls to LegendServerBox project [复制生成的Msg.dll与MsgSerializer.dll以及protobuf-net.dll到后台工具Box目录中]
copy /y Msg.dll ..\..\..\..\..\LegendServer\AssistantServices\LegendServerBox\bin\Msg.dll
copy /y MsgSerializer.dll ..\..\..\..\..\LegendServer\AssistantServices\LegendServerBox\bin\MsgSerializer.dll
copy /y protobuf-net.dll ..\..\..\..\..\LegendServer\AssistantServices\LegendServerBox\bin\protobuf-net.dll

echo --copy dlls to LegendServerBox project [复制生成的Msg.dll与MsgSerializer.dll以及protobuf-net.dll到后台WEB目录中]
copy /y Msg.dll ..\..\..\..\..\LegendServer\AssistantServices\WebSystem\CustomerSystem\bin\Msg.dll
copy /y MsgSerializer.dll ..\..\..\..\..\LegendServer\AssistantServices\WebSystem\CustomerSystem\bin\MsgSerializer.dll
copy /y protobuf-net.dll ..\..\..\..\..\LegendServer\AssistantServices\WebSystem\CustomerSystem\bin\protobuf-net.dll

echo --copy dlls to LegendServerBox project [复制生成的Msg.dll与MsgSerializer.dll以及protobuf-net.dll到后台WEB目录中]
copy /y Msg.dll ..\..\..\..\..\LegendServer\AssistantServices\WebSystem\GameData\bin\Msg.dll
copy /y MsgSerializer.dll ..\..\..\..\..\LegendServer\AssistantServices\WebSystem\GameData\bin\MsgSerializer.dll
copy /y protobuf-net.dll ..\..\..\..\..\LegendServer\AssistantServices\WebSystem\GameData\bin\protobuf-net.dll

echo --copy dlls to LegendServerBox project [复制生成的Msg.dll与MsgSerializer.dll以及protobuf-net.dll到后台WEB目录中]
copy /y Msg.dll ..\..\..\..\..\LegendServer\AssistantServices\WebSystem\ListServer\bin\Msg.dll
copy /y MsgSerializer.dll ..\..\..\..\..\LegendServer\AssistantServices\WebSystem\ListServer\bin\MsgSerializer.dll
copy /y protobuf-net.dll ..\..\..\..\..\LegendServer\AssistantServices\WebSystem\ListServer\bin\protobuf-net.dll

echo --copy dlls to LegendServerBox project [复制生成的Msg.dll与MsgSerializer.dll以及protobuf-net.dll到后台WEB目录中]
copy /y Msg.dll ..\..\..\..\..\LegendServer\AssistantServices\WebSystem\MobileTableGameGM\bin\Msg.dll
copy /y MsgSerializer.dll ..\..\..\..\..\LegendServer\AssistantServices\WebSystem\MobileTableGameGM\bin\MsgSerializer.dll
copy /y protobuf-net.dll ..\..\..\..\..\LegendServer\AssistantServices\WebSystem\MobileTableGameGM\bin\protobuf-net.dll

echo --copy dlls to LegendServerWeb project [复制生成的Msg.dll与MsgSerializer.dll以及protobuf-net.dll到WebServer目录中]
copy /y Msg.dll ..\..\..\..\..\LegendServer\LegendServerWeb\LegendServerWeb\bin\Msg.dll
copy /y MsgSerializer.dll ..\..\..\..\..\LegendServer\LegendServerWeb\LegendServerWeb\bin\MsgSerializer.dll
copy /y protobuf-net.dll ..\..\..\..\..\LegendServer\LegendServerWeb\LegendServerWeb\bin\protobuf-net.dll

echo 游戏：【%1】 协议编译完毕，过滤掉协议库：【%2 %3】，请检查控制台的日志信息

pause
exit