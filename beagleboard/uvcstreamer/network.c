#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/socket.h>
#include <netinet/in.h>
#include <arpa/inet.h>
#include <netdb.h>
#include <netinet/tcp.h>
#include "webcam.h"
#include "../common/utils.h"
#include "../common/network_utils.h"

#define UDP_BLOCK_SIZE 1200
#define IMAGE_HEADER_MAGIC_NUMBER 0x34343434

int image_count = 0;

struct image_header
{
	int magic_number;
	int width;
	int height;
	int size;
	char format[4];
	int image_number;
	long long timestamp;
};

int write_image(char *buffer, int length, struct webcam *webcam, int sock)
{
	struct image_header header;
	int res;

	header.magic_number = IMAGE_HEADER_MAGIC_NUMBER;
	header.image_number = image_count++;
	strncpy(header.format, webcam->format, sizeof webcam->format);
	header.width = webcam->width;
	header.height = webcam->height;
	header.size = length;
	header.timestamp = get_time_in_ms();
	res = sendall(sock, &header, sizeof(header));
	if (res == -1) return -1;

	res = sendall(sock, buffer, length);
	if (res == -1) return -1;

	return 0;	
}

