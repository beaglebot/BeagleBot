#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/socket.h>
#include <netinet/in.h>
#include "linereader.h"

void init_line_reader(struct line_reader *reader, 
		int (*read)(char *buffer, int max_num_bytes_to_read, void *data), 
		int buffer_size, void *data)
{
	reader->read = read;
	reader->buffer_size = buffer_size;
	reader->buffer = (char*)malloc(buffer_size);
	reader->next_free_byte = 0;
	reader->first_used_byte = -1;
	reader->next_place_to_start_scan = -1;
	reader->data = data;
}

enum State
{
	Empty,
	SingleSegment,
	DoubleSegment
};

int read_line(struct line_reader *reader, char *result_buffer, int result_buffer_size)
{
	int first_used_byte = reader->first_used_byte;
	int next_free_byte = reader->next_free_byte;
	int next_place_to_start_scan = reader->next_place_to_start_scan;
	int buffer_size = reader->buffer_size;
	char *buffer = reader->buffer;
	
	while (1)
	{
		// What state is the buffer in?
		enum State state;
		if (first_used_byte == -1 && next_free_byte == 0)
			state = Empty;
		else if (first_used_byte > next_free_byte && next_free_byte > 0 || first_used_byte == next_free_byte && next_free_byte > 0)
			state = DoubleSegment;
		else 
			state = SingleSegment;

		// Is there a line in the buffer already?
		if (state == SingleSegment)
		{
			int count = next_free_byte - next_place_to_start_scan;
			if (count <= 0) count = buffer_size - next_place_to_start_scan;
			char *index = memchr(&buffer[next_place_to_start_scan], 0x0A, count);
			if (index)
			{
				next_place_to_start_scan = index + 1 - buffer;
				if (next_place_to_start_scan == buffer_size) next_place_to_start_scan = 0;

				int bytesToCopy = index - buffer - first_used_byte + 1;
				if (bytesToCopy > result_buffer_size - 1) {
					fprintf(stderr, "ERROR => Result buffer too small. bytesToCopy=%d\n", bytesToCopy);
					reader->first_used_byte = first_used_byte;
					reader->next_free_byte = next_free_byte;
					reader->next_place_to_start_scan = next_place_to_start_scan;
					result_buffer[0] = 0;
					return 2;
				}
				memcpy(result_buffer, &buffer[first_used_byte], bytesToCopy);
				result_buffer[bytesToCopy] = 0;
				first_used_byte = index - buffer + 1;
				if (first_used_byte == buffer_size) first_used_byte = 0;

				// Have we emptied the buffer?
				if (first_used_byte == next_free_byte) { next_place_to_start_scan = first_used_byte = -1; next_free_byte = 0; }

				reader->first_used_byte = first_used_byte;
				reader->next_free_byte = next_free_byte;
				reader->next_place_to_start_scan = next_place_to_start_scan;
 
				return 0;
			}
			next_place_to_start_scan = next_free_byte;
		}
		else if (state == DoubleSegment)
		{
			// Check the last segment. Can't be in first segment as we must have checked it earlier.
			char *index = memchr(&buffer[next_place_to_start_scan], 0x0A, next_free_byte-next_place_to_start_scan);
			if (index)
			{
				next_place_to_start_scan = index - buffer + 1;
				if (next_place_to_start_scan == buffer_size) next_place_to_start_scan = 0;

				int bytesToCopy = (buffer_size - first_used_byte) + (index - buffer + 1);
				if (bytesToCopy > result_buffer_size - 1) {
					fprintf(stderr, "ERROR => Result buffer too small (3). bytesToCopy=%d,buffer=%d\n", bytesToCopy, buffer_size);
					reader->first_used_byte = first_used_byte;
					reader->next_free_byte = next_free_byte;
					reader->next_place_to_start_scan = next_place_to_start_scan;
					result_buffer[0] = 0;
					return 2;
				}
				memcpy(result_buffer, &buffer[first_used_byte], buffer_size - first_used_byte);
				memcpy(&result_buffer[buffer_size - first_used_byte], buffer, index - buffer + 1);
				result_buffer[bytesToCopy] = 0;
				first_used_byte = index - buffer + 1;
				if (first_used_byte == buffer_size) first_used_byte = 0;

				// Have we emptied the buffer?
				if (first_used_byte == next_free_byte) { next_place_to_start_scan = first_used_byte = -1; next_free_byte = 0; }

				reader->first_used_byte = first_used_byte;
				reader->next_free_byte = next_free_byte;
				reader->next_place_to_start_scan = next_place_to_start_scan;

				return 0;
			}
			next_place_to_start_scan = next_free_byte;
		} 

		// Is the buffer full?
		if (first_used_byte == next_free_byte) {
			fprintf(stderr, "ERROR => Buffer overflow. Received more than %d bytes without a new line character.\n", buffer_size);

			reader->first_used_byte = first_used_byte;
			reader->next_free_byte = next_free_byte;
			reader->next_place_to_start_scan = next_place_to_start_scan;
			result_buffer[0] = 0;

			return 3;
		}

		// Work out the amount of data we can recieve.
		int maxBytesCanReceive;
		if (first_used_byte < next_free_byte)
			maxBytesCanReceive = buffer_size - next_free_byte;
		else
			maxBytesCanReceive = first_used_byte - next_free_byte;

		// Receive it.
		int bytesReceived = reader->read(&buffer[next_free_byte], maxBytesCanReceive, reader->data);

		// Was the connection closed?
		if (bytesReceived == 0) {

			reader->first_used_byte = first_used_byte;
			reader->next_free_byte = next_free_byte;
			reader->next_place_to_start_scan = next_place_to_start_scan;
			result_buffer[0] = 0;

			return 1;
		}

		// Update the buffer indicies.
		if (first_used_byte == -1) next_place_to_start_scan = first_used_byte = next_free_byte;
		next_free_byte += bytesReceived;
		if (next_free_byte == buffer_size) next_free_byte = 0;
	}
}

bool isEmpty(struct line_reader *reader)
{
	return reader->first_used_byte == -1;
}

bool isFull(struct line_reader *reader)
{
	return reader->first_used_byte == reader->next_free_byte;
}

void reset(struct line_reader *reader)
{
	reader->next_free_byte = 0;
	reader->first_used_byte = -1;
	reader->next_place_to_start_scan = -1;	
}

void close_reader(struct line_reader *reader)
{
	if (reader->buffer) free(reader->buffer);
	reader->buffer = 0;
	reader->buffer_size = 0;
}
