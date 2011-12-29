#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include "linereader.h"

char *mock_reads_to_return[10];
int next_mock_read = 0;
int num_mock_reads = 0;

void assert(bool value)
{
	if (!value)
	{
		printf("ASSERT failed\n");
		exit(1);
	}
}

void add_mock_read(char *result)
{
	mock_reads_to_return[num_mock_reads++] = result;
}

void clear_mock_reads()
{
	num_mock_reads = 0;
	next_mock_read = 0;
}

int mock_read(char *buffer, int max_num_bytes_to_read, void *data)
{
	int len;

	if (next_mock_read == num_mock_reads) {
		fprintf(stderr, "Too many reads - not enough mock data\n");
		exit(1);
	}

	len = strlen(mock_reads_to_return[next_mock_read]);
	if (len > max_num_bytes_to_read) {
		fprintf(stderr, "Test data is too long\n");
		exit(1);
	}

	memcpy(buffer, mock_reads_to_return[next_mock_read++], len);
	return len;
}

void receive_one_line()
{
	char result[30];
	struct line_reader reader;
	int failed;

	printf("Starting test receive_one_line\n");
	clear_mock_reads();
	add_mock_read("test\n");

	init_line_reader(&reader, mock_read, 20, 0);

	failed = read_line(&reader, result, sizeof(result));

	assert(!failed);
	assert(strcmp(result,"test\n") == 0);
	assert(isEmpty(&reader));
	assert(!isFull(&reader));

	close_reader(&reader);
}

void one_line_in_multiple_pieces()
{
	struct line_reader reader;
	char result[30];
	int failed;

	printf("Starting test one_line_in_multiple_pieces\n");
	clear_mock_reads();
	add_mock_read("te");
	add_mock_read("st\n");

	init_line_reader(&reader, mock_read, 20, 0);

	failed = read_line(&reader, result, sizeof(result));

	assert(!failed);
	assert(strcmp(result,"test\n") == 0);
	assert(isEmpty(&reader));
	assert(!isFull(&reader));

	close_reader(&reader);
}

void receive_multiple_lines_at_once()
{
	char result[30];
	struct line_reader reader;
	int failed;

	printf("Starting test receive_multiple_lines_at_once\n");
	clear_mock_reads();
	add_mock_read("test1\ntest2\ntest3\n");

	init_line_reader(&reader, mock_read, 20, 0);

	failed = read_line(&reader, result, sizeof(result));
	assert(!failed);
	assert(strcmp(result,"test1\n") == 0);
	assert(!isEmpty(&reader));
	assert(!isFull(&reader));
	
	failed = read_line(&reader, result, sizeof(result));
	assert(!failed);
	assert(strcmp(result,"test2\n") == 0);
	assert(!isEmpty(&reader));
	assert(!isFull(&reader));

	failed = read_line(&reader, result, sizeof(result));
	assert(!failed);
	assert(strcmp(result,"test3\n") == 0);
	assert(isEmpty(&reader));
	assert(!isFull(&reader));
	
	close_reader(&reader);
}

void wrap_around()
{
	char result[30];
	struct line_reader reader;
	int failed;

	printf("Starting test wrap_around\n");
	clear_mock_reads();
	add_mock_read("test\n1");
	add_mock_read("23");
	add_mock_read("45");
	add_mock_read("678\n");
	
	init_line_reader(&reader, mock_read, 10, 0);

	failed = read_line(&reader, result, sizeof(result));
	assert(!failed);
	assert(strcmp(result,"test\n") == 0);
	assert(!isEmpty(&reader));
	assert(!isFull(&reader));
	
	failed = read_line(&reader, result, sizeof(result));
	assert(!failed);
	assert(strcmp(result,"12345678\n") == 0);
	assert(isEmpty(&reader));
	assert(!isFull(&reader));

	close_reader(&reader);
}

void fill_buffer()
{
	char result[30];
	struct line_reader reader;
	int failed;

	printf("Starting test fill_buffer\n");
	clear_mock_reads();	
	add_mock_read("123456789\n");
	
	init_line_reader(&reader, mock_read, 10, 0);

	failed = read_line(&reader, result, sizeof(result));
	assert(!failed);
	assert(strcmp(result,"123456789\n") == 0);
	assert(isEmpty(&reader));
	assert(!isFull(&reader));
	
	close_reader(&reader);
}

void wrap_around_and_fill_buffer()
{
	char result[30];
	struct line_reader reader;
	int failed;

	printf("Starting test wrap_around_and_fill_buffer\n");
	clear_mock_reads();
	add_mock_read("ab\n1");
	add_mock_read("234567");
	add_mock_read("89\n");

	init_line_reader(&reader, mock_read, 10, 0);

	failed = read_line(&reader, result, sizeof(result));
	assert(!failed);
	assert(strcmp(result,"ab\n") == 0);
	assert(!isEmpty(&reader));
	assert(!isFull(&reader));

	failed = read_line(&reader, result, sizeof(result));
	assert(!failed);
	assert(strcmp(result,"123456789\n") == 0);
	assert(isEmpty(&reader));
	assert(!isFull(&reader));

	close_reader(&reader);
}

void to_the_end_of_the_buffer()
{
	char result[30];
	struct line_reader reader;
	int failed;

	printf("Starting test to_the_end_of_the_buffer\n");
	clear_mock_reads();
	add_mock_read("TEST\ntest");
	add_mock_read("\n");

	init_line_reader(&reader, mock_read, 10, 0);

	failed = read_line(&reader, result, sizeof(result));
	assert(!failed);
	assert(strcmp(result,"TEST\n") == 0);
	assert(!isEmpty(&reader));
	assert(!isFull(&reader));

	failed = read_line(&reader, result, sizeof(result));
	assert(!failed);
	assert(strcmp(result,"test\n") == 0);
	assert(isEmpty(&reader));
	assert(!isFull(&reader));

	close_reader(&reader);
}

