@echo off

(echo ^@echo off
echo.
echo cd ..\Proto
echo ..\protobuf-net\ProtoGen\protogen.exe ^^
cd ..\Proto\
for %%i in (*.proto) do (if not %%i==%2 (if not %%i==%3 (echo -i:%%i ^^)))
cd ..\Gen
echo -o:..\Msg\Msg.cs -ns:LegendProtocol
echo cd ..\Gen)>__protogen.bat

call __protogen.bat