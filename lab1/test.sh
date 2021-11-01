dotnet build -c Release

$(bin/Release/net6.0/dotnetLab1 UPR &> output/outUPR.log) &
$(./bin/Release/net6.0/dotnetLab1 semafor 0 &> output/out0.log) &
$(./bin/Release/net6.0/dotnetLab1 semafor 1 &> output/out1.log) &
$(./bin/Release/net6.0/dotnetLab1 Biciklo 1 1 &> output/outBiciklo2.log) &