void overflows_buffer_throws_exception()
{
	char result[30];
	struct line_reader reader;
	int failed;

	printf("Starting test overflows_buffer_throws_exception\n");
	clear_mock_reads();
	add_mock_read("12345");
	add_mock_read("67890");
	add_mock_read("\n");

	init_line_reader(&reader, mock_read, 10, 0);

	failed = read_line(&reader, result, sizeof(result));

	assert(failed == 3);

	close_reader(&reader);
}

void result_buffer_too_small_one_segment()
{
	char result[5];
	struct line_reader reader;
	int failed;

	printf("Starting test ReadLine_ResultBufferTooSmall\n");
	clear_mock_reads();
	add_mock_read("12345\n");

	init_line_reader(&reader, mock_read, 10, 0);

	failed = read_line(&reader, result, sizeof(result));

	assert(failed == 2);

	close_reader(&reader);
}

void result_buffer_too_small_double_segment()
{
	char result[7];
	struct line_reader reader;
	int failed;

	printf("Starting test result_buffer_too_small_double_segment\n");
	clear_mock_reads();
	add_mock_read("1234\n12345");
	add_mock_read("67\n");

	init_line_reader(&reader, mock_read, 10, 0);

	failed = read_line(&reader, result, sizeof(result));
	assert(!failed);
	assert(strcmp(result,"1234\n") == 0);
	assert(!isEmpty(&reader));
	assert(!isFull(&reader));

	failed = read_line(&reader, result, sizeof(result));
	assert(failed == 2);

	close_reader(&reader);
}

void wrap_around2()
{
	char result[30];
	struct line_reader reader;
	int failed;

	printf("Starting test wrap_around\n");
	clear_mock_reads();
	add_mock_read("test\n1");
	add_mock_read("2\n34");
	add_mock_read("56\n");
	
	init_line_reader(&reader, mock_read, 10, 0);

	failed = read_line(&reader, result, sizeof(result));
	assert(!failed);
	assert(strcmp(result,"test\n") == 0);
	assert(!isEmpty(&reader));
	assert(!isFull(&reader));
	
	failed = read_line(&reader, result, sizeof(result));
	assert(!failed);
	assert(strcmp(result,"12\n") == 0);
	assert(!isEmpty(&reader));
	assert(!isFull(&reader));

	failed = read_line(&reader, result, sizeof(result));
	assert(!failed);
	assert(strcmp(result,"3456\n") == 0);
	assert(isEmpty(&reader));
	assert(!isFull(&reader));

	close_reader(&reader);
}

// Test parameters
#define RANDOM_SEED 1
#define SEND_BUFFER_SIZE 2000000
#define MAX_LINE_LENGTH 20
#define CIRCULAR_BUFFER_SIZE 400
#define RESULT_BUFFER_SIZE 21

int nextToSend = 0;
char sendBuffer[SEND_BUFFER_SIZE];

int readTestData(char *result_buffer, int max_num_bytes_to_read, void *data)
{
	int dataLeftToSend = SEND_BUFFER_SIZE - nextToSend;
	if (dataLeftToSend == 0) return 0;

	int maxToSend = max_num_bytes_to_read < dataLeftToSend ? max_num_bytes_to_read : dataLeftToSend;
	int bytesToSend = (rand() % maxToSend) + 1;

	memcpy(result_buffer, &sendBuffer[nextToSend], bytesToSend);
	nextToSend += bytesToSend;

	return bytesToSend;
}

void random_test()
{
	printf("Starting test random_test\n");

	srand(RANDOM_SEED);

	// Generate some test data.
	int i;
	int countTilNewLine = rand() % MAX_LINE_LENGTH;
	for (i=0; i < sizeof(sendBuffer) - 1; i++) {
		if (countTilNewLine-- > 0) {
			sendBuffer[i] = (rand() % 26) + 'A'; }
		else {
			sendBuffer[i] = '\n';
			countTilNewLine = rand() % MAX_LINE_LENGTH;
		}
	}
	sendBuffer[sizeof(sendBuffer)-1] = '\n';

	// Setup a line reader to read it.
	struct line_reader reader;
	init_line_reader(&reader, readTestData, CIRCULAR_BUFFER_SIZE, 0);

	int linesRead = 0;
	char *startOfNextExpectedLine = sendBuffer;
	char result_buffer[RESULT_BUFFER_SIZE];
	while (1) {
		int result = read_line(&reader, result_buffer, sizeof(result_buffer));
		if (result == 1) break;

		int len = strlen(result_buffer);
		assert(result == 0);
		assert(result_buffer[len-1] == '\n');

		char *expectedEndOfLine = strchr(startOfNextExpectedLine, 0x0A);
		int expectedLength = expectedEndOfLine - startOfNextExpectedLine + 1;
		assert(len == expectedLength);
		
		assert(strncmp(result_buffer, startOfNextExpectedLine, expectedLength) == 0);
		startOfNextExpectedLine += expectedLength;
		linesRead++;
	}

	assert(startOfNextExpectedLine - sendBuffer == SEND_BUFFER_SIZE);
}

int main(int argc, char **argv)
{
	receive_one_line();
	one_line_in_multiple_pieces();
	receive_multiple_lines_at_once();
	wrap_around();
	fill_buffer();
	wrap_around_and_fill_buffer();
	to_the_end_of_the_buffer();
	to_the_end_of_the_buffer();
	overflows_buffer_throws_exception();
	result_buffer_too_small_one_segment();
	result_buffer_too_small_double_segment();
	wrap_around2();
	random_test();

	printf("All tests passed.\n");

	return 0;
}
