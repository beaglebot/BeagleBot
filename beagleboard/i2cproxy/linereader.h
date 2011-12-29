#ifndef READLINE_H
#define READLINE_H

#include <stdbool.h>

struct line_reader
{
	int (*read)(char *buffer, int max_bytes_to_read, void *data);
	char *buffer;
	int buffer_size;
	int next_free_byte;
	int first_used_byte;
	int next_place_to_start_scan;
	void *data;
};

/* Initializes a line_reader struct, ready for calls to read_line. */
void init_line_reader(struct line_reader *reader, 
		int (*read)(char *buffer, int max_num_bytes_to_read, void *data), 
		int buffer_size, void *data);

/* 
   Repeatedly reads data into the circular buffer until a full line is read. Returns:
   0 - success. The new line is copied into the result_buffer.
   1 - disconnected. The network connection was closed.
   2 - the result buffer wasn't big enough.
   3 - circular buffer is full, but no \n was found.
   4 - something really bad happened.
*/
int read_line(struct line_reader *reader, char *result_buffer, int result_buffer_size);

/* Whether the buffer is empty. */
bool is_empty(struct line_reader *reader);

/* Whether the buffer is full. */
bool is_full(struct line_reader *reader);

/* Discards any read data (ie empties, but doesn't deallocate the buffer. */
void reset(struct line_reader *reader);

/* Deallocates the buffer in the line_reader (previously allocated by call to init_line_reader). */
void close_reader(struct line_reader *reader);

#endif
