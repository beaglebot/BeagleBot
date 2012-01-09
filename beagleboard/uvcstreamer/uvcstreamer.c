#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <errno.h>
#include <unistd.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <netinet/in.h>
#include <arpa/inet.h>
#include <netdb.h>
#include <linux/videodev2.h>
#include "webcam.h"
#include "network.h"
#include "../common/utils.h"
#include "../common/network_utils.h"

const char *default_device_path = "/dev/video0";

void show_usage()
{
	printf("USAGE: uvcstreamer -p {port} -w {width} -h {height} -f {fps} -o {format} [-q {quality}] [-d {device path}] [-s {skip}]\n");
	printf("where {port} - the port to listen on\n");
	printf("      {width} - the image width in pixels, eg 320 or 640\n");
	printf("      {height} - the image height in pixels, eg 240 or 480\n");
	printf("      {fps} - the number of frames per second, eg 5, 15, 20, or 30\n");
	printf("      {format} - the fourcc format eg YUYV or MJPG\n");  
	printf("      {quality} - a number from 0 to 100. Only relevant when the format is MJPG\n");
	printf("      {device path} - the path to the video device. Defaults to /dev/video0\n");
	printf("      {skip} - the number of frames to skip after sending a frame. Defaults to 0.\n");
	printf("\n");
}

void read_arguments(int argc, char *argv[], struct webcam *webcam, int *port)
{
	char *endptr;
	int c;

	*port = 0;
	memset(webcam, 0, sizeof *webcam);
	webcam->quality = -1;
   	strncpy(webcam->device_path, default_device_path, sizeof webcam->device_path);

	char *x;
	while ((c = getopt(argc, argv, ":p:w:h:f:o:q:d:s:")) != -1)
         switch (c)
           {
           case 'p':
             *port = strtol(optarg, &endptr, 10); 
			 if (endptr[0] != 0) *port = 0;
             break;

           case 'w':
             webcam->width= strtol(optarg, &endptr, 10); 
			 if (endptr[0] != 0) webcam->width = 0;
             break;

           case 'h':
             webcam->height = strtol(optarg, &endptr, 10); 
			 if (endptr[0] != 0) webcam->height = 0;
             break;

           case 'f':
             webcam->framerate = strtod(optarg, &endptr); 
			 if (endptr[0] != 0) webcam->framerate = -1;
             break;

           case 'q':
			 x = optarg;
             webcam->quality = strtod(optarg, &endptr); 
			 if (endptr[0] != 0) webcam->quality = 0;
             break;

		   case 'o':
			 if (strlen(optarg) != 4) fatal("The format parameter must be 4 characters, eg YUYV or MJPG");
		     strncpy(webcam->format, optarg, sizeof webcam->format);
			 break;

           case 'd':
		     strncpy(webcam->device_path, optarg, sizeof webcam->device_path);
             break;

           case 's':
             webcam->skip = strtol(optarg, &endptr, 10); 
			 if (endptr[0] != 0) webcam->skip = 0;
             break;

		   case ':':
			  fprintf(stderr, "ERROR => Argument -%c missing argument\n", optopt);
			  show_usage();
			  exit(1);

		   case '?':
			  fprintf(stderr, "ERROR => Unknown argument -%c\n", optopt);
			  show_usage();
			  exit(1);
		   default:
				show_usage();
				exit(1);
           }

	// Check all mandatory arguments were supplied.
	if (*port == 0 || webcam->width == 0 || webcam->height ==0 || webcam->framerate == 0 || webcam->format[0] == 0) {
		show_usage();
		exit(1);
	}

	// Print arguments
	printf("Port:       %d\n", *port);
	printf("Width:      %d\n", webcam->width);
	printf("Height:     %d\n", webcam->height);
	printf("Framerate:  %f\n", webcam->framerate);
	printf("Format:     %s\n", webcam->format);
	printf("Device:     %s\n", webcam->device_path);
	printf("Skip:       %d\n", webcam->skip);
	printf("\n");

}

int main (int argc, char ** argv)
{
	struct webcam webcam;
	int socket, accepted_socket, res, port;
	struct sockaddr_storage their_addr;
	socklen_t their_addr_size;
	char client[INET6_ADDRSTRLEN];
	char device_path[100];

	setlinebuf(stdout);

	printf("\n");
	printf("UVCSTREAMER\n");
	printf("===========\n");
	printf("\n");

	read_arguments(argc, argv, &webcam, &port);

	printf("Opening TCP port\n");
	socket = create_and_bind_tcp_socket(port);

	res = listen(socket, 20);
	if (res == -1) {
		fatal_errno("listen");
	}
	
	while (1) {
		printf("Waiting for connection\n");
		their_addr_size = sizeof their_addr;
		accepted_socket = accept(socket, (struct sockaddr *)&their_addr, &their_addr_size);

		inet_ntop(their_addr.ss_family, get_in_addr((struct sockaddr *)&their_addr), client, sizeof client);
		printf("Accepted connection from %s\n", client); 

		printf("Initializing webcam\n");
		if (init_webcam(&webcam) == -1) goto Cleanup;

		if (start_capturing(&webcam) == -1) goto Cleanup;

		while (process_frame(&webcam, accepted_socket) != -1);
		stop_capturing(&webcam);

Cleanup:
		printf("Closing camera\n");
		close_webcam(&webcam);

		printf("Closing connection\n");
		close(accepted_socket);
	}

	close(socket);

	printf("Done.\n");
}
