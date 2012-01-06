#include <stdio.h>
#include <stdlib.h>
#include <stdint.h>
#include <stdbool.h>
#include <string.h>
#include "commands.h"
#include "prlist.h"

void process_ping_command(const char *command, char *reply, int reply_size)
{
	strncpy(reply, "OK\r\n", reply_size);
}

void read_i2c_multiple_as_string(int i2c_handle, uint8_t address, uint8_t reg, int count, char *result, int result_size)
{
	int r, result_length, i;
	uint8_t i2c_buffer[256];
	char s[20];

	if (count > sizeof(i2c_buffer)) {
		fprintf(stderr, "ERROR => I2C buffer too small to read that many registers.");
		strncpy(result, "ERROR\r\n", result_size);
		return;
	}
	memset(i2c_buffer,0, sizeof(i2c_buffer));
	r = read_i2c_multiple(i2c_handle, address, reg, count, true, i2c_buffer);
	if (r != 0) {
		char message[100];
		snprintf(message, sizeof(message),
				"ERROR => Error reading %d i2c value(s) at address=%d, register=%d. The error was", 
				count, address, reg);
		perror(message);
		strncpy(result, "ERROR\r\n", result_size);
		return;
	}

	result_length = 0;
	result[0] = 0;
	for (i=0; i < count; i++) {
		snprintf(s, sizeof(s), "%s%u", i == 0 ? "" : " ", i2c_buffer[i]);
		result_length += strlen(s);
		if (result_length + 2 >= result_size) {
			fatal("ERROR => Overflowed result buffer.");
		}
		strcat(result, s);
	}
	strcat(result, "\r\n");
}

void process_get_command(const char *command, int i2c_handle, char *reply, int reply_size)
{
	int count = 1;
	uint8_t address, reg; 

	int n = sscanf(command, "get %hhd %hhd %d", &address, &reg, &count);
	if (n < 2 || n > 3) {
		fprintf(stderr, "ERROR => Incorrect arguments. Expected slave address, i2c register, and " \
				"optionally register count, not '%s'.\n", command);
		strcpy(reply, "ERROR\r\n");
		return;
	}

	read_i2c_multiple_as_string(i2c_handle, address, reg, count, reply, reply_size);
}

void process_set_command(const char *command, int i2c_handle, char *reply, int reply_size)
{
	uint8_t address, reg, value;
	int n = sscanf(command, "set %hhd %hhd %hhd", &address, &reg, &value);
	if (n != 3) {
		fprintf(stderr, "ERROR => Incorrect arguments. Expected slave 2ddress, i2c register and value.\n");
		strcpy(reply, "ERROR\r\n");
		return;
	}

	int result = write_i2c(i2c_handle, address, reg, value, 1);
	if (result == -1) {
		char message[100];
		snprintf(message, sizeof(message),
			"ERROR => Error writing i2c value at address=%d, register=%d. The error was", 
			(int)address, (int)reg);
		perror(message);
		strcpy(reply, "ERROR\r\n");
		return;
	}

	strncpy(reply, "OK\r\n", reply_size);
}

void process_help(const char *command, char *reply, int reply_size)
{
	snprintf(reply, reply_size, "Valid commands are:\r\n" 
			"ping\r\n" 
			"get <address> <register> [register count]\r\n" 
			"set <addreess> <register> <value>\r\n" 
			"addpoll <delay in ms> <address> <register> [register count]\r\n" 
			"rmpoll <poll id>\r\n" 
			"help\r\n");
}
