#include "common.h"
#include "threads.h"
#include "assert.h"

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


void my_q_init(my_q_t* q) {
    q->head = NULL;
    q->tail = NULL;
    q->size = 0;
}
void my_q_push(my_q_t* q, my_job_t const * j) {
    my_q_node_t* j_heap = malloc(sizeof(my_q_node_t));
    memcpy(&j_heap->job, j, sizeof(j));
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
my_job_t my_q_pop(my_q_t* q) {
    assert( (q->head != NULL) );
    my_q_node_t* cur = q->head;
    q->head = q->head->next;
    my_job_t ret;
    memcpy(&ret, cur, sizeof(cur->job));
    free(cur);
    --q->size;
    return ret;
}
int my_q_is_empty(my_q_t q) {
    return q.head == NULL;
}
void potrosac_rcv(mqd_t opisnik_reda) {
    char poruka[MAX_VELICINA_PORUKE];
    int duljina;
    unsigned prioritet;
    struct timespec ts = {
        .tv_sec = 30,
        .tv_nsec = 0
    };
    duljina = mq_timedreceive(opisnik_reda, poruka, MAX_VELICINA_PORUKE,
                         &prioritet, &ts);
    if (duljina < 0)
    {
        perror("mq_receive");
        return -1;
    }
    printf("Primljeno: %s [prio=%d]\n", poruka, prioritet);
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
    
    mq_close(opisnik_reda);
    return 0;
}

int main(int argc, char** argv)
{
    int N = atoi(argv[0]), M = atoi(argv[1]);
    my_q_init(&Q);
    while(!potrosac(N, M));
    exit(0);
    return 0;
}
