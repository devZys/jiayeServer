@echo off

cd Msg

echo --delete all files in bin\Release\ [ɾ���ɵ������ļ�]
del /q bin\Release\*

echo --delete all files in obj\Release\ [ɾ���ɵ������ļ�]
del /q obj\Release\*

echo --copy proto files to local proto directory [����proto�ļ�������protoĿ¼��]
copy /y ..\..\LegendProtocol\Msg\Proto\*.* ..\Proto\

echo --gen proto message to Msg.cs [���ɽ�������,ȫ���ŵ�һ��csԴ����]
cd ..\Gen
call __buildprotogenbat.bat %1 %2 %3

echo --compile Msg.cs [����Դ�� ����DLL]
cd ..\Msg
C:\Windows\Microsoft.NET\Framework\v4.0.30319\Csc.exe /noconfig /nowarn:1701,1702 /nostdlib+ /errorreport:prompt /warn:4 /define:TRACE /reference:C:\Windows\Microsoft.NET\Framework\v2.0.50727\mscorlib.dll /reference:protobuf-net.dll /reference:C:\Windows\Microsoft.NET\Framework\v2.0.50727\System.dll /debug:pdbonly /filealign:512 /optimize+ /out:obj\Release\Msg.dll /target:library /utf8output Msg.cs Properties\AssemblyInfo.cs

echo --output to bin\Release\ [����DLL���·��]
copy /y obj\Release\Msg.dll bin\Release\Msg.dll
copy /y obj\Release\Msg.pdb bin\Release\Msg.pdb
copy /y protobuf-net.dll bin\Release\protobuf-net.dll

echo --precompile Msg.dll [����ר�����л���DLL�ļ�]
cd bin\Release
..\..\..\protobuf-net\Precompile\precompile.exe Msg.dll -o:MsgSerializer.dll -t:MsgSerializer

: �����ļ���ָ���ļ���
echo --copy dlls to bin path [�������ɵ�Msg.dll��MsgSerializer.dll�Լ�protobuf-net.dll����ǰBinĿ¼]
copy /y Msg.dll ..\..\..\Bin\Msg.dll
copy /y MsgSerializer.dll ..\..\..\Bin\MsgSerializer.dll
copy /y protobuf-net.dll ..\..\..\Bin\protobuf-net.dll

echo --copy Msg.cs to Msg project [�������ɵ�Msg.cs�ļ���LegendProtocol���̵�MsgĿ¼��]
copy /y ..\..\Msg.cs ..\..\..\..\LegendProtocol\Msg\Msg.cs

echo --copy dlls to lib project [�������ɵ�Msg.dll��MsgSerializer.dll�Լ�protobuf-net.dll��������libĿ¼��]
copy /y Msg.dll ..\..\..\..\lib\Msg.dll
copy /y MsgSerializer.dll ..\..\..\..\lib\MsgSerializer.dll
copy /y protobuf-net.dll ..\..\..\..\lib\protobuf-net.dll

echo --copy dlls to Mahjong unity project [�������ɵ�Msg.dll��MsgSerializer.dll�Լ�protobuf-net.dll���ͻ���PluginĿ¼��]
copy /y Msg.dll ..\..\..\..\..\LegendClient\Mahjong\Assets\Plugins\Msg.dll
copy /y MsgSerializer.dll ..\..\..\..\..\LegendClient\Mahjong\Assets\Plugins\MsgSerializer.dll
copy /y protobuf-net.dll ..\..\..\..\..\LegendClient\Mahjong\Assets\Plugins\protobuf-net.dll

echo --copy dlls to RunFast unity project [�������ɵ�Msg.dll��MsgSerializer.dll�Լ�protobuf-net.dll���ͻ���PluginĿ¼��]
copy /y Msg.dll ..\..\..\..\..\LegendClient\RunFast\Assets\Plugins\Msg.dll
copy /y MsgSerializer.dll ..\..\..\..\..\LegendClient\RunFast\Assets\Plugins\MsgSerializer.dll
copy /y protobuf-net.dll ..\..\..\..\..\LegendClient\RunFast\Assets\Plugins\protobuf-net.dll

echo --copy dlls to YiYangWordPlate unity project [�������ɵ�Msg.dll��MsgSerializer.dll�Լ�protobuf-net.dll���ͻ���PluginĿ¼��]
copy /y Msg.dll ..\..\..\..\..\LegendClient\YiYangWordPlate\Assets\Plugins\Msg.dll
copy /y MsgSerializer.dll ..\..\..\..\..\LegendClient\YiYangWordPlate\Assets\Plugins\MsgSerializer.dll
copy /y protobuf-net.dll ..\..\..\..\..\LegendClient\YiYangWordPlate\Assets\Plugins\protobuf-net.dll

