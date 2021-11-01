#!/bin/bash

for a in {1..10}
do
    b=2
    ss=$((a%b));
    # ss=1;
    $(./bin/Release/net6.0/dotnetLab1 Biciklo $ss $a &> output/outBiciklo$a-$ss.log) &
    sleep 0.5
    $(./bin/Release/net6.0/dotnetLab1 Auto $ss $a &> output/outAuto$a-$ss.log) &
    sleep 0.5
done