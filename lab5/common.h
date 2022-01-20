#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#include <time.h>
#include <mqueue.h>
#include <sys/wait.h>
#include <sys/mman.h>
#include <fcntl.h>
#include <pthread.h>

#define NAZIV_REDA "/msgq_example_name"
#define MAX_PORUKA_U_REDU 5
#define MAX_VELICINA_PORUKE 64
typedef struct mq_attr mq_attr_t;

#define TICKS_PER_SECOND 10000000

void my_sleep(int seconds)
{
    for (volatile int i = 0; i < TICKS_PER_SECOND * seconds; ++i)
        ;
}

typedef struct
{
    int job_id;
    int duration;
    char name[64];
} my_job_t;

struct dijeljeno
{
    int jobs;
    pthread_mutex_t lock;
};
#define NAZIV_ZAJ_SPREMNIKA "/srsv_lab5" /* napravljno u /dev/shm/ */
