#include <stdio.h>
#include <stdlib.h>
#include <assert.h>
#include <math.h>
#include <pthread.h>
#include <sys/types.h>
#include <stdint.h>
#include "pollrecord.h"
#include "../common/utils.h"

static int next_free_id = 1;
struct poll_record *head = NULL;
pthread_mutex_t list_mutex; 
char *lock_owner = NULL;

void pr_init()
{
	pthread_mutexattr_t attr;

	if (pthread_mutexattr_init(&attr))
		perror("ERROR => Error attempting to init mutexattr. The error was:");

	//if (pthread_mutexattr_settype(&attr, PTHREAD_MUTEX_RECURSIVE)) 
	//    perror("ERROR => Error attempting to set mutex type to recursive. The error was");

	if (pthread_mutex_init(&list_mutex, &attr)) 
		perror("ERROR => Error attempting to init mutex. The error was");

	if (pthread_mutexattr_destroy(&attr)) 
		perror("ERROR => Error attempting to destroy mutexattr. The error was");
}

void pr_lock(char *new_lock_owner)
{
	assert(new_lock_owner);
	pthread_mutex_lock(&list_mutex);
	lock_owner = new_lock_owner;
}

void pr_unlock()
{
	assert(lock_owner);
	lock_owner = NULL;
	pthread_mutex_unlock(&list_mutex);
}

struct poll_record *pr_get_head()
{
	assert(lock_owner);
	return head;
}

void pr_insert(struct poll_record *record)
{
	assert(record);
	assert(lock_owner);

	if (record->id == 0) record->id = next_free_id++;

	/* Work out the next run time. */
	if (record->next_poll_time == 0) {
		record->next_poll_time = ceilf((float)get_time_in_ms() / 1000) * 1000;
	}

	/* Is the list empty? */
	if (!head)
	{
		head = record;
		record->prev = record->next = 0;
		return;
	}

	/* Find the first existing record that is set to run AFTER this one. */
	struct poll_record *current = (struct poll_record*)head;
    struct poll_record *last = NULL;
	while (current && current->next_poll_time < record->next_poll_time) {
		last = current;
		current = current->next;
	}

	/* Do we need to insert it at the head of the list? */
	if (last == NULL) {
		record->prev = NULL;
		record->next = current;
		head = record;
		current->prev = record;
	}
	/* Do we need to insert it at the tail of the list? */
	else if (current == NULL) {
		last->next = record;
		record->prev = last;
		record->next = NULL;
	}
	/* Otherwise, insert it between last and current. */
	else {
		record->prev = last;
		record->next = current;
		last->next = record;
		current->prev = record;
	}
}

void pr_remove(struct poll_record *record)
{
	assert(record);
	assert(lock_owner);

	if (head == record) {
		head = record->next;
		if (record->next) record->next->prev = NULL;
	}
	else {
		if (!record->prev) {
			fprintf(stderr, "record isn't in list.");
			return;
		}
		record->prev->next = record->next;
		if (record->next) record->next->prev = record->prev;
	}
	record->next = NULL;
	record->prev = NULL;
}

struct poll_record *pr_find(int id)
{
	assert(lock_owner);

	struct poll_record *current = (struct poll_record *)head;
	while (current) {
		if (current->id == id) return current;
		current = current->next;
	}
	return NULL;
}

void pr_clear_and_free_all()
{
	assert(lock_owner);

	struct poll_record *current, *next;
	current = head;
	while (current) {
		next = current->next;
		free(current);
		current = next;
	}
	head = NULL;
}

