#include "common.h"

mqd_t generator_open(void) {
    mq_attr_t attr = {
        .mq_flags = 0,
        .mq_maxmsg = MAX_PORUKA_U_REDU,
        .mq_msgsize = MAX_VELICINA_PORUKE
    };
    mqd_t opisnik_reda = mq_open(NAZIV_REDA, O_WRONLY | O_CREAT, 00600, &attr);
    if (opisnik_reda == (mqd_t)-1)
    {
        perror("generator:mq_open");
        exit(1);
    }
    return opisnik_reda;
}

int genrator_snd(mqd_t opisnik_reda, my_job_t* job) {
    char poruka[64];
    unsigned prioritet = 10;
    sprintf(poruka, "%d %d %s", job->job_id, job->duration, job->name);
    size_t duljina = strlen(poruka) + 1;
    if (mq_send(opisnik_reda, poruka, duljina, prioritet))
    {
        perror("mq_send");
        return -1;
    }
    printf("Poslano: %s [prio=%d]\n", poruka, prioritet);
    return 0;
}

my_job_t* generate_spr(int id, size_t K) {
    char name[64];
    my_job_t* job;
    sprintf(name, "/srsv_lab5_%d", id);
    int sm = shm_open(name, O_CREAT | O_RDWR, 00600);
    if (sm == -1 || ftruncate(sm, sizeof(my_job_t)) == -1)
    {
        perror("shm_open/ftruncate");
        exit(1);
    }
    job = mmap(NULL, sizeof(my_job_t), PROT_READ | PROT_WRITE, MAP_SHARED, sm, 0);
    job->duration = id % K;
    job->job_id = id;
    memcpy(job->name, name, sizeof(job->name));
    return job;
}
int generator_reseve(size_t J) {
    int id = shm_open(NAZIV_ZAJ_SPREMNIKA, O_CREAT | O_RDWR, 00600);
    if (id == -1 || ftruncate(id, sizeof(struct dijeljeno)) == -1)
    {
        perror("shm_open/ftruncate");
        exit(1);
    }
    struct dijeljeno* x = mmap(NULL, sizeof(struct dijeljeno), PROT_READ | PROT_WRITE, MAP_SHARED, id, 0);
    pthread_mutex_lock(&x->lock);
    int start = x->jobs;
    x->jobs += J;
    pthread_mutex_unlock(&x->lock);
    return start;
}


int generator(size_t J, size_t K)
{
    mqd_t opisnik_reda = generator_open();
    
    int start = generator_reseve(J);
    printf("Start: %d", start);
    for (int i = 0; i < J; ++i) {
        my_job_t* job = generate_spr(start + i, K);
        genrator_snd(opisnik_reda, job);
        sleep(1);
    }
    mq_close(opisnik_reda);
    return 0;
}

int main(int argc, char** argv )
{
    size_t J = atoi(argv[1]), K = atoi(argv[2]);
    generator(J, K);
    return 0;
}