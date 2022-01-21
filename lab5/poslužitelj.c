#include "common.h"
#include "threads.h"
#include "assert.h"
#include "signal.h"
#include "stdbool.h"

typedef struct my_q_node{
    my_job_t job;
    struct my_q_node* next;
    struct my_q_node* prev;
} my_q_node_t;
typedef struct {
    my_q_node_t* head;
    my_q_node_t* tail;
    _Atomic size_t size;
} my_q_t;

my_q_t Q;
int stop = false;
pthread_mutex_t lock;
pthread_t* WORKERS[100] = {NULL};
pthread_cond_t cond_start;
pthread_mutex_t cond_lock;

void my_q_init(my_q_t* q) {
    q->head = NULL;
    q->tail = NULL;
    q->size = 0;
}
void my_q_push(my_q_t* q, my_job_t const * j) {
    pthread_mutex_lock(&lock);
    {
        my_q_node_t* j_heap = malloc(sizeof(my_q_node_t));
        memcpy(j_heap, j, sizeof(my_job_t));
        if (q->head == NULL) {
            j_heap->next = j_heap->next = NULL;
            q->head = q->tail = j_heap;
        }
        else {
            q->tail->next = j_heap;
            j_heap->prev = q->tail;
            j_heap->next = NULL;
        }
        ++q->size;
    }
    pthread_mutex_unlock(&lock);
}
my_job_t my_q_pop(my_q_t* q) {
    assert( (q->head != NULL) );
    my_q_node_t* cur = q->head;
    q->head = cur->next;
    my_job_t ret;
    memcpy(&ret, cur, sizeof(cur->job));
    free(cur);
    --q->size;
    if (q->size == 0) {
        q->tail = q->head = NULL;
    }
    return ret;
}
int my_q_is_empty(my_q_t const * q) {
    return q->head == NULL;
}
void potrosac_rcv(mqd_t opisnik_reda) {
    char poruka[MAX_VELICINA_PORUKE];
    int duljina;
    unsigned prioritet;
    struct timespec ts = {
        .tv_sec = time(NULL) + 30,
        .tv_nsec = 0
    };
    duljina = mq_timedreceive(opisnik_reda, poruka, MAX_VELICINA_PORUKE,
                         &prioritet, &ts);
    if (duljina < 0)
    {
        perror("mq_receive");
        return -1;
    }
    printf("Primljeno: %s \n", poruka);
    my_job_t j;
    if (sscanf(poruka, "%d %d %s", &j.job_id, &j.duration, j.name) != 3) {
        perror("sscanf");
    }
    my_q_push(&Q, &j);
}
int potrosac(size_t N, size_t M)
{
    mqd_t opisnik_reda = mq_open(NAZIV_REDA, O_RDONLY);
    if (opisnik_reda == (mqd_t)-1)
    {
        perror("potrosac:mq_open");
        return -1;
    }
    potrosac_rcv(opisnik_reda);
    
    if (Q.size >= N) {

        pthread_cond_broadcast(&cond_start);
    }
    pthread_mutex_lock(&lock);
    {
        int empty = my_q_is_empty(&Q);
        if (empty == 0)
            pthread_cond_broadcast(&cond_start);
    }
    pthread_mutex_unlock(&lock);

    mq_close(opisnik_reda);
    return 0;
}



void radna(int* id) {
    printf("R[%d] Starting\n", *id);
    my_job_t job;
    int empty = 1;
    while(!stop) {

        pthread_cond_wait(&cond_start, &cond_lock);
        pthread_mutex_lock(&lock);
        {
            empty = my_q_is_empty(&Q);
            if (empty == 0)
                job = my_q_pop(&Q);
        }
        pthread_mutex_unlock(&lock);
        if (empty == 0) {
            for(int i = 0; i < job.duration ; ++i) {
                printf("R[%d] doing job %d. %d/%d\n", *id, job.job_id, i + 1, job.duration);
                my_sleep(1);
            }
        }
    }
    printf("R[%d] Stoping\n", *id);
    free(id);
}



int main(int argc, char** argv)
{
    int N = atoi(argv[1]), M = atoi(argv[2]);
    my_q_init(&Q);
    for (int i = 0; i < N ; ++i) {
        WORKERS[i] = malloc(sizeof(pthread_t));
        int *id=malloc(sizeof(int));
        *id = i;
        pthread_create(WORKERS[i],  NULL, radna, id);
    }
    while(!potrosac(N, M));
    exit(0);
    return 0;
}
