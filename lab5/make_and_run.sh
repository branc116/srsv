#!/bin/bash
set -ex

gcc -pthread -O3 -g -fsanitize=address -Wall -Wpedantic -Wextra -o gen generator.c -lrt
gcc -pthread -O3 -g -fsanitize=address -Wall -Wpedantic -Wextra -o pos poslužitelj.c -lrt

./gen 10 10
wait
sudo mount -t mqueue none mq
rm mq/msgq_example_name
sudo umnount
sudo rm /dev/shm/srsv-lab5
