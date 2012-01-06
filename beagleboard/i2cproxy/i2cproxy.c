#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <stdint.h>
#include <stdbool.h>
#include <unistd.h>
#include <fcntl.h>
#include <errno.h>
#include <limits.h>
#include <netdb.h>
#include "../common/network_utils.h"
#include "linereader.h"
#include "commands.h"
#include "pollcommands.h"

#define DEFAULT_LOG_PATH "/var/log/i2cproxy.log" 
#define BUFFER_SIZE 4096

void show_usage()
{
	printf("USAGE: i2cproxy -p {port} -b {bus} [-v] [-d] [-l path]\n");
	printf("where {port} is the port number which the application will listen on\n");
	printf("      {bus} is the number of the i2c bus\n");
	printf("      -v indicates that all requests and responses should be logged\n");
	printf("      -d indicates it should run as a daemon\n");
	printf("      -l indicates the path the log should be saved to if run as a daemon\n");
	printf("         Defaults to %s\n", DEFAULT_LOG_PATH);
}

void read_args(int argc, char *argv[], int *port, int *bus, bool *daemonize, bool *verbose, char *log_path, int sizeOfLogPath)
{
	char *endptr;
	int c;
   	strcpy(log_path, DEFAULT_LOG_PATH);
	while ((c = getopt(argc, argv, "p:b:dvl:")) != -1)
         switch (c)
           {
           case 'p':
             *port = strtol(optarg, &endptr, 10); 
			 if (endptr[0] != 0) *port = -1;
             break;
           case 'b':
             *bus = strtol(optarg, &endptr, 10); 
			 if (endptr[0] != 0) *bus = -1;
             break;
           case 'd':
			 *daemonize = true;
			 break;
		   case 'v':
			 *verbose = true;
		     break;
		   case 'l':
		     strncpy(log_path, optarg, sizeOfLogPath);
		     break;
		   default:
			 show_usage();
			 exit(1);
           }

	/* Check all mandatory arguments were supplied. */
	if (*port == -1 || *bus == -1) {
		show_usage();
		exit(1);
	}

	printf("Cmd port:      %d\n", *port);
	printf("Poll port:     %d\n", (*port)+1);
	printf("Bus:           %d\n", *bus);
	printf("Daemonize:     %s\n", *daemonize ? "yes" : "no");
	printf("Verbose:       %s\n", *verbose ? "yes" : "no");
	if (daemonize) printf("Log path:  %s\n", log_path);
	printf("\n");

}

void daemonize_process(char *log_path)
{
	int i = fork();
	if (i < 0) { perror("ERROR => Error attempting to fork daemon. The error was"); exit(1); }
	if (i > 0)
	{
		printf("Successfully forked daemon.\n");
		exit(0);	
	}

	setsid();
	umask(022);
	for (i=getdtablesize();i>=0;--i) close(i); 
	i=open("/dev/null",O_RDWR);
	i=open(log_path, O_WRONLY|O_TRUNC|O_CREAT, 0644);
	i=dup(i);
}

int read_from_socket(char *buffer, int max_num_bytes_to_read, void *data)
{
	int handle = *(int*)data;
	return recv(handle, buffer, max_num_bytes_to_read, 0);
}

void process_command_connection(int con, int i2c_handle, bool verbose)
{
	char request[256];
	char *back;
	char response[256];

	struct line_reader reader;
	init_line_reader(&reader, read_from_socket, BUFFER_SIZE, &con);

	while (1) {

		int result = read_line(&reader, request, sizeof(request));
		if (result != 0) break;

		/* Get rid of any trailing \r or \n. */
		back = request + strlen(request) - 1;
		while (back >= request && (*back == '\n' || *back == '\r'))
			*(back--) = 0;

		if (verbose) printf("Request: %s\n", request);
		if (strncmp("ping", request, 4) == 0)
		{
			process_ping_command(request, response, sizeof(response));
		} 
		else if (strncmp("get", request, 3) == 0) {
			process_get_command(request, i2c_handle, response, sizeof(response));
		}
		else if (strncmp("set", request, 3) == 0) {
			process_set_command(request, i2c_handle, response, sizeof(response));
		}
		else if (strncmp("addpoll", request, 7) == 0) {
			process_add_poll_command(request, response, sizeof(response));
		}
		else if (strncmp("rmpoll", request, 5) == 0) {
			process_remove_poll_command(request, response, sizeof(response));
		}
		else if (strncmp("help", request, 4) == 0) {
			process_help(request, response, sizeof(response));
		}
		else 
		{
			fprintf(stderr, "ERROR: unknown command\n");
			strcpy(response, "ERROR\r\n");
		}

		if (verbose) printf("Response: %s", response);
		result = send(con, response, strlen(response), MSG_NOSIGNAL);
		if (result != strlen(response)) {
			fprintf(stderr, "ERROR: Error writing to socket\n");
			break;	
		}
	}

	close_reader(&reader);
}

void process_command_connections(int port, int bus, bool verbose)
{
	int i2c_handle, sock, result, con;
	socklen_t client_address_size;
	struct sockaddr_storage client_address;
	char client_ip[INET6_ADDRSTRLEN];

	if (verbose) printf("Opening command I2C handle\n");
	i2c_handle = open_i2c(bus, 1); 
	if (i2c_handle == -1) {
		perror("ERROR => Couldn't open i2c bus. The error was:");
		exit(1);
	}

	sock = create_and_bind_tcp_socket(port);
	if (sock == -1) {
		exit(1);
	}

	while (1) {
		
		if (verbose) printf("Listening for incoming command connections\n");

		result = listen(sock, 20);
		if (result != 0) {
			fprintf(stderr, "ERROR: Error attempting to listen on socket");
			continue;
		}

		client_address_size = sizeof(client_address);
		con = accept(sock, (struct sockaddr *) &client_address, &client_address_size);
		if (con < 0) {
			fprintf(stderr, "ERROR: Error attempting to accept connection");
			continue;
		}

		get_address_ip((struct sockaddr*)&client_address, client_ip, sizeof(client_ip));
		printf("Command connection accepted from %s\n", client_ip);

		process_command_connection(con, i2c_handle, verbose);

		printf("Closing command connection\n");
		close(con);

		printf("Removing all poll records\n");
		pr_lock("pcc");
		pr_clear_and_free_all();
		pr_unlock();
	}

	printf("Closing command socket\n");
	close(sock);

	printf("Closing command I2C handle\n");
	close(i2c_handle);
}

int main(int argc, char *argv[])
{
	char log_path[PATH_MAX];
	bool daemonize = false, verbose = false;
	int port=-1, bus=-1;

	setlinebuf(stdout);

	printf("\n");
	printf("I2CPROXY\n");
	printf("========\n");
	printf("\n");

	read_args(argc, argv, &port, &bus, &daemonize, &verbose, log_path, sizeof(log_path));
	if (daemonize) daemonize_process(log_path);

	pr_init();
	start_poll_thread(port + 1, bus, verbose);

	process_command_connections(port, bus, verbose);

	printf("Done\n");
	return 0;
}
