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

void init_poll_record_list();
struct poll_record *get_head_poll_record();
void insert_poll_record(struct poll_record *record);
struct poll_record *find_poll_record(int id);
void remove_poll_record(struct poll_record *record);
void remove_all_poll_records();
void update_poll_record_order(struct poll_record *record);

#endif