echo --copy dlls to XingShaMahjong unity project [�������ɵ�Msg.dll��MsgSerializer.dll�Լ�protobuf-net.dll���ͻ���PluginĿ¼��]
copy /y Msg.dll ..\..\..\..\..\LegendClient\XingShaMahjong\Assets\Plugins\Msg.dll
copy /y MsgSerializer.dll ..\..\..\..\..\LegendClient\XingShaMahjong\Assets\Plugins\MsgSerializer.dll
copy /y protobuf-net.dll ..\..\..\..\..\LegendClient\XingShaMahjong\Assets\Plugins\protobuf-net.dll

echo --copy dlls to deploy project [�������ɵ�Msg.dll��MsgSerializer.dll�Լ�protobuf-net.dll��������BuildĿ¼��]
copy /y Msg.dll ..\..\..\..\..\PhotonServer\deploy\Build\Msg.dll
copy /y MsgSerializer.dll ..\..\..\..\..\PhotonServer\deploy\Build\MsgSerializer.dll
copy /y protobuf-net.dll ..\..\..\..\..\PhotonServer\deploy\Build\protobuf-net.dll

echo --copy dlls to LegendServerBox project [�������ɵ�Msg.dll��MsgSerializer.dll�Լ�protobuf-net.dll����̨����BoxĿ¼��]
copy /y Msg.dll ..\..\..\..\..\LegendServer\AssistantServices\LegendServerBox\bin\Msg.dll
copy /y MsgSerializer.dll ..\..\..\..\..\LegendServer\AssistantServices\LegendServerBox\bin\MsgSerializer.dll
copy /y protobuf-net.dll ..\..\..\..\..\LegendServer\AssistantServices\LegendServerBox\bin\protobuf-net.dll

echo --copy dlls to LegendServerBox project [�������ɵ�Msg.dll��MsgSerializer.dll�Լ�protobuf-net.dll����̨WEBĿ¼��]
copy /y Msg.dll ..\..\..\..\..\LegendServer\AssistantServices\WebSystem\CustomerSystem\bin\Msg.dll
copy /y MsgSerializer.dll ..\..\..\..\..\LegendServer\AssistantServices\WebSystem\CustomerSystem\bin\MsgSerializer.dll
copy /y protobuf-net.dll ..\..\..\..\..\LegendServer\AssistantServices\WebSystem\CustomerSystem\bin\protobuf-net.dll

echo --copy dlls to LegendServerBox project [�������ɵ�Msg.dll��MsgSerializer.dll�Լ�protobuf-net.dll����̨WEBĿ¼��]
copy /y Msg.dll ..\..\..\..\..\LegendServer\AssistantServices\WebSystem\GameData\bin\Msg.dll
copy /y MsgSerializer.dll ..\..\..\..\..\LegendServer\AssistantServices\WebSystem\GameData\bin\MsgSerializer.dll
copy /y protobuf-net.dll ..\..\..\..\..\LegendServer\AssistantServices\WebSystem\GameData\bin\protobuf-net.dll

echo --copy dlls to LegendServerBox project [�������ɵ�Msg.dll��MsgSerializer.dll�Լ�protobuf-net.dll����̨WEBĿ¼��]
copy /y Msg.dll ..\..\..\..\..\LegendServer\AssistantServices\WebSystem\ListServer\bin\Msg.dll
copy /y MsgSerializer.dll ..\..\..\..\..\LegendServer\AssistantServices\WebSystem\ListServer\bin\MsgSerializer.dll
copy /y protobuf-net.dll ..\..\..\..\..\LegendServer\AssistantServices\WebSystem\ListServer\bin\protobuf-net.dll

echo --copy dlls to LegendServerBox project [�������ɵ�Msg.dll��MsgSerializer.dll�Լ�protobuf-net.dll����̨WEBĿ¼��]
copy /y Msg.dll ..\..\..\..\..\LegendServer\AssistantServices\WebSystem\MobileTableGameGM\bin\Msg.dll
copy /y MsgSerializer.dll ..\..\..\..\..\LegendServer\AssistantServices\WebSystem\MobileTableGameGM\bin\MsgSerializer.dll
copy /y protobuf-net.dll ..\..\..\..\..\LegendServer\AssistantServices\WebSystem\MobileTableGameGM\bin\protobuf-net.dll

echo --copy dlls to LegendServerWeb project [�������ɵ�Msg.dll��MsgSerializer.dll�Լ�protobuf-net.dll��WebServerĿ¼��]
copy /y Msg.dll ..\..\..\..\..\LegendServer\LegendServerWeb\LegendServerWeb\bin\Msg.dll
copy /y MsgSerializer.dll ..\..\..\..\..\LegendServer\LegendServerWeb\LegendServerWeb\bin\MsgSerializer.dll
copy /y protobuf-net.dll ..\..\..\..\..\LegendServer\LegendServerWeb\LegendServerWeb\bin\protobuf-net.dll

echo ��Ϸ����%1�� Э�������ϣ����˵�Э��⣺��%2 %3�����������̨����־��Ϣ

pause
exit