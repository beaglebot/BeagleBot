#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <stdint.h>
#include <pthread.h>
#include <bits/pthreadtypes.h>
#include <sys/socket.h>
#include <netinet/in.h>
#include <netdb.h>
#include <sys/types.h>
#include <math.h>
#include "pollcommands.h"
#include "prlist.h"

#define POLL_BUFFER_SIZE 4000
#define SMALL_TIME_PERIOD 20

struct poll_thread_args
{
	int bus;
	int port;
	bool verbose;
};

void process_add_poll_command(const char *command, char *reply, int reply_size)
{
	int delay;
	uint8_t address, reg, num_regs_to_read = 1, n; 
	struct poll_record *record;

	n = sscanf(command, "addpoll %d %hhd %hhd %hhd", &delay, &address, &reg, &num_regs_to_read);
	if (n < 3 || n >> 4) {
		fprintf(stderr, "ERROR => Incorrect arguments. Expected delay, slave address and i2c register, " \
						"and optionally num registers.\n");
		strcpy(reply, "ERROR\r\n");
		return;
	}

	record = (struct poll_record*)malloc(sizeof(struct poll_record));
	record->id = 0;
	record->delay = delay;
	record->address = address;
	record->reg = reg;
	record->num_regs_to_read = num_regs_to_read;
	record->next_poll_time = 0;

	pr_lock("papc");	
	pr_insert(record);
	pr_unlock();

	sprintf(reply, "OK %d\r\n", record->id);
}

void process_remove_poll_command(const char *command, char *reply, int reply_size)
{
	int id_to_remove, n;
	struct poll_record *record;

	n = sscanf(command, "rmpoll %d", &id_to_remove);
	if (n != 1) {
		fprintf(stderr, "ERROR => Incorrect arguments. Expected id of poll record to remove.\n");
		strcpy(reply, "ERROR\r\n");
		return;
	}

	pr_lock("prpc");
	record = pr_find(id_to_remove);
	if (record) {
		pr_remove(record);
		free(record);
		strcpy(reply, "OK\r\n");
	} else {
		fprintf(stderr, "ERROR => Couldn't find record with id %d.\n", id_to_remove);
		strcpy(reply, "ERROR\r\n");
	}
	pr_unlock();

}

void process_poll_connection(int con, int i2c_handle)
{
	int num_sent, delay, response_buffer_count, r, result_length, num_periods, i;
	struct poll_record *current;
	uint8_t i2c_buffer[256];
	char result[1000], response_buffer[POLL_BUFFER_SIZE], s[18];
	long time_till_next_run;

	/* Repeat until the connection is closed. */
	while (1)
	{
		response_buffer[0] = 0;
		response_buffer_count = 0;

		/* Loop until there are no more PollRecords due to run. */
		pr_lock("ppc");
		while (current = pr_get_head()) {

			/* Is the poll_record at the head of the list not due to run yet? */
			if (current && current->next_poll_time > get_time_in_ms() + SMALL_TIME_PERIOD) break;

			snprintf(result, sizeof(result), "%d: ", current->id);
			result_length = strlen(result);

			/* Query the I2C values. */
			read_i2c_multiple_as_string(i2c_handle, current->address, current->reg, current->num_regs_to_read, 
					&result[result_length], sizeof(result) - result_length);

			/* Add the string to the result_buffer, to be sent out over the network later. */
			result_length = strlen(result);
			if (response_buffer_count + result_length >= sizeof(response_buffer)) {
				fprintf(stderr, "ERROR => Poll buffer overrun.");
				break;
			}
			strcpy(&response_buffer[response_buffer_count], result);
			response_buffer_count += result_length;
			
			/* Calculate the new poll time. */
			current->next_poll_time += current->delay;
			time_till_next_run = current->next_poll_time - get_time_in_ms();
			if (time_till_next_run < SMALL_TIME_PERIOD) {
				num_periods = ceilf((float)(SMALL_TIME_PERIOD - time_till_next_run) / current->delay);
				fprintf(stderr, "WARNING: had to skip %d polls for poll ID %d\n", num_periods, current->id);
				current->next_poll_time += num_periods * current->delay;
			}
		
			/* Reinsert the record at the appropriate place in the list */	
			pr_remove(current);
			pr_insert(current);
		}
		pr_unlock();

		/* If the buffer isn't empty, send it to the client. */
		if (response_buffer_count > 0) {
			num_sent = send(con, response_buffer, response_buffer_count, MSG_NOSIGNAL);
			if (num_sent == -1) {
				perror("ERROR => Error sending poll buffer. The error was");
				break;
			}
		}

		/* How long to the next poll_record is due to run? */
		if (current) {
			delay = (current->next_poll_time - get_time_in_ms()) * 1000;
		}
		else {
			delay = 1000000;
		}

		usleep(delay);
	}
}

void *poll_thread_main(void *args)
{
	int bus, port, i2c_handle, sock, con, result;
	bool verbose;
	socklen_t client_address_size;
	struct sockaddr_storage client_address;
	char client_ip[INET6_ADDRSTRLEN];
	
	bus = ((struct poll_thread_args*)args)->bus;
	port = ((struct poll_thread_args*)args)->port;
	verbose = ((struct poll_thread_args*)args)->verbose;

	if (verbose) printf("Poll thread started\n");

	if (verbose) printf("Opening poll I2C handle\n");
	i2c_handle = open_i2c(bus, 1); 
	if (i2c_handle == -1) {
		perror("ERROR => Couldn't open i2c bus. The error was");
		exit(1);
	}

	sock = create_and_bind_tcp_socket(port);
	if (sock == -1) {
		perror("ERROR => Couldn't open socket. The error was");
		exit(1);
	}

	while (1)
	{
		if (verbose) printf("Listening for incoming poll connections\n");

		result = listen(sock, 20);
		if (result != 0) {
			perror("ERROR => Error attempting to listen on socket. The error was");
			exit(1);
		}

		client_address_size = sizeof(client_address);
		con = accept(sock, (struct sockaddr *) &client_address, &client_address_size);
		if (con < 0) {
			perror("ERROR => Error attempting to accept connection. The error was");
			exit(1);
		}

		get_address_ip((struct sockaddr*)&client_address, client_ip, sizeof(client_ip));
		printf("Poll connection accepted from %s\n", client_ip);

		process_poll_connection(con, i2c_handle);

		close(con);
		printf("Closed poll connection\n");
	}
	
	if (verbose) printf("Closing poll I2C\n");
	close(i2c_handle);

	if (verbose) printf("Closing poll socket\n");
	close(sock);

	if (verbose) printf("Removing poll records\n");

	pr_lock("ptm");
	pr_clear_and_free_all();
	pr_unlock();

	printf("Closing poll thread\n");
}

void start_poll_thread(int port, int bus, bool verbose)
{
	struct poll_thread_args *args;
	pthread_t poll_thread;

	args = (struct poll_thread_args *)malloc(sizeof(struct poll_thread_args));
	args->bus = bus;
	args->port = port;
	args->verbose = verbose;

	if (pthread_create(&poll_thread, NULL, &poll_thread_main, args)) {
		perror("ERROR => Error creating poll thread. The error was");
        exit(1);
	}
}
