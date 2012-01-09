#ifndef POLLRECORD_H
#define POLLRECORD_H

extern int poll_record_next_free_id;

struct poll_record
{
	int id;
	long next_poll_time;
	int delay;
	uint8_t address;
	uint8_t reg;
	uint8_t num_regs_to_read;
	struct poll_record *prev;
	struct poll_record *next;
} ;

void pr_init();

void pr_lock();
void pr_unlock();

struct poll_record *pr_get_head();
void pr_insert(struct poll_record *record);
void pr_remove(struct poll_record *record);
struct poll_record *pr_find(int id);
void pr_clear_and_free_all();

#endif
