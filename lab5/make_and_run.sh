#!/bin/bash
set -ex

# sudo rm /dev/shm/srsv*

gcc -pthread -O3 -g -fsanitize=address -Wall -Wpedantic -Wextra -o gen generator.c -lrt
gcc -pthread -O3 -g -fsanitize=address -Wall -Wpedantic -Wextra -o pos poslužitelj.c -lrt

./gen 10 10 & ./pos 4 5
wait
sudo mount -t mqueue none mq
rm mq/msgq_example_name
sudo umnount

