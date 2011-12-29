#include <stdio.h>
#include <stdlib.h>
#include <math.h>
#include <pthread.h>
#include <sys/types.h>
#include <stdint.h>
#include "pollrecord.h"
#include "../common/utils.h"

int poll_record_next_free_id = 1;
volatile struct poll_record *head_poll_record = 0;

pthread_mutex_t head_poll_record_mutex; 

void init_poll_record_list()
{
	pthread_mutexattr_t attr;

	if (pthread_mutexattr_init(&attr))
		perror("ERROR => Error attempting to init mutexattr. The error was:");

	//if (pthread_mutexattr_settype(&attr, PTHREAD_MUTEX_RECURSIVE)) 
	//    perror("ERROR => Error attempting to set mutex type to recursive. The error was");

	if (pthread_mutex_init(&head_poll_record_mutex, &attr)) 
		perror("ERROR => Error attempting to init mutex. The error was");

	if (pthread_mutexattr_destroy(&attr)) 
		perror("ERROR => Error attempting to destroy mutexattr. The error was");
}

struct poll_record *get_head_poll_record()
{
	volatile struct poll_record *result;
	pthread_mutex_lock(&head_poll_record_mutex);
	result = head_poll_record;
	pthread_mutex_unlock(&head_poll_record_mutex);
	return (struct poll_record*)result;
}

void insert_poll_recordNotLocked(struct poll_record *record)
{

	// Working out the next run time.
	if (record->next_poll_time == 0) {
		record->next_poll_time = ceilf((float)get_time_in_ms() / 1000) * 1000;
	}

	// Is the list empty?
	if (head_poll_record == 0)
	{
		head_poll_record = record;
		record->prev = record->next = 0;
		pthread_mutex_unlock(&head_poll_record_mutex);
		return;
	}

	// Find the first existing record that is set to run AFTER this one.
	struct poll_record *current = (struct poll_record*)head_poll_record, *last = 0;
	while (current && current->next_poll_time < record->next_poll_time) {
		last = current;
		current = current->next;
	}

	// Do we need to insert it at the head of the list?
	if (last == 0)
	{
		record->prev = 0;
		record->next = current;
		head_poll_record = record;
		current->prev = record;
	}
	// Do we need to insert it at the tail of the list?
	else if (current == 0) {
		last->next = record;
		record->prev = last;
		record->next = 0;
	}
	// Otherwise, insert it between last and current.
	else
	{
		record->prev = last;
		record->next = current;
		last->next = record;
		current->prev = record;
	}

}

void insert_poll_record(struct poll_record *record)
{
	pthread_mutex_lock(&head_poll_record_mutex);
	insert_poll_recordNotLocked(record);
	pthread_mutex_unlock(&head_poll_record_mutex);
}

struct poll_record *find_poll_record(int id)
{
	pthread_mutex_lock(&head_poll_record_mutex);

	struct poll_record *current = (struct poll_record *)head_poll_record;
	while (current) {
		if (current->id == id) break;
		current = current->next;
	}

	pthread_mutex_unlock(&head_poll_record_mutex);

	return current;
}

void remove_poll_recordNotLocked(struct poll_record *record)
{
	// Is it the head?
	if (head_poll_record == record) {
		head_poll_record = record->next;
	}
	else {
		record->prev->next = record->next;
		if (record->next) record->next->prev = record->prev;
	}
	record->next = 0;
	record->prev = 0;

}

void remove_poll_record(struct poll_record *record)
{
	pthread_mutex_lock(&head_poll_record_mutex);
	remove_poll_recordNotLocked(record);
	pthread_mutex_unlock(&head_poll_record_mutex);
}

void remove_all_poll_records()
{
	volatile struct poll_record *record;
	pthread_mutex_lock(&head_poll_record_mutex);
	while ((record = head_poll_record))
	{
		head_poll_record = record->next;	
		free((void*)record);
	}
	pthread_mutex_unlock(&head_poll_record_mutex);
}

void update_poll_record_order(struct poll_record *record)
{
	pthread_mutex_lock(&head_poll_record_mutex);
	remove_poll_recordNotLocked(record);
	insert_poll_recordNotLocked(record);
	pthread_mutex_unlock(&head_poll_record_mutex);
}